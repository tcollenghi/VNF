Imports MetsoFramework.SAP

Public Class modNF

    ' View Model
    Public IGNORE_EMAIL As Boolean

    Public Sub New()
        SAP_DETAILS = New SAP_RFC.PurchaseOrder()
        ITENS_NF = New List(Of modNFItem)()
        DUPLICATAS = New List(Of modNFDuplicata)()
        NF_REFERENCIADAS = New List(Of modNFReferenciada)()

        IGNORE_EMAIL = False
    End Sub

    Public Enum TipoData
        Emissao = 0
        Chegada = 1
        EnvioIP = 2
        Integracao = 3
    End Enum

    Public Const tipo_doc_cte = "CTE"
    Public Const tipo_doc_nfe = "NFE"
    Public Const tipo_doc_talonario = "TAL"
    Public Const tipo_cte_frete_pedido = "FRETE PEDIDO"
    Public Const tipo_cte_debito_posterior = "DÉBITO POSTERIOR"
    Public Const tipo_cte_exportacao = "EXPORTAÇÃO"
    Public Const tipo_cte_importacao = "IMPORTAÇÃO"
    Public Const tipo_cte_nao_definido = "NÃO DEFINIDO"
    Public Const tipo_tal_compra = "COMPRA"
    Public Const tipo_tal_remessa = "REMESSA"
    Public Const tipo_nfse = "NFSE"
    Public Const tipo_NFS = "NFS" 'nota fiscal
    Public Const tipo_FAT = "FAT" 'fatura
    Public Const tipo_TLC = "TLC" 'Telecom

    Public Structure StatusIP
        Public SITUACAO As String
        Public ACAO As String
        Public ENVIAR_IP As Boolean
    End Structure

    Public NF_IDE_CUF As Integer
    Public NF_IDE_CNF As Integer
    Public NF_IDE_INDPAG As Integer
    Public NF_IDE_NATOP As String
    Public NF_IDE_MOD As Integer
    Public NF_IDE_SERIE As String
    Public NF_IDE_NNF As String
    Public NF_IDE_DHEMI As DateTime
    Public NF_IDE_TPNF As Integer
    Public NF_IDE_IDDEST As Integer
    Public NF_IDE_CMUNFG As String
    Public NF_IDE_TPEMISS As Integer
    Public NF_IDE_TPAMB As Integer
    Public NF_IDE_FINNFE As Integer
    Public NF_IDE_INDFINAL As Integer
    Public NF_IDE_INDPRES As Integer
    Public NF_IDE_PROCEMI As Integer
    Public NF_IDE_DHCONT As String
    Public NF_IDE_XJUST As String
    Public NF_IDE_NFREF As String
    Public NF_IDE_MODAL As String

    Public NF_EMIT_CNPJ As String
    Public NF_EMIT_IE As String
    Public NF_EMIT_XNOME As String
    Public NF_EMIT_XLGR As String
    Public NF_EMIT_NRO As String
    Public NF_EMIT_XCPL As String
    Public NF_EMIT_XBAIRRO As String
    Public NF_EMIT_CMUN As String
    Public NF_EMIT_UF As String
    Public NF_EMIT_CEP As String
    Public NF_EMIT_CPAIS As String
    Public NF_EMIT_XPAIS As String
    Public NF_EMIT_FONE As String
    Public NF_EMIT_IEST As String
    Public NF_EMIT_IM As String
    Public NF_EMIT_CNAE As String
    Public NF_EMIT_CRT As String

    Public NF_REM_XNOME As String
    Public NF_REM_CNPJ As String
    Public NF_REM_IE As String
    Public NF_REM_XLGR As String
    Public NF_REM_NRO As String
    Public NF_REM_XCPL As String
    Public NF_REM_XBAIRRO As String
    Public NF_REM_CMUN As String
    Public NF_REM_UF As String
    Public NF_REM_CEP As String
    Public NF_REM_CPAIS As String
    Public NF_REM_XPAIS As String

    Public NF_DEST_XNOME As String
    Public NF_DEST_CNPJ As String
    Public NF_DEST_XLGR As String
    Public NF_DEST_NRO As String
    Public NF_DEST_XCPL As String
    Public NF_DEST_XBAIRRO As String
    Public NF_DEST_CMUN As String
    Public NF_DEST_XMUN As String
    Public NF_DEST_UF As String
    Public NF_DEST_CEP As String
    Public NF_DEST_CPAIS As String
    Public NF_DEST_XPAIS As String
    Public NF_DEST_FONE As String
    Public NF_DEST_INDIEDEST As String
    Public NF_DEST_IE As String
    Public NF_DEST_ISUF As String
    Public NF_DEST_IM As String

    Public NF_EXPED_XNOME As String
    Public NF_EXPED_CNPJ As String
    Public NF_EXPED_XLGR As String
    Public NF_EXPED_NRO As String
    Public NF_EXPED_XCPL As String
    Public NF_EXPED_XBAIRRO As String
    Public NF_EXPED_CMUN As String
    Public NF_EXPED_XMUN As String
    Public NF_EXPED_UF As String
    Public NF_EXPED_CEP As String
    Public NF_EXPED_CPAIS As String
    Public NF_EXPED_XPAIS As String
    Public NF_EXPED_FONE As String
    Public NF_EXPED_INDIEDEST As String
    Public NF_EXPED_IE As String
    Public NF_EXPED_ISUF As String
    Public NF_EXPED_IM As String

    Public NF_RECEB_XNOME As String
    Public NF_RECEB_CNPJ As String
    Public NF_RECEB_XLGR As String
    Public NF_RECEB_NRO As String
    Public NF_RECEB_XCPL As String
    Public NF_RECEB_XBAIRRO As String
    Public NF_RECEB_CMUN As String
    Public NF_RECEB_XMUN As String
    Public NF_RECEB_UF As String
    Public NF_RECEB_CEP As String
    Public NF_RECEB_CPAIS As String
    Public NF_RECEB_XPAIS As String
    Public NF_RECEB_FONE As String
    Public NF_RECEB_INDIEDEST As String
    Public NF_RECEB_IE As String
    Public NF_RECEB_ISUF As String
    Public NF_RECEB_IM As String

    Public NF_ICMSTOT_VBC As Decimal
    Public NF_ICMSTOT_VICMS As Decimal
    Public NF_ICMSTOT_VBCST As Decimal
    Public NF_ICMSTOT_VST As Decimal
    Public NF_ICMSTOT_VPROD As Decimal
    Public NF_ICMSTOT_VFRETE As Decimal
    Public NF_ICMSTOT_VSEG As Decimal
    Public NF_ICMSTOT_VDESC As Decimal
    Public NF_ICMSTOT_VII As Decimal
    Public NF_ICMSTOT_VIPI As Decimal
    Public NF_ICMSTOT_VPIS As Decimal
    Public NF_ICMSTOT_VCOFINS As Decimal
    Public NF_ICMSTOT_VOUTRO As Decimal
    Public NF_ICMSTOT_VNF As Decimal
    Public NF_ICMSTOT_VTOTTRIB As Decimal
    Public NF_ICMSTOT_VICMSDESON As Decimal
    Public NF_ICMSTOT_VICMSUFDEST As Decimal
    Public NF_ICMSTOT_VICMSUFREMET As Decimal
    Public NF_ICMSTOT_VFCPUFDEST As Decimal

    Public NF_ISSQNTOT_VSERV As Decimal
    Public NF_ISSQNTOT_VBC As Decimal
    Public NF_ISSQNTOT_VISS As Decimal
    Public NF_ISSQNTOT_VPIS As Decimal
    Public NF_ISSQNTOT_VCOFINS As Decimal
    Public NF_ISSQNTOT_DTCOMPET As String
    Public NF_ISSQNTOT_VDEDUCAO As Decimal
    Public NF_ISSQNTOT_VOUTRO As Decimal
    Public NF_ISSQNTOT_VDESCINCOD As Decimal
    Public NF_ISSQNTOT_VDESCCOND As Decimal
    Public NF_ISSQNTOT_VISSRET As Decimal
    Public NF_ISSQNTOT_CREGTRIB As String

    Public NF_RETTRIN_VRETPIS As Decimal
    Public NF_RETTRIN_VRETCOFINS As Decimal
    Public NF_RETTRIN_VRETCSLL As Decimal
    Public NF_RETTRIN_VBCIRRF As Decimal
    Public NF_RETTRIN_VIRRF As Decimal
    Public NF_RETTRIN_VBCRETPREV As Decimal
    Public NF_RETTRIN_VRETPREV As Decimal

    Public NF_RETTRANSP_VSERV As Decimal
    Public NF_RETTRANSP_VBCRET As Decimal
    Public NF_RETTRANSP_PICMSRET As Decimal
    Public NF_RETTRANSP_VICMSRET As Decimal
    Public NF_RETTRANSP_CFOP As Integer
    Public NF_RETTRANSP_CMUNFG As String

    Public NF_COBR_NFAT As String
    Public NF_COBR_VORIG As Decimal
    Public NF_COBR_VDESC As Decimal
    Public NF_COBR_VLIQ As Decimal

    Public NF_PAG_TPAG As String
    Public NF_PAG_VPAG As Decimal
    Public NF_PAG_CNPJ As String
    Public NF_PAG_TBAND As String
    Public NF_PAG_CAUT As String
    Public NF_PAG_TPINTEGRA As String

    Public NF_OUTROS_SIGNATURE As Boolean
    Public NF_OUTROS_INFORMACAO_ADICIONAL As String
    Public NF_OUTROS_VERSAO As String
    Public NF_OUTROS_STATUS_CODE As String
    Public NF_OUTROS_STATUS_DESC As String

    Public NF_TRANSP_MODFRETE As String
    Public NF_TRANSP_XNOME As String
    Public NF_TRANSP_CNPJ As String
    Public NF_TRANSP_IE As String
    Public NF_TRANSP_XENDER As String
    Public NF_TRANSP_XMUN As String
    Public NF_TRANSP_UF As String

    Public CT_IDE_CMUNINI As String
    Public CT_IDE_XMUNINI As String
    Public CT_IDE_UFINI As String
    Public CT_IDE_CMUNFIM As String
    Public CT_IDE_XMUNFIM As String
    Public CT_IDE_UFFIM As String
    Public CT_IDE_RETIRA As String
    Public CT_IDE_TOMA As String
    Public CT_IDE_TOMA_DESC As String
    Public CT_IDE_TPCTE As Integer

    Public CT_TOMA_CNPJ As String
    Public CT_TOMA_IE As String
    Public CT_TOMA_XNOME As String
    Public CT_TOMA_XLGR As String
    Public CT_TOMA_NRO As String
    Public CT_TOMA_XBAIRRO As String
    Public CT_TOMA_CMUN As String
    Public CT_TOMA_XMUN As String
    Public CT_TOMA_CEP As String
    Public CT_TOMA_UF As String
    Public CT_TOMA_CPAIS As String
    Public CT_TOMA_XPAIS As String

    Public CT_VPREST_VTPREST As Decimal
    Public CT_VPREST_VREC As Decimal
    Public CT_INFCARGA_VCARGA As Decimal

    Public CT_VPREST_COMP_FRETE_PESO As Decimal
    Public CT_VPREST_COMP_FRETE_VALOR As Decimal
    Public CT_VPREST_COMP_SEC_CAT As Decimal
    Public CT_VPREST_COMP_ADEME As Decimal
    Public CT_VPREST_COMP_PEDAGIO As Decimal
    Public CT_VPREST_COMP_GRIS As Decimal
    Public CT_VPREST_COMP_TAXAEMICTRC As Decimal
    Public CT_VPREST_COMP_COLETAENTREGA As Decimal
    Public CT_VPREST_COMP_OUTROSVALORES As Decimal
    Public CT_VPREST_COMP_FRETE As Decimal
    Public CT_VPREST_COMP_DESCONTO As Decimal
    Public CT_VPREST_COMP_DESPACHO As Decimal
    Public CT_VPREST_COMP_ENTREGA As Decimal
    Public CT_VPREST_COMP_OUTROS As Decimal
    Public CT_VPREST_COMP_ESCOLTA As Decimal
    Public CT_VPREST_COMP_COLETA As Decimal
    Public CT_VPREST_COMP_SEGURO As Decimal
    Public CT_VPREST_COMP_PERNOITE As Decimal
    Public CT_VPREST_COMP_REDESPACHO As Decimal

    Public VNF_CONTEUDO_XML As String
    Public VNF_DANFE_ONLINE As String
    Public VNF_TIPO_DOCUMENTO As String
    Public VNF_CHAVE_ACESSO As String
    Public VNF_STATUS As String
    Public VNF_STATUS_IP As StatusIP
    Public VNF_ANEXO As Byte()
    Public VNF_ANEXO_NOME As String
    Public VNF_ANEXO_EXTENSAO As String
    Public VNF_CODIGO_VERIFICACAO As String
    Public VNF_MATERIAL_RECEBIDO As Boolean
    Public VNF_ID_MODO_PROCESSO As Integer
    Public VNF_CLASSIFICACAO As String
    Public VNF_RATEAR As Boolean
    Public VNF_NOTA_MANUAL_J1B1N As Boolean = False
    Public VNF_J1B1N_NFTYPE As String

    Public NF_NFREF_REFNNF As String
    Public NF_NFREF_REFSerie As String
    Public NF_NFREF_REFDHEMI As DateTime

    Public NF_STATUS_TRIANGULUS As String 'Marcio Spinosa - 29/11/2018

    Public SAP_DETAILS As SAP_RFC.PurchaseOrder
    Public ITENS_NF As List(Of modNFItem)
    Public DUPLICATAS As List(Of modNFDuplicata)
    Public NF_REFERENCIADAS As List(Of modNFReferenciada)
    Public SAP_PO_HEADER_COLLECTION As New List(Of SAP_RFC.PurchaseOrder)



End Class