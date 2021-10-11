Public Class BLJ1B1N_Planta_Metso_Parceiro

    Public Function GetAll() As DataTable
        Return modSQL.Fill("select * from TBJ1B1N_PLANTA_METSO_PARCEIRO order by PLANTA")
    End Function

    Public Function Adicionar(ByVal pCNPJ As String, ByVal pPlanta As String)

        Dim strQuery As String

        strQuery = " INSERT INTO TBJ1B1N_PLANTA_METSO_PARCEIRO (CNPJ, PLANTA)" &
                   " VALUES('" & pCNPJ & "', '" & pPlanta & "');"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Editar(ByVal pCNPJ As String, ByVal pPlanta As String)

        Dim strQuery As String

        strQuery = " UPDATE TBJ1B1N_PLANTA_METSO_PARCEIRO SET PLANTA = '" & pPlanta & "' WHERE CNPJ = '" & pCNPJ & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Excluir(ByVal pCNPJ As String)

        Dim strQuery As String

        strQuery = " DELETE FROM TBJ1B1N_PLANTA_METSO_PARCEIRO WHERE CNPJ = '" & pCNPJ & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

End Class
