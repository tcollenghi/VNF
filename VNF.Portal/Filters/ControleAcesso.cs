using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using MetsoFramework.Utils;
using VNF.Portal.DataLayer;
using System.Configuration;

namespace VNF.Portal.Filters
{
    class ControleAcesso : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            filterContext.HttpContext.Response.Cache.SetValidUntilExpires(false);
            filterContext.HttpContext.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            filterContext.HttpContext.Response.Cache.SetNoStore();
            VerifyAccess(filterContext);
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {

        }
                
        public void VerifyAccess(ActionExecutingContext filterContext)
        {
            var originalPath = filterContext.RequestContext.HttpContext.Request.Path;
            var controllerName = filterContext.Controller.ToString().Substring(filterContext.Controller.ToString().LastIndexOf(".", StringComparison.Ordinal) + 1).Replace("Controller", "").Trim().ToLower();
            var actionName = filterContext.ActionDescriptor.ActionName.Trim().ToLower();

            var UrlBase = HttpContext.Current.Request.Url.ToString().Replace("http://", "");
            UrlBase = "http://" + UrlBase.Remove(UrlBase.IndexOf("/"));
            var UrlPage = new UrlHelper(filterContext.RequestContext);
            var urlRedirect = UrlBase + UrlPage.Action("Login", "User");
            string urlHome = UrlBase + UrlPage.Action("Index", "Home");

            //Se for para fazer o login na página, autorizar...
            if (controllerName.ToLower() == "user" && actionName.ToLower() == "login" && HttpContext.Current.Request.Url.ToString().Contains("?"))
            {
                string strLoginPage = HttpContext.Current.Request.Url.ToString();
                strLoginPage = strLoginPage.Remove(strLoginPage.IndexOf("?"));
                filterContext.Result = new RedirectResult(strLoginPage);
            }
            else if (controllerName.ToLower() != "user" || (actionName.ToLower() != "login" && actionName.ToLower() != "loginfornecedor" && actionName.ToLower() != "loginmetso"))
            {
                //Verifica se o usuário já passou pela página de autenticação
                string LogonName = "LogonName" + ConfigurationManager.AppSettings["appUrlPrefix"].ToString();
                string UserName = "UserName" + ConfigurationManager.AppSettings["appUrlPrefix"].ToString();
                if ((controllerName != "user") && (filterContext.RequestContext.HttpContext.Request.Cookies[LogonName] == null || filterContext.RequestContext.HttpContext.Request.Cookies[UserName] == null))
                {
                    filterContext.Result = new RedirectResult(urlRedirect);
                }
                else
                {
                    //filterContext.Result = new RedirectResult(urlHome);
                }


                if (filterContext.Result != null)
                {
                    //Após o login, o usuário deve ser direcionado para a URL que foi bloqueada
                    if (filterContext.RequestContext.HttpContext.Request.Cookies["BlockedUrl"] == null && originalPath.Replace(Uteis.GetSettingsValue<string>("appUrlPrefix").ToString(), "").Length > 2)
                    {
                        string strUrlToRedirect = originalPath.Replace(Uteis.GetSettingsValue<string>("appUrlPrefix").ToString() + "/", "");
                        if (strUrlToRedirect.StartsWith("/"))
                            strUrlToRedirect = strUrlToRedirect.Substring(1, strUrlToRedirect.Length - 1);

                        string strAppUrlPref = Uteis.GetSettingsValue<string>("appUrlPrefix").ToString() + "/";
                        strUrlToRedirect = strUrlToRedirect.Replace(strAppUrlPref, "");

                        Uteis.AddCookieValue("BlockedUrl", strUrlToRedirect, Uteis.ValueType.String);
                    }

                    string returnUrl = filterContext.Result.ToString();
                    if (string.IsNullOrEmpty(returnUrl) && HttpContext.Current.Request.UrlReferrer != null)
                        returnUrl = HttpContext.Current.Server.UrlEncode(HttpContext.Current.Request.UrlReferrer.PathAndQuery);

                    if (UrlPage.IsLocalUrl(returnUrl) && !string.IsNullOrEmpty(returnUrl))
                    {
                        filterContext.Controller.ViewBag.ReturnURL = returnUrl;
                    }
                }

                //if (!string.IsNullOrEmpty(objBlUsers.GetCurrentUser(filterContext)))
                //{
                //    var userlog = new BLUserLog();
                //    var destinPath = filterContext.RequestContext.HttpContext.Request.Path;
                //    var logPath = accessDenied ? originalPath + " -> " + destinPath : destinPath;
                //    userlog.Save(new UserLog
                //    {
                //        UserIp = filterContext.RequestContext.HttpContext.Request.UserHostAddress,
                //        date = DateTime.Now,
                //        path = logPath,
                //        User = objBlUsers.GetCurrentUser(filterContext)
                //    });
                //}
            }

        }

        //public void VerifyAccess(ActionExecutingContext filterContext)
        //{

        //    var controllerName = filterContext.Controller.ToString().Substring(filterContext.Controller.ToString().LastIndexOf(".", StringComparison.Ordinal) + 1).Replace("Controller", "");
        //    var objBlUsers = new BLUsers();
        //    var oDlPage = new DLPage();

        //    if (controllerName != "Menu" && controllerName != "Error" && controllerName != "Users")
        //    {
        //        if (objBlUsers.AdLogin() && filterContext.RequestContext.HttpContext.Request.Cookies["user"] == null)
        //            filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Users" }, { "action", "Login" } });

        //        //Se nao for autenticado pelo ad 
        //        if (filterContext.RequestContext.HttpContext.Request.Cookies["user"] != null)
        //        {
        //            var arrPages = oDlPage.GetUserAccessOnPageFornecedor(controllerName);
        //            if (arrPages == null || !arrPages.Any())
        //                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Users" }, { "action", "Login" } });
        //        }
        //        else
        //        {
        //            if (!objBlUsers.AdLogin())
        //            {
        //                var arrPages = oDlPage.GetUserAccessOnPage(Uteis.LogonName(), controllerName);

        //                if (arrPages == null || !arrPages.Any())
        //                    filterContext.Result =
        //                        new RedirectToRouteResult(new RouteValueDictionary
        //                        {
        //                            {"controller", "Home"},
        //                            {"action", "NoAccess"}
        //                        });
        //            }
        //            else
        //                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Users" }, { "action", "Login" } });
        //        }
        //    }

        //    if (!string.IsNullOrEmpty(objBlUsers.GetCurrentUser(filterContext)))
        //    {
        //        var userlog = new BLUserLog();
        //        userlog.Save(new UserLog
        //        {
        //            UserIp = filterContext.RequestContext.HttpContext.Request.UserHostAddress,
        //            date = DateTime.Now,
        //            path = filterContext.RequestContext.HttpContext.Request.Path,
        //            User = objBlUsers.GetCurrentUser(filterContext)
        //        });
        //    }
        //}
    }
}
