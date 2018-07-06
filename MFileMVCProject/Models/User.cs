using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MFileMVCProject.Models
{
    public class User
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; }

        [Required]
        [MinLength(3)]
        public string Password { get; set; }

        public User(string username, string password)
        {
            Username = username;
            Password = password;
        }
        public User() { }
       
    }
}