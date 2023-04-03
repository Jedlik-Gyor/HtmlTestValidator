using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HtmlTestValidator.Models.Project
{
    public class DBElement: Element
    {
        [JsonProperty("delimiter")]
        public string Delimiter { get; set; }

        [JsonProperty("database")]
        public string Database { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }
        public string FindElement(string file) {
            if (File.Exists(file.Replace("file:///", "")))
            {
                Regex r = new Regex(this.Delimiter, RegexOptions.IgnoreCase);
                List<string> sorok = File.ReadAllLines(file.Replace("file:///", "")).Where(x=>x!="").ToList();
                for (int i = sorok.Count() - 1; i >= 0; i--)
                {
                    if (r.IsMatch(sorok[i]))
                    {
                        if (i > 0 && !r.IsMatch(sorok[i-1]) && sorok[i - 1].Last() != ';') sorok[i - 1] += ';';
                        sorok.RemoveAt(i);
                    }
                }
                sorok = String.Join(' ', sorok).Split(';').ToList();
                return sorok[this.Index].Trim();
            }
            return "";
        }
    }
}
