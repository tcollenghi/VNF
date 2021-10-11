Public Class modInboundDelivery

    Public SAP_INBOUND_DELIVERY_NUMBER As String
    Public SAP_INBOUND_DELIVERY_ITEM_NUMBER As Decimal
    Public SAP_DELIVERY_DATE As DateTime
    Public SAP_QTY As Decimal
    Public NOTAS_FISCAIS As List(Of modInboundDeliveryNFs)
    Public OPEN_QTY As Decimal

End Class

Public Class modInboundDeliveryNFs

    Public VNF_NFEID As String
    Public VNF_NUMERO As String
    Public VNF_SERIE As String
    Public VNF_ITEM As Integer
    Public VNF_EMISSAO As DateTime
    Public VNF_CFOP As String
    Public VNF_SITUACAO As String
    Public VNF_QTY As Decimal

End Class