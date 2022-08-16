using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using VCILib.JobManagment;

namespace VCILib.VCI
{
    public class CANalystII : ICANVCI, INotifyPropertyChanged
    {
        #region Structs

        //1.ZLGCAN系列接口卡信息的数据类型。
        public struct VCI_BOARD_INFO
        {
            public ushort hw_Version;
            public ushort fw_Version;
            public ushort dr_Version;
            public ushort in_Version;
            public ushort irq_Num;
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
            public uint AccCode;
            public uint AccMask;
            public uint Reserved;
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
            public ushort hw_Version;
            public ushort fw_Version;
            public ushort dr_Version;
            public ushort in_Version;
            public ushort irq_Num;
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
            public uint ExtFrame;	//是否为扩展帧
            public uint Start;
            public uint End;
        }

        /*------------数据结构描述完成---------------------------------*/

        public struct CHGDESIPANDPORT
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public byte[] szpwd;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] szdesip;
            public int desport;

            public void Init()
            {
                szpwd = new byte[10];
                szdesip = new byte[20];
            }
        }
        #endregion
        #region Extern
        [DllImport("controlcan.dll")]
        static extern uint VCI_OpenDevice(uint DeviceType, uint DeviceInd, uint Reserved);
        [DllImport("controlcan.dll")]
        static extern uint VCI_CloseDevice(uint DeviceType, uint DeviceInd);
        [DllImport("controlcan.dll")]
        static extern uint VCI_InitCAN(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_INIT_CONFIG pInitConfig);

        [DllImport("controlcan.dll")]
        static extern uint VCI_ReadBoardInfo(uint DeviceType, uint DeviceInd, ref VCI_BOARD_INFO pInfo);

        [DllImport("controlcan.dll")]
        static extern uint VCI_GetReceiveNum(uint DeviceType, uint DeviceInd, uint CANInd);
        [DllImport("controlcan.dll")]
        static extern uint VCI_ClearBuffer(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport("controlcan.dll")]
        static extern uint VCI_StartCAN(uint DeviceType, uint DeviceInd, uint CANInd);
        [DllImport("controlcan.dll")]
        static extern uint VCI_ResetCAN(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport("controlcan.dll")]
        static extern uint VCI_Transmit(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_CAN_OBJ pSend, uint Len);

        [DllImport("controlcan.dll")]
        static extern uint VCI_Receive(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_CAN_OBJ pReceive, uint Len, int WaitTime);

        [DllImport("controlcan.dll")]
        static extern uint VCI_SetReference(uint DeviceType, uint DeviceInd, uint CANInd, uint RefType, IntPtr pData);
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

        public CANalystII(uint DeviceIndex = 0, uint CanChannel = 0, uint DeviceType = 4)
        {
            this.DeviceType = DeviceType;
            this.DeviceIndex = DeviceIndex;
            this.CanChannel = CanChannel;
            Scheduler = new JobScheduler((VCILib.ICANVCI)this);
        }
        public bool ClearFilter()
        {
            filterNum = 0;
            var result = VCI_SetReference(21, DeviceIndex, CanChannel, 3, IntPtr.Zero);
            return result == 1;
        }

        public bool SetFilter(uint address, int filterId = -1)
        {
            var id = filterId >= 0 && filterId < filterData.Length ? filterId : filterNum;
            filterNum++;
            filterData[id] = new VCI_FILTER_RECORD()
            {
                End = address,
                Start = address,
                ExtFrame = 0
            };

            uint result = 0;
            result += VCI_SetReference(21, DeviceIndex, CanChannel, 1, Marshal.UnsafeAddrOfPinnedArrayElement(filterData, id));
            result += VCI_SetReference(21, DeviceIndex, CanChannel, 2, IntPtr.Zero);
            return result == 2;
        }
        public bool SetAndFilterSendRecvAddress(uint send, uint recv)
        {
            SendAddress = send;
            RecvAddress = recv;
            return ClearFilter() && SetFilter(SendAddress) && SetFilter(RecvAddress);
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
            Console.WriteLine(string.Empty);
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

        VCI_CAN_OBJ[] m_recobj = new VCI_CAN_OBJ[1000];
        public bool Reveice(out IEnumerable<object>? data)
        {
            uint res = VCI_Receive(DeviceType, DeviceIndex, CanChannel, ref m_recobj[0], 1000, 100);
            VCI_ClearBuffer(DeviceType, DeviceIndex, CanChannel);
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
        public IEnumerable<byte>? GetFrameData(object frame) => frame is VCI_CAN_OBJ obj ? obj.GetData() : default;
        public uint GetFrameAddress(object frame) => frame is VCI_CAN_OBJ obj ? obj.ID : default;
        public uint GetFrameTime(object frame) => frame is VCI_CAN_OBJ obj && obj.TimeFlag == 1 ? obj.TimeStamp : default;
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
    public enum VCIStatus
    {
        Closed = 0,
        Connected = 1,
        Initialized = 2,
        Ready = 3,
    }
    /// <summary>
    /// VCI操作库的接口
    /// </summary>
    public interface ICANVCI
    {
        /// <summary>
        /// 发送地址
        /// </summary>
        uint SendAddress { get; set; }
        /// <summary>
        /// 接收地址
        /// </summary>
        uint RecvAddress { get; set; }
        VCIStatus Status { get; set; }
        /// <summary>
        /// 启动VCI
        /// </summary>
        /// <returns>启动操作是否成功</returns>
        bool Start();
        /// <summary>
        /// 关闭VCI
        /// </summary>
        /// <returns>关闭操作是否成功</returns>
        bool Stop();
        /// <summary>
        /// 设置对应地址的过滤器
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="filterId">可选的参数，用于指定所设置的过滤器编号</param>
        /// <returns>是否成功设置过滤器</returns>
        bool SetFilter(uint address, int filterId = -1);
        /// <summary>
        /// 设置发送和接收地址，并设置对应地址的过滤器
        /// </summary>
        /// <param name="send">发送地址</param>
        /// <param name="recv">接收地址</param>
        /// <returns>VCI是否成功设置过滤器</returns>
        bool SetAndFilterSendRecvAddress(uint send, uint recv);
        /// <summary>
        /// 清除所有地址过滤器
        /// </summary>
        /// <returns>VCI是否成功清除过滤器</returns>
        bool ClearFilter();
        /// <summary>
        /// 尝试用VCI发送一帧数据，返回是否成功发送
        /// </summary>
        /// <param name="sendBytes">发送的数据</param>
        /// <returns>VCI是否成功发送</returns>
        bool SendOneFrame(byte[] sendBytes);
        /// <summary>
        /// 尝试从VCI接收数据，返回是否成功接收
        /// </summary>
        /// <param name="data">接收到的数据</param>
        /// <returns>是否成功从VCI接收数据</returns>
        bool Reveice(out IEnumerable<object>? data);
        IEnumerable<byte>? GetFrameData(object frame);
        uint GetFrameAddress(object frame);
        uint GetFrameTime(object frame);
        /// <summary>
        /// VCI初始化完成的事件
        /// </summary>
        event EventHandler CANInitialized;
        /// <summary>
        /// VCI关闭完成的事件
        /// </summary>
        event EventHandler CANClosed;
        /// <summary>
        /// 诊断任务调度器
        /// </summary>
        JobScheduler Scheduler { get; }
    }
}