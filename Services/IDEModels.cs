using System;
using System.Collections.Generic;

namespace Report.Services
{
    public class IntelliSenseRequest
    {
        public string Code { get; set; }
        public int CaretPosition { get; set; }
        public string CurrentLine { get; set; }
        public List<string> ReferencedAssemblies { get; set; }

        public IntelliSenseRequest()
        {
            ReferencedAssemblies = new List<string>();
        }
    }

    public class IntelliSenseItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string ReturnType { get; set; }
        public string Description { get; set; }
        public List<ParameterInfo> Parameters { get; set; }

        public IntelliSenseItem()
        {
            Parameters = new List<ParameterInfo>();
        }
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsOptional { get; set; }
    }

    public class IntelliSenseResponse
    {
        public List<IntelliSenseItem> Suggestions { get; set; }
        public string CurrentWord { get; set; }
        public bool ShowParameterInfo { get; set; }
        public IntelliSenseItem MethodInfo { get; set; }

        public IntelliSenseResponse()
        {
            Suggestions = new List<IntelliSenseItem>();
        }
    }
}