using VCILib.JobManagment;

namespace VCILib.Jobs
{
    /// <summary>
    /// 发送指令的诊断任务
    /// </summary>
    public class Job
    {
        public Job(uint SendAddress, uint ResponseAddress, int SendLength, IEnumerable<byte> SendBytes, UDSTimingParams Timer)
        {
            this.SendAddress = SendAddress;
            this.ResponseAddress = ResponseAddress;
            this.SendLength = SendLength;
            this.SendBytes = SendBytes;
            this.Timer = Timer;
        }

        public uint SendAddress { get; }
        public uint ResponseAddress { get; }
        public int SendLength { get; }
        /// <summary>
        /// 
        /// </summary>
        public bool JobFail { get; set; }
        /// <summary>
        /// 失败后的重试次数
        /// </summary>
        public int RetryTime { get; set; }
        public IEnumerable<byte> SendBytes { get; }
        /// <summary>
        /// 收到ECU响应报文的事件
        /// </summary>
        public EventHandler<IEnumerable<byte>>? Response { get; set; }
        /// <summary>
        /// 发生错误的事件，如发送失败、ECU无响应等
        /// </summary>
        public EventHandler<string>? Error { get; set; }
        /// <summary>
        /// 诊断的时间参数
        /// </summary>
        public UDSTimingParams Timer { get; }
    }
}
