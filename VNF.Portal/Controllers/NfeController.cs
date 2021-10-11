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
    public class NfeController : Controller
    {

        [HttpGet]
        public ActionResult Index()
        {
            
            return View();
        }

        [HttpGet]
        public ActionResult NOTF()
        {
            ViewBag.UnidadeMetso = CarregarUnidadeMetso();
            ViewBag.Situacao = CarregarSituacaoNf();
            ViewBag.Motivo = CarregarMotivo();
            ViewBag.TipoData = CarregarData();
            return View();
        }

        [HttpGet]
        public ActionResult VNFE(string pIdNfe)
        {
            ViewBag.UnidadeMetso = CarregarUnidadeMetso();
            ViewBag.Situacao = CarregarSituacao();
            ViewBag.Motivo = CarregarMotivo();

            //BLNotaFiscal obj = new BLNotaFiscal();
            //obj.Validar(pIdNfe);
            return View();
        }

        [HttpGet]
        public ActionResult Edit(string id = "")
        {
            //            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            modNF objmodNF = new modNF();
            objmodNF.VNF_CHAVE_ACESSO = id;
            //objBLNotaFiscal.GetByID(id);
            return View(objmodNF);
        }

        [HttpGet]
        public string LoadDataVNFE(int pStart, int pLength, string pPasta, string pDocE, Boolean pPastaAberta)
        {
            
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();
            dadosRetorno.Clear();

            DataTable dt = new BLPortaria().GetNotasFiscais((string.IsNullOrEmpty(pDocE) ? null : pDocE), null, pPasta, null);
            int indexInicial = pStart * pLength;
            int indexFinal = indexInicial + pLength;
            int qtde = 0;
            for (int i = indexInicial; i < dt.Rows.Count; i++)
            {
                if (i >= indexFinal)
                    break;

                dadosRetorno.Append("<tr id='" + dt.Rows[i]["NFEID"].ToString() + "' class='" + dt.Rows[i]["NFEID"].ToString() + "' onclick='SelectRowInfoAssociacao(&#39;Nfe&#39;, &#39;" + dt.Rows[i]["NFEID"].ToString() + "&#39;)'>");

                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_XNOME"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["NF_IDE_DHEMI"]).ToString("dd/MM/yyyy") + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_CNPJ"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["REL"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["SITUACAO"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["IPLOG"].ToString() + "</td>");

                dadosRetorno.Append("</tr>");
                qtde++;
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
                                                                                      { "data", dadosRetorno.ToString() }, 
                                                                                      {"pagina" , qtde.ToString()}});
        }

        [HttpGet]
        public string EventoCodigoBarras(string pCodBarra)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();
            dadosRetorno.Clear();

            DataTable dt = new BLPortaria().GetNotasFiscais(pCodBarra, null, null, null);
            int qtde = 0;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dadosRetorno.Append("<tr id='" + dt.Rows[i]["NFEID"].ToString() + "' class='" + dt.Rows[i]["NFEID"].ToString() + "' onclick='SelectRowInfoAssociacao(&#39;Nfe&#39;, &#39;" + dt.Rows[i]["NFEID"].ToString() + "&#39;)'>");

                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_XNOME"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["NF_IDE_DHEMI"]).ToString("dd/MM/yyyy") + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_CNPJ"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["REL"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["SITUACAO"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["IPLOG"].ToString() + "</td>");
                dadosRetorno.Append("</tr>");
                qtde++;
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
                                                                                      { "data", dadosRetorno.ToString() }, 
                                                                                      {"pagina" , qtde.ToString() }});

        }
        
        [HttpGet]
        public string LoadDataNOTF(int pStart, int pLength, string pNumeroDocumento, string pCNPJ, string pPedidoCompra, string pUnidadeMetso, string pSituacao, string pTipoData, string pDataDe, string pDataAte)
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
                                                       pCNPJ == null ? string.Empty : pCNPJ.Replace(".", "").Replace("-", ""),
                                                       pSituacao,
                                                       pPedidoCompra == null ? string.Empty : pPedidoCompra,
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty,
                                                       pUnidadeMetso, 
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty);

            int indexInicial = pStart * pLength;
            int indexFinal = indexInicial + pLength;
            int qtde = 0;
            for (int i = indexInicial; i < dt.Rows.Count; i++)
            {
                if (i >= indexFinal)
                    break;

                dadosRetorno.Append("<tr id='" + dt.Rows[i]["NFEID"].ToString() + "' class='" + dt.Rows[i]["NFEID"].ToString() + "' onclick='SelectRow(&#39;Nfe&#39;, &#39;" + dt.Rows[i]["NFEID"].ToString() + "&#39;)'>");
                dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["NF_IDE_DHEMI"]).ToString("dd/MM/yyyy") + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_XNOME"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_SERIE"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["SITUACAO"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_CNPJ"].ToString() + "</td>");
                if (dt.Rows[i]["DATVAL"] != DBNull.Value)
                    dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["DATVAL"]).ToString("dd/MM/yyyy") + "</td>");
                else
                    dadosRetorno.Append("<td></td>");

                dadosRetorno.Append("<td>" + dt.Rows[i]["NFEID"].ToString() + "</td>");
                if (dt.Rows[i]["NFEREL"].ToString() == "S")
                    dadosRetorno.Append("<td>Sim</td>");
                else
                    dadosRetorno.Append("<td>Não</td>");

                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_DEST_CNPJ"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["CODCOM"].ToString() + "</td>");

                if (dt.Rows[i]["DATLOG"] != DBNull.Value)
                    dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["DATLOG"]).ToString("dd/MM/yyyy") + "</td>");
                else
                    dadosRetorno.Append("<td> - </td>");
                dadosRetorno.Append("</tr>");
                qtde++;
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
            { "data", dadosRetorno.ToString()}, {"pagina" , qtde.ToString()}  });
        }

        [HttpGet]
        public string LoadDataConsulta(int pStart, int pLength, string pNumeroDocumento, string pCNPJ, string pPedidoCompra, string pUnidadeMetso, string pSituacao, string pTipoData, string pDataDe, string pDataAte)
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
                                                       pCNPJ == null ? string.Empty : pCNPJ.Replace(".", "").Replace("-", ""),
                                                       pSituacao,
                                                       pPedidoCompra == null ? string.Empty : pPedidoCompra,
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty,
                                                       pUnidadeMetso, 
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty);

            int indexInicial = pStart * pLength;
            int indexFinal = indexInicial + pLength;
            int qtde = 0;
            for (int i = indexInicial; i < dt.Rows.Count; i++)
            {
                if (i >= indexFinal)
                    break;

                dadosRetorno.Append("<tr id='" + dt.Rows[i]["NFEID"].ToString() + "' class='" + dt.Rows[i]["NFEID"].ToString() + "' onclick='SelectRow(&#39;Nfe&#39;, &#39;" + dt.Rows[i]["NFEID"].ToString() + "&#39;)'>");
                dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["NF_IDE_DHEMI"]).ToString("dd/MM/yyyy") + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_XNOME"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_NNF"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_IDE_SERIE"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["SITUACAO"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_EMIT_CNPJ"].ToString() + "</td>");
                if (dt.Rows[i]["DATVAL"] != DBNull.Value)
                    dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["DATVAL"]).ToString("dd/MM/yyyy") + "</td>");
                else
                    dadosRetorno.Append("<td></td>");

                dadosRetorno.Append("<td>" + dt.Rows[i]["NFEID"].ToString() + "</td>");
                if (dt.Rows[i]["NFEREL"].ToString() == "S")
                    dadosRetorno.Append("<td>Sim</td>");
                else
                    dadosRetorno.Append("<td>Não</td>");

                dadosRetorno.Append("<td>" + dt.Rows[i]["NF_DEST_CNPJ"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["CODCOM"].ToString() + "</td>");

                if (dt.Rows[i]["DATLOG"] != DBNull.Value)
                    dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["DATLOG"]).ToString("dd/MM/yyyy") + "</td>");
                else
                    dadosRetorno.Append("<td> - </td>");
                dadosRetorno.Append("</tr>");
                qtde++;
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
                                                                                      { "data", dadosRetorno.ToString()}, 
                                                                                      {"pagina" , qtde.ToString()}});
        }

        [HttpGet]
        public string ExportarNOTF(string pNumeroDocumento, string pCNPJ, string pPedidoCompra, string pUnidadeMetso, string pSituacao, string pTipoData, string pDataDe, string pDataAte)
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
                                                       pCNPJ == null ? string.Empty : pCNPJ.Replace(".", "").Replace("-", ""),
                                                       pSituacao,
                                                       pPedidoCompra == null ? string.Empty : pPedidoCompra,
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty,
                                                       pUnidadeMetso, 
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty);

            string fileName = "NOTF - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
        }

        [HttpGet]
        public string ExportaVerificaNF(string pNumeroDocumento, string pCNPJ, string pPedidoCompra, string pUnidadeMetso, string pSituacao, string pDataEmissao, string pDataProcessamento, string pDataEnvioIP)
        {


            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            pSituacao = string.IsNullOrEmpty(pSituacao) ? "(TODAS)" : pSituacao;
            pUnidadeMetso = string.IsNullOrEmpty(pUnidadeMetso) ? "(TODAS)" : pUnidadeMetso;

            DataTable dt = objBLNotaFiscal.GetByFilter(string.Empty,
                                                       string.Empty,
                                                       modNF.TipoData.Emissao,
                                                       pNumeroDocumento == null ? string.Empty : pNumeroDocumento,
                                                       pCNPJ == null ? string.Empty : pCNPJ,
                                                       pSituacao,
                                                       pPedidoCompra == null ? string.Empty : pPedidoCompra,
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty,
                                                       pUnidadeMetso, 
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty,
                                                       string.Empty);

            string fileName = "NFE - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
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


            string fileName = "DIVE - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
        }

        [HttpGet]
        public string LoadDataDivergenciaNfe(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();

            DataTable dt = objBLNotaFiscal.GetDivergencias(pIdNfe);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dadosRetorno.Append("<tr id='divDivergencia_" + dt.Rows[i]["NFEID"].ToString() + "' class='" + dt.Rows[i]["NFEID"].ToString() + "' onclick='SelectRow(&#39;Nfe&#39;, &#39;" + dt.Rows[i]["NFEID"].ToString() + "&#39;)'>");
                dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["DATLOG"]).ToString("dd/MM/yyyy") + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["SITUACAO"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["ITENFE"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["CAMPO"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["VALOR_NFE"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["VALOR_PED"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["PEDCOM"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["ITEPED"].ToString() + "</td>");

                if (dt.Rows[i]["DATA_CORRECAO"] != DBNull.Value)
                    dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["DATA_CORRECAO"]).ToString("dd/MM/yyyy") + "</td>");
                else
                    dadosRetorno.Append("<td> - </td>");

                dadosRetorno.Append("<td>" + dt.Rows[i]["MOTIVO"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["JUSTIFICATIVA"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["USUARIO_ANULACAO"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["OBSERVACAO"].ToString() + "</td>");
                dadosRetorno.Append("</tr>");
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
            { "data", dadosRetorno.ToString()} });
        }

        [HttpGet]
        public DataTable LoadDataDIVE(int pStart, int pLength, string txtDocE, string Motivo, string Situacao, string txtNumeroPO, string txtCodigoComprador, string txtFornecedor, string txtDataEmissaoDe, string txtDataEmissaoAte, string txtDataDivergenciaDe, string txtDataDivergenciaAte, string arrDivergencia)
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

            return dt;
        }

        [HttpGet]
        public PartialViewResult GetDataDivergencia(string txtDocE, string Motivo, string Situacao, string txtNumeroPO, string txtCodigoComprador, string txtFornecedor, string txtDataEmissaoDe, string txtDataEmissaoAte, string txtDataDivergenciaDe, string txtDataDivergenciaAte, string arrDivergencia)
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
            return PartialView("ViewDive", dt);
        }

        [HttpGet]
        public string LoadDataItens(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();
            modNFItem[] objListItem = objBLNotaFiscal.GetItens(pIdNfe);

            for (int i = 0; i < objListItem.Length; i++)
            {
                modNFItem item = objListItem[i];
                Decimal valorTotal = item.NF_PROD_VUNCOM * item.NF_PROD_QCOM;

                dadosRetorno.Append("<tr>");
                dadosRetorno.Append("<td>" + item.NF_PROD_ITEM.ToString() + "</td>");
                if (item.NF_PROD_XPED != null)
                    dadosRetorno.Append("<td>" + item.NF_PROD_XPED.ToString() + "</td>");
                else
                    dadosRetorno.Append("<td> - </td>");
                dadosRetorno.Append("<td>" + item.NF_PROD_NITEMPED.ToString() + "</td>");
                dadosRetorno.Append("<td>" + item.NF_PROD_CPROD.ToString() + "</td>");
                dadosRetorno.Append("<td>" + item.NF_PROD_XPROD.ToString() + "</td>");
                dadosRetorno.Append("<td>" + item.NF_PROD_UCOM.ToString() + "</td>");
                dadosRetorno.Append("<td>" + item.NF_PROD_QCOM.ToString() + "</td>");
                dadosRetorno.Append("<td>" + item.NF_PROD_VUNCOM.ToString("N2") + "</td>");
                dadosRetorno.Append("<td>" + valorTotal.ToString("N2") + "</td>");
                dadosRetorno.Append("<td>" + item.NF_PROD_CFOP.ToString() + "</td>");
                dadosRetorno.Append("<td>" + item.VNF_ITEM_VALIDO.ToString() + "</td>");
                dadosRetorno.Append("</tr>");
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
            { "data", dadosRetorno.ToString()} });
        }

        [HttpGet]
        public string LoadDataMensagens(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();
            DataTable dt = objBLNotaFiscal.GetMensagens(pIdNfe);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dadosRetorno.Append("<tr onclick='exibirMensagem(" + i.ToString() + ")' >");
                dadosRetorno.Append("<td>" + Convert.ToDateTime(dt.Rows[i]["DATENV"]).ToString("dd/MM/yyyy") + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["EMAIL"].ToString() + "</td>");
                dadosRetorno.Append("<td>" + dt.Rows[i]["SITUACAO"].ToString() + "</td>");
                dadosRetorno.Append("</tr>");

                dadosRetorno.Append("<tr id='trMensagem_" + i.ToString() + "' style='display:none'>");
                dadosRetorno.Append("<td colspan='3'>" + dt.Rows[i]["MENENV"].ToString() + "</td>");
                dadosRetorno.Append("</tr>");
            }
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
            { "data", dadosRetorno.ToString()} });
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
        public ActionResult GetXml(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            string dadosRetorno = objBLNotaFiscal.GetXml(pIdNfe);

            //string dadosRetorno = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"2.00\"><NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\"><infNFe Id=\"NFe35150100988202000193550010000171341932337988\" versao=\"2.00\"><ide><cUF>35</cUF><cNF>93233798</cNF><natOp>Vendas</natOp><indPag>1</indPag><mod>55</mod><serie>1</serie><nNF>17134</nNF><dEmi>2015-01-05</dEmi><tpNF>1</tpNF><cMunFG>3552205</cMunFG><tpImp>2</tpImp><tpEmis>1</tpEmis><cDV>8</cDV><tpAmb>1</tpAmb><finNFe>1</finNFe><procEmi>0</procEmi><verProc>AGIW</verProc></ide><emit><CNPJ>00988202000193</CNPJ><xNome>CONECT PECAS E ACESSORIOS INDUSTRIAIS EIRELLI</xNome><xFant>CONECT</xFant><enderEmit><xLgr>AV. FERNANDO STECCA</xLgr><nro>423</nro><xBairro>VILA SAO JUDAS</xBairro><cMun>3552205</cMun><xMun>Sorocaba</xMun><UF>SP</UF><CEP>18087149</CEP><cPais>1058</cPais><xPais>BRASIL</xPais><fone>1532384711</fone></enderEmit><IE>669308700118</IE><CRT>1</CRT></emit><dest><CNPJ>16622284000198</CNPJ><xNome>METSO BRASIL INDUSTRIA E COMERCIO LTDA.</xNome><enderDest><xLgr>AV. INDEPENDENCIA</xLgr><nro>2500</nro><xBairro>Iporanga</xBairro><cMun>3552205</cMun><xMun>Sorocaba</xMun><UF>SP</UF><CEP>18087101</CEP><cPais>1058</cPais><xPais>BRASIL</xPais><fone>1521021300</fone></enderDest><IE>669575300114</IE><email>nfefornecedor.mctbr@metso.com</email></dest><det nItem=\"1\"><prod><cProd>1001985750</cProd><cEAN></cEAN><xProd>TEE FEMEA GIR LATERAL X MACHO JIC 37  3/4 8 R6X-S</xProd><NCM>73072200</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>20.0000</qCom><vUnCom>45.6100</vUnCom><vProd>912.20</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>20.0000</qTrib><vUnTrib>45.6100</vUnTrib><indTot>1</indTot><xPed>4501269058</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>35.12</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501269058;</infAdProd></det><det nItem=\"2\"><prod><cProd>MM0349200</cProd><cEAN></cEAN><xProd>PORCA ACO M12X1,5 M06LCFX</xProd><NCM>73072900</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>4.0000</qCom><vUnCom>1.9500</vUnCom><vProd>7.80</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>4.0000</qTrib><vUnTrib>1.9500</vUnTrib><indTot>1</indTot><xPed>4501280945</xPed><nItemPed>000050</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>0.30</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501280945;</infAdProd></det><det nItem=\"3\"><prod><cProd>53334267006</cProd><cEAN></cEAN><xProd>MANGUEIRA C/ TERMINAIS PONTA LISA C/ 4000MM</xProd><NCM>40092190</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>5.0000</qCom><vUnCom>101.1300</vUnCom><vProd>505.65</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>5.0000</qTrib><vUnTrib>101.1300</vUnTrib><indTot>1</indTot><xPed>4501280945</xPed><nItemPed>000060</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>19.47</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501280945;</infAdProd></det><det nItem=\"4\"><prod><cProd>53334267006</cProd><cEAN></cEAN><xProd>MANGUEIRA C/ TERMINAIS PONTA LISA C/ 4000MM</xProd><NCM>40092190</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>1.0000</qCom><vUnCom>101.1400</vUnCom><vProd>101.14</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>1.0000</qTrib><vUnTrib>101.1400</vUnTrib><indTot>1</indTot><xPed>4501291879</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>3.89</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501291879;</infAdProd></det><det nItem=\"5\"><prod><cProd>1044252204</cProd><cEAN></cEAN><xProd>MANGUEIRA F471TC6868080806-775MM</xProd><NCM>40092110</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>68.0000</qCom><vUnCom>63.7300</vUnCom><vProd>4333.64</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>68.0000</qTrib><vUnTrib>63.7300</vUnTrib><indTot>1</indTot><xPed>4501295340</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>166.85</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501295340;</infAdProd></det><det nItem=\"6\"><prod><cProd>MM0217667</cProd><cEAN></cEAN><xProd>BUJAO SEXTAVADO INTERNO 1/2 BSP INOX VSTI1/2ED71</xProd><NCM>73072200</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>1.0000</qCom><vUnCom>48.9400</vUnCom><vProd>48.94</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>1.0000</qTrib><vUnTrib>48.9400</vUnTrib><indTot>1</indTot><xPed>4501295340</xPed><nItemPed>000050</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>1.88</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501295340;</infAdProd></det><det nItem=\"7\"><prod><cProd>1044252612</cProd><cEAN></cEAN><xProd>MANGUEIRA F471TC6868080806-2175MM</xProd><NCM>40092190</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>8.0000</qCom><vUnCom>99.2300</vUnCom><vProd>793.84</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>8.0000</qTrib><vUnTrib>99.2300</vUnTrib><indTot>1</indTot><xPed>4501297913</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>30.56</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501297913;</infAdProd></det><total><ICMSTot><vBC>0.00</vBC><vICMS>0.00</vICMS><vBCST>0.00</vBCST><vST>0.00</vST><vProd>6703.21</vProd><vFrete>0.00</vFrete><vSeg>0.00</vSeg><vDesc>0.00</vDesc><vII>0</vII><vIPI>0.00</vIPI><vPIS>0.00</vPIS><vCOFINS>0.00</vCOFINS><vOutro>0.00</vOutro><vNF>6703.21</vNF></ICMSTot></total><transp><modFrete>1</modFrete><transporta><CNPJ>52548435014200</CNPJ><xNome>JSL S/A</xNome><IE>669653462115</IE><xEnder>AVENIDA JEROME CASE 2302</xEnder><xMun>Sorocaba</xMun><UF>SP</UF></transporta><vol><qVol>1</qVol><esp>VOLUME</esp></vol></transp><cobr><dup><nDup>017134A</nDup><dVenc>2015-02-16</dVenc><vDup>6703.21</vDup></dup></cobr><infAdic><infCpl>Empresa optante pelo simples nacional conforme L.C 123/2006 - ;Permite o aproveitamento de credito de ICMS conforme aliquota 3,85% no valor de R$: 258,07</infCpl></infAdic></infNFe><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#NFe35150100988202000193550010000171341932337988\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>Qab3OPDblmrAfISyM+FqgBIdVaw=</DigestValue></Reference></SignedInfo><SignatureValue>hLzGsismfcllwXp/O9b78ifzwlMDDOgDgmEVusDKwx8Wh1urbji58J/9+XX0i/hATqnybzCTO6r+f9LoSkcohtNkuE/+xQ0a4UR+pXGUFIw7SPhRobwh9LVGQPHalF06uu5gbb1LI0i5Pt/mokCT2rNrgN5hN93GT5q15USrLZdM+PZf2o6uqelpgAvachCZl7yDc4RtPC6XVlMUzvXBQQ42s8lZlpXdwLW2vksaX624naw8eAo8glCqDDSktvettqtz5MJahSs0ZhqQI8wLVp8NEM7xKHKJIhWgFmT3sdriHC6rzZXiHgygtfq5aZ1ebS1W+v929kR1B2cUFBQzng==</SignatureValue><KeyInfo><X509Data><X509Certificate>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</X509Certificate></X509Data></KeyInfo></Signature></NFe><protNFe versao=\"2.00\" xmlns=\"http://www.portalfiscal.inf.br/nfe\"><infProt><tpAmb>1</tpAmb><verAplic>SP_NFE_PL_006q</verAplic><chNFe>35150100988202000193550010000171341932337988</chNFe><dhRecbto>2015-01-05T17:22:10</dhRecbto><nProt>135150004276546</nProt><digVal>Qab3OPDblmrAfISyM+FqgBIdVaw=</digVal><cStat>100</cStat><xMotivo>Autorizado o uso da NF-e</xMotivo></infProt></protNFe></nfeProc>";
            dadosRetorno = dadosRetorno.Replace("\"", "'");

            string fileName = pIdNfe + ".xml";

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
        public ActionResult PrintDanfe(string id)
        {
            ViewBag.Nfeid = id;
            return View();
        }

        [HttpGet]
        public ActionResult DIVE()
        {
            ViewBag.UnidadeMetso = CarregarUnidadeMetso();
            ViewBag.Situacao = CarregarSituacao();
            ViewBag.Motivo = CarregarMotivo();
            ViewBag.ddlTipoDivergencia = ddlTipoDivergencia();

            return View();
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

        #region Metodos

        private List<SelectListItem> CarregarUnidadeMetso()
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            DataTable dt = objBLNotaFiscal.GetUnidadeMetso();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                lista.Add(new SelectListItem { Value = dt.Rows[i][0].ToString(), Text = dt.Rows[i][0].ToString() });
            }
            return lista;
        }

        public List<SelectListItem> CarregarSituacaoNf()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "ACEITA", Text = "ACEITA" });
            lista.Add(new SelectListItem { Value = "PENDENTE", Text = "PENDENTE" });
            lista.Add(new SelectListItem { Value = "REJEITADA", Text = "REJEITADA" });
            lista.Add(new SelectListItem { Value = "CANCELADA", Text = "CANCELADA" });
            lista.Add(new SelectListItem { Value = "PENDENTE", Text = "RECUSADA" });

            return lista;
        }

        public List<SelectListItem> CarregarSituacao()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "ATIVO", Text = "ATIVO" });
            lista.Add(new SelectListItem { Value = "INATIVO", Text = "INATIVO" });


            return lista;
        }

        public List<SelectListItem> CarregarData()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "E", Text = "Emissão da NFe", Selected = true });
            lista.Add(new SelectListItem { Value = "C", Text = "Chegada da DANFE" });
            lista.Add(new SelectListItem { Value = "V", Text = "Envio para IP" });


            return lista;
        }


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

        public List<SelectListItem> ddlTipoDivergencia()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "CONDICAOPAGAMENTO", Text = "CONDIÇÃO DE PAGAMENTO" });
            lista.Add(new SelectListItem { Value = "REMESSAFINAL", Text = "REMESSA FINAL" });
            lista.Add(new SelectListItem { Value = "AVISOEMBARQUE", Text = "AVISO DE EMBARQUE" });
            lista.Add(new SelectListItem { Value = "ANTECIPACAOPEDIDO", Text = "ANTECIPAÇÃO DO PEDIDO" });
            lista.Add(new SelectListItem { Value = "CNPJEMITENTE", Text = "CNPJ EMITENTE" });
            lista.Add(new SelectListItem { Value = "QUANTIDADE", Text = "QUANTIDADE" });
            lista.Add(new SelectListItem { Value = "APROVADO", Text = "APROVADO" });
            lista.Add(new SelectListItem { Value = "DELETADO", Text = "DELETADO" });
            lista.Add(new SelectListItem { Value = "VALOR", Text = "VALOR" });
            lista.Add(new SelectListItem { Value = "PLANTA", Text = "PLANTA" });
            lista.Add(new SelectListItem { Value = "NCM", Text = "NCM" });
            lista.Add(new SelectListItem { Value = "RE", Text = "RE" });


            return lista;
        }

        [HttpGet]
        public string VerificarDIVE(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            modNF mod = new modNF();
            string strMensagem = "";
            mod = objBLNotaFiscal.Validar(pIdNfe, string.Empty, Uteis.LogonName(), ref strMensagem);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" } });
        }

        [HttpGet]
        public void Visualizar(string pIdNfe)
        {
            // chamar a tela da fase 2
        }

        [HttpGet]
        public string CancelarDIVE(string pIdNfe)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            var retorno = objBLNotaFiscal.Cancelar(pIdNfe, Uteis.LogonName(), false);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
                                                                                      { "data", retorno.ToString()}});
        }

        [HttpGet]
        public string AnularDIVE(string NfeId, int CodLog, string Justificativa, string Detalhe)
        {
            BLDivergencias objBLDivergencia = new BLDivergencias();
            string retorno = string.Empty;
            try
            {
                retorno = objBLDivergencia.Anular(NfeId, CodLog, Justificativa, Detalhe, null, "", "");//, "VNFE");
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, 
                                                                                          { "data", retorno.ToString()}});
            }
            catch (Exception erro)
            {
                retorno = "Ocorreu um erro na execução da chamada";
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Error" }, 
                                                                                          { "data", retorno.ToString()}});
            }
        }

        #endregion
    }
}
