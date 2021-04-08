using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Netops_Decoder
{
    public class Composer
    {
        [JsonProperty(PropertyName = "ActiveTimerValue")]
        public ActiveTimerValue[] ActiveTimerValue { get; set; }

        [JsonProperty(PropertyName = "DeviceInformationParameters")]
        public DeviceInformationParameters[] DeviceInformationParameters { get; set; }

        [JsonProperty(PropertyName = "PeriodicTimerValue")]
        public PeriodicTimerValue[] PeriodicTimerValue { get; set; }

        [JsonProperty(PropertyName = "SensorBoardTypes")]
        public SensorBoardTypes[] SensorBoardTypes { get; set; }

        [JsonProperty(PropertyName = "SensorFunctions")]
        public SensorFunctions[] SensorFunctions { get; set; }
    }
}
