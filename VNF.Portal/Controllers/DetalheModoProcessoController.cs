using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VNF.Portal.DataLayer;
using VNF.Portal.Models;

namespace VNF.Portal.Controllers
{
    public class DetalheModoProcessoController : Controller
    { 
        public ActionResult Index()
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            DLDetalheModoProcesso dal = new DLDetalheModoProcesso();
            var lista = dal.Get();
            return View(lista);
        }

        public ActionResult Edit(int id=0)
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            DLDetalheModoProcesso dal = new DLDetalheModoProcesso();
            var m = dal.GetByID(id);
            if (m == null) m = new Models.TbModoProcessoDetalhe();
            return View(m);
        }

        [HttpPost]
        public ActionResult Edit(TbModoProcessoDetalhe d)
        {
            DLDetalheModoProcesso dal = new DLDetalheModoProcesso();
            if(d.id_modo_processo_detalhe == 0)
            {
                dal.Insert(d);
            }
            else
            {
                dal.Update(d);
            }
            dal.Save();
            return RedirectToAction("Index");
        }
    }
}
