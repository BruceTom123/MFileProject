
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MFileMVCProject.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            return View();
        }
        
        [HttpPost]
        public ActionResult GoLogin()
        {
            return RedirectToRoute("Login");
        }
        public ActionResult About()
        {
            ViewBag.Message = "ENTTEC COM.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}