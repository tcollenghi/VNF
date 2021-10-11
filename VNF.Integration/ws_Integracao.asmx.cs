using MetsoFramework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using VNF.Business;
using VNF.Integration.DataLayer;
using VNF.Integration.DataLayer.DAL;
using VNF.Integration.DataLayer.Model;
using VNF.Integration.Helper;

namespace VNF.Integration
{
    /// <summary>
    /// Summary description for ws_Integracao
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class ws_Integracao : System.Web.Services.WebService
    {
        [WebMethod]
        public void ImportacaoNotaFiscal(NFS _objT, string _tipoDocumento, string _logonName)
        {
            try
            {
                SpDeleteNFS objExcluirNFS = new SpDeleteNFS();
                var RetornoSP = objExcluirNFS.DeleleNFS(_objT.IdNFS, "N");

                tbImportacaoNotaFiscal objImpNotaFiscal = new tbImportacaoNotaFiscal();
                objImpNotaFiscal.IdImportacaoNotaFiscal = 0;
                objImpNotaFiscal.IdNotaFiscal = _objT.IdNFS;

                tbImportacaoItemNF objImpItem = new tbImportacaoItemNF();
                objImpItem.IdImportacaoItemNF = 0;

                modNF NF = new modNF();
                NF.NF_IDE_NNF = _objT.NumeroDocumento;
                var auxSerie = string.IsNullOrEmpty(_objT.Serie) ? "0" : _objT.Serie;

                //Gera a chave de acesso
                if (!String.IsNullOrEmpty(_objT.ChaveAcesso))
                {
                    NF.VNF_CHAVE_ACESSO = _objT.ChaveAcesso;
                }
                else
                {
                    objImpNotaFiscal.ChaveAcesso = _objT.CNPJEmitente.Replace(".", "").Replace("-", "").Replace("/", "") + _objT.NumeroDocumento.PadLeft(10, '0') + auxSerie.PadLeft(5, '0') + Convert.ToDateTime(_objT.DataEmissao).ToString("ddMMyyyy");
                    objImpItem.ChaveAcesso = _objT.CNPJEmitente.Replace(".", "").Replace("-", "").Replace("/", "") + _objT.NumeroDocumento.PadLeft(10, '0') + auxSerie.PadLeft(5, '0') + Convert.ToDateTime(_objT.DataEmissao).ToString("ddMMyyyy");
                    NF.VNF_CHAVE_ACESSO = _objT.CNPJEmitente.Replace(".", "").Replace("-", "").Replace("/", "") + _objT.NumeroDocumento.PadLeft(10, '0') + auxSerie.PadLeft(5, '0') + Convert.ToDateTime(_objT.DataEmissao).ToString("ddMMyyyy");
                }

                objImpNotaFiscal.IdImportacaoTipoDocumento = (_tipoDocumento == "NFS-e" ? 1 : (_tipoDocumento == "Fatura" ? 2 : 3));
                tbImportacaoNotaFiscalDAL objImpoNotaFiscalDal = new tbImportacaoNotaFiscalDAL();
                objImpoNotaFiscalDal.Insert(objImpNotaFiscal);
                objImpoNotaFiscalDal.Save();

                NF.NF_IDE_SERIE = string.IsNullOrEmpty(_objT.Serie) ? "" : _objT.Serie;
                NF.NF_EMIT_CNPJ = _objT.CNPJEmitente;
                NF.NF_EMIT_XNOME = _objT.RazaoSocialEmitente;
                NF.NF_DEST_CNPJ = _objT.CNPJMetso;
                NF.NF_DEST_XNOME = _objT.RazaoSocialMetso;
                NF.NF_IDE_DHEMI = Convert.ToDateTime(_objT.DataEmissao);
                NF.VNF_TIPO_DOCUMENTO = (_tipoDocumento == "NFS-e" ? modNF.tipo_NFS : (_tipoDocumento == "Fatura" ? modNF.tipo_FAT : modNF.tipo_TLC));
                NF.VNF_CLASSIFICACAO = "COMPRA";
                NF.VNF_CODIGO_VERIFICACAO = _objT.CodigoVerificacao;
                NF.VNF_DANFE_ONLINE = _objT.LinkDocumento;
                NF.VNF_ANEXO = _objT.Anexo;
                NF.VNF_ANEXO_EXTENSAO = _objT.AnexoExtensao;
                NF.VNF_ANEXO_NOME = _objT.AnexoNome;
                NF.NF_OUTROS_INFORMACAO_ADICIONAL = _objT.Observacao;
                NF.NF_ICMSTOT_VNF = Convert.ToDecimal(_objT.ValorTotal);
                NF.VNF_STATUS = "ACEITA";

                modNFDuplicata objDuplicata = new modNFDuplicata();
                objDuplicata.NF_COBR_DUP_DVENC = Convert.ToDateTime(_objT.Vencimento);
                if (NF.DUPLICATAS == null) NF.DUPLICATAS = new List<modNFDuplicata>();
                NF.DUPLICATAS.Add(objDuplicata);

                //Pega os itens da nota
                NF.ITENS_NF = new List<modNFItem>();
                int indexItem = 0;
                foreach (NFSItem i in _objT.NFSItem)
                {
                    indexItem++;

                    modNFItem it = new modNFItem();
                    it.NF_PROD_ITEM = indexItem;
                    it.NF_PROD_XPED = i.Pedido;
                    it.NF_PROD_NITEMPED = Convert.ToInt32(i.ItemPedido);
                    it.NF_PROD_CPROD = i.Codigo;
                    it.NF_PROD_XPROD = i.Descricao;
                    it.NF_PROD_NCM = "";
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
                    it.VNF_ITEM_VALIDO = "S";
                    it.MDP_TIPO_MOVIMENTO_MIGO = "101";//Consumo, Nao tem confirmacao de estoque.
                    NF.ITENS_NF.Add(it);

                    objImpItem.IdItemNotaFiscal = i.IdNFSItem;
                    objImpItem.IdImportacaoNotaFiscal = objImpNotaFiscal.IdImportacaoNotaFiscal;
                    tbImportacaoItemNFDAL objImpItemDal = new tbImportacaoItemNFDAL();
                    objImpItemDal.Insert(objImpItem);
                    objImpItemDal.Save();
                }

                _objT.Finalizado = true;

                modVerificar modVer = new modVerificar();
                modVer.GravarDadosNFS(NF, true, _logonName);

                
                TbLOGApplicationDAL objlogAppDAL = new TbLOGApplicationDAL();
                objlogAppDAL.Insert(new TbLOGApplication
                {
                    id_log = 0,
                    log_date = DateTime.Now,
                    log_description = "Registro enviado com sucesso.",
                    log_icon = "<i class=\"fa fa - thumbs - o - up txt - color - greenDark\"></i>",
                    log_link = "",
                    log_nfeid = NF.VNF_CHAVE_ACESSO,
                    log_title = "Documento enviado do Portal Serviço",
                    log_type = "Application",
                    log_user = "SYSTEM"
                });
                objlogAppDAL.Save();

                SpInsertHistorico spHistorico = new SpInsertHistorico();
                spHistorico.InsereHistorico(DateTime.Now, _objT.IdNFS, "SYSTEM", "SYSTEM", "Registro foi importado com sucesso", "O Documento foi importado para o VNF");

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod]
        public string VNFExcluirNFS(int IdNotaFiscal, string ConfirmaConcluido)
        {
            try
            {
                SpDeleteNFS objExcluirNFS = new SpDeleteNFS();

                var RetornoSP = objExcluirNFS.DeleleNFS(IdNotaFiscal, ConfirmaConcluido);

                return RetornoSP;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [WebMethod]
        public bool VNFUpdateSituacaoNFS(int IdNotaFiscal, string Situacao)
        {
            try
            {
                tbImportacaoNotaFiscalDAL objImpNotaFiscalDal = new tbImportacaoNotaFiscalDAL();
                var objImpNotaFiscal = objImpNotaFiscalDal.getByIdNotaFiscal(IdNotaFiscal);

                if (objImpNotaFiscal != null)
                {
                    vwStatusIntegracaoDAL objStatusIntegracaoDAL = new vwStatusIntegracaoDAL();
                    var objStatusIntegracao = objStatusIntegracaoDAL.getByChaveAcesso(objImpNotaFiscal.ChaveAcesso);

                    if (objStatusIntegracao != null)
                    {
                        if (objStatusIntegracao.STATUS_INTEGRACAO == "PENDENTE")
                        {
                            tbNFEDAL objNfeDal = new tbNFEDAL();
                            var objNfe = objNfeDal.getByChaveAcesso(objImpNotaFiscal.ChaveAcesso);

                            if (objNfe != null)
                            {
                                objNfe.SITUACAO = Situacao;
                                objNfeDal.Update(objNfe);
                                objNfeDal.Save();
                                
                                TbLOGApplicationDAL objlogAppDAL = new TbLOGApplicationDAL();
                                objlogAppDAL.Insert(new TbLOGApplication
                                {
                                    id_log = 0,
                                    log_date = DateTime.Now,
                                    log_description = "Registro foi estornado situacao Cancelada",
                                    log_icon = "<i class=\"fa fa - smile - o txt - color - orange\"></i>",
                                    log_link = "",
                                    log_nfeid = objNfe.NFEID,
                                    log_title = "Estorno Portal Serviço",
                                    log_type = "Application",
                                    log_user = "SYSTEM"
                                });
                                objlogAppDAL.Save();

                                SpInsertHistorico spHistorico = new SpInsertHistorico();
                                spHistorico.InsereHistorico(DateTime.Now, IdNotaFiscal, "SYSTEM", "SYSTEM", "Registro foi estornado situacao Cancelada", "O Documento foi estornado no VNF");

                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
