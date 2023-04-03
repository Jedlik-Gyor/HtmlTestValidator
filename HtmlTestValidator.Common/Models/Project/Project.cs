using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTestValidator.Models.Project
{
    public class Project
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("teacher")]
        public string Teacher { get; set; }
        [JsonProperty("class")]
        public string Class { get; set; }
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("steps")]
        public Steps[] Steps { get; set; }
        
        [JsonProperty("npmStartDir")]
        public string NpmStartDir { get; set; }
        
        [JsonProperty("localStartDir")]
        public string LocalStartDir { get; set; }
        public static Project ReadFromFile(string path)
        {
            var content = File.ReadAllText(path);            
            return JsonConvert.DeserializeObject<Project>(content);
        }
    }
}
