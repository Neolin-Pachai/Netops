using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Netops_Decoder
{
    public class ActiveTimerValue : Composer
    {
        [JsonProperty(PropertyName = "BitValue")]
        public string BitValue { get; set; }

        [JsonProperty(PropertyName = "Timer")]
        public string Timer { get; set; }

        [JsonProperty(PropertyName = "Value")]
        public string Value { get; set; }

        public ActiveTimerValue[] ActiveTimerValueArray { get; set; }

    }
}
