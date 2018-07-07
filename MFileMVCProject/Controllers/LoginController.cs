using MFileMVCProject.Models;
using MFileMVCProject.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MFileMVCProject.Controllers
{
    public class LoginController : Controller
    {

        public List<User> users = new List<User>()
        {
            new User("vlad", "birthday"),
            new User("Harry", "harry"),
            new User("Nicolas", "nicolas"),
            new User("angel", "angel"),
            new User("bruce", "bruce"),
            new User("mfile", "mfile"),
        };

        // GET: Login
        User sampleuser = new User();
        public ActionResult Index()
        {
            if (!string.IsNullOrEmpty(Session["Username"] as string))
            {
                return RedirectToRoute("Products");
            }
            else
                return View(sampleuser);
        }

        [HttpPost]
        public ActionResult Index(User user)
        {
            
            if (ModelState.IsValid)
            {
                ViewBag.Message = "";
                foreach (User curuser in users)
                {
                    if (curuser.Username == user.Username && curuser.Password == user.Password)
                    {
                        Session["Username"] = user.Username;
                        Session["Password"] = user.Password;                    
                        return RedirectToRoute("Products");
                    }                        
                }
                ViewBag.Message = "Your username or password does not match!";
            }
            return View(user);
        }
    }
}