/*
 * Autor: Marcio Spinosa - CR00008351
 * Data: 28/05/2018 
 * obs: Ajuste para o VNF não consultar o AD para trazer dados do usuário e sim o banco de dados.
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
using VNF.Portal.DataLayer;//Marcio Spinosa - 23/05/2018 - CR00008351


namespace VNF.Portal.Controllers
{
    public class PriorizacaoController : Controller
    {


        public ActionResult Index()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("PRIO", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|PRIO";

            ViewBag.Itens = CarregarItens();
            return View();

        }

        //public string GetDataPriorizacao(string ptxtPedido, string pddlItensRecebidos, string pddlItensVencidos) 
        //{

        //    BLPriorizacao objBLPriorizacao = new BLPriorizacao();
        //    DataTable dt = objBLPriorizacao.GetByFilter(pddlItensRecebidos, pddlItensVencidos, ptxtPedido);


        //    StringBuilder dadosRetorno = new StringBuilder();


        //    int qtde = 0;
        //    for (int i = 0; i < dt.Rows.Count; i++)
        //    {

        //        dadosRetorno.Append("<tr id='" + dt.Rows[i]["pid_pedido"].ToString() + "' class='" + dt.Rows[i]["pid_pedido"].ToString() + "'>");

        //        dadosRetorno.Append("<td>" + dt.Rows[i]["pid_pedido"].ToString() + "</td>");
        //        dadosRetorno.Append("<td>" + dt.Rows[i]["pid_item"].ToString() + "</td>");
        //        dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["pid_data_insercao"]).ToString("dd/MM/yyyy") + "</td>");
        //        dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["pid_data_limite"]).ToString("dd/MM/yyyy") + "</td>");
        //        dadosRetorno.Append("<td>" + dt.Rows[i]["pid_recebido"].ToString() + "</td>");
        //        if (dt.Rows[i]["pid_recebido_em"] == DBNull.Value)
        //        {
        //            dadosRetorno.Append("<td> - </td>");
        //        }
        //        else
        //        {
        //            dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["pid_recebido_em"]).ToString("dd/MM/yyyy") + "</td>");
        //        }

        //        dadosRetorno.Append("</tr>");
        //        qtde++;
        //    }
        //    return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
        //    { "data", dadosRetorno.ToString()}, {"pagina" , qtde.ToString()}  });

        //}


        public PartialViewResult GetDataPriorizacao(string ptxtPedido, string pddlItensRecebidos, string pddlItensVencidos)
        {

            BLPriorizacao objBLPriorizacao = new BLPriorizacao();
            DataTable dt = objBLPriorizacao.GetByFilter(pddlItensRecebidos, pddlItensVencidos, ptxtPedido);
            return PartialView("Grid", dt);
        }


        public List<SelectListItem> CarregarItens()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "TODOS", Text = "TODOS", Selected = true });
            lista.Add(new SelectListItem { Value = "SIM", Text = "SIM" });
            lista.Add(new SelectListItem { Value = "NÃO", Text = "NÃO" });

            return lista;
        }

        [HttpGet]
        public string Exportar(string ptxtPedido, string pddlItensRecebidos, string pddlItensVencidos)
        {

            BLPriorizacao objBLPriorizacao = new BLPriorizacao();

            StringBuilder dadosRetornoBLPriorizacao = new StringBuilder();




            DataTable dt = objBLPriorizacao.GetByFilter(pddlItensRecebidos, pddlItensVencidos, ptxtPedido);

            string fileName = "Priorização - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
        }


        //Marcio Spinosa - 03/05/2018 - CR00008351
        //public string SalvarPrioridade(string ptxtNPedido, string ptxtItem, int ptxtDias)
        /// <summary>
        /// Salvar a prioridade da NF
        /// </summary>
        /// <param name="ptxtNPedido"></param>
        /// <param name="ptxtItem"></param>
        /// <param name="ptxtDias"></param>
        /// <param name="penviaEmail"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 28/05/2018 - CR00008351 - Ajuste para o VNF não consultar o AD para trazer dados do usuário</example>
        public string SalvarPrioridade(string ptxtNPedido, string ptxtItem, int ptxtDias, bool penviaEmail = false)
        {

            BLPriorizacao objBLPriorizacao = new BLPriorizacao();
            //Marcio Spinosa - 28/05/2018 - CR00008351
            DLUsers objDLUser = new DLUsers();
            //objBLPriorizacao.Adicionar(ptxtNPedido, ptxtItem, ptxtDias, Uteis.UserName());
            objBLPriorizacao.Adicionar(ptxtNPedido, ptxtItem, ptxtDias, objDLUser.getDadosByLogon(Uteis.LogonName())[1]);
            //Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
            //Marcio Spinosa - 03/05/2018 - CR00008351
            if (penviaEmail)
            {
                string txtBody = "Olá, " + Environment.NewLine + " A priorização do Pedido: " + ptxtNPedido + " para o item: "
                    + ptxtItem + " foi efetuada para a quantidade de dias: " + ptxtDias.ToString();
                //Marcio Spinosa - 23/05/2018 - CR00008351
                //Uteis.SendMail(Uteis.GetUserInfoBySamId(Uteis.LogonName())[1], Uteis.GetUserInfoBySamId(Uteis.LogonName())[1], "", "", "Priorização Pedido: " + ptxtNPedido, txtBody, null);
                DLUsers objDLUsers = new DLUsers();
                string[] arrInfoUser = objDLUsers.getDadosByLogon(Uteis.LogonName()); 
                Uteis.SendMail(arrInfoUser[2], arrInfoUser[1], "", "", "Priorização Pedido: " + ptxtNPedido, txtBody, null);
                //Marcio Spinosa - 23/05/2018 - CR00008351 - Fim
            }
            //Marcio Spinosa - 03/05/2018 - CR00008351 - Fim

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" }, 
                        { "mensagem", "Priorização realizada com sucesso"}
                    });

        }
        //Marcio Spinosa - 03/05/2018 - Fim

    }
}

