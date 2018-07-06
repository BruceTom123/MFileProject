using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MFileMVCProject.Models
{
    public class ValueOfTypedvalue
    {
        public string Value { get; set; }
        public object value { get; set; }
        public bool HasValue { get; set; }
        public string DisplayValue { get; set; }
        public string SerializedValue { get; set; }
        public int DataType { get; set; }
        public string SortingKey { get; set; }
        public bool HasAutomaticPermission { get; set; }
    }
}