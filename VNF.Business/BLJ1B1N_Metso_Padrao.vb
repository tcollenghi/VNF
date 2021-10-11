Public Class BLJ1B1N_Metso_Padrao

    Public Function GetAll() As DataTable
        Return modSQL.Fill("select * from TBJ1B1N_DADOS_METSO_PADRAO order by PLANTA")
    End Function

    Public Function Adicionar(ByVal pCNPJ As String, ByVal pPlanta As String, ByVal idFornMetso As String)

        Dim strQuery As String

        strQuery = " INSERT INTO TBJ1B1N_DADOS_METSO_PADRAO (CNPJ, PLANTA, ID_FORN_METSO)" &
                   " VALUES('" & pCNPJ & "', '" & pPlanta & "', '" & idFornMetso & "');"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Editar(ByVal pCNPJ As String, ByVal pPlanta As String, ByVal idFornMetso As String)

        Dim strQuery As String

        strQuery = " UPDATE TBJ1B1N_DADOS_METSO_PADRAO SET PLANTA = '" & pPlanta & "', ID_FORN_METSO = '" & idFornMetso & "' WHERE CNPJ = '" & pCNPJ & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Excluir(ByVal pCNPJ As String)

        Dim strQuery As String

        strQuery = " DELETE FROM TBJ1B1N_DADOS_METSO_PADRAO WHERE CNPJ = '" & pCNPJ & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function
End Class
