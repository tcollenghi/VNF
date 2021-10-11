/*
 * Autor: Marcio Spinosa - CR00008351
 * Data: 28/05/2018 
 * obs: ajuste para que manunteção de parâmetros.
 */
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
    public class ParametrosController : Controller
    {       

        public ActionResult Index(string pType = "") //Marcio Spinosa - 21/02/2019 - CR00009165
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("PARA", Uteis.LogonName()) == false && objAcesso.ConsultaAcesso("J1B1", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|PARA";

            GetDataParametro(pType);//Marcio Spinosa - 21/02/2019 - CR00009165
            return View();
            
        }

     

        public DataTable Atualizar(string pType) //Marcio Spinosa - 21/02/2019 - CR00009165
        {

            BLParametros objBLParametros = new BLParametros();
            DataTable dt = objBLParametros.GetAll(pType);//Marcio Spinosa - 21/02/2019 - CR00009165


            return dt;

        }

        public PartialViewResult GetDataParametro(string pType)//Marcio Spinosa - 21/02/2019 - CR00009165
        {

            BLParametros objBLParametros = new BLParametros();
            DataTable dt = objBLParametros.GetAll(pType);//Marcio Spinosa - 21/02/2019 - CR00009165
            return PartialView("Grid", dt);
        }

     

        public string SalvarParametro(string ptxtParametro, string ptxtValor, string ptxtDescricao)
        {
            BLParametros objBLParametros = new BLParametros();
            objBLParametros.Adicionar(ptxtParametro, ptxtValor, ptxtDescricao);
            

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Parâmetro realizada com sucesso"}
                    });

        }

        public string EditaParametro(string ptxtParametro, string ptxtValorAtual,string ptxtValorNovo, string ptxtDescricao)
        {
            BLParametros objBLParametros = new BLParametros();
            objBLParametros.Editar(ptxtParametro, ptxtValorAtual, ptxtValorNovo, ptxtDescricao);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Parâmetro realizada com sucesso"}
                    });

        }

        public string ExcluirParametro(string ptxtParametro, string ptxtValor)
        {
            BLParametros objBLParametros = new BLParametros();
            objBLParametros.Excluir(ptxtParametro, ptxtValor);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Parâmetro realizada com sucesso"}
                    });

        }


    }
}
