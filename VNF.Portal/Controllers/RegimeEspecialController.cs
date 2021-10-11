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

namespace VNF.Portal.Controllers
{
    public class RegimeEspecialController : Controller
    {
        //
        // GET: /Cfre/

        public ActionResult Index()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("CFRE", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|CFRE";

            BLRegimeEspecial objBLRegimeEspecial = new BLRegimeEspecial();
            DataTable dt = objBLRegimeEspecial.GetAll();
            return View(dt);

        }

        [HttpGet]
        public string ExportarNF()
        {

            BLRegimeEspecial objBLRegimeEspecial = new BLRegimeEspecial();

            StringBuilder dadosRetornoRegimeEspecial = new StringBuilder();

            DataTable dt = objBLRegimeEspecial.GetAll();

            string fileName = "CFRE - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
        }

        [HttpGet]
        public string InserirNCM(string ptxtRE, string pCNPJ, string pArrItens)
        {
            var arrItens = pArrItens;
            var itens = arrItens.Split(',');
            BLRegimeEspecial objRegimeEspecial = new BLRegimeEspecial();

            objRegimeEspecial.CadastrarFornecedor(pCNPJ, itens);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Cadastro de Regime Especial realizado com sucesso"}
                    });
            //}


        }

    }
}
