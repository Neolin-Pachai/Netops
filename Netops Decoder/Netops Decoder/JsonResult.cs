using System;
using System.Collections.Generic;
using System.Text;

namespace Netops_Decoder
{
    public class JsonResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string SerialNo { get; set; }
        public string DeviceType { get; set; }
    }
}
