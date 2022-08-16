using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Utils;

namespace VCILib.Jobs
{
    public class JobWithResponse
    {
        public uint SendAddress { get; set; }
        public uint ResponseAddress { get; set; }
        public string Send
        {
            get => string.Empty;
            set => SendBytes = value.HexToByteArray();
        }
        public string SendData
        {
            get => string.Empty;
            set => SendDataBytes = value.HexToByteArray();
        }
        public string ResponseCheckData
        {
            get => string.Empty;
            set => ResponseCheckDataBytes = value.HexToByteArray();
        }
        public string SecurityAccessMethod { get; set; }
        public string SecurityAccessParam { get; set; }
        [XmlIgnore]
        [JsonIgnore]
        public byte[] SendBytes { get; set; }
        [XmlIgnore]
        [JsonIgnore]
        public byte[] SendDataBytes { get; set; }
        [XmlIgnore]
        [JsonIgnore]
        public byte[] ResponseCheckDataBytes { get; set; }
    }
}
