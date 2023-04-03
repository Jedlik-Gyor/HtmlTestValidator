using HtmlTestValidator.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HtmlTestValidator.Models.Project
{
    [JsonConverter(typeof(AssertActualConverter))]
    public abstract class AssertWebActual
    {
        public abstract string GetValue(IWebElement webElement);
    }

    public class AssertActualByAttribute : AssertWebActual
    {
        [JsonProperty("attribute")]
        public string Attribute { get; set; }

        public override string GetValue(IWebElement webElement)
        {
            return webElement.GetAttribute(Attribute);
        }
    }

    public class AssertActualText : AssertWebActual
    {
        public override string GetValue(IWebElement webElement)
        {
            return webElement.Text;
        }
    }

    public class AssertActualTagName : AssertWebActual
    {
        public override string GetValue(IWebElement webElement)
        {
            return webElement.TagName;
        }
    }

    public class AssertActualCssValue : AssertWebActual
    {
        [JsonProperty("cssValue")]
        public string CssValue { get; set; }

        public override string GetValue(IWebElement webElement)
        {
            return webElement.GetCssValue(CssValue);
        }
    }

    public class AssertActualCssValuePseudo : AssertWebActual
    {
        //ezzel foglalkozni kell
        [JsonProperty("cssValueWithPseudo")]
        public string CssValue { get; set; }

        public override string GetValue(IWebElement webElement)
        {
            webElement.GetComputedCssStyles();
            return webElement.GetCssValue(CssValue);
        }
    }
    public class AssertActualJsValue : AssertWebActual
    {
        [JsonProperty("jsValue")]
        public string JsValue { get; set; }

        public override string GetValue(IWebElement webElement)
        {
            return "";
        }
    }

    public class AssertActualCssLinearGradient : AssertWebActual
    {
        [JsonProperty("degree")]
        public string Degree { get; set; }
        [JsonProperty("from")]
        public string From { get; set; }
        [JsonProperty("to")]
        public string To { get; set; }

        public override string GetValue(IWebElement webElement)
        {
            string background = webElement.GetCssValue("background");
            Regex color = new Regex("rgba\\((\\d{1,3}),\\s*(\\d{1,3}),\\s*(\\d{1,3}),\\s*(\\d{1,3})\\)");
            string r_from = color.Match(this.From).Groups[1].Value;
            string g_from = color.Match(this.From).Groups[2].Value;
            string b_from = color.Match(this.From).Groups[3].Value;
            string a_from = color.Match(this.From).Groups[4].Value;
            string r_to = color.Match(this.To).Groups[1].Value;
            string g_to = color.Match(this.To).Groups[2].Value;
            string b_to = color.Match(this.To).Groups[3].Value;
            string a_to = color.Match(this.To).Groups[4].Value;
            Regex regex = new Regex("linear\\-gradient\\(rgb\\("+r_from+ ", "+g_from+", "+b_from+"\\), rgba\\("+r_to+ ", "+b_to+", "+g_to+ ", "+a_to+"\\)\\)");
            return regex.IsMatch(background)?"1":"0";
        }
    }

    public class AssertActualCssTextShadow : AssertWebActual
    {
        [JsonProperty("x")]
        public string X { get; set; }
        [JsonProperty("y")]
        public string Y { get; set; }
        [JsonProperty("blur")]
        public string Blur { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        public override string GetValue(IWebElement webElement)
        {
            string textshadow = webElement.GetCssValue("text-shadow");
            Regex regex = new Regex("(rgb\\(\\d{1,3},\\s*\\d{1,3},\\s*\\d{1,3}\\))\\s*(\\d{1,3})px\\s*(\\d{1,3})px\\s*(\\d{1,3})px");
            bool regexresult = regex.IsMatch(textshadow);
            regexresult &= regex.Match(textshadow).Groups[1].Value == this.Color;
            regexresult &= regex.Match(textshadow).Groups[2].Value == this.X;
            regexresult &= regex.Match(textshadow).Groups[3].Value == this.Y;
            regexresult &= regex.Match(textshadow).Groups[4].Value == this.Blur;
            return regexresult ? "1" : "0";
        }
    }

    public class AssertActualClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(AssertWebActual).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null;
            return base.ResolveContractConverter(objectType);
        }
    }

    public class AssertActualConverter : JsonConverter
    {
        static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new AssertActualClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(AssertWebActual));
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
            if (jo.ContainsKey("jsValue"))
                return JsonConvert.DeserializeObject<AssertActualJsValue>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo.ContainsKey("cssValueWithPseudo"))
                return JsonConvert.DeserializeObject<AssertActualCssValuePseudo>(jo.ToString(), SpecifiedSubclassConversion); 
            if (jo.ContainsKey("degree"))
                return JsonConvert.DeserializeObject<AssertActualCssLinearGradient>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo.ContainsKey("text-shadow"))
                return JsonConvert.DeserializeObject<AssertActualCssTextShadow>(jo.ToString(), SpecifiedSubclassConversion);

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
