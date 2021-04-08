using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Netops_Decoder
{
    public class SensorFunctions : Composer
    {
        [JsonProperty(PropertyName = "Value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "Description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "PayloadLength")]
        public string PayloadLength { get; set; }

        //public SensorFunctions[] SensorFunctionsArray { get; set; }
    }
}
