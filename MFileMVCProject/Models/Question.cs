using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MFileMVCProject.Models
{
    public class Question
    {
        public string Type { get; set; }
        public string Caption { get; set; }       
        public int Id { get; set; }
        public string Value { get; set; }
        public int Sequence { get; set; }
    }
}