Imports MetsoFramework.SAP

Public Class modNFItem

    '--> INFORMAÇÕES DO CTE
    Public CT_INFNFE_CHAVE As String

    '--> INFORMAÇÕES DA NOTA FISCAL
    Public NF_PROD_ITEM As Integer
    Public NF_PROD_CPROD As String
    Public NF_PROD_CEAN As String
    Public NF_PROD_XPROD As String
    Public NF_PROD_NCM As String
    Public NF_PROD_CFOP As String
    Public NF_PROD_CFOP_DESC As String
    Public NF_PROD_UCOM As String
    Public NF_PROD_QCOM As Decimal
    Public NF_PROD_VUNCOM As Decimal
    Public NF_PROD_VPROD As Decimal
    Public NF_PROD_UTRIB As String
    Public NF_PROD_QTRIB As Decimal
    Public NF_PROD_VDESC As Decimal
    Public NF_PROD_INF_ADICIONAL_ITEM As String
    Public NF_PROD_NVE As String
    Public NF_PROD_EXTIPI As String
    Public NF_PROD_VFRETE As Decimal
    Public NF_PROD_VSEG As Decimal
    Public NF_PROD_VOUTRO As Decimal
    Public NF_PROD_INDTOT As Integer
    Public NF_PROD_DI As String
    Public NF_PROD_DETESPECIFICO As String
    Public NF_PROD_XPED As String
    Public NF_PROD_NITEMPED As Integer
    Public NF_PROD_FCI As String

    Public NF_ICMS_PICMS As Decimal
    Public NF_ICMS_ORIG As String
    Public NF_ICMS_CST As String
    Public NF_ICMS_MODBC As Integer
    Public NF_ICMS_PREDBC As Decimal
    Public NF_ICMS_VBC As Decimal
    Public NF_ICMS_VICMS As Decimal
    Public NF_ICMS_MODBCST As Integer
    Public NF_ICMS_MVAST As Decimal
    Public NF_ICMSREDBCST As Decimal
    Public NF_ICMS_VBCST As Decimal
    Public NF_ICMS_PICMSST As Decimal
    Public NF_ICMS_VICMSST As Decimal
    Public NF_ICMS_VBCSTRET As Decimal
    Public NF_ICMS_VBCSTDEST As Decimal
    Public NF_ICMS_VICMSSTDEST As Decimal
    Public NF_ICMS_MOTDESICMS As Integer
    Public NF_ICMS_PBCOP As Decimal
    Public NF_ICMS_UFST As String
    Public NF_ICMS_PCREDSN As Decimal
    Public NF_ICMS_VCREICMSSN As Decimal
    Public NF_ICMS_VICMSDESON As Decimal
    Public NF_ICMS_VICMSOP As Decimal
    Public NF_ICMS_PDIF As Decimal
    Public NF_ICMS_VICMSDIF As Decimal

    Public NF_IPI_CLENQ As String
    Public NF_IPI_CNPJPROD As String
    Public NF_IPI_CSELO As String
    Public NF_IPI_QSELO As Decimal
    Public NF_IPI_CENQ As String
    Public NF_IPI_CST As String
    Public NF_IPI_VBC As Decimal
    Public NF_IPI_PIPI As Decimal
    Public NF_IPI_VIPI As Decimal
    Public NF_IPI_QUNID As Decimal
    Public NF_IPI_VUNID As Decimal

    Public NF_II_VBC As Decimal
    Public NF_II_VDESPADU As Decimal
    Public NF_II_VII As Decimal
    Public NF_II_VIOF As Decimal

    Public NF_PIS_CST As String
    Public NF_PIS_VBC As Decimal
    Public NF_PIS_PPIS As Decimal
    Public NF_PIS_VPIS As Decimal
    Public NF_PIS_QBCPROD As Decimal
    Public NF_PIS_VALIQPROD As Decimal

    Public NF_PISST_VBC As Decimal
    Public NF_PISST_PPIS As Decimal
    Public NF_PISST_VPIS As Decimal
    Public NF_PISST_QBCPROD As Decimal
    Public NF_PISST_VALIQPROD As Decimal

    Public NF_COFINS_CST As String
    Public NF_COFINS_VBC As Decimal
    Public NF_COFINS_PCOFINS As Decimal
    Public NF_COFINS_VCOFINS As Decimal
    Public NF_COFINS_QBCPROD As Decimal
    Public NF_COFINS_VALIQPROD As Decimal

    Public NF_COFINSST_VBC As Decimal
    Public NF_COFINSST_PCOFINS As Decimal
    Public NF_COFINSST_VCOFINS As Decimal
    Public NF_COFINSST_QBCPROD As Decimal
    Public NF_COFINSST_VALIQPROD As Decimal

    Public NF_ISSQN_VBC As Decimal
    Public NF_ISSQN_VALIQ As Decimal
    Public NF_ISSQN_VISSQN As Decimal
    Public NF_ISSQN_CMUNFG As String
    Public NF_ISSQN_CLISTSERV As String
    Public NF_ISSQN_VDEDUCAO As Decimal
    Public NF_ISSQN_VOUTRO As Decimal
    Public NF_ISSQN_VDESCINCOND As Decimal
    Public NF_ISSQN_VDESCCOND As Decimal
    Public NF_ISSQN_VISSRET As Decimal
    Public NF_ISSQN_INDISS As Integer
    Public NF_ISSQN_CSERVICO As String
    Public NF_ISSQN_CMUN As String
    Public NF_ISSQN_CPAIS As String
    Public NF_ISSQN_NPROCESSO As String
    Public NF_ISSQN_INDINCENTIVO As Integer

    Public NF_ICMSUFDEST_VBCUFDEST As Decimal
    Public NF_ICMSUFDEST_PFCPUDEST As Decimal
    Public NF_ICMSUFDEST_PICMSUFDEST As Decimal
    Public NF_ICMSUFDEST_PICMSINTER As Decimal
    Public NF_ICMSUFDEST_PICMSINTERPART As Decimal
    Public NF_ICMSUFDEST_VFCPUFDEST As Decimal
    Public NF_ICMSUFDEST_VICMSUFDEST As Decimal
    Public NF_ICMSUFDEST_VICMSUFREMET As Decimal

    Public SAP_PO_NUMBER As String
    Public SAP_ITEM_DETAILS As SAP_RFC.PuchaseOrderItems

    Public VNF_FATOR As Decimal
    Public VNF_CODJUN As Decimal
    Public VNF_ITEM_VALIDO As String
    Public VNF_CONFIRMADO_PORTAL As Boolean
    Public VNF_ID_MODO_PROCESSO As Integer
    Public VNF_INBOUND_DELIVERY_NUMBER As String
    Public VNF_INBOUND_DELIVERY_ITEM_NUMBER As Decimal
    Public VNF_INBOUND As List(Of modInboundDelivery)
    Public VNF_NFREF_CONSIGNACAO As List(Of modTBDOC_CONSIGNACAO_REFNF)
    Public VNF_NFREF_SUBCONTRATACAO As List(Of modTBDOC_SUBCONTRATACAO_REFNF)
    Public VNF_NFREF_NOTA_COMPLEMENTAR As List(Of modTBDOC_NOTA_COMPLEMENTAR_REFNF)
    Public VNF_IS_SUBCONTRATACAO As Boolean = False

    Public MDP_MODO As String
    Public MDP_PROCESSO As String
    Public MDP_TIPO_MOVIMENTO_MIGO As String
    Public MDP_AGUARDAR_LIBERACAO_MIGO As Boolean
    Public MDP_CRIAR_MIRO As Boolean
    Public MDP_TIPO_MIRO As String
    Public MDP_DEBITO_POSTERIOR As Boolean
    Public MDP_TIPO_NF As String
    Public MDP_ENVIAR_TAXCODE_MIGO As Boolean


End Class