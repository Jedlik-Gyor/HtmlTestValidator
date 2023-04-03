using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTestValidator.Models.Project
{
    [JsonConverter(typeof(ActionConverter))]
    public abstract class Action
    {
      
        public abstract object DoIt(WebDriver webDriver, IWebElement webElement);

        public class ActionMoveToElement: Action
        {
            public override object DoIt(WebDriver webDriver, IWebElement webElement)
            {
                var actions = new OpenQA.Selenium.Interactions.Actions(webDriver);                
                actions.MoveToElement(webElement).Perform();
                return null;
            }
        }

        public class ActionExecuteScript : Action
        {
            [JsonProperty("execute")]
            public string Code { get; set; }
          

            public override object DoIt(WebDriver webDriver, IWebElement webElement)
            {
                try
                {
                    return ((IJavaScriptExecutor)webDriver).ExecuteScript(this.Code);
                } catch
                {
                    return null;
                }
            }
        }

        public class ActionClassConverter : DefaultContractResolver
        {
            protected override JsonConverter ResolveContractConverter(Type objectType)
            {
                if (typeof(Action).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                    return null;
                return base.ResolveContractConverter(objectType);
            }
        }

        public class ActionConverter : JsonConverter
        {
            static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new ActionClassConverter() };

            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(Action));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                if (jo.ContainsKey("action") && jo["action"].Value<string>() == "moveToElement")
                    return JsonConvert.DeserializeObject<ActionMoveToElement>(jo.ToString(), SpecifiedSubclassConversion);
                if (jo.ContainsKey("execute"))
                    return JsonConvert.DeserializeObject<ActionExecuteScript>(jo.ToString(), SpecifiedSubclassConversion);

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
        }
    }
}
