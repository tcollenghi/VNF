using MetsoFramework.Utils;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web.Mvc;
using VNF.Portal.DataLayer;
using VNF.Portal.Models;

namespace VNF.Portal.Controllers
{
    public class ValidacoesController : Controller
    {
        DLValidacoes dal = new DLValidacoes();
        DLValidacoesExcecoes dales = new DLValidacoesExcecoes();

        public ActionResult Index()
        {
            //Marcio Spinosa - 29/11/2018 - CR00009165
            //if ((new VNF.Business.BLAcessos()).ConsultaAcesso("ADMN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|ADMN";
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("VALI", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|VALI";
            //Marcio Spinosa - 29/11/2018 - CR00009165 - Fim
            var lista = dal.Get();
            ViewBag.Excecao = CarregarListaExcecoes();
            return View(lista);
        }

        [HttpGet]
        public List<SelectListItem> CarregarListaExcecoes()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "CFOP", Text = "CFOP" });
            lista.Add(new SelectListItem { Value = "DEPOSITO", Text = "Planta + Depósito" });
            lista.Add(new SelectListItem { Value = "MATERIAL", Text = "Material" });

            return lista;
        }

        public string Salvar(string pValcodigo, string pValtitulousuario, string pValtextoreprovacao)
        {
            if (!string.IsNullOrEmpty(pValcodigo) && !string.IsNullOrEmpty(pValtitulousuario.ToString()) && !string.IsNullOrEmpty(pValtextoreprovacao.ToString()))
            {
                TbValidacoes objVal = new TbValidacoes();
                objVal.val_codigo = pValcodigo;
                objVal.val_titulo_usuario = pValtitulousuario;
                objVal.val_texto_reprovacao = pValtextoreprovacao;

                dal.Insert(objVal);
                dal.Save();

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Validação realizada com sucesso"}
                    });
            }

            return null;
        }

        public string Editar(int pId, bool pCheck, string pColumn)
        {
            if (pCheck)
                pCheck = false;
            else
                pCheck = true;

            TbValidacoes objVal = dal.GetByID(pId);

            if (pColumn == "notificar_compras")
                objVal.val_notificar_compras = pCheck;
            if (pColumn == "notificar_fornecedor")
                objVal.val_notificar_fornecedor = pCheck;
            if (pColumn == "anulacao_compras")
                objVal.val_permitir_anulacao_compras = pCheck;
            if (pColumn == "anulacao_fiscal")
                objVal.val_permitir_anulacao_fiscal = pCheck;
            //Marcio Spinosa - 11/09/2018 - CRXXXXXX
            if (pColumn == "validar_nfe")
                objVal.val_validar_nfe = pCheck;
            if (pColumn == "validar_cte")
                objVal.val_validar_cte = pCheck;
            if (pColumn == "validar_tal")
                objVal.val_validar_tal = pCheck;
            if (pColumn == "validar_tcom")
                objVal.val_validar_tcom = pCheck;
            if (pColumn == "validar_fat")
                objVal.val_validar_fat = pCheck;
            if (pColumn == "validar_nfs")
                objVal.val_validar_nfs = pCheck;
            //Marcio Spinosa - 11/09/2018 - CRXXXXXX - Fim
            dal.Update(objVal);
            dal.Save();

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Validação realizada com sucesso"}
                    });

        }

        public string Excluir(int pid)
        {
            var objValEx = dales.GetByIDValidacaoes(pid);
            if (objValEx.Count > 0 && objValEx != null)
                foreach (var item in objValEx)
                {
                    dales.Delete(item.id_validacao_excecao);
                    dales.Save();
                }

            dal.Delete(pid);
            dal.Save();
            return null;
        }

        public string GetValidacoesExecoes(int id, string pStrExecType)
        {
            var obj = dales.GetByIDValidacaoes(id);
            StringBuilder tabela = new StringBuilder();
            StringBuilder tabelaExeDeposito = new StringBuilder();
           
            obj.ForEach(x =>
            {
                switch (pStrExecType)
                {
                    case "CFOP":
                        if (!string.IsNullOrWhiteSpace(x.vex_cfop))
                        {
                            tabela.Append("<tr id=\"" + x.id_validacao_excecao + "\">");
                            tabela.Append("<td width='90%'>");
                            tabela.Append(x.vex_cfop);
                            tabela.Append("</td>");
                            tabela.Append("<td>");
                            tabela.Append("<a onclick=\"ExcluirExcecoes('" + x.id_validacao_excecao + "','" + x.vex_id_validacao + "')\" href=\"#\" class=\"btn btn-danger btn-xs\"><i class=\"fa fa-times\"></i></a>");
                            tabela.Append("</td>");
                            tabela.Append("</tr>");
                        }
                        break;
                    case "DEPOSITO":
                        if (!string.IsNullOrWhiteSpace(x.vex_deposito))
                        {
                            tabela.Append("<tr id=\"" + x.id_validacao_excecao + "\">");
                            tabela.Append("<td width='90%'>");
                            tabela.Append(x.vex_deposito);
                            tabela.Append("</td>");
                            tabela.Append("<td>");
                            tabela.Append("<a onclick=\"ExcluirExcecoes('" + x.id_validacao_excecao + "','" + x.vex_id_validacao + "')\" href=\"#\" class=\"btn btn-danger btn-xs\"><i class=\"fa fa-times\"></i></a>");
                            tabela.Append("</td>");
                            tabela.Append("</tr>");
                        }
                        break;
                    case "MATERIAL":
                        if (!string.IsNullOrWhiteSpace(x.vex_cod_material))
                        {
                            tabela.Append("<tr id=\"" + x.id_validacao_excecao + "\">");
                            tabela.Append("<td width='90%'>");
                            tabela.Append(x.vex_cod_material);
                            tabela.Append("</td>");
                            tabela.Append("<td>");
                            tabela.Append("<a onclick=\"ExcluirExcecoes('" + x.id_validacao_excecao + "','" + x.vex_id_validacao + "')\" href=\"#\" class=\"btn btn-danger btn-xs\"><i class=\"fa fa-times\"></i></a>");
                            tabela.Append("</td>");
                            tabela.Append("</tr>");
                        }
                        break;
                }

                



            });
            return Serialization.JSON.CreateString(new Dictionary<string, object>()
                {
                    { "result", "Ok" },
                    { "data", tabela.ToString()}
                }
            );
        }

        public string SalvarExcecoes(int pid, string pStrValExcept, string pStrExecType)
        {
            TbValidacoesExcecoes objVal = new TbValidacoesExcecoes();

            objVal.id_validacao_excecao = 0;
            objVal.vex_id_validacao = pid;

            switch (pStrExecType)
            {
                case "CFOP":
                    objVal.vex_cfop = pStrValExcept;
                    break;
                case "DEPOSITO":
                    objVal.vex_deposito = pStrValExcept;
                    break;
                case "MATERIAL":
                    objVal.vex_cod_material = pStrValExcept;
                    break;
            }


            dales.Insert(objVal);
            dales.Save();

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Cadastro realizada com sucesso"}
                    });

        }

        public string ExcluirExcecoes(int pid)
        {
            dales.Delete(pid);
            dales.Save();
            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Exclusão realizada com sucesso"}
                    });
        }
    }
}
