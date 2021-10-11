using MetsoFramework.Utils;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using VNF.Business;
using VNF.Portal.Models;

namespace VNF.Portal.Controllers
{
    public class PortariaController : Controller
    {

        [HttpGet]
        public ActionResult Neles()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("PTNL", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|PTNL";

            ViewBag.TipoPortaria = "PTNL";
            PortariaModel portariaModel = new PortariaModel();
            portariaModel.dtEquipamentos = GetNeles();
            return View("Neles", portariaModel);
        }

        [HttpPost]
        public ActionResult Neles(string txtNumeroPasta)
        {
            ViewBag.TipoPortaria = "PTNL";
            PortariaModel portariaModel = new PortariaModel();
            portariaModel.dtEquipamentos = GetNeles(txtNumeroPasta);
            return View("Neles", portariaModel);
        }

        [HttpGet]
        public ActionResult Pasta(string pUnidade, string pPasta)
        {
            if (string.IsNullOrEmpty(pPasta))
            {
                return PartialView();
            }
            else
            {
                PortariaModel portaria = new PortariaModel();
                PastaModel pasta = new PastaModel();
                NotaModel nota = null;
                List<NotaModel> lstNota = null;

                DataTable dtt = new DataTable();
                BLPortaria objBLPortaria = new BLPortaria();
                dtt = objBLPortaria.GetByFilter(pUnidade, pPasta);

                if (dtt != null && dtt.Rows.Count > 0)
                {
                    pasta.IDPASTA = dtt.Rows[0]["IDPASTA"].ToString();
                    pasta.NOMMOT = dtt.Rows[0]["NOMMOT"].ToString();
                    pasta.DATLAN = dtt.Rows[0]["DATLAN"].ToString();
                    pasta.HORCHE = dtt.Rows[0]["HORCHE"].ToString();
                    pasta.HORENT = dtt.Rows[0]["HORENT"].ToString();
                    pasta.NOMTRA = dtt.Rows[0]["NOMTRA"].ToString();
                    pasta.PLACA = dtt.Rows[0]["PLACA"].ToString();
                    pasta.SETOR = dtt.Rows[0]["SETOR"].ToString();
                    pasta.QTD_NOTAS = dtt.Rows[0]["QTD_NOTAS"].ToString();

                    dtt = new DataTable();
                    dtt = objBLPortaria.GetNotasFiscais("", pUnidade, pasta.IDPASTA);

                    if (dtt != null && dtt.Rows.Count > 0)
                    {
                        lstNota = new List<NotaModel>();
                        foreach (DataRow dr in dtt.Rows)
                        {
                            nota = new NotaModel();
                            nota.ChaveAcesso = dr["NFEID"].ToString();
                            nota.DOCe = dr["NF_IDE_NNF"].ToString();
                            nota.Fornecedor = dr["NF_EMIT_XNOME"].ToString();
                            nota.PrioridadeAlta = dr["PRIORIDADE_ALTA"].ToString();
                            nota.Status = dr["SITUACAO"].ToString();
                            lstNota.Add(nota);
                        }
                        pasta.NOTAS = lstNota;
                    }
                }
                portaria.pasta = pasta;
                return PartialView(portaria);
            }
        }

        [HttpPost]
        public string RegistrarChegada(string pUnidade, string pPasta, string pMotorista, string pPlaca, string pSetor, string pTransportadora, string pDataChega, string pNotas)
        {
            string retorno = string.Empty;
            string[] listaNotas = pNotas.Split(';');
            BLPortaria objBLPortaria = new BLPortaria();
            retorno = objBLPortaria.RegistrarChegada(pUnidade, pPasta, pMotorista, pPlaca, pSetor, pTransportadora, pDataChega, listaNotas);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, { "data", retorno.ToString() } });
        }

        [HttpPost]
        public string RegistrarEntrada(string pUnidade, string pPasta)
        {
            BLPortaria objBLPortaria = new BLPortaria();
            objBLPortaria.RegistrarEntrada(pUnidade, pPasta);

            //return RedirectToAction("Equipamentos",GetEquipamentos());

            //return RedirectToAction("Equipamentos");
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, { "data", "" } });
        }

        [HttpPost]
        public string RegistrarSaida(string pUnidade, string pPasta)
        {
            BLPortaria objBLPortaria = new BLPortaria();
            objBLPortaria.RegistrarSaida(pUnidade, pPasta);

            //return RedirectToAction("Equipamentos",GetEquipamentos());

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, { "data", "" } });
        }

        [HttpPost]//[HttpGet]
        public string VerificaSeExcluilDanfe(string pDanfe, string pNotas)
        {


            string[] arrNotas = pNotas.Split(';');

            for (int i = 0; i < arrNotas.Length; i++)
            {
                if (arrNotas[i].ToString() == pDanfe)
                {
                    return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                    { "result", "Ok" },
                    { "data", pDanfe.ToString()}
                    });
                }
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                    { "result", "Ok" },
                    { "data", ""}
                    });


        }

        [HttpGet]
        public string ConsultarDanfe(string pDanfe)
        {
            DataTable dt = new DataTable();
            StringBuilder dadosRetorno = new StringBuilder();
            dadosRetorno.Clear();
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();

            BLPortaria objBLPortaria = new BLPortaria();

            //Verifica se a nota pode ser recebida consultando o método 'PodeModificar' que identifica se o doucmento não
            //está cancela, recusado ou mesmo se já foi inegrado no SAP
            if (objBLNotaFiscal.PodeModificar(pDanfe))
            {

                dt = objBLPortaria.GetNotasFiscais(pDanfe);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    dadosRetorno.Append("<tr id='" + dt.Rows[i]["NFEID"].ToString() + "' class='" + dt.Rows[i]["NFEID"].ToString() + "'>");
                    if (dt.Rows[i]["PRIORIDADE_ALTA"].ToString() == "SIM")
                        dadosRetorno.Append("<td class='center'><h5 class='padding0 margin0 line-height0' title='Prioridade alta'><i class='fa fa-arrow-circle-up txt-color-red'></i></h5></td>");
                    else
                        dadosRetorno.Append("<td class='center'><h5 class='padding0 margin0 line-height0' title='Prioridade baixa'><i class='fa fa-arrow-circle-down txt-color-green'></i></h5></td>");

                    dadosRetorno.Append("<td class='chave'>" + dt.Rows[i]["NFEID"].ToString() + "</td>");
                    dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "</td>");
                    dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_XNOME"].ToString() + "</td>");
                    dadosRetorno.Append("<td>" + dt.Rows[i]["SITUACAO"].ToString() + "</td>");
                    dadosRetorno.Append("<td><h5 class='padding0 margin0 line-height0' title=\"remover documento\"><i class=\"fa fa-times txt-color-red cursor-pointer\" onclick=\"SelecionaNFe('" + dt.Rows[i]["NFEID"].ToString() + "'); \"></i></h5></td>");
                    dadosRetorno.Append("</tr>");
                }
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, { "data", dadosRetorno.ToString() } });
            }
            else
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, { "data", "false" } });
            }

        }

        [HttpGet]
        public string ConsultarDanfeNFe(string pDanfe)
        {
            string html = "";
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            DataTable dt = new DataTable();


            dt = objBLNotaFiscal.GetByFilter(string.Empty, string.Empty, modNF.TipoData.Emissao, pDanfe, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string strFornecedor = dt.Rows[i]["NF_EMIT_XNOME"].ToString();
                string strDataValidacao = String.IsNullOrEmpty(dt.Rows[i]["DATVAL"].ToString()) ? "" : Convert.ToDateTime(dt.Rows[i]["DATVAL"].ToString()).ToShortDateString();
                string strRelevante = dt.Rows[i]["NFEREL"].ToString() == "S" ? "SIM" : "NÃO";

                html += "<tr>";
                if (!string.IsNullOrEmpty(dt.Rows[i]["NF_IDE_DHEMI"].ToString()))
                    html += "<td>" + Convert.ToDateTime(dt.Rows[i]["NF_IDE_DHEMI"].ToString()).ToShortDateString() + "</td>";
                else
                    html += "<td></td>";
                html += "<td>" + strFornecedor + "</td>";
                html += "<td>" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "</td>";
                html += "<td>" + dt.Rows[i]["NF_IDE_SERIE"].ToString() + "</td>";
                html += "<td>" + dt.Rows[i]["SITUACAO"].ToString() + "</td>";
                html += "<td>" + strDataValidacao + "</td>";
                html += "<td>" + strRelevante + "</td>";
                html += "<td><h5 class='padding0 margin0 line-height0' title=\"selecionar documento\"><i class=\"fa fa-check-circle txt-color-green cursor-pointer\" onclick=\"SelecionaNFe('" + dt.Rows[i]["NFEID"].ToString() + "'); \"></i></h5></td>";
                html += "</tr>";
            }

            return html;
        }

        [HttpGet]
        public PartialViewResult LoadDataNeles(string pNumeroPasta, string r)
        {
            BLPortaria objBLPortaria = new BLPortaria();
            StringBuilder dadosRetorno = new StringBuilder();
            DataTable dt = new DataTable();
            if (string.IsNullOrEmpty(pNumeroPasta)) //Marcio Spinosa - 12/08/2019 
                dt = GetNeles();
            else
                dt = objBLPortaria.GetByFilter("PTNL", pNumeroPasta);

            PortariaModel portariaModel = new PortariaModel();
            portariaModel.dtEquipamentos = dt;

            return PartialView("Grid", portariaModel);
        }

        [HttpGet]
        public ActionResult NelesEdit(string id = "")
        {
            //BLPortaria objBLPortaria = new BLPortaria();
            DataTable dt = null; //= objBLPortaria.GetByFilter(id);

            //ViewBag.id = id;

            return View(dt);
        }

        [HttpPost]
        public string ExportarNeles(string pNumeroPasta, string pstrPortaria)
        {
            DataTable dt;
            //if (pstrPortaria == "PTNL")
                dt = GetNeles(pNumeroPasta);

            string fileName = "PORT" + pstrPortaria + " - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
        }

        [HttpGet]
        public ActionResult RelatorioDivergencias(string pNotas)
        {
            BLPortaria portaria = new BLPortaria();
            string[] listaNotas = string.IsNullOrEmpty(pNotas) ? null : pNotas.Split(';');
            DataTable dt = portaria.GetRelatorioDivergencias(listaNotas);
            ReportViewer objViewer = new ReportViewer();
            objViewer.ProcessingMode = ProcessingMode.Local;

            //Caminho onde o arquivo do Report Viewer está localizado
            objViewer.LocalReport.ReportPath = Server.MapPath("~/Reports/rptPortaria.rdlc");

            //Define o nome do nosso DataSource e qual rotina irá preenche-lo, no caso, nosso método criado anteriormente
            //relatorio.DataSources.Add(new ReportDataSource("DTB_VNFDataSet", dt));
            objViewer.LocalReport.DataSources.Add(new ReportDataSource("vwRPT_PORTARIA", dt));

            Warning[] warnings;
            string[] streams;
            byte[] bytes;
            string reportType = "PDF";
            string mimeType;
            string encoding;
            string fileNameExtension;
            string deviceInfo = "<DeviceInfo>" +
                                " <OutputFormat>PDF</OutputFormat>" +
                                " <PageWidth></PageWidth>" +
                                " <PageHeight></PageHeight>" +
                                " <MarginTop></MarginTop>" +
                                " <MarginLeft></MarginLeft>" +
                                " <MarginRight></MarginRight>" +
                                " <MarginBottom></MarginBottom>" +
                                "</DeviceInfo>";


            bytes = objViewer.LocalReport.Render("PDF", null, out mimeType, out encoding, out fileNameExtension, out streams, out warnings);

            Response.AddHeader("content-disposition", "attachment; filename=PORTARIA." + fileNameExtension);
            return File(bytes, mimeType);
        }

        [HttpGet]
        public ActionResult Dados(string id = "")
        {
            //BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            //modNF objmodNF = new modNF();
            //objmodNF.NF_CHAVE_ACESSO = id;
            //objmodNF = objBLNotaFiscal.GetByID(id);

            //ViewBag.DataDivergencias = LoadDataDivergenciaNfe(id);
            //ViewBag.DataMensagens = LoadDataMensagens(id);
            //ViewBag.id = id;

            //return View(objmodNF);
            return View();
        }


        private DataTable GetNeles()
        {
            DataTable dttEquipamentos = new DataTable();
            BLPortaria objBLPortaria = new BLPortaria();
            dttEquipamentos = objBLPortaria.GetByFilter("PTNL", "");
            dttEquipamentos.DefaultView.Sort = "[DATLAN] desc";
            return dttEquipamentos.DefaultView.ToTable();
        }

        private DataTable GetNeles(string txtNumeroPasta)
        {
            DataTable dttEquipamentos = new DataTable();
            BLPortaria objBLPortaria = new BLPortaria();
            dttEquipamentos = objBLPortaria.GetByFilter("PTNL", txtNumeroPasta);
            dttEquipamentos.DefaultView.Sort = "[DATLAN] desc";

            //Marcio Spinosa - 12/08/2019
            //return dttEquipamentos.DefaultView.ToTable();
            return dttEquipamentos;
            //Marcio Spinosa - 12/08/2019 - Fim
        }

   
    }
}
