using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using VCILib.JobManagment;

namespace VCILib
{
    public class CANalystII : ICANVCI, INotifyPropertyChanged
    {
        #region Structs

        //1.ZLGCAN系列接口卡信息的数据类型。
        public struct VCI_BOARD_INFO
        {
            public UInt16 hw_Version;
            public UInt16 fw_Version;
            public UInt16 dr_Version;
            public UInt16 in_Version;
            public UInt16 irq_Num;
            public byte can_Num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public byte[] str_Serial_Num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public byte[] str_hw_Type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Reserved;
        }

        /*
        2.定义CAN信息帧的数据类型。
        public struct VCI_CAN_OBJ  //使用不安全代码
        {
            public uint ID;
            public uint TimeStamp;        //时间标识
            public byte TimeFlag;         //是否使用时间标识
            public byte SendType;         //发送标志。保留，未用
            public byte RemoteFlag;       //是否是远程帧
            public byte ExternFlag;       //是否是扩展帧
            public byte DataLen;          //数据长度
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.I1)]
            public byte[] Data;    //数据
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I1)]
            public byte[] Reserved;//保留位
        }
        */
        /// <summary>
        /// 数据帧结构体，VCI厂家定义的结构
        /// </summary>
        unsafe public struct VCI_CAN_OBJ  //使用不安全代码
        {
            /// <summary>
            /// 帧ID
            /// </summary>
            public uint ID;
            /// <summary>
            /// 时间标识
            /// </summary>
            public uint TimeStamp;
            /// <summary>
            /// 是否使用时间标识
            /// </summary>
            public byte TimeFlag;
            /// <summary>
            /// 发送标志。保留，未用
            /// </summary>
            public byte SendType;
            /// <summary>
            /// 是否是远程帧
            /// </summary>
            public byte RemoteFlag;
            /// <summary>
            /// 是否是扩展帧
            /// </summary>
            public byte ExternFlag;
            /// <summary>
            /// 数据长度
            /// </summary>
            public byte DataLen;
            /// <summary>
            /// 数据
            /// </summary>
            public fixed byte Data[8];
            /// <summary>
            /// 保留位
            /// </summary>
            public fixed byte Reserved[3];
        }

        //3.定义初始化CAN的数据类型
        public struct VCI_INIT_CONFIG
        {
            public UInt32 AccCode;
            public UInt32 AccMask;
            public UInt32 Reserved;
            public byte Filter;   //0或1接收所有帧。2标准帧滤波，3是扩展帧滤波。
            public byte Timing0;  //波特率参数，具体配置，请查看二次开发库函数说明书。
            public byte Timing1;
            /// <summary>
            /// 模式，0表示正常模式，1表示只听模式,2自测模式
            /// </summary>
            public byte Mode;     //模式，0表示正常模式，1表示只听模式,2自测模式
        }

        /*------------其他数据结构描述---------------------------------*/
        //4.USB-CAN总线适配器板卡信息的数据类型1，该类型为VCI_FindUsbDevice函数的返回参数。
        public struct VCI_BOARD_INFO1
        {
            public UInt16 hw_Version;
            public UInt16 fw_Version;
            public UInt16 dr_Version;
            public UInt16 in_Version;
            public UInt16 irq_Num;
            public byte can_Num;
            public byte Reserved;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public byte[] str_Serial_Num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] str_hw_Type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] str_Usb_Serial;
        }

        //5.滤波器结构体
        public struct VCI_FILTER_RECORD
        {
            public UInt32 ExtFrame;	//是否为扩展帧
            public UInt32 Start;
            public UInt32 End;
        }

        /*------------数据结构描述完成---------------------------------*/

        public struct CHGDESIPANDPORT
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public byte[] szpwd;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] szdesip;
            public Int32 desport;

            public void Init()
            {
                szpwd = new byte[10];
                szdesip = new byte[20];
            }
        }
        #endregion
        #region Extern
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_OpenDevice(UInt32 DeviceType, UInt32 DeviceInd, UInt32 Reserved);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_CloseDevice(UInt32 DeviceType, UInt32 DeviceInd);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_InitCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_INIT_CONFIG pInitConfig);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ReadBoardInfo(UInt32 DeviceType, UInt32 DeviceInd, ref VCI_BOARD_INFO pInfo);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_GetReceiveNum(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ClearBuffer(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_StartCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_ResetCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_Transmit(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_OBJ pSend, UInt32 Len);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_Receive(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_OBJ pReceive, UInt32 Len, Int32 WaitTime);

        [DllImport("controlcan.dll")]
        static extern UInt32 VCI_SetReference(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, UInt32 RefType, IntPtr pData);
        #endregion
        private VCIStatus status;
        private uint recvAddress;
        private uint sendAddress;
        private int filterNum = 0;
        private int filterStructSize = Marshal.SizeOf<VCI_FILTER_RECORD>();
        public VCI_FILTER_RECORD[] filterData = new VCI_FILTER_RECORD[4];

        public uint SendAddress { get => sendAddress; set => SetProperty(ref sendAddress, value); }
        public uint RecvAddress { get => recvAddress; set => SetProperty(ref recvAddress, value); }
        public VCIStatus Status { get => status; set => SetProperty(ref status, value); }
        public uint DeviceType { get; private set; } = 4;
        public uint DeviceIndex { get; private set; } = 0;
        public uint CanChannel { get; private set; } = 0;

        public event EventHandler CANInitialized;
        public event EventHandler CANClosed;
        public event PropertyChangedEventHandler? PropertyChanged;
        public JobScheduler Scheduler { get; private set; }
        public UDS.UDSTransceiver Transceiver { get; private set; }

        public CANalystII(uint DeviceIndex = 0, uint CanChannel = 0, uint DeviceType = 4)
        {
            this.DeviceType = DeviceType;
            this.DeviceIndex = DeviceIndex;
            this.CanChannel = CanChannel;
            Scheduler = new JobScheduler(this);
            Transceiver = new UDS.UDSTransceiver(this);
        }
        public bool ClearFilter()
        {
            filterNum = 0;
            var result = VCI_SetReference(21, DeviceIndex, CanChannel, 3, IntPtr.Zero);
            return result == 1;
        }

        public bool SetFilter(uint address, int filterId = -1) => SetFilter(address, true, filterId);
        public bool SetFilter(uint address, bool apply, int filterId = -1)
        {
            var id = (filterId >= 0 && filterId < filterData.Length) ? filterId : filterNum;
            filterNum++;
            filterData[id] = new VCI_FILTER_RECORD()
            {
                End = address,
                Start = address,
                ExtFrame = 0
            };

            uint result = 0;
            IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(filterData, id);
            result += VCI_SetReference(21, DeviceIndex, CanChannel, 1, pData);
            if (apply) result += VCI_SetReference(21, DeviceIndex, CanChannel, 2, IntPtr.Zero);
            return result == (apply ? 2 : 1);
        }
        public bool SetAndFilterSendRecvAddress(uint send, uint recv)
        {
            SendAddress = send;
            RecvAddress = recv;
            if (!ClearFilter()) return false;
            if (!SetFilter(SendAddress, apply: false)) return false;
            if (!SetFilter(RecvAddress)) return false;
            return true;
        }
        public bool Start()
        {
            if (status != VCIStatus.Closed) Stop();
            VCI_CloseDevice(DeviceType, DeviceIndex);
            if (VCI_OpenDevice(DeviceType, DeviceIndex, 0) == 0)
            {
                Console.WriteLine("打开设备失败,请检查设备类型和设备索引号是否正确");
                return false;
            }
            Status = VCIStatus.Connected;
            VCI_INIT_CONFIG config = new()
            {
                AccCode = 0,
                AccMask = 0xFFFFFFFF,
                Timing0 = 0,
                Timing1 = 0x1C,
                Filter = 1,
                Mode = 0
            };
            if (VCI_InitCAN(DeviceType, DeviceIndex, CanChannel, ref config) == 0)
            {
                Console.WriteLine($"{0}号通道初始化失败");
                return false;
            }
            Status = VCIStatus.Initialized;
            if (VCI_StartCAN(DeviceType, DeviceIndex, CanChannel) == 0)
            {
                Console.WriteLine($"{0}号通道启动失败");
                return false;
            }
            Status = VCIStatus.Ready;
            Console.WriteLine(String.Empty);
            Console.WriteLine($"{0}号通道启动");
            VCI_ClearBuffer(DeviceType, DeviceIndex, CanChannel);
            CANInitialized?.Invoke(this, null);
            return true;
        }

        public bool Stop()
        {
            if (status == VCIStatus.Closed)
                return true;
            if (VCI_ResetCAN(DeviceType, DeviceIndex, CanChannel) == 0)
            {
                Console.WriteLine($"{0}号通道关闭失败");
                return false;
            }
            CANClosed?.Invoke(this, null);
            Status = VCIStatus.Closed;
            Console.WriteLine($"{0}号通道关闭");
            return true;
        }

        public bool SendOneFrame(byte[] sendBytes)
        {
            if (status == VCIStatus.Ready)
            {
                if (sendBytes == null || sendBytes.Length == 0)
                    return false;
                var send = sendBytes;
                if (send.Length > 8) send = send.Take(8).ToArray();
                else if (send.Length < 8)
                {
                    byte[] b = { 0, 0, 0, 0, 0, 0, 0, 0 };
                    send = send.Concat(b.Take(8 - send.Length)).ToArray();
                }

                VCI_CAN_OBJ obj = new VCI_CAN_OBJ()
                {
                    DataLen = 8,
                    ExternFlag = 0,
                    ID = SendAddress,
                    RemoteFlag = 0,
                };
                obj.FillData(send);
                var ret = VCI_Transmit(DeviceType, DeviceIndex, CanChannel, ref obj, 1);
                if (ret == 0)
                {
                    Console.WriteLine($"{0}号通道发送失败");
                    return false;
                }
                // Console.WriteLine($" Send: {sendAddress:X2}\t{obj.GetDataAsString()}");
                return true;
            }
            Console.WriteLine($"{0}号通道无法发送：VCI未启动");
            return false;
        }

        public bool ClearBuffer() => VCI_ClearBuffer(DeviceType, DeviceIndex, CanChannel) != 0;

        VCI_CAN_OBJ[] m_recobj = new VCI_CAN_OBJ[1000];
        private bool receiving;

        public bool Receive(out IEnumerable<object>? data)
        {
            uint res = VCI_Receive(DeviceType, DeviceIndex, CanChannel, ref m_recobj[0], 1000, 100);
            //VCI_ClearBuffer(DeviceType, DeviceIndex, CanChannel);
            if (res == 0xFFFFFFFF)
            {
                data = default;
                return false;
            }
            else
            {
                data = Get();
                return true;
            }
            IEnumerable<object> Get()
            {
                for (uint i = 0; i < res; i++)
                    yield return m_recobj[i];
            }
        }

        public IEnumerable<object> StartReceive(int timeout)
        {
            receiving = true;
            var t0 = Environment.TickCount64;
            var anyFrame = false;
            while (receiving)
            {
                var t = Environment.TickCount64;
                if (Receive(out IEnumerable<object>? data))
                {
                    if (data.Any())
                        foreach (var item in data)
                        {
                            anyFrame = true;
                            yield return item;
                        }
                    else yield return null;
                    Thread.Sleep((int)Math.Max(0, 80 - (Environment.TickCount64 - t)));
                }
                else yield break;
                if (!anyFrame && Environment.TickCount64 - t0 > timeout) yield break;
            }
        }
        public void StopReceive()
        {
            receiving = false;
        }
        public IEnumerable<byte>? GetFrameData(object frame) => (frame is VCI_CAN_OBJ obj) ? obj.GetData() : default;
        public uint GetFrameAddress(object frame) => (frame is VCI_CAN_OBJ obj) ? obj.ID : default;
        public uint GetFrameTime(object frame) => (frame is VCI_CAN_OBJ obj && obj.TimeFlag == 1) ? obj.TimeStamp : default;
        protected bool SetProperty<T>(ref T field, T newValue, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }
    }
    public static class CANalystIIUtil
    {
        public static void FillData(this ref CANalystII.VCI_CAN_OBJ obj, byte[] bytes)
        {
            Fill(ref obj, bytes);
            unsafe void Fill(ref CANalystII.VCI_CAN_OBJ obj, byte[] arr)
            {
                for (int i = 0; i < 8; i++)
                    obj.Data[i] = arr[i];
            }
        }

        public static string GetDataAsString(this CANalystII.VCI_CAN_OBJ obj)
        {
            return makeStr(obj);
            unsafe static string makeStr(CANalystII.VCI_CAN_OBJ obj)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < obj.DataLen; i++)
                    sb.Append($"{obj.Data[i]:X2} ");
                if (sb.Length > 0) sb = sb.Remove(sb.Length - 1, 1);
                return sb.ToString();
            }
        }
        public static IEnumerable<byte> GetData(this CANalystII.VCI_CAN_OBJ obj)
        {
            return makeByteArr(obj);

            unsafe static IEnumerable<byte> makeByteArr(CANalystII.VCI_CAN_OBJ obj)
            {
                var arr = new byte[obj.DataLen];
                Marshal.Copy(new IntPtr(obj.Data), arr, 0, obj.DataLen);
                return arr;
            }
        }
    }
}