using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTestValidator.Models.Project
{
    public class Steps
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("points")]
        public int Points { get; set; }
        [JsonProperty("conditions")]
        public StepCondition[] Conditions { get; set; } = new StepCondition[0];
        [JsonProperty("conditionsNumberHaveToPass")]
        public int ConditionsNumberHaveToPass { get; set; }
    }
}
