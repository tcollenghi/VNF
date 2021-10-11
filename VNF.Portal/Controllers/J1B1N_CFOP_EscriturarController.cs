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
    public class J1B1N_CFOP_EscriturarController : Controller
    {
        //
        // GET: /J1B1N_CFOP_Escriturar/

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

            BLJ1B1N_CFOP_Escriturar objBLJ1B1N_CFOP_Escriturar = new BLJ1B1N_CFOP_Escriturar();
            DataTable dt = objBLJ1B1N_CFOP_Escriturar.GetAll();

            return dt;
        }

        public PartialViewResult GetData()
        {

            BLJ1B1N_CFOP_Escriturar objBLJ1B1N_CFOP_Escriturar = new BLJ1B1N_CFOP_Escriturar();
            DataTable dt = objBLJ1B1N_CFOP_Escriturar.GetAll();
            return PartialView("Grid", dt);
        }

        public string Salvar(string pCFOP, string pCfopEscriturar)
        {
            BLJ1B1N_CFOP_Escriturar objBLJ1B1N_CFOP_Escriturar = new BLJ1B1N_CFOP_Escriturar();
            objBLJ1B1N_CFOP_Escriturar.Adicionar(pCFOP, pCfopEscriturar);


            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Editar(string pCFOP, string pCfopEscriturar)
        {
            BLJ1B1N_CFOP_Escriturar objBLJ1B1N_CFOP_Escriturar = new BLJ1B1N_CFOP_Escriturar();
            objBLJ1B1N_CFOP_Escriturar.Editar(pCFOP, pCfopEscriturar);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Excluir(string pCFOP)
        {
            BLJ1B1N_CFOP_Escriturar objBLJ1B1N_CFOP_Escriturar = new BLJ1B1N_CFOP_Escriturar();
            objBLJ1B1N_CFOP_Escriturar.Excluir(pCFOP);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }
    }
}
