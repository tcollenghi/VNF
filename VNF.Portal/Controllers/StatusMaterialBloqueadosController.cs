using MetsoFramework.Utils;
using System.Collections.Generic;
using System.Web.Mvc;
using VNF.Portal.DataLayer;
using VNF.Portal.Models;

namespace VNF.Portal.Controllers
{
    public class StatusMaterialBloqueadosController : Controller
    {
        DLStatusMaterialBloqueado dal = new DLStatusMaterialBloqueado();

        public ActionResult Index()
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";

            var lista = dal.Get();
            return View(lista);
        }
       
        public string Salvar(string smb_descricao)
        {
            if (!string.IsNullOrEmpty(smb_descricao))
            {
                TbStatusMateriaisBloqueados objSmb = new TbStatusMateriaisBloqueados();
                objSmb.id_status_material_bloqueado = 0;
                objSmb.smb_descricao = smb_descricao;
                dal.Insert(objSmb);
                dal.Save();

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Status Material Bloqueado realizada com sucesso"}
                    });
            }

            return null;
        }
      
        public string Editar(int id_status_material_bloqueado, string smb_descricao)
        {
            if (!string.IsNullOrEmpty(id_status_material_bloqueado.ToString()) && !string.IsNullOrEmpty(smb_descricao))
            {
                TbStatusMateriaisBloqueados objSmb = new TbStatusMateriaisBloqueados();
                objSmb.id_status_material_bloqueado = id_status_material_bloqueado;
                objSmb.smb_descricao = smb_descricao;
                dal.Update(objSmb);
                dal.Save();

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Status Material Bloqueado realizada com sucesso"}
                    });
            }

            return null;
        }

        public string Excluir(int id_status_material_bloqueado)
        {
            if (!string.IsNullOrEmpty(id_status_material_bloqueado.ToString()))
            {

                dal.Delete(id_status_material_bloqueado);
                dal.Save();

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Status Material Bloqueado realizada com sucesso"}
                    });
            }

            return null;
        }

    }
}
