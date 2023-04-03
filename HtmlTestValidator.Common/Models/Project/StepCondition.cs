using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTestValidator.Models.Project
{
    //[JsonConverter(typeof(StepConverter))]

    public class StepCondition
    {
        [JsonProperty("element")]
        public WebElement Element { get; set; }
        [JsonProperty("sql")]
        public DBElement DBElement { get; set; }
        [JsonProperty("assert")]
        public AssertionWebElement Assertion { get; set; }
        [JsonProperty("assertdb")]
        public AssertionDBElement AssertionDB { get; set; }

        [JsonProperty("url")]
        public string URL { get; set; }

    }

  /*  public class StepConverter : JsonConverter
    {
        static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { 
            ContractResolver = new AssertionWebElementClassConverter() 
            
        };

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(AssertionWebElement));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo.ContainsKey("assertdb"))
                (existingValue as StepCondition).Assertion =  (AssertionDBElement)JsonConvert.DeserializeObject<AssertionDBElement>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo.ContainsKey("assert"))
                (existingValue as StepCondition).Assertion = (AssertionWebElement)JsonConvert.DeserializeObject<AssertionWebElement>(jo.ToString(), SpecifiedSubclassConversion);
            throw new NotImplementedException();
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }*/
}
