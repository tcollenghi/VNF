using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VNF.Business;
using System.Data;
using MetsoFramework.Utils;
using MetsoFramework.Files;
using System.Text;
using System.IO;
using System.Xml;

namespace VNF.Portal.Controllers
{
    public class FornecedoresController : Controller
    {
        public ActionResult Index()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("FORN", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|FORN";

            return View();
        }

        [HttpGet]
        public PartialViewResult GetData(int pStart, int pLength, string pCNPJ, string pRazaoSocial, string pCodigoSAP)
        {
            pCNPJ = pCNPJ.Replace(".", "").Replace("-", "").Replace("/", "");
            BLFornecedores objBLFornecedores = new BLFornecedores();
            StringBuilder dadosRetorno = new StringBuilder();
            DataTable dt = new DataTable();
            dt = objBLFornecedores.GetByFilter(pCNPJ, pRazaoSocial, pCodigoSAP);

            return PartialView("Grid", dt);
        }

        [HttpGet]
        public string Exportar(string pCNPJ, string pRazaoSocial, string pCodigoSAP)
        {
            BLFornecedores objBLFornecedores = new BLFornecedores();
            DataTable dt = objBLFornecedores.GetByFilter(pCNPJ, pRazaoSocial, pCodigoSAP);

            string fileName = "Fornecedores - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
        }


        [HttpGet]
        public string RegimeEspecial(string ptxtRE, string pCNPJ, string pArrItens)
        {
            var arrItens = pArrItens;
            var itens = arrItens.Split(',');
            BLRegimeEspecial objRegimeEspecial = new BLRegimeEspecial();
            if (ptxtRE == "S")
            {
                objRegimeEspecial.RemoverFornecedor(pCNPJ);

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" }, 
                        { "mensagem", "Remoção de Regime Especial realizado com sucesso"}
                    });
            }
            else
            {
                objRegimeEspecial.CadastrarFornecedor(pCNPJ, itens);

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" }, 
                        { "mensagem", "Cadastro de Regime Especial realizado com sucesso"}
                    });
            }

            
        }
        
        [HttpGet]
        public string NotasFiscais(int pStart, int pLength, string pCNPJ)
        {
            pCNPJ = pCNPJ.Replace(".", "").Replace("-", "").Replace("/", "");
            
            BLFornecedores objBLFornecedores = new BLFornecedores();
            DataTable dt = objBLFornecedores.GetNotasFiscais(pCNPJ);

            

            StringBuilder dadosRetorno = new StringBuilder();
            int indexInicial = pStart * pLength;
            int indexFinal = indexInicial + pLength;
            int qtde = 0;
            for (int i = indexInicial; i < dt.Rows.Count; i++)
            {
                if (i >= indexFinal)
                    break;

                dadosRetorno.Append("<tr id='" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "' class='" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "'>");

                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_SERIE"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NFEID"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["NF_IDE_DHEMI"]).ToString("dd/MM/yyyy") + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["SITUACAO"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_XNOME"].ToString() + "</td>");


                dadosRetorno.Append("</tr>");
                qtde++;
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
            { "data", dadosRetorno.ToString()}, {"pagina" , qtde.ToString()}  });
        }


        [HttpGet]
        public string Edit(string id)
        {
            {
                BLFornecedores objBLFornecedores = new BLFornecedores();
                DataTable dt = objBLFornecedores.GetNotasFiscais(id);
                StringBuilder dadosRetorno = new StringBuilder();


                int qtde = 0;
                for (int i = 0; i < dt.Rows.Count; i++)
                {


                    dadosRetorno.Append("<tr id='" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "' class='" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "'>");

                    dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "</td>");
                    dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_SERIE"].ToString() + "</td>");
                    dadosRetorno.Append("<td>" + dt.Rows[i]["NFEID"].ToString() + "</td>");
                    dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["NF_IDE_DHEMI"]).ToString("dd/MM/yyyy") + "</td>");
                    dadosRetorno.Append("<td>" + dt.Rows[i]["SITUACAO"].ToString() + "</td>");
                    dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_XNOME"].ToString() + "</td>");


                    dadosRetorno.Append("</tr>");
                    qtde++;
                }
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
            { "data", dadosRetorno.ToString()}, {"pagina" , qtde.ToString()}  });
            }
        }

        [HttpGet]
        public ActionResult ViewDados(string id = "")
        {
            BLFornecedores objBLFornecedores = new BLFornecedores();
            modNF objmodNF = new modNF();

            DataTable dtForn = objBLFornecedores.GetByFilter(id, string.Empty, string.Empty);
            ViewBag.id = id;
            ViewBag.RAZFOR = dtForn.Rows[0]["razfor"].ToString();
            ViewBag.CNPJ = dtForn.Rows[0]["cnpj"].ToString().PadLeft(14, '0');
            ViewBag.CODFOR = dtForn.Rows[0]["codfor"].ToString();

            DataTable dt;
            dt = objBLFornecedores.GetNotasFiscais(id);
            return View(dt);
        }

        [HttpPost]
        public void UpdateEmail(string Id, string Email)
        {
            BLFornecedores objBLFornecedores = new BLFornecedores();
            objBLFornecedores.UpdateEmail(Id, Email);
        }
    }
}
