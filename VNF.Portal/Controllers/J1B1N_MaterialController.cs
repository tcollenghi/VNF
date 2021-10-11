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
    public class J1B1N_MaterialController : Controller
    {
        //
        // GET: /J1B1N_Material/

        public ActionResult Index()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("J1B1", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|J1B1";

            GetDataJ1B1N_Material();
            return View();

        }

        public DataTable Atualizar()
        {

            BLJ1B1N_Material BLJ1B1N_Material = new BLJ1B1N_Material();
            DataTable dt = BLJ1B1N_Material.GetAll();

            return dt;
        }

        public PartialViewResult GetDataJ1B1N_Material()
        {

            BLJ1B1N_Material objBLJ1B1N_Material = new BLJ1B1N_Material();
            DataTable dt = objBLJ1B1N_Material.GetAll();
            return PartialView("Grid", dt);
        }

        public string SalvarJ1B1N_Material(string ptxtValor)
        {
            BLJ1B1N_Material objBLJ1B1N_Material = new BLJ1B1N_Material();
            objBLJ1B1N_Material.Adicionar(ptxtValor);


            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string EditaJ1B1N_Material(int pID, string pValor)
        {
            BLJ1B1N_Material objBLJ1B1N_Material = new BLJ1B1N_Material();
            objBLJ1B1N_Material.Editar(pID, pValor);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string ExcluirJ1B1N_Material(int pID)
        {
            BLJ1B1N_Material objBLJ1B1N_Material = new BLJ1B1N_Material();
            objBLJ1B1N_Material.Excluir(pID);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

    }
}
