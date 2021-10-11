using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc; 
using VNF.Portal.Models;
using VNF.Portal.DataLayer;

namespace VNF.Portal.Controllers
{
    public class GroupsController : Controller
    {
        DLGroups dal = new DLGroups();
        DLGroupUsers idal = new DLGroupUsers();


        public ActionResult Index()
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            return View(dal.Get());
        }

        [HttpGet]
        public ActionResult Edit(int Id = 0)
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            Groups g = dal.GetByID(Id);
            if (g == null) g = new Groups();
            return View(g);
        }

        [HttpPost]
        public ActionResult Edit(Groups g)
        {
            if(g.IdGroup == 0)
            {
                dal.Insert(g);
            }
            else
            {
                dal.Update(g);
            }
            dal.Save();
            return RedirectToAction("Edit", new { Id = g.IdGroup });
        } 

        [HttpPost]
        public void AdicionaItem(int IdGroup, string LoginName)
        {
            GroupUsers gu = new GroupUsers();
            gu.IdGroup = IdGroup;
            gu.LoginName = LoginName;
            idal.Insert(gu);
            idal.Save();
        }

        [HttpPost]
        public void RemoveItem(int Id)
        {
            idal.Delete(Id);
            idal.Save();
        }

        [HttpGet]
        public string GetUsers(int Id)
        {
            string html = "";
            Groups g = dal.GetByID(Id);
            foreach(var i in g.GroupUsers)
            {
                html += "<tr>";
                html += "<td>" + i.LoginName + "</td>";
                html += "<td width=\"3%\">";
                html += "<button type=\"button\" title=\"Remover\" class=\"btn btn-xs btn-danger\" onclick=\"RemoveItem(" + i.IdGroupUser.ToString() + ")\">";
                html += "<i class=\"fa fa-trash-o\"></i>";
                html += "</button>";
                html += "</td>";
                html += "</tr>";
            }
            return html;
            
        }

    }
}
