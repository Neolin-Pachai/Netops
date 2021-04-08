using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using Newtonsoft.Json;

namespace Netops_Decoder
{
    public class SensorBoardTypes : Composer
    {
        [JsonProperty(PropertyName = "Value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "Description")]
        public string Description { get; set; }

        public SensorBoardTypes[] SensorBoardTypesArray { get; set; }
    }
}
