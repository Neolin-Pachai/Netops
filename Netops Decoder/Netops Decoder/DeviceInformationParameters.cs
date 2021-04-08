using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Netops_Decoder
{
    public class DeviceInformationParameters : ActiveTimerValue
    {
        [JsonProperty(PropertyName = "Description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "PayloadLength")]
        public int PayloadLength { get; set; }

        [JsonProperty(PropertyName = "Value")]
        public string Value { get; set; }

        public DeviceInformationParameters[] DeviceInformationParametersArray { get; set; }
    }
}
