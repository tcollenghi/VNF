/*
 * Autor: Marcio Spinosa- CR00008351
 * Data: 28/05/2018 
 * obs: Ajuste para o VNF não consultar o AD para trazer dados do usuário e sim o banco de dados.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Text;
using System.IO;
using System.Xml;
using System.Net.Mail;
using VNF.Portal;
using VNF.Portal.Models;
using VNF.Portal.DataLayer;
using VNF.Portal.ViewsModel;
using VNF.Business;
using MetsoFramework.Utils;
using MetsoFramework.SAP;
using System.Data.SqlClient;

namespace VNF.Portal.Controllers
{
    public class ComprasController : Controller
    {



        [HttpGet]
        public ActionResult NotaFiscal()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("NOTF", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|NOTF";

            ViewBag.UnidadeMetso = CarregarUnidadeMetso();
            ViewBag.Situacao = CarregarSituacaoNf();
            ViewBag.Motivo = CarregarMotivo();
            ViewBag.TipoData = CarregarData();
            ViewBag.TipoFrete = CarregarTipoFrete();
            ViewBag.StatusIntegracao = CarregarStatusIntegracao();
            ViewBag.AcessoModificar = (objAcesso.ConsultaAcesso("ANFE", Uteis.LogonName()) == true);
            return View();
        }

        [HttpPost]
        public ActionResult NotaFiscal(string txtNumeroDocumento, string txtCNPJ, string txtPedidoCompra, string UnidadeMetso, string Situacao, string TipoData, string txtDataDe, string txtDataAte)
        {
            ViewBag.UnidadeMetso = CarregarUnidadeMetso();
            ViewBag.Situacao = CarregarSituacaoNf();
            ViewBag.Motivo = CarregarMotivo();
            ViewBag.TipoData = CarregarData();
            return View();
        }

        [HttpGet]
        public string ExportarNOTF(string pNumeroDocumento, string pCNPJ, string pPedidoCompra, string pFornecedor, string pTipoDocumento, string pTipoFrete, string pUnidadeMetso, string pSituacao, string pMaterialRecebido, string pStatusIntegracao, string pTipoData, string pDataDe, string pDataAte,
                                    string pNfeid,//Marcio 28/08/2018 - CRXXXXX
                                    string pTipoNotaFiscal)//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
        {

            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();

            pSituacao = string.IsNullOrEmpty(pSituacao) ? "(TODAS)" : pSituacao;
            pUnidadeMetso = string.IsNullOrEmpty(pUnidadeMetso) ? "(TODAS)" : pUnidadeMetso;

            modNF.TipoData tipoData = modNF.TipoData.Emissao;
            if (pTipoData == "E")
                tipoData = modNF.TipoData.Emissao;
            else if (pTipoData == "C")
                tipoData = modNF.TipoData.Chegada;
            else if (pTipoData == "I")
                tipoData = modNF.TipoData.Integracao;

            DataTable dt = objBLNotaFiscal.GetByFilter(pDataDe,
                                                       pDataAte,
                                                       tipoData,
                                                       pNumeroDocumento,
                                                       pCNPJ == null ? string.Empty : pCNPJ.RemoveLetters(),
                                                       pSituacao.RemoveAccents(true),
                                                       pPedidoCompra == null ? string.Empty : pPedidoCompra,
                                                       pFornecedor,
                                                       pTipoDocumento,
                                                       pTipoFrete.RemoveAccents(true),
                                                       pUnidadeMetso.RemoveAccents(true),
                                                       pMaterialRecebido.RemoveAccents(true),
                                                       pStatusIntegracao.RemoveAccents(true),
                                                       //string.Empty);
                                                       pNfeid,
                                                       pTipoNotaFiscal);//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165

            string fileName = "NOTF - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
        }

        [HttpGet]
        public PartialViewResult LoadDataNOTF(string pNumeroDocumento, string pCNPJ, string pPedidoCompra, string pFornecedor, string pTipoDocumento, string pTipoFrete, string pUnidadeMetso, string pSituacao, string pMaterialRecebido, string pStatusIntegracao, string pTipoData, string pDataDe, string pDataAte,
                                              string pNFeid, //Marcio 28/08/2018 - CRXXXXX
                                              string pTipoNotaFiscal)//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
        {
            //Cria uma cookie que armazena os filtros da pesquisa
            HttpCookie cookie = new HttpCookie("LoadDataNOTF");
            cookie.Value = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}", pNFeid,
                                                                                                             pNumeroDocumento,
                                                                                                             pTipoNotaFiscal, //Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
                                                                                                             pCNPJ,
                                                                                                             pPedidoCompra,
                                                                                                             pFornecedor,
                                                                                                             pTipoDocumento,
                                                                                                             pTipoFrete.RemoveAccents(true),
                                                                                                             pUnidadeMetso.RemoveAccents(true),
                                                                                                             pSituacao.RemoveAccents(true),
                                                                                                             pMaterialRecebido.RemoveAccents(true),
                                                                                                             pStatusIntegracao.RemoveAccents(true),
                                                                                                             pTipoData,
                                                                                                             pDataDe,
                                                                                                             pDataAte);//Marcio 28/08/2018 - CRXXXXX
            Response.Cookies.Add(cookie);

            ViewBag.AcessoModificar = (new BLAcessos().ConsultaAcesso("ANFE", Uteis.LogonName()) == true);

            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();

            pSituacao = string.IsNullOrEmpty(pSituacao) ? "(TODAS)" : pSituacao;
            pUnidadeMetso = string.IsNullOrEmpty(pUnidadeMetso) ? "(TODAS)" : pUnidadeMetso;

            modNF.TipoData tipoData = modNF.TipoData.Emissao;
            if (pTipoData == "E")
                tipoData = modNF.TipoData.Emissao;
            else if (pTipoData == "C")
                tipoData = modNF.TipoData.Chegada;
            else if (pTipoData == "I")
                tipoData = modNF.TipoData.Integracao;


            DataTable dt = objBLNotaFiscal.GetByFilter(pDataDe,
                                                       pDataAte,
                                                       tipoData,
                                                       pNumeroDocumento,
                                                       pCNPJ == null ? string.Empty : pCNPJ.RemoveLetters(),
                                                       pSituacao.RemoveAccents(true),
                                                       pPedidoCompra == null ? string.Empty : pPedidoCompra,
                                                       pFornecedor,
                                                       pTipoDocumento,
                                                       pTipoFrete.RemoveAccents(true),
                                                       pUnidadeMetso.RemoveAccents(true),
                                                       pMaterialRecebido.RemoveAccents(true),
                                                       pStatusIntegracao.RemoveAccents(true),
                                                       //string.Empty);
                                                       pNFeid,//Marcio 28/08/2018 - CRXXXXX
                                                       pTipoNotaFiscal); //Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165

            return PartialView("GridNotaFiscal", dt);
        }

        [HttpGet]
        public PartialViewResult LoadItensNF(modNF model)
        {
            return PartialView(model);
        }

        [HttpGet]
        public PartialViewResult LoadDocumentosRelacionados()
        {
            return PartialView();
        }

        [HttpGet]
        public ActionResult Edit(string id = "")
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            modNF objmodNF = new modNF();
            objmodNF = objBLNotaFiscal.GetByID(id, false);

            ViewBag.DataDivergencias = LoadDataDivergenciaNfe(id);
            ViewBag.id = id;
            ViewBag.DocumentosRelacionados = objBLNotaFiscal.GetDocumentosRelacionados(id);

            if (objmodNF != null && objmodNF.VNF_TIPO_DOCUMENTO == "CTE")
                ViewBag.NotasFiscais = objBLNotaFiscal.GetNotasFiscaisCte(id);

            if (objmodNF == null)
                objmodNF = new modNF();

            DLLogApplication lDal = new DLLogApplication();
            ViewBag.LogApplication = lDal.GetByNFEID(id);

            ViewBag.PodeModificar = objBLNotaFiscal.PodeModificar(id);
            ViewBag.AcessoModificar = (new BLAcessos().ConsultaAcesso("ANFE", Uteis.LogonName()) == true);
            ViewBag.MaterialRecebido = objBLNotaFiscal.getMaterialRecebido(id);
            return View(objmodNF);
        }

        [HttpGet]
        public string LoadDataDivergenciaNfe(string pIdNfe)
        {
            int qtdeDivPendente = 0;
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();
            DataTable dt = objBLNotaFiscal.GetDivergencias(pIdNfe);
            string fundoAtivo = string.Empty;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (i == 0)
                    dadosRetorno.Append("<div id='accordionDivergencia' class='panel panel-default'>");
                else
                    dadosRetorno.Append("<div class='panel panel-default'>");

                fundoAtivo = string.Empty;
                if (dt.Rows[i]["SITUACAO"].ToString() == "ATIVO")
                {
                    fundoAtivo = "bg-color-redLight txt-color-white";
                    qtdeDivPendente++;
                }

                dadosRetorno.Append("<div class='panel-heading'>");
                dadosRetorno.Append("<h4 class='panel-title " + fundoAtivo + "'><a data-toggle='collapse' data-parent='#accordionDivergencia' href='#collapseDiv" + i.ToString() + "' class='collapsed'>");
                dadosRetorno.Append("ITEM NF " + dt.Rows[i]["ITENFE"].ToString().PadLeft(3, '0') + " - ");
                dadosRetorno.Append(dt.Rows[i]["CAMPO"].ToString() + " - ");
                dadosRetorno.Append(dt.Rows[i]["SITUACAO"].ToString());
                if (!string.IsNullOrEmpty(dt.Rows[i]["MOTIVO"].ToString()))
                    dadosRetorno.Append(" - " + dt.Rows[i]["MOTIVO"].ToString());
                dadosRetorno.Append(" </a></h4></div>");
                dadosRetorno.Append("<div id='collapseDiv" + i.ToString() + "' class='panel-collapse collapse'>");
                dadosRetorno.Append("<div class='panel-body padding0'>");

                dadosRetorno.Append("<table class='table table-striped table-bordered table-hover table-click  margin0' width='100%'>");
                dadosRetorno.Append("<thead>");

                dadosRetorno.Append("<tr>");
                dadosRetorno.Append("<th>Data</th>");
                dadosRetorno.Append("<th>Valor NF</th>");
                dadosRetorno.Append("<th>Valor PO</th>");
                dadosRetorno.Append("<th>Número PO</th>");
                dadosRetorno.Append("<th>Item PO</th>");
                dadosRetorno.Append("<th>Data Correção</th>");
                dadosRetorno.Append("<th>Justificativa</th>");
                dadosRetorno.Append("<th>Usuário Anulação</th>");
                dadosRetorno.Append("<th>Observação</th>");
                dadosRetorno.Append("<th></th>");
                dadosRetorno.Append("</tr>");
                dadosRetorno.Append("</thead>");

                dadosRetorno.Append("<tbody>");
                dadosRetorno.Append("<tr>");
                dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["DATLOG"]).ToString("dd/MM/yyyy") + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["VALOR_NFE"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["VALOR_PED"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["PEDCOM"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["ITEPED"].ToString() + "</td>");

                if (dt.Rows[i]["DATA_CORRECAO"] != DBNull.Value)
                    dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["DATA_CORRECAO"]).ToString("dd/MM/yyyy") + "</td>");
                else
                    dadosRetorno.Append("<td> - </td>");

                dadosRetorno.Append("<td>" + dt.Rows[i]["JUSTIFICATIVA"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["USUARIO_ANULACAO"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["OBSERVACAO"].ToString() + "</td>");
                if (dt.Columns["TEM_ANEXO"] == null || dt.Rows[i]["TEM_ANEXO"].ToString() == "0")
                {
                    dadosRetorno.Append("<td></td>");
                }
                else
                {
                    dadosRetorno.Append("<td><i class=\"fa fa-download\" title=\"Download do anexo\" style=\"cursor:pointer;\" onclick=\"DownloadAnexo(" + dt.Rows[i]["CODLOG"].ToString() + ")\"></i></td>");
                }

                dadosRetorno.Append("</tr>");
                dadosRetorno.Append("</tbody>");
                dadosRetorno.Append("</table>");

                dadosRetorno.Append("</div>");
                dadosRetorno.Append("</div>");
                dadosRetorno.Append("</div>");
            }

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
                                                                                      { "pendentes", qtdeDivPendente},
                                                                                      { "data", dadosRetorno.ToString()} });
        }

        [HttpGet]
        public ActionResult DownloadAnexo(string CodLog)
        {
            byte[] file = null;
            string fileExtension = null;
            string fileName = null;
            BLDivergencias blDiv = new BLDivergencias();
            DataTable dt = blDiv.DownloadAnexoDivergencia(CodLog);
            file = (byte[])dt.Rows[0]["ANEXO"];
            fileExtension = dt.Rows[0]["ANEXOEXTENSAO"].ToString();
            fileName = dt.Rows[0]["ANEXONOME"].ToString();
            return File(file, fileExtension, fileName);
        }

        [HttpGet]
        public string LoadDataMensagens(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();
            DataTable dt = objBLNotaFiscal.GetMensagens(pIdNfe);

            string status = objBLNotaFiscal.GetSituacaoDocumento(pIdNfe);
            string fundo = string.Empty;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (i == 0)
                    dadosRetorno.Append("<div id='accordion' class='panel panel-default'>");
                else
                    dadosRetorno.Append("<div class='panel panel-default'>");

                switch (dt.Rows[i]["SITUACAO"].ToString())
                {
                    case "AUTORIZADA":
                        fundo = "bg-color-greenLight txt-color-white";
                        break;
                    case "DOC-e ENVIADO PARA IP":
                        fundo = "bg-color-greenLight txt-color-white";
                        break;
                    case "NAO AUTORIZADA":
                        fundo = "bg-color-redLight txt-color-white";
                        break;
                    case "RECUSADA":
                        fundo = "bg-color-redLight txt-color-white";
                        break;
                    case "DOC-e CANCELADO":
                        fundo = (status == "CANCELADA" ? "bg-color-redLight txt-color-white" : "");
                        break;
                    case "ASSOCIAÇÃO VIA PORTAL":
                        fundo = "";
                        break;
                    case "CANCELAMENTO DESFEITO DE DOC-e":
                        fundo = "";
                        break;
                    default:
                        fundo = "";
                        break;
                }

                dadosRetorno.Append("<div class='panel-heading'>");
                dadosRetorno.Append("<h4 class='panel-title " + fundo + "'><a data-toggle='collapse' data-parent='#accordion' href='#collapse" + i.ToString() + "' class='collapsed'>");
                dadosRetorno.Append("Data: " + Convert.ToDateTime(dt.Rows[i]["DATENV"]).ToString("dd/MM/yyyy") + " - ");
                if (!string.IsNullOrEmpty(dt.Rows[i]["EMAIL"].ToString()))
                    dadosRetorno.Append(dt.Rows[i]["EMAIL"].ToString() + " - ");

                dadosRetorno.Append(dt.Rows[i]["SITUACAO"].ToString() + " </a></h4>");
                dadosRetorno.Append("</div>");
                dadosRetorno.Append("<div id='collapse" + i.ToString() + "' class='panel-collapse collapse'>");
                dadosRetorno.Append("<div class='panel-body'>");
                dadosRetorno.Append(dt.Rows[i]["MENENV"].ToString().Replace("\r", "</br>"));
                dadosRetorno.Append("</div>");
                dadosRetorno.Append("</div>");
                dadosRetorno.Append("</div>");
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
            { "data", dadosRetorno.ToString()} });
        }

        /// <summary>
        /// Método que carrega as datas de comparações
        /// </summary>
        /// <param name="pIdNfe"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 28/05/2018- CR00008351 - ajuste para apresentar o nome do usuário sem AD</example>
        [HttpGet]
        public string LoadDatasComparacoes(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();

            DataTable dt = objBLNotaFiscal.GetHistoricoValidacoes(pIdNfe);
            string strUserName = string.Empty;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //Marcio Spinosa - 28/05/2018- CR00008351
                //strUserName = Uteis.GetUserNameBySamId(dt.Rows[i]["com_usuario"].ToString());
                strUserName = dt.Rows[i]["USUNOMUSU"].ToString();
                //Marcio Spinosa - 28/05/2018- CR00008351 - Fim
                if (strUserName.Split(' ').Count() > 1)
                {
                    strUserName = String.IsNullOrEmpty(strUserName) ? "" : strUserName.Split(' ')[0] + " " + strUserName.Split(' ')[strUserName.Split(' ').Count() - 1];
                }



                dadosRetorno.Append("<tr role='row' id='row" + Convert.ToDateTime(dt.Rows[i]["com_data_hora"]).ToString("ddMMyyyyHHmmss") + "' onclick='LoadComparacoes(&#39;" + dt.Rows[i]["com_data_hora"].ToString() + "&#39;);'>");
                dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["com_data_hora"]).ToString("dd/MM/yyyy HH:mm") + "</td>");
                dadosRetorno.Append("<td>" + strUserName + "</td>");
                dadosRetorno.Append("</tr>");
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
            { "data", dadosRetorno.ToString()} });
        }

        [HttpGet]
        public string LoadComparacoes(string pData, string pNfeID)
        {
            string strRetorno = "";
            DateTime datDataComparacao = Convert.ToDateTime(pData);
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            strRetorno = objBLNotaFiscal.GetTabelaValidacoes(datDataComparacao, pNfeID);


            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
                                                                                      { "data", strRetorno }});
        }

        [HttpGet]
        private List<SelectListItem> CarregarUnidadeMetso()
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            DataTable dt = objBLNotaFiscal.GetUnidadeMetso();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                lista.Add(new SelectListItem { Value = dt.Rows[i][0].ToString().RemoveAccents(true), Text = dt.Rows[i][0].ToString() });
            }
            return lista;
        }

        [HttpGet]
        public ActionResult DownloadDanfe(string pIdNfe)
        {
            string strIdDoc = "";
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            //Marcio Spinosa - 26/02/2019 - CR00009165
            //modNF objNF = objBLNotaFiscal.GetByID(pIdNfe, false); 
            SqlDataReader dr = modSQL.ExecuteReader(@"SELECT VNF_CONTEUDO_XML, VNF_TIPO_DOCUMENTO, NF_OUTROS_VERSAO FROM TbDOC_CAB C
                                                      INNER JOIN TBDOC_CAB_NFE N ON N.NFEID = C.NFEID WHERE C.NFEID = '" + pIdNfe + "' ", modSQL.connectionString);
            //Marcio Spinosa - 26/02/2019 - CR00009165 - Fim
            string FullPath = objBLNotaFiscal.GetDanfePath() + pIdNfe + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".pdf";
            byte[] file = null;
            string Message = "";

            //Marcio Spinosa - 26/02/2019 - CR00009165
            #region COMENTARIO
            //if (objNF.VNF_TIPO_DOCUMENTO != null)
            //{
            //    if (objNF.VNF_TIPO_DOCUMENTO.ToUpper() == modNF.tipo_doc_nfe)
            //        file = MetsoFramework.Files.XMLFile.CreateDanfe(objNF.VNF_CONTEUDO_XML, FullPath, MetsoFramework.Files.XMLFile.OutputType.Bynary, out Message);
            //    else
            //        if (objNF.NF_OUTROS_VERSAO == "1.04")
            //        {
            //            file = MetsoFramework.Files.XMLFile.CreateDacte(objNF.VNF_CONTEUDO_XML, FullPath, MetsoFramework.Files.XMLFile.OutputType.Bynary, out Message);
            //        }
            //        else
            //        {
            //            file = MetsoFramework.Files.XMLFile.CreateDacte3_0(objNF.VNF_CONTEUDO_XML, FullPath, MetsoFramework.Files.XMLFile.OutputType.Bynary, out Message);
            //        }
            //    string fileExtension = ".pdf";
            //    string fileName = pIdNfe + ".pdf";
            //    string mimeType = "application/pdf";
            //    Response.AppendHeader("Content-Disposition", "inline; filename=" + fileName);
            //    if (file != null)
            //    {
            //        return File(file, mimeType);
            //    }
            //    else
            //    {
            //        ViewBag.Message = Message;
            //        return View();
            //    }
            //}
            //else
            //{
            //    ViewBag.Message = Message;
            //    return View();
            //}
            #endregion
            if (dr.Read())
            {

                if (!string.IsNullOrEmpty(dr["VNF_CONTEUDO_XML"].ToString()))
                {
                    if (dr["VNF_TIPO_DOCUMENTO"].ToString().ToUpper() == modNF.tipo_doc_nfe)
                        file = MetsoFramework.Files.XMLFile.CreateDanfe(dr["VNF_CONTEUDO_XML"].ToString(), FullPath, MetsoFramework.Files.XMLFile.OutputType.Bynary, out Message);
                    else
                        if (dr["NF_OUTROS_VERSAO"].ToString() == "1.04")
                    {
                        file = MetsoFramework.Files.XMLFile.CreateDacte(dr["VNF_CONTEUDO_XML"].ToString(), FullPath, MetsoFramework.Files.XMLFile.OutputType.Bynary, out Message);
                    }
                    else
                    {
                        file = MetsoFramework.Files.XMLFile.CreateDacte3_0(dr["VNF_CONTEUDO_XML"].ToString(), FullPath, MetsoFramework.Files.XMLFile.OutputType.Bynary, out Message);
                    }
                    string fileExtension = ".pdf";
                    string fileName = pIdNfe + ".pdf";
                    string mimeType = "application/pdf";
                    Response.AppendHeader("Content-Disposition", "inline; filename=" + fileName);
                    if (file != null)
                    {
                        return File(file, mimeType);
                    }
                    else
                    {
                        ViewBag.Message = Message;
                        return View();
                    }
                }
                else
                {
                    ViewBag.Message = Message;
                    return View();
                }
            }
            else
            {
                ViewBag.Message = Message;
                return View();
            }
            //Marcio Spinosa - 26/02/2019 - CR00009165 - Fim
        }

        [HttpGet]
        public string ReenviarEmail(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            string dadosRetorno = objBLNotaFiscal.ReenviarEmail(pIdNfe);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
                                                                                      { "data", dadosRetorno.ToString()} });
        }

        [HttpGet]
        public ActionResult GetXml(string id)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            string dadosRetorno = objBLNotaFiscal.GetXml(id);

            //string dadosRetorno = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"2.00\"><NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\"><infNFe Id=\"NFe35150100988202000193550010000171341932337988\" versao=\"2.00\"><ide><cUF>35</cUF><cNF>93233798</cNF><natOp>Vendas</natOp><indPag>1</indPag><mod>55</mod><serie>1</serie><nNF>17134</nNF><dEmi>2015-01-05</dEmi><tpNF>1</tpNF><cMunFG>3552205</cMunFG><tpImp>2</tpImp><tpEmis>1</tpEmis><cDV>8</cDV><tpAmb>1</tpAmb><finNFe>1</finNFe><procEmi>0</procEmi><verProc>AGIW</verProc></ide><emit><CNPJ>00988202000193</CNPJ><xNome>CONECT PECAS E ACESSORIOS INDUSTRIAIS EIRELLI</xNome><xFant>CONECT</xFant><enderEmit><xLgr>AV. FERNANDO STECCA</xLgr><nro>423</nro><xBairro>VILA SAO JUDAS</xBairro><cMun>3552205</cMun><xMun>Sorocaba</xMun><UF>SP</UF><CEP>18087149</CEP><cPais>1058</cPais><xPais>BRASIL</xPais><fone>1532384711</fone></enderEmit><IE>669308700118</IE><CRT>1</CRT></emit><dest><CNPJ>16622284000198</CNPJ><xNome>METSO BRASIL INDUSTRIA E COMERCIO LTDA.</xNome><enderDest><xLgr>AV. INDEPENDENCIA</xLgr><nro>2500</nro><xBairro>Iporanga</xBairro><cMun>3552205</cMun><xMun>Sorocaba</xMun><UF>SP</UF><CEP>18087101</CEP><cPais>1058</cPais><xPais>BRASIL</xPais><fone>1521021300</fone></enderDest><IE>669575300114</IE><email>nfefornecedor.mctbr@metso.com</email></dest><det nItem=\"1\"><prod><cProd>1001985750</cProd><cEAN></cEAN><xProd>TEE FEMEA GIR LATERAL X MACHO JIC 37  3/4 8 R6X-S</xProd><NCM>73072200</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>20.0000</qCom><vUnCom>45.6100</vUnCom><vProd>912.20</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>20.0000</qTrib><vUnTrib>45.6100</vUnTrib><indTot>1</indTot><xPed>4501269058</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>35.12</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501269058;</infAdProd></det><det nItem=\"2\"><prod><cProd>MM0349200</cProd><cEAN></cEAN><xProd>PORCA ACO M12X1,5 M06LCFX</xProd><NCM>73072900</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>4.0000</qCom><vUnCom>1.9500</vUnCom><vProd>7.80</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>4.0000</qTrib><vUnTrib>1.9500</vUnTrib><indTot>1</indTot><xPed>4501280945</xPed><nItemPed>000050</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>0.30</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501280945;</infAdProd></det><det nItem=\"3\"><prod><cProd>53334267006</cProd><cEAN></cEAN><xProd>MANGUEIRA C/ TERMINAIS PONTA LISA C/ 4000MM</xProd><NCM>40092190</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>5.0000</qCom><vUnCom>101.1300</vUnCom><vProd>505.65</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>5.0000</qTrib><vUnTrib>101.1300</vUnTrib><indTot>1</indTot><xPed>4501280945</xPed><nItemPed>000060</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>19.47</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501280945;</infAdProd></det><det nItem=\"4\"><prod><cProd>53334267006</cProd><cEAN></cEAN><xProd>MANGUEIRA C/ TERMINAIS PONTA LISA C/ 4000MM</xProd><NCM>40092190</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>1.0000</qCom><vUnCom>101.1400</vUnCom><vProd>101.14</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>1.0000</qTrib><vUnTrib>101.1400</vUnTrib><indTot>1</indTot><xPed>4501291879</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>3.89</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501291879;</infAdProd></det><det nItem=\"5\"><prod><cProd>1044252204</cProd><cEAN></cEAN><xProd>MANGUEIRA F471TC6868080806-775MM</xProd><NCM>40092110</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>68.0000</qCom><vUnCom>63.7300</vUnCom><vProd>4333.64</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>68.0000</qTrib><vUnTrib>63.7300</vUnTrib><indTot>1</indTot><xPed>4501295340</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>166.85</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501295340;</infAdProd></det><det nItem=\"6\"><prod><cProd>MM0217667</cProd><cEAN></cEAN><xProd>BUJAO SEXTAVADO INTERNO 1/2 BSP INOX VSTI1/2ED71</xProd><NCM>73072200</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>1.0000</qCom><vUnCom>48.9400</vUnCom><vProd>48.94</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>1.0000</qTrib><vUnTrib>48.9400</vUnTrib><indTot>1</indTot><xPed>4501295340</xPed><nItemPed>000050</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>1.88</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501295340;</infAdProd></det><det nItem=\"7\"><prod><cProd>1044252612</cProd><cEAN></cEAN><xProd>MANGUEIRA F471TC6868080806-2175MM</xProd><NCM>40092190</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>8.0000</qCom><vUnCom>99.2300</vUnCom><vProd>793.84</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>8.0000</qTrib><vUnTrib>99.2300</vUnTrib><indTot>1</indTot><xPed>4501297913</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>30.56</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501297913;</infAdProd></det><total><ICMSTot><vBC>0.00</vBC><vICMS>0.00</vICMS><vBCST>0.00</vBCST><vST>0.00</vST><vProd>6703.21</vProd><vFrete>0.00</vFrete><vSeg>0.00</vSeg><vDesc>0.00</vDesc><vII>0</vII><vIPI>0.00</vIPI><vPIS>0.00</vPIS><vCOFINS>0.00</vCOFINS><vOutro>0.00</vOutro><vNF>6703.21</vNF></ICMSTot></total><transp><modFrete>1</modFrete><transporta><CNPJ>52548435014200</CNPJ><xNome>JSL S/A</xNome><IE>669653462115</IE><xEnder>AVENIDA JEROME CASE 2302</xEnder><xMun>Sorocaba</xMun><UF>SP</UF></transporta><vol><qVol>1</qVol><esp>VOLUME</esp></vol></transp><cobr><dup><nDup>017134A</nDup><dVenc>2015-02-16</dVenc><vDup>6703.21</vDup></dup></cobr><infAdic><infCpl>Empresa optante pelo simples nacional conforme L.C 123/2006 - ;Permite o aproveitamento de credito de ICMS conforme aliquota 3,85% no valor de R$: 258,07</infCpl></infAdic></infNFe><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#NFe35150100988202000193550010000171341932337988\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>Qab3OPDblmrAfISyM+FqgBIdVaw=</DigestValue></Reference></SignedInfo><SignatureValue>hLzGsismfcllwXp/O9b78ifzwlMDDOgDgmEVusDKwx8Wh1urbji58J/9+XX0i/hATqnybzCTO6r+f9LoSkcohtNkuE/+xQ0a4UR+pXGUFIw7SPhRobwh9LVGQPHalF06uu5gbb1LI0i5Pt/mokCT2rNrgN5hN93GT5q15USrLZdM+PZf2o6uqelpgAvachCZl7yDc4RtPC6XVlMUzvXBQQ42s8lZlpXdwLW2vksaX624naw8eAo8glCqDDSktvettqtz5MJahSs0ZhqQI8wLVp8NEM7xKHKJIhWgFmT3sdriHC6rzZXiHgygtfq5aZ1ebS1W+v929kR1B2cUFBQzng==</SignatureValue><KeyInfo><X509Data><X509Certificate>MIIH3jCCBcagAwIBAgIIP8+W3XAykcIwDQYJKoZIhvcNAQELBQAwTDELMAkGA1UEBhMCQlIxEzARBgNVBAoTCklDUC1CcmFzaWwxKDAmBgNVBAMTH1NFUkFTQSBDZXJ0aWZpY2Fkb3JhIERpZ2l0YWwgdjIwHhcNMTQwMzEyMTIwNjAwWhcNMTUwMzEyMTIwNjAwWjCB9zELMAkGA1UEBhMCQlIxEzARBgNVBAoTCklDUC1CcmFzaWwxFDASBgNVBAsTCyhFTSBCUkFOQ08pMRgwFgYDVQQLEw8wMDAwMDEwMDUwNDQyMzAxFDASBgNVBAsTCyhFTSBCUkFOQ08pMRQwEgYDVQQLEwsoRU0gQlJBTkNPKTEUMBIGA1UECxMLKEVNIEJSQU5DTykxFDASBgNVBAsTCyhFTSBCUkFOQ08pMRQwEgYDVQQLEwsoRU0gQlJBTkNPKTE1MDMGA1UEAxMsQ09ORUNUIFBFQ0FTIEUgQUNFU1NPUklPUyBJTkRVU1RSSUFJUyBFSVJFTEkwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCQY6JUjF+tYJu9iuFZQmXvy18VPkrddnPJs+U0uxdd0BKIGoqSFXAEvZ2KZJIG7gqdz2VYE9vrRF096FQ+kaCndg0bWz0Fk4+x9RX8cfsKAvk3WAxhp0M9UtgMsgN5BjejcVlkwQVjlscc0s1rHOUZp0vsCjOG+sk/XGcRz6jQ1fG43U1V1KlFDwlZ3F6pvzVprIWbWvmRSJarXly6pUukEBniXVZOubEBaO+Qbnv5T1vuJBF4UqPmV/g2KdPx+STxW3fU0LhO7Mcb4D42DVImai1V63FxOxPGYf2xMT3c3xF7/3hr5tUwO+x0RNZTLRkgBMCZGAIARisIBaZ0YWCxAgMBAAGjggMWMIIDEjCBlwYIKwYBBQUHAQEEgYowgYcwRwYIKwYBBQUHMAKGO2h0dHA6Ly93d3cuY2VydGlmaWNhZG9kaWdpdGFsLmNvbS5ici9jYWRlaWFzL3NlcmFzYWNkdjIucDdiMDwGCCsGAQUFBzABhjBodHRwOi8vb2NzcC5jZXJ0aWZpY2Fkb2RpZ2l0YWwuY29tLmJyL3NlcmFzYWNkdjIwHwYDVR0jBBgwFoAUmuCDENcmm+m62oKygc45GtOHcIYwcQYDVR0gBGowaDBmBgZgTAECAQYwXDBaBggrBgEFBQcCARZOaHR0cDovL3B1YmxpY2FjYW8uY2VydGlmaWNhZG9kaWdpdGFsLmNvbS5ici9yZXBvc2l0b3Jpby9kcGMvZGVjbGFyYWNhby1zY2QucGRmMIHwBgNVHR8EgegwgeUwSaBHoEWGQ2h0dHA6Ly93d3cuY2VydGlmaWNhZG9kaWdpdGFsLmNvbS5ici9yZXBvc2l0b3Jpby9sY3Ivc2VyYXNhY2R2Mi5jcmwwQ6BBoD+GPWh0dHA6Ly9sY3IuY2VydGlmaWNhZG9zLmNvbS5ici9yZXBvc2l0b3Jpby9sY3Ivc2VyYXNhY2R2Mi5jcmwwU6BRoE+GTWh0dHA6Ly9yZXBvc2l0b3Jpby5pY3BicmFzaWwuZ292LmJyL2xjci9TZXJhc2EvcmVwb3NpdG9yaW8vbGNyL3NlcmFzYWNkdjIuY3JsMA4GA1UdDwEB/wQEAwIF4DAdBgNVHSUEFjAUBggrBgEFBQcDAgYIKwYBBQUHAwQwgb8GA1UdEQSBtzCBtIEZTUFVUk9AQ09ORUNUUEFSS0VSLkNPTS5CUqA+BgVgTAEDBKA1EzMwNTA5MTk1ODAwMDAwNTc4ODYwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDCgIwYFYEwBAwKgGhMYTUFVUk8gRkVSTkFOREVTIERBIENPU1RBoBkGBWBMAQMDoBATDjAwOTg4MjAyMDAwMTkzoBcGBWBMAQMHoA4TDDAwMDAwMDAwMDAwMDANBgkqhkiG9w0BAQsFAAOCAgEAY45kLVDjj+DA+v37sJ0i8OTIH+Sm85SslyFtliDL/HYGxFwGqmYas+MpGeEjCKVPzIAK+ONkNELV8afLehQHp1OB1kAEUWHDStyoLnbmQyElbBxFivcJ8pizSGK0e5n0G+W+9e/ihC8jNp9+PRUu5UJ1qlDqCttZF6h7dGauu3QPIojWoxtaqQIyMNpSAZ8QWaXotndA6BTy6WRmvdmZfRTvuK5Wzx8Jr2YJiqU7l0QHZeUJEdT8LuhlqzV2xAMXgweEjJANeJSUh7wVVbzBbGM8YBqvvNeYooWwJOsVoeIihnYrsq3zYRgJhkEm5AUcr/pu0AOyr0Q05Lqgj5MOy+chq6ivAwhQNWuYkN2qV/Wv5JqpUCqzhN9yDJOFZTJs/WGKGl07PfHd4A/C84o0/kjcoB7Eni4DJvmEg+RC8oYAQgzZXYmMZJPV/Wv88tsaq9quEvdz/0DAwuQAH7gdekxEkKH5SwSTg8wOe5OtaTnkc2/sE2KKe20+qd2tcqJIaDIHephofC/v8tIuE0aiFphrt69ptEuuZZQSQEK9I/wOgFyyTSlHTXxfc+ahmlQgoznrdFbi7+dIx7lwMMi7/c6A9ioAu02grz/EpCOqdlA0OBs9r95i7PtAp7HlYmt4pSjAdFMg8xJMT6orj9B0neTzMDONeOjXQf/zC3LVIvw=</X509Certificate></X509Data></KeyInfo></Signature></NFe><protNFe versao=\"2.00\" xmlns=\"http://www.portalfiscal.inf.br/nfe\"><infProt><tpAmb>1</tpAmb><verAplic>SP_NFE_PL_006q</verAplic><chNFe>35150100988202000193550010000171341932337988</chNFe><dhRecbto>2015-01-05T17:22:10</dhRecbto><nProt>135150004276546</nProt><digVal>Qab3OPDblmrAfISyM+FqgBIdVaw=</digVal><cStat>100</cStat><xMotivo>Autorizado o uso da NF-e</xMotivo></infProt></protNFe></nfeProc>";
            dadosRetorno = dadosRetorno.Replace("\"", "'");

            string fileName = id + ".xml";

            string xmlText = dadosRetorno;

            // Create an UTF8 byte buffer from it (assuming UTF8 is the desired encoding)
            byte[] xmlBuffer = Encoding.UTF8.GetBytes(xmlText);

            // Write the UTF8 byte buffer to the response stream
            Stream stream = Response.OutputStream;
            Response.ContentType = "text/xml";
            Response.ContentEncoding = Encoding.UTF8;
            stream.Write(xmlBuffer, 0, xmlBuffer.Length);

            // Done
            stream.Close();
            Response.End();
            return File(fileName, "application/xml", fileName);
        }

        [HttpGet]
        public List<SelectListItem> CarregarSituacaoNf()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "ACEITA", Text = "ACEITA" });
            lista.Add(new SelectListItem { Value = "CANCELADA", Text = "CANCELADA" });
            lista.Add(new SelectListItem { Value = "PENDENTE", Text = "PENDENTE" });
            lista.Add(new SelectListItem { Value = "RECUSADA", Text = "RECUSADA" });
            lista.Add(new SelectListItem { Value = "REJEITADA", Text = "REJEITADA" });

            return lista;
        }

        [HttpGet]
        public List<SelectListItem> CarregarData()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "E", Text = "Emissão da NFe", Selected = true });
            lista.Add(new SelectListItem { Value = "C", Text = "Chegada da DANFE" });
            lista.Add(new SelectListItem { Value = "I", Text = "Integração SAP" });


            return lista;
        }

        [HttpGet]
        public List<SelectListItem> CarregarMotivo()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "CORRIGIDO", Text = "CORRIGIDO" });
            lista.Add(new SelectListItem { Value = "ANULADO", Text = "ANULADO" });
            lista.Add(new SelectListItem { Value = "ERRO ASSOCIACAO", Text = "ERRO ASSOCIACAO" });
            lista.Add(new SelectListItem { Value = "TEMPO MAXIMO PARA CORRECAO (5 DIAS) ATINGIDO", Text = "TEMPO MAXIMO PARA CORRECAO (5 DIAS) ATINGIDO" });
            lista.Add(new SelectListItem { Value = "NF CANCELADA POR FORNECEDOR", Text = "NF CANCELADA POR FORNECEDOR" });
            return lista;
        }

        [HttpGet]
        public List<SelectListItem> CarregarStatusIntegracao()
        {
            //BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            //DataTable dttTipoFrete = new DataTable();
            //dttTipoFrete = objBLNotaFiscal.GetStatusIntegracao();

            //List<SelectListItem> lista = new List<SelectListItem>();
            //foreach (DataRow dtrLinha in dttTipoFrete.Rows)
            //{
            //    lista.Add(new SelectListItem { Value = dtrLinha["STATUS_INTEGRACAO"].ToString().RemoveAccents(true), Text = dtrLinha["STATUS_INTEGRACAO"].ToString() });
            //}

            //return lista;

            return new List<SelectListItem>()
            {
                new SelectListItem{Value = "PENDENTE", Text = "PENDENTE"},
                new SelectListItem{Value = "INCOMPLETA", Text = "INCOMPLETA"},
                new SelectListItem{Value = "CONCLUÍDO", Text = "CONCLUÍDO"}
            };
        }

        [HttpGet]
        public List<SelectListItem> CarregarTipoFrete()
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            DataTable dttTipoFrete = new DataTable();
            dttTipoFrete = objBLNotaFiscal.GetTipoFrete();

            List<SelectListItem> lista = new List<SelectListItem>();
            foreach (DataRow dtrLinha in dttTipoFrete.Rows)
            {
                lista.Add(new SelectListItem { Value = dtrLinha["TipoFrete"].ToString().RemoveAccents(true), Text = dtrLinha["TipoFrete"].ToString() });
            }

            return lista;
        }

        [HttpGet]
        public ActionResult Verif()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("VNFE", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|VNFE";

            //ViewBag.UnidadeMetso = CarregarUnidadeMetso();
            //ViewBag.Situacao = CarregarSituacaoNf();
            //ViewBag.TipoData = CarregarData();
            //ViewBag.TipoFrete = CarregarTipoFrete();
            //ViewBag.StatusIntegracao = CarregarStatusIntegracao();

            return View();
        }

        #region :: JSON Methods ::
        [HttpPost]
        public string GetUnidadesMetso()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Result = "ok",
                UnidadesMetso = CarregarUnidadeMetso()
            }).ToString();
        }

        [HttpPost]
        public string GetSituacoesNf()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Result = "ok",
                SituacoesNf = CarregarSituacaoNf()
            }).ToString();
        }

        [HttpPost]
        public string GetTiposData()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Result = "ok",
                TiposData = CarregarData()
            }).ToString();
        }

        [HttpPost]
        public string GetTiposFrete()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Result = "ok",
                TiposFrete = CarregarTipoFrete()
            }).ToString();
        }

        [HttpPost]
        public string GetStatusIntegracao()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Result = "ok",
                StatusIntegracao = CarregarStatusIntegracao()
            }).ToString();
        }
        #endregion :: JSON Methods ::

        [HttpGet]
        public PartialViewResult LoadDataVNFE(string pNumeroNf, string pPasta, string pUnidade, string pFornecedor, string pSituacao, string pTipoDocumento, string pTipoFrete, string pRelevante, string pMaterialRecebido, string pStatusIntegracao, string pTipoData, string pDataDe, string pDataAte, string pCodFornecedor,
                                              int? pQtdRegistros,
                                              string pNfeid,//Marcio Spinosa - 28/08/2018 - CRXXXXX
                                              string pTipoNotaFiscal)//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
        {
            int min = 100, max = 50000;
            pQtdRegistros = pQtdRegistros ?? min;
            pQtdRegistros = pQtdRegistros < min ? min : pQtdRegistros > max ? max : pQtdRegistros;

            HttpCookie cookie = new HttpCookie("LoadDataVNFE");
            cookie.Value = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}|{15}|{16}", pNfeid, pNumeroNf, pTipoNotaFiscal, pPasta, pUnidade.RemoveAccents(true), pFornecedor, pSituacao.RemoveAccents(true), pTipoDocumento.RemoveAccents(true), pTipoFrete.RemoveAccents(true), pRelevante.RemoveAccents(true), pMaterialRecebido.RemoveAccents(true), pStatusIntegracao.RemoveAccents(true), pTipoData, pDataDe, pDataAte, pCodFornecedor, pQtdRegistros);//Marcio Spinosa - 28/08/2018 - CRXXXXX
            Response.Cookies.Add(cookie);

            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            bool exceedQtd = false;
            DataTable dttNotasFiscais = objBLNotaFiscal.GetByFilterRegistroFiscal(pNumeroNf, pPasta, pUnidade.RemoveAccents(true), pFornecedor, pSituacao.RemoveAccents(true), pTipoDocumento.RemoveAccents(true), pTipoFrete.RemoveAccents(true), pRelevante.RemoveAccents(true), pMaterialRecebido.RemoveAccents(true), pStatusIntegracao.RemoveAccents(true), pTipoData, pDataDe, pDataAte, pCodFornecedor,
                                                                                  pQtdRegistros.Value, ref exceedQtd, pNfeid, //Marcio Spinosa - 28/08/2018 - CRXXXXX
                                                                                  pTipoNotaFiscal);//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
            //Marcio Spinosa - 14/06/2019 - SR00284683
            if (dttNotasFiscais.Rows.Count > 0)
                ViewBag.MaterialRecebido = objBLNotaFiscal.getMaterialRecebido(dttNotasFiscais.Rows[0][0].ToString());
            //Marcio Spinosa - 14/06/2019  - SR00284683 - Fim
            var msg = "* Quantidade de registros excedem o limite de {0} registros consultados (por favor, melhore o seu filtro)";
            ViewBag.ExceedQtdMsg = exceedQtd ? String.Format(msg, pQtdRegistros) : "";
            return PartialView("GridVerificar", dttNotasFiscais);
        }

        [HttpGet]
        public string ExportVNFE(string pNumeroNf, string pPasta, string pUnidade, string pFornecedor, string pSituacao, string pTipoDocumento, string pTipoFrete, string pRelevante, string pMaterialRecebido, string pStatusIntegracao, string pTipoData, string pDataDe, string pDataAte, string pCodFornecedor,
                                 int? pQtdRegistros,
                                 string pNfeid,//Marcio Spinosa - 28/08/2018 - CRXXXXX
                                 string pTipoNotaFiscal)//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
        {
            int min = 100, max = 50000;
            pQtdRegistros = pQtdRegistros ?? min;
            pQtdRegistros = pQtdRegistros < min ? min : pQtdRegistros > max ? max : pQtdRegistros;

            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            bool exceedQtd = false;
            DataTable dttNotasFiscais = objBLNotaFiscal.GetByFilterRegistroFiscal(pNumeroNf, pPasta, pUnidade.RemoveAccents(true), pFornecedor, pSituacao.RemoveAccents(true), pTipoDocumento.RemoveAccents(true), pTipoFrete.RemoveAccents(true), pRelevante.RemoveAccents(true), pMaterialRecebido.RemoveAccents(true), pStatusIntegracao.RemoveAccents(true), pTipoData, pDataDe, pDataAte, pCodFornecedor,
                                                                                  pQtdRegistros.Value, ref exceedQtd,
                                                                                  pNfeid, //Marcio Spinosa - 28/08/2018 - CRXXXXX
                                                                                  pTipoNotaFiscal);//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165

            string fileName = "VNFE - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            for (var i = 0; i < dttNotasFiscais.Rows.Count; i++)
            {
                if (dttNotasFiscais.Rows[i]["NFEID"].ToString().Trim().Length > 0)
                    dttNotasFiscais.Rows[i]["NFEID"] = "'" + dttNotasFiscais.Rows[i]["NFEID"];
                if (dttNotasFiscais.Rows[i]["NF_IDE_NNF"].ToString().Trim().Length > 0)
                    dttNotasFiscais.Rows[i]["NF_IDE_NNF"] = "'" + dttNotasFiscais.Rows[i]["NF_IDE_NNF"];
                if (dttNotasFiscais.Rows[i]["NF_IDE_SERIE"].ToString().Trim().Length > 0)
                    dttNotasFiscais.Rows[i]["NF_IDE_SERIE"] = "'" + dttNotasFiscais.Rows[i]["NF_IDE_SERIE"];
                if (dttNotasFiscais.Rows[i]["NF_EMIT_CNPJ"].ToString().Trim().Length > 0)
                    dttNotasFiscais.Rows[i]["NF_EMIT_CNPJ"] = "'" + dttNotasFiscais.Rows[i]["NF_EMIT_CNPJ"];
                if (dttNotasFiscais.Rows[i]["CODFOR"].ToString().Trim().Length > 0)
                    dttNotasFiscais.Rows[i]["CODFOR"] = "'" + dttNotasFiscais.Rows[i]["CODFOR"];
            }

            MetsoFramework.Files.HtmlFile.ExportToExcel(dttNotasFiscais, filePath);

            var exceedQtdMsg = exceedQtd ? String.Format("* Quantidade de registros excedem o limite de {0} registros exportados", pQtdRegistros) : "";
            return Serialization.JSON.CreateString(new Dictionary<string, object>() {{ "exceedQtdMsg", exceedQtdMsg },
                                                                                     { "fileName", fileName },
                                                                                     });
        }

        [HttpGet]
        public string RecusaVNFE(string NfeId, string tipo, string observacao)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            string retorno = string.Empty;
            try
            {
                if (tipo == "R")
                    objBLNotaFiscal.Recusar(NfeId, observacao);
                else
                    objBLNotaFiscal.DesfazerRecusa(NfeId, observacao);
                retorno = "Ação concluída com sucesso!";
            }
            catch (Exception erro)
            {
                retorno = "Erro ao executar ação.";
            }

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
            { "data", retorno.ToString()}});

        }

        [HttpGet]
        public List<SelectListItem> CarregarSituacao()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "ATIVO", Text = "ATIVO", Selected = true });
            lista.Add(new SelectListItem { Value = "INATIVO", Text = "INATIVO" });
            lista.Add(new SelectListItem { Value = "(TODAS)", Text = "(TODAS)" });
            return lista;
        }

        [HttpGet]
        public List<SelectListItem> ddlTipoDivergencia()
        {
            DataTable dttValidacoes = new DataTable();
            dttValidacoes = modSQL.Fill("SELECT val_titulo_usuario FROM TbValidacoes");

            List<SelectListItem> lista = new List<SelectListItem>();
            foreach (DataRow dtrLinha in dttValidacoes.Rows)
            {
                lista.Add(new SelectListItem { Value = dtrLinha["val_titulo_usuario"].ToString(), Text = dtrLinha["val_titulo_usuario"].ToString() });
            }
            return lista;
        }

        [HttpGet]
        public string ExportarDIVE(string txtDocE, string Motivo, string Situacao, string txtNumeroPO, string txtCodigoComprador, string txtFornecedor, string txtDataEmissaoDe, string txtDataEmissaoAte, string txtDataDivergenciaDe, string txtDataDivergenciaAte, string arrDivergencia)
        {
            Boolean varCondPag = false;
            Boolean varRemFinal = false;
            Boolean varAvisoEmbarque = false;
            Boolean varAntped = false;
            Boolean varCnpjEmit = false;
            Boolean varQuant = false;
            Boolean varAprov = false;
            Boolean varDel = false;
            Boolean varVal = false;
            Boolean varPlanta = false;
            Boolean varNcm = false;
            Boolean varRe = false;

            if (arrDivergencia != null)
            {
                arrDivergencia = arrDivergencia.TrimEnd(';');
                string[] tiposDivergencias = arrDivergencia.Split(';');
                for (int i = 0; i < tiposDivergencias.Length; i++)
                {
                    if (tiposDivergencias[i].ToString() == "CONDIÇÃO DE PAGAMENTO")
                    {
                        varCondPag = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "REMESSA FINAL")
                    {
                        varRemFinal = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "AVISO DE EMBARQUE")
                    {
                        varAvisoEmbarque = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "ANTECIPAÇÃO DO PEDIDO")
                    {
                        varAntped = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "CNPJ EMITENTE")
                    {
                        varCnpjEmit = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "QUANTIDADE")
                    {
                        varQuant = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "APROVADO")
                    {
                        varAprov = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "DELETADO")
                    {
                        varDel = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "VALOR")
                    {
                        varVal = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "PLANTA")
                    {
                        varPlanta = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "NCM")
                    {
                        varNcm = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "RE")
                    {
                        varRe = true;
                    }
                }
            }
            BLDivergencias objBLNotaFiscal = new BLDivergencias();


            DataTable dt = objBLNotaFiscal.GetByFilter(
                DataLogInicial: txtDataDivergenciaDe == string.Empty ? "/  /" : txtDataDivergenciaDe,
                DataLogFinal: txtDataDivergenciaAte == string.Empty ? "/  /" : txtDataDivergenciaAte,
                DataNfInicial: txtDataEmissaoDe == string.Empty ? "/  /" : txtDataEmissaoDe,
                DataNfFinal: txtDataEmissaoAte == string.Empty ? "/  /" : txtDataEmissaoAte,

                NumeroNF: txtDocE,
                Situacao: Situacao == string.Empty ? "(TODOS)" : Situacao,
                Motivo: Motivo == string.Empty ? "(TODOS)" : Motivo,
                PurchaseOrder: txtNumeroPO,
                CodComprador: txtCodigoComprador,
                Fornecedor: txtFornecedor,
                CondicaoPagamento: varCondPag,
                RemessaFinal: varRemFinal,
                AvisoEmbarque: varAvisoEmbarque,
                AntecipacaoPedido: varAntped,
                CnpjEmitente: varCnpjEmit,
                Quantidade: varQuant,
                Aprovado: varAprov,
                Deletado: varDel,
                Valor: varVal,
                Planta: varPlanta,
                Ncm: varNcm,
                Re: varRe);


            string fileName = "DIVE - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
        }

        [HttpGet]
        public PartialViewResult GetDataDivergencia(string txtDocE, string Motivo, string Situacao, string txtNumeroPO, string txtCodigoComprador, string txtFornecedor, string txtDataEmissaoDe, string txtDataEmissaoAte, string txtDataDivergenciaDe, string txtDataDivergenciaAte, string arrDivergencia, string cboPrioridade, string cboOrigem)
        {

            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("DIVE", Uteis.LogonName()) == false)
                return null;


            //Cria uma cookie que armazena os filtros da pesquisa
            HttpCookie cookie = new HttpCookie("GetDataDivergencia");
            cookie.Value = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}", txtDocE, Motivo, Situacao, txtNumeroPO, txtCodigoComprador, txtFornecedor, txtDataEmissaoDe, txtDataEmissaoAte, txtDataDivergenciaDe, txtDataDivergenciaAte, arrDivergencia);
            Response.Cookies.Add(cookie);

            Boolean varCondPag = false;
            Boolean varRemFinal = false;
            Boolean varAvisoEmbarque = false;
            Boolean varAntped = false;
            Boolean varCnpjEmit = false;
            Boolean varQuant = false;
            Boolean varAprov = false;
            Boolean varDel = false;
            Boolean varVal = false;
            Boolean varPlanta = false;
            Boolean varNcm = false;
            Boolean varRe = false;

            if (arrDivergencia != null)
            {
                arrDivergencia = arrDivergencia.TrimEnd(';');
                string[] tiposDivergencias = arrDivergencia.Split(';');
                for (int i = 0; i < tiposDivergencias.Length; i++)
                {
                    if (tiposDivergencias[i].ToString() == "CONDIÇÃO DE PAGAMENTO")
                    {
                        varCondPag = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "REMESSA FINAL")
                    {
                        varRemFinal = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "AVISO DE EMBARQUE")
                    {
                        varAvisoEmbarque = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "ANTECIPAÇÃO DO PEDIDO")
                    {
                        varAntped = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "CNPJ EMITENTE")
                    {
                        varCnpjEmit = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "QUANTIDADE")
                    {
                        varQuant = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "APROVADO")
                    {
                        varAprov = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "DELETADO")
                    {
                        varDel = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "VALOR")
                    {
                        varVal = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "PLANTA")
                    {
                        varPlanta = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "NCM")
                    {
                        varNcm = true;
                    }
                    else if (tiposDivergencias[i].ToString() == "RE")
                    {
                        varRe = true;
                    }
                }
            }
            BLDivergencias objBLDivergencias = new BLDivergencias();

            DataTable dt = objBLDivergencias.GetByFilter(
                DataLogInicial: txtDataDivergenciaDe == string.Empty ? "/  /" : txtDataDivergenciaDe,
                DataLogFinal: txtDataDivergenciaAte == string.Empty ? "/  /" : txtDataDivergenciaAte,
                DataNfInicial: txtDataEmissaoDe == string.Empty ? "/  /" : txtDataEmissaoDe,
                DataNfFinal: txtDataEmissaoAte == string.Empty ? "/  /" : txtDataEmissaoAte,

                NumeroNF: txtDocE,
                Situacao: Situacao == string.Empty ? "(TODOS)" : Situacao,
                Motivo: Motivo == string.Empty ? "(TODOS)" : Motivo,
                PurchaseOrder: txtNumeroPO,
                CodComprador: txtCodigoComprador,
                Fornecedor: txtFornecedor,
                CondicaoPagamento: varCondPag,
                RemessaFinal: varRemFinal,
                AvisoEmbarque: varAvisoEmbarque,
                AntecipacaoPedido: varAntped,
                CnpjEmitente: varCnpjEmit,
                Quantidade: varQuant,
                Aprovado: varAprov,
                Deletado: varDel,
                Valor: varVal,
                Planta: varPlanta,
                Ncm: varNcm,
                Re: varRe);

            //monta a consuta com as ocorrencias das divergencias
            var NFChave = dt.Rows.OfType<DataRow>().Select(x => x["NFEID"].ToString()).Distinct().ToList();

            var ocorrencias = new DLOcorrencias().GetListagem(NFChave).OrderByDescending(x => x.Data).ToList();

            if (cboOrigem != null && cboPrioridade != null)
            {
                if (cboOrigem.ToUpper() == "FISCAL" || cboPrioridade.ToUpper() == "ALTA")
                {
                    dt.Rows.Clear();
                }
                if (cboOrigem.ToUpper() == "MATCHING" || cboPrioridade.ToUpper() == "NORMAL")
                {
                    ocorrencias = new List<OcorrenciasListagemViewModel>();
                }
            }

            ViewBag.Lista = ocorrencias;
            return PartialView("ViewDive", dt);
        }

        [HttpGet]
        public string VerificarDIVE(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            modNF mod = new modNF();
            string strMensagem = "";
            mod = objBLNotaFiscal.Validar(pIdNfe, string.Empty, Uteis.LogonName(), ref strMensagem);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, { "mensagem", strMensagem } });
        }

        [HttpPost]
        public string ReassociarEmMassa(string pIdNfe)
        {
            Object objRetorno;
            SqlDataReader vObjDR = null;
            modVerificar vObjModVerificar = new modVerificar();
            try
            {
                //Recupera parametro para avaliar quais notas podem ser reassociadas
                objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'REASSOCIAR_COMPRAS'");

                //Recupera somente as notas que atendem aos parametros
                modSQL.CommandText = "SP_GET_NFEID_ASSOCIACAO_MASSA '" + pIdNfe + "','" + objRetorno.ToString() + "'";

                vObjDR = modSQL.ExecuteReader(modSQL.CommandText, modSQL.connectionString);
                if (vObjDR.HasRows)
                {
                    while (vObjDR.Read())
                    {
                        modSQL.ExecuteNonQuery("UPDATE TBNFE SET REPROCESSAR = 'S' WHERE NFEID = '" + vObjDR.GetString(0) + "'");
                        vObjModVerificar.RegistrarLog(modVerificar.TipoProcessamento.Aplicacao, null, "DOCUMENTO MARCADO PARA REPROCESSAMENTO", "Documento " + vObjDR.GetString(0) + " marcado para reassociação via Portal VNF");
                        BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.VerificarDocumento, "Solicitou uma nova associação do documento " + vObjDR.GetString(0) + ".", vObjDR.GetString(0));
                    }

                }

                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "true" }, { "mensagem", "" } });
            }
            catch (Exception EX)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "false" }, { "mensagem", EX.Message } });
            }
            finally
            {
                if (vObjDR != null)
                {
                    try
                    {
                        vObjDR.Close();
                    }
                    catch { }
                    vObjDR = null;
                }
            }
        }

        [HttpGet]
        public string CancelarDIVE(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            var retorno = objBLNotaFiscal.Cancelar(pIdNfe, Uteis.LogonName(), false);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, { "data", retorno.ToString() } });
        }

        [HttpGet]
        public string DesfazerCancelamento(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            objBLNotaFiscal.DesfazerCancelamento(pIdNfe);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" } });
        }

        [HttpPost]
        public JsonResult AnularDIVE(string NfeId, int CodLog, string Justificativa, string Detalhe)
        {
            BLDivergencias objBLDivergencia = new BLDivergencias();
            string retorno = string.Empty;
            try
            {
                retorno = objBLDivergencia.Anular(NfeId, CodLog, Justificativa, Detalhe, null, null, null);//, "COMP");

                if (!string.IsNullOrEmpty(retorno))
                    RedirectToAction("User", "Login");

                return Json(new { result = "ok", data = retorno.ToString() });
            }
            catch (AccessViolationException avex)
            {
                retorno = avex.Message;
                return Json(new { result = "Error", data = retorno.ToString() });
            }
            catch (Exception)
            {
                retorno = "Ocorreu um erro na execução da chamada. ";
                return Json(new { result = "Error", data = retorno.ToString() });
            }
        }

        [HttpGet]
        public ActionResult DIVE()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("DIVE", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|DIVE";

            object objComprador = modSQL.ExecuteScalar("select isnull(codcom, '') from TbCOM inner join TbUsuario on email = usuEmail where usucodusu = '" + Uteis.LogonName() + "'");
            ViewBag.Comprador = objComprador == null ? "" : objComprador.ToString();

            ViewBag.UnidadeMetso = CarregarUnidadeMetso();
            ViewBag.Situacao = CarregarSituacao();
            ViewBag.Motivo = CarregarMotivo();
            ViewBag.ddlTipoDivergencia = ddlTipoDivergencia();

            return View();
        }

        [HttpGet]
        public ActionResult VerifVisaoCompleta(string id)
        {
            DLComparacao cDal = new DLComparacao();
            DateTime datComparacao = cDal.GetDataComparacao(id);

            string strValidacao = "";
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            strValidacao = objBLNotaFiscal.GetTabelaValidacoes(datComparacao, id);

            DLLogApplication lDal = new DLLogApplication();
            ViewBag.Validacao = strValidacao;
            ViewBag.LogApplication = lDal.GetByNFEID(id);
            ViewBag.NFEID = id;
            ViewBag.Resumo = lDal.GetResumo(id);

            DLImportacaoNotaFiscal objImportacaoNotafiscal = new DLImportacaoNotaFiscal();
            var obj = objImportacaoNotafiscal.getImportacaoNotaByChaveAcesso(id);

            if (obj != null)
                ViewBag.TipoDocumentoPortalServico = obj.IdNotaFiscal.ToString();
            else
                ViewBag.TipoDocumentoPortalServico = "";

            return View();
        }

        [HttpGet]
        public string LoadDataInfoVNFE(String pDocE)
        {
            BLNotaFiscal objBLNF = new BLNotaFiscal();
            modNF objNF = objBLNF.GetByID(pDocE, true);

            //Caso seja uma nota manual, retorna 
            if (objNF.VNF_NOTA_MANUAL_J1B1N)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { {"IsNotaManualJ1B1N", objNF.VNF_NOTA_MANUAL_J1B1N },
                                                                                          {"NFType", objNF.VNF_J1B1N_NFTYPE}});
            }


            bool PodeModificar = true;
            bool vBlnSituacao_Ok = false;
            bool vBlnIntegracaoConcluida = false;
            string Situacao_Ok = string.Empty;
            string IntegracaoConcluida = string.Empty;

            //---> SE TODOS OS ITENS DA NOTA FISCAL FOREM IRRELEVANTES, NÃO DEIXAR ENVIAR PARA O SAP
            bool EnviarSAP = true;
            if (objNF.ITENS_NF.Where(x => x.VNF_ITEM_VALIDO == "X").Count() == objNF.ITENS_NF.Count && !(objNF.VNF_TIPO_DOCUMENTO == modNF.tipo_doc_cte && objNF.VNF_CLASSIFICACAO == modNF.tipo_cte_debito_posterior))
                EnviarSAP = false;

            //---> MONTA A TABELA COM O LOG DO DOCUMENTO
            DLLogApplication objDLLogApplication = new DLLogApplication();
            string strLog = objDLLogApplication.GetLogStringByNfeId(objNF.VNF_CHAVE_ACESSO);

            //---> BUSCA A SITUACAO DA NOTA FISCAL E STATUS DA INTEGRAÇÃO DO SAP            
            string IsRecusada = objNF.VNF_STATUS == "RECUSADA" ? "S" : "N";
            string strStatusIntegracao = objBLNF.GetStatusIntegracao(objNF.VNF_CHAVE_ACESSO);

            //---> MONTA A TABELA DE ACORDO COM O TIPO DO DOCUMENTO
            string strTabela = "";
            string strTipoMiro = "";
            string strCabecalho = "";
            string strMotivoNaoEnvioSap = "";

            if (objNF.VNF_TIPO_DOCUMENTO == modNF.tipo_doc_cte && objNF.VNF_CLASSIFICACAO == modNF.tipo_cte_debito_posterior)
                strTabela = GetTabelaDebitoPosterior(objNF, ref EnviarSAP, ref strMotivoNaoEnvioSap, ref strTipoMiro, ref strCabecalho);
            else
                strTabela = GetTabelaNotaFiscal(objNF, ref EnviarSAP, ref strMotivoNaoEnvioSap, ref strTipoMiro);

            //Verifica se o XML da nota/ct-e estão em condições válidas para serem modificadas e verifica se já foi integrado e se sua
            //situação é válida
            if (!objBLNF.PodeModificar(objNF.VNF_CHAVE_ACESSO, ref vBlnSituacao_Ok, ref vBlnIntegracaoConcluida))
            {
                PodeModificar = false;
            }
            Situacao_Ok = vBlnSituacao_Ok.ToString();
            IntegracaoConcluida = vBlnIntegracaoConcluida.ToString();

            //Verifica se o modelo de documento deve ou não habilidar a opção de impressão de SLIP

            bool ImprimirSLIP = objBLNF.ImprimirSLIP(objNF.VNF_TIPO_DOCUMENTO);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {{ "result", "Ok" },
                                                                                     { "data", strTabela },
                                                                                     { "cabecalho", strCabecalho },
                                                                                     { "recusada" , IsRecusada},
                                                                                     { "EnviadaIP", false },
                                                                                     { "EnviarSap", EnviarSAP },
                                                                                     { "TipoMiro", strTipoMiro },
                                                                                     { "StatusIntegracao", strStatusIntegracao },
                                                                                     { "StatusNF", objNF.VNF_STATUS },
                                                                                     { "Log", strLog },
                                                                                     { "MotivoNaoEnvioSap", strMotivoNaoEnvioSap },
                                                                                     { "PodeModificar", PodeModificar },
                                                                                     { "Situacao_Ok", bool.Parse(Situacao_Ok) },
                                                                                     { "IntegracaoConcluida", bool.Parse(IntegracaoConcluida) },
                                                                                     { "ImprimirSLIP", ImprimirSLIP },
                                                                                     { "IsNotaManualJ1B1N", objNF.VNF_NOTA_MANUAL_J1B1N }});
        }

        [HttpGet]
        public string EnviarSap(string SapUser, string SapPassword, string pIdNfe, string pTipoMiro, string pStrImprimirSLIP)
        {
            Uteis.AddCookieValue("UsuarioSAP_VNF", SapUser);
            Uteis.AddCookieValue("SenhaSAP_VNF", SapPassword);

            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();

            modNF objNotaFiscal = objBLNotaFiscal.GetByID(pIdNfe, true);
            string strRetorno = string.Empty;

            if (objNotaFiscal.VNF_TIPO_DOCUMENTO == modNF.tipo_doc_cte && objNotaFiscal.VNF_CLASSIFICACAO == modNF.tipo_cte_debito_posterior)
            {
                SAP_RFC.SubsequentDebitReturn objDebPosteriorRetorno = objBLNotaFiscal.EnviarSapDebitoPosterior(SapUser, SapPassword, true, objNotaFiscal);
                if (String.IsNullOrEmpty(objDebPosteriorRetorno.DOCUMENT_NUMBER) && objDebPosteriorRetorno.SapReturn.BapiMessage.Count > 0)
                    strRetorno = objDebPosteriorRetorno.SapReturn.BapiMessage.FirstOrDefault().MESSAGE;
                else if (String.IsNullOrEmpty(objDebPosteriorRetorno.DOCUMENT_NUMBER))
                    strRetorno = "Documento não foi registrado no SAP. Nenhuma informação adicional foi encontrada.";
            }
            else
            {
                strRetorno = objBLNotaFiscal.EnviarSapPedido(SapUser, SapPassword, pIdNfe, pTipoMiro, bool.Parse(pStrImprimirSLIP));

                //********************************************************************************************************
                //Atualiza o Status no portal de serviço qdo o status de integracao no SAP estiver Concluido.
                //No portal de serviço o status fica como 'Registro Fiscal concluído'
                DB context = new DB();
                if (objNotaFiscal.VNF_TIPO_DOCUMENTO == modNF.tipo_NFS || objNotaFiscal.VNF_TIPO_DOCUMENTO == modNF.tipo_TLC || objNotaFiscal.VNF_TIPO_DOCUMENTO == modNF.tipo_FAT)
                {
                    if (objNotaFiscal.VNF_CHAVE_ACESSO != "")
                    {
                        SqlParameter param1 = new SqlParameter("@ChaveAcesso", objNotaFiscal.VNF_CHAVE_ACESSO);
                        SqlParameter param2 = new SqlParameter("@DataAtual", DateTime.Now);
                        context.Database.SqlQuery<string>("sp_AtualizaStatusPortalServico @ChaveAcesso, @DataAtual", param1, param2).Single();
                    }
                }
                //********************************************************************************************************
            }

            if (strRetorno.Trim().ToUpper() == "O nome ou a senha não está correto (repetir o logon)".Trim().ToUpper())
            {
                Uteis.RemoveCookie("UsuarioSAP_VNF");
                Uteis.RemoveCookie("SenhaSAP_VNF");
            }

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, { "mensagem", strRetorno } });
        }

        [HttpGet]
        public string MaterialRecebido(string pIdNfe)
        {
            BLNotaFiscal objNotaFiscal = new BLNotaFiscal();

            if (objNotaFiscal.PodeModificar(pIdNfe))
            {
                objNotaFiscal.MaterialRecebido(pIdNfe);
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "true" } });
            }
            else
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "false" } });
            }
        }

        [HttpGet]
        public string EstornarMaterialRecebido(string pIdNfe)
        {
            BLNotaFiscal objNotaFiscal = new BLNotaFiscal();

            if (objNotaFiscal.PodeModificar(pIdNfe))
            {
                objNotaFiscal.EstornarMaterialRecebido(pIdNfe);
                //return RedirectToAction("Edit/" + pIdNfe);
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "true" } });
            }
            else
            {
                //return View();
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "false" } });
            }
        }

        [HttpGet]
        public string StatusIntegracao(string pIdNfe)
        {
            BLNotaFiscal objNotaFiscal = new BLNotaFiscal();
            return objNotaFiscal.GetStatusIntegracao(pIdNfe);
        }

        [HttpGet]
        public string EnviarIP(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            string retorno = string.Empty;
            retorno = objBLNotaFiscal.EnviarIP(pIdNfe);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
                        { "data", retorno.ToString()}});
        }

        [HttpGet]
        public string RegistroManual(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            string retorno = string.Empty;
            retorno = objBLNotaFiscal.RegistroManual(pIdNfe);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
                                                                                      { "data", retorno.ToString()}});
        }

        [HttpGet]
        public string AlterarRateio(string pIdNfe, bool ratear)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            string retorno = string.Empty;
            retorno = objBLNotaFiscal.AlterarRateio(pIdNfe, ratear);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
                                                                                      { "data", retorno.ToString()}});
        }

        #region Ocorrencias
        [HttpGet]
        public string GetMotivoOcorrencia()
        {
            string html = "<option></option>";
            var Motivos = new DLMotivoCorrecao().Get().OrderBy(x => x.Titulo);
            foreach (MotivoCorrecao m in Motivos)
            {
                html += "<option value='" + m.IdMotivoCorrecao.ToString() + "'>" + m.Titulo + "</option>";
            }
            return html;
        }

        [HttpGet]
        public string GetResponsaveis()
        {
            string html = "<option></option>";
            var Responsaveis = new DLUsers().Get().OrderBy(x => x.usunomusu);
            foreach (var m in Responsaveis)
            {
                html += "<option value='" + m.usucodusu + "'>" + m.usunomusu + "</option>";
            }
            return html;
        }

        /// <summary>
        /// Retorna os históricos das ocorrências
        /// </summary>
        /// <param name="IdOcorrencia"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 22/05/2018 - CR00008351 - Ajuste para apresentação do usuário sem AD</example>
        [HttpGet]
        public string GetOcorrenciaHistorico(int IdOcorrencia)
        {
            string html = "";
            var oc = new DLOcorrenciasComentarios().GetComentarios(IdOcorrencia);
            //foreach (OcorrenciasComentarios i in oc)
            foreach (OcorrenciasComentariosViewModel i in oc)
            {
                html += "<tr>";
                html += "<td>" + Convert.ToDateTime(i.Data).ToString() + "</td>";
                //Marcio Spinosa - 22/05/2018 - CR00008351
                //html += "<td>" + Uteis.GetUserInfoBySamId(i.Usuario)[0] + "</td>";
                html += "<td>" + i.Usuario + "</td>";
                //html += "<td>" +  new DLUsers().GetNameByLogonName(i.Usuario) + "</td>";
                //Marcio Spinosa - 22/05/2018 - CR00008351 - Fim
                html += "<td>" + i.Comentario + "</td>";
                if (i.Anexo != null)
                {
                    html += "<td><a href='/Compras/DownloadAnexoOcorrencia/" + i.IdOcorrenciaComentario.ToString() + "' ><i title=\"Download\" onclick=\"DownloadAnexo();\" class=\"fa fa-download\"></i></a></td>";
                }
                else
                {
                    html += "<td></td>";
                }
                html += "</tr>";
            }
            return html;
        }



        [HttpPost]
        public void FinalizaOcorrencia(int IdOcorrencia, string Comentario)
        {
            DLOcorrenciasComentarios cDal = new DLOcorrenciasComentarios();
            DLOcorrencias dal = new DLOcorrencias();
            var ol = new DLOcorrencias().GetByNFEID(IdOcorrencia);
            Ocorrencias oc = dal.GetByID(ol.IdOcorrencia);
            oc.Status = "Finalizado";
            dal.Update(oc);
            dal.Save();

            OcorrenciasComentarios oci = new OcorrenciasComentarios();
            oci.IdOcorrencia = oc.IdOcorrencia;
            oci.Comentario = Comentario;
            oci.Data = DateTime.Now;
            oci.Usuario = Uteis.LogonName();
            for (int i = 0; i < Request.Files.Count; i++)
            {
                if (Request.Files[i].ContentLength > 0)
                {
                    oci.Anexo = Request.Files[i].InputStream.ToByteArray();
                    oci.AnexoExtensao = Request.Files[i].ContentType;
                    oci.AnexoNome = Request.Files[i].FileName.Remove(0, Request.Files[i].FileName.LastIndexOf("\\") + 1);
                }
                else
                {
                    oci.Anexo = null;
                    oci.AnexoNome = "";
                    oci.AnexoExtensao = "";
                }
            }
            cDal.Insert(oci);
            cDal.Save();
        }

        [HttpGet]
        public JsonResult GetOcorrencia(int IdOcorrencia)
        {
            bool hasAcessoOcorrencia = (new BLAcessos()).ConsultaAcesso("OCOR", Uteis.LogonName());

            OcorrenciasListagemViewModel oc = new DLOcorrencias().GetByNFEID(IdOcorrencia);
            string MotivoCorrecao = oc.Motivo;
            string DataEsperada = Convert.ToDateTime(oc.DataEsperada).ToShortDateString();
            string DataRecebimento = Convert.ToDateTime(oc.DataRecebimento).ToShortDateString();
            string Status = oc.Status;
            string Usuario = Uteis.LogonName();
            string Responsavel = oc.Responsavel;
            //cria campo que verifica se a pessoa pode finalizar a ocorrencia pelo acesso FINO
            string FinalizaOcorrencia = hasAcessoOcorrencia ? "S" : "N";
            //cria campo que verifica se a pessoa pode alterar ocorrencia em qualquer status pelo acesso OCOR
            string AlteraQualquerOcorrencia = hasAcessoOcorrencia ? "S" : "";

            if (oc.Status == "Finalizado")
            {
                FinalizaOcorrencia = "N";
            }

            var Ocorrencia = new
            {
                MotivoCorrecao,
                DataEsperada,
                DataRecebimento,
                Status,
                Usuario,
                Responsavel,
                FinalizaOcorrencia,
                AlteraQualquerOcorrencia
            };
            return Json(Ocorrencia, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Retorna o responsavel pelo motivo da correção
        /// </summary>
        /// <param name="idMotivo"></param>
        /// <param name="CodFor"></param>
        /// <param name="CodCom"></param>
        /// <param name="Responsavel"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 23/05/2018 - CR00008351 - Ajuste para utilizar o usuário do Banco de dados sem AD</example>
        [HttpGet]
        public JsonResult GetResponsavelMotivoCorrecao(int idMotivo, string CodFor, string CodCom, string Responsavel)
        {
            DLMotivoCorrecao mDal = new DLMotivoCorrecao();
            var Motivo = mDal.GetByID(idMotivo);
            string Email = "";
            //Verifica se pesquisa o email do responsavel do motivo ou se foi informado um responsavel na tela.
            string xResponsavel = String.IsNullOrEmpty(Responsavel) ? Motivo.Responsavel : Responsavel;
            if (xResponsavel == "Comprador")
            {
                Email = mDal.GetEmailComprador(CodCom);
            }
            else if (xResponsavel == "Grupo")
            {
                DLGroups gDal = new DLGroups();
                int id = Motivo.IdGroup == null ? 0 : Convert.ToInt32(Motivo.IdGroup);
                Groups grupo = gDal.GetByID(id);
                foreach (var i in grupo.GroupUsers)
                {
                    //Email += Uteis.GetUserInfoBySamId(i.LoginName)[1] + ", ";
                    //Marcio Spinosa - 23/05/2018 - CR00008351
                    DLUsers objDLUsers = new DLUsers();
                    Email += objDLUsers.getDadosByLogon(i.LoginName)[2] + ", ";
                    //Marcio Spinosa - 23/05/2018 - CR00008351 - Fim
                }
            }
            else if (xResponsavel == "Fornecedor")
            {
                Email = mDal.GetEmailFornecedor(CodFor);
            }

            var oReturn = new
            {
                Responsavel = xResponsavel,
                Email = Email
            };
            return Json(oReturn, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult DownloadAnexoOcorrencia(int Id)
        {
            DLOcorrenciasComentarios dal = new DLOcorrenciasComentarios();
            OcorrenciasComentarios oc = dal.GetByID(Id);

            return File(oc.Anexo, oc.AnexoExtensao, Server.UrlEncode(oc.AnexoNome));
        }

        /// <summary>
        /// Método para criação das ocorrências
        /// </summary>
        /// <param name="NFEID"></param>
        /// <param name="Motivo"></param>
        /// <param name="Comentario"></param>
        /// <param name="Responsavel"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 28/05/2018 - CR00008351 - Ajuste para consultar o usuário no AD</example>
        [HttpPost]
        public JsonResult CriaOcorrencia(string NFEID, int Motivo, string Comentario, string Responsavel)
        {
            DLOcorrencias oDal = new DLOcorrencias();
            DLOcorrenciasComentarios cDal = new DLOcorrenciasComentarios();
            Ocorrencias o = new Ocorrencias();
            DLUsers objDLUser = new DLUsers();
            o.NFEID = NFEID;
            o.IdMotivoCorrecao = Motivo;
            o.DataEsperada = DateTime.Now.AddHoursWorkingDays(8, null);
            o.DataRecebimento = DateTime.Now;
            if (String.IsNullOrEmpty(Responsavel) || Responsavel == "undefined")
            {
                o.Responsavel = new DLMotivoCorrecao().GetByID(Motivo).Responsavel;
                if (o.Responsavel == "Comprador")
                {
                    string strEmailComprador = oDal.GetCompradorResponsavel(o.NFEID);
                    //Marcio Spinosa - 28/05/2018 - CR00008351
                    //o.Responsavel = Uteis.GetUserInfoByMail(strEmailComprador)[2];
                    o.Responsavel = objDLUser.GetNameByMail(strEmailComprador);
                    //Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
                }
            }
            else
            {
                o.Responsavel = Responsavel;
            }

            o.Status = "Pendente";
            oDal.Insert(o);
            oDal.Save();


            OcorrenciasComentarios oc = new OcorrenciasComentarios();
            oc.IdOcorrencia = o.IdOcorrencia;
            oc.Comentario = Comentario;
            oc.Data = DateTime.Now;
            oc.Usuario = Uteis.LogonName();
            for (int i = 0; i < Request.Files.Count; i++)
            {
                if (Request.Files[i].ContentLength > 0)
                {
                    oc.Anexo = Request.Files[i].InputStream.ToByteArray();
                    oc.AnexoExtensao = Request.Files[i].ContentType;
                    oc.AnexoNome = Request.Files[i].FileName.Remove(0, Request.Files[i].FileName.LastIndexOf("\\") + 1);

                    DLOcorrenciasAnexos dal = new DLOcorrenciasAnexos();
                    TbDOC_CAB_ANEXOS a = new TbDOC_CAB_ANEXOS();
                    a.ANEXO = oc.Anexo;
                    a.EXTENSAO = oc.AnexoExtensao;
                    a.NOME = oc.AnexoNome;
                    a.NFEID = NFEID;
                    dal.Insert(a);
                    dal.Save();

                }
                else
                {
                    oc.Anexo = null;
                    oc.AnexoNome = "";
                    oc.AnexoExtensao = "";
                }

            }
            cDal.Insert(oc);
            cDal.Save();

            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            var nf = objBLNotaFiscal.GetByID(o.NFEID, false);
            string Body = "Uma ocorrência foi encaminhada aos seus cuidados.<br />";
            Body += "NF: " + nf.NF_IDE_NNF + "<br />";
            Body += "Fornecedor: " + nf.NF_EMIT_XNOME + "<br />";
            Body += "Prioridade: Alta <br />";
            Body += "Comentário: " + Comentario + "<br />";
            string strUrl = Request.Url.GetLeftPart(UriPartial.Authority) + "/VNF/Ocorrencias/index/MinhaAtividade";
            //string strUrl = Request.Url.GetLeftPart(UriPartial.Authority) + "/" + Uteis.GetSettingsValue<string>("Prefix") + "/Ocorrencias/index/MinhaAtividade";

            //Marcio Spinosa - 23/05/2018 - CR00008351
            //string EmailFrom = Uteis.GetUserInfoBySamId(Uteis.LogonName())[1];
            DLUsers objDLuser = new DLUsers();
            string[] arrUsersInfo = objDLuser.getDadosByLogon(Uteis.LogonName());
            //string msgBody = Uteis.GetMailBody.StandardTemplate("VNF", "Validador de Notas Fiscais", Uteis.GetUserNameBySamId(o.Responsavel), Body, strUrl);
            string msgBody = Uteis.GetMailBody.StandardTemplate("VNF", "Validador de Notas Fiscais", objDLuser.getDadosByLogon(o.Responsavel)[1], Body, strUrl);
            arrUsersInfo = objDLuser.getDadosByLogon(o.Responsavel);
            //string EmailTo = Uteis.GetUserInfoBySamId(o.Responsavel)[1];
            string EmailTo = arrUsersInfo[2];
            Uteis.SendMailStream(arrUsersInfo[2], EmailTo, "", "", "Nova Ocorrência", msgBody, null);

            BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.CriarOcorrencia, "Criou uma ocorrência para a " + objBLNotaFiscal.GetNumeroDocumento(o.NFEID), o.NFEID);
            //Marcio Spinosa - 23/05/2018 - CR00008351 - Fim
            var oReturn = new
            {
                OcorrenciaRealizada = true,
            };
            return Json(oReturn, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Encaminha o email da ocorrência
        /// </summary>
        /// <param name="IdOcorrencia"></param>
        /// <param name="IdMotivoCorrecao"></param>
        /// <param name="Responsavel"></param>
        /// <param name="Email"></param>
        /// <param name="Comentario"></param>
        /// <example>Marcio Spinosa - 23/05/2018 - CR00008351 - Ajuste para não utilizar o usuário do AD</example>
        [HttpPost]
        public void EncaminhaOcorrencia(int IdOcorrencia, int IdMotivoCorrecao, string Responsavel, string Email, string Comentario)
        {
            DLOcorrencias oDal = new DLOcorrencias();
            Ocorrencias oc = oDal.GetByID(IdOcorrencia);
            oc.IdMotivoCorrecao = IdMotivoCorrecao;
            oc.DataRecebimento = DateTime.Now;
            oc.DataEsperada = DateTime.Now.AddHoursWorkingDays(8);
            oc.Responsavel = Responsavel;
            oDal.Update(oc);
            oDal.Save();

            DLOcorrenciasComentarios ocDal = new DLOcorrenciasComentarios();
            OcorrenciasComentarios oi = new OcorrenciasComentarios();
            oi.IdOcorrencia = oc.IdOcorrencia;
            oi.Comentario = Comentario;
            oi.Data = DateTime.Now;
            oi.Usuario = Uteis.LogonName();
            for (int i = 0; i < Request.Files.Count; i++)
            {
                if (Request.Files[i].ContentLength > 0)
                {
                    oi.Anexo = Request.Files[i].InputStream.ToByteArray();
                    oi.AnexoExtensao = Request.Files[i].ContentType;
                    oi.AnexoNome = Request.Files[i].FileName.Remove(0, Request.Files[i].FileName.LastIndexOf("\\") + 1);
                }
                else
                {
                    oi.Anexo = null;
                    oi.AnexoNome = "";
                    oi.AnexoExtensao = "";
                }
            }
            ocDal.Insert(oi);
            ocDal.Save();
            string Body = "Uma ocorrência foi encaminhada aos seus cuidados.<br />";
            Body += "NF: " + oc.NFEID + "<br />";
            Body += "Data de recebimento: " + oc.DataRecebimento.ToString() + "<br />";
            Body += "Data esperada: " + oc.DataEsperada.ToString() + "<br />";
            Body += "Motivo da ocorrência: " + oc.MotivoCorrecao.Titulo + "<br />";

            //Marcio Spinosa - 23/05/2018 - CR00008351
            //string EmailFrom = Uteis.GetUserInfoBySamId(Uteis.LogonName())[1];
            DLUsers objDLusers = new DLUsers();
            string EmailFrom = objDLusers.getDadosByLogon(Uteis.LogonName())[1];
            //Marcio Spinosa - 23/05/2018 - CR00008351 - Fim
            string msgBody = Uteis.GetMailBody.StandardTemplate("VNF", "Validador de Notas Fiscais", Uteis.LogonName(), Body, "");
            //Enviar Email
            string[] Emails = Email.Split(',');
            List<Uteis.FilesAttachStream> Anexos = new List<Uteis.FilesAttachStream>();
            Uteis.FilesAttachStream anexo = new Uteis.FilesAttachStream();
            if (oi.Anexo != null)
            {
                anexo.Attachment = new MemoryStream(oi.Anexo);
                Anexos.Add(anexo);
            }


            foreach (string s in Emails)
            {
                if (s.IsValidMail())
                {
                    Uteis.SendMailStream(EmailFrom, s, "", "", "Ocorrência encaminhada", msgBody, Anexos);
                }
            }


        }

        #endregion


        private string GetTabelaNotaFiscal(modNF objNF, ref bool EnviarSAP, ref string strMotivoNaoEnvioSap, ref string strTipoMiro)
        {
            BLNotaFiscal objBLNF = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();
            int indiceLinha = 0;
            string strLinha = "";
            string strDetalhe = "";
            decimal decQuantidade = 0;
            decimal decTotalComIpi = 0;
            string strComplementoTituloIpi = "";

            DataTable dttMigoMiro = new DataTable();
            DataTable dttMigoMensagens = new DataTable();
            string strNumeroDocMigo = "";
            string strNumeroDocMiro = "";
            string strMigoMensagens = "";
            string strCfop = "";

            DataTable dttMiroMensagens = new DataTable();
            string strMiroMensagens = "";
            strTipoMiro = "A";

            if (objNF.VNF_TIPO_DOCUMENTO == modNF.tipo_doc_cte && objNF.VNF_CLASSIFICACAO == modNF.tipo_cte_frete_pedido)
            {
                modNFItem objItemFretePedido = objNF.ITENS_NF.FirstOrDefault();
                objNF.ITENS_NF.Clear();
                objNF.ITENS_NF.Add(objItemFretePedido);
            }

            // --> VERIFICA SE É A NOTA FISCAL REFERENCIADA É REQUERIDA
            var objModVerificar = new modVerificar();
            string strTabelaNFRef = "";
            if (objNF.ITENS_NF.Count > 0)
            {
                if (objModVerificar.IsMandatoryNotaFiscalReferenciada(objNF.ITENS_NF.FirstOrDefault().NF_PROD_CFOP))
                {
                    strTabelaNFRef = @"
                            <table class=""display table table-responsive table-striped table-hover"" width=""25% !important"">
                                <tbody>
                                    <tr>
                                        <td>Nota fiscal referenciada nro <b>" + objNF.NF_NFREF_REFNNF + "-" + objNF.NF_NFREF_REFSerie + @"</b></td>
                                    </tr>
                                </tbody>
                            </table>";
                }
            }

            foreach (modNFItem itemNF in objNF.ITENS_NF)
            {
                strMigoMensagens = "";
                strMiroMensagens = "";
                strCfop = string.Empty;

                //--> A CFOP PARA FRETE PEDIDO DEVE SER A CFOP DE ENTRADA, PARA NFE, UTILIZAR A CFOP DEFINIDA PELO GETPO
                if (objNF.VNF_TIPO_DOCUMENTO == modNF.tipo_doc_cte && objNF.VNF_CLASSIFICACAO == modNF.tipo_cte_frete_pedido)
                    strCfop = itemNF.NF_PROD_CFOP;


                if (string.IsNullOrWhiteSpace(strCfop))
                {
                    if (itemNF.SAP_ITEM_DETAILS.SAP_ITEM_CFOP != null)
                    {
                        strCfop = objBLNF.GetCFOPEscrituracaoMIRO(itemNF.NF_PROD_CFOP, itemNF.SAP_ITEM_DETAILS.SAP_ITEM_CFOP.Replace("AA", ""));
                        if (!string.IsNullOrWhiteSpace(strCfop))
                        {
                            strCfop += "AA";
                        }

                    }
                }

                if (string.IsNullOrWhiteSpace(strCfop))
                {
                    strCfop = itemNF.SAP_ITEM_DETAILS.SAP_ITEM_CFOP;
                }


                strDetalhe = "<div>integração ainda não foi realizada</div>";
                if (strTipoMiro == "A" && itemNF.MDP_TIPO_MIRO != "A")
                    strTipoMiro = "M";

                //--> SE NÃO DETERMINOU O MODO/PROCESSO PARA ALGUM ITEM NÃO LIBERAR O ENVIO SAP
                if (itemNF.VNF_ITEM_VALIDO != "X" && String.IsNullOrEmpty(itemNF.MDP_MODO))
                    EnviarSAP = false;

                if (objNF.VNF_TIPO_DOCUMENTO == modNF.tipo_NFS || objNF.VNF_TIPO_DOCUMENTO == modNF.tipo_FAT || objNF.VNF_TIPO_DOCUMENTO == modNF.tipo_TLC)
                {
                    strTipoMiro = "M";
                    EnviarSAP = true;
                }

                if (String.IsNullOrEmpty(strComplementoTituloIpi) && itemNF.SAP_ITEM_DETAILS.TAX_SPLIT == "X")
                    strComplementoTituloIpi = " - 50%";

                //---> PROCURA AS INFORMAÇÕES DA MIGO E MIRO
                strNumeroDocMigo = "NENHUM DOCUMENTO CRIADO";
                strNumeroDocMiro = "NENHUM DOCUMENTO CRIADO";
                dttMigoMiro = modSQL.Fill("SELECT * FROM TbIntegracao WHERE nfeid = '" + objNF.VNF_CHAVE_ACESSO + "' and int_migo_nf_item_number = " + itemNF.NF_PROD_ITEM + " order by int_migo_mat_doc_number desc");
                if (dttMigoMiro.Rows.Count > 0)
                {
                    strNumeroDocMigo = dttMigoMiro.Rows[0]["INT_MIGO_MAT_DOC_NUMBER"].ToString().Trim();
                    strNumeroDocMiro = dttMigoMiro.Rows[0]["INT_MIRO_MAT_DOC_NUMBER"].ToString().Trim();
                    strNumeroDocMigo = string.IsNullOrEmpty(strNumeroDocMigo) ? "NENHUM DOCUMENTO CRIADO" : strNumeroDocMigo;
                    strNumeroDocMiro = string.IsNullOrEmpty(strNumeroDocMiro) ? "NENHUM DOCUMENTO CRIADO" : strNumeroDocMiro;
                }

                //---> PROCURA AS MENSAGENS DE RETORNO DO SAP RELACIONADA A MIGO
                dttMigoMensagens = modSQL.Fill("SELECT * FROM TbIntegracaoMensagens WHERE nfeid = '" + objNF.VNF_CHAVE_ACESSO + "' and sap_item_nota_fiscal = " + itemNF.NF_PROD_ITEM + " and sap_category = 'MIGO' ORDER BY sap_date_insert desc");
                if (dttMigoMensagens.Rows.Count == 0)
                {
                    strMigoMensagens = "<tr><td>---</td><td>---</td></tr>";
                }
                else
                {
                    foreach (DataRow dtrMigo in dttMigoMensagens.Rows)
                    {
                        strMigoMensagens += "<tr><td>" + Convert.ToDateTime(dtrMigo["sap_date_insert"].ToString()).ToString("dd/MM/yyyy HH:mm") + "</td><td>" + dtrMigo["sap_message"].ToString() + "</td></tr>";
                    }
                }

                //---> PROCURA AS MENSAGENS DE RETORNO DO SAP RELACIONADA A MIRO
                dttMiroMensagens = modSQL.Fill("SELECT * FROM TbIntegracaoMensagens WHERE nfeid = '" + objNF.VNF_CHAVE_ACESSO + "' and sap_item_nota_fiscal = " + itemNF.NF_PROD_ITEM + " and sap_category = 'MIRO' ORDER BY sap_date_insert desc");
                if (dttMiroMensagens.Rows.Count == 0)
                {
                    strMiroMensagens = "<tr><td>---</td><td>---</td></tr>";
                }
                else
                {
                    foreach (DataRow dtrMiro in dttMiroMensagens.Rows)
                    {
                        strMiroMensagens += "<tr><td>" + Convert.ToDateTime(dtrMiro["sap_date_insert"].ToString()).ToString("dd/MM/yyyy HH:mm") + "</td><td>" + dtrMiro["sap_message"].ToString() + "</td></tr>";
                    }
                }

                //---> ICMS                
                SAP_RFC.PurchaseOrderItemsTaxes objIcms = new SAP_RFC.PurchaseOrderItemsTaxes();
                if (itemNF.SAP_ITEM_DETAILS.ITEM_TAXES != null)
                {
                    foreach (SAP_RFC.PurchaseOrderItemsTaxes objTax in itemNF.SAP_ITEM_DETAILS.ITEM_TAXES)
                    {
                        if (objTax.TAX_NAME.ToUpper() == "ICMS")
                        {
                            objIcms = objTax;
                            break;
                        }
                    }
                }

                //---> IPI            
                SAP_RFC.PurchaseOrderItemsTaxes objIpi = new SAP_RFC.PurchaseOrderItemsTaxes();
                if (itemNF.SAP_ITEM_DETAILS.ITEM_TAXES != null)
                {
                    foreach (SAP_RFC.PurchaseOrderItemsTaxes objTax in itemNF.SAP_ITEM_DETAILS.ITEM_TAXES)
                    {
                        if (objTax.TAX_NAME.ToUpper() == "IPI")
                        {
                            objIpi = objTax;
                            break;
                        }
                    }
                }


                strDetalhe = @" <div id='accordionMigo" + indiceLinha.ToString() + @"' class='panel panel-default'>
                                    <div class='panel-heading'><h4 class='panel-title '><a data-toggle='collapse' data-parent='#accordionMigo" + indiceLinha.ToString() + @"' href='#collapseMigo" + indiceLinha.ToString() + @"' class='collapsed'>MIGO - " + strNumeroDocMigo + @"</a></h4></div>
                                    <div id='collapseMigo" + indiceLinha.ToString() + @"' class='panel-collapse collapse'><div class='panel-body padding0'><table class='table table-striped table-bordered table-hover table-click  margin0' width='100%'>
                                        <thead><tr><th width=""15%"">Data</th><th width=""85%"">Mensagem SAP</th></tr></thead>
                                        <tbody>" + strMigoMensagens + @"</tbody></table></div>
                                    </div>
                                </div>
                                <div id='accordionMiro" + indiceLinha.ToString() + @"' class='panel panel-default'>
                                    <div class='panel-heading'><h4 class='panel-title '><a data-toggle='collapse' data-parent='#accordionMiro" + indiceLinha.ToString() + @"' href='#collapseMiro" + indiceLinha.ToString() + @"' class='collapsed'>MIRO - " + strNumeroDocMiro + @"</a></h4></div>
                                    <div id='collapseMiro" + indiceLinha.ToString() + @"' class='panel-collapse collapse'><div class='panel-body padding0'><table class='table table-striped table-bordered table-hover table-click  margin0' width='100%'>
                                        <thead><tr><th width=""15%"">Data</th><th width=""85%"">Mensagem SAP</th></tr></thead>
                                        <tbody>" + strMiroMensagens + @"</tbody></table></div>
                                    </div>
                                </div>";

                //decQuantidade = (itemNF.NF_PROD_QCOM > 0 ? itemNF.NF_PROD_QCOM : itemNF.NF_PROD_QTRIB);
                decQuantidade = itemNF.NF_PROD_QCOM;
                decTotalComIpi = itemNF.SAP_ITEM_DETAILS.NF_TOTAL_ITEM;

                // --> SE O MODO PROCESSO ESTIVER MARCADO COMO "ENVIAR CÓDIGO DE IMPOSTO NA MIGO" 
                string taxCode = itemNF.SAP_ITEM_DETAILS.TAX_CODE;
                if (itemNF.MDP_ENVIAR_TAXCODE_MIGO)
                {
                    taxCode = modSQL.ExecuteScalar("SELECT TAX_CODE_REMESSA FROM TbTaxCode_MIGO WHERE TAX_CODE_FATURA = '" + taxCode + "'");
                }

                SAP_RFC.PurchaseOrderItemsAccount objAccount = itemNF.SAP_ITEM_DETAILS.ITEM_ACCOUNT == null ? new SAP_RFC.PurchaseOrderItemsAccount() : itemNF.SAP_ITEM_DETAILS.ITEM_ACCOUNT.FirstOrDefault();
                string strOrdem = !String.IsNullOrEmpty(objAccount.ORDER_NUMBER) ? objAccount.ORDER_NUMBER : !String.IsNullOrEmpty(objAccount.COST_CENTER) ? objAccount.COST_CENTER : "";
                strOrdem = strOrdem.TrimStart('0');


                if (objNF.NF_IDE_FINNFE == 2)
                {
                    //Marcio Spinosa - 30/07/2018 - CR00008351
                    //                    strLinha = @"<tr id='" + itemNF.NF_PROD_ITEM.ToString() + @"' class='" + itemNF.NF_PROD_ITEM.ToString() + @"'>
                    //                                <td><a data-toggle=""collapse"" data-parent=""#accordion"" href=""#detail" + indiceLinha.ToString() + @""" class=""collapsed""><i class=""fa fa-chevron-down""></i></a></td>
                    //                                <td id='tdCodigo_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? itemNF.NF_PROD_XPROD : String.IsNullOrEmpty(itemNF.SAP_ITEM_DETAILS.MATERIAL) ? itemNF.SAP_ITEM_DETAILS.PO_ITEM_SHORT_TEXT : itemNF.SAP_ITEM_DETAILS.MATERIAL) + @"</td>
                    //                                <td id='tdPlanta_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.PLANT + @"</td>
                    //                                <td id='tdOrdem_" + indiceLinha.ToString() + "'>" + strOrdem + @"</td>
                    //                                <td id='tdQtde_" + indiceLinha.ToString() + "'>" + decQuantidade.ToString("N2") + @"</td>
                    //                                <td id='tdNcm_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.NCM_CODE + @"</td>
                    //                                <td id='tdTaxCode_" + indiceLinha.ToString() + "'>" + taxCode + @"</td>
                    //                                <td id='tdCfop_" + indiceLinha.ToString() + "'>" + strCfop + @"</td>
                    //                                <td id='tdTipoNf_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? "" : itemNF.MDP_TIPO_NF) + @"</td>
                    //                                <td id='tdProcesso_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? "IRRELEVANTE" : itemNF.MDP_PROCESSO) + @"</td>
                    //                                <td id='tdIcmsValor_" + indiceLinha.ToString() + "'>" + objIcms.TAX_PERCENTAGE.ToString("N0") + "% - " + itemNF.NF_ICMS_VICMS.ToString("C2") + @"</td>
                    //                                <td id='tdIpi_" + indiceLinha.ToString() + "'>" + objIpi.TAX_PERCENTAGE.ToString("N0") + "% - " + itemNF.NF_IPI_VIPI.ToString("C2") + @"</td>
                    //                                <td id='tdPrecoNet_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE.ToString("C2") + @"</td>
                    //                                <td id='tdTotalImpostos_" + indiceLinha.ToString() + "'>" + itemNF.NF_PROD_VPROD.ToString("C2") + @"</td>
                    //                            </tr>
                    //                            <tr id=""detail" + indiceLinha.ToString() + @""" class=""collapse"" style=""padding:0px; margin:0px;"">
                    //                                <td colspan=""15"" class=""panel-group smart-accordion-default"" style=""padding:0px; margin:0px;"">" + strDetalhe + @"</td>
                    //                            </tr>
                    //                            ";
                    strLinha = @"<tr id='" + itemNF.NF_PROD_ITEM.ToString() + @"' class='" + itemNF.NF_PROD_ITEM.ToString() + @"'>
                                <td><a data-toggle=""collapse"" data-parent=""#accordion"" href=""#detail" + indiceLinha.ToString() + @""" class=""collapsed""><i class=""fa fa-chevron-down""></i></a></td>
                                <td id='tdCodigo_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? itemNF.NF_PROD_XPROD : String.IsNullOrEmpty(itemNF.SAP_ITEM_DETAILS.MATERIAL) ? itemNF.SAP_ITEM_DETAILS.PO_ITEM_SHORT_TEXT : itemNF.SAP_ITEM_DETAILS.MATERIAL) + @"</td>
                                <td id='tdPlanta_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.PLANT + @"</td>
                                <td id='tdOrdem_" + indiceLinha.ToString() + "'>" + strOrdem + @"</td>
                                <td id='tdQtde_" + indiceLinha.ToString() + "'>" + decQuantidade.ToString("N3") + @"</td>
                                <td id='tdNcm_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.NCM_CODE + @"</td>
                                <td id='tdTaxCode_" + indiceLinha.ToString() + "'>" + taxCode + @"</td>
                                <td id='tdCfop_" + indiceLinha.ToString() + "'>" + strCfop + @"</td>
                                <td id='tdTipoNf_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? "" : itemNF.MDP_TIPO_NF) + @"</td>
                                <td id='tdProcesso_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? "IRRELEVANTE" : itemNF.MDP_PROCESSO) + @"</td>
                                <td id='tdIcmsValor_" + indiceLinha.ToString() + "'>" + objIcms.TAX_PERCENTAGE.ToString("N0") + "% - " + itemNF.NF_ICMS_VICMS.ToString("C2") + @"</td>
                                <td id='tdIpi_" + indiceLinha.ToString() + "'>" + objIpi.TAX_PERCENTAGE.ToString("N0") + "% - " + itemNF.NF_IPI_VIPI.ToString("C2") + @"</td>
                                <td id='tdPrecoNet_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE.ToString("C2") + @"</td>
                                <td id='tdTotalImpostos_" + indiceLinha.ToString() + "'>" + itemNF.NF_PROD_VPROD.ToString("C2") + @"</td>
                            </tr>
                            <tr id=""detail" + indiceLinha.ToString() + @""" class=""collapse"" style=""padding:0px; margin:0px;"">
                                <td colspan=""15"" class=""panel-group smart-accordion-default"" style=""padding:0px; margin:0px;"">" + strDetalhe + @"</td>
                            </tr>
                            ";

                }
                else
                {
                    //                    strLinha = @"<tr id='" + itemNF.NF_PROD_ITEM.ToString() + @"' class='" + itemNF.NF_PROD_ITEM.ToString() + @"'>
                    //                                <td><a data-toggle=""collapse"" data-parent=""#accordion"" href=""#detail" + indiceLinha.ToString() + @""" class=""collapsed""><i class=""fa fa-chevron-down""></i></a></td>
                    //                                <td id='tdCodigo_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? itemNF.NF_PROD_XPROD : String.IsNullOrEmpty(itemNF.SAP_ITEM_DETAILS.MATERIAL) ? itemNF.SAP_ITEM_DETAILS.PO_ITEM_SHORT_TEXT : itemNF.SAP_ITEM_DETAILS.MATERIAL) + @"</td>
                    //                                <td id='tdPlanta_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.PLANT + @"</td>
                    //                                <td id='tdOrdem_" + indiceLinha.ToString() + "'>" + strOrdem + @"</td>
                    //                                <td id='tdQtde_" + indiceLinha.ToString() + "'>" + decQuantidade.ToString("N2") + @"</td>
                    //                                <td id='tdNcm_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.NCM_CODE + @"</td>
                    //                                <td id='tdTaxCode_" + indiceLinha.ToString() + "'>" + taxCode + @"</td>
                    //                                <td id='tdCfop_" + indiceLinha.ToString() + "'>" + strCfop + @"</td>
                    //                                <td id='tdTipoNf_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? "" : itemNF.MDP_TIPO_NF) + @"</td>
                    //                                <td id='tdProcesso_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? "IRRELEVANTE" : itemNF.MDP_PROCESSO) + @"</td>
                    //                                <td id='tdIcmsValor_" + indiceLinha.ToString() + "'>" + objIcms.TAX_PERCENTAGE.ToString("N0") + "% - " + objIcms.TAX_AMOUNT.ToString("C2") + @"</td>
                    //                                <td id='tdIpi_" + indiceLinha.ToString() + "'>" + objIpi.TAX_PERCENTAGE.ToString("N0") + "% - " + objIpi.TAX_AMOUNT.ToString("C2") + @"</td>
                    //                                <td id='tdPrecoNet_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE.ToString("C2") + @"</td>
                    //                                <td id='tdTotalImpostos_" + indiceLinha.ToString() + "'>" + decTotalComIpi.ToString("C2") + @"</td>
                    //                            </tr>
                    //                            <tr id=""detail" + indiceLinha.ToString() + @""" class=""collapse"" style=""padding:0px; margin:0px;"">
                    //                                <td colspan=""15"" class=""panel-group smart-accordion-default"" style=""padding:0px; margin:0px;"">" + strDetalhe + @"</td>
                    //                            </tr>
                    //                            ";

                    strLinha = @"<tr id='" + itemNF.NF_PROD_ITEM.ToString() + @"' class='" + itemNF.NF_PROD_ITEM.ToString() + @"'>
                                <td><a data-toggle=""collapse"" data-parent=""#accordion"" href=""#detail" + indiceLinha.ToString() + @""" class=""collapsed""><i class=""fa fa-chevron-down""></i></a></td>
                                <td id='tdCodigo_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? itemNF.NF_PROD_XPROD : String.IsNullOrEmpty(itemNF.SAP_ITEM_DETAILS.MATERIAL) ? itemNF.SAP_ITEM_DETAILS.PO_ITEM_SHORT_TEXT : itemNF.SAP_ITEM_DETAILS.MATERIAL) + @"</td>
                                <td id='tdPlanta_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.PLANT + @"</td>
                                <td id='tdOrdem_" + indiceLinha.ToString() + "'>" + strOrdem + @"</td>
                                <td id='tdQtde_" + indiceLinha.ToString() + "'>" + decQuantidade.ToString("N3") + @"</td>
                                <td id='tdNcm_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.NCM_CODE + @"</td>
                                <td id='tdTaxCode_" + indiceLinha.ToString() + "'>" + taxCode + @"</td>
                                <td id='tdCfop_" + indiceLinha.ToString() + "'>" + strCfop + @"</td>
                                <td id='tdTipoNf_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? "" : itemNF.MDP_TIPO_NF) + @"</td>
                                <td id='tdProcesso_" + indiceLinha.ToString() + "'>" + (itemNF.VNF_ITEM_VALIDO == "X" ? "IRRELEVANTE" : itemNF.MDP_PROCESSO) + @"</td>
                                <td id='tdIcmsValor_" + indiceLinha.ToString() + "'>" + objIcms.TAX_PERCENTAGE.ToString("N0") + "% - " + objIcms.TAX_AMOUNT.ToString("C2") + @"</td>
                                <td id='tdIpi_" + indiceLinha.ToString() + "'>" + objIpi.TAX_PERCENTAGE.ToString("N0") + "% - " + objIpi.TAX_AMOUNT.ToString("C2") + @"</td>
                                <td id='tdPrecoNet_" + indiceLinha.ToString() + "'>" + itemNF.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE.ToString("C2") + @"</td>
                                <td id='tdTotalImpostos_" + indiceLinha.ToString() + "'>" + decTotalComIpi.ToString("C2") + @"</td>
                            </tr>
                            <tr id=""detail" + indiceLinha.ToString() + @""" class=""collapse"" style=""padding:0px; margin:0px;"">
                                <td colspan=""15"" class=""panel-group smart-accordion-default"" style=""padding:0px; margin:0px;"">" + strDetalhe + @"</td>
                            </tr>
                            ";
                    //Marcio Spinosa - 30/07/2018 - CR00008351 - Fim
                }


                dadosRetorno.Append(strLinha);
                indiceLinha++;
            }

            string strTabela = strTabelaNFRef + @"
                            <table id=""dttAvisoAssociacao"" class=""display table table-responsive table-striped table-hover"" width=""100%"">
                                <thead>
                                    <tr>
                                        <th></th>
                                        <th>Código</th>
                                        <th>Planta</th>
                                        <th>Ordem</th>
                                        <th>Qtde</th>
                                        <th>NCM</th>
                                        <th>Tax Code</th>
                                        <th>CFOP</th>
                                        <th>Tipo NF</th>
                                        <th>Processo</th>
                                        <th>ICMS</th>
                                        <th>IPI " + strComplementoTituloIpi + @"</th>
                                        <th>Total sem imp.</th>
                                        <th>Total c/ imp + IPI</th>
                                    </tr>
                                </thead>
                                <tbody>" + dadosRetorno.ToString() + @"</tbody>
                            </table>";

            return strTabela;
        }

        private string GetTabelaDebitoPosterior(modNF objCte, ref bool EnviarSAP, ref string strMotivoNaoEnvioSap, ref string strTipoMiro, ref string strCabecalho)
        {
            strCabecalho = @"
                            <div class=""row"">                                
                                <div class=""col col-6"">
                                    <section class=""col col-6"">
                                        <label class=""input"">
                                            <label class=""input"">Transportadora</label>
                                            <input type=""text"" disabled=""disabled"" value='%TRANSPORTADORA%' />
                                        </label>
                                    </section>
                                    <section class=""col col-3"">
                                        <label class=""input"">
                                            <label class=""input"">CNPJ</label>
                                            <input type=""text"" disabled=""disabled"" value='%TRANSP_CNPJ%' />
                                        </label>
                                    </section>
                                    <section class=""col col-3"">
                                        <label class=""input"">
                                            <label class=""input"">Código SAP</label>
                                            <input type=""text"" disabled=""disabled"" value='%TRANSP_ID_SAP%' />
                                        </label>
                                    </section>
                                </div>                   
                                <div class=""col col-6"">
                                    <section class=""col col-6"">
                                        <label class=""input"">
                                            <label class=""input"">Fornecedor</label>
                                            <input type=""text"" disabled=""disabled"" value='%FORNECEDOR%' />
                                        </label>
                                    </section>
                                    <section class=""col col-3"">
                                        <label class=""input"">
                                            <label class=""input"">CNPJ</label>
                                            <input type=""text"" disabled=""disabled"" value='%FORN_CNPJ%' />
                                        </label>
                                    </section>
                                    <section class=""col col-3"">
                                        <label class=""input"">
                                            <label class=""input"">Código SAP</label>
                                            <input type=""text"" disabled=""disabled"" value='%FORN_ID_SAP%' />
                                        </label>
                                    </section>
                                </div>            
                            </div>
                            <div class=""row"">                                
                                <div class=""col col-6"">
                                    <section class=""col col-3"">
                                        <label class=""input"">
                                            <label class=""input"">Modo/Processo</label>
                                            <input type=""text"" disabled=""disabled"" value='%MODO_PROCESSO%' />
                                        </label>
                                    </section>
                                    <section class=""col col-2"">
                                        <label class=""input"">
                                            <label class=""input"">CFOP</label>
                                            <input type=""text"" disabled=""disabled"" value='%CFOP%' />
                                        </label>
                                    </section>
                                    <section class=""col col-4"">
                                        <label class=""input"">
                                            <label class=""input"">Valor líquido do CT-e</label>
                                            <input type=""text"" disabled=""disabled"" value='%VALOR_LIQUIDO%' />
                                        </label>
                                    </section>
                                    <section class=""col col-3"">
                                        <label class=""input"">
                                            <label class=""input"">ICMS Creditado</label>
                                            <input type=""text"" disabled=""disabled"" value='%VALOR_ICMS%' />
                                        </label>
                                    </section>
                                </div>
                                <div class=""col col-6"">
                                    <section class=""col col-3"">
                                        <label class=""input"">
                                            <label class=""input"">PIS Creditado</label>
                                            <input type=""text"" disabled=""disabled"" value='%VALOR_PIS%' />
                                        </label>
                                    </section>
                                    <section class=""col col-3"">
                                        <label class=""input"">
                                            <label class=""input"">COFINS Creditado</label>
                                            <input type=""text"" disabled=""disabled"" value='%VALOR_COFINS%' />
                                        </label>
                                    </section>
                                    <section class=""col col-4"">
                                        <label class=""input"">
                                            <label class=""input"">Valor bruto do CT-e</label>
                                            <input type=""text"" disabled=""disabled"" value='%VALOR_BRUTO%' />
                                        </label>
                                    </section>
                                    <section class=""col col-2"">
                                        <label class=""input"">
                                            <label class=""input"">TIPO NF</label>
                                            <input type=""text"" disabled=""disabled"" value='%TIPO_NF%' />
                                        </label>
                                    </section>
                                </div>
                            </div>
                            ";

            string strTabela = @"<table id=""dttAvisoAssociacao"" class=""display table table-responsive table-hover"" width=""100%"">
                                    <thead>
                                        <tr>
                                            <th colspan=""2"" width=""5%"">MIRO</th>
                                            <th colspan=""2"" width=""15%"">Código</th>
                                            <th width=""23%"">Descrição</th>
                                            <th width=""8%"">Tax Code</th>                                            
                                            <th width=""8%"">Planta</th>                                            
                                            <th width=""7%"">ICMS</th>                                    
                                            <th width=""7%"">PIS</th>                                    
                                            <th width=""7%"">COFINS</th>
                                            <th width=""10%"">Valor Líquido</th>
                                            <th width=""10%"">Valor Bruto</th>
                                        </tr>
                                    </thead>
                                    <tbody>   
                                        %LINHAS%                                     
                                    </tbody>
                                </table>";

            string strNotas = @"
                                <tr id=""trRow%INDICE%"" class=""%CSS_CLASS_RATEIO%"" nfeid=""%NFEID%"">
                                    <td><a href=""#"" onclick=""ShowNfeDetails(%INDICE%)""><i id=""btnNfDetails%INDICE%"" class=""fa fa-chevron-down %CSS_CLASS_DETAIL% ""></i></a></td>
                                    <td colspan=""3"">Nota Fiscal: <strong>%NUMERO%-%SERIE%</strong></td>
                                    <td colspan=""4"">Situação: <strong>%SITUACAO%</strong></td>
                                    <td colspan=""3""><i class=""fa fa-exclamation-triangle txt-color-red %TYPE_ERROR%"" title=""Documento não pode ser rateado""></i> Status SAP: <strong>%INTEGRACAO%</strong></td>
                                    <td>
                                        <div class=""smart-form right"">
                                            <input id=""chkNf%INDICE%"" type=""hidden"" value=""%RATEAR%"" />
                                            <label id=""lblNf%INDICE%"" class=""toggle"">
                                                <input type=""checkbox"" name=""checkbox-toggle"" %CHECK_RATEIO% onclick=""AlterarRateio(%INDICE%)"">
                                                <i data-swchon-text=""SIM"" data-swchoff-text=""NÃO""></i>RATEAR
                                            </label>
                                        </div>
                                    </td>
                                </tr>
                                ";

            string strNotasNaoEncontrada = @"
                                <tr id=""trRow%INDICE%"">                                    
                                    <td colspan=""12""><label class='padding5 padding-top7 txt-color-blue'>Nota Fiscal não encontrada no VNF - %NFEID%</label></td>
                                </tr>
                                ";

            string strItens = @"
                                <tr class=""collapse NotaFiscalDetails NotaFiscal%INDICE% shadow-row %BLOCKED%"">
                                    <th colspan=""2"" class=""center""><i class=""fa fa-exclamation-triangle txt-color-red cursor-default %ITEM_ERROR%"" title=""%ITEM_ERROR_TITLE%""></i> <label>%ITEM%</label></th>                                    
                                    <th colspan=""2""><label>%MATERIAL%</label></th>
                                    <th><label>%DESCRICAO%</label></th>
                                    <th><label>%TAX_CODE%</label></th>
                                    <th><label>%PLANTA%</label></th>
                                    <th><label>%VALOR_ICMS%</label></th>
                                    <th><label>%VALOR_PIS%</label></th>
                                    <th><label>%VALOR_COFINS%</label></th>
                                    <th><label>%VALOR_LIQUIDO%</label></th>
                                    <th title=""Valor da NF:%VALOR_NOTA_FISCAL% (%PERCENTUAL_RATEIO%%)""><label>%VALOR_BRUTO%</label></th>
                                </tr>
                              ";

            modNF objNotaFiscal = new modNF();
            StringBuilder stbNotaFiscal = new StringBuilder();
            decimal decValorLiquidoCte = 0;
            decimal decTotalIcms = 0;
            decimal decTotalPis = 0;
            decimal decTotalCofins = 0;
            decimal decItemIcms = 0;
            decimal decItemPis = 0;
            decimal decItemCofins = 0;
            string strIdTransportadora = "";
            string strFornecedorNome = "";
            string strFornecedorCnpj = "";
            string strNumeroNfe = "";
            string strSerieNf = "";
            string strLinha = "";
            int indice = 0;
            strTipoMiro = "A";

            if (objCte.NF_IDE_TPNF > 0)
            {
                strTipoMiro = "M";
                EnviarSAP = false;
                return "<label class='padding5 padding-top7 txt-color-blue'>Documento complementar. Rateio não habilitado.</label>";
            }

            SAP_RFC.SubsequentDebitReturn objDebPosterior = new SAP_RFC.SubsequentDebitReturn();
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            strIdTransportadora = objBLNotaFiscal.GetVendorCodeFretes(objCte.VNF_CHAVE_ACESSO, objCte.NF_EMIT_CNPJ);
            objDebPosterior = objBLNotaFiscal.EnviarSapDebitoPosterior(string.Empty, string.Empty, false, objCte);

            SAP_RFC.SubsequentDebitNf objStatus = new SAP_RFC.SubsequentDebitNf();
            foreach (modNFItem itemCte in objCte.ITENS_NF)
            {
                objNotaFiscal = objBLNotaFiscal.GetByID(itemCte.CT_INFNFE_CHAVE, false);
                if (objNotaFiscal.VNF_STATUS == "NÃO ENCONTRADO")
                {
                    strTipoMiro = "M";
                    EnviarSAP = false;
                    stbNotaFiscal.Append(strNotasNaoEncontrada.Replace("%NFEID%", itemCte.CT_INFNFE_CHAVE));
                }
                else
                {
                    if (objNotaFiscal.NF_IDE_NNF.Length > 6)
                        objNotaFiscal.NF_IDE_NNF = objNotaFiscal.NF_IDE_NNF.Substring(objNotaFiscal.NF_IDE_NNF.Length - 6).TrimStart('0');

                    if (!String.IsNullOrEmpty(Uteis.GetSettingsValue<string>("SerieMigoMiro")))
                        strSerieNf = Uteis.GetSettingsValue<string>("SerieMigoMiro");
                    else
                        strSerieNf = objNotaFiscal.NF_IDE_SERIE;

                    if (string.IsNullOrEmpty(strFornecedorNome))
                    {
                        strFornecedorNome = objNotaFiscal.NF_EMIT_XNOME;
                        strFornecedorCnpj = objNotaFiscal.NF_EMIT_CNPJ;
                    }

                    objStatus = new SAP_RFC.SubsequentDebitNf();
                    if (objDebPosterior.NotaFiscal != null)
                        objStatus = objDebPosterior.NotaFiscal.Where(x => x.NF_REFERENCE == objNotaFiscal.NF_IDE_NNF + "-" + strSerieNf && x.CNPJ_VENDOR == objNotaFiscal.NF_EMIT_CNPJ).FirstOrDefault();

                    strLinha = strNotas.Replace("%INDICE%", indice.ToString())
                                       .Replace("%NFEID%", objNotaFiscal.VNF_CHAVE_ACESSO)
                                       .Replace("%NUMERO%", objNotaFiscal.NF_IDE_NNF)
                                       .Replace("%SERIE%", strSerieNf)
                                       .Replace("%SITUACAO%", objNotaFiscal.VNF_STATUS)
                                       .Replace("%INTEGRACAO%", objStatus.TEXT)
                                       .Replace("%RATEAR%", (objNotaFiscal.VNF_RATEAR && !String.IsNullOrEmpty(objStatus.STATUS)).ToString())
                                       .Replace("%TYPE_ERROR%", objStatus.STATUS != "E" ? "hide" : "")
                                       .Replace("%CSS_CLASS_RATEIO%", objNotaFiscal.VNF_RATEAR && !String.IsNullOrEmpty(objStatus.STATUS) ? "txt-color-blueDark" : "nao-ratear")
                                       .Replace("%CSS_CLASS_DETAIL%", objNotaFiscal.VNF_RATEAR && !String.IsNullOrEmpty(objStatus.STATUS) ? "" : "hide")
                                       .Replace("%CHECK_RATEIO%", objNotaFiscal.VNF_RATEAR && !String.IsNullOrEmpty(objStatus.STATUS) ? "checked='checked'" : "");

                    stbNotaFiscal.Append(strLinha);

                    if (objStatus.STATUS == "E" && objNotaFiscal.VNF_RATEAR)
                    {
                        strMotivoNaoEnvioSap = "Existem itens na lista que rateio que não podem ser processados. Revise a distribuição do rateio.";
                    }
                    else if (string.IsNullOrEmpty(strIdTransportadora))
                    {
                        strMotivoNaoEnvioSap = "O código da transportadora não foi encontrado. Revise a informação no sistema de Fretes.";
                    }

                    if (objDebPosterior.Allocation != null)
                    {
                        foreach (SAP_RFC.SubsequentDebitAllocation objAllocation in objDebPosterior.Allocation.Where(x => x.NF_REFERENCE == objNotaFiscal.NF_IDE_NNF + "-" + strSerieNf))
                        {
                            strNumeroNfe = objBLNotaFiscal.AjustarNumeroNf(objNotaFiscal.NF_IDE_NNF);

                            if (objAllocation.USAGE == "2" || objAllocation.USAGE == "3")
                            {
                                decItemIcms = 0;
                                decItemPis = 0;
                                decItemCofins = 0;
                            }
                            else
                            {
                                decItemIcms = objAllocation.ICMS;
                                decItemPis = objAllocation.PIS;
                                decItemCofins = objAllocation.COFINS;
                            }

                            decTotalIcms += decItemIcms;
                            decTotalPis += decItemPis;
                            decTotalCofins += decItemCofins;
                            decValorLiquidoCte += objAllocation.CTE_ALLOCATION_NET;

                            //CfopSemRateio(itemNfe.NF_PROD_CFOP) ? "blocked-row" :
                            strLinha = strItens.Replace("%INDICE%", indice.ToString())
                                               .Replace("%ITEM%", objAllocation.MIRO_DOCUMENT_NUMBER + "-" + objAllocation.MIRO_DOCUMENT_ITEM.ToString())
                                               .Replace("%MATERIAL%", objAllocation.MATERIAL_NUMBER)
                                               .Replace("%DESCRICAO%", objAllocation.MATERIAL_DESCRIPTION)
                                               .Replace("%TAX_CODE%", objAllocation.TAX_CODE)
                                               .Replace("%PLANTA%", objAllocation.PLANT)
                                               .Replace("%VALOR_ICMS%", decItemIcms.ToString("C2"))
                                               .Replace("%VALOR_PIS%", decItemPis.ToString("C2"))
                                               .Replace("%VALOR_COFINS%", decItemCofins.ToString("C2"))
                                               .Replace("%VALOR_LIQUIDO%", objAllocation.CTE_ALLOCATION_NET.ToString("C2"))
                                               .Replace("%VALOR_BRUTO%", objAllocation.CTE_ALLOCATION_GROSS.ToString("C2"))
                                               .Replace("%BLOCKED%", "")
                                               .Replace("%ITEM_ERROR%", objStatus.STATUS != "E" ? "hide" : "")
                                               .Replace("%ITEM_ERROR_TITLE%", objStatus.TEXT)
                                               .Replace("%PERCENTUAL_RATEIO%", (objAllocation.CTE_ALLOCATION_PERCENTAGE * 100).ToString("N4"))
                                               .Replace("%VALOR_NOTA_FISCAL%", objAllocation.NF_NET_VALUE.ToString("C2"));

                            stbNotaFiscal.Append(strLinha);
                        }
                    }
                }

                indice++;
            }

            string strPercentualIcms = "";
            string strPercentualPis = "";
            string strPercentualCofins = "";
            if (!Object.ReferenceEquals(objCte.ITENS_NF.FirstOrDefault().SAP_ITEM_DETAILS, null) && !Object.ReferenceEquals(objCte.ITENS_NF.FirstOrDefault().SAP_ITEM_DETAILS.ITEM_TAXES, null))
            {
                strPercentualIcms = objCte.ITENS_NF.FirstOrDefault().SAP_ITEM_DETAILS.ITEM_TAXES.Where(x => x.TAX_NAME == "ICMS").FirstOrDefault().TAX_PERCENTAGE.ToString("N0") + "% - ";
                strPercentualPis = objCte.ITENS_NF.FirstOrDefault().SAP_ITEM_DETAILS.ITEM_TAXES.Where(x => x.TAX_NAME == "PIS").FirstOrDefault().TAX_PERCENTAGE.ToString("N2") + "% - ";
                strPercentualCofins = objCte.ITENS_NF.FirstOrDefault().SAP_ITEM_DETAILS.ITEM_TAXES.Where(x => x.TAX_NAME == "COFINS").FirstOrDefault().TAX_PERCENTAGE.ToString("N2") + "% - ";
            }

            strCabecalho = strCabecalho.Replace("%TRANSPORTADORA%", objCte.NF_EMIT_XNOME)
                                       .Replace("%TRANSP_CNPJ%", objCte.NF_EMIT_CNPJ.ToCnpj())
                                       .Replace("%TRANSP_ID_SAP%", strIdTransportadora)
                                       .Replace("%FORNECEDOR%", strFornecedorNome)
                                       .Replace("%FORN_CNPJ%", strFornecedorCnpj.ToCnpj())
                                       .Replace("%FORN_ID_SAP%", objBLNotaFiscal.GetVendorCode(strFornecedorCnpj))
                                       .Replace("%CFOP%", objBLNotaFiscal.GetCfopEntrada(objCte))
                                       .Replace("%MODO_PROCESSO%", objCte.ITENS_NF.FirstOrDefault().MDP_PROCESSO)
                                       .Replace("%VALOR_ICMS%", strPercentualIcms + decTotalIcms.ToString("C2"))
                                       .Replace("%VALOR_PIS%", strPercentualPis + decTotalPis.ToString("C2"))
                                       .Replace("%VALOR_COFINS%", strPercentualCofins + decTotalCofins.ToString("C2"))
                                       .Replace("%VALOR_LIQUIDO%", decValorLiquidoCte.ToString("C2"))
                                       .Replace("%VALOR_BRUTO%", objCte.ITENS_NF.FirstOrDefault().NF_PROD_VPROD.ToString("C2"))
                                       .Replace("%TIPO_NF%", objCte.ITENS_NF.FirstOrDefault().MDP_TIPO_NF);

            return strTabela.Replace("%LINHAS%", stbNotaFiscal.ToString());
        }


        private bool CfopSemRateio(string pCfop)
        {
            return modSQL.ExecuteScalar("SELECT count(*) FROM TbCfopSemRateio WHERE cfop = '" + pCfop + "'").ToString().ToInt() > 0;
        }


        [HttpGet]
        public ActionResult DownloadAnexoPS(string id)
        {
            byte[] file = null;
            string fileExtension = null;
            string fileName = null;
            string mimeType = "application/pdf";

            DLImportacaoNotaFiscal objImportacaoNotafiscal = new DLImportacaoNotaFiscal();
            var idNotaFiscal = objImportacaoNotafiscal.getImportacaoNotaByChaveAcesso(id).IdNotaFiscal;

            DLIntegracaoPSGetPDF objIntegracao = new DLIntegracaoPSGetPDF();
            var integracao = objIntegracao.getPdfPS(idNotaFiscal);

            file = integracao.BINARY_ARQUIVO;
            fileExtension = ".pdf";
            fileName = integracao.ANEXO_NF;

            Response.AppendHeader("Content-Disposition", "inline; filename=" + fileName);
            return File(file, mimeType);
        }


        [HttpGet]
        public string EstornarVNF(string NfeId, string observacao)
        {
            BLNotaFiscal vObjBLNotafiscal = new BLNotaFiscal();
            string retorno = string.Empty;
            DB vObjdb = new DB();

            try
            {
                vObjdb.SP_ESTORNO_INTEGRACAO_VNF(NfeId);
                vObjdb.SaveChanges();
                retorno = "Estorno realizado com sucesso!";
                BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.EstornoVNF, "Realizou o estorno do documento " + vObjBLNotafiscal.GetNumeroDocumento(NfeId) + " no VNF. " + observacao, NfeId);
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
                                                                                { "data", retorno.ToString()}});

            }
            catch (Exception erro)
            {
                retorno = "Erro ao executar estorno de documento. Mensagem técnica: " + erro.Message;
                BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.ErroEstornoVNF, "Tentou estornar o doumento " + vObjBLNotafiscal.GetNumeroDocumento(NfeId) + " sem sucesso. Mensagem técnica: " + erro.Message, NfeId);
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
                                                                                { "data", retorno.ToString()}});

            }
            finally
            {

                vObjBLNotafiscal = null;
                vObjdb = null;
            }

        }

    }
}
