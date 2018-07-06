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
        // GET: Login
        User sampleuser = new User();
        ProductRepository productRepository = new ProductRepository();
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
                if(productRepository.CheckAuth(user.Username, user.Password))
                {
                    Session["Username"] = user.Username;
                    Session["Password"] = user.Password;                    
                    return RedirectToRoute("Products");
                }
                else
                {
                    ViewBag.Message = "Your username or password does not match!";
                }
            }
            return View(user);
        }
    }
}