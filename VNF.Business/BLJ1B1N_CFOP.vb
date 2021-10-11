Public Class BLJ1B1N_CFOP

    Public Function GetAll() As DataTable
        Return modSQL.Fill("select * from TBJ1B1N_CADASTRO_CFOP order by CFOP")
    End Function

    Public Function Adicionar(ByVal pCFOP As String, ByVal pDescricao As String)

        Dim strQuery As String

        strQuery = " INSERT INTO TBJ1B1N_CADASTRO_CFOP (CFOP, DESCRICAO)" &
                   " VALUES('" & pCFOP & "', '" & pDescricao & "');"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Editar(ByVal pCFOP As String, ByVal pDescricao As String)

        Dim strQuery As String

        strQuery = " UPDATE TBJ1B1N_CADASTRO_CFOP SET DESCRICAO = '" & pDescricao & "' WHERE CFOP= '" & pCFOP & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Excluir(ByVal pCFOP As String)

        Dim strQuery As String

        strQuery = " DELETE FROM TBJ1B1N_CADASTRO_CFOP WHERE CFOP = '" & pCFOP & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function


End Class
