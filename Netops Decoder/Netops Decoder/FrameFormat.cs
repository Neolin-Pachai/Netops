using System;
using System.Collections.Generic;
using System.Text;

namespace Netops_Decoder
{
    public class FrameFormat
    {
        public string ProtocolHeader { get; set; }
        public string SerialNumber { get; set; }
        public string DeviceType { get; set; }
        public DataBlocks DataBlocks { get; set; }
        public int CheckSum { get; set; }
    }
}
