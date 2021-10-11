using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using VNF.Portal.ViewsModel;

namespace VNF.Portal.Controllers
{
    public class ErrorController : Controller
    {
        [HttpGet]
        public ViewResult Index()
        {
            return View("Error");
        }

        [HttpGet]
        public ActionResult Errors(string message = "", string local = "")
        {
            ErrorsViewModel error = new ErrorsViewModel();
            error.Message = message;
            error.Local = local;
            return View(error);
        }

        [HttpGet]
        public ActionResult ServiceUnavailable()
        {
            Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            return View("ServiceUnavailable");
        }

        [HttpGet]
        public ActionResult ServerError(string message = "", string local = "")
        {
            Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            return View("ServerError");
        }

        [HttpGet]
        public ActionResult UnauthorizedAccess()
        {
            Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return View("UnauthorizedAccess");
        }

        [HttpGet]
        public ActionResult NotFound(string message = "", string local = "")
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return View();
        }
    }
}
