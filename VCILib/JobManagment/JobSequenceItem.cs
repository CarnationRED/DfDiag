using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Utils;

namespace VCILib.JobManagment
{
    public class JobSequenceItem
    {
        public uint SendAddress { get; set; }
        public uint ResponseAddress { get; set; }
        /// <summary>
        /// 发送的数据（文字格式）
        /// </summary>
        public string Send
        {
            get => string.Empty;
            set => SendBytes = value.HexToByteArray();
        }
        /// <summary>
        /// 期待的响应数据（文字格式）
        /// </summary>
        public string Response
        {
            get => string.Empty;
            set => ResponseBytes = value.HexToByteArray();
        }
        private string sendData;

        /// <summary>
        /// 发送的数据（文字格式）。接在Send之后，可以填入动态参数
        /// </summary>
        public string SendData
        {
            get => sendData;
            set
            {
                if (value.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F')))
                    SendDataBytes = value.HexToByteArray();
                else if(value.StartsWith('{')&&value.EndsWith('}'))
                    sendData = value;
            }
        }
        /// <summary>
        /// 期待的响应数据（文字格式）。接在Response之后，可以填入动态参数
        /// </summary>
        public string ResponseCheckData
        {
            get => string.Empty;
            set => ResponseCheckBytes = value.HexToByteArray();
        }
        public string SecurityAccessMethod { get; set; }
        public string SecurityAccessParam { get; set; }
        public string ErrorMsg { get; set; }
        /// <summary>
        /// 发送的数据（程序可识别的格式）
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public byte[] SendBytes { get; set; }
        /// <summary>
        /// 期待的响应数据（程序可识别的格式）
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public byte[] ResponseBytes { get; set; }
        /// <summary>
        /// 发送的数据（程序可识别的格式）。接在Send之后，可以填入动态参数
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public byte[] SendDataBytes { get; set; }
        /// <summary>
        /// 期待的响应数据（程序可识别的格式）。接在Response之后，可以填入动态参数
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public byte[] ResponseCheckBytes { get; set; }

        public IEnumerable<byte> CalculateSendBytes()
        {
            if (SendBytes == null) return Enumerable.Empty<byte>();
            IEnumerable<byte> bytes = SendBytes;
            if (SendDataBytes != null)
                bytes = bytes.Concat(SendDataBytes);
            else if (!string.IsNullOrEmpty(SendData))
            {
                if (GlobalParameters.TryGetParamBytes(SendData, out var param))
                    bytes = bytes.Concat(param);
            }
            return bytes;
        }
        public IEnumerable<byte> CalculateResponseCheckBytes()
        {
            if(ResponseBytes==null) return Enumerable.Empty<byte>();
            IEnumerable<byte> bytes = ResponseBytes;
            if (ResponseCheckBytes != null)
                bytes = bytes.Concat(ResponseCheckBytes);
            else if (!string.IsNullOrEmpty(ResponseCheckData))
            {
                if (GlobalParameters.TryGetParamBytes(ResponseCheckData, out var param))
                    bytes = bytes.Concat(param);
            }
            return bytes;
        }
        public override string ToString()
        {
            return $"[{SendAddress:X2},{ResponseAddress:X2}]:{SendBytes.ByteArrToHexString()}";
        }
    }
}
