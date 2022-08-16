using Logger;
using Utils;
using VCILib.Jobs;

namespace VCILib.JobManagment
{
    /// <summary>
    /// 诊断任务调度器
    /// <para>调度器存储了一个任务队列，运行时可向队列添加任务</para>
    /// <para>调度器不断获取队列最开始的任务，并发送任务对应指令，然后移除已完成的任务</para>  
    /// </summary>
    public class JobScheduler
    {
        public ICANVCI VCI { get; set; }
        public EventHandler AllJobsCompleted { get; set; }
        /// <summary>
        /// 任务队列，添加的任务加入队列
        /// </summary>
        Queue<Job> CANJobs { get; set; } = new();
        AutoResetEvent anyJobAvailable = new AutoResetEvent(false);
        bool anyJob = false;
        private Thread thread;
        private CancellationTokenSource tokenSource;

        public JobScheduler(ICANVCI vCI)
        {
            VCI = vCI;
            VCI.CANInitialized += (o, e) =>
            {
                thread = new Thread(Run) { IsBackground = false };
                tokenSource = new CancellationTokenSource();
                thread.Start(tokenSource);
            };
            vCI.CANClosed += (o, e) => Terminate();
        }
        public void AddAndExecute(Job job)
        {
            anyJob = true;
            CANJobs.Enqueue(job);
            anyJobAvailable.Set();
            if (tokenSource.IsCancellationRequested)
                StartNewThread();
        }

        private void StartNewThread()
        {
            if (thread != null)
                tokenSource?.Cancel();
            thread = new Thread(Run) { IsBackground = true };
            thread.Start(tokenSource = new CancellationTokenSource());
        }

        public void Terminate()
        {
            tokenSource?.Cancel();
        }

        public void ExecuteDiagTask(DiagTask task) => ExecuteJobSequence(task.Jobs);
        public void ExecuteJobSequence(JobSequence sequence)
        {
            $"[INFO]流程开始".LogToFile();
            foreach (var item in sequence.Items)
            {
                IEnumerable<byte> bytes = item.CalculateSendBytes();
                int level;
                if (IsSecurityAccessSeedRequest(bytes, out level))
                {
                }
                else if (IsSecurityAccessKeyResponse(bytes, out level))
                {
                    bytes = bytes.Concat(SecurityAccess.GetKey(sequence.Seed[level - 1], item.SecurityAccessMethod, item.SecurityAccessParam));
                }
                var job = new Job(item.SendAddress, item.ResponseAddress, 8, bytes, sequence.TimingParameters);
                job.Response += (object o, IEnumerable<byte> e) =>
                {
                    var check = item.CalculateResponseCheckBytes();
                    if (check.Any())
                    {
                        if (IsSecurityAccessSeedResponse(e, out var level, out byte[] seed))
                        {
                            sequence.Seed[level - 1] = seed;
                        }
                        else
                        {
                            if (e.SequenceEqual(check))
                                ;
                            else
                            {
                                job.Error?.Invoke(o, item.ErrorMsg);
                                tokenSource?.Cancel();
                                $"[ERORR]期待接收:{check.ByteArrToHexString()}".LogToFile();
                                $"[ERORR]流程中止".LogToFile();
                            }
                        }
                    }
                };
                job.Error += (o, e) => e.LogToFile();
                AddAndExecute(job);
            }
            $"[INFO]流程完成".LogToFile();
        }
        public void ResumeJob(Job job)
        {
            //
        }
        void Run(object token)
        {
            var tokenSource = (CancellationTokenSource)token;
            while (VCI.Status == VCIStatus.Ready && !tokenSource.IsCancellationRequested)
            {
                if (CANJobs.Count != 0) anyJobAvailable.Set();
                else
                {
                    anyJob = false;
                    AllJobsCompleted?.Invoke(this, null);
                }
                anyJobAvailable.WaitOne();
                if (tokenSource.IsCancellationRequested) return;
                if (CANJobs.TryDequeue(out var job))
                {
                    var failTime = 0;
                    VCI.SetAndFilterSendRecvAddress(job.SendAddress, job.ResponseAddress);
                    if (job.SendLength <= 8)
                    {
                        byte[] sendBytes = job.SendBytes.ToArray();
                        if (tokenSource.IsCancellationRequested) return;
                        while (failTime < job.RetryTime)
                        {
                            if (VCI.ClearBuffer() && VCI.SendOneFrame(sendBytes))
                            {
                                if (tokenSource.IsCancellationRequested) return;
                                LogSendFrame(sendBytes);
                                Thread.Sleep(job.Timer.P2CAN_Client);
                                if (tokenSource.IsCancellationRequested) return;
                                if (VCI.Receive(out var recv))
                                {
                                    //取到不是NRC78的第一帧数据
                                    var firstFrame = recv.FirstOrDefault(f => !IsNRC78Frame(VCI.GetFrameData(f)));
                                    if (firstFrame != null)
                                    {
                                        var firstFrameData = VCI.GetFrameData(firstFrame);
                                        if (IsFirstFrame(firstFrameData)) { }
                                        else
                                        {
                                            if (tokenSource.IsCancellationRequested) return;
                                            LogOneFrame(firstFrame);
                                            job.Response?.Invoke(job, firstFrameData);
                                            if (job.JobFail && failTime < job.RetryTime)
                                            {
                                                failTime++;
                                                continue;
                                            }
                                            else break;
                                            if (recv.Count() > 1)
                                                job.Error?.Invoke(job, "ECU响应多于一帧");
                                        }
                                    }
                                    else
                                        job.Error?.Invoke(job, "ECU超时未响应");
                                }
                                else
                                    job.Error?.Invoke(job, "接收失败");
                            }
                            else
                                job.Error?.Invoke(job, "发送失败");
                        }
                    }
                    else { }
                }
            }
        }
        /// <summary>
        /// 判断是否为多帧传输的起始帧
        /// </summary>
        /// <param name="frameData"></param>
        /// <returns></returns>
        bool IsFirstFrame(IEnumerable<byte> frameData)
        {
            if (frameData != null && frameData.Any())
            {
                var first = frameData.First();
                return (first & 0x10) == 0x10;
            }
            return false;
        }
        /// <summary>
        /// 判断是否为NRC78的帧（ECU答复“请求已接收，但ECU正忙”）
        /// </summary>
        /// <param name="frameData"></param>
        /// <returns></returns>
        bool IsNRC78Frame(IEnumerable<byte> frameData)
        {
            if (frameData != null && frameData.Count() >= 4)
                return frameData.First() == 0x03 && frameData.ElementAt(1) == 0x7f && frameData.ElementAt(3) == 0x78;
            return false;
        }

        bool IsSecurityAccessSeedRequest(IEnumerable<byte> frameData, out int level)
        {
            if (frameData != null && frameData.Count() == 3)
            {
                var levelByte = frameData.ElementAt(2);
                if (frameData.First() == 0x02 && frameData.ElementAt(1) == 0x27 && (levelByte == 1 || levelByte == 3 || levelByte == 5))
                {
                    level = levelByte;
                    return true;
                }
            }
            level = -1;
            return false;
        }
        bool IsSecurityAccessSeedResponse(IEnumerable<byte> frameData, out int level, out byte[] seed)
        {
            if (frameData != null && frameData.Count() == 7)
            {
                var levelByte = frameData.ElementAt(2);
                if (frameData.First() == 0x06 && frameData.ElementAt(1) == 0x67 && (levelByte == 2 || levelByte == 4 || levelByte == 6))
                {
                    level = levelByte - 1;
                    seed = frameData.Skip(3).Take(4).ToArray();
                    return true;
                }
            }
            level = -1;
            seed = Array.Empty<byte>();
            return false;
        }
        bool IsSecurityAccessKeyResponse(IEnumerable<byte> frameData, out int level)
        {
            if (frameData != null && frameData.Count() == 7)
            {
                var levelByte = frameData.ElementAt(2);
                if (frameData.First() == 0x06 && frameData.ElementAt(1) == 0x27 && (levelByte == 2 || levelByte == 4 || levelByte == 6))
                {
                    level = levelByte - 1;
                    return true;
                }
            }
            level = -1;
            return false;
        }

        void LogSendFrame(byte[] frame) { $"{DateTime.Now:HH:mm:ss.f}\t{VCI.SendAddress:X2}\t{frame.ByteArrToHexString()}".LogToFile(); }
        void LogOneFrame(object frame) { $"{VCI.GetFrameTime(frame)}\t{VCI.GetFrameAddress(frame):X2}\t{VCI.GetFrameData(frame).ToArray().ByteArrToHexString()}".LogToFile(); }
    }
}
