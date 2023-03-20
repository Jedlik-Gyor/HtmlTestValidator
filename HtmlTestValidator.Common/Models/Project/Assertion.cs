using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HtmlTestValidator.Models.Project
{
    [JsonConverter(typeof(AssertionConverter))]
    public abstract class Assertion
    {
        public abstract bool Assert(IWebElement webElement);
        public abstract bool Assert(ReadOnlyCollection<IWebElement> webElement);
    }

    public class AssertionEquals: Assertion
    {
        [JsonProperty("expected")]
        public string Expected { get; set; }
        [JsonProperty("actual")]
        public AssertActual Actual { get; set; }

        public override bool Assert(IWebElement webElement)
        {
            var value = Actual.GetValue(webElement).ToLower();
            return Expected.ToLower().CompareTo(value) == 0;
        }

        public override bool Assert(ReadOnlyCollection<IWebElement> webElement)
        {
            return false;
        }

    }

    public class AssertionCount : Assertion
    {
        [JsonProperty("expected")]
        public string Expected { get; set; }
        [JsonProperty("greaterThen")]
        public string GreaterThen { get; set; }

        public override bool Assert(IWebElement webElement)
        {
            return Expected.Trim() == "1";
        }

        public override bool Assert(ReadOnlyCollection<IWebElement> webElement)
        {
            if (!string.IsNullOrEmpty(Expected))
                return Expected.Trim() == webElement.Count().ToString();
            if (!string.IsNullOrEmpty(GreaterThen))
                try 
                {
                    return int.Parse(GreaterThen) < webElement.Count();
                }
                catch
                {
                    return false;
                }
            return false;
        }
    }

    public class AssertionEmpty : Assertion
    {
        [JsonProperty("actual")]
        public AssertActual Actual { get; set; }

        public override bool Assert(IWebElement webElement)
        {
            return string.IsNullOrEmpty(Actual.GetValue(webElement));
        }

        public override bool Assert(ReadOnlyCollection<IWebElement> webElements)
        {
            foreach (var webElement in webElements)
                if (!string.IsNullOrEmpty(Actual.GetValue(webElement)))
                    return false;
            return true;
        }
    }

    public class AssertionContains : Assertion
    {
        [JsonProperty("actual")]
        public AssertActual Actual { get; set; }

        [JsonProperty("values")]
        public string[] Values { get; set; }

        public override bool Assert(IWebElement webElement)
        {
            var actualValue = Actual.GetValue(webElement);
            foreach (var value in Values)
                if (!actualValue.Contains(value))
                    return false;
            return true;
        }

        public override bool Assert(ReadOnlyCollection<IWebElement> webElement)
        {
            return false;
        }
    }

    public class AssertionRegex : Assertion 
    {
        [JsonProperty("actual")]
        public AssertActual Actual { get; set; }

        [JsonProperty("pattern")]
        public string Pattern { get; set; }

        public override bool Assert(IWebElement webElement)
        {
            var actualValue = Actual.GetValue(webElement);
            return Regex.Match(actualValue, Pattern, RegexOptions.IgnoreCase).Success;
        }

        public override bool Assert(ReadOnlyCollection<IWebElement> webElement)
        {
            return false;
        }
    }

    public class AssertionHtmlValidation : Assertion
    {
        [JsonProperty("actual")]
        public AssertActual Actual { get; set; }

        public bool Assert(string path)
        {


            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://validator.w3.org/nu/?out=json");
                request.Content = new StringContent(System.IO.File.ReadAllText(path, Encoding.UTF8));
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");

                var task = client.SendAsync(request);
                task.Wait();
                var taskResponse = task.Result.Content.ReadAsStringAsync();
                return taskResponse.Result.Contains("\"messages\":[]");
            }
        }
        public override bool Assert(IWebElement webElement)
        {
            throw new NotImplementedException();
        }

        public override bool Assert(ReadOnlyCollection<IWebElement> webElement)
        {
            throw new NotImplementedException();
        }
    }

    public class AssertionCssValidation : Assertion
    {
        [JsonProperty("actual")]
        public AssertActual Actual { get; set; }

        public bool Assert(string path)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                
                var builder = new UriBuilder("https://jigsaw.w3.org/css-validator/validator");
                builder.Port = -1;
                var query = System.Web.HttpUtility.ParseQueryString(builder.Query);
                query["lang"] = "en";
                query["text"] = System.IO.File.ReadAllText(path, Encoding.UTF8);
                builder.Query = query.ToString();

                var request = new HttpRequestMessage(HttpMethod.Get, builder.ToString());

                var task = client.SendAsync(request);
                task.Wait();
                var taskResponse = task.Result.Content.ReadAsStringAsync();
                return taskResponse.Result.Contains("Congratulations! No Error Found.");
            }
        }
        public override bool Assert(IWebElement webElement)
        {
            throw new NotImplementedException();
        }

        public override bool Assert(ReadOnlyCollection<IWebElement> webElement)
        {
            throw new NotImplementedException();
        }


    }

    public class AssertionClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(Assertion).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null;
            return base.ResolveContractConverter(objectType);
        }
    }

    public class AssertionConverter : JsonConverter
    {
        static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new AssertionClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Assertion));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["operation"].Value<string>() == "equals")
                return JsonConvert.DeserializeObject<AssertionEquals>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo["operation"].Value<string>() == "count")
                return JsonConvert.DeserializeObject<AssertionCount>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo["operation"].Value<string>() == "contains")
                return JsonConvert.DeserializeObject<AssertionContains>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo["operation"].Value<string>() == "regex")
                return JsonConvert.DeserializeObject<AssertionRegex>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo["operation"].Value<string>() == "isempty")
                return JsonConvert.DeserializeObject<AssertionEmpty>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo["operation"].Value<string>() == "htmlvalidation")
                return JsonConvert.DeserializeObject<AssertionHtmlValidation>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo["operation"].Value<string>() == "cssvalidation")
                return JsonConvert.DeserializeObject<AssertionCssValidation>(jo.ToString(), SpecifiedSubclassConversion);

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
