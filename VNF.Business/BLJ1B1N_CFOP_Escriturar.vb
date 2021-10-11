Public Class BLJ1B1N_CFOP_Escriturar

    Public Function GetAll() As DataTable
        Return modSQL.Fill("select * from TBJ1B1N_DE_PARA_CFOP_ESCRITURAR order by CFOP_XML")
    End Function

    Public Function Adicionar(ByVal pCFOP As String, ByVal pCFOPEscriturar As String)

        Dim strQuery As String

        strQuery = " INSERT INTO TBJ1B1N_DE_PARA_CFOP_ESCRITURAR (CFOP_XML, CFOP_ESCRITURAR)" &
                   " VALUES('" & pCFOP & "', '" & pCFOPEscriturar & "');"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Editar(ByVal pCFOP As String, ByVal pCFOPEscriturar As String)

        Dim strQuery As String

        strQuery = " UPDATE TBJ1B1N_DE_PARA_CFOP_ESCRITURAR SET CFOP_ESCRITURAR = '" & pCFOPEscriturar & "' WHERE CFOP_XML = '" & pCFOP & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Excluir(ByVal pCFOP As String)

        Dim strQuery As String

        strQuery = " DELETE FROM TBJ1B1N_DE_PARA_CFOP_ESCRITURAR WHERE CFOP_XML = '" & pCFOP & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

End Class
