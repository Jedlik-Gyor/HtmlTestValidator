using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTestValidator.Models.Project
{
    public class StepCondition
    {
        [JsonProperty("element")]
        public Element Element { get; set; }
        [JsonProperty("assert")]
        public Assertion Assertion { get; set; }
        [JsonProperty("url")]
        public string URL { get; set; }

    }
}
