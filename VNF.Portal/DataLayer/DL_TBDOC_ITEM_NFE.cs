using MetsoFramework.SAP;
using MetsoFramework.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DL_TBDOC_ITEM_NFE : Repository<TbDOC_ITEM_NFE>
    {
        public string GetJsonJQGridNFItensByChave(string pStrNFEID, out List<TBJ1B1N_ITEM_NFE> pLstJ1B1N_ITEM_NFE, SAP_RFC.MainDataJ1B1N pObjJ1B1NData)
        {
            ArrayList vObjArrayNFItems = new ArrayList();
            List<TBJ1B1N_ITEM_NFE> vLstJ1B1N_ITEM_NFE = new List<TBJ1B1N_ITEM_NFE>();
            pLstJ1B1N_ITEM_NFE = vLstJ1B1N_ITEM_NFE;
            string vStrDescricaoMaterial = string.Empty;
            string vStrMaterialOrigem = string.Empty;
            string vStrMaterialUtilizacao = string.Empty;
            string vStrMaterialUnidade = string.Empty;
            string vStrMaterialGrupo = string.Empty;
            string vStrCodMaterialDePara = string.Empty;
         
            try
            {
                var nfitem = (from i in db.TbDOC_ITEM_NFE
                              join c in db.TbDOC_CAB_NFE on i.NFEID equals c.NFEID
                              join p in db.TBJ1B1N_PLANTA_METSO_PARCEIRO on c.NF_DEST_CNPJ equals p.CNPJ
                              let cfopEscr = db.TBJ1B1N_DE_PARA_CFOP_ESCRITURAR.Where(x => x.CFOP_XML == i.NF_PROD_CFOP).FirstOrDefault()
                              // left outer join com a própria tabela de item para pegar os dados já alterados caso existam
                              join ij1 in db.TBJ1B1N_ITEM_NFE on new { i.NFEID, ITEM = i.NF_PROD_ITEM } equals new { ij1.NFEID, ITEM = ij1.ITEM_NF } into jij1
                              from ij1 in jij1.DefaultIfEmpty()
                              //left outer join novamente com a tabela de itens para procurar de/para de dados para preenchimento automativo
                              join itm_de_para in db.VWJ1B1N_DE_PARA_MATERIAL on new { CNPJ_EMITENTE = c.NF_EMIT_CNPJ, MATERIAL = i.NF_PROD_CPROD } equals new { itm_de_para.CNPJ_EMITENTE, MATERIAL = itm_de_para.COD_MATERIAL_DE } into jitm_de_para
                              from itm_de_para in jitm_de_para.DefaultIfEmpty()
                              where i.NFEID == pStrNFEID
                              select new
                              {
                                  i.NF_PROD_ITEM,
                                  i.NF_PROD_CPROD,
                                  i.NF_PROD_XPROD,
                                  i.NF_PROD_QCOM,
                                  i.NF_PROD_VUNCOM,
                                  i.NF_PROD_VPROD,
                                  i.NF_PROD_NCM,
                                  i.NF_PROD_UCOM,
                                  i.NF_ICMS_ORIG,
                                  ij1,
                                  itm_de_para,
                                  p.PLANTA,
                                  c.NF_EMIT_CNPJ,
                                  cfopEscr.CFOP_ESCRITURAR
                              }).OrderBy(x => x.NF_PROD_ITEM).ToList();

                if (nfitem.Count == 0) throw new Exception("Verificar tabelas de parâmetro da J1B1N");

                //Caso a nota já existir carrega as informações que foram previamente editadas, caso contrário carrrega padrão
                if (nfitem[0].ij1 == null)
                {
                    foreach (var item in nfitem)
                    {
                        //Verifica se irá considerar a descrição que veio do SAP ou do XML com base na tabela de parametrização
                        if (item.itm_de_para != null)
                        {
                            vStrCodMaterialDePara = db.TBJ1B1N_COD_MATERIAL_DESCRICAO.Where(x => x.COD_MATERIAL == item.itm_de_para.COD_MATERIAL_PARA).Select(x => x.COD_MATERIAL).FirstOrDefault();    
                        }

                        if (db.TBJ1B1N_COD_MATERIAL_DESCRICAO.Where(x => x.COD_MATERIAL == item.NF_PROD_CPROD.Trim()).Count() > 0 || db.TBJ1B1N_COD_MATERIAL_DESCRICAO.Where(x => x.COD_MATERIAL == vStrCodMaterialDePara.Trim()).Count() > 0)
                        {
                            vStrDescricaoMaterial = item.NF_PROD_XPROD.Substring(0, Math.Min(item.NF_PROD_XPROD.Length, 40));
                        }
                        else
                        {
                            vStrDescricaoMaterial = pObjJ1B1NData.DADOS_MATERIAS.Where(x => x.ITEM_NUMBER_NFITEM == item.NF_PROD_ITEM).Select(x => x.MATERIAL_DESCRIPTION_MAKTX).First();
                        }
                        
                        vStrMaterialOrigem = pObjJ1B1NData.DADOS_MATERIAS.Where(x => x.ITEM_NUMBER_NFITEM == item.NF_PROD_ITEM).Select(x => x.ORIGIN_MATERIAL_MTORG).First();
                        vStrMaterialUtilizacao = pObjJ1B1NData.DADOS_MATERIAS.Where(x => x.ITEM_NUMBER_NFITEM == item.NF_PROD_ITEM).Select(x => x.USAGE_MATERIAL_MTUSE).First(); ;
                        vStrMaterialGrupo = pObjJ1B1NData.DADOS_MATERIAS.Where(x => x.ITEM_NUMBER_NFITEM == item.NF_PROD_ITEM).Select(x => x.MATERIAL_GROUP_MATKL).First();
                        vStrMaterialUnidade = pObjJ1B1NData.DADOS_MATERIAS.Where(x => x.ITEM_NUMBER_NFITEM == item.NF_PROD_ITEM).Select(x => x.BASE_MEASURE_MEINS).First();

                        vObjArrayNFItems.Add(new
                        {
                            ITEM_NF = item.NF_PROD_ITEM,
                            ITEM_TYPE_PARA = "1",
                            COD_MATERIAL_PARA = item.itm_de_para == null ? item.NF_PROD_CPROD.Substring(0, Math.Min(item.NF_PROD_CPROD.Length, 18)) : item.itm_de_para.COD_MATERIAL_PARA,
                            MAT_DESCRICAO_MAKTX = string.IsNullOrWhiteSpace(vStrDescricaoMaterial) ? item.itm_de_para == null ? item.NF_PROD_XPROD.Substring(0, Math.Min(item.NF_PROD_XPROD.Length,40)) : item.itm_de_para.MAT_DESCRICAO_MAKTX : vStrDescricaoMaterial,
                            PLANTA_PARA = item.PLANTA,
                            QUANTIDADE_PARA = item.NF_PROD_QCOM.ToString().Replace(".", ","),
                            //Marcio Spinosa - Edson Caio Ridiculamente ridiculo concatenar string e dar um replace com Espaço
                            //VALOR_UNIT_PARA = (item.NF_PROD_VUNCOM ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            //VALOR_TOT_PARA = (item.NF_PROD_VPROD ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            VALOR_UNIT_PARA = (item.NF_PROD_VUNCOM ?? 0).ToString("C2").Replace("R$","").Trim(),
                            VALOR_TOT_PARA = (item.NF_PROD_VPROD ?? 0).ToString("C2").Replace("R$","").Trim(),
                            //Marcio Spinosa - Edson Caio Ridiculamente ridiculo concatenar string e dar um replace com Espaço
                            CFOP_PARA = string.IsNullOrWhiteSpace(item.CFOP_ESCRITURAR) ? "0000" : item.CFOP_ESCRITURAR,
                            NCM_PARA = item.NF_PROD_NCM,
                            UNIDADE_PARA = string.IsNullOrWhiteSpace(vStrMaterialUnidade) ? item.itm_de_para == null ? item.NF_PROD_UCOM : item.itm_de_para.UNIDADE_PARA : vStrMaterialUnidade,
                            MAT_ORIG_PARA = string.IsNullOrWhiteSpace(vStrMaterialOrigem) ? item.itm_de_para == null ? short.Parse(item.NF_ICMS_ORIG) : item.itm_de_para.MAT_ORIG_PARA : short.Parse(vStrMaterialOrigem),
                            MAT_USO = string.IsNullOrWhiteSpace(vStrMaterialUtilizacao) ? item.itm_de_para == null ? "" : item.itm_de_para.MAT_USO : vStrMaterialUtilizacao,
                            MAT_GRUPO_MATKL = string.IsNullOrWhiteSpace(vStrMaterialGrupo) ? item.itm_de_para == null ? item.NF_PROD_UCOM : item.itm_de_para.MAT_GRUPO_MATKL : vStrMaterialGrupo,  
                            DOCNUM_REF = "",
                            ITEM_DOCNUM_REF = ""
                        });
                        TBJ1B1N_ITEM_NFE vObjJ1B1N_ITEM_NFE = new TBJ1B1N_ITEM_NFE();
                        vObjJ1B1N_ITEM_NFE.NFEID = pStrNFEID;
                        vObjJ1B1N_ITEM_NFE.ITEM_NF = item.NF_PROD_ITEM;
                        vObjJ1B1N_ITEM_NFE.ITEM_TYPE_DE = "1";
                        vObjJ1B1N_ITEM_NFE.ITEM_TYPE_PARA = "1";
                        vObjJ1B1N_ITEM_NFE.COD_MATERIAL_DE = item.NF_PROD_CPROD.Substring(0, Math.Min(item.NF_PROD_CPROD.Length, 18));
                        vObjJ1B1N_ITEM_NFE.COD_MATERIAL_PARA = item.itm_de_para == null ? item.NF_PROD_CPROD.Substring(0, Math.Min(item.NF_PROD_CPROD.Length, 18)) : item.itm_de_para.COD_MATERIAL_PARA;
                        vObjJ1B1N_ITEM_NFE.PLANTA_DE = item.PLANTA;
                        vObjJ1B1N_ITEM_NFE.PLANTA_PARA = item.PLANTA;
                        vObjJ1B1N_ITEM_NFE.QUANTIDADE_DE = item.NF_PROD_QCOM ?? 0;
                        vObjJ1B1N_ITEM_NFE.QUANTIDADE_PARA = item.NF_PROD_QCOM ?? 0;
                        vObjJ1B1N_ITEM_NFE.VALOR_UNIT_DE = item.NF_PROD_VUNCOM ?? 0;
                        vObjJ1B1N_ITEM_NFE.VALOR_UNIT_PARA = item.NF_PROD_VUNCOM ?? 0;
                        vObjJ1B1N_ITEM_NFE.VALOR_TOT_DE = item.NF_PROD_VPROD ?? 0;
                        vObjJ1B1N_ITEM_NFE.VALOR_TOT_PARA = item.NF_PROD_VPROD ?? 0;
                        vObjJ1B1N_ITEM_NFE.CFOP_DE = string.IsNullOrWhiteSpace(item.CFOP_ESCRITURAR) ? "0000" : item.CFOP_ESCRITURAR;
                        vObjJ1B1N_ITEM_NFE.CFOP_PARA = string.IsNullOrWhiteSpace(item.CFOP_ESCRITURAR) ? "0000" : item.CFOP_ESCRITURAR;
                        vObjJ1B1N_ITEM_NFE.NCM_DE = item.NF_PROD_NCM;
                        vObjJ1B1N_ITEM_NFE.NCM_PARA = item.NF_PROD_NCM;
                        vObjJ1B1N_ITEM_NFE.UNIDADE_DE = string.IsNullOrWhiteSpace(vStrMaterialUnidade) ? item.NF_PROD_UCOM : vStrMaterialUnidade;
                        vObjJ1B1N_ITEM_NFE.UNIDADE_PARA = string.IsNullOrWhiteSpace(vStrMaterialUnidade) ? item.itm_de_para == null  ? item.NF_PROD_UCOM : item.itm_de_para.UNIDADE_PARA : vStrMaterialUnidade;
                        vObjJ1B1N_ITEM_NFE.MAT_ORIG_DE = string.IsNullOrWhiteSpace(vStrMaterialOrigem) ? item.itm_de_para == null  ? short.Parse(item.NF_ICMS_ORIG) : item.itm_de_para.MAT_ORIG_PARA : short.Parse(vStrMaterialOrigem);
                        vObjJ1B1N_ITEM_NFE.MAT_ORIG_PARA = string.IsNullOrWhiteSpace(vStrMaterialOrigem) ? item.itm_de_para == null ? short.Parse(item.NF_ICMS_ORIG) : item.itm_de_para.MAT_ORIG_PARA : short.Parse(vStrMaterialOrigem);
                        vObjJ1B1N_ITEM_NFE.CNPJ_EMITENTE = item.NF_EMIT_CNPJ;
                        vObjJ1B1N_ITEM_NFE.MAT_DESCRICAO_MAKTX = string.IsNullOrWhiteSpace(vStrDescricaoMaterial) ? item.itm_de_para == null ? item.NF_PROD_XPROD.Substring(0, Math.Min(item.NF_PROD_XPROD.Length, 40)) : item.itm_de_para.MAT_DESCRICAO_MAKTX : vStrDescricaoMaterial;
                        vObjJ1B1N_ITEM_NFE.MAT_USO = string.IsNullOrWhiteSpace(vStrMaterialUtilizacao) ? item.itm_de_para == null  ? "" : item.itm_de_para.MAT_USO : vStrMaterialUtilizacao;
                        vObjJ1B1N_ITEM_NFE.MAT_GRUPO_MATKL = string.IsNullOrWhiteSpace(vStrMaterialGrupo) ?item.itm_de_para == null  ? "" : item.itm_de_para.MAT_GRUPO_MATKL : vStrMaterialGrupo;
                        vLstJ1B1N_ITEM_NFE.Add(vObjJ1B1N_ITEM_NFE);
         
                    }
                }
                else
                {
                    foreach (var item in nfitem)
                    {
                        var quantidadePara = item.ij1 == null  ? item.NF_PROD_QCOM : item.ij1.QUANTIDADE_PARA;
                        var valorUnitarioPara = item.ij1 == null ? item.NF_PROD_VUNCOM : item.ij1.VALOR_UNIT_PARA;
                        var valorTotalPara = Math.Round((quantidadePara ?? 0) * (valorUnitarioPara ?? 0), 2);

                        vObjArrayNFItems.Add(new
                        {
                            ITEM_NF = item.NF_PROD_ITEM,
                            ITEM_TYPE_PARA = item.ij1 == null  ? "1" : item.ij1.ITEM_TYPE_PARA,
                            COD_MATERIAL_PARA = item.ij1 == null  ? item.NF_PROD_CPROD : item.ij1.COD_MATERIAL_PARA,
                            MAT_DESCRICAO_MAKTX = item.ij1 == null  ? item.NF_PROD_XPROD.Substring(0,40) : item.ij1.MAT_DESCRICAO_MAKTX,
                            PLANTA_PARA = item.ij1 == null  ? item.PLANTA : item.ij1.PLANTA_PARA,
                            QUANTIDADE_PARA =   item.ij1 == null  ? item.NF_PROD_QCOM.ToString().Replace(".", ",") : item.ij1.QUANTIDADE_PARA.ToString().Replace(".", ","),
                            VALOR_UNIT_PARA = item.ij1 == null ? (item.NF_PROD_VUNCOM ?? 0).ToString("C2").Replace("R$", "").Trim() : item.ij1.VALOR_UNIT_PARA.ToString("C2").Replace("R$", "").Trim(),
                            VALOR_TOT_PARA = valorTotalPara.ToString("C2").Replace("R$", "").Trim(),
                            CFOP_PARA = item.ij1 == null  ? item.CFOP_ESCRITURAR : item.ij1.CFOP_PARA,
                            NCM_PARA = item.ij1 == null  ? item.NF_PROD_NCM : item.ij1.NCM_PARA,
                            UNIDADE_PARA = item.ij1 == null  ? item.NF_PROD_UCOM : item.ij1.UNIDADE_PARA,
                            MAT_ORIG_PARA = item.ij1 == null  ? item.NF_ICMS_ORIG : item.ij1.MAT_ORIG_PARA.ToString(),
                            MAT_USO = item.ij1.MAT_USO,
                            MAT_GRUPO_MATKL = item.ij1.MAT_GRUPO_MATKL,
                            DOCNUM_REF = item.ij1.DOCNUM_REF,
                            ITEM_DOCNUM_REF = item.ij1.ITEM_DOCNUM_REF
                        });
                    }
                }

                pLstJ1B1N_ITEM_NFE = vLstJ1B1N_ITEM_NFE;
                return Serialization.JSON.CreateString(vObjArrayNFItems);
            }
            catch //(Exception ex)
            {
                throw;
            }
        }

        public string GetPartnerTypeFromCFOPFirstNFItem(string pStrNFEID)
        {
            try
            {
   
                //Recuperando o primeiro iem da nota
                var FirstNFeItem = db.TbDOC_ITEM_NFE.Where(x => x.NFEID == pStrNFEID)
                                                     .Min(y => y.NF_PROD_ITEM);

                var CFOPItem = db.TbDOC_ITEM_NFE.Where(x => x.NFEID == pStrNFEID && x.NF_PROD_ITEM == FirstNFeItem)
                                            .Select(x => x.NF_PROD_CFOP).FirstOrDefault().ToInt();


                var PartnerType = db.TBJ1B1N_CFOP_PARCEIRO.Where(x => x.CFOP == CFOPItem)
                                                        .Select(x => x.TIPO_PARCEIRO).FirstOrDefault();

                return PartnerType;
            }
            catch (Exception ex)
            {
                
                throw;
            }
        }

        public string GetJsonJQGridNFItensTaxByChave(string pStrNFEID, out List<TBJ1B1N_ITEMTAX_NFE> pLstJ1B1N_ITEMTAX_NFE)
        {
            ArrayList vObjArrayItemTaxCollection = new ArrayList();
            ArrayList vObjArrayNFItemsTax;
            List<TBJ1B1N_ITEMTAX_NFE> vLstJ1B1N_ITEMTAX_NFE = new List<TBJ1B1N_ITEMTAX_NFE>();
            pLstJ1B1N_ITEMTAX_NFE = null;
            int vIntItemProcessado = 0;
            double? vDblExBase = 0.0;
            double? vDbOtBase = 0.0;

            try
            {
                var nfitem = (from i in db.TbDOC_ITEM_NFE
                              join c in db.TbDOC_CAB_NFE on i.NFEID equals c.NFEID
                              let tipoImpostoICMS = db.VWJ1B1N_IMPOSTO_POR_CFOP_ICMS
                                                .Where(x => x.CFOP == i.NF_PROD_CFOP)
                                                .FirstOrDefault()
                              let tipoImpostoIPI = db.VWJ1B1N_IMPOSTO_POR_CFOP_IPI
                                                .Where(x => x.CFOP == i.NF_PROD_CFOP)
                                                .FirstOrDefault()
                              let tipoImpostoPIS = db.VWJ1B1N_IMPOSTO_POR_CFOP_PIS
                                                .Where(x => x.CFOP == i.NF_PROD_CFOP)
                                                .FirstOrDefault()
                              let tipoImpostoCOFINS = db.VWJ1B1N_IMPOSTO_POR_CFOP_COFINS
                                                .Where(x => x.CFOP == i.NF_PROD_CFOP)
                                                .FirstOrDefault()
                              let leiICMS = db.VWJ1B1N_DIREITO_FISCAL_ICMS
                                              .Where(p2 => p2.TIPO_IMPOSTO == "ICMS" && ((p2.UF_DEST == c.NF_DEST_UF && p2.CFOP == i.NF_PROD_CFOP) || (p2.UF_DEST == c.NF_DEST_UF && p2.VALOR_PADRAO == "X")))
                                              .OrderByDescending(p2 => p2.CFOP)
                                              .FirstOrDefault()
                              let leiIPI = db.VWJ1B1N_DIREITO_FISCAL_IPI
                                              .Where(p2 => p2.TIPO_IMPOSTO == "IPI" && ((p2.UF_DEST == c.NF_DEST_UF && p2.CFOP == i.NF_PROD_CFOP) || (p2.UF_DEST == c.NF_DEST_UF && p2.VALOR_PADRAO == "X")))
                                              .OrderByDescending(p2 => p2.CFOP)
                                              .FirstOrDefault()
                              let leiPIS = db.VWJ1B1N_DIREITO_FISCAL_PIS
                                              .Where(p2 => p2.TIPO_IMPOSTO == "PIS" && ((p2.UF_DEST == c.NF_DEST_UF && p2.CFOP == i.NF_PROD_CFOP) || (p2.UF_DEST == c.NF_DEST_UF && p2.VALOR_PADRAO == "X")))
                                              .OrderByDescending(p2 => p2.CFOP)
                                              .FirstOrDefault()
                              let leiCOFINS = db.VWJ1B1N_DIREITO_FISCAL_COFINS
                                              .Where(p2 => p2.TIPO_IMPOSTO == "COFI" && ((p2.UF_DEST == c.NF_DEST_UF && p2.CFOP == i.NF_PROD_CFOP) || (p2.UF_DEST == c.NF_DEST_UF && p2.VALOR_PADRAO == "X")))
                                              .OrderByDescending(p2 => p2.CFOP)
                                              .FirstOrDefault()
                              join ij1 in db.TBJ1B1N_ITEMTAX_NFE on new { i.NFEID, ITEM = i.NF_PROD_ITEM } equals new { ij1.NFEID, ITEM = ij1.ITEM_NF } into jij1
                              from ij1 in jij1.DefaultIfEmpty()
                              where i.NFEID == pStrNFEID
                              select new
                              {
                                  i.NFEID,
                                  i.NF_PROD_ITEM,
                                  i.NF_ICMS_CST,
                                  i.NF_ICMS_VBC,
                                  i.NF_ICMS_PICMS,
                                  i.NF_ICMS_VICMS,
                                  i.NF_IPI_CST,
                                  i.NF_IPI_VBC,
                                  i.NF_IPI_PIPI,
                                  i.NF_IPI_VIPI,
                                  i.NF_PIS_CST,
                                  i.NF_PIS_VBC,
                                  i.NF_PIS_PPIS,
                                  i.NF_PIS_VPIS,
                                  i.NF_COFINS_CST,
                                  i.NF_COFINS_VBC,
                                  i.NF_COFINS_PCOFINS,
                                  i.NF_COFINS_VCOFINS,
                                  i.NF_PROD_CFOP,
                                  i.NF_PROD_VPROD,
                                  ij1,
                                  tipoImpostoICMS.TIPO_IMPOSTO_ICMS,
                                  tipoImpostoICMS.CFOP,
                                  tipoImpostoICMS.BASE_ESCRITURAR,
                                  tipoImpostoIPI.TIPO_IMPOSTO_IPI,
                                  tipoImpostoPIS.TIPO_IMPOSTO_PIS,
                                  tipoImpostoCOFINS.TIPO_IMPOSTO_COFINS,
                                  leiICMS.DIREITO_FISCAL_ICMS,
                                  leiIPI.DIREITO_FISCAL_IPI,
                                  leiPIS.DIREITO_FISCAL_PIS,
                                  leiCOFINS.DIREITO_FISCAL_COFINS,
                              }).OrderBy(x => x.NF_PROD_ITEM).ToList();

                if (nfitem.Count == 0) throw new Exception("Verificar tabelas de parâmetro da J1B1N");
                //Caso a nota já existir carrega as informações que foram previamente editadas, caso contrário carrrega padrão

                if (nfitem[0].ij1 == null)
                {
                    foreach (var item in nfitem)
                    {
                        
                        if (item.NF_PROD_ITEM <= vIntItemProcessado)
                        {
                            continue;
                        }
                        vIntItemProcessado = item.NF_PROD_ITEM;
                        vObjArrayNFItemsTax = new ArrayList();
                        //ICMS
                        //Verifica se o CFOP do XML é igual ao da tabela de configuração de impostos, 
                        //caso seja irá atribuir o valor do produto em outras bases ou em exclusão
                        if (item.NF_PROD_CFOP == item.CFOP)
                        {
                            if (item.BASE_ESCRITURAR == "EX")
                            {
                                vDblExBase = item.NF_PROD_VPROD;
                            }
                            else
                            {
                                vDbOtBase = item.NF_PROD_VPROD;
                            }
                        }

                        vObjArrayNFItemsTax.Add(new
                        {
                            Imposto = "ICMS",
                            TIPO_PARA = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_ICMS) ? "" : item.TIPO_IMPOSTO_ICMS.Trim(),
                            LEI_PARA = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_ICMS) ? "" : item.DIREITO_FISCAL_ICMS.Trim(),
                            CST = string.IsNullOrWhiteSpace(item.NF_ICMS_CST) ? "" : item.NF_ICMS_CST.Trim(),
                            BASE_CALC_PARA = string.Format("{0:0.00}", 0.0).Replace(".00", ""),
                            ALIQUOTA_PARA = string.Format("{0:0.00}", item.NF_ICMS_PICMS).Replace(".00", ""),
                            BASE_EXCL_PARA = (vDblExBase ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            BASE_OUTR_PARA = (vDbOtBase ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            VALOR_PARA = (item.NF_ICMS_VICMS ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            ITEM_NF = item.NF_PROD_ITEM
                        });
                        TBJ1B1N_ITEMTAX_NFE vObjJ1B1N_ITEMTAX_ICMS_NFE = new TBJ1B1N_ITEMTAX_NFE();
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.NFEID = pStrNFEID;
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.ITEM_NF = item.NF_PROD_ITEM;
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.IMPOSTO = "ICMS";
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.TIPO_DE = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_ICMS) ? "" : item.TIPO_IMPOSTO_ICMS.Trim();
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.TIPO_PARA = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_ICMS) ? "" : item.TIPO_IMPOSTO_ICMS.Trim();
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.LEI_DE = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_ICMS) ? "" : item.DIREITO_FISCAL_ICMS.Trim();
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.LEI_PARA = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_ICMS) ? "" : item.DIREITO_FISCAL_ICMS.Trim();
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.BASE_CALC_DE = 0;
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.ALIQUOTA_DE = item.NF_ICMS_PICMS;
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.ALIQUOTA_PARA = item.NF_ICMS_PICMS;
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.VALOR_DE = item.NF_ICMS_VICMS;
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.VALOR_PARA = item.NF_ICMS_VICMS;
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.BASE_EXCL_DE = (decimal)vDblExBase;
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.BASE_EXCL_PARA = (decimal)vDblExBase;
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.BASE_OUTR_DE = (decimal)vDbOtBase;
                        vObjJ1B1N_ITEMTAX_ICMS_NFE.BASE_OUTR_PARA = (decimal)vDbOtBase;
                        vLstJ1B1N_ITEMTAX_NFE.Add(vObjJ1B1N_ITEMTAX_ICMS_NFE);



                        //IPI
                        vObjArrayNFItemsTax.Add(new
                        {
                            Imposto = "IPI",
                            TIPO_PARA = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_IPI) ? "" : item.TIPO_IMPOSTO_IPI.Trim(),
                            LEI_PARA = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_IPI) ? "" : item.DIREITO_FISCAL_IPI.Trim(),
                            CST = item.NF_IPI_CST,
                            BASE_CALC_PARA = string.Format("{0:0.00}", 0.0).Replace(".00", ""),
                            ALIQUOTA_PARA = string.Format("{0:0.00}", item.NF_IPI_PIPI).Replace(".00", ""),
                            BASE_EXCL_PARA = (vDblExBase ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            BASE_OUTR_PARA = (vDbOtBase ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            VALOR_PARA = (item.NF_IPI_VIPI ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            ITEM_NF = item.NF_PROD_ITEM
                        });
                        TBJ1B1N_ITEMTAX_NFE vObjJ1B1N_ITEMTAX_IPI_NFE = new TBJ1B1N_ITEMTAX_NFE();
                        vObjJ1B1N_ITEMTAX_IPI_NFE.NFEID = pStrNFEID;
                        vObjJ1B1N_ITEMTAX_IPI_NFE.ITEM_NF = item.NF_PROD_ITEM;
                        vObjJ1B1N_ITEMTAX_IPI_NFE.IMPOSTO = "IPI";
                        vObjJ1B1N_ITEMTAX_IPI_NFE.TIPO_DE = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_IPI) ? "" : item.TIPO_IMPOSTO_IPI.Trim();
                        vObjJ1B1N_ITEMTAX_IPI_NFE.TIPO_PARA = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_IPI) ? "" : item.TIPO_IMPOSTO_IPI.Trim();
                        vObjJ1B1N_ITEMTAX_IPI_NFE.LEI_DE = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_IPI) ? "" : item.DIREITO_FISCAL_IPI.Trim();
                        vObjJ1B1N_ITEMTAX_IPI_NFE.LEI_PARA = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_IPI) ? "" : item.DIREITO_FISCAL_IPI.Trim();
                        vObjJ1B1N_ITEMTAX_IPI_NFE.BASE_CALC_DE = 0;
                        vObjJ1B1N_ITEMTAX_IPI_NFE.ALIQUOTA_DE = item.NF_IPI_PIPI;
                        vObjJ1B1N_ITEMTAX_IPI_NFE.ALIQUOTA_PARA = item.NF_IPI_PIPI;
                        vObjJ1B1N_ITEMTAX_IPI_NFE.VALOR_DE = item.NF_IPI_VIPI;
                        vObjJ1B1N_ITEMTAX_IPI_NFE.VALOR_PARA = item.NF_IPI_VIPI;
                        vObjJ1B1N_ITEMTAX_IPI_NFE.BASE_OUTR_DE = (decimal)vDbOtBase;
                        vObjJ1B1N_ITEMTAX_IPI_NFE.BASE_OUTR_PARA = (decimal)vDbOtBase;
                        vObjJ1B1N_ITEMTAX_IPI_NFE.BASE_EXCL_DE = (decimal)vDblExBase;
                        vObjJ1B1N_ITEMTAX_IPI_NFE.BASE_EXCL_PARA = (decimal)vDblExBase;
                        vLstJ1B1N_ITEMTAX_NFE.Add(vObjJ1B1N_ITEMTAX_IPI_NFE);

                        //PIS
                        vObjArrayNFItemsTax.Add(new
                        {
                            Imposto = "PIS",
                            TIPO_PARA = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_PIS) ? "" : item.TIPO_IMPOSTO_PIS.Trim(),
                            LEI_PARA = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_PIS) ? "" : item.DIREITO_FISCAL_PIS.Trim(),
                            CST = item.NF_PIS_CST,
                            BASE_CALC_PARA = string.Format("{0:0.00}", 0.0).Replace(".00", ""),
                            ALIQUOTA_PARA = string.Format("{0:0.00}", item.NF_PIS_PPIS).Replace(".00", ""),
                            BASE_EXCL_PARA = "",
                            BASE_OUTR_PARA = "",
                            VALOR_PARA = (item.NF_PIS_VPIS ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            ITEM_NF = item.NF_PROD_ITEM
                        });
                        TBJ1B1N_ITEMTAX_NFE vObjJ1B1N_ITEMTAX_PIS_NFE = new TBJ1B1N_ITEMTAX_NFE();
                        vObjJ1B1N_ITEMTAX_PIS_NFE.NFEID = pStrNFEID;
                        vObjJ1B1N_ITEMTAX_PIS_NFE.ITEM_NF = item.NF_PROD_ITEM;
                        vObjJ1B1N_ITEMTAX_PIS_NFE.IMPOSTO = "PIS";
                        vObjJ1B1N_ITEMTAX_PIS_NFE.TIPO_DE = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_PIS) ? "" : item.TIPO_IMPOSTO_PIS.Trim();
                        vObjJ1B1N_ITEMTAX_PIS_NFE.TIPO_PARA = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_PIS) ? "" : item.TIPO_IMPOSTO_PIS.Trim();
                        vObjJ1B1N_ITEMTAX_PIS_NFE.LEI_DE = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_PIS) ? "" : item.DIREITO_FISCAL_PIS.Trim();
                        vObjJ1B1N_ITEMTAX_PIS_NFE.LEI_PARA = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_PIS) ? "" : item.DIREITO_FISCAL_PIS.Trim();
                        vObjJ1B1N_ITEMTAX_PIS_NFE.BASE_CALC_DE = 0;
                        vObjJ1B1N_ITEMTAX_PIS_NFE.ALIQUOTA_DE = item.NF_PIS_PPIS;
                        vObjJ1B1N_ITEMTAX_PIS_NFE.VALOR_DE = item.NF_PIS_VPIS;
                        vLstJ1B1N_ITEMTAX_NFE.Add(vObjJ1B1N_ITEMTAX_PIS_NFE);

                        //COFINS
                        vObjArrayNFItemsTax.Add(new
                        {
                            Imposto = "COFI",
                            TIPO_PARA = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_COFINS) ? "" : item.TIPO_IMPOSTO_COFINS.Trim(),
                            LEI_PARA = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_COFINS) ? "" : item.DIREITO_FISCAL_COFINS.Trim(),
                            CST = item.NF_COFINS_CST,
                            BASE_CALC_PARA = string.Format("{0:0.00}", 0.0).Replace(".00", ""),
                            ALIQUOTA_PARA = string.Format("{0:0.00}", item.NF_COFINS_PCOFINS).Replace(".00", ""),
                            BASE_EXCL_PARA = "",
                            BASE_OUTR_PARA = "",
                            VALOR_PARA = (item.NF_COFINS_VCOFINS ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            ITEM_NF = item.NF_PROD_ITEM
                        });
                        TBJ1B1N_ITEMTAX_NFE vObjJ1B1N_ITEMTAX_COFINS_NFE = new TBJ1B1N_ITEMTAX_NFE();
                        vObjJ1B1N_ITEMTAX_COFINS_NFE.NFEID = pStrNFEID;
                        vObjJ1B1N_ITEMTAX_COFINS_NFE.ITEM_NF = item.NF_PROD_ITEM;
                        vObjJ1B1N_ITEMTAX_COFINS_NFE.IMPOSTO = "COFI";
                        vObjJ1B1N_ITEMTAX_COFINS_NFE.TIPO_DE = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_COFINS) ? "" : item.TIPO_IMPOSTO_COFINS.Trim();
                        vObjJ1B1N_ITEMTAX_COFINS_NFE.TIPO_PARA = string.IsNullOrWhiteSpace(item.TIPO_IMPOSTO_COFINS) ? "" : item.TIPO_IMPOSTO_COFINS.Trim();
                        vObjJ1B1N_ITEMTAX_COFINS_NFE.LEI_DE = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_COFINS) ? "" : item.DIREITO_FISCAL_COFINS.Trim();
                        vObjJ1B1N_ITEMTAX_COFINS_NFE.LEI_PARA = string.IsNullOrWhiteSpace(item.DIREITO_FISCAL_COFINS) ? "" : item.DIREITO_FISCAL_COFINS.Trim();
                        vObjJ1B1N_ITEMTAX_COFINS_NFE.BASE_CALC_DE = 0;
                        vObjJ1B1N_ITEMTAX_COFINS_NFE.ALIQUOTA_DE = item.NF_COFINS_PCOFINS;
                        vObjJ1B1N_ITEMTAX_COFINS_NFE.VALOR_DE = item.NF_COFINS_VCOFINS;
                        vLstJ1B1N_ITEMTAX_NFE.Add(vObjJ1B1N_ITEMTAX_COFINS_NFE);

                        vObjArrayItemTaxCollection.Add(vObjArrayNFItemsTax.Clone());
                        vObjArrayNFItemsTax.Clear();
                    }
                }
                else
                {
                    DataLayer.DLTBJ1B1N_ITEMTAX_NFE vObjDLItemTax = new DLTBJ1B1N_ITEMTAX_NFE();
                    TBJ1B1N_ITEMTAX_NFE vObjItemTax = new TBJ1B1N_ITEMTAX_NFE();
                    foreach (var item in nfitem)
                    {

                        if (item.NF_PROD_ITEM <= vIntItemProcessado)
                        {
                            continue;
                        }
                        vIntItemProcessado = item.NF_PROD_ITEM;
                        vObjArrayNFItemsTax = new ArrayList();
                        //ICMS
                        vObjItemTax = vObjDLItemTax.db.TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.NFEID == item.NFEID && x.ITEM_NF == item.NF_PROD_ITEM && x.IMPOSTO.Trim() == "ICMS");
                        vObjArrayNFItemsTax.Add(new
                        {
                            Imposto = "ICMS",
                            TIPO_PARA = vObjItemTax.TIPO_PARA,
                            LEI_PARA = vObjItemTax.LEI_PARA,
                            CST = item.NF_ICMS_CST,
                            BASE_CALC_PARA = string.Format("{0:0.00}", 0.0).Replace(".00", ""),
                            ALIQUOTA_PARA = string.Format("{0:0.00}", vObjItemTax.ALIQUOTA_PARA).Replace(".00", ""),
                            BASE_EXCL_PARA = (vObjItemTax.BASE_EXCL_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            BASE_OUTR_PARA = (vObjItemTax.BASE_OUTR_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            VALOR_PARA = (vObjItemTax.VALOR_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            ITEM_NF = item.NF_PROD_ITEM
                        });

                        //IPI
                        vObjItemTax = vObjDLItemTax.db.TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.NFEID == item.NFEID && x.ITEM_NF == item.NF_PROD_ITEM && x.IMPOSTO.Trim() == "IPI");
                        vObjArrayNFItemsTax.Add(new
                        {
                            Imposto = "IPI",
                            TIPO_PARA = vObjItemTax.TIPO_PARA,
                            LEI_PARA = vObjItemTax.LEI_PARA,
                            CST = item.NF_IPI_CST,
                            BASE_CALC_PARA = string.Format("{0:0.00}", 0.0).Replace(".00", ""),
                            ALIQUOTA_PARA = string.Format("{0:0.00}", vObjItemTax.ALIQUOTA_PARA).Replace(".00",""),
                            BASE_EXCL_PARA = (vObjItemTax.BASE_EXCL_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            BASE_OUTR_PARA = (vObjItemTax.BASE_OUTR_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            VALOR_PARA = (vObjItemTax.VALOR_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            ITEM_NF = item.NF_PROD_ITEM
                        });

                        //PIS
                        vObjItemTax = vObjDLItemTax.db.TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.NFEID == item.NFEID && x.ITEM_NF == item.NF_PROD_ITEM && x.IMPOSTO.Trim() == "PIS");
                        vObjArrayNFItemsTax.Add(new
                        {
                            Imposto = "PIS",
                            TIPO_PARA = vObjItemTax.TIPO_PARA,
                            LEI_PARA = vObjItemTax.LEI_PARA,
                            CST = item.NF_PIS_CST,
                            BASE_CALC_PARA = string.Format("{0:0.00}", 0.0).Replace(".00", ""),
                            ALIQUOTA_PARA = string.Format("{0:0.00}", vObjItemTax.ALIQUOTA_PARA).Replace(".00",""),
                            BASE_EXCL_PARA = (vObjItemTax.BASE_EXCL_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            BASE_OUTR_PARA = (vObjItemTax.BASE_OUTR_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            VALOR_PARA = (vObjItemTax.VALOR_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            ITEM_NF = item.NF_PROD_ITEM
                        });

                        //COFINS
                        vObjItemTax = vObjDLItemTax.db.TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.NFEID == item.NFEID && x.ITEM_NF == item.NF_PROD_ITEM && x.IMPOSTO.Trim() == "COFI");
                        vObjArrayNFItemsTax.Add(new
                        {
                            Imposto = "COFI",
                            TIPO_PARA = vObjItemTax.TIPO_PARA,
                            LEI_PARA = vObjItemTax.LEI_PARA,
                            CST = item.NF_COFINS_CST,
                            BASE_CALC_PARA = string.Format("{0:0.00}", 0.0).Replace(".00", ""),
                            ALIQUOTA_PARA = string.Format("{0:0.00}", vObjItemTax.ALIQUOTA_PARA).Replace(".00",""),
                            BASE_EXCL_PARA = (vObjItemTax.BASE_EXCL_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            BASE_OUTR_PARA = (vObjItemTax.BASE_OUTR_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            VALOR_PARA = (vObjItemTax.VALOR_PARA ?? 0).ToString("C2").Replace("R$", "").Trim(),
                            ITEM_NF = item.NF_PROD_ITEM
                        });

                        if (!vObjArrayItemTaxCollection.Contains(vObjArrayNFItemsTax))
                        {
                            vObjArrayItemTaxCollection.Add(vObjArrayNFItemsTax.Clone());
                        }

                        vObjArrayNFItemsTax.Clear();
                    }
                }
                
                pLstJ1B1N_ITEMTAX_NFE = vLstJ1B1N_ITEMTAX_NFE;
                return Serialization.JSON.CreateString(vObjArrayItemTaxCollection);

            }
            catch
            {
                throw;
            }
        }

        public SAP_RFC.MainDataJ1B1NFilter GetSearchFilterMaterialJ1B1N(string pStrNFEID)
        {
            SAP_RFC.MainDataJ1B1NFilter vObjFilter = new MetsoFramework.SAP.SAP_RFC.MainDataJ1B1NFilter();
            SAP_RFC.MainDataSearchItemsJ1B1N vObjItemtoSearch = new SAP_RFC.MainDataSearchItemsJ1B1N();
            vObjFilter.SearchItems = new List<SAP_RFC.MainDataSearchItemsJ1B1N>();
            long vLngCodmaterial;

            try
            {
                var nfitem = (from i in db.TbDOC_ITEM_NFE
                              join c in db.TbDOC_CAB_NFE on i.NFEID equals c.NFEID
                              join p in db.TBJ1B1N_PLANTA_METSO_PARCEIRO on c.NF_DEST_CNPJ equals p.CNPJ
                              //left outer join novamente com a tabela de itens para procurar de/para de dados para preenchimento automativo
                              join itm_de_para in db.VWJ1B1N_DE_PARA_MATERIAL on new { CNPJ_EMITENTE = c.NF_EMIT_CNPJ, MATERIAL = i.NF_PROD_CPROD } equals new { itm_de_para.CNPJ_EMITENTE, MATERIAL = itm_de_para.COD_MATERIAL_DE } into jitm_de_para
                              from itm_de_para in jitm_de_para.DefaultIfEmpty()
                              where i.NFEID == pStrNFEID
                              select new
                              {
                                  i.NF_PROD_ITEM,
                                  i.NF_PROD_CPROD,
                                  p.PLANTA,
                                  c.NF_EMIT_CNPJ,
                                  itm_de_para,
                              }).OrderBy(x => x.NF_PROD_ITEM).ToList();

            
                foreach (var item in nfitem)
                {
                    vObjItemtoSearch.Planta = item.PLANTA.Trim();
                    vObjItemtoSearch.NumeroMaterial = item.itm_de_para == null ? long.TryParse(item.NF_PROD_CPROD.Trim(), out vLngCodmaterial) ? vLngCodmaterial.ToString().Trim().PadLeft(18, '0') : item.NF_PROD_CPROD.Trim() : long.TryParse(item.itm_de_para.COD_MATERIAL_PARA.Trim(),out vLngCodmaterial) ? vLngCodmaterial.ToString().Trim().PadLeft(18, '0') : item.itm_de_para.COD_MATERIAL_PARA.Trim();
                    vObjItemtoSearch.NFItemNumero = item.NF_PROD_ITEM;

                    vObjFilter.SearchItems.Add(vObjItemtoSearch);
                }



                return vObjFilter;
            }
            catch //(Exception ex)
            {
                throw;
            }
        }

       
    }
}