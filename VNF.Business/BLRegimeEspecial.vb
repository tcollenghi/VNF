Public Class BLRegimeEspecial

    Public Function GetAll() As DataTable
        Dim strQuery As String = "SELECT CNPJ, RAZFOR FROM TBFOR WHERE REGIME_ESPECIAL = 'S'"
        Return modSQL.Fill(strQuery)
    End Function

    Public Sub CadastrarFornecedor(ByVal Cnpj As String, ByVal Ncm As String())
        For Each item As String In Ncm
            If Not String.IsNullOrEmpty(item) Then
                modSQL.ExecuteNonQuery("INSERT INTO REGIME_ESPECIAL VALUES (" + Cnpj + ",'" + item + "')")
            End If
        Next
    End Sub

    Public Sub RemoverFornecedor(ByVal Cnpj As String)
        Dim strQuery As String = "DELETE FROM REGIME_ESPECIAL WHERE CNPJ = " & Cnpj
        modSQL.ExecuteNonQuery(strQuery)
    End Sub

End Class
