// See https://aka.ms/new-console-template for more information
using VCILib;
using Utils;
using VCILib.Jobs;
using VCILib.JobManagment;
using Job = VCILib.Jobs.Job;


//新建VCI对象
var vci = new CANalystII();
//启动VCI
vci.Start();
vci.Transceiver.timing = new UDSTimingParams() { P2CAN_Client = 100};
var data = " 22 f1 87".HexToByteArray();
var result = vci.Transceiver.UDSRequest(0X7C3, 0x7CB, data, out var res);
vci.Stop();
 

void SAS()
{
    //新建VCI对象
    var vci = new CANalystII();
    //启动VCI
    vci.Start();

    //设置ID
    vci.SendAddress = 0x6f0;
    //发送一帧数据，从字符串转换为8 bytes数据
    vci.SendOneFrame("50".HexTo8ByteArray());
    Console.WriteLine("SAS解除标定");
    Thread.Sleep(500);
    //发送一帧数据，从字符串转换为8 bytes数据
    vci.SendOneFrame("30".HexTo8ByteArray());
    Console.WriteLine("SAS标定完成");
    //关闭VCI
    vci.Stop();
}

void ClearSAS()
{
    //新建VCI对象
    var vci = new CANalystII();
    //启动VCI
    vci.Start();

    //设置ID
    vci.SendAddress = 0x6f0;
    //发送一帧数据，从字符串转换为8 bytes数据
    vci.SendOneFrame("50".HexTo8ByteArray());
    Console.WriteLine("SAS解除标定");
    //关闭VCI
    vci.Stop();
}
