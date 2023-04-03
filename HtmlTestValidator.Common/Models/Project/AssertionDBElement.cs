using MySqlConnector;
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
    [JsonConverter(typeof(AssertionDBConverter))]
    public abstract class AssertionDBElement: Assertion
    {
        public override bool Assert(object element, object data = null)
        {
            return this.AssertDBElement((string)element, data);
        }
        public override bool Assert(ReadOnlyCollection<object> elements, object result = null)
        {
            throw new NotImplementedException();
        }
        public abstract bool AssertDBElement(string sql, object data = null);
    }

    public class AssertionDBEquals: AssertionDBElement
    {
        [JsonProperty("expected")]
        public string Expected { get; set; }
        [JsonProperty("cell")]
        public AssertDBActual Actual { get; set; }

        public override bool AssertDBElement(string sql, object data = null)
        {
            return Expected.ToLower().CompareTo(Actual.GetValue(sql, (MySqlConnection)data).ToLower()) == 0;
        }
    }
    public class AssertionDBRegex : AssertionDBElement
    {
        [JsonProperty("sqlCondition")]
        public string sqlCondition { get; set; }

        public override bool AssertDBElement(string sql, object data = null)
        {
            Regex r = new Regex(this.sqlCondition, RegexOptions.IgnoreCase);
            return r.IsMatch(sql);
        }
    }

    public class AssertionDBElementClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(AssertionDBElement).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null;
            return base.ResolveContractConverter(objectType);
        }
    }

    public class AssertionDBConverter : JsonConverter
    {
        static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new AssertionDBElementClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(AssertionDBElement));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["operation"].Value<string>() == "equals")
                return JsonConvert.DeserializeObject<AssertionDBEquals>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo["operation"].Value<string>() == "regexmatch")
                return JsonConvert.DeserializeObject<AssertionDBRegex>(jo.ToString(), SpecifiedSubclassConversion);
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
