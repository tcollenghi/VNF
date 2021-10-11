using MetsoFramework.Utils;
using System.Collections.Generic;
using System.Web.Mvc;
using VNF.Portal.DataLayer;
using VNF.Portal.Models;

namespace VNF.Portal.Controllers
{
    public class CondicaoPagamentoController : Controller
    {
        DLCondicaoPagamento dal = new DLCondicaoPagamento();

        public ActionResult Index()
        {
             //Marcio Spinosa - 29/11/2018 - CR00009165
            //if ((new VNF.Business.BLAcessos()).ConsultaAcesso("CNPG", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|CNPG";
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("CDPG", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|CDPG";
             //Marcio Spinosa - 29/11/2018 - CR00009165 - Fim
            var lista = dal.Get();
            return View(lista);
        }

        public string SalvarConPag(string CONPAG, int NUMDIA)
        {
            if (!string.IsNullOrEmpty(CONPAG) && !string.IsNullOrEmpty(NUMDIA.ToString()))
            {
                TbCON conpag = new TbCON();
                conpag.CONPAG = CONPAG;
                conpag.NUMDIA = NUMDIA;
                dal.Insert(conpag);
                dal.Save();

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Condição de Pagamento realizada com sucesso"}
                    });
            }

            return null;         
        }

        public string EditarConPag(string CONPAG, int NUMDIA, int NUMDIAATUAL)
        {
            if (!string.IsNullOrEmpty(CONPAG) && !string.IsNullOrEmpty(NUMDIA.ToString()) && !string.IsNullOrEmpty(NUMDIAATUAL.ToString()))
            {
                TbCON conpag = new TbCON();
                conpag.CONPAG = CONPAG;
                conpag.NUMDIA = NUMDIA;
                dal.Update(conpag);
                dal.Save();

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Condição de Pagamento realizada com sucesso"}
                    });
            }

            return null;
        }

        public string ExcluirConPag(string CONPAG)
        {
            if (!string.IsNullOrEmpty(CONPAG) )
            {
                
                dal.DeleteConPag(CONPAG);
                dal.Save();

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Condição de Pagamento realizada com sucesso"}
                    });
            }

            return null;
        }
    }
}
