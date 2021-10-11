/*
 * Autor: Marcio Spinosa - CR00008351
 * Data: 28/05/2018 
 * obs: Ajuste para o VNF efetuar o login no AD
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MetsoFramework.Core;
using MetsoFramework.Utils;
using System.Configuration;
using VNF.Portal.Models;
using VNF.Business;
using System.Data;

namespace VNF.Portal.Controllers
{
    public class UserController : Controller
    {
        //
        // GET: /User/

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public string LoginFornecedor(string LogonName, int SapCode, string Recaptcha)
        {
            //var objBLUser = new BLUsers();
            bool recaptchOk = true; //objBLUser.ValidateCaptcha(Recaptcha);
            bool IsSuccess = false;
            string strReturnText = string.Empty;

            if (recaptchOk)
            {
                IsSuccess = ValidarLoginFornecedor(LogonName, SapCode, out strReturnText);
            }
            else
            {
                strReturnText = "Falha ao validar autenticação das imagens";
            }

            string strUrl = string.Empty;
            if (Request.Cookies["BlockedUrl"] != null)
            {
                strUrl = Request.Cookies["BlockedUrl"].Value;
                Uteis.RemoveCookie("BlockedUrl");
            }

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "Success", IsSuccess },
                                                                                      { "Text", strReturnText },
                                                                                      { "Url", strUrl } });
        }

        private bool ValidarLoginFornecedor(string CNPJ, int? IDSap, out string Mensagem)
        {
            List<modVendors> arrVendor = ValidateVendor(CNPJ);

            if (arrVendor == null || arrVendor.Count == 0 || arrVendor.First().VendorCode <= 0 || !IDSap.HasValue)
            {
                Mensagem = "CNPJ informado não foi encontrado. Por favor verifique se está correto.";
                return false;
            }
            else
            {
                bool vendorOk = false;
                modVendors objVendor = new modVendors();
                foreach (modVendors item in arrVendor)
                {
                    if (item.VendorCode == IDSap.Value)
                    {
                        objVendor = item;
                        vendorOk = true;
                        break;
                    }
                }

                if (vendorOk)
                {
                    Uteis.AddCookieValue("LogonType", "FORNECEDOR", Uteis.ValueType.String);
                    Uteis.AddCookieValue("LogonName", CNPJ.RemoveLetters(), Uteis.ValueType.String);
                    Uteis.AddCookieValue("UserName", objVendor.Name1 + objVendor.Name2, Uteis.ValueType.String);
                    Uteis.AddCookieValue("UserCode", IDSap.Value);
                    Mensagem = string.Empty;
                    return true;
                }
                else
                {
                    Mensagem = "CNPJ não é compatível com o ID informado. Por favor verifique os dados e tente novamente.";
                    return false;
                }
            }
        }

        public List<modVendors> ValidateVendor(string CNPJ)
        {
            var objBlVendors = new BLVendors();
            return objBlVendors.GetByCNPJ(CNPJ);
        }
        /// <summary>
        /// Valida o login do usuário metso no AD
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        /// <example>Marcio Spinosa - 28/05/2018 - CR00008351 - Ajuste para não consultar o AD para trazer dados do usuário após o login validado</example>
        public ActionResult LoginMetso(Login login)
        {
            string logonName = "";
            string[] ArrInfouser;//Marcio Spinosa - 28/05/2018 - CR00008351
            var dal = new VNF.Portal.DataLayer.DLUsers();
            bool IsSuccess = Uteis.ValidateUser(login.Email, login.Senha, out logonName, out ArrInfouser);

            if (IsSuccess)
            {
                string strAppUrl = ConfigurationManager.AppSettings["appUrlPrefix"].ToString();
                FormsAuthentication.SetAuthCookie(logonName, false);
                Uteis.AddCookieValue("LogonType" + strAppUrl, "METSO", Uteis.ValueType.String);
                Uteis.AddCookieValue("LogonName" + strAppUrl, logonName, Uteis.ValueType.String);
                //Marcio Spinosa - 28/05/2018 - CR00008351
                //Uteis.AddCookieValue("UserName" + strAppUrl, Uteis.GetUserNameBySamId(logonName), Uteis.ValueType.String);
                Uteis.AddCookieValue("UserName" + strAppUrl, dal.getDadosByLogon(Uteis.LogonName())[1], Uteis.ValueType.String);

                // Cria o usuário na base caso não exista
                try
                {

                    var usuemail = dal.GetEmailByLogonName(logonName);
                    if (usuemail == null)
                    {
                        dal.Insert(new TbUsuario()
                        {
                            usucodusu = logonName,
                            //usunomusu = Uteis.GetUserNameBySamId(logonName),
                            usunomusu = ArrInfouser[0],
                            usuEmail = login.Email
                        });
                        dal.db.SaveChanges();
                        //Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
                    }
                }
                catch { }

                //Marcio Spinosa - xxxx
                BLAcessos objBLAcesso = new BLAcessos();
                DataSet objTelas = objBLAcesso.ConsultarTelas();
                DataTable item = objTelas.Tables[0];

                DataSet dtsValida = objBLAcesso.ConsultarTelas(logonName);


                if (dtsValida.Tables[0].Rows.Count < item.Rows.Count)
                {
                    foreach (DataRow row in item.Rows)
                        objBLAcesso.ConsultaAcesso(row["ACECODTEL"].ToString(), Uteis.LogonName());
                }

                return RedirectToAction("Index", "Home");
            }
            else
            {
                if (Request.Cookies["BlockedUrl"] != null)
                {
                    if (Request.Cookies["BlockedUrl"].Value.ToString().Replace(@"/", "").ToUpper() != Uteis.GetSettingsValue<string>("appUrlPrefix").ToString().ToUpper())
                    {
                        Uteis.RemoveCookie("BlockedUrl");
                    }
                }
                TempData["LoginInvalido"] = logonName.Substring(0, (logonName.Length - 2));
                
                return View("Login");
            }
        }

        //[HttpPost]
        //public string LoginMetso(string mail, string password)
        //{
        //    string logonName = "";
        //    bool IsSuccess = Uteis.ValidateUser(mail, password, out logonName);

        //    if (IsSuccess)
        //    {
        //        string strAppUrl = ConfigurationManager.AppSettings["appUrlPrefix"].ToString();
        //        FormsAuthentication.SetAuthCookie(logonName, false);
        //        Uteis.AddCookieValue("LogonType" + strAppUrl, "METSO", Uteis.ValueType.String);
        //        Uteis.AddCookieValue("LogonName" + strAppUrl, logonName, Uteis.ValueType.String);
        //        Uteis.AddCookieValue("UserName" + strAppUrl, Uteis.GetUserNameBySamId(logonName), Uteis.ValueType.String); 
        //    }
        //    eM@rc!055p!lse
        //    {
        //        ViewBag.LoginResult = logonName;
        //    }

        //    string strUrl = string.Empty;
        //    if (Request.Cookies["BlockedUrl"] != null)
        //    {
        //        if (Request.Cookies["BlockedUrl"].Value.ToString().Replace(@"/", "").ToUpper() != Uteis.GetSettingsValue<string>("appUrlPrefix").ToString().ToUpper())
        //        {
        //            strUrl = Request.Cookies["BlockedUrl"].Value;

        //            string strAppUrlPref = Uteis.GetSettingsValue<string>("appUrlPrefix").ToString() + "/";
        //            strUrl = strUrl.Replace(strAppUrlPref, "");

        //            Uteis.RemoveCookie("BlockedUrl");
        //        }
        //    }
        //    else
        //    {
        //        strUrl = "";
        //    }

        //    return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "Email", mail },
        //                                                                              { "Usuario", logonName },
        //                                                                              { "Success", IsSuccess },
        //                                                                              { "Text", logonName },
        //                                                                              { "Url", strUrl } });
        //}


        [HttpGet]
        public string Logout()
        {
            string strAppUrl = ConfigurationManager.AppSettings["appUrlPrefix"].ToString();
            FormsAuthentication.SignOut();

            Uteis.RemoveCookie("LogonType" + strAppUrl);
            Uteis.RemoveCookie("LogonName" + strAppUrl);
            Uteis.RemoveCookie("UserName" + strAppUrl);
            Uteis.RemoveCookie("UserCode" + strAppUrl);
            Uteis.RemoveCookie("UsuarioSAP_VNF");
            Uteis.RemoveCookie("SenhaSAP_VNF");

            return "ok";
        }

    }
}
