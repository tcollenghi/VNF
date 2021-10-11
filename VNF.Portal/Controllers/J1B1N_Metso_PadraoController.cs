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
    public class J1B1N_Metso_PadraoController : Controller
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

            BLJ1B1N_Metso_Padrao objBLJ1B1N_Metso_Padrao = new BLJ1B1N_Metso_Padrao();
            DataTable dt = objBLJ1B1N_Metso_Padrao.GetAll();

            return dt;
        }

        public PartialViewResult GetData()
        {

            BLJ1B1N_Metso_Padrao objBLJ1B1N_Metso_Padrao = new BLJ1B1N_Metso_Padrao();
            DataTable dt = objBLJ1B1N_Metso_Padrao.GetAll();
            return PartialView("Grid", dt);
        }

        public string Salvar(string pCNPJ, string pPlanta, string pIdFornMetso)
        {
            BLJ1B1N_Metso_Padrao objBLJ1B1N_Metso_Padrao = new BLJ1B1N_Metso_Padrao();
            objBLJ1B1N_Metso_Padrao.Adicionar(pCNPJ, pPlanta, pIdFornMetso);


            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Editar(string pCNPJ, string pPlanta, string pIdFornMetso)
        {
            BLJ1B1N_Metso_Padrao objBLJ1B1N_Metso_Padrao = new BLJ1B1N_Metso_Padrao();
            objBLJ1B1N_Metso_Padrao.Editar(pCNPJ, pPlanta, pIdFornMetso);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Excluir(string pCNPJ)
        {
            BLJ1B1N_Metso_Padrao objBLJ1B1N_Metso_Padrao = new BLJ1B1N_Metso_Padrao();
            objBLJ1B1N_Metso_Padrao.Excluir(pCNPJ);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }
    }
}
