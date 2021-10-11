/*
 * Autor: Marcio Spinosa - CR00008351
 * Data: 28/05/2018 
 * obs: Ajuste para o VNF não consultar o AD para trazer dados do usuário e sim o banco de dados.
 */

using MetsoFramework.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VNF.Business;
using VNF.Portal.DataLayer;
using VNF.Portal.Models;

namespace VNF.Portal.Controllers
{
    public class OcorrenciasController : Controller
    {
        //
        // GET: /Ocorrencias/

        public ActionResult Index(string id = "")
        {
            //ViewBag.RecebedorCNPJ = "";
            //ViewBag.DataEnvioDe = "";
            //ViewBag.DataEnvioAte = "";
            //ViewBag.VencimentoDe = "";
            //ViewBag.VencimentoAte = "";
            //ViewBag.FornecedorCNPJ = "";
            //ViewBag.NumeroDocumento = "";
            //ViewBag.Status = "Retornado";
            //ViewBag.Responsavel = "";

            ViewBag.Status = "Retornado";
            ViewBag.CookiePage = "";
            if (!String.IsNullOrEmpty(id))
            {
                // ViewBag.Responsavel = new DLUsers().GetNameByLogonName(Uteis.LogonName());
                ViewBag.Responsavel = Uteis.LogonName();
                ViewBag.Status = "Pendente";
                ViewBag.CookiePage = "MinhaAtividade";

            }
            else
                if ((new VNF.Business.BLAcessos()).ConsultaAcesso("OCOR", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|OCOR";

            //if (Request.Cookies["ConsultaOcorrencia"] != null)
            //{
            //    try
            //    {
            //        string ValorCookie = Request.Cookies["ConsultaOcorrencia"].Value.ToString();
            //        string[] valores = ValorCookie.Split('|');
            //        ViewBag.RecebedorCNPJ = valores[0];
            //        ViewBag.DataEnvioDe = valores[1];
            //        ViewBag.DataEnvioAte = valores[2];
            //        ViewBag.VencimentoDe = valores[3];
            //        ViewBag.VencimentoAte = valores[4];
            //        ViewBag.FornecedorCNPJ = valores[5];
            //        ViewBag.NumeroDocumento = valores[6];
            //        ViewBag.Status = valores[7];
            //    }
            //    catch
            //    {

            //    }
            //}
            ////DLOcorrencias dal = new DLOcorrencias();
            ////var lista = dal.GetByParams(ViewBag.RecebedorCNPJ, ViewBag.DataEnvioDe, ViewBag.DataEnvioAte, ViewBag.VencimentoDe, ViewBag.VencimentoAte, ViewBag.FornecedorCNPJ, ViewBag.NumeroDocumento, ViewBag.Status, false, ViewBag.Responsavel);

            //var ListaUsuarios = new DLUsers().Get().OrderBy(x => x.usunomusu).ToList();
            //string html = "";
            //foreach (var i in ListaUsuarios)
            //{
            //    html += "<option value=\"" + i.usucodusu + "\">" + i.usunomusu + "</option>";
            //}
            //ViewBag.comboResponsavel = html;

            //ViewBag.comboStatus = "";
            //string htmlStatus = "";
            //htmlStatus += "<option></option>";
            //htmlStatus += String.Format("<option {0} value=\"Pendente\">Pendente</option>", ViewBag.Status == "Pendente" ? "selected=\"selected\"" : "");
            //htmlStatus += String.Format("<option {0} value=\"Retornado\">Retornado</option>", ViewBag.Status == "Retornado" ? "selected=\"selected\"" : "");
            //htmlStatus += String.Format("<option {0} value=\"Finalizado\">Finalizado</option>", ViewBag.Status == "Finalizado" ? "selected=\"selected\"" : "");
            //ViewBag.comboStatus = htmlStatus;
            //return View(lista);
            return View();
        }

        #region :: JSON Methods ::
        [HttpPost]
        public string GetStatus(string statusPadrao)
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            lista.Add(new SelectListItem { Value = "Pendente", Text = "Pendente", Selected = statusPadrao == "Pendente" });
            lista.Add(new SelectListItem { Value = "Retornado", Text = "Retornado", Selected = statusPadrao == "Retornado" || string.IsNullOrWhiteSpace(statusPadrao) });
            lista.Add(new SelectListItem { Value = "Finalizado", Text = "Finalizado", Selected = statusPadrao == "Finalizado" });

            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Result = "ok",
                Status = lista
            }).ToString();
        }

        [HttpPost]
        public string GetResponsaveis()
        {
            var ListaUsuarios = new DLUsers().Get().OrderBy(x => x.usunomusu).ToList();
            List<SelectListItem> lista = new List<SelectListItem>();
            foreach (var i in ListaUsuarios)
            {
                lista.Add(new SelectListItem { Value = i.usucodusu, Text = i.usunomusu });
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Result = "ok",
                Responsaveis = lista
            }).ToString();
        }
        #endregion :: JSON Methods ::

        //[HttpPost]
        //public ActionResult Index(string RecebedorCNPJ, string DataEnvioDe, string DataEnvioAte, string VencimentoDe, string VencimentoAte, string FornecedorCNPJ, string NumeroDocumento, string Status, string Responsavel)
        //{
        //    HttpCookie cookie = new HttpCookie("ConsultaOcorrencia");
        //    cookie.Value = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}", RecebedorCNPJ, DataEnvioDe, DataEnvioAte, VencimentoDe, VencimentoAte, FornecedorCNPJ, NumeroDocumento, Status);
        //    Response.Cookies.Add(cookie);

        //    ViewBag.RecebedorCNPJ = RecebedorCNPJ;
        //    ViewBag.DataEnvioDe = DataEnvioDe;
        //    ViewBag.DataEnvioAte = DataEnvioAte;
        //    ViewBag.VencimentoDe = VencimentoDe;
        //    ViewBag.VencimentoAte = VencimentoAte;
        //    ViewBag.FornecedorCNPJ = FornecedorCNPJ;
        //    ViewBag.NumeroDocumento = NumeroDocumento;
        //    ViewBag.Status = Status;

        //    DLOcorrencias dal = new DLOcorrencias();
        //    var ListaUsuarios = new DLUsers().Get().OrderBy(x => x.usunomusu).ToList();
        //    string html = "";
        //    foreach (var i in ListaUsuarios)
        //    {
        //        html += "<option value=\"" + i.usucodusu + "\">" + i.usunomusu + "</option>";
        //    }
        //    ViewBag.comboResponsavel = html;

        //    ViewBag.comboStatus = "";
        //    string htmlStatus = "";
        //    htmlStatus += "<option></option>";
        //    htmlStatus += String.Format("<option {0} value=\"Pendente\">Pendente</option>", ViewBag.Status == "Pendente" ? "selected=\"selected\"" : "");
        //    htmlStatus += String.Format("<option {0}  value=\"Retornado\">Retornado</option>", ViewBag.Status == "Retornado" ? "selected=\"selected\"" : "");
        //    htmlStatus += String.Format("<option {0}  value=\"Finalizado\">Finalizado</option>", ViewBag.Status == "Finalizado" ? "selected=\"selected\"" : "");
        //    ViewBag.comboStatus = htmlStatus; 
        //    var lista = dal.GetByParams(RecebedorCNPJ, DataEnvioDe, DataEnvioAte, VencimentoDe, VencimentoAte, FornecedorCNPJ, NumeroDocumento, Status, false, Responsavel);
        //    return View(lista);
        //}

        [HttpGet]
        public PartialViewResult LoadData(string RecebedorCNPJ, string DataEnvioDe, string DataEnvioAte, string VencimentoDe, string VencimentoAte, string FornecedorCNPJ, string NumeroDocumento, string Status, string Responsavel, string CookiePage,
                                              int? pQtdRegistros)
        {
            //int min = 100, max = 50000;
            //pQtdRegistros = pQtdRegistros ?? min;
            //pQtdRegistros = pQtdRegistros < min ? min : pQtdRegistros > max ? max : pQtdRegistros;

            // Hoje já está filtrado com 200
            pQtdRegistros = 200;

            HttpCookie cookie = new HttpCookie("ConsultaOcorrencia" + CookiePage);
            cookie.Value = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}", RecebedorCNPJ, DataEnvioDe, DataEnvioAte, VencimentoDe, VencimentoAte, FornecedorCNPJ, NumeroDocumento, Status);
            Response.Cookies.Add(cookie);

            DLOcorrencias dal = new DLOcorrencias();
            bool exceedQtd = false;
            var lista = dal.GetByParams(RecebedorCNPJ, DataEnvioDe, DataEnvioAte, VencimentoDe, VencimentoAte, FornecedorCNPJ, NumeroDocumento, Status,
                                                                                  pQtdRegistros.Value, ref exceedQtd,
                                                                                  false, Responsavel);

            var msg = "* Quantidade de registros excedem o limite de {0} registros consultados (por favor, melhore o seu filtro)";
            ViewBag.ExceedQtdMsg = exceedQtd ? String.Format(msg, pQtdRegistros) : "";
            return PartialView("GridOcorrencias", lista);
        }

        [HttpPost]
        public string Responder(int IdOcorrencia, string Texto)
        {
            string Retorno = "ok";
            try
            {
                DLOcorrenciasComentarios idal = new DLOcorrenciasComentarios();
                var ultimocomentario = idal.GetComentarios(IdOcorrencia).FirstOrDefault();
                DLOcorrencias dal = new DLOcorrencias();
                var oc = dal.GetByID(IdOcorrencia);
                oc.Status = "Retornado";
                if (ultimocomentario != null)
                {
                    //Marcio Spinosa - 23/08/2018 - CR00008351
                    //oc.Responsavel = ultimocomentario.Usuario;
                    oc.Responsavel = ultimocomentario.idUsuario;
                    //Marcio Spinosa - 23/08/2018 - CR00008351 - Fim
                }
                dal.Update(oc);
                dal.Save();

                //lança o comentario

                OcorrenciasComentarios i = new OcorrenciasComentarios();
                i.Comentario = Texto;
                i.Data = DateTime.Now;
                i.IdOcorrencia = oc.IdOcorrencia;
                i.Usuario = Uteis.LogonName();
                idal.Insert(i);
                idal.Save();

                BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
                BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.ResponderOcorrencia, "Respondeu a ocorrência da " + objBLNotaFiscal.GetNumeroDocumento(oc.NFEID), oc.NFEID);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }


            return Retorno;
        }

        /// <summary>
        /// Encaminha email para o responsável da ocorrência
        /// </summary>
        /// <param name="IdOcorrencia"></param>
        /// <param name="Responsavel"></param>
        /// <param name="Comentario"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 23/05/2018 - CR00008351 - Ajuste para não retornar dados do usuário do AD</example>
        [HttpPost]
        public string Encaminhar(int IdOcorrencia, string Responsavel, string Comentario)
        {
            string Retorno = "ok";
            try
            {
                DLOcorrencias dal = new DLOcorrencias();
                var oc = dal.GetByID(IdOcorrencia);
                oc.Status = "Pendente";
                oc.Responsavel = Responsavel;
                oc.DataRecebimento = DateTime.Now;
                dal.Update(oc);
                dal.Save();

                DLOcorrenciasComentarios idal = new DLOcorrenciasComentarios();
                OcorrenciasComentarios i = new OcorrenciasComentarios();
                //Marcio Spinosa - 23/05/2018 - CR00008351
                DLUsers objDLUser = new DLUsers();
                //i.Comentario = "Ocorrência encaminhada para " + Uteis.GetUserNameBySamId(Responsavel) + ": " + Comentario;
                i.Comentario = "Ocorrência encaminhada para " + objDLUser.getDadosByLogon(Responsavel)[1] + ": " + Comentario;
                i.Data = DateTime.Now;
                i.IdOcorrencia = oc.IdOcorrencia;
                i.Usuario = Uteis.LogonName();
                idal.Insert(i);
                idal.Save();

                BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
                var nf = objBLNotaFiscal.GetByID(oc.NFEID, false);
                string Body = "Uma ocorrência foi encaminhada aos seus cuidados.<br />";
                Body += "NF: " + nf.NF_IDE_NNF + "<br />";
                Body += "Fornecedor: " + nf.NF_EMIT_XNOME + "<br />";
                Body += "Prioridade: Alta <br />";
                Body += "Comentário: " + Comentario + "<br />";

                string strUrl = Request.Url.GetLeftPart(UriPartial.Authority) + "/VNF/Ocorrencias/index/MinhaAtividade";
                //string strUrl = Request.Url.GetLeftPart(UriPartial.Authority) + "/" + Uteis.GetSettingsValue<string>("Prefix") + "/Ocorrencias/index/MinhaAtividade";

                //string EmailFrom = Uteis.GetUserInfoBySamId(Uteis.LogonName())[1];

                string EmailFrom = objDLUser.getDadosByLogon(Uteis.LogonName())[2];
                //string msgBody = Uteis.GetMailBody.StandardTemplate("VNF", "Validador de Notas Fiscais", Uteis.GetUserNameBySamId(Responsavel), Body, strUrl);
                string msgBody = Uteis.GetMailBody.StandardTemplate("VNF", "Validador de Notas Fiscais", objDLUser.getDadosByLogon(Responsavel)[1], Body, strUrl);
                //string EmailTo = Uteis.GetUserInfoBySamId(Responsavel)[1];
                string EmailTo = objDLUser.getDadosByLogon(Responsavel)[2];
                Uteis.SendMailStream(EmailFrom, EmailTo, "", "", "Ocorrencia Encaminhada", msgBody, null);

                BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.EncaminharOcorrencia, "Encaminhou a ocorrência da " + objBLNotaFiscal.GetNumeroDocumento(oc.NFEID), oc.NFEID);
                //Marcio Spinosa - 23/05/2018 - CR00008351 - Fim
            }
            catch (Exception ex)
            {
                return ex.Message;
            }


            return Retorno;
        }

        [HttpGet]
        public string GetOCSTatus(int id)
        {
            DLOcorrencias dal = new DLOcorrencias();
            var oc = dal.GetByID(id);
            if (oc == null)
            {
                return "";
            }
            else
            {
                return oc.Status;
            }

        }
        /// <summary>
        /// Finaliza a ocorrência
        /// </summary>
        /// <param name="IdOcorrencia"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 23/05/2018 - CR00008351 - Ajuste para não utilizar o AD</example>
        [HttpPost]
        public string Finalizar(int IdOcorrencia)
        {
            string Retorno = "ok";
            try
            {
                DLOcorrencias dal = new DLOcorrencias();
                var oc = dal.GetByID(IdOcorrencia);
                oc.Status = "Finalizado";
                //oc.Responsavel = ""; Marcio Spinosa - 24/08/2018 - CR00008351
                dal.Update(oc);
                dal.Save();

                //lança o comentario
                DLOcorrenciasComentarios idal = new DLOcorrenciasComentarios();
                OcorrenciasComentarios i = new OcorrenciasComentarios();
                //Marcio Spinosa - 28/05/2018 - CR00008351
                DLUsers objDLUser = new DLUsers();
                //i.Comentario = "Ocorrência finalizada por " + Uteis.UserName();
                i.Comentario = "Ocorrência finalizada por " + objDLUser.getDadosByLogon(Uteis.LogonName())[1];
                //Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
                i.Data = DateTime.Now;
                i.IdOcorrencia = oc.IdOcorrencia;
                i.Usuario = Uteis.LogonName();
                idal.Insert(i);
                idal.Save();

                BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
                BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.FinalizarOcorrencia, "Finalizou a ocorrência da " + objBLNotaFiscal.GetNumeroDocumento(oc.NFEID), oc.NFEID);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }


            return Retorno;
        }

        public ActionResult Fiscal()
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("OCOR", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|OCOR";
            return View();
        }

        [HttpGet]
        public string GetOcorrenciaAnexos(string NFEID)
        {
            string html = "";
            var oc = new DLOcorrencias().GetOcorrenciaAnexos(NFEID);
            foreach (var i in oc)
            {
                html += "<tr>";
                html += "<td>" + i.NOME + "</td>";
                //html += "<td><a href='" + Url.Action("DownloadAnexoOcorrencia", "Ocorrencias") + "/" + i.id.ToString() + "' ><i title=\"Download\" onclick=\"DownloadAnexo();\" class=\"fa fa-download\"></i></a></td>";
                html += "<td><a href='" + Url.Action("DownloadAnexoOcorrencia", "Ocorrencias") + "/" + i.id.ToString() + "' ><i title=\"Download\" class=\"fa fa-download\"></i></a></td>";

                //html += "<td><a href='" + Url.Action("ExcluirAnexoOcorrencia", "Ocorrencias") + "/" + i.id.ToString() + "' ><i title=\"Excluir\" onclick=\"ExcluirdAnexo();\" class=\"btn btn-danger btn-xs\"><i class=\"fa fa-times\"></i></a></td>";
                html += "<td><a onclick=\"ExcluirdAnexo('" + i.id.ToString() + "')\" href=\"#\" class=\"btn btn-danger btn-xs\"><i class=\"fa fa-times\"></i></a></td>";


                html += "</tr>";
            }

            html = String.IsNullOrWhiteSpace(html) ? "<tr><td></td><td></td><td></td></tr>" : html;
            //return html;

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {{ "html", html },
                                                                                     { "qtdOcorr", oc.Count },
                                                                                     });

        }

        [HttpPost]
        public void UploadAnexo(string NFEID)
        {
            for (int i = 0; i < Request.Files.Count; i++)
            {
                if (Request.Files[i].ContentLength > 0)
                {
                    TbDOC_CAB_ANEXOS a = new TbDOC_CAB_ANEXOS();
                    a.ANEXO = Request.Files[i].InputStream.ToByteArray();
                    a.EXTENSAO = Request.Files[i].ContentType;
                    a.NOME = Request.Files[i].FileName.Remove(0, Request.Files[i].FileName.LastIndexOf("\\") + 1);
                    a.NFEID = NFEID;
                    DLOcorrenciasAnexos dal = new DLOcorrenciasAnexos();
                    dal.Insert(a);
                    dal.Save();
                }
            }
        }

        [HttpGet]
        public ActionResult DownloadAnexoOcorrencia(int id)
        {
            DLOcorrenciasAnexos dal = new DLOcorrenciasAnexos();
            var anexo = dal.GetByID(id);
            byte[] file = null;
            string fileExtension = null;
            string fileName = null;
            if (anexo != null)
            {
                file = anexo.ANEXO;
                fileExtension = anexo.EXTENSAO;
                fileName = anexo.NOME;
            }
            return File(file, fileExtension, fileName);
        }

        [HttpGet]
        public void ExcluirAnexoOcorrencia(int id)
        {
            try
            {
                DLOcorrenciasAnexos dal = new DLOcorrenciasAnexos();
                dal.Delete(id);
                dal.Save();
            }
            catch (Exception)
            {
            }
        }

        [HttpGet]
        public string Exportar(string RecebedorCNPJ, string DataEnvioDe, string DataEnvioAte, string VencimentoDe, string VencimentoAte, string FornecedorCNPJ, string NumeroDocumento, string Status,
                                int? pQtdRegistros)
        {
            //int min = 100, max = 50000;
            //pQtdRegistros = pQtdRegistros ?? min;
            //pQtdRegistros = pQtdRegistros < min ? min : pQtdRegistros > max ? max : pQtdRegistros;

            // Hoje já está filtrado com 200
            pQtdRegistros = 200;

            DLOcorrencias dal = new DLOcorrencias();
            bool exceedQtd = false;
            var lista = dal.GetByParams(RecebedorCNPJ, DataEnvioDe, DataEnvioAte, VencimentoDe, VencimentoAte, FornecedorCNPJ, NumeroDocumento, Status, pQtdRegistros.Value, ref exceedQtd, true);

            var listaDT = lista.Select(x => new { x.Data, x.NumeroDocumento, x.Origem, x.Prioridade, x.CodigoFornecedor, x.Comprador, x.Motivo, x.Status, x.Valor }).ToList();

            DataTable dt = Util.Util.ListToDataTable(listaDT);
            string fileName = "OCORR - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);
            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);
            return fileName;
        }
    }
}
