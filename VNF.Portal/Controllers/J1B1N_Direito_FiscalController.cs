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
    public class J1B1N_Direito_FiscalController : Controller
    {
        //
        // GET: /J1B1N_Direito_FIscal/


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

            BLJ1B1N_Direito_FIscal objBLJ1B1N_Direito_FIscal = new BLJ1B1N_Direito_FIscal();
            DataTable dt = objBLJ1B1N_Direito_FIscal.GetAll();

            return dt;
        }

        public PartialViewResult GetData()
        {

            BLJ1B1N_Direito_FIscal objBLJ1B1N_Direito_FIscal = new BLJ1B1N_Direito_FIscal();
            DataTable dt = objBLJ1B1N_Direito_FIscal.GetAll();
            return PartialView("Grid", dt);
        }

        public string Salvar(string pTipoImposto, string pCFOP, string pUF, string pDireito, string pValor)
        {
            BLJ1B1N_Direito_FIscal objBLJ1B1N_Direito_FIscal = new BLJ1B1N_Direito_FIscal();
            objBLJ1B1N_Direito_FIscal.Adicionar(pTipoImposto, pCFOP, pUF, pDireito, pValor);


            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

         //Marcio Spinosa - 21/02/2019 - CR00009165
        public string Editar(string pTipoImposto, string pCFOP, string pUF, string pDireito, string pValor, string pDireitoAntigo)
        {
            BLJ1B1N_Direito_FIscal objBLJ1B1N_Direito_FIscal = new BLJ1B1N_Direito_FIscal();
            objBLJ1B1N_Direito_FIscal.Editar(pTipoImposto, pCFOP, pUF, pDireito, pValor, pDireitoAntigo);
            //Marcio Spinosa - 21/02/2019 - CR00009165 - Fim
            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Excluir(string pTipoImposto, string pUF, string pCFOP)
        {
            BLJ1B1N_Direito_FIscal objBLJ1B1N_Planta_Metso_Parceiro = new BLJ1B1N_Direito_FIscal();
            objBLJ1B1N_Planta_Metso_Parceiro.Excluir(pTipoImposto, pUF, pCFOP);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

    }
}
