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
using System.Threading.Tasks;

namespace HtmlTestValidator.Models.Project
{
    public abstract class Assertion
    {
        public string ProjectName = string.Empty;
        public abstract bool Assert(object element, object result = null);
        public abstract bool Assert(ReadOnlyCollection<object> elements, object result = null);
    }
}
