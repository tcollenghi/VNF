Public Class TriangulusImportacaoViewModel
    Public CHAVE_ACESSO As String
    Public XML As String
    Public ID_DOC As String
    Public STAT As String
    Public DT_ALT As DateTime
    Public MODO As ModoType
    Public IGNORE_EMAIL As Boolean

    Public Enum ModoType
        REPROCESSAMENTO = 0
        DOCUMENTO_FALTANTE_IMPORTACAO = 1
        IMPORTACAO = 2
        IGNORAR_IMPORTACAO = 3
    End Enum

    Public Function Clone() As TriangulusImportacaoViewModel
        Dim newObj = New TriangulusImportacaoViewModel()
        newObj.CHAVE_ACESSO = CHAVE_ACESSO
        newObj.XML = XML
        newObj.ID_DOC = ID_DOC
        newObj.STAT = STAT
        newObj.DT_ALT = DT_ALT
        newObj.MODO = MODO
        newObj.IGNORE_EMAIL = IGNORE_EMAIL

        Return newObj
    End Function

End Class
