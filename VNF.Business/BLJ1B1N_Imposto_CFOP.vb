Public Class BLJ1B1N_Imposto_CFOP
    Public Function GetAll() As DataTable
        Return modSQL.Fill("select * from TBJ1B1N_TIPO_IMPOSTO_POR_CFOP order by IMPOSTO")
    End Function

    Public Function Adicionar(ByVal pImposto As String, ByVal pCFOP As String, ByVal pTipo As String, ByVal pBase As String)

        Dim strQuery As String

        strQuery = " INSERT INTO TBJ1B1N_TIPO_IMPOSTO_POR_CFOP (IMPOSTO, CFOP, TIPO_IMPOSTO, BASE_ESCRITURAR)" &
                   " VALUES('" & pImposto & "', '" & pCFOP & "', '" & pTipo & "', '" & pBase & "');"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Editar(ByVal pImposto As String, ByVal pCFOP As String, ByVal pTipo As String, ByVal pBase As String)

        Dim strQuery As String

        strQuery = "UPDATE TBJ1B1N_TIPO_IMPOSTO_POR_CFOP SET BASE_ESCRITURAR = '" & pBase & "' WHERE IMPOSTO = '" & pImposto & "' AND CFOP = '" & pCFOP & "' AND TIPO_IMPOSTO = '" & pTipo & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Excluir(ByVal pImposto As String, ByVal pCFOP As String, ByVal pTipo As String)

        Dim strQuery As String

        strQuery = " DELETE FROM TBJ1B1N_TIPO_IMPOSTO_POR_CFOP WHERE IMPOSTO = '" & pImposto & "' AND CFOP = '" & pCFOP & "' AND TIPO_IMPOSTO = '" & pTipo & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

End Class
