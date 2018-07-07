using MFileMVCProject.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MFileMVCProject.Controllers
{
    public class SettingController : Controller
    {
        public AdminData userinfo = new AdminData();
        public User user = new User();
        // GET: Setting
        public ActionResult Index()
        {
            return View(user);
        }

        [HttpPost]
        public ActionResult Index(User user)
        {
            if (user.Username == "Nicolas" && user.Password == "nicolas")
            {
                Session["mainuser"] = user.Username;
                setUserinfo();
                ViewBag.Message = "";
                return RedirectToRoute("setdata");
            }
            else
            {
                ViewBag.Message = "UserName or password is wrong";
            }
            return View(user);
        }

        public ActionResult SetData()
        {
            setUserinfo();
            return View(userinfo);           
        }

        [HttpPost]
        public ActionResult SetData(AdminData admindata)
        {
            string tempdata = JsonConvert.SerializeObject(admindata);
            string fileLoc = AppDomain.CurrentDomain.BaseDirectory + "Assets\\setting.bak";
            using (StreamWriter sw = new StreamWriter(fileLoc))
            {
                sw.Write(tempdata);
                sw.Close();
            }
            
            return View(admindata);
        }

        private void setUserinfo()
        {
            string fileLoc = AppDomain.CurrentDomain.BaseDirectory + "Assets\\setting.bak";
            try
            {
                StreamReader sr = new StreamReader(fileLoc);
                string settingData = sr.ReadToEnd();
                userinfo = JsonConvert.DeserializeObject<AdminData>(settingData);
                sr.Close();
            }
            catch(Exception ex)
            {
                userinfo = new AdminData();
            }            
        }
    }
}