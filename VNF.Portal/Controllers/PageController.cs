using System.Web.Mvc;

namespace VNF.Portal.Controllers
{
    public class PageController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            //DLPage objDLPage = new DLPage();
            return View();
        }

        //[HttpGet]
        //public ActionResult Edit(int id = 0)
        //{
        //    DLPage objDLPage = new DLPage();
        //    Pages objPage = new Pages();
        //    objPage = objDLPage.GetByID(id);
        //    if (objPage == null) { objPage = new Pages(); }

        //    DLIcons objDLIcons = new DLIcons();
        //    ViewBag.Icon = new SelectList(objDLIcons.GetAll(), "Description", "Description", objPage.Icon);
        //    return View(objPage);
        //}

        //[HttpPost]
        //public JsonResult Edit(Pages pPage)
        //{
        //    try
        //    {
        //        DLPage objDLPage = new DLPage();
        //        objDLPage.Save(pPage);
        //        return Json(null);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(ex.Message);
        //    }
        //}

        //[HttpPost]
        //public JsonResult Delete(int id)
        //{
        //    try
        //    {
        //        DLPage objDLPage = new DLPage();
        //        objDLPage.Delete(id);
        //        return Json(string.Empty);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(ex.Message);
        //    }
        //}
    }
}