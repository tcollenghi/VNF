using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VNF.Business;
using System.Data;
using MetsoFramework.Utils;
using System.Text;
using System.IO;
using System.Xml;
using VNF.Portal.DataLayer;
using VNF.Portal.Models;

namespace VNF.Portal.Controllers
{
    public class CompradoresController : Controller
    {
        //
        // GET: /Compradores/

        public ActionResult Index()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("COMP", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|COMP";

            BLCompradores objBLCompradores = new BLCompradores();
            DataTable dt = objBLCompradores.GetAll();
            return View(dt);
        }

        public ActionResult Edit(string id = "")
        {
            DLComprador dal = new DLComprador();
            TbCOM c = dal.GetByIDComprador(id);
            if (c == null) c = new TbCOM();
            return View(c);
        }

        [HttpPost]
        public ActionResult Edit(TbCOM t)
        {
            DLComprador dal = new DLComprador();
            int Contador = dal.GetCount(t.CODCOM);
            if (Contador == 0)
            {
                dal.Insert(t);
            }
            else
            {
                dal.Update(t);
            }
            dal.Save();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public string ExportarNF()
        {
            BLCompradores objBLCompradores = new BLCompradores();
            DataTable dt = objBLCompradores.GetAll();

            string fileName = "Compradores - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
        } 
    }
}
