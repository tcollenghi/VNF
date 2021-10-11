using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MetsoFramework.Utils;
using VNF.Business;
using VNF.Portal.Util;
using VNF.Portal.Models;
using System.Data;
using VNF.Portal.DataLayer;
using MetsoFramework.Core;
using System.Data.Entity;

namespace VNF.Portal.Controllers
{
    public class TalonarioController : Controller
    {
        //
        // GET: /ProcessamentoFiscal/
        DLTalonario dal = new DLTalonario();
        DLTalonarioItem idal = new DLTalonarioItem();
        bool HeaderSalvo = false;

        [HttpGet]
        public ActionResult Index()
        {
            var lista = dal.GetAll();
            //this.LoadGrid("", "", "");
            return View(lista);
        }

        //////////[HttpGet]
        ////////public ActionResult Edit()
        ////////{

        ////////    ViewBag.UnidadeMetso = CarregarUnidadeMetso();
        ////////    ViewBag.Situacao = CarregarSituacaoNf();
        ////////    ViewBag.StatusIntegracao = CarregarStatusIntegracao();
        ////////    ViewBag.TipoData = CarregarData();
        ////////    return View();
        ////////}


        [HttpGet]
        public ActionResult Edit(int Id = 0)
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("TALN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|TALN";

            //ViewBag.UnidadeMetso = CarregarUnidadeMetso();
            //ViewBag.Finalizado = CarregarSatusTalonario();
            //ViewBag.StatusIntegracao = CarregarStatusIntegracao();
            //ViewBag.TipoData = CarregarData();
            ViewBag.StatusIntegracao = StatusIntegracaoSAP(Id);
            Talonario t = dal.GetByID(Id);
            if (t == null) t = new Talonario();
            ViewBag.ddlTipoDocumento = getTipoDocumento(t.TipoDocumento);
            if (t == null) t = new Talonario();
            //Verifica se o formulário foi chamdo pelo modo de modificação, caso sim oculta o grid de itens para obrigar o usuário a clicar no botão salvar
            if (Request.Params.Get("Modificar") != null && Request.Params.Get("Modificar") == "true")
            {
                t.Finalizado = false;
                var HeaderSalvo = TempData["HeaderSalvo"];
                if (HeaderSalvo != null)
                {
                    if (HeaderSalvo.ToString() == "True")
                    {
                        ViewBag.SalvarHeader = true;
                    }
                }
                
                
            }
            return View(t);
        }

        [HttpGet]
        public ActionResult CadastroManual()
        {
            if ((new VNF.Business.BLAcessos()).ConsultaAcesso("TALN", MetsoFramework.Utils.Uteis.LogonName()) == false) ViewBag.Acesso = "false|TALN";

            //ViewBag.UnidadeMetso = CarregarUnidadeMetso();
            //ViewBag.Finalizado = CarregarSatusTalonario();
            //ViewBag.StatusIntegracao = CarregarStatusIntegracao();
            //ViewBag.TipoData = CarregarData();
           // var lista = dal.LoadGrid("", "", "", "", "", "", "", "");
            return View();
        }


        [HttpPost]
        public void AdicionaItem(TalonarioItem item)
        {

            if (idal.db.TalonarioItem.Any(a => a.IdTalonarioItem.Equals(item.IdTalonarioItem)))
            {
                idal.Update(item);
            }
            else
            {
                idal.Insert(item);
            }
            idal.Save();
        }

        [HttpPost]
        [AllowAnonymous]
        public void RemoveItem(int Id)
        {
            idal.Delete(Id);
            idal.Save();
        }

        [HttpPost]
        public ActionResult ExcluirTalonario(int Id)
        {
            dal.db.SP_DELETE_TALONARIO(Id);
            dal.Save();
            return null;
        }

        [HttpGet]
        [AllowAnonymous]
        public string CarregaItens(int IdTalonario)
        {
            Talonario t = dal.GetByID(IdTalonario);
            string html = "";

            foreach (TalonarioItem i in t.TalonarioItem)
            {
                //html += "<tr onclick=\"selectRow('@IdTalonarioItem','@i.Descricao', '@i.Pedido', '@i.ItemPedido', '@i.Quantidade', '@i.ValorUnitario', '@i.ValorTotal', '@i.CFOP', '@i.ICMS', '@i.IPI', '@i.ICMSAliq', '@i.FRETE', '@i.SEGURO', '@i.OUTRASDESPESAS', '@i.DESCONTO')\">";
                html += "<tr onclick=\"selectRow('" + i.IdTalonarioItem + "','" + i.Descricao + "', '" + i.Pedido + "', '" + i.ItemPedido + "', '" + i.Quantidade + "', '" + i.ValorUnitario + "', '" + i.ValorTotal + "', '" + i.CFOP + "', '" + i.ICMS + "', '" + i.IPI + "', '" + i.ICMSAliq + "', '" + i.FRETE + "', '" + i.SEGURO + "', '" + @i.OUTRASDESPESAS + "', '" + i.DESCONTO + "')\">";
                html += "<td>" + i.Descricao + "</td>";
                html += "<td>" + Convert.ToDecimal(i.Quantidade).ToString("N2") + "</td>";
                html += "<td>" + Convert.ToDecimal(i.ValorUnitario).ToString("C2") + "</td>";
                html += "<td>" + Convert.ToDecimal(i.ValorTotal).ToString("C2") + "</td>";
                html += "<td width=\"2%\">";
                html += "<button type=\"button\" class=\"btn btn-xs btn-danger\" title=\"Remover\" onclick=\"Remover(" + i.IdTalonarioItem.ToString() + ")\">";
                html += "<i class=\"fa fa-trash-o\"></i> Remover";
                html += "</button>";
                html += "</td>";
                html += "</tr>";
                html += "";
            }

            return html;
        }

        [HttpPost]
        public ActionResult CadastroManual(Talonario t, FormCollection pFormCollection)
        {
            try
            {
                //Anexo específico de algum campo
                dal.db.Talonario.Attach(t);
                dal.db.Entry(t).Property(x => x.Observacao).IsModified = true;
                dal.db.Entry(t).Property(x => x.CNPJMetso).IsModified = true;
                dal.db.Entry(t).Property(x => x.IE_Metso).IsModified = true;
                dal.db.Entry(t).Property(x => x.RazaoSocialMetso).IsModified = true;
                dal.db.Entry(t).Property(x => x.CNPJEmitente).IsModified = true;
                dal.db.Entry(t).Property(x => x.IE_Emitente).IsModified = true;
                dal.db.Entry(t).Property(x => x.RazaoSocialEmitente).IsModified = true;
                dal.db.Entry(t).Property(x => x.Vencimento).IsModified = true;
                dal.db.Entry(t).Property(x => x.ValorTotal).IsModified = true;
                dal.db.Entry(t).Property(x => x.TipoDocumento).IsModified = true;
                dal.db.Entry(t).Property(x => x.Finalizado).IsModified = true;
                dal.db.Configuration.ValidateOnSaveEnabled = false;

                bool? FinalizadoModoEdit = t.Finalizado;
                if (!t.Finalizado == true || t.Finalizado == null)
                {
                    t.Finalizado = false;
                }
                
                
                t.NumeroDocumento = t.NumeroDocumento.TrimStart(new Char[] { '0' });
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    if (Request.Files[i].ContentLength > 0)
                    {
                        dal.db.Entry(t).Property(x => x.Anexo).IsModified = true;
                        dal.db.Entry(t).Property(x => x.AnexoExtensao).IsModified = true;
                        dal.db.Entry(t).Property(x => x.AnexoNome).IsModified = true;
                        t.Anexo = Request.Files[i].InputStream.ToByteArray();
                        t.AnexoExtensao = Request.Files[i].ContentType;
                        t.AnexoNome = Request.Files[i].FileName.Remove(0, Request.Files[i].FileName.LastIndexOf("\\") + 1);

                    }
                    else
                    {
                        dal.db.Entry(t).Property(x => x.Anexo).IsModified = false;
                        dal.db.Entry(t).Property(x => x.AnexoExtensao).IsModified = false;
                        dal.db.Entry(t).Property(x => x.AnexoNome).IsModified = false;
                    }
                }

                //Gera a chave de acesso
                if (String.IsNullOrEmpty(t.ChaveAcesso))
                {
                    string Serie = "";
                    if (!String.IsNullOrEmpty(t.Serie))
                        Serie = t.Serie;
                    t.ChaveAcesso = t.CNPJEmitente.Replace(".", "").Replace("-", "").Replace("/", "") + t.NumeroDocumento.PadLeft(10, '0') + Serie.PadLeft(5, '0') + Convert.ToDateTime(t.DataEmissao).ToString("ddMMyyyy");
                }

                if (t.IdTalonario == 0)
                {
                    //Verifica se a chave de acesso ja existe no talonario
                    Talonario talonario = dal.GetByChave(t.ChaveAcesso);
                    if (talonario == null)
                    {
                        dal.Insert(t);
                    }
                    else
                    {
                        ViewBag.ddlTipoDocumento = getTipoDocumento(t.TipoDocumento);
                        TempData["TalonarioExistente"] = "Talonário já existe na base de dados, favor verifique as informações. Obrigado!";
                        return View("Edit", t);
                    }
                }

                TempData["HeaderSalvo"] = true;
                dal.db.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return RedirectToAction("Edit", new { Id = t.IdTalonario, Modificar = "true" });
        }


        [HttpPost]
        public ActionResult FinalizaTalonario(int Id)
        {
            //Finaliza o talonário e gera a classe modNF e salva no banco de dados.
            Talonario t = dal.GetByID(Id);

          
            modNF NF = new modNF();
            NF.NF_IDE_NNF = t.NumeroDocumento;
            NF.NF_IDE_SERIE = t.Serie;

            //Gera a chave de acesso
            if (!String.IsNullOrEmpty(t.ChaveAcesso))
            {
                NF.VNF_CHAVE_ACESSO = t.ChaveAcesso;
            }
            else
            {
                NF.VNF_CHAVE_ACESSO = t.CNPJEmitente.Replace(".", "").Replace("-", "").Replace("/", "") + t.NumeroDocumento.PadLeft(10, '0') + t.Serie.PadLeft(5, '0') + Convert.ToDateTime(t.DataEmissao).ToString("ddMMyyyy");
            }
            NF.NF_EMIT_CNPJ = t.CNPJEmitente.RemoveLetters();
            NF.NF_EMIT_IE = t.IE_Emitente.Trim();
            NF.NF_EMIT_XNOME = t.RazaoSocialEmitente;
            NF.NF_DEST_CNPJ = t.CNPJMetso.RemoveLetters();
            NF.NF_DEST_IE = string.IsNullOrWhiteSpace(t.IE_Metso) ?  "" : t.IE_Metso.Trim();
            NF.NF_DEST_XNOME = t.RazaoSocialMetso;
            NF.NF_IDE_DHEMI = Convert.ToDateTime(t.DataEmissao);
            NF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario;
            NF.VNF_CLASSIFICACAO = t.TipoDocumento.ToUpper();
            NF.VNF_CODIGO_VERIFICACAO = t.CodigoVerificacao;
            NF.VNF_DANFE_ONLINE = t.LinkDocumento;

            if (t.Anexo != null)
            {
                NF.VNF_ANEXO = t.Anexo;
                NF.VNF_ANEXO_EXTENSAO = t.AnexoExtensao;
                NF.VNF_ANEXO_NOME = t.AnexoNome;
            }
            NF.NF_OUTROS_INFORMACAO_ADICIONAL = t.Observacao;
            NF.NF_ICMSTOT_VNF = Convert.ToDecimal(t.ValorTotal);
            NF.VNF_STATUS = NF.VNF_CLASSIFICACAO == modNF.tipo_tal_compra ? "PENDENTE" : "ACEITA";

            modNFDuplicata objDuplicata = new modNFDuplicata();
            objDuplicata.NF_COBR_DUP_DVENC = Convert.ToDateTime(t.Vencimento);
            if (NF.DUPLICATAS == null) NF.DUPLICATAS = new List<modNFDuplicata>();
            NF.DUPLICATAS.Add(objDuplicata);

            //Pega os itens da nota
            NF.ITENS_NF = new List<modNFItem>();
            int indexItem = 0;
            foreach (TalonarioItem i in t.TalonarioItem)
            {
                indexItem++;

                modNFItem it = new modNFItem();
                it.NF_PROD_ITEM = indexItem;
                it.NF_PROD_XPED = i.Pedido;
                it.NF_PROD_NITEMPED = Convert.ToInt32(i.ItemPedido);
                it.NF_PROD_CPROD = i.Codigo;
                it.NF_PROD_XPROD = i.Descricao;
                it.NF_PROD_NCM = i.NCM;
                it.NF_PROD_QCOM = Convert.ToDecimal(i.Quantidade);
                it.NF_PROD_QTRIB = Convert.ToDecimal(i.Quantidade);
                it.NF_PROD_VUNCOM = Convert.ToDecimal(i.ValorUnitario);
                it.NF_PROD_VPROD = Convert.ToDecimal(i.ValorTotal);
                it.NF_PROD_CFOP = i.CFOP;
                it.NF_ICMS_VICMS = i.ICMS == null ? 0 : Convert.ToDecimal(i.ICMS);
                it.NF_IPI_VIPI = i.IPI == null ? 0 : Convert.ToDecimal(i.IPI);
                it.NF_PROD_VFRETE = i.FRETE == null ? 0 : Convert.ToDecimal(i.FRETE);
                it.NF_PROD_VSEG = i.SEGURO == null ? 0 : Convert.ToDecimal(i.SEGURO);
                it.NF_PROD_VDESC = i.DESCONTO == null ? 0 : Convert.ToDecimal(i.DESCONTO);
                it.NF_PROD_VOUTRO = i.OUTRASDESPESAS == null ? 0 : Convert.ToDecimal(i.OUTRASDESPESAS);
              //  it.VNF_CODJUN = Convert.ToInt32(modSQL.ExecuteScalar("select codjun from tbjun where nfeid = '" + t.ChaveAcesso + "'")); //marcio wwwwww
                it.VNF_ITEM_VALIDO = NF.VNF_CLASSIFICACAO == modNF.tipo_tal_compra ? "N" : "X";
                it.NF_ICMS_PICMS = i.ICMSAliq == null ? 0 : Convert.ToDecimal(i.ICMSAliq);
                NF.ITENS_NF.Add(it);
            }

            modVerificar modVer = new modVerificar();
            modVer.GravarDadosNF(NF, true, Uteis.LogonName());

            t.Finalizado = true;
            dal.Update(t);
            dal.Save();

            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.InserirTalonario, "Cadastrou o talonário número " + NF.NF_IDE_NNF, NF.VNF_CHAVE_ACESSO);

            ModelState.Clear();
            return null;
        }


        [HttpGet]
        public string GetRazaoSocialIEFornecedor(string cnpj)
        {
            string vStrRazsocFor = string.Empty;
            string vStrIEFor = string.Empty;
            BLVendors objBLVendors = new BLVendors();
            modVendors objVendor = objBLVendors.GetByCNPJ(cnpj).FirstOrDefault();

            if (objVendor != null)
            {
                vStrRazsocFor = objVendor.Name1 + objVendor.Name2;
                vStrIEFor = objVendor.Taxnumber3;
            }

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "RazsocFor", vStrRazsocFor }, 
                                                                                      { "IEFor", vStrIEFor}        });
            
        }

        [HttpGet]
        public string GetRazaoSocialIEMetso(string cnpj)
        {
            DataTable dttMetso = new DataTable();
            string vStrRazsocMetso = string.Empty;
            string vStrIEMetso = string.Empty;

            dttMetso = modSQL.Fill("SELECT planta, cnpj, ie, descricao FROM tbplantacnpj WHERE cnpj = '" + cnpj.RemoveLetters() + "'");
          
            if (dttMetso.Rows.Count > 0)
            {
                vStrRazsocMetso = dttMetso.Rows[0]["descricao"].ToString();
                vStrIEMetso = dttMetso.Rows[0]["IE"].ToString();
            }


            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "RazsocMetso", vStrRazsocMetso }, 
                                                                                      { "IEMetso", vStrIEMetso}        });

        }


        #region Combos 
        private List<SelectListItem> getClassificacao(string SelectedValue = "")
        {
            List<SelectListItem> cl = new List<SelectListItem>();
            cl.Add(new SelectListItem() { Text = "Teste", Value = "Teste", Selected = (SelectedValue == "Teste") });
            return cl;
        }

        private List<SelectListItem> getTipoDocumento(string SelectedValue = "")
        {
            List<SelectListItem> cl = new List<SelectListItem>();
            cl.Add(new SelectListItem() { Text = "Compra", Value = "Compra", Selected = (SelectedValue == "Compra") });
            cl.Add(new SelectListItem() { Text = "Remessa", Value = "Remessa", Selected = (SelectedValue == "Remessa") });
            return cl;
        }
        #endregion

        [HttpGet]
        public ActionResult DownloadAnexo(int id, string fieldName = null)
        {
            byte[] file = null;
            string fileExtension = null;
            string fileName = null;

            if (fieldName != null)
            {
                Talonario pf = dal.GetByID(id);
                file = pf.Anexo;
                fileExtension = pf.AnexoExtensao;
                fileName = pf.AnexoNome;
            }
            return File(file, fileExtension, fileName);
        }

        [HttpGet]
        public ActionResult DownloadAnexoChave(string id)
        {
            byte[] file = null;
            string fileExtension = null;
            string fileName = null;
            string mimeType = "application/pdf";

            DLTalonario objDLTalonario = new DLTalonario();
            Talonario pf = objDLTalonario.GetByChave(id);
            file = pf.Anexo;
            fileExtension = pf.AnexoExtensao;
            fileName = pf.AnexoNome;

            Response.AppendHeader("Content-Disposition", "inline; filename=" + fileName);
            return File(file, mimeType);
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
        public List<SelectListItem> CarregarSatusTalonario()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "1", Text = "SIM" });
            lista.Add(new SelectListItem { Value = "0", Text = "NAO" });
            return lista;
        }

        [HttpGet]
        public List<SelectListItem> CarregarStatusIntegracao()
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            DataTable dttTipoFrete = new DataTable();
            dttTipoFrete = objBLNotaFiscal.GetStatusIntegracao();

            List<SelectListItem> lista = new List<SelectListItem>();
            foreach (DataRow dtrLinha in dttTipoFrete.Rows)
            {
                lista.Add(new SelectListItem { Value = dtrLinha["STATUS_INTEGRACAO"].ToString().RemoveAccents(true), Text = dtrLinha["STATUS_INTEGRACAO"].ToString() });
            }

            return lista;
        }

        [HttpGet]
        public List<SelectListItem> CarregarData()
        {
            List<SelectListItem> lista = new List<SelectListItem>();

            lista.Add(new SelectListItem { Value = "E", Text = "Emissão da NFe", Selected = true });
            lista.Add(new SelectListItem { Value = "C", Text = "Chegada da DANFE" });
            lista.Add(new SelectListItem { Value = "I", Text = "Integração SAP" });


            return lista;
        }

        [HttpGet]
        public PartialViewResult LoadGrid(string pStrNumeroDocumento, string pStrSerie, string pStrCNPJEmitente, string pStrCNPJMetso, string pStrFinalizado, string pStrStatusIntegracao, string pStrDataDe, string pStrDataAte)
        {
            var vArrTalonario =  dal.LoadGrid(pStrNumeroDocumento, pStrSerie, pStrCNPJEmitente, pStrCNPJMetso, pStrFinalizado, pStrStatusIntegracao, pStrDataDe, pStrDataAte);
            return PartialView("GridTalonario", vArrTalonario);
        }

        [HttpGet]
        public string StatusIntegracaoSAP(int Id)
        {

            if (Id > 0)
            {
                var retorno = (from p in dal.db.vwTALONARIO
                               where p.IdTalonario.Equals(Id)
                               select p.STATUS_INTEGRACAO).ToList();
                if (retorno[0] != null)
                {
                    return retorno[0].ToString();
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

    }
}
