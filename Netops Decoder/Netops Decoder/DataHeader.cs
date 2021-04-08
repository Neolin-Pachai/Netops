using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace Netops_Decoder
{
    public class DataHeader
    {
        public string McrTime { get; set; }

        public string SlotNumber { get; set; }

        public string DpmhError { get; set; }

        public string SensorFunction { get; set; }

        public string SensorFunctionStr { get; set; }

        public string SensorBoardType { get; set; }

        public string SensorBoardTypeStr { get; set; }
    }
}
