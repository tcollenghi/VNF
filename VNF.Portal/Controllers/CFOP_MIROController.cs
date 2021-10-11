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
    public class CFOP_MIROController : Controller
    {
        //
        // GET: /J1B1N_Metso_Padrao/

        public ActionResult Index()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("J1B1", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|J1B1";

            GetData();
            return View();

        }

        public DataTable Atualizar()
        {

            BLCFOP_MIRO objBLCFOP_MIRO = new BLCFOP_MIRO();
            DataTable dt = objBLCFOP_MIRO.GetAll();

            return dt;
        }

        public PartialViewResult GetData()
        {

            BLCFOP_MIRO objBLCFOP_MIRO = new BLCFOP_MIRO();
            DataTable dt = objBLCFOP_MIRO.GetAll();
            return PartialView("Grid", dt);
        }

        public string Salvar(string pCFOP_XML, string pCFOP_SAP, string PCFOP_ESCRITURAR)
        {
            BLCFOP_MIRO objBLCFOP_MIRO = new BLCFOP_MIRO();
            objBLCFOP_MIRO.Adicionar(pCFOP_XML, pCFOP_SAP, PCFOP_ESCRITURAR);


            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Editar(string pCFOP_XML, string pCFOP_SAP, string PCFOP_ESCRITURAR)
        {
            BLCFOP_MIRO objBLCFOP_MIRO = new BLCFOP_MIRO();
            objBLCFOP_MIRO.Editar(pCFOP_XML, pCFOP_SAP, PCFOP_ESCRITURAR);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Excluir(string pCFOP_XML, string pCFOP_SAP, string PCFOP_ESCRITURAR)
        {
            BLCFOP_MIRO objBLCFOP_MIRO = new BLCFOP_MIRO();
            objBLCFOP_MIRO.Excluir(pCFOP_XML, pCFOP_SAP, PCFOP_ESCRITURAR);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }
    }
}