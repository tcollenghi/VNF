using System.Web;
using System.Web.Mvc;
using VNF.Portal.Filters;

namespace VNF.Portal
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new ControleAcesso());
        }
    }

}