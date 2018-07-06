using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MFileMVCProject.Models
{
    public class Product
    {
        public string Title { get; set; }
        public int TypeId { get; set; }
        public int Id { get; set; }
        public string ProductCode { get; set; }
        public string ProductExtendedName { get; set; }
        public int DisplayNumber { get; set; }
    }
    
}