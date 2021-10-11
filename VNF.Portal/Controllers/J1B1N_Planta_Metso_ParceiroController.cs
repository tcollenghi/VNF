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
    public class J1B1N_Planta_Metso_ParceiroController : Controller
    {
        //
        // GET: /J1B1N_Planta_Metso_Parceiro/

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

            BLJ1B1N_Planta_Metso_Parceiro objBLJ1B1N_Planta_Metso_Parceiro = new BLJ1B1N_Planta_Metso_Parceiro();
            DataTable dt = objBLJ1B1N_Planta_Metso_Parceiro.GetAll();

            return dt;
        }

        public PartialViewResult GetData()
        {

            BLJ1B1N_Planta_Metso_Parceiro objBLJ1B1N_Planta_Metso_Parceiro = new BLJ1B1N_Planta_Metso_Parceiro();
            DataTable dt = objBLJ1B1N_Planta_Metso_Parceiro.GetAll();
            return PartialView("Grid", dt);
        }

        public string Salvar(string pCNPJ, string pPlanta)
        {
            BLJ1B1N_Planta_Metso_Parceiro objBLJ1B1N_Planta_Metso_Parceiro = new BLJ1B1N_Planta_Metso_Parceiro();
            objBLJ1B1N_Planta_Metso_Parceiro.Adicionar(pCNPJ, pPlanta);


            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Editar(string pCNPJ, string pPlanta)
        {
            BLJ1B1N_Planta_Metso_Parceiro objBLJ1B1N_Planta_Metso_Parceiro = new BLJ1B1N_Planta_Metso_Parceiro();
            objBLJ1B1N_Planta_Metso_Parceiro.Editar(pCNPJ, pPlanta);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Excluir(string pCNPJ)
        {
            BLJ1B1N_Planta_Metso_Parceiro objBLJ1B1N_Planta_Metso_Parceiro = new BLJ1B1N_Planta_Metso_Parceiro();
            objBLJ1B1N_Planta_Metso_Parceiro.Excluir(pCNPJ);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

    }
}
