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
    public class AcessoController : Controller
    {
        //
        // GET: /Aces/

        public ActionResult Index()
        {

            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("ACES", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|ACES";

            if (Uteis.LogonName() == null)
                return RedirectToAction("Login", "User");

            DataTable dt = new DataTable();
            //BLAcessos objBLAcessos = new BLAcessos();
            //DataSet ds = objBLAcessos.Consultar("VNFE");
            //DataTable dtBloqueado = ds.Tables[0];

            return View(dt);
        }

        public PartialViewResult GetDataBloqueado(string ptxtCodigoPagina, string ptxtUser)
        {
            BLAcessos objBLAcessos = new BLAcessos();
            StringBuilder dadosRetornoBloqueado = new StringBuilder();
            //Marcio Spinosa - 24/07/2019 
            DataSet ds;
            DataTable dtBloqueado;

            //Marcio Spinosa - 13/08/2019  
            //if (string.IsNullOrEmpty(ptxtUser))
            //{
            //    ds = objBLAcessos.ConsultarBloqueados(ptxtCodigoPagina);
            //    dtBloqueado = ds.Tables[0];
            //}
            //else
            //{
            ds = objBLAcessos.ConsultarUsuarioBloqueados(ptxtUser, ptxtCodigoPagina);
            dtBloqueado = ds.Tables[0];

            //}
            //Marcio Spinosa - 13/08/2019 - Fim
            return PartialView("ViewDados", dtBloqueado);
        }

        public PartialViewResult GetDataLiberado(string ptxtCodigoPagina, string ptxtUser)
        {
            BLAcessos objBLAcessos = new BLAcessos();
            StringBuilder dadosRetornoBloqueado = new StringBuilder();
            dadosRetornoBloqueado.Clear();
            //Marcio Spinosa - 24/07/2019 
            DataSet ds;
            DataTable dtLiberado;
            //if (string.IsNullOrEmpty(ptxtUser))
            //{
            //    ds = objBLAcessos.ConsultarLiberados(ptxtCodigoPagina);
            //    dtLiberado = ds.Tables[0];
            //}
            //else
            //{
            ds = objBLAcessos.ConsultarUsuariosLiberados(ptxtUser, ptxtCodigoPagina);
            dtLiberado = ds.Tables[0];
            //}
            //Marcio Spinosa - 24/07/2019  - Fim
            return PartialView("ViewDados", dtLiberado);
        }

        public string BloqueiaUsuario(string txtCodigoPagina, string usuario)
        {
            BLAcessos objBLAcessos = new BLAcessos();
            objBLAcessos.BloquearAcesso(txtCodigoPagina, usuario);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" } });
        }

        public string LiberaUsuario(string txtCodigoPagina, string usuario)
        {
            BLAcessos objBLAcessos = new BLAcessos();
            objBLAcessos.LiberarAcesso(txtCodigoPagina, usuario);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" } });
        }


    }
}
