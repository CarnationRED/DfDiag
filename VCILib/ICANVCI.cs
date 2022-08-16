using VCILib.JobManagment;
using VCILib.UDS;

namespace VCILib
{
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
        /// 清除接收缓存中的旧数据
        /// </summary>
        /// <returns></returns>
        bool ClearBuffer();
        /// <summary>
        /// 尝试从VCI接收数据，返回是否成功接收
        /// </summary>
        /// <param name="data">接收到的数据</param>
        /// <returns>是否成功从VCI接收数据</returns>
        bool Receive(out IEnumerable<object>? data);
        IEnumerable<object> StartReceive(int timeout);
        void StopReceive();
        /// <summary>
        /// 获取一帧数据的字节
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        IEnumerable<byte>? GetFrameData(object frame);
        /// <summary>
        /// 获取一帧数据的地址
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        uint GetFrameAddress(object frame);
        /// <summary>
        /// 获取一帧数据的时间
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
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
        /// <summary>
        /// UDS报文收发器
        /// </summary>
        UDSTransceiver Transceiver { get; }
    }
}