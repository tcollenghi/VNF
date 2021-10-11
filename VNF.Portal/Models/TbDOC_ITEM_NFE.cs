//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace VNF.Portal.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class TbDOC_ITEM_NFE
    {
        public TbDOC_ITEM_NFE()
        {
            this.TBDOC_CONSIGNACAO_REFNF = new HashSet<TBDOC_CONSIGNACAO_REFNF>();
            this.TBDOC_SUBCONTRATACAO_REFNF = new HashSet<TBDOC_SUBCONTRATACAO_REFNF>();
            this.TBDOC_NOTA_COMPLEMENTAR_REFNF = new HashSet<TBDOC_NOTA_COMPLEMENTAR_REFNF>();
        }
    
        public string NFEID { get; set; }
        public int NF_PROD_ITEM { get; set; }
        public string NF_PROD_CPROD { get; set; }
        public string NF_PROD_CEAN { get; set; }
        public string NF_PROD_XPROD { get; set; }
        public string NF_PROD_NCM { get; set; }
        public string NF_PROD_CFOP { get; set; }
        public string NF_PROD_CFOP_DESC { get; set; }
        public string NF_PROD_UCOM { get; set; }
        public Nullable<double> NF_PROD_QCOM { get; set; }
        public Nullable<double> NF_PROD_VUNCOM { get; set; }
        public Nullable<double> NF_PROD_VPROD { get; set; }
        public string NF_PROD_UTRIB { get; set; }
        public Nullable<double> NF_PROD_QTRIB { get; set; }
        public Nullable<double> NF_PROD_VDESC { get; set; }
        public string NF_PROD_INF_ADICIONAL_ITEM { get; set; }
        public string NF_PROD_NVE { get; set; }
        public string NF_PROD_EXTIPI { get; set; }
        public Nullable<decimal> NF_PROD_VFRETE { get; set; }
        public Nullable<decimal> NF_PROD_VSEG { get; set; }
        public Nullable<decimal> NF_PROD_VOUTRO { get; set; }
        public Nullable<int> NF_PROD_INDTOT { get; set; }
        public string NF_PROD_DI { get; set; }
        public string NF_PROD_DETESPECIFICO { get; set; }
        public string NF_PROD_XPED { get; set; }
        public Nullable<int> NF_PROD_NITEMPED { get; set; }
        public string NF_PROD_FCI { get; set; }
        public Nullable<decimal> NF_ICMS_PICMS { get; set; }
        public string NF_ICMS_ORIG { get; set; }
        public string NF_ICMS_CST { get; set; }
        public Nullable<int> NF_ICMS_MODBC { get; set; }
        public Nullable<decimal> NF_ICMS_PREDBC { get; set; }
        public Nullable<decimal> NF_ICMS_VBC { get; set; }
        public Nullable<decimal> NF_ICMS_VICMS { get; set; }
        public Nullable<int> NF_ICMS_MODBCST { get; set; }
        public Nullable<decimal> NF_ICMS_MVAST { get; set; }
        public Nullable<decimal> NF_ICMSREDBCST { get; set; }
        public Nullable<decimal> NF_ICMS_VBCST { get; set; }
        public Nullable<decimal> NF_ICMS_PICMSST { get; set; }
        public Nullable<decimal> NF_ICMS_VICMSST { get; set; }
        public Nullable<decimal> NF_ICMS_VBCSTRET { get; set; }
        public Nullable<decimal> NF_ICMS_VBCSTDEST { get; set; }
        public Nullable<decimal> NF_ICMS_VICMSSTDEST { get; set; }
        public Nullable<int> NF_ICMS_MOTDESICMS { get; set; }
        public Nullable<decimal> NF_ICMS_PBCOP { get; set; }
        public string NF_ICMS_UFST { get; set; }
        public Nullable<decimal> NF_ICMS_PCREDSN { get; set; }
        public Nullable<decimal> NF_ICMS_VCREICMSSN { get; set; }
        public Nullable<decimal> NF_ICMS_VICMSDESON { get; set; }
        public Nullable<decimal> NF_ICMS_VICMSOP { get; set; }
        public Nullable<decimal> NF_ICMS_PDIF { get; set; }
        public Nullable<decimal> NF_ICMS_VICMSDIF { get; set; }
        public string NF_IPI_CLENQ { get; set; }
        public string NF_IPI_CNPJPROD { get; set; }
        public string NF_IPI_CSELO { get; set; }
        public Nullable<decimal> NF_IPI_QSELO { get; set; }
        public string NF_IPI_CENQ { get; set; }
        public string NF_IPI_CST { get; set; }
        public Nullable<decimal> NF_IPI_VBC { get; set; }
        public Nullable<decimal> NF_IPI_PIPI { get; set; }
        public Nullable<decimal> NF_IPI_VIPI { get; set; }
        public Nullable<decimal> NF_IPI_QUNID { get; set; }
        public Nullable<decimal> NF_IPI_VUNID { get; set; }
        public Nullable<decimal> NF_II_VBC { get; set; }
        public Nullable<decimal> NF_II_VDESPADU { get; set; }
        public Nullable<decimal> NF_II_VII { get; set; }
        public Nullable<decimal> NF_II_VIOF { get; set; }
        public string NF_PIS_CST { get; set; }
        public Nullable<decimal> NF_PIS_VBC { get; set; }
        public Nullable<decimal> NF_PIS_PPIS { get; set; }
        public Nullable<decimal> NF_PIS_VPIS { get; set; }
        public Nullable<decimal> NF_PIS_QBCPROD { get; set; }
        public Nullable<decimal> NF_PIS_VALIQPROD { get; set; }
        public Nullable<decimal> NF_PISST_VBC { get; set; }
        public Nullable<decimal> NF_PISST_PPIS { get; set; }
        public Nullable<decimal> NF_PISST_VPIS { get; set; }
        public Nullable<decimal> NF_PISST_QBCPROD { get; set; }
        public Nullable<decimal> NF_PISST_VALIQPROD { get; set; }
        public string NF_COFINS_CST { get; set; }
        public Nullable<decimal> NF_COFINS_VBC { get; set; }
        public Nullable<decimal> NF_COFINS_PCOFINS { get; set; }
        public Nullable<decimal> NF_COFINS_VCOFINS { get; set; }
        public Nullable<decimal> NF_COFINS_QBCPROD { get; set; }
        public Nullable<decimal> NF_COFINS_VALIQPROD { get; set; }
        public Nullable<decimal> NF_COFINSST_VBC { get; set; }
        public Nullable<decimal> NF_COFINSST_PCOFINS { get; set; }
        public Nullable<decimal> NF_COFINSST_VCOFINS { get; set; }
        public Nullable<decimal> NF_COFINSST_QBCPROD { get; set; }
        public Nullable<decimal> NF_COFINSST_VALIQPROD { get; set; }
        public Nullable<decimal> NF_ISSQN_VBC { get; set; }
        public Nullable<decimal> NF_ISSQN_VALIQ { get; set; }
        public Nullable<decimal> NF_ISSQN_VISSQN { get; set; }
        public string NF_ISSQN_CMUNFG { get; set; }
        public string NF_ISSQN_CLISTSERV { get; set; }
        public Nullable<decimal> NF_ISSQN_VDEDUCAO { get; set; }
        public Nullable<decimal> NF_ISSQN_VOUTRO { get; set; }
        public Nullable<decimal> NF_ISSQN_VDESCINCOND { get; set; }
        public Nullable<decimal> NF_ISSQN_VDESCCOND { get; set; }
        public Nullable<decimal> NF_ISSQN_VISSRET { get; set; }
        public Nullable<int> NF_ISSQN_INDISS { get; set; }
        public string NF_ISSQN_CSERVICO { get; set; }
        public string NF_ISSQN_CMUN { get; set; }
        public string NF_ISSQN_CPAIS { get; set; }
        public string NF_ISSQN_NPROCESSO { get; set; }
        public Nullable<int> NF_ISSQN_INDINCENTIVO { get; set; }
        public Nullable<decimal> NF_ICMSUFDEST_VBCUFDEST { get; set; }
        public Nullable<decimal> NF_ICMSUFDEST_PFCPUDEST { get; set; }
        public Nullable<decimal> NF_ICMSUFDEST_PICMSUFDEST { get; set; }
        public Nullable<decimal> NF_ICMSUFDEST_PICMSINTER { get; set; }
        public Nullable<decimal> NF_ICMSUFDEST_PICMSINTERPART { get; set; }
        public Nullable<decimal> NF_ICMSUFDEST_VFCPUFDEST { get; set; }
        public Nullable<decimal> NF_ICMSUFDEST_VICMSUFDEST { get; set; }
        public Nullable<decimal> NF_ICMSUFDEST_VICMSUFREMET { get; set; }
    
        public virtual ICollection<TBDOC_CONSIGNACAO_REFNF> TBDOC_CONSIGNACAO_REFNF { get; set; }
        public virtual ICollection<TBDOC_SUBCONTRATACAO_REFNF> TBDOC_SUBCONTRATACAO_REFNF { get; set; }
        public virtual ICollection<TBDOC_NOTA_COMPLEMENTAR_REFNF> TBDOC_NOTA_COMPLEMENTAR_REFNF { get; set; }
    }
}