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
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Text;
using System.Data;
using System.ServiceProcess;
using MetsoFramework.Utils;
using MetsoFramework.Files;
using VNF.Business;
using VNF.Portal.DataLayer;

namespace VNF.Portal.Controllers
{
    public class HomeController : Controller
    {

        [OutputCache(Duration = 120, VaryByParam = "*")]
        public ActionResult Index()
        {
            //BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            //Dictionary<string, object> objChartRecebimentoNF = new Dictionary<string, object>();
            //objChartRecebimentoNF = objBLNotaFiscal.GetChartRecebimentoNF();
            //ViewBag.DadosRecebimentoNF = objChartRecebimentoNF["dados"];
            //ViewBag.LegendaRecebimentoNF = objChartRecebimentoNF["legenda"];

            //BLDivergencias objBLDivergencias = new BLDivergencias();
            //Dictionary<string, object> objChartDivergenciasAtivas = new Dictionary<string, object>();
            //objChartDivergenciasAtivas = objBLDivergencias.GetChartDivergenciasAtivas();
            //ViewBag.DadosDivergenciasAtivas = objChartDivergenciasAtivas["dados"];
            //ViewBag.LegendaDivergenciasAtivas = objChartDivergenciasAtivas["legenda"];

            //ViewBag.DocumentosRecebidos = objBLNotaFiscal.GetDocumentosRecebidos();

            //BLLog objBLLog = new BLLog();
            //ViewBag.LogApplication = objBLLog.getLog(BLLog.LogType.Application);
            //ViewBag.LogService = objBLLog.getLog(BLLog.LogType.Service);


            string strServiceStatus = string.Empty;
            try
            {
                // Recupera o status do windows service
                ServiceController sc = new ServiceController("Metso - Validador de Nota Fiscal");
                switch (sc.Status)
                {
                    case ServiceControllerStatus.Running:
                        strServiceStatus = "Running";
                        break;
                    case ServiceControllerStatus.Stopped:
                        strServiceStatus = "Stopped";
                        break;
                    case ServiceControllerStatus.Paused:
                        strServiceStatus = "Paused";
                        break;
                    case ServiceControllerStatus.StopPending:
                        strServiceStatus = "Stopping";
                        break;
                    case ServiceControllerStatus.StartPending:
                        strServiceStatus = "Starting";
                        break;
                    default:
                        strServiceStatus = "Status Changing";
                        break;
                }
            }
            catch (Exception)
            {
                strServiceStatus = "Unknow";
            }

            Uteis.AddCookieValue("ServiceStatus", strServiceStatus, Uteis.ValueType.String);

            return View();
        }

        [HttpPost]
        public string GetChartRecebimentoNF() {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            Dictionary<string, object> objChartRecebimentoNF = new Dictionary<string, object>();
            objChartRecebimentoNF = objBLNotaFiscal.GetChartRecebimentoNF();
            return Newtonsoft.Json.JsonConvert.SerializeObject(new { Result = "ok",
                                                                     DadosRecebimentoNF = objChartRecebimentoNF["dados"].ToString(),
                                                                     LegendaRecebimentoNF = objChartRecebimentoNF["legenda"].ToString()
            }).ToString();
        }

        [HttpPost]
        public string GetChartDivergenciasAtivas() {
            BLDivergencias objBLDivergencias = new BLDivergencias();
            Dictionary<string, object> objChartDivergenciasAtivas = new Dictionary<string, object>();
            objChartDivergenciasAtivas = objBLDivergencias.GetChartDivergenciasAtivas();
            return Newtonsoft.Json.JsonConvert.SerializeObject(new { Result = "ok",
                                                                     DadosDivergenciasAtivas = objChartDivergenciasAtivas["dados"].ToString(),
                                                                     LegendaDivergenciasAtivas = objChartDivergenciasAtivas["legenda"].ToString()
            }).ToString();
        }

        [HttpPost]
        public string GetDocumentosRecebidos() {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            var result = objBLNotaFiscal.GetDocumentosRecebidos();
            return Newtonsoft.Json.JsonConvert.SerializeObject(new {
                Result = "ok",
                DocumentosRecebidos = result.Rows.OfType<DataRow>().ToList().Select(x => new {
                    NFEID = x["NFEID"].ToString(),
                    VNF_TIPO_DOCUMENTO = x["VNF_TIPO_DOCUMENTO"].ToString(),
                    NF_IDE_NNF = x["NF_IDE_NNF"].ToString(),
                    NF_NOME_EMITENTE = x["NF_NOME_EMITENTE"].ToString(),
                    NF_QTD_ITENS = x["NF_QTD_ITENS"].ToString(),
                    SITUACAO = x["SITUACAO"].ToString(),
                    PRIORIDADE_ALTA = x["PRIORIDADE_ALTA"].ToString()
                }).ToList()
            }).ToString();
        }

        /// <summary>
        /// Retorna os logs da aplicação
        /// </summary>
        /// <returns></returns>
        /// <example>Ajuste para retornar os dados do usuário sem usar AD</example>
        [HttpPost]
        public string GetLogApplication() {
            //<li class="message">
            //    <img src="@Html.AvenuePicture(dtrLogUsuarios["log_user"].ToString())" 
            //        class="online" 
            //        alt="@Uteis.GetUserNameBySamId(dtrLogUsuarios["log_user"].ToString())" 
            //        onerror="this.src='@Url.Content("~/Images/Users/user_undefined_square.png")'">
            //    <div class="message-text">
            //        <time>
            //            @Convert.ToDateTime(dtrLogUsuarios["log_date"].ToString()).ToString("dd/MM/yyyy HH:mm")
            //        </time>
            //        <a href="#" class="username cursor-default">
            //            @Uteis.GetUserNameBySamId(dtrLogUsuarios["log_user"].ToString())
            //        </a>
            //        @dtrLogUsuarios["log_description"].ToString() &nbsp; @Html.Raw(dtrLogUsuarios["log_icon"].ToString())
            //    </div>
            //</li>
            BLLog objBLLog = new BLLog();
            var result = objBLLog.getLog(BLLog.LogType.Application);
            return Newtonsoft.Json.JsonConvert.SerializeObject(new {
                Result = "ok",
                LogApplication = result.Rows.OfType<DataRow>().ToList().Select(x => new {
                    log_user_img = "", //((HtmlHelper) null).AvenuePicture(x["log_user"].ToString()),
                    log_user = x["log_user"].ToString(),
                    log_date = Convert.ToDateTime(x["log_date"].ToString()).ToString("dd/MM/yyyy HH:mm"),
                    //Marcio Spinosa - 24/05/2018 - CR00008351
                    //UserNameBySamId = Uteis.GetUserNameBySamId(x["log_user"].ToString()),
                    UserNameBySamId = x["usunomusu"].ToString(),
                    //Marcio Spinosa - 24/05/2018 - CR00008351 - Fim
                    log_description = x["log_description"].ToString(),
                    log_icon = x["log_icon"].ToString()
                }).ToList()
            }).ToString();
        }

        [HttpPost]
        public string GetLogService() {
            BLLog objBLLog = new BLLog();
            var result = objBLLog.getLog(BLLog.LogType.Service);
            return Newtonsoft.Json.JsonConvert.SerializeObject(new {
                Result = "ok",
                LogService = result.Rows.OfType<DataRow>().ToList().Select(x => new {
                    log_icon = x["log_icon"].ToString(),
                    log_date = Convert.ToDateTime(x["log_date"].ToString()).GetRelativeTimeNow(),
                    log_title = x["log_title"].ToString(),
                    log_description = x["log_description"].ToString()
                }).ToList()
            }).ToString();
        }

        [OutputCache(Duration = 120, VaryByParam = "*")]
        public PartialViewResult Notificacoes()
        {
            return PartialView();
        }

        [OutputCache(Duration = 120, VaryByParam = "*")]
        public PartialViewResult Alertas()
        {
            return PartialView();
        }

        [OutputCache(Duration = 120, VaryByParam = "*")]
        public string GetNotificacoes()
        {
            BLNotificacoes objBLNotificacoes = new BLNotificacoes();
            return Serialization.JSON.CreateString(objBLNotificacoes.GetNotificacoes());
        }


    }
}
