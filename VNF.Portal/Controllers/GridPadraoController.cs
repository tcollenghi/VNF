using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MetsoFramework.Utils;
using System.Data;
using System.Text;

namespace MetsoAdmin.Controllers
{
    public class GridPadraoController : Controller
    {

        [HttpGet]
        public ActionResult Index()
        {
            return View(GetTable());
        }

        [HttpPost]
        public ActionResult Index(string doce, string cnpj)
        {
            return View("Index", GetTable());
        }

        public DataTable GetTable()
        {
            DataTable dttTeste = new DataTable();
            dttTeste.Columns.Add("ID");
            dttTeste.Columns.Add("Nome");
            dttTeste.Columns.Add("Data");
            dttTeste.Columns.Add("Numero");
            dttTeste.Columns.Add("Valor");
            dttTeste.Columns.Add("Codigo");
            dttTeste.Columns.Add("Descricao");

            DataRow dtrLinha;
            for (int i = 0; i < 50; i++)
            {
                dtrLinha = dttTeste.NewRow();
                dtrLinha["ID"] = i;
                dtrLinha["Nome"] = Uteis.GetRandomString(10, CharCasing.Upper);
                dtrLinha["Data"] = DateTime.Now.AddMonths(i);
                dtrLinha["Numero"] = i * 13;
                dtrLinha["Valor"] = i * 7.2;
                dtrLinha["Codigo"] = Uteis.GetRandomString(2, CharCasing.Upper);
                dtrLinha["Descricao"] = Uteis.GetRandomString(20, CharCasing.Any) + "...";

                dttTeste.Rows.Add(dtrLinha);
            }

            return dttTeste;
        }

        public List<modTeste> GetData()
        {
            List<modTeste> arrTeste = new List<modTeste>();

            modTeste objTeste;
            for (int i = 0; i < 200; i++)
            {
                objTeste = new modTeste();
                objTeste.ID = i;
                objTeste.Nome = Uteis.GetRandomString(10, CharCasing.Upper);
                objTeste.Data = DateTime.Now.AddMonths(i);
                objTeste.Numero = i * 13;
                objTeste.Valor = i * 7.2;
                objTeste.Codigo = Uteis.GetRandomString(2, CharCasing.Upper);
                objTeste.Descricao = Uteis.GetRandomString(20, CharCasing.Any) + "...";
                arrTeste.Add(objTeste);
            }

            StringBuilder stbTable = new StringBuilder();
            foreach (modTeste item in arrTeste)
            {
                stbTable.Append("<tr>");
                stbTable.Append("<td>" + item.ID + "</td>");
                stbTable.Append("<td>" + item.Nome + "</td>");
                stbTable.Append("<td>" + item.Numero.ToString("N2") + "</td>");
                stbTable.Append("<td>" + item.Descricao + "</td>");
                stbTable.Append("<td>" + item.Codigo + "</td>");
                stbTable.Append("<td>" + item.Valor.ToString("C2") + "</td>");
                stbTable.Append("<td>" + item.Data.ToShortDateString() + "</td>");
                stbTable.Append("</tr>");

            }

            return arrTeste;
        }

    }

    public class modTeste
    {
        public int ID { get; set; }
        public string Nome { get; set; }
        public DateTime Data { get; set; }
        public int Numero { get; set; }
        public double Valor { get; set; }
        public string Codigo { get; set; }
        public string Descricao { get; set; }
    }
}
