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
using Pomelo.EntityFrameworkCore.MySql;
using MySqlConnector;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using System.Data;

namespace HtmlTestValidator.Models.Project
{
    [JsonConverter(typeof(AssertActualDBConverter))]
    public abstract class AssertDBActual
    {
        public abstract string GetValue(string sql, MySqlConnection connection);
    }

    public class AssertActualByRowAndField : AssertDBActual
    {
        [JsonProperty("field")]
        public string Field { get; set; }
        [JsonProperty("row")]
        public int Row { get; set; }
        [JsonProperty("columnOrdinal")]
        public int? ColumnOrdinal { get; set; }

        public override string GetValue(string sql, MySqlConnection connection)
        {
            if (sql.Substring(0, 6).ToLower() != "select") return "";
            var result = "";
            try
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                using var reader = command.ExecuteReader();
                var columns = reader.GetColumnSchema();
                var columnid = this.ColumnOrdinal ?? columns.First(x => x.ColumnName == this.Field).ColumnOrdinal ?? -1;
                int index = 0;
                
                while (reader.Read())
                {
                    if (index == this.Row)
                    {
                        result = reader.GetValue(columnid).ToString();
                        break;
                    }
                    index++;
                }
                reader.DisposeAsync();
            } finally { 
                connection.Close();
            }
            return result.ToString(); 
        }
    }

    public class AssertActualByFieldAndValue : AssertDBActual
    {
        [JsonProperty("field")]
        public string Field { get; set; }
        
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("columnOrdinal")]
        public int ColumnOrdinal { get; set; }

        public override string GetValue(string sql, MySqlConnection connection)
        {
            if (sql.Substring(0, 6).ToLower() != "select") return "";
            var result = "";
            try
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                using var reader = command.ExecuteReader();
                var columns = reader.GetColumnSchema();
                var columnid = columns.First(x => x.ColumnName == this.Field).ColumnOrdinal ?? -1;
                int index = 0;

                while (reader.Read())
                {
                    if (reader.GetValue(columnid).ToString().ToLower() == this.Value.ToLower())
                    {
                        result = reader.GetValue(this.ColumnOrdinal).ToString();
                        break;
                    }
                    index++;
                }
                reader.DisposeAsync();
            }
            finally
            {
                connection.Close();
            }
            return result.ToString();
        }
    }

    public class AssertActualDBClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(AssertDBActual).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null;
            return base.ResolveContractConverter(objectType);
        }
    }

    public class AssertActualDBConverter : JsonConverter
    {
        static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new AssertActualDBClassConverter() };

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(AssertWebActual));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo.ContainsKey("row"))
                return JsonConvert.DeserializeObject<AssertActualByRowAndField>(jo.ToString(), SpecifiedSubclassConversion);
            if (jo.ContainsKey("value"))
                return JsonConvert.DeserializeObject<AssertActualByFieldAndValue>(jo.ToString(), SpecifiedSubclassConversion);
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
