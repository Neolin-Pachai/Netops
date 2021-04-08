using System;
using System.Collections.Generic;
using System.Text;

namespace Netops_Decoder
{
    public class DataBlockObject
    {
        public string Timestamp_Exists { get; set; }
        public string Slot_No { get; set; }
        public string Error_Bit { get; set; }
        public string Sensor_Function { get; set; }
        public string Sensor_Board_Type { get; set; }
        public string Sensor_Function_Value { get; set; }
        public string Sensor_Board_Type_Value { get; set; }
        public string Charging { get; set; }
        public string Key { get; set; }
        public DeviceInformationParameters DeviceInformationParameters { get; set; }
    }
}
