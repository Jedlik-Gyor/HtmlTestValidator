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
    [JsonConverter(typeof(AssertActualConverter))]
    public abstract class AssertActual
    {
        public abstract string GetValue(IWebElement webElement);
    }

    public class AssertActualByAttribute : AssertActual
    {
        [JsonProperty("attribute")]
        public string Attribute { get; set; }

        public override string GetValue(IWebElement webElement)
        {
            return webElement.GetAttribute(Attribute);
        }
    }

    public class AssertActualText : AssertActual
    {
        public override string GetValue(IWebElement webElement)
        {
            return webElement.Text;
        }
    }

    public class AssertActualTagName : AssertActual
    {
        public override string GetValue(IWebElement webElement)
        {
            return webElement.TagName;
        }
    }

    public class AssertActualCssValue : AssertActual
    {
        [JsonProperty("cssValue")]
        public string CssValue { get; set; }

        public override string GetValue(IWebElement webElement)
        {
            return webElement.GetCssValue(CssValue);
        }
    }

    public class AssertActualClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(AssertActual).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null;
            return base.ResolveContractConverter(objectType);
        }
    }

    public class AssertActualConverter : JsonConverter
    {
        static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new AssertActualClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(AssertActual));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo.ContainsKey("attribute"))
                return JsonConvert.DeserializeObject<AssertActualByAttribute>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo.ContainsKey("text"))
                return JsonConvert.DeserializeObject<AssertActualText>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo.ContainsKey("tagName"))
                return JsonConvert.DeserializeObject<AssertActualTagName>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo.ContainsKey("cssValue"))
                return JsonConvert.DeserializeObject<AssertActualCssValue>(jo.ToString(), SpecifiedSubclassConversion);
            
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
