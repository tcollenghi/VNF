using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VNF.Portal.Models;

namespace VNF.Portal.DataLayer
{
    public class DLTBJ1B1N_CAB_NFE : Repository<TBJ1B1N_CAB_NFE>
    {
        public TBJ1B1N_CAB_NFE GetByChave(string pStrNFEID)
        {
            return (from p in db.TBJ1B1N_CAB_NFE
                    where p.NFEID == pStrNFEID
                    select p).FirstOrDefault();
        }
        public void SalvarAlteracoes(TBJ1B1N_CAB_NFE pObjJ1B1N_CAB_NFE)
        {

            try
            {
                //Recuperando registro entidade a ser atualizada
                var Cabecalho = this.GetByChave(pObjJ1B1N_CAB_NFE.NFEID);

                //Atualizando campos do cabeçalho da nota
                Cabecalho.NFTYPE_PARA = pObjJ1B1N_CAB_NFE.NFTYPE_PARA;
                Cabecalho.PARTNER_FUNCTION_PARA = pObjJ1B1N_CAB_NFE.PARTNER_FUNCTION_PARA;
                Cabecalho.PARTNER_TYPE_PARA = pObjJ1B1N_CAB_NFE.PARTNER_TYPE_PARA;
                Cabecalho.PARTNER_ID = pObjJ1B1N_CAB_NFE.PARTNER_ID;
                Cabecalho.DOCNUM_REF = pObjJ1B1N_CAB_NFE.DOCNUM_REF;
                Cabecalho.OBSERVACAO = pObjJ1B1N_CAB_NFE.OBSERVACAO;
                Cabecalho.SALVO = "1";

                //Atualizando informações dos itens da nota
                foreach (var item in pObjJ1B1N_CAB_NFE.TBJ1B1N_ITEM_NFE)
                {
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).CFOP_PARA = item.CFOP_PARA;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).COD_MATERIAL_PARA = item.COD_MATERIAL_PARA;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).DOCNUM_REF = item.DOCNUM_REF;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).ITEM_DOCNUM_REF = item.ITEM_DOCNUM_REF;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).ITEM_TYPE_PARA = item.ITEM_TYPE_PARA;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).NCM_PARA = item.NCM_PARA;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).PLANTA_PARA = item.PLANTA_PARA;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).QUANTIDADE_PARA = item.QUANTIDADE_PARA;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).UNIDADE_PARA = item.UNIDADE_PARA;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).VALOR_TOT_PARA = item.VALOR_TOT_PARA;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).VALOR_UNIT_PARA = item.VALOR_UNIT_PARA;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).MAT_ORIG_PARA = item.MAT_ORIG_PARA;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).MAT_USO = item.MAT_USO;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).MAT_GRUPO_MATKL = item.MAT_GRUPO_MATKL;
                    Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).MAT_DESCRICAO_MAKTX = item.MAT_DESCRICAO_MAKTX;
                    //Atualizando informações dos impostos
                    foreach (var itemtax in pObjJ1B1N_CAB_NFE.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).TBJ1B1N_ITEMTAX_NFE)
                    {
                        Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == itemtax.ITEM_NF && x.IMPOSTO.Trim() == itemtax.IMPOSTO).ALIQUOTA_PARA = itemtax.ALIQUOTA_PARA;
                        Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == itemtax.ITEM_NF && x.IMPOSTO.Trim() == itemtax.IMPOSTO).BASE_CALC_PARA = itemtax.BASE_CALC_PARA;
                        Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == itemtax.ITEM_NF && x.IMPOSTO.Trim() == itemtax.IMPOSTO).BASE_EXCL_PARA = itemtax.BASE_EXCL_PARA;
                        Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == itemtax.ITEM_NF && x.IMPOSTO.Trim() == itemtax.IMPOSTO).BASE_OUTR_PARA = itemtax.BASE_OUTR_PARA;
                        Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == itemtax.ITEM_NF && x.IMPOSTO.Trim() == itemtax.IMPOSTO).LEI_PARA = itemtax.LEI_PARA;
                        Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == itemtax.ITEM_NF && x.IMPOSTO.Trim() == itemtax.IMPOSTO).TIPO_PARA = itemtax.TIPO_PARA;
                        Cabecalho.TBJ1B1N_ITEM_NFE.FirstOrDefault(x => x.ITEM_NF == item.ITEM_NF).TBJ1B1N_ITEMTAX_NFE.FirstOrDefault(x => x.ITEM_NF == itemtax.ITEM_NF && x.IMPOSTO.Trim() == itemtax.IMPOSTO).VALOR_PARA = itemtax.VALOR_PARA;

                    }

                }



                this.db.SaveChanges();

            }
            catch (Exception ex)
            {
                
                throw;
            }
            
        }
       
    }
}