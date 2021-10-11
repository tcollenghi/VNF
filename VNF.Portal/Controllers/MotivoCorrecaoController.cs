using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VNF.Portal.DataLayer; 
using VNF.Portal.Models;

namespace VNF.Portal.Controllers
{
    public class MotivoCorrecaoController : Controller
    {
        //
        // GET: /MotivoCorrecao/

        DLMotivoCorrecao dal = new DLMotivoCorrecao();  
        public ActionResult Index()
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            var lista = dal.Get();
            return View(lista);
        }

        [HttpGet]
        public ActionResult Edit(int id = 0)
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            MotivoCorrecao m = dal.GetByID(id);
            if (m == null) m = new MotivoCorrecao();
            ViewBag.ddlTipoDocumento = getTipoDocumento(m.TipoDocumento);
            ViewBag.ddlClassificacao = getClassificacao(m.Classificacao);
            ViewBag.ddlResponsavel = getResponsavel(m.Responsavel);
            ViewBag.ddlIdGroup = getGrupos(Convert.ToInt32(m.IdGroup));
            ViewBag.ddlSelecionaResponsavel = getSelecaoResponsavel(m.InformaResponsavel == null ? false : Convert.ToBoolean(m.InformaResponsavel));
            return View(m);
        }

        [HttpGet]
        public string SelecionaResponsavel(int id)
        {
            MotivoCorrecao m = dal.GetByID(id);
            if (m == null)
            {
                return "N";
            }
            else
            {
                if(Convert.ToBoolean(m.InformaResponsavel))
                {
                    return "S";
                }
            }
            return "N";
        }

        [HttpPost]
        public ActionResult Edit(MotivoCorrecao m, string ddlSelecionaResponsavel)
        {
            if(ddlSelecionaResponsavel == "Sim")
            {
                m.InformaResponsavel = true;
            }
            else
            {
                m.InformaResponsavel = false;
            }

            if(m.IdMotivoCorrecao == 0)
            {
                dal.Insert(m);
            }
            else
            {
                dal.Update(m);
            }

            dal.Save();
            return RedirectToAction("index");
        }

        [HttpPost]
        public string Remover(int id)
        {
            try
            {
                dal.Delete(id);
                dal.Save();
                return "ok";
            }
            catch (Exception ex)
            {
                return "Não foi possível apagar o registro.";
            } 
        }

        #region Combos 
        private List<SelectListItem> getTipoDocumento(string Value = "")
        {
            List<SelectListItem> cl = new List<SelectListItem>();
            cl.Add(new SelectListItem() { Text = "NF-e", Value = "NF-e", Selected = (Value == "NF-e") });
            cl.Add(new SelectListItem() { Text = "NFS-e", Value = "NFS-e", Selected = (Value == "NFS-e") });
            cl.Add(new SelectListItem() { Text = "CT-e", Value = "CT-e", Selected = (Value == "CT-e") }); 
            return cl;
        }

        private List<SelectListItem> getClassificacao(string Value = "")
        {
            List<SelectListItem> cl = new List<SelectListItem>();
            var classificacao = new DLClassificacao().getClassificacao();
            foreach(var item in classificacao)
            {
                cl.Add(new SelectListItem() { Text = item.Descricao, Value = item.Descricao, Selected = (item.Descricao == Value) });
            } 
            return cl;
        }

        private List<SelectListItem> getResponsavel(string sValue = "")
        {
            List<SelectListItem> cl = new List<SelectListItem>();
            cl.Add(new SelectListItem() { Text = "Comprador", Value = "Comprador", Selected = (sValue == "Comprador") });
            cl.Add(new SelectListItem() { Text = "Grupo", Value = "Grupo", Selected = (sValue == "Grupo") });
            cl.Add(new SelectListItem() { Text = "Fornecedor", Value = "Fornecedor", Selected = (sValue == "Fornecedor") });
            cl.Add(new SelectListItem() { Text = "Manual", Value = "Manual", Selected = (sValue == "Manual") });
            return cl;
        }

        private List<SelectListItem> getGrupos(int Id = 0)
        {
            List<SelectListItem> cl = new List<SelectListItem>();
            var grupos = new DLGroups().Get();
            foreach (var item in grupos)
            {
                cl.Add(new SelectListItem() { Text = item.GroupName, Value = item.IdGroup.ToString(), Selected = (item.IdGroup == Id) });
            }
            return cl;
        }

        private List<SelectListItem> getSelecaoResponsavel(bool sValue)
        {
            List<SelectListItem> cl = new List<SelectListItem>();
            cl.Add(new SelectListItem() { Text = "Sim", Value = "Sim", Selected = (sValue == true) });
            cl.Add(new SelectListItem() { Text = "Não", Value = "Não", Selected = (sValue == false) }); 
            return cl;
        }
        #endregion

    }
}
