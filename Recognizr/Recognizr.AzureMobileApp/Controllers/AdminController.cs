using Recognizr.AzureMobileApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Recognizr.AzureMobileApp.Controllers
{
    public class AdminController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(AdminViewModel m)
        {
            ViewBag.Message = "Assignment has been successfully created!";
            return View();
        }
    }
}