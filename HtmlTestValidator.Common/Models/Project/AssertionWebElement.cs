using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenQA.Selenium;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HtmlTestValidator.Models.Project
{
    [JsonConverter(typeof(AssertionConverter))]
    public abstract class AssertionWebElement:Assertion
    {
        public override bool Assert(object element, object result = null) {
            if (element is ICollection)
            {
                return this.AssertWebElement(new ReadOnlyCollection<IWebElement>((element as IList).Cast<IWebElement>().ToList()), result);
            }
            else
            {
                return this.AssertWebElement((IWebElement)element, result);
            }
        }
        public override bool Assert(ReadOnlyCollection<object> element, object result = null)
        {
            return this.AssertWebElement(new ReadOnlyCollection<IWebElement>((element as IList).Cast<IWebElement>().ToList()), result);
        }

        public abstract bool AssertWebElement(IWebElement webElement, object result = null);
        public abstract bool AssertWebElement(ReadOnlyCollection<IWebElement> webElement, object result = null);
    }

    public class AssertionEquals: AssertionWebElement
    {
        [JsonProperty("expected")]
        public string Expected { get; set; }
        [JsonProperty("actual")]
        public AssertWebActual Actual { get; set; }

        public override bool AssertWebElement(IWebElement webElement, object result = null)
        {
            string value;
            if (result == null)
            { 
                value = Actual.GetValue(webElement).ToLower();
            } else {
                value = result.ToString().ToLower();
            }
            return Expected.Replace("$$ProjectName", this.ProjectName).ToLower().CompareTo(value) == 0;
        }

        public override bool AssertWebElement(ReadOnlyCollection<IWebElement> webElement, object result = null)
        {
            return false;
        }

    }

    public class AssertionCount : AssertionWebElement
    {
        [JsonProperty("expected")]
        public string Expected { get; set; }
        [JsonProperty("greaterThen")]
        public string GreaterThen { get; set; }

        public override bool AssertWebElement(IWebElement webElement, object result = null)
        {
            return Expected.Trim() == "1";
        }

        public override bool AssertWebElement(ReadOnlyCollection<IWebElement> webElement, object result = null)
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

    public class AssertionEmpty : AssertionWebElement
    {
        [JsonProperty("actual")]
        public AssertWebActual Actual { get; set; }

        public override bool AssertWebElement(IWebElement webElement, object result = null)
        {
            return string.IsNullOrEmpty(Actual.GetValue(webElement));
        }

        public override bool AssertWebElement(ReadOnlyCollection<IWebElement> webElements, object result = null)
        {
            foreach (var webElement in webElements)
                if (!string.IsNullOrEmpty(Actual.GetValue(webElement)))
                    return false;
            return true;
        }
    }

    public class AssertionContains : AssertionWebElement
    {
        [JsonProperty("actual")]
        public AssertWebActual Actual { get; set; }

        [JsonProperty("values")]
        public string[] Values { get; set; }

        public override bool AssertWebElement(IWebElement webElement, object result = null)
        {
            var actualValue = Actual.GetValue(webElement);
            if (Values != null)
                foreach (var value in Values)
                    if (!actualValue.Contains(value))
                        return false;
            return true;
        }

        public override bool AssertWebElement(ReadOnlyCollection<IWebElement> webElement, object result = null)
        {
            return false;
        }
    }

    public class AssertionHtmlValidation : AssertionWebElement
    {
        [JsonProperty("actual")]
        public AssertWebActual Actual { get; set; }

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
        public override bool AssertWebElement(IWebElement webElement, object result = null)
        {
            throw new NotImplementedException();
        }

        public override bool AssertWebElement(ReadOnlyCollection<IWebElement> webElement, object result = null)
        {
            throw new NotImplementedException();
        }
    }


    public class AssertionCssValidation : AssertionWebElement
    {
        [JsonProperty("actual")]
        public AssertWebActual Actual { get; set; }

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
        public override bool AssertWebElement(IWebElement webElement, object result = null)
        {
            throw new NotImplementedException();
        }

        public override bool AssertWebElement(ReadOnlyCollection<IWebElement> webElement, object result = null)
        {
            throw new NotImplementedException();
        }


    }
    public class AssertionWebElementClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(AssertionWebElement).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null;
            return base.ResolveContractConverter(objectType);
        }
    }

    public class AssertionConverter : JsonConverter
    {
        static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new AssertionWebElementClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(AssertionWebElement));
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
