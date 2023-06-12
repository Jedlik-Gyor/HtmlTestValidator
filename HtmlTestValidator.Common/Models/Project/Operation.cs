using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenQA.Selenium;

namespace HtmlTestValidator.Models.Project
{
    [JsonConverter(typeof(OperationConverter))]
    public abstract class Operation
    {
        public abstract void DoIt(string path);
    }

    public class CreateFileOperation : Operation
    {
        public string FileName { get; set; }
        public string Content { get; set; }

        public override void DoIt(string path)
        {
            File.WriteAllBytes(Path.Combine(path, FileName), Convert.FromBase64String(Content));
        }
    }

    public class OperationClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(Operation).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null;
            return base.ResolveContractConverter(objectType);
        }
    }

    public class OperationConverter : JsonConverter
    {
        static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new OperationClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Operation));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["type"].Value<string>() == "createFile")
                return JsonConvert.DeserializeObject<CreateFileOperation>(jo["options"].ToString(), SpecifiedSubclassConversion);

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