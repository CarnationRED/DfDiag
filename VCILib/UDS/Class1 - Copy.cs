//using ModularCAN;
//using ModularCANWPF.CANCmd;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using USB2XXX_CAN_MultiThreadTest;

//namespace ModularCANWPF.CAN_UDS
//{
//    public class CANUDS
//    {
//        public static bool SessionNormal(ICANCmd cmd, int retrys = 3, int timeout = 1000)
//        {
//            retrys = Math.Max(retrys, 1);
//            while (retrys-- > 0)
//            {
//                if (!SessionEnterOnce(cmd, 1, timeout)) continue;
//                return true;
//            }
//            return false;
//        }
//        public static bool SessionBT(ICANCmd cmd, int retrys = 3, int timeout = 1000)
//        {
//            retrys = Math.Max(retrys, 1);
//            while (retrys-- > 0)
//            {
//                if (!SessionEnterOnce(cmd, 2, timeout)) continue;
//                return true;
//            }
//            return false;
//        }
//        public static bool SessionExtend(ICANCmd cmd, int retrys = 3, int timeout = 1000)
//        {
//            retrys = Math.Max(retrys, 1);
//            while (retrys-- > 0)
//            {
//                if (!SessionEnterOnce(cmd, 3, timeout)) continue;
//                return true;
//            }
//            return false;
//        }
//        private static bool SessionEnterOnce(ICANCmd cmd, int sessionID, int timeout = 1000)
//        {
//            if (cmd == null || !cmd.IsActive())
//                return false;
//            var sessionByteStr = sessionID.ToString("00");
//            string session;
//            switch (sessionID)
//            {
//                case 1: session = "Normal"; break;
//                case 2: session = "BootLoader"; break;
//                case 3: session = "Extend"; break;
//                default: return false;
//            }
//            Logger.Log($"Entering {session} Session", Logger.LogType.Information);
//            if (!cmd.Send($"02 10 {sessionByteStr}".To8ByteArray())) return false;
//            CANMsgRecord last = null;
//            if (!WaitOneResponce(cmd, ref last, timeout))
//            {
//                Logger.Log("Session Control: TimeOut", Logger.LogType.Error);
//                return false;
//            }
//            if (last != null)
//            {
//                if (last.MessageBytes[1] == 0x7F)
//                    if (last.MessageBytes[2] != 0x10)
//                    {
//                        Logger.Log("Session Control: response disorder", Logger.LogType.Error);
//                        return false;
//                    }
//                    else
//                    {
//                        Logger.Log($"Session Control: NRC{last.MessageBytes[3]:X2} {NRC.NRCs[last.MessageBytes[3]]}", Logger.LogType.Error);
//                        return false;
//                    }
//                else if (last.MessageBytes[1] == 0x50) return true;
//                return false;
//            }
//            else
//            {
//                Logger.Log("Session Control: unkown response", Logger.LogType.Error);
//                return false;
//            }
//        }
//        public static bool RequestSecuritySeed(ICANCmd cmd, int level, out byte[] seeds, int retrys = 1, int timeout = 1000)
//        {
//            retrys = Math.Max(retrys, 1);
//            while (retrys-- > 0)
//            {
//                if (!RequestSeedOnce(cmd, level, out seeds, timeout)) continue;
//                return true;
//            }
//            seeds = new byte[4];
//            return false;
//        }
//        private static bool RequestSeedOnce(ICANCmd cmd, int level, out byte[] seeds, int timeout = 1000)
//        {
//            seeds = null;
//            if (cmd == null || !cmd.IsActive())
//                return false;
//            var subFuncByteStr = level.ToString("00");
//            Logger.Log("Request Security Seed", Logger.LogType.Information);

//            if (!cmd.Send($"02 27 {subFuncByteStr}".To8ByteArray())) return false;
//            CANMsgRecord last = null;
//            if (!WaitOneResponce(cmd, ref last, timeout))
//            {
//                Logger.Log("Request Security Seed: TimeOut", Logger.LogType.Error);
//                return false;
//            }
//            Logger.Log("Seed:" + last.Message);
//            if (last.MessageBytes[1] == 0x7F)
//                if (last.MessageBytes[2] != 0x27)
//                {
//                    Logger.Log("Request Security Seed: response disorder", Logger.LogType.Error);
//                    return false;
//                }
//                else
//                {
//                    Logger.Log($"Request Security Seed: NRC{last.MessageBytes[3]:X2} {NRC.NRCs[last.MessageBytes[3]]}", Logger.LogType.Error);
//                    return false;
//                }
//            else if (last.MessageBytes[1] == 0x67)
//            {
//                seeds = last.MessageBytes.Skip(3).Take(4).ToArray();
//                return true;
//            }
//            else
//            {
//                Logger.Log("Request Security Seed: unkown response", Logger.LogType.Error);
//                return false;
//            }
//        }
//        public static bool SendSecurityKey(ICANCmd cmd, int level, IEnumerable<byte> key, int retrys = 1, int timeout = 1000)
//        {
//            if (key == null || key.Count() < 4) return false;
//            key = key.Take(4);
//            retrys = Math.Max(retrys, 1);
//            while (retrys-- > 0)
//            {
//                if (!SendKeyOnce(cmd, level + 1, key, timeout)) continue;
//                return true;
//            }
//            return false;
//        }
//        private static bool SendKeyOnce(ICANCmd cmd, int level, IEnumerable<byte> key, int timeout = 1000)
//        {
//            if (cmd == null || !cmd.IsActive())
//                return false;
//            var subFuncByteStr = level.ToString("00");
//            Logger.Log("Send Security Key", Logger.LogType.Information);

//            if (!cmd.Send($"06 27 {subFuncByteStr} {key.ToArray().ByteArrToString()}".To8ByteArray())) return false;
//            CANMsgRecord last = null;
//            if (!WaitOneResponce(cmd, ref last, timeout))
//            {
//                Logger.Log("Send Security Key: TimeOut", Logger.LogType.Error);
//                return false;
//            }
//            if (last.MessageBytes[1] == 0x7F)
//                if (last.MessageBytes[2] != 0x27)
//                {
//                    Logger.Log("Send Security Key: response disorder", Logger.LogType.Error);
//                    return false;
//                }
//                else
//                {
//                    Logger.Log($"Send Security Key: NRC{last.MessageBytes[3]:X2} {NRC.NRCs[last.MessageBytes[3]]}", Logger.LogType.Error);
//                    return false;
//                }
//            else if (last.MessageBytes[1] == 0x67) return true;
//            else
//            {
//                Logger.Log("Send Security Key: unkown response", Logger.LogType.Error);
//                return false;
//            }
//        }
//        public static bool ReadDID(ICANCmd cmd, string did, out byte[] data, int retrys = 1, int timeout = 1000)
//        {
//            var didBytes = did.ToByteArray(2);
//            if (didBytes == null)
//            {
//                data = null;
//                return false;
//            }
//            retrys = Math.Max(retrys, 1);
//            while (retrys-- > 0)
//            {
//                if (!ReadDIDOnce(cmd, did, out data, timeout)) continue;
//                return true;
//            }
//            data = null;
//            return false;
//        }
//        private static bool ReadDIDOnce(ICANCmd cmd, string did, out byte[] data, int timeout = 1000)
//        {
//            data = null;
//            if (cmd == null || !cmd.IsActive())
//                return false;
//            Logger.Log($"Read DID {did}", Logger.LogType.Information);
//            if (!cmd.Send($"03 22 {did}".To8ByteArray())) return false;
//            CANMsgRecord last = null;
//            if (!WaitOneResponce(cmd, ref last, timeout))
//            {
//                Logger.Log("Read DID: TimeOut", Logger.LogType.Error);
//                return false;
//            }
//            if (last != null)
//                if ((last.MessageBytes[0] & 0x10) == 0x10)
//                {
//                    return LongMsgRecv(cmd, last.MessageBytes, out data, timeout);
//                }
//                else
//                {
//                    if (last.MessageBytes[1] == 0x7F)
//                        if (last.MessageBytes[2] != 0x22)
//                        {
//                            Logger.Log("Read DID: response disorder", Logger.LogType.Error);
//                            return false;
//                        }
//                        else
//                        {
//                            Logger.Log($"Read DID: NRC{last.MessageBytes[3]:X2} {NRC.NRCs[last.MessageBytes[3]]}", Logger.LogType.Error);
//                            return false;
//                        }
//                    else if (last.MessageBytes[1] == 0x62)
//                    {
//                        data = last.MessageBytes.Skip(4).Take(4).ToArray();
//                        return true;
//                    }
//                    else
//                    {
//                        Logger.Log("Read DID: unkown response", Logger.LogType.Error);
//                        return false;
//                    }
//                }
//            else return false;
//        }
//        public static bool WriteDID(ICANCmd cmd, string did, byte[] data, int retrys = 1, int timeout = 1000)
//        {
//            var didBytes = did.ToByteArray(2);
//            if (didBytes == null)
//            {
//                data = null;
//                return false;
//            }
//            retrys = Math.Max(retrys, 1);
//            while (retrys-- > 0)
//            {
//                if (!WriteDIDOnce(cmd, didBytes, data, timeout)) continue;
//                return true;
//            }
//            data = null;
//            return false;
//        }
//        private static bool WriteDIDOnce(ICANCmd cmd, byte[] didBytes, byte[] data, int timeout)
//        {
//            byte[] sendBytes = new byte[] { (byte)(data.Length + 3), 0x2E }.Concat(didBytes).Concat(data).ToArray();
//            if (data.Length <= 4)
//            {
//                return cmd.Send(sendBytes);
//            }
//            return LongMsgSend(cmd, sendBytes, timeout);
//        }
//        public static bool LongMsgSend(ICANCmd cmd, byte[] data, int timeout = 1000)
//        {
//            if (cmd == null || !cmd.IsActive()) return false;
//            if (data == null || data.Length <= 8 || data.Length > 0xFFFF)
//            {
//                Logger.Log("Data length not correct, send abort", Logger.LogType.Error);
//                return false;
//            }

//            bool recv = false;
//            CANMsgRecord FC = default;
//            IEnumerable<byte> dataCurrentPos;
//            int t = Environment.TickCount;
//            var waitFC = new EventHandler<ReceivedOrSentEventArgs>((o, e) =>
//            {
//                var cfs = e.msgs.Where(m =>
//                {
//                    if (m.Address == cmd.RecvAddress)
//                    {
//                        var FS = m.MessageBytes[0];
//                        if (FS >= 0x30 && FS <= 0x32)
//                            return true;
//                    }
//                    return false;
//                });
//                //wait for CTS flowcontrol
//                if (cfs.Any())
//                    lock (FC = cfs.Last())
//                    {
//                        if (FC.MessageBytes[1] == 0x31)
//                        {
//                            FC = null;
//                            t = Environment.TickCount;
//                        }
//                        else if (FC.MessageBytes[2] == 0x32)
//                            Logger.Log("Server reported overflow", Logger.LogType.Information);
//                    }
//            });
//            cmd.ReceivedOrSentEvent += waitFC;
//            #region FF
//            if (data.Length <= 0xFFF)
//            {
//                byte[] arr = BitConverter.GetBytes((short)(0x1000 | data.Length));
//                if (BitConverter.IsLittleEndian) Array.Reverse(arr);
//                if (!cmd.Send(arr.Concat(data.Take(6)).ToArray()))
//                    return false;
//                dataCurrentPos = data.Skip(6);
//            }
//            else
//            {
//                byte[] arr = BitConverter.GetBytes(data.Length);
//                if (BitConverter.IsLittleEndian) Array.Reverse(arr);
//                if (!cmd.Send((new byte[] { 0x10, 0x00 }).Concat(arr).Concat(data.Take(2)).ToArray()))
//                    return false;
//                dataCurrentPos = data.Skip(2);
//            }
//            #endregion
//            int BS = 0;
//            TimeSpan STmin = default;
//            #region wait for CTS FC
//            while (Environment.TickCount - t < timeout)
//            {
//                Thread.Sleep(10);
//                if (!cmd.BusyReceiving) cmd.Recv();
//                if (FC != null)
//                {
//                    //FC:overflow
//                    if (FC.MessageBytes[0] == 0x32)
//                        return false;
//                    cmd.ReceivedOrSentEvent -= waitFC;
//                    BS = FC.MessageBytes[1];
//                    STmin = GetSTmin(FC.MessageBytes[2]);
//                    break;
//                }
//            }
//            #endregion

//            //frame serial
//            byte SN = 1;
//            int BN = 0;
//            int frameDataLen;
//            for (int i = data.Length > 0xFFF ? 2 : 6; i < data.Length; i += frameDataLen)
//            {
//                Thread.Sleep(STmin);
//                frameDataLen = Math.Min(7, data.Length - i);
//                if (!cmd.Send((new byte[] { (byte)(0x20 + SN) }).Concat(dataCurrentPos.Take(frameDataLen)).ToArray())) return false;
//                if (++SN > 0xF) SN = 1;
//                dataCurrentPos = dataCurrentPos.Skip(frameDataLen);
//                if (++BN == BS)
//                {
//                    BN = 0;
//                    #region wait for CTS FC
//                    cmd.ReceivedOrSentEvent += waitFC;
//                    t = Environment.TickCount;
//                    FC = default;
//                    while (Environment.TickCount - t < timeout)
//                    {
//                        Thread.Sleep(10);
//                        if (FC != null)
//                        {
//                            //FC:overflow
//                            if (FC.MessageBytes[0] == 0x32)
//                                return false;
//                            cmd.ReceivedOrSentEvent -= waitFC;
//                            BS = FC.MessageBytes[1];
//                            STmin = GetSTmin(FC.MessageBytes[2]);
//                            break;
//                        }
//                    }
//                    #endregion
//                }
//            }
//            return true;
//        }
//        public static TimeSpan GetSTmin(byte stminbyte)
//        {
//            if (stminbyte <= 0x7F) return new TimeSpan(0, 0, 0, 0, stminbyte);
//            else if (stminbyte >= 0xF1 && stminbyte <= 0xF9)
//                return new TimeSpan((stminbyte - 0xF0) * 1000);
//            return new TimeSpan(0, 0, 0, 0, 20);
//        }

//        public static bool LongMsgRecv(ICANCmd cmd, byte[] firstFrameReceived, out byte[] data, int timeout = 2000)
//        {
//            data = Array.Empty<byte>();
//            if (cmd == null || !cmd.IsActive()) return false;
//            CANMsgRecord response = default;
//            //Receive first frame
//            if (firstFrameReceived == null || firstFrameReceived.Length != 8)
//            {
//                int t = Environment.TickCount;
//                if (!WaitOneResponce(cmd, ref response))
//                {
//                    Logger.Log("First Frame Receive TimeOut", Logger.LogType.Error);
//                    return false;
//                }
//                else if ((response.MessageBytes[0] & 0x10) != 0x10)
//                {
//                    Logger.Log("First Frame lost, or message length less than 9", Logger.LogType.Error);
//                    return false;
//                }
//                timeout -= Environment.TickCount - t;
//                firstFrameReceived = response.MessageBytes;
//            }
//            var dataLen = ((firstFrameReceived[0] << 8) + firstFrameReceived[1]) & 0b0000111111111111;
//            if (dataLen < 8)
//            {
//                Logger.Log("Message length less than 8, use UDS Single Frame", Logger.LogType.Error);
//                return false;
//            }
//            var dataList = new List<byte>(dataLen);
//            dataList.AddRange(firstFrameReceived.Skip(2));
//            //Flow control
//            if (!cmd.Send("30".To8ByteArray()))
//            {
//                Logger.Log("Send Flow Control failed", Logger.LogType.Error);
//                return false;
//            }
//            var left = dataLen - 6;

//            byte[][] continuousFrames = null;
//            //Receive all continuous frames
//            if (!WaitNResponces(cmd, continuousFrames, left / 7 + (left % 7 == 0 ? 0 : 1), 20, Math.Max(1000, timeout)))
//            {
//                Logger.Log("Continuous frames receive timeout", Logger.LogType.Error);
//                return false;
//            }
//            var SN = 1;
//            var i = 0;
//            //Read data from continuous frames
//            while (left > 0)
//            {
//                var bytes = continuousFrames[i++];
//                if (i == continuousFrames.Length)
//                {
//                    Logger.Log($"Internal error, message length missmatch", Logger.LogType.Error);
//                    return false;
//                }
//                if ((bytes[0] & 0b00001111) != SN++)
//                {
//                    Logger.Log($"Continuous Frame sequence error, expected 0x{(0x20 + SN):X2}, received 0x{(bytes[0]):X2}", Logger.LogType.Error);
//                    return false;
//                }
//                if (SN > 0xF) SN = 0x1;
//                left = Math.Max(0, left - 7);
//                dataList.AddRange(bytes.Skip(1).Take(Math.Min(7, left)));
//            }
//            data = dataList.ToArray();
//            return true;
//        }
//        /*public static bool LongMsgRecv(ICANCmd cmd, IEnumerable<byte[]> msgReceived, out byte[] data, int timeout = 2000)
//          {
//              data = Array.Empty<byte>();
//              if (cmd == null || !cmd.IsActive()) return false;
//              CANMsgRecord response = default;
//              byte[] firstFrameReceived = msgReceived.First();
//              //Receive first frame
//              if (firstFrameReceived == null || firstFrameReceived.Length != 8)
//              {
//                  int t = Environment.TickCount;
//                  if (!WaitOneResponce(cmd, ref response))
//                  {
//                      Logger.Log("First Frame Receive TimeOut", Logger.LogType.Error);
//                      return false;
//                  }
//                  else if ((response.MessageBytes[0] & 0x10) != 0x10)
//                  {
//                      Logger.Log("First Frame lost, or message length less than 9", Logger.LogType.Error);
//                      return false;
//                  }
//                  timeout -= Environment.TickCount - t;
//                  firstFrameReceived = response.MessageBytes;
//              }
//              var dataLen = ((firstFrameReceived[0] << 8) + firstFrameReceived[1]) & 0b0000111111111111;
//              if (dataLen < 8)
//              {
//                  Logger.Log("Message length less than 8, use UDS Single Frame", Logger.LogType.Error);
//                  return false;
//              }
//              var dataList = new List<byte>(dataLen);
//              dataList.AddRange(firstFrameReceived.Skip(2));
//              if (msgReceived.Count() > 1)
//              {
//                  var cfs = msgReceived.Skip(1).SkipWhile(m => m[0] != 0x30).TakeWhile(m => (m[0] & 0x20) == 0x20);
//              }
//              //Flow control
//              if (!cmd.Send("30".To8ByteArray()))
//              {
//                  Logger.Log("Send Flow Control failed", Logger.LogType.Error);
//                  return false;
//              }
//              var left = dataLen - 6;
          
//              byte[][] continuousFrames = null;
//              //Receive all continuous frames
//              if (!WaitNResponces(cmd, continuousFrames, left / 7 + (left % 7 == 0 ? 0 : 1), 20, Math.Max(1000, timeout)))
//              {
//                  Logger.Log("Continuous frames receive timeout", Logger.LogType.Error);
//                  return false;
//              }
//              var SN = 1;
//              var i = 0;
//              //Read data from continuous frames
//              while (left > 0)
//              {
//                  var bytes = continuousFrames[i++];
//                  if (i == continuousFrames.Length)
//                  {
//                      Logger.Log($"Internal error, message length missmatch", Logger.LogType.Error);
//                      return false;
//                  }
//                  if ((bytes[0] & 0b00001111) != SN++)
//                  {
//                      Logger.Log($"Continuous Frame sequence error, expected 0x{(0x20 + SN):X2}, received 0x{(bytes[0]):X2}", Logger.LogType.Error);
//                      return false;
//                  }
//                  if (SN > 0xF) SN = 0x1;
//                  left = Math.Max(0, left - 7);
//                  dataList.AddRange(bytes.Skip(1).Take(Math.Min(7, left)));
//              }
//              data = dataList.ToArray();
//              return true;
        
//        }*/
//        public static bool WaitOneResponce(ICANCmd cmd, ref CANMsgRecord res, int checkInterval = 20, int timeout = 1000)
//        {
//            //lock (response)
//            {
//                var response = res = null;
//                int t = Environment.TickCount;
//                var wait = new EventHandler<ReceivedOrSentEventArgs>((o, e) =>
//                {
//                    var cfs = e.msgs.Where(m => m.Address == cmd.RecvAddress);
//                    if (cfs.Any())
//                        lock (response = cfs.Last())
//                            if (response.MessageBytes[0] < 0x08 && response.MessageBytes[1] == 0x7F && response.MessageBytes[3] == 0x78)
//                            {
//                                response = null;
//                                t = Environment.TickCount;
//                            }
//                });
//                cmd.ReceivedOrSentEvent += wait;
//                t = Environment.TickCount;
//                while (Environment.TickCount - t < timeout && response == null)
//                {
//                    if (!cmd.BusyReceiving) cmd.Recv();
//                    Thread.Sleep(checkInterval);
//                }
//                cmd.ReceivedOrSentEvent -= wait;
//                res = response;
//                return response != null;
//            }
//        }
//        void a(object o, EventArgs e)
//        {

//        }
//        private static bool WaitNResponces(ICANCmd cmd, byte[][] responses, int nResponses, int checkInterval = 20, int timeout = 1000)
//        {
//            CANMsgRecord response = default;
//            responses = new byte[nResponses][];
//            int nReceived = 0;
//            int t = Environment.TickCount;
//            var wait = new EventHandler<ReceivedOrSentEventArgs>((o, e) =>
//            {
//                var cfs = e.msgs.Where(m => m.Address == cmd.RecvAddress);
//                if (cfs.Any())
//                    lock (response = cfs.Last())
//                        if (response.MessageBytes[0] < 0x08 && response.MessageBytes[1] == 0x7F && response.MessageBytes[3] == 0x78)
//                        {
//                            response = null;
//                            t = Environment.TickCount;
//                        }
//                        else if (nReceived < nResponses)
//                            responses[nReceived++] = response.MessageBytes;
//            });
//            cmd.ReceivedOrSentEvent += wait;
//            t = Environment.TickCount;
//            while (Environment.TickCount - t < timeout && nReceived < nResponses)
//            {
//                if (!cmd.BusyReceiving) cmd.Recv();
//                Thread.Sleep(checkInterval);
//            }
//            cmd.ReceivedOrSentEvent -= wait;
//            return nReceived == nResponses;
//        }
//        public static async Task<byte[]> SendRecvCheck(ICANCmd cmd, byte[] SendBytes, byte[] CheckBytes)
//        {
//            if (cmd == null || !cmd.IsActive())
//            {
//                Logger.Log("CAN not opened", Logger.LogType.Error);
//                return null;
//            }
//            return await Task.Run<byte[]>(() =>
//            {
//                CANMsgRecord response = default;
//                int t;
//                var wait = new EventHandler<ReceivedOrSentEventArgs>((o, e) =>
//                {
//                    var cfs = e.msgs.Where(m => m.Address == cmd.RecvAddress);
//                    if (cfs.Any())
//                        lock (response = cfs.Last())
//                            if (response.MessageBytes[0] < 0x08 && response.MessageBytes[1] == 0x7F && response.MessageBytes[3] == 0x78)
//                            {
//                                response = null;
//                                t = Environment.TickCount;
//                            }
//                });
//                if (SendBytes != null && SendBytes.Length > 0)
//                    if (!cmd.Send(SendBytes)) return null;
//                cmd.ReceivedOrSentEvent += wait;
//                t = Environment.TickCount;
//                while (Environment.TickCount - t < 1000 && response == null)
//                {
//                    if (!cmd.BusyReceiving) cmd.Recv();
//                    Thread.Sleep(20);
//                }
//                cmd.ReceivedOrSentEvent -= wait;
//                if (response == null) return null;
//                var RecvBytes = response.MessageBytes;
//                if (RecvBytes.Length < CheckBytes.Length)
//                    return null;
//                for (int i = 0; i < CheckBytes.Length; i++)
//                    if (RecvBytes[i] != CheckBytes[i])
//                        return null;
//                return RecvBytes;
//            });
//        }
//    }
//}
