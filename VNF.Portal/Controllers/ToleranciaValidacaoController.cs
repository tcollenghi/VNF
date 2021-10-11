using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VNF.Portal.DataLayer;
using VNF.Portal.Models;

namespace VNF.Portal.Controllers
{
    public class ToleranciaValidacaoController : Controller
    {
        //
        // GET: /ToleraciaValidacao/

        public ActionResult Index()
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            DLToleranciaValidacao dal = new DLToleranciaValidacao();
            var lista = dal.Get().OrderBy(x => x.ValorDe).ToList();
            return View(lista);
        }

        public ActionResult Edit(int Id = 0)
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            DLToleranciaValidacao dal = new DLToleranciaValidacao();
            ToleranciaValidacao t = dal.GetByID(Id);
            if (t == null) t = new ToleranciaValidacao();
            return View(t);
        } 

        [HttpPost]
        public ActionResult Edit(ToleranciaValidacao t)
        {
            DLToleranciaValidacao dal = new DLToleranciaValidacao();
            if(t.IdToleranciaValidacao == 0)
            {
                dal.Insert(t);
            }
            else
            {
                dal.Update(t);
            }
            dal.Save();
            return RedirectToAction("Index", "ToleranciaValidacao");
        }

        [HttpPost]
        public string Remover(int id)
        {
            try
            {
                DLToleranciaValidacao dal = new DLToleranciaValidacao();
                dal.Delete(id);
                dal.Save();
                return "ok";
            }
            catch (Exception ex)
            {
                return "Não foi possível apagar o registro.";
            }
        }
    }
}
