using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VNF.Business;
using VNF.Portal.DataLayer;
using VNF.Portal.Models;

namespace VNF.Portal.Controllers
{
    public class ModoProcessoController : Controller
    {
        //
        // GET: /ModoProcesso/

        public ActionResult Index()
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            var lista = new DLModoProcesso().Get().OrderBy(x => x.mod_tipo_documento).ToList();
            return View(lista);
        }

        public ActionResult Edit(int Id = 0)
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            TbModoProcesso m = new DLModoProcesso().GetByID(Id);
            if (m == null) m = new TbModoProcesso();
            ViewBag.TipoDocumento = CarregarTipoDocumento(m.mod_tipo_documento);
            ViewBag.Moeda = CarregarMoeda(m.mod_moeda);
            ViewBag.ModoProcesso = CarregarModoProcesso(m.mod_id_modo_processo_detalhe.ToString());
            ViewBag.Plantas = CarregarPlantas(m.mod_planta);
            return View(m);
        }

        [HttpPost]
        public ActionResult Edit(TbModoProcesso m)
        {
            DLModoProcesso dal = new DLModoProcesso();
            ViewBag.TipoDocumento = CarregarTipoDocumento(m.mod_tipo_documento);
            ViewBag.Moeda = CarregarMoeda(m.mod_moeda);
            ViewBag.ModoProcesso = CarregarModoProcesso(m.id_modo_processo.ToString());
            ViewBag.Plantas = CarregarPlantas(m.mod_planta);

            if(m.id_modo_processo == 0)
            {
                dal.Insert(m);
            }
            else
            {
                dal.Update(m);
            }
            dal.Save();
            return RedirectToAction("Edit", new { Id = m.id_modo_processo });
        }

        [HttpPost]
        public string Remover(int id)
        {
            DLModoProcesso dal = new DLModoProcesso();
            return dal.Remove(id);
        }

        #region Codigo Contabilidade
        [HttpGet]
        public string CarregaCC(int Id)
        {
            string html = "";
            DLCodigoContabilidade dal = new DLCodigoContabilidade();
            var lista = dal.GetByProcessoId(Id).OrderBy(x => x.mcc_categoria_contabil).ToList(); ;
            foreach (var i in lista)
            {
                html += "<tr>";
                html += "<td>" + i.mcc_categoria_contabil + "</td>";
                html += "<td width=\"10%\">";
                html += "<button type=\"button\" class=\"btn btn-danger\" onclick=\"RemoverCC(" + i.id_modo_processo_categoria_contabil + ")\">";
                html += "Remover";
                html += "</button>";
                html += "</td>";
                html += "</tr>";
            }

            return html;
        } 

        [HttpPost]
        public void AdicionaCC(int Id, string CC)
        {
            DLCodigoContabilidade dal = new DLCodigoContabilidade();
            TbModoProcessoCategoriaContabil cc = new TbModoProcessoCategoriaContabil();
            cc.mcc_id_modo_processo = Id;
            cc.mcc_categoria_contabil = CC;
            dal.Insert(cc);
            dal.Save();
        }

        [HttpPost]
        public void RemoveCC(int Id)
        {
            DLCodigoContabilidade dal = new DLCodigoContabilidade();
            dal.Delete(Id);
            dal.Save();
        }
        #endregion

        #region CFOP
        [HttpGet]
        public string CarregaCFOP(int Id)
        {
            string html = "";
            DLCFOP dal = new DLCFOP();
            var lista = dal.GetByProcessoId(Id).OrderBy(x => x.mpc_cfop_codigo).ToList(); ;
            foreach(var i in lista)
            {
                html += "<tr>";
                html += "<td>" + i.mpc_cfop_codigo +"</td>";
                html += "<td width=\"10%\">";
                html += "<button type=\"button\" class=\"btn btn-danger\" onclick=\"RemoverCFOP(" + i.id_modo_processo_cfop + ")\">";
                html += "Remover";
                html += "</button>";
                html += "</td>";
                html += "</tr>";
            }
            
            return html;
        }

        [HttpGet]
        public string GetCFOPList()
        {
            DLCFOP dal = new DLCFOP();
            var lista = dal.GetList();
            string itens = "";
            foreach(var i in lista)
            {
                itens += "[" + i.cfop_codigo + "]";
            }
            return itens;
        }

        [HttpPost]
        public void AdicionaCFOP(int Id, string CFOP)
        {
            DLCFOP dal = new DLCFOP();
            TbModoProcessoCfop cfop = new TbModoProcessoCfop();
            cfop.mpc_id_modo_processo = Id;
            cfop.mpc_cfop_codigo = CFOP;
            dal.Insert(cfop);
            dal.Save();
        }

        [HttpPost]
        public void RemoveCFOP(int Id)
        {
            DLCFOP dal = new DLCFOP();
            dal.Delete(Id);
            dal.Save();
        }

        #endregion

        #region SelectLists
        [HttpGet]
        private List<SelectListItem> CarregarTipoDocumento(string Doc = "")
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            lista.Add(new SelectListItem { Value = "", Text = "" });
            lista.Add(new SelectListItem { Value = modNF.tipo_doc_nfe, Text = modNF.tipo_doc_nfe, Selected = (Doc == modNF.tipo_doc_nfe) });
            lista.Add(new SelectListItem { Value = modNF.tipo_doc_cte, Text = modNF.tipo_doc_cte, Selected = (Doc == modNF.tipo_doc_cte) });
            lista.Add(new SelectListItem { Value = modNF.tipo_doc_talonario, Text = modNF.tipo_doc_talonario, Selected = (Doc == modNF.tipo_doc_talonario) });
            lista.Add(new SelectListItem { Value = modNF.tipo_NFS, Text = modNF.tipo_NFS, Selected = (Doc == modNF.tipo_NFS) });
            lista.Add(new SelectListItem { Value = modNF.tipo_FAT, Text = modNF.tipo_FAT, Selected = (Doc == modNF.tipo_FAT) });
            lista.Add(new SelectListItem { Value = modNF.tipo_TLC, Text = modNF.tipo_TLC, Selected = (Doc == modNF.tipo_TLC) });
            return lista;
        }

        private List<SelectListItem> CarregarMoeda(string Moeda = "")
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            lista.Add(new SelectListItem { Value = "", Text = "" });
            lista.Add(new SelectListItem { Value = "BRL", Text = "BRL", Selected = (Moeda == "BRL") });
            return lista;
        }

        private List<SelectListItem> CarregarModoProcesso(string id = "")
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            lista.Add(new SelectListItem { Value = "", Text = "" });
            var Processos = new DLModoProcesso().GetModoProcessoDetalhe();
            foreach(var p in Processos)
            {
                lista.Add(new SelectListItem { Value = p.id_modo_processo_detalhe.ToString(), Text = p.mdp_processo, Selected = (id == p.id_modo_processo_detalhe.ToString()) });
            }
            
            return lista;
        }

        private List<SelectListItem> CarregarPlantas(string Planta = "")
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            lista.Add(new SelectListItem { Value = "", Text = "" });
            var Plantas = new DLPlanta().Get().OrderBy(x => x.planta).ToList();
            foreach(var i in Plantas)
            {
                lista.Add(new SelectListItem { Value = i.planta, Text = i.planta, Selected = (Planta == i.planta) });
            }
            
            return lista;
        }
        #endregion

    }
}
