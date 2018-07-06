using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MFileMVCProject
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Products",
                url: "products/",
                defaults: new { controller = "Product", action = "Index" }
            );

            routes.MapRoute(
                name: "Setting",
                url: "setting/",
                defaults: new { controller = "Setting", action = "Index" }
            );

            routes.MapRoute(
                name: "setdata",
                url: "setting/setdata",
                defaults: new { controller = "Setting", action = "SetData" }
            );


            routes.MapRoute(
                name: "ImageUpload",
                url: "imageUpload/",
                defaults: new { controller = "Product", action = "ImageUpload" }
            );

            routes.MapRoute(
                name: "Login",
                url: "login/",
                defaults: new { controller = "Login", action = "Index" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
