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
    public class J1B1N_CFOP_ParceiroController : Controller
    {
        //
        // GET: /J1B1N_CFOP_Parceiro/

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

            BLB1J1N_CFOP_Parceiro objBLB1J1N_CFOP_Parceiro = new BLB1J1N_CFOP_Parceiro();
            DataTable dt = objBLB1J1N_CFOP_Parceiro.GetAll();

            return dt;
        }

        public PartialViewResult GetData()
        {

            BLB1J1N_CFOP_Parceiro objBLB1J1N_CFOP_Parceiro = new BLB1J1N_CFOP_Parceiro();
            DataTable dt = objBLB1J1N_CFOP_Parceiro.GetAll();
            return PartialView("Grid", dt);
        }

        public string Salvar(string pCFOP, string pTipoParceiro)
        {
            BLB1J1N_CFOP_Parceiro objBLB1J1N_CFOP_Parceiro = new BLB1J1N_CFOP_Parceiro();
            objBLB1J1N_CFOP_Parceiro.Adicionar(pCFOP, pTipoParceiro);


            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Editar(string pCFOP, string pTipoParceiro)
        {
            BLB1J1N_CFOP_Parceiro objBLB1J1N_CFOP_Parceiro = new BLB1J1N_CFOP_Parceiro();
            objBLB1J1N_CFOP_Parceiro.Editar(pCFOP, pTipoParceiro);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Excluir(string pCFOP)
        {
            BLB1J1N_CFOP_Parceiro objBLB1J1N_CFOP_Parceiro = new BLB1J1N_CFOP_Parceiro();
            objBLB1J1N_CFOP_Parceiro.Excluir(pCFOP);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }
    }
}
