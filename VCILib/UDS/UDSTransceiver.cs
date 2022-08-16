using Logger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using VCILib.JobManagment;

namespace VCILib.UDS
{
    public static class NRC
    {
        private static readonly Dictionary<byte, string> nRCs;
        public static string GetENG(byte code) => nRCs.TryGetValue(code, out var str) ? str : string.Empty;

        static NRC()
        {
            if (nRCs != null) return;
            nRCs = new Dictionary<byte, string>() {
                {0x00,"PR(PositiveResponse)"                                },
                {0x10,"GR(GeneralReject)"                                   },
                {0x11,"SNS(ServiceNotSupported)."                           },
                {0x12,"SFNS(SubFunctionNotSupported)"                       },
                {0x13,"IMLOIF(IncorrectMessageLengthOrInvalidFormat)"       },
                {0x14,"RTL(ResponseTooLong)"                                },
                {0x21,"BRR(BusyRepeatReques)"                               },
                {0x22,"CNC(ConditionsNotCorrect)"                           },
                {0x23,"ISOSAERESRVD"                                        },
                {0x24,"RSE(RequestSequenceError)"                           },
                {0x25,"NRFSC(NoResponseFromSubnetComponent)"                },
                {0x26,"FPEORA(FailurePreventsExecutionOfRequestedAction)"   },
                {0x31,"ROOR(RequestOutOfRange)"                             },
                {0x32,"ISOSAERESRVD"                                        },
                {0x33,"SAD(SecurityAccessDenied)"                           },
                {0x34,"ISOSAERESRVD"                                        },
                {0x35,"IK(InvalidKey)"                                      },
                {0x36,"ENOA(ExceedNumberOfAttempts)"                        },
                {0x37,"RTDNE(RequiredTimeDelayNotExpired)"                  },
                {0x70,"UDNA(UploadDownloadNotAccepted)"                     },
                {0x71,"TDS(TransferDataSuspended)"                          },
                {0x72,"GPF(GeneralProgrammingFailure)"                      },
                {0x73,"WBSC(WrongBlockSequenceCounter)"                     },
                {0x78,"RCRRP(RequestCorrectlyReceived-ResponsePending)"     },
                {0x7E,"SFNSIAS(SubFunctionNotSupportedInActiveSession)"     },
                {0x7F,"SNSIAS(ServiceNotSupportedInActiveSession)"          },
                {0x80,"ISOSAERESRVD"                                        },
                {0x81,"RPMTH(RpmTooHigh)"                                   },
                {0x82,"RPMTL(RpmTooLow)"                                    },
                {0x83,"EIR(EngineIsRunning)"                                },
                {0x84,"EINR(EngineIsNotRunning)"                            },
                {0x85,"ERTTL(EngineRunTimeTooLow)"                          },
                {0x86,"TEMPTH(TemperatureTooHigh)"                          },
                {0x87,"TEMPTL(TemperatureTooLow)"                           },
                {0x88,"VSTH(VehicleSpeedTooHigh)"                           },
                {0x89,"VSTL(VehicleSpeedTooLow)"                            },
                {0x8A,"TPTH(Throttle/PedalTooHigh)"                         },
                {0x8B,"TPTL(Throttle/PedalTooLow)"                          },
                {0x8C,"TRNIG(TransmissionRangeNotInNeutral)"                },
                {0x8D,"TRNIG(TransmissionRangeNotInGear)"                   },
                {0x8E,"ISOSAERESRVD"                                        },
                {0x8F,"BSNC(BrakeSwitch(es)NotClosed)"                      },
                {0x90,"SLNIP(ShifterLeverNotInPark)"                        },
                {0x91,"TCCL(TorqueConverterClutchLocked)"                   },
                {0x92,"VTH(VoltageTooHigh)"                                 },
                {0x93,"VTL(VoltageTooLow)"                                  },
            };
            for (int i = 0; i <= 255; i++)
            {
                if (!nRCs.ContainsKey((byte)i))
                    nRCs.Add((byte)i, "ISOSAERESRVD");
            }
        }
    }
    public struct UDSResult
    {
        public bool Success { get; set; }
        public byte NRC { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class UDSTransceiver
    {
        private ICANVCI vci;
        public UDSTimingParams timing;

        public UDSTransceiver(ICANVCI vci)
        {
            this.vci = vci;
        }
        public UDSResult UDSRequest_backup(uint sendAddress, uint receiveAddress, IEnumerable<byte> udsRequest, out IEnumerable<byte> udsResponse)
        {
            UDSResult result = default;
            var error = false;
            var responseComplete = false;
            if (vci.Status != VCIStatus.Ready)
            {
                result.ErrorMessage = "VCI未准备好，无法发送";
                udsResponse = Enumerable.Empty<byte>();
                return result;
            }
            if (udsRequest == null || !udsRequest.Any())
            {
                result.ErrorMessage = "请求的数据为空";
                udsResponse = Enumerable.Empty<byte>();
                return result;
            }

            int sid = udsRequest.First();
            if (sid + 0x40 > 0xC7)
            {
                result.ErrorMessage = "请求的SID错误";
                udsResponse = Enumerable.Empty<byte>();
                return result;
            }

            vci.SetAndFilterSendRecvAddress(sendAddress, receiveAddress);
            if (udsRequest.Skip(7).Any())
            {
                udsResponse = Enumerable.Empty<byte>();
            }
            //发送单帧
            else
            {
                var bytes = new byte[8];
                var content = udsRequest.ToArray();
                bytes[0] = (byte)content.Length;
                Array.Copy(content, 0, bytes, 1, content.Length);
                //清除VCI缓冲
                if (vci.ClearBuffer())
                    //发送一帧
                    if (vci.SendOneFrame(bytes))
                        //接收数据
                        if (vci.Receive(out var recevieFrames))
                        {
                            var isECUBusy = true;
                            result.Success = false;
                            udsResponse = Enumerable.Empty<byte>();
                            result.ErrorMessage = "ECU无响应";
                            error = true;
                            var frames = recevieFrames.ToArray();
                            for (int i = 0; i < frames.Length; i++)
                            {
                                byte[]? frameData = LogAndTakeFrameData(frames[i]);
                                if (responseComplete)
                                {
                                    result.ErrorMessage = "ECU响应了多余数据";
                                    error = true;
                                }
                                else
                                {
                                    #region 等待，直到ECU不再发送“正忙”的指令
                                    if (IsNRC78Frame(frameData))
                                        if (isECUBusy)
                                            continue;
                                        else
                                        {
                                            result.ErrorMessage = "ECU答复错误";
                                            error = true;
                                        }
                                    else isECUBusy = false;
                                    #endregion
                                    //响应的指令为多帧传输
                                    if (IsFirstFrame(frameData, out int len))
                                    {
                                        //TO-DO:完善多帧传输
                                        var udsResponseData = new List<byte>(len);

                                        for (; i < frames.Length; i++)
                                        {

                                        }
                                        udsResponse = Enumerable.Empty<byte>();
                                    }
                                    //响应的指令为单帧
                                    else
                                    {
                                        udsResponse = ParseSingleFrame(sid, frameData, ref result);
                                        responseComplete = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            result.ErrorMessage = "VCI接收失败";
                            udsResponse = Enumerable.Empty<byte>();
                            error = true;
                        }
                    else
                    {
                        result.ErrorMessage = "VCI发送失败";
                        udsResponse = Enumerable.Empty<byte>();
                        error = true;
                    }
                else
                {
                    result.ErrorMessage = "清除VCI缓冲失败";
                    udsResponse = Enumerable.Empty<byte>();
                    error = true;
                }
            }
            return result;
        }
        public UDSResult UDSRequest(uint sendAddress, uint receiveAddress, IEnumerable<byte> udsRequest, out IEnumerable<byte> udsResponse)
        {
            UDSResult result = default;
            var error = false;
            var responseComplete = false;
            if (vci.Status != VCIStatus.Ready)
            {
                result.ErrorMessage = "VCI未准备好，无法发送";
                udsResponse = Enumerable.Empty<byte>();
                return result;
            }
            if (udsRequest == null || !udsRequest.Any())
            {
                result.ErrorMessage = "请求的数据为空";
                udsResponse = Enumerable.Empty<byte>();
                return result;
            }

            int sid = udsRequest.First();
            if (sid + 0x40 > 0xC7)
            {
                result.ErrorMessage = "请求的SID错误";
                udsResponse = Enumerable.Empty<byte>();
                return result;
            }

            vci.SetAndFilterSendRecvAddress(sendAddress, receiveAddress);
            //多帧传输
            if (udsRequest.Skip(7).Any())
            {
                udsResponse = Enumerable.Empty<byte>();
                var requestData = udsRequest.ToArray();

                var first2Bytes = 0x1000 | requestData.Length;
                var firstFrame = new byte[8];
                firstFrame[0] = (byte)((first2Bytes & 0xFF00) >> 8);
                firstFrame[1] = (byte)(first2Bytes & 0x00FF);
                for (int i = 0; i < 6; firstFrame[i + 2] = requestData[i++]) ;
                IEnumerable<byte> dataCurrentPos = requestData.Skip(6);
                //发送一帧
                if (LogAndSendFrame(firstFrame))
                {
                    int sent = 6;
                    long sendCompleted = 0;
                    foreach (var frame in vci.StartReceive(timing.P2CAN_Client))
                    {
                        var frameData = LogAndTakeFrameData(frame);
                        if (sendCompleted == 0)
                        {
                            if (frameData == null) continue;
                            var first = frameData[0];
                            if (first == 0x30)
                            {
                                var BS = frameData[1];
                                var STmin = GetSTmin(frameData[2]);//frame serial
                                byte SN = 1;
                                int BN = 0;
                                int frameDataLen;
                                for (; sent < requestData.Length; sent += frameDataLen)
                                {
                                    Thread.Sleep(STmin);
                                    frameDataLen = Math.Min(7, requestData.Length - sent);
                                    if (!LogAndSendFrame((new byte[] { (byte)(0x20 + SN) }).Concat(dataCurrentPos.Take(frameDataLen)).ToArray()))
                                    {
                                        result.ErrorMessage = "VCI发送失败";
                                        result.Success = false;
                                        error = true;
                                        vci.StopReceive();
                                        break;
                                    }
                                    if (++SN > 0xF) SN = 1;
                                    dataCurrentPos = dataCurrentPos.Skip(frameDataLen);
                                    if (++BN == BS)
                                    {
                                        BN = 0;
                                        break;
                                    }
                                }
                                if (sent == requestData.Length)
                                {
                                    sendCompleted = Environment.TickCount64;
                                    continue;
                                }
                            }
                            else if (first == 0x31)
                                continue;
                            else if (first == 0x32)
                            {
                                result.ErrorMessage = "ECU报错：流状态溢出";
                                result.Success = false;
                                error = true;
                                vci.StopReceive();
                                break;
                            }
                        }
                        else
                        {
                            if (Environment.TickCount64 - sendCompleted <= timing.P2CAN_Client)
                            {
                                result.Success = false;
                                result.ErrorMessage = "ECU未响应";
                                break;
                            }
                            else if (frameData != null)
                            {
                                ParseSingleFrame(sid, frameData, ref result);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    result.ErrorMessage = "VCI发送失败";
                    result.Success = false;
                    error = true;
                }
            }
            //发送单帧
            else
            {
                var bytes = new byte[8];
                var content = udsRequest.ToArray();
                bytes[0] = (byte)content.Length;
                Array.Copy(content, 0, bytes, 1, content.Length);
                //清除VCI缓冲
                if (vci.ClearBuffer())
                    //发送一帧
                    if (LogAndSendFrame(bytes))
                    {
                        var t = Environment.TickCount64;
                        result.Success = false;
                        result.ErrorMessage = "ECU未响应";
                        var isECUBusy = true;
                        var isLongMessage = false;
                        var totalLen = 0;
                        var totalFrames = 0;
                        var received = 0;
                        var receivedFrames = 0;
                        var SN = 0;
                        udsResponse = Enumerable.Empty<byte>();
                        foreach (var frame in vci.StartReceive(timing.P2CAN_Client))
                        {
                            var frameData = LogAndTakeFrameData(frame);
                            if (frameData == null)
                            {
                                if (Environment.TickCount64 - t > timing.P2CAN_Client)
                                    break;
                                continue;
                            }
                            #region 等待，直到ECU不再发送“正忙”的指令
                            if (IsNRC78Frame(frameData))
                                if (isECUBusy)
                                    continue;
                                else
                                {
                                    result.ErrorMessage = "ECU答复错误";
                                    result.Success = false;
                                    error = true;
                                    vci.StopReceive();
                                }
                            else isECUBusy = false;
                            #endregion
                            //收到首帧，或收到单帧
                            if (!isLongMessage)
                            {
                                if (IsFirstFrame(frameData, out totalLen))
                                {
                                    totalFrames = (totalLen - 6) / 7 + 1;
                                    if ((totalLen - 6) % 7 != 0) totalFrames++;
                                    isLongMessage = true;
                                    udsResponse = frameData.Skip(2 + GetDataOffset(sid));
                                    received = udsResponse.Count();
                                    //发送流控帧
                                    Thread.Sleep(10);
                                    if (LogAndSendFrame(new byte[] { 0x30, 0, 0, 0, 0, 0, 0, 0 }))
                                    {
                                        SN = 1;
                                        continue;
                                    }
                                    else
                                    {
                                        result.ErrorMessage = "VCI发送失败";
                                        result.Success = false;
                                        error = true;
                                        vci.StopReceive();
                                        break;
                                    }
                                }
                                else
                                {
                                    udsResponse = ParseSingleFrame(sid, frameData, ref result);
                                    //vci.StopReceive();
                                    //break;
                                }
                            }
                            //接收连续帧
                            else
                            {
                                var first = frameData[0];
                                //校验连续帧的序列号
                                if ((first & 0xF0) == 0x20 && (first & 0x0F) == SN)
                                {
                                    SN++;
                                    if (SN == 0x10) SN = 0x0F;
                                    //将收到的一帧数据，添加到总结果中
                                    udsResponse = udsResponse.Concat(frameData.Skip(1));
                                    received += 7;
                                    receivedFrames++;
                                    if(receivedFrames == totalFrames)
                                    {
                                        result.Success = true;
                                        vci.StopReceive();
                                        error = false;
                                        break;
                                    }
                                    if (received - totalLen >= 7)
                                    {
                                        result.ErrorMessage = "ECU实际发送长度与报文定义长度不符";
                                        result.Success = false;
                                        error = true;
                                        vci.StopReceive();
                                        break;
                                    }  
                                    continue;
                                }
                                else
                                {
                                    result.ErrorMessage = "ECU发送连续帧错误或序号错误";
                                    result.Success = false;
                                    error = true;
                                    vci.StopReceive();
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        result.ErrorMessage = "VCI发送失败";
                        udsResponse = Enumerable.Empty<byte>();
                        error = true;
                    }
                else
                {
                    result.ErrorMessage = "清除VCI缓冲失败";
                    udsResponse = Enumerable.Empty<byte>();
                    error = true;
                }
            }
            return result;
        }

        private static TimeSpan GetSTmin(byte stminbyte)
        {
            if (stminbyte <= 0x7F) return new TimeSpan(0, 0, 0, 0, stminbyte);
            else if (stminbyte >= 0xF1 && stminbyte <= 0xF9)
                return new TimeSpan((stminbyte - 0xF0) * 1000);
            return new TimeSpan(0, 0, 0, 0, 20);
        }
        byte[] LogAndTakeFrameData(object frame)
        {
            var data = vci.GetFrameData(frame);
            byte[] array = null;
            if (data != null)
            {
                array = data.ToArray();
                $"{vci.GetFrameTime(frame)}\t{vci.GetFrameAddress(frame):X2}\t{array.ByteArrToHexString()}".LogToFile();
            }
            return array;
        }
        bool LogAndSendFrame(byte[] frame)
        {
            if (vci.SendOneFrame(frame))
            {
                $"{DateTime.Now:HH:mm:ss.f}\t{vci.SendAddress:X2}\t{frame.ByteArrToHexString()}".LogToFile();
                return true;
            }
            else
            {
                $"VCI发送失败".LogToFile();
                return false;
            }
        }
        /// <summary>
        /// 判断是否为多帧传输的起始帧
        /// </summary>
        /// <param name = "frameData" ></ param >
        /// <returns></returns>
        bool IsFirstFrame(IEnumerable<byte> frameData, out int len)
        {
            if (frameData != null && frameData.Skip(1).Any())
            {
                var first = frameData.First();
                bool is1st = (first & 0xF0) == 0x10;
                if (is1st)
                    len = ((first & 0x0F) << 8) + frameData.Skip(1).First();
                else len = -1;
                return is1st;
            }
            len = -1;
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

        IEnumerable<byte> ParseSingleFrame(int udsSid, byte[] frame, ref UDSResult result)
        {
            var udsLen = frame[0];
            if (frame[1] == 0x7F)
            {
                result.NRC = frame[3];
                result.ErrorMessage = "负响应" + NRC.GetENG(result.NRC);
                return Enumerable.Empty<byte>();
            }
            if (udsSid + 0x40 != frame[1])
            {
                result.ErrorMessage = "ECU响应错误";
                return Enumerable.Empty<byte>();
            }
            result.Success = true;
            result.ErrorMessage = String.Empty;
            int offet = GetDataOffset(udsSid);
            //02 67 02
            //offset=2, udsLen=2
            return frame.Skip(offet).Take(udsLen - offet);
        }

        static int GetDataOffset(int sid)
        {
            return sid switch
            {
                0x10 or 0x50 => 1,
                0x11 or 0x51 => 1,
                0x27 or 0x67 => 2,
                0x28 or 0x68 => 2,
                0x29 or 0x69 => 2,
                0x3E or 0x7E => 1,
                0x83 or 0xC3 => 2,
                0x84 or 0xC4 => 2,
                0x85 or 0xC5 => 2,
                0x86 or 0xC6 => 2,
                0x87 or 0xC7 => 2,
                0x22 or 0x62 => 3,
                0x23 or 0x63 => 2,
                0x24 or 0x64 => 2,
                0x2A or 0x6A => 2,
                0x2C or 0x6C => 2,
                0x2E or 0x6E => 3,
                0x3D or 0x7D => 2,
                0x14 or 0x54 => 1,
                0x19 or 0x59 => 2,
                0x2F or 0x6F => 2,
                //31 01 F1 05
                0x31 or 0x71 => 4,
                0x34 or 0x74 => 4,
                0x35 or 0x75 => 4,
                0x36 or 0x76 => 4,
                0x37 or 0x77 => 4,
                0x38 or 0x78 => 4,
                _ => 1,
            };
        }
    }
}
