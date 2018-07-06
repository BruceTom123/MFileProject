using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MFileMVCProject.Models
{
    public class AdminData
    {
        [Required]
        public string BaseUrl { get; set; }
        [Required]
        public string MUsername { get; set; }
        [Required]
        public string MPassword { get; set; }
        [Required]
        public string MVaultId { get; set; }
        [Required]
        public int MProductTypeId { get; set; }
        [Required]
        public int MProductCodeId { get; set; }
        [Required]
        public int MProductExtenedNameId { get; set; }
        [Required]
        public int MProductPropertyId { get; set; }
        [Required]
        public int MSerialId { get; set; }
        [Required]
        public int MQCReportId { get; set; }
    }
}