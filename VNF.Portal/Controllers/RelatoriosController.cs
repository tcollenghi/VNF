using MetsoFramework.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VNF.Business;

namespace VNF.Portal.Controllers
{
    public class RelatoriosController : Controller
    {
        //
        // GET: /Relatorios/
        public static readonly string[] RGBA_CHART_COLORS = { "rgba(255, 195, 77,1)"
                                                            , "rgba(179, 255, 102,1)"
                                                            , "rgba(230, 230, 0,1)"
                                                            , "rgba(102, 204, 255,1)"
                                                            , "rgba(255, 148, 77,1)"
                                                            , "rgba(163, 163, 117,1)"
                                                            , "rgba(102, 255, 204,1)"
                                                            , "rgba(102, 140, 255,1)"
                                                            , "rgba(255, 153, 153,1)"
                                                            , "rgba(128, 255, 170,1)"};

        public ActionResult MonitorDivergencias()
        {
            return View();
        }

        [HttpGet]
        public string GetRelatorioSucatas()
        {
            DataTable vObjDT = new DataTable();

            return "Teste";// vObjDT;
        }

        public ActionResult RelatorioSucatas()
        {
            ViewBag.UnidadeMetso = CarregarUnidadeMetso();
            ViewBag.table = GetHTMlDataTable();
            return View();
        }

        [HttpGet]
        //Recupera as principais divergencias dos compradores indiretos
        public string GetDiveCompraIndireto(int pCategoriaComprador, string pStrTopSQLCommand, string pUnidadeMetso)
        {
            BLDivergencias vObjDive = new BLDivergencias();
            SqlDataReader vObjDR;
            ArrayList vObjArrayQTD = new ArrayList();
            ArrayList vObjArrayLabelCompradores = new ArrayList();
            ArrayList vObjArrayLabelDivergencias = new ArrayList();
            ArrayList vObjArrayDataSet = new ArrayList();
            string vStrCompradores = string.Empty;
            string vStrLabelDivergencias = string.Empty;
            int vInCountArray = 0;



            try
            {
                //Buscando dados no banco de dados
                vObjDR = vObjDive.GetDataMonitorDivergencais(pCategoriaComprador, pStrTopSQLCommand, pUnidadeMetso);

                //Criando objetso JSON com as informações das divergências
                if (vObjDR.HasRows)
                {
                    while (vObjDR.Read())
                    {
                        vObjArrayDataSet.Add(new
                        {
                            data = vObjDR["QTD_CSV"].ToString().Split(','),
                            backgroundColor = RGBA_CHART_COLORS[vInCountArray],
                            hoverBackgroundColor = "rgba(50,90,100,1)",
                            label = "Comprador " + vObjDR["CODCOM"].ToString(),
                        });

                        vStrLabelDivergencias = vObjDR["CODCOM_CSV"].ToString();
                        vInCountArray++;
                    }
                }


                return Serialization.JSON.CreateString(new
                {
                    labels = vStrLabelDivergencias.Split(','),
                    datasets = vObjArrayDataSet,
                    timerRefresh = Uteis.GetSettingsValue<String>("timerRefreshMonitorDivergencias").ToString()
                });
            }
            catch (Exception ex)
            {

                throw ex;
            }


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
        private MvcHtmlString GetHTMlDataTable()
        {
        //{
        //    var table = MvcHtmlString.Create("table id='dttNotaFiscal' datatables_fixedheader='top' datatables_fixedheader_offsettop='60' class='table table-striped table-bordered table-hover table-click' width='100%'" +
        //                                     " <thead> <tr> " +
        //                                     "<th width='10%' data-class='expand'>Número</th> " +
        //                                     "</tr> </thead> ");

            
            //var table = MvcHtmlString.Create("<th width='10%' data-class='expand'>Número</th>" +
            //                                 "  <th width='8%'>Série</th> " +
            //                                 "  <th width='18%'>Fornecedor</th>" +
            //                                 "  <th width='13%'>Id Fornecedor</th>" +
            //                                 "  <th width='13%'>Tipo</th>" +
            //                                 "  <th width='10%'>Data Emissão</th>" +
            //                                 "  <th width='13%'>CNPJ</th>" +
            //                                 "  <th width='10%'>Situação</th>" +
            //                                 "  <th width='10%'>Integração SAP</th>" +
            //                                 "  <th width='8%'>Recebido</th>") ;

            var table = MvcHtmlString.Create("<th data-class='expand'>Número</th>" +
                                             "  <th >Série</th> " +
                                             "  <th >Fornecedor</th>" +
                                             "  <th >Id Fornecedor</th>" +
                                             "  <th >Tipo</th>" +
                                             "  <th >Data Emissão</th>" +
                                             "  <th >CNPJ</th>" +
                                             "  <th >Situação</th>" +
                                             "  <th >Integração SAP</th>" +
                                             "  <th >Recebido</th>");


            return table;
        }
    }
}
