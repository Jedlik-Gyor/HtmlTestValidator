using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTestValidator.Models.Project
{
    [JsonConverter(typeof(ElementConverter))]
    public abstract class WebElement:Element
    {
        public abstract IWebElement FindElement(WebDriver webDriver);
        public abstract ReadOnlyCollection<IWebElement> FindElements(WebDriver webDriver);
    }

    public class ElementByXPath: WebElement
    {
        [JsonProperty("byXPath")]
        public string XPath { get; set; }

        public override IWebElement FindElement(WebDriver webDriver)
        {
            return webDriver.FindElement(By.XPath(XPath));
        }

        public override ReadOnlyCollection<IWebElement> FindElements(WebDriver webDriver)
        {
            return webDriver.FindElements(By.XPath(XPath));
        }
    }

    public class ElementByCssSelector : WebElement
    {
        [JsonProperty("byCssSelector")]
        public string CssSelector { get; set; }

        public override IWebElement FindElement(WebDriver webDriver)
        {
            return webDriver.FindElement(By.CssSelector(CssSelector));
        }

        public override ReadOnlyCollection<IWebElement> FindElements(WebDriver webDriver)
        {
            return webDriver.FindElements(By.CssSelector(CssSelector));
        }

    }

    public class ElementClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(WebElement).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null;
            return base.ResolveContractConverter(objectType);
        }
    }

    public class ElementConverter : JsonConverter
    {
        static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new ElementClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(WebElement));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo.ContainsKey("byXPath"))
                return JsonConvert.DeserializeObject<ElementByXPath>(jo.ToString(), SpecifiedSubclassConversion);            
            if (jo.ContainsKey("byCssSelector"))
                return JsonConvert.DeserializeObject<ElementByCssSelector>(jo.ToString(), SpecifiedSubclassConversion);

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
