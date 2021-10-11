using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VNF.Business;
using VNF.Portal.Models;
using MetsoFramework.SAP;
using System.Text;
using System.IO;
using MetsoFramework.Utils;
using System.Data;
using VNF.Portal.DataLayer;

namespace VNF.Portal.Controllers
{
    public class NotaManual_J1B1NController : Controller
    {
        //
        // GET: /NotaManual_J1B1N/

        public ActionResult NotaManual(string pNFEID, string pStrNFType)
        {
            //Variáveis locais
            model_J1B1N vObjJ1B1N = new model_J1B1N();
            DB vObjDb = new DB();
            SAP_RFC.MainDataJ1B1N vObjJ1B1NData = new SAP_RFC.MainDataJ1B1N();
            TBJ1B1N_CAB_NFE vObjJ1B1NCabNFE = new TBJ1B1N_CAB_NFE();
            string vStrNFTypeDeterminado = string.Empty;
            string vStrPartnerType = string.Empty;
            string vStrDocNum = string.Empty;
            List<TBJ1B1N_ITEM_NFE> vLstJ1B1N_ITEM_NFE;
            List<TBJ1B1N_ITEMTAX_NFE> vLstJ1B1N_ITEMTAX_NFE;
            
            DataLayer.DL_TBDOC_CAB_NFE vObjDLCabNFe = new DataLayer.DL_TBDOC_CAB_NFE();
            DataLayer.DL_TBDOC_ITEM_NFE vObjDLItemNFe = new DataLayer.DL_TBDOC_ITEM_NFE();
            DataLayer.DLTBJ1B1N_CAB_NFE vObjDLJ1B1NCab = new DataLayer.DLTBJ1B1N_CAB_NFE();
            DataLayer.DLTBJ1B1N_ITEM_NFE vObjDLTBJ1B1N_ITEM_NFE = new DataLayer.DLTBJ1B1N_ITEM_NFE();
            DataLayer.DLTBJ1B1N_ITEMTAX_NFE vObjDLTBJ1B1N_ITEMTAX_NFE = new DataLayer.DLTBJ1B1N_ITEMTAX_NFE();
            SAP_RFC.MainDataJ1B1NFilter vObjFilter = new MetsoFramework.SAP.SAP_RFC.MainDataJ1B1NFilter();
            modVerificar vObjModVerificar = new modVerificar();
            BLNotaFiscal vObjBLNotaFiscal = new BLNotaFiscal();

            try
            {
                var PartnerFunction = String.Empty;
                try
                {

                    #region Carregando campos da tela
                    //View bag com a chave da nota
                    ViewBag.NFEID = pNFEID;
                    //Set ViewBag de identificação de erros como false
                    ViewBag.HasError = false;
                    //Chamando método para carregar as categorias de notas
                    ViewBag.NFTypes = LoadNFTypes(pStrNFType, out vStrNFTypeDeterminado, pNFEID);
                    //Carregando dados do cabeçalho da nota vindos do XML que foi desmembrado em tableas
                    //Os dados do cabeçalho obedecem a estrutura e criam um objeto model
                    vObjJ1B1N.TBDOC_CAB_NFE = vObjDLCabNFe.GetByChave(pNFEID);
                    //---> MONTA A TABELA COM O LOG DO DOCUMENTO
                    DLLogApplication objDLLogApplication = new DLLogApplication();
                    ViewBag.LogApplication = objDLLogApplication.GetLogStringByNfeId(pNFEID);
                    //Verificando status do documento
                    ViewBag.PodeModificar = vObjBLNotaFiscal.PodeModificar(pNFEID).ToString();
                    //Verifica se o documento carregdo é uma NF-e ou um CT-e, caso seja um CT-e irá abortar as demais funções e dexar a opção de 
                    //concluir manualmente ativo
                    if (vObjJ1B1N.TBDOC_CAB_NFE.NF_IDE_MOD == 57)
                    {
                        //Verificando status do documento
                        ViewBag.PodeModificar = vObjBLNotaFiscal.PodeModificar(pNFEID).ToString();
                        ViewBag.NFTypes = ViewBag.NFTypes ?? new List<SelectListItem>();
                        ViewBag.PartnerFunction = ViewBag.PartnerFunction ?? new List<SelectListItem>();
                        ViewBag.PartnerId = ViewBag.PartnerId ?? new List<SelectListItem>();
                        ViewBag.Modelo = "57";
                        return View(vObjJ1B1N);
                    }
                    else
                    {
                        ViewBag.Modelo = "55";
                    }
                    //Carrega informações do SAP com base em algusn dados do XML da nota
                    //Carrega filtro com dos dados dos materiasis da nota para consultar no SAP
                    vObjFilter = vObjDLItemNFe.GetSearchFilterMaterialJ1B1N(pNFEID);
                    vObjJ1B1NData = vObjModVerificar.LerSAP(vObjJ1B1N.TBDOC_CAB_NFE.NF_EMIT_CNPJ, PartnerFunction, ref vObjFilter);
                    ViewBag.PartnerId = LoadPartnerId(vObjJ1B1NData.DADOS_EMITENTES, vObjJ1B1N.TBDOC_CAB_NFE.NF_EMIT_CNPJ);
                    //Carregando os tipos de pafrceiros
                    //e setando o parceiro conforme tabela de parametrização
                    PartnerFunction = vObjDLItemNFe.GetPartnerTypeFromCFOPFirstNFItem(pNFEID);
                    ViewBag.PartnerFunction = LoadPartnerFunction(PartnerFunction, out vStrPartnerType, pNFEID);
                    //Carregando os dados dos itens da nota vindos do XML e desmembrados em tabelas
                    //Cria um objeto JSOn para que possa carregar diretamente o JQGrid
                    ViewBag.NFItens = vObjDLItemNFe.GetJsonJQGridNFItensByChave(pNFEID, out vLstJ1B1N_ITEM_NFE, vObjJ1B1NData);
                    //Carregando ipostos dos itens
                    ViewBag.NFItensTax = vObjDLItemNFe.GetJsonJQGridNFItensTaxByChave(pNFEID, out vLstJ1B1N_ITEMTAX_NFE);
                    //Carrega lista de CFOPs que podem ser escriurados
                    ViewBag.CFOP_List = LoadCFOPsParaEscriturar();
                    
                    
                    
                    #endregion
                }
                catch (Exception ex)
                {
                    ViewBag.HasError = true;
                    ViewBag.MsgError = ex.Message;
                    return View(vObjJ1B1N);
                }
                finally
                {
                    ViewBag.NFTypes = ViewBag.NFTypes ?? new List<SelectListItem>();
                    ViewBag.PartnerFunction = ViewBag.PartnerFunction ?? new List<SelectListItem>();
                    ViewBag.PartnerId = ViewBag.PartnerId ?? new List<SelectListItem>();
                }

                #region Gravando dados nas tabelas de integração da J1B1N
                if (vObjDLJ1B1NCab.db.TBJ1B1N_CAB_NFE.Where(x => x.NFEID == pNFEID).Count() == 0)
                {
                    //Gravando informações nas tabelas de integração que serão utilizadas para enviar a nota para o SAP
                    //Inserindo dados do cabeçalho na tabela de integração 
                    vObjJ1B1NCabNFE.NFEID = pNFEID;
                    vObjJ1B1NCabNFE.NFTYPE_DE = vStrNFTypeDeterminado;
                    vObjJ1B1NCabNFE.NFTYPE_PARA = vStrNFTypeDeterminado;
                    vObjJ1B1NCabNFE.PARTNER_TYPE_DE = string.IsNullOrWhiteSpace(vStrPartnerType) ? "" : vStrPartnerType;
                    vObjJ1B1NCabNFE.PARTNER_TYPE_PARA = string.IsNullOrWhiteSpace(vStrPartnerType) ? "" : vStrPartnerType;
                    vObjJ1B1NCabNFE.PARTNER_FUNCTION_DE = string.IsNullOrWhiteSpace(PartnerFunction) ? "" : PartnerFunction;
                    vObjJ1B1NCabNFE.PARTNER_FUNCTION_PARA = string.IsNullOrWhiteSpace(PartnerFunction) ? "" : PartnerFunction;
                    vObjJ1B1NCabNFE.SALVO = "0";    
                    try
                    {
                        vObjDLJ1B1NCab.Insert(vObjJ1B1NCabNFE);
                        vObjDLJ1B1NCab.Save();
                    }
                    catch (Exception ex)
                    {
                        ViewBag.HasError = true;
                        ViewBag.MsgError = ex.Message;
                        return View(vObjJ1B1N);
                    }
                    //Inserindo itens na tabela de integração
                    try
                    {
                        foreach (var item in vLstJ1B1N_ITEM_NFE)
                        {
                            vObjDLTBJ1B1N_ITEM_NFE.Insert(item);
                        }
                        vObjDLTBJ1B1N_ITEM_NFE.Save();
                    }
                    catch (Exception ex)
                    {
                        ViewBag.HasError = true;
                        ViewBag.MsgError = ex.Message;
                        return View(vObjJ1B1N);
                    }
                    //Inserindo impostos na tabela de integração
                    try
                    {
                        foreach (var item in vLstJ1B1N_ITEMTAX_NFE)
                        {
                            vObjDLTBJ1B1N_ITEMTAX_NFE.Insert(item);
                        }
                        vObjDLTBJ1B1N_ITEMTAX_NFE.Save();
                    }
                    catch (Exception ex)
                    {
                        ViewBag.HasError = true;
                        ViewBag.MsgError = ex.Message;
                        return View(vObjJ1B1N);
                    }
                }
                else
                {
                    //Verifica se existem dados salvos que foram alterados para carregar as alterações
                    //Verifica se a nota já tem alterações, para carregar os ultimos dados salvos
                    var CabecalhoJ1B1N = vObjDLJ1B1NCab.db.TBJ1B1N_CAB_NFE.FirstOrDefault(x => x.NFEID == pNFEID);

                    if (CabecalhoJ1B1N != null)
                    {
                        ViewBag.CabObservacao = CabecalhoJ1B1N.OBSERVACAO;
                        ViewBag.CabDocnumOrig = CabecalhoJ1B1N.DOCNUM_REF;
                        if (!string.IsNullOrWhiteSpace(CabecalhoJ1B1N.DOC_NUM_J1B1N))
                        {
                            ViewBag.SAPDocNumJ1B1N = string.Format("SAP docnum: {0}", CabecalhoJ1B1N.DOC_NUM_J1B1N);
                            ViewBag.Salvo = CabecalhoJ1B1N.SALVO;
                        }
                    }
                }
                #endregion


                #region Validando informações de tela durante o carregamento
                //Verificando se a nota que está sendo editada jã não existe no SAP
                if (this.CheckDOCNUMExists(vObjJ1B1N.TBDOC_CAB_NFE.NF_IDE_NNF, vObjJ1B1N.TBDOC_CAB_NFE.NF_IDE_SERIE, vObjJ1B1N.TBDOC_CAB_NFE.NF_EMIT_CNPJ, out vStrDocNum))
                {
                    ViewBag.SAPDocNumJ1B1N = string.Format("SAP docnum: {0}", vStrDocNum);
                    ViewBag.Salvo = "2";
                    //Caso o campo do docnum esteja vazio, significa que a nota foi postada manualmente no SAP e será criada manualmente no VNF
                    if (string.IsNullOrWhiteSpace(vObjJ1B1NCabNFE.DOC_NUM_J1B1N))
                    {
                        ViewBag.CriadoManualmente = "True";    
                    }
                }

                #endregion



                return View(vObjJ1B1N);
            }
            catch (Exception ex)
            {
                ViewBag.HasError = true;
                ViewBag.MsgError = ex.Message;
                throw;
            }
        }

        private List<SelectListItem> LoadNFTypes(string pStrNFType, out string pStrNFTypeDeterminado, string pStrNFEID)
        {
            List<SelectListItem> vLstNFTypes = new List<SelectListItem>();
            pStrNFTypeDeterminado = string.Empty;
            DataLayer.DLTBJ1B1N_CAB_NFE vObjcabJ1B1N = new DataLayer.DLTBJ1B1N_CAB_NFE();

            try
            {
                //Recupera parametro que contem as categorias de notas
                var vObjRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'NF_TYPES' ").Split(',');

                //Verfica se o socumento já foi alterado para recupar as informações salvas
                if (vObjcabJ1B1N.db.TBJ1B1N_CAB_NFE.FirstOrDefault(x => x.NFEID == pStrNFEID) != null)
                {
                    pStrNFType = vObjcabJ1B1N.db.TBJ1B1N_CAB_NFE.FirstOrDefault(x => x.NFEID == pStrNFEID).NFTYPE_PARA;
                }
                else
                {
                    pStrNFType = "Z2";
                }


                //Gerando lista coma s categorias de notas
                foreach (var item in vObjRetorno)
                {
                    
                    if (item == pStrNFType)
                    {
                        vLstNFTypes.Add(new SelectListItem { Value = item, Text = item, Selected = true });
                        pStrNFTypeDeterminado = item;
                    }
                    else
                    {
                        vLstNFTypes.Add(new SelectListItem { Value = item, Text = item });
                    }
                    
                }

              
                return vLstNFTypes;

            }
            catch (Exception)
            {
                
                throw;
            }
            
            
        
        }

        private List<SelectListItem> LoadPartnerFunction(string pStrPartnerFunction, out string pStrPartnerType, string pStrNFEID)
        {
            pStrPartnerType = string.Empty;
            List<SelectListItem> vLstPartnerFunction = new List<SelectListItem>();
            DataLayer.DLTBJ1B1N_CAB_NFE vObjcabJ1B1N = new DataLayer.DLTBJ1B1N_CAB_NFE();

            //Verfica se o socumento já foi alterado para recupar as informações salvas
            if (vObjcabJ1B1N.db.TBJ1B1N_CAB_NFE.FirstOrDefault(x => x.NFEID == pStrNFEID) != null)
            {
                pStrPartnerFunction = vObjcabJ1B1N.db.TBJ1B1N_CAB_NFE.FirstOrDefault(x => x.NFEID == pStrNFEID).PARTNER_FUNCTION_PARA;
            }

            try
            {
                vLstPartnerFunction.Add(new SelectListItem { Value = "LF", Text = "LF Fornecedor" });
                vLstPartnerFunction.Add(new SelectListItem { Value = "AG", Text = "AG Emissor OV" });
                vLstPartnerFunction.Add(new SelectListItem { Value = "BP", Text = "Business Partner" });

                //Setando partnert type para aparecer selecionado no combo
                foreach (var item in vLstPartnerFunction)
                {
                    if (item.Value == pStrPartnerFunction)
                    {
                        item.Selected = true;
                        pStrPartnerType = DeterminaTipoParceito(item.Value);
                        break;
                    }
                }

                return vLstPartnerFunction;
            }
            catch (Exception)
            {
                
                throw;
            }


        }
        private string LoadCFOPsParaEscriturar()
        {
            DLTBJ1B1N_CADASTRO_CFOP vObjDLCFOPEscriturar = new DLTBJ1B1N_CADASTRO_CFOP();
            StringBuilder vObjSBCFOPsEscriturar = new StringBuilder();
            int vIntCont = 0;
            try
            {
                foreach (var item in vObjDLCFOPEscriturar.db.TBJ1B1N_CADASTRO_CFOP.ToList())
	            {
		            vObjSBCFOPsEscriturar.Append(vIntCont.ToString() + ":" + item.CFOP + " | " + item.DESCRICAO + ";");
                    vIntCont ++;
	            }
                 
                return vObjSBCFOPsEscriturar.ToString().TrimEnd(';');
            }
            catch (Exception)
            {
                
                throw;
            }


        }
        private List<SelectListItem> LoadPartnerId(List<SAP_RFC.MainDataEmitenteJ1B1N> pLstPartnerId, string pstrCNPFEmitente)
        {
            DataLayer.DLTBJ1B1N_DADOS_METSO_PADRAO vObjDLDadosMetsoPadrao = new DataLayer.DLTBJ1B1N_DADOS_METSO_PADRAO();
            List<SelectListItem> vLstPartnerId = new List<SelectListItem>();
            try
            {

                //Caso o emitente seja a própria Metso, busca o Id Partner de uma tabela padrão
                if (vObjDLDadosMetsoPadrao.db.TBJ1B1N_DADOS_METSO_PADRAO.Where(x => x.CNPJ == pstrCNPFEmitente).Count() > 0)
                {
                    var IdPartner = vObjDLDadosMetsoPadrao.db.TBJ1B1N_DADOS_METSO_PADRAO.Where(x => x.CNPJ == pstrCNPFEmitente).FirstOrDefault();
                    vLstPartnerId.Add(new SelectListItem { Value = IdPartner.ID_FORN_METSO, Text = IdPartner.ID_FORN_METSO + " - " + "Neles " + IdPartner.PLANTA, Selected = true });
                }
                else
                {
                    //Verifica se possui apenas um código, caso possua apenas um já deixa selecionado, caso contrário
                    //ficará sem seleção para que o usuário possa escolher
                    if (pLstPartnerId.Count > 1)
                    {
                        foreach (var item in pLstPartnerId)
                        {
                            vLstPartnerId.Add(new SelectListItem { Value = item.NumeroVendor_LIFNR, Text = item.NumeroVendor_LIFNR + " - " + item.Nome_NAME1 });
                        }
                    }
                    else if (pLstPartnerId.Count == 1)
                    {
                        foreach (var item in pLstPartnerId)
                        {
                            vLstPartnerId.Add(new SelectListItem { Value = item.NumeroVendor_LIFNR, Text = item.NumeroVendor_LIFNR + " - " + item.Nome_NAME1, Selected = true });
                        }
                    }
                }
          
                return vLstPartnerId;

            }
            catch (Exception)
            {

                throw;
            }



        }

        [HttpGet]
        public string GetMaterialDataFromSap(string pStrItemNF, string pStrCodmaterial, string pStrPlanta, string pStrDescricao)
        {
            SAP_RFC.RfcReturn vObjRFCReturn = new MetsoFramework.SAP.SAP_RFC.RfcReturn();
            SAP_RFC.MainDataJ1B1NFilter vObjFilter = new MetsoFramework.SAP.SAP_RFC.MainDataJ1B1NFilter();
            DataLayer.DLTBJ1B1N_COD_MATERIAL_DESCRICAO vObjDLCOD_MATERIAL_DESCRICAO = new DLTBJ1B1N_COD_MATERIAL_DESCRICAO();
            
            SAP_RFC.MainDataSearchItemsJ1B1N vObjItemtoSearch = new MetsoFramework.SAP.SAP_RFC.MainDataSearchItemsJ1B1N();
            long vLngCodMaterial;


            string vStrDescricaoMaterial = string.Empty;
            string vStrMaterialOrigem = string.Empty;
            string vStrMaterialUtilizacao = string.Empty;
            string vStrMaterialUnidade = string.Empty;
            string vStrMaterialGrupo = string.Empty;
           
            try
            {
                vObjItemtoSearch.Planta = pStrPlanta.Trim();
                vObjItemtoSearch.NumeroMaterial = long.TryParse(pStrCodmaterial.Trim(), out vLngCodMaterial) ? vLngCodMaterial.ToString().Trim().PadLeft(18,'0') : pStrCodmaterial.Trim();
                vObjItemtoSearch.NFItemNumero = int.Parse(pStrItemNF);

                

                vObjFilter.SearchItems = new List<MetsoFramework.SAP.SAP_RFC.MainDataSearchItemsJ1B1N>();
                

                vObjFilter.SearchItems.Add(vObjItemtoSearch);
                

                var MaterialData = SAP_RFC.getMainDataJ1B1N(vObjFilter, out vObjRFCReturn);
                
                
                
                foreach (var item in MaterialData.DADOS_MATERIAS)
                {
                    if (vObjDLCOD_MATERIAL_DESCRICAO.db.TBJ1B1N_COD_MATERIAL_DESCRICAO.Where(x => x.COD_MATERIAL == item.MATERIAL).Count() > 0)
                    {
                        vStrDescricaoMaterial = pStrDescricao;
                       
                    }
                    else
                    {
                        vStrDescricaoMaterial = item.MATERIAL_DESCRIPTION_MAKTX.Trim();   
                    }

                    vStrMaterialOrigem = item.ORIGIN_MATERIAL_MTORG;
                    vStrMaterialUtilizacao = item.USAGE_MATERIAL_MTUSE;
                    vStrMaterialGrupo = item.MATERIAL_GROUP_MATKL;
                    vStrMaterialUnidade = item.BASE_MEASURE_MEINS;

                }

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", true },
                                                                                     {"DescricaoMaterial", vStrDescricaoMaterial },
                                                                                     {"MaterialOrigem", vStrMaterialOrigem },
                                                                                     {"MaterialUtilizacao", vStrMaterialUtilizacao },
                                                                                     {"MaterialGrupo", vStrMaterialGrupo},
                                                                                     {"MaterialUnidadeMed", vStrMaterialUnidade},
                                                                                     {"MensagemErro", ""}});
            }
            catch (Exception ex)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"DescricaoMaterial", vStrDescricaoMaterial },
                                                                                     {"MensagemErro","BAPI Message: " + vObjRFCReturn.BapiMessage + " .NetMessage: " + ex.Message}});
            }
        }

        private string DeterminaTipoParceito(string pStrFuncaoParceiro) 
        {
            switch (pStrFuncaoParceiro)
            {
                case "LF":
                    return "V";
                case "AG":
                    return "C";
                default:
                    return "B";
            }
        }

        [HttpPost]
        public string SalvarJ1B1N(TBJ1B1N_CAB_NFE pObjNF)
        {
            DataLayer.DLTBJ1B1N_CAB_NFE vObjDLJ1B1NCab = new DataLayer.DLTBJ1B1N_CAB_NFE();            
            try
            {
               
                               
                //Salvando dados do cabeçalho
                vObjDLJ1B1NCab.SalvarAlteracoes(pObjNF);



                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", true }});
            }
            catch (Exception ex)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro",".NetMessage: " + ex.Message}});
                throw;
            }
        }
        [HttpPost]
        public void UnSaveDocument(string pStrNFEID)
        { 
            DataLayer.DLTBJ1B1N_CAB_NFE vObjDLNF = new DataLayer.DLTBJ1B1N_CAB_NFE();

            vObjDLNF.db.TBJ1B1N_CAB_NFE.FirstOrDefault(x => x.NFEID == pStrNFEID).SALVO = "0";
            vObjDLNF.db.SaveChanges();
        }
        [HttpPost]
        public string GerarJ1B1N(string pStrNFEID)
        {
            DataLayer.DLTBJ1B1N_CAB_NFE vObjDLJ1B1NCab = new DataLayer.DLTBJ1B1N_CAB_NFE();
            string vStrMensagemErro = string.Empty;
            string vStrDocnum = string.Empty;
            long vLngCodMaterial;
       
            try
            {
                var vObjNFJ1B1N = vObjDLJ1B1NCab.db.TBJ1B1N_CAB_NFE.FirstOrDefault(x => x.NFEID == pStrNFEID);
               

                if (vObjNFJ1B1N != null)
                {
                    DataLayer.DL_TBDOC_CAB_NFE vObjDLCAB_NFE = new DataLayer.DL_TBDOC_CAB_NFE();
                    var vObjNFE = vObjDLCAB_NFE.db.TbDOC_CAB_NFE.FirstOrDefault(x => x.NFEID == pStrNFEID);
                    //Verificando se a nota que está sendo enviada jã não existe no SAP
                    string vStrDocNum = string.Empty;
                    if (this.CheckDOCNUMExists(vObjNFE.NF_IDE_NNF, vObjNFE.NF_IDE_SERIE, vObjNFE.NF_EMIT_CNPJ, out vStrDocNum))
                    {
                        ViewBag.SAPDocNumJ1B1N = string.Format("SAP docnum: {0}", vStrDocNum);
                        ViewBag.Salvo = "2";
                        vStrMensagemErro = "Documento " + vStrDocnum + " já processado na J1B1N.";
                        return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro", " " + vStrMensagemErro}});
                    }

                    SAP_RFC.InvoiceHeaderJ1B1N vObjJ1B1NHeader = new MetsoFramework.SAP.SAP_RFC.InvoiceHeaderJ1B1N();
                    SAP_RFC.InvoiceXmlItemJ1B1N vObjJ1B1NItem;
                    SAP_RFC.InvoiceXmlItemTaxJ1B1N vObjJ1B1NItemTax;
                    List<SAP_RFC.InvoiceXmlItemJ1B1N> vLstJ1B1NItens = new List<SAP_RFC.InvoiceXmlItemJ1B1N>();
                    List<SAP_RFC.InvoiceXmlItemTaxJ1B1N> vLstJ1B1NItemTax = new List<SAP_RFC.InvoiceXmlItemTaxJ1B1N>();
                    string vStrSapUser = string.Empty;
                    string vStrSapPassword = string.Empty;
                    int vIntSAPItemNum = 0;
                    SAP_RFC.RfcReturn vObjRFCReturn;


                    if (vObjNFJ1B1N.SALVO == "1")
                    {
                        //Preenchendo dados do cabeçalho da BAPI para gerar a nota manual (J1B1N)
                        vObjJ1B1NHeader.CREATE_NAME_USER_CRENAM = Uteis.LogonName();
                        vObjJ1B1NHeader.LOGICAL_SYSTEM_AWSYS = "VNF";
                        vObjJ1B1NHeader.MODEL_NOTA_FISCAL_MODEL = vObjNFE.NF_IDE_MOD ??0 ;
                        vObjJ1B1NHeader.POSTING_DATE_PSTDAT = System.DateTime.Now.Date;
                        vObjJ1B1NHeader.DOCUMENT_DATE_DOCDAT = vObjNFE.NF_IDE_DHEMI.Value.Date;
                        vObjJ1B1NHeader.PARTNER_ID_PARID = vObjNFJ1B1N.PARTNER_ID.ToString();
                        vObjJ1B1NHeader.NOTA_FISCAL_PARTNER_FUNCTION_PARVW = vObjNFJ1B1N.PARTNER_FUNCTION_PARA;
                        vObjJ1B1NHeader.GROSS_WEIGHT_BRGEW = vObjNFE.NF_ICMSTOT_VNF ?? 0;
                        vObjJ1B1NHeader.NET_WEIGHT_NTGEW = vObjNFE.NF_ICMSTOT_VNF ?? 0;
                        vObjJ1B1NHeader.NOTA_FISCAL_TYPE_NFTYPE = vObjNFJ1B1N.NFTYPE_PARA;
                        vObjJ1B1NHeader.NOTA_FISCAL_NUMBER_NFNUM = vObjNFE.NF_IDE_NNF.PadLeft(9, '0').Substring(3, 6).ToInt(); //Limita o tamanho máximo do número da nota em 6 dígitos por conta do SAP
                        vObjJ1B1NHeader.SERIES_SERIES = vObjNFE.NF_IDE_SERIE;
                        vObjJ1B1NHeader.LEGAL_BOOKS_HEADER_TEXT_OBSERVAT = vObjNFJ1B1N.OBSERVACAO;
                        vObjJ1B1NHeader.REFERENCE_TO_NF_DOCREF = vObjNFJ1B1N.DOCNUM_REF;
                        //vObjJ1B1NHeader.WEIGHT_UNIT_GEWEI = "EA";

                        //Preenchendo dados do item da BAPI para gerar a nota manual (J1B1N)
                        foreach (var item in vObjNFJ1B1N.TBJ1B1N_ITEM_NFE)
                        {   
                            vObjJ1B1NItem = new MetsoFramework.SAP.SAP_RFC.InvoiceXmlItemJ1B1N();
                            vIntSAPItemNum = vIntSAPItemNum + 10;
                            vObjJ1B1NItem.DOCUMENT_ITEM_NUMBER_ITMNUM = vIntSAPItemNum;
                            vObjJ1B1NItem.MATERIAL_NUMBER_MATNR = long.TryParse(item.COD_MATERIAL_PARA, out vLngCodMaterial) ? vLngCodMaterial.ToString().Trim().PadLeft(18, '0') : item.COD_MATERIAL_PARA;
                            vObjJ1B1NItem.VALUATION_AREA_BWKEY = item.PLANTA_PARA;
                            vObjJ1B1NItem.MATERIAL_GROUP_MATKL = item.MAT_GRUPO_MATKL;
                            vObjJ1B1NItem.BASE_UNIT_MEASURE_MEINS = item.UNIDADE_PARA.Trim();
                            vObjJ1B1NItem.MATERIAL_DESCRIPTION_MAKTX = item.MAT_DESCRICAO_MAKTX;
                            vObjJ1B1NItem.CONTROL_CODE_FOREIGN_TRADE_NBM = item.NCM_PARA;
                            vObjJ1B1NItem.ORIGIN_MATERIAL_MATORG = item.MAT_ORIG_PARA.ToString();
                            vObjJ1B1NItem.USAGE_MATERIAL_MATUSE = item.MAT_USO;
                            vObjJ1B1NItem.QUANTITY_MENGE = decimal.Parse(item.QUANTIDADE_PARA.ToString());
                            vObjJ1B1NItem.NET_PRICE_NETPR = decimal.Parse(item.VALOR_UNIT_PARA.ToString());
                            vObjJ1B1NItem.NET_VALUE_NETWR = decimal.Parse(item.VALOR_TOT_PARA.ToString());
                            vObjJ1B1NItem.TAX_LAW_ICMS_TAXLW1 = item.TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF && x.IMPOSTO.Trim() == "ICMS").LEI_PARA;
                            vObjJ1B1NItem.TAX_LAW_IPI_TAXLW2 = item.TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF && x.IMPOSTO.Trim() == "IPI").LEI_PARA;
                            vObjJ1B1NItem.PIS_TAX_LAW_TAXLW5 = item.TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF && x.IMPOSTO.Trim() == "PIS").LEI_PARA;
                            vObjJ1B1NItem.COFINS_TAX_LAW_TAXLW4 = item.TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF && x.IMPOSTO.Trim() == "COFI").LEI_PARA;
                            vObjJ1B1NItem.NOTA_FISCAL_ITEM_TYPE_ITMTYP = item.ITEM_TYPE_PARA.Trim();
                            vObjJ1B1NItem.PLANT_WERKS = item.PLANTA_PARA;
                            vObjJ1B1NItem.CFOP_CODE_EXTENSION_CFOP_10 = item.CFOP_PARA;
                            vObjJ1B1NItem.EXTERNAL_ITEM_NUMBER_NUM_ITEM = item.ITEM_NF;
                            vObjJ1B1NItem.REFERENCE_TO_NF_DOCREF = item.DOCNUM_REF;
                            vObjJ1B1NItem.REFERENCE_TO_ITEM_NUMBER_ITMREF = item.ITEM_DOCNUM_REF ?? 0;
                            //Adicionando item a coleção de itens para chamada da BAPI
                            vLstJ1B1NItens.Add(vObjJ1B1NItem);

                            //Preenchendo dados dos impostos dos itens da BAPI para gerar a nota manual (J1B1N)
                            foreach (var itemtax in item.TBJ1B1N_ITEMTAX_NFE)
                            {
                                if (!string.IsNullOrWhiteSpace(itemtax.TIPO_PARA))
                                {
                                    vObjJ1B1NItemTax = new MetsoFramework.SAP.SAP_RFC.InvoiceXmlItemTaxJ1B1N();
                                    vObjJ1B1NItemTax.DOCUMENT_ITEM_NUMBER_ITMNUM = vIntSAPItemNum;
                                    vObjJ1B1NItemTax.TAX_TYPE_TAXTYP = itemtax.TIPO_PARA;
                                    vObjJ1B1NItemTax.BASE_AMOUNT_BASE = itemtax.BASE_CALC_PARA ?? 0;
                                    vObjJ1B1NItemTax.TAX_RATE_RATE = itemtax.ALIQUOTA_PARA ?? 0;
                                    vObjJ1B1NItemTax.TAX_VALUE_TAXVAL = itemtax.VALOR_PARA ?? 0;
                                    vObjJ1B1NItemTax.EXCLUDED_BASE_AMOUNT_EXCBAS = itemtax.BASE_EXCL_PARA ?? 0;
                                    vObjJ1B1NItemTax.OTHER_BASE_AMOUNT_OTHBAS = itemtax.BASE_OUTR_PARA ?? 0;
                                    vLstJ1B1NItemTax.Add(vObjJ1B1NItemTax);
                                }
                                
                            }
                        }

                        //Chamando a BAPI para geração da nota manual
                        if (bool.Parse(Uteis.GetSettingsValue<string>("FixedSapSystemUser")))
	                    {
                            vStrSapUser = Uteis.GetSettingsValue<string>("User");
                            vStrSapPassword = Uteis.GetSettingsValue<string>("Password");
	                    }
                        var RetornoBAPI = SAP_RFC.createInvoiceJ1B1N(vStrSapUser, vStrSapPassword, vObjJ1B1NHeader, vLstJ1B1NItens, vLstJ1B1NItemTax, out vStrDocnum, out vObjRFCReturn);
                        if (RetornoBAPI != null)
                        {
                            if (RetornoBAPI.Count == 1)
                            {
                                if (vStrDocnum.Replace("0", "").Trim() != string.Empty)
                                {
                                    vObjNFJ1B1N.SALVO = "2";
                                    vObjNFJ1B1N.DOC_NUM_J1B1N = vStrDocnum.TrimStart('0').Trim();
                                    vObjNFJ1B1N.TBJ1B1N_ITEM_NFE.Where(x => x.NFEID == pStrNFEID).ToList().ForEach(a => a.SALVO = "2");
                                    vObjDLJ1B1NCab.Save();

                                    if (!string.IsNullOrWhiteSpace(vObjNFJ1B1N.DOC_NUM_J1B1N))
                                    {
                                        ViewBag.SAPDocNumJ1B1N = string.Format("SAP docnum: {0}", vObjNFJ1B1N.DOC_NUM_J1B1N);
                                        ViewBag.Salvo = vObjNFJ1B1N.SALVO;
                                        BLNotaFiscal vObjBLNotaFiscal = new BLNotaFiscal();
                                        vObjBLNotaFiscal.UpdateStatusIntegracaoForJ1B1N(pStrNFEID, "CONCLUÍDO");
                                        BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.GeradaNotaManualJ1B1N, "Emitiu a nota manual (J1B1N). SAP docnum: " + vObjNFJ1B1N.DOC_NUM_J1B1N, pStrNFEID);
                                    }
                                }
                                else
                                {
                                    vStrMensagemErro = vStrMensagemErro + RetornoBAPI[0].MESSAGE + "  |##|  ";
                                    vObjNFJ1B1N.SALVO = "0";
                                    vObjNFJ1B1N.TBJ1B1N_ITEM_NFE.Where(x => x.NFEID == pStrNFEID).ToList().ForEach(a => a.SALVO = "0");
                                    vObjDLJ1B1NCab.Save();
                                    return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro", " " + vStrMensagemErro}});
                                }   
                            }
                            else if (RetornoBAPI.Count > 1)
                            {
                                foreach (var item in RetornoBAPI)
                                {
                                        vStrMensagemErro = vStrMensagemErro + item.MESSAGE + "  |##|  ";
                                }
                                vObjNFJ1B1N.SALVO = "0";
                                vObjNFJ1B1N.TBJ1B1N_ITEM_NFE.Where(x => x.NFEID == pStrNFEID).ToList().ForEach(a => a.SALVO = "0");
                                vObjDLJ1B1NCab.Save();
                                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro", " " + vStrMensagemErro}});
                            }
                        }
                        else
                        {
                            vStrMensagemErro = vObjRFCReturn.Exception.Message;
                            vObjNFJ1B1N.SALVO = "0";
                            vObjNFJ1B1N.TBJ1B1N_ITEM_NFE.Where(x => x.NFEID == pStrNFEID).ToList().ForEach(a => a.SALVO = "0");
                            vObjDLJ1B1NCab.Save();
                            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro", " " + vStrMensagemErro}});
                        }
                        

                    }
                    else if (vObjNFJ1B1N.SALVO == "0")
                    {
                        vStrMensagemErro = "Documento não está salvo";
                        return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro", " " + vStrMensagemErro}});
                    }
                    else if (vObjNFJ1B1N.SALVO == "3")
                    {
                        vStrMensagemErro = "Documento já processado.";
                        return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro", " " + vStrMensagemErro}});
                    }
                }
                else
                {
                    ViewBag.HasError = true;
                    ViewBag.MsgError = "Documento não encontrado.";
                    return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro", " " + vStrMensagemErro}});
                }
       

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", true },
                                                                                     {"Docnum", vStrDocnum.TrimStart('0')}   });
            }
            catch (Exception ex)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro", " " + vStrMensagemErro + " .NetMessage: " + ex.Message}});
                throw;
            }
        }

        [HttpGet]
        public string CheckDOCNUMExists(string pStrDocnum)
        {
            SAP_RFC.RfcReturn vObjRFCReturn = new MetsoFramework.SAP.SAP_RFC.RfcReturn();
            SAP_RFC.MainDataJ1B1NFilter vObjFilter = new SAP_RFC.MainDataJ1B1NFilter();
            SAP_RFC.SearchReferenceInvoice vObjSearchReferenceInvoice = new SAP_RFC.SearchReferenceInvoice();
            string vStrmensagemRetorno = string.Empty;
        
            try
            {

                vObjSearchReferenceInvoice.DOCNUM = pStrDocnum;
                vObjFilter.SearchReferenceInvoice = vObjSearchReferenceInvoice;


                //Chamando função (RFC) SAP
                var DOCNUM = SAP_RFC.getMainDataJ1B1N(vObjFilter, out vObjRFCReturn);

                if (DOCNUM.RETORNO_CONSULTA_NOTAS_REF != null)
                {
                    if (DOCNUM.RETORNO_CONSULTA_NOTAS_REF.Count > 0)
                    {
                        return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", true },
                                                                                     {"MensagemRetorno", DOCNUM.RETORNO_CONSULTA_NOTAS_REF[0].DOCNUM}});
                    }
                    else
                    {
                        return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemRetorno", pStrDocnum}});
                    }
                }
                else
                {
                    return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemRetorno", pStrDocnum}});
                }
               
            }
            catch (Exception ex)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro","BAPI Message: " + vObjRFCReturn.BapiMessage + " .NetMessage: " + ex.Message}});
            }
        }

        public bool CheckDOCNUMExists(string pStrNumeroNF, string pStrSerieNF, string pStrCNPJ, out string pStrDocNum)
        {
            SAP_RFC.RfcReturn vObjRFCReturn = new MetsoFramework.SAP.SAP_RFC.RfcReturn();
            SAP_RFC.MainDataJ1B1NFilter vObjFilter = new SAP_RFC.MainDataJ1B1NFilter();
            SAP_RFC.SearchReferenceInvoice vObjSearchReferenceInvoice = new SAP_RFC.SearchReferenceInvoice();
            pStrDocNum = string.Empty;

            try
            {

                vObjSearchReferenceInvoice.NFNUM = pStrNumeroNF;
                vObjSearchReferenceInvoice.SERIES = pStrSerieNF;
                vObjSearchReferenceInvoice.CGC = pStrCNPJ;
                vObjFilter.SearchReferenceInvoice = vObjSearchReferenceInvoice;



                var DOCNUM = SAP_RFC.getMainDataJ1B1N(vObjFilter, out vObjRFCReturn);

                if (DOCNUM.RETORNO_CONSULTA_NOTAS_REF != null)
                {
                    if (DOCNUM.RETORNO_CONSULTA_NOTAS_REF.Where(x => string.IsNullOrWhiteSpace(x.CANCEL) && x.NFTYPE != "A1" ).Count() > 0)
                    {
                        pStrDocNum = DOCNUM.RETORNO_CONSULTA_NOTAS_REF.OrderByDescending(x => x.DOCNUM).Select(x => x.DOCNUM).FirstOrDefault();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }   
}
