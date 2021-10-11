Public Class BLB1J1N_CFOP_Parceiro

    Public Function GetAll() As DataTable
        Return modSQL.Fill("select * from TBJ1B1N_CFOP_PARCEIRO order by CFOP")
    End Function

    Public Function Adicionar(ByVal pCFOP As String, ByVal pTipoParceiro As String)

        Dim strQuery As String

        strQuery = " INSERT INTO TBJ1B1N_CFOP_PARCEIRO (CFOP, TIPO_PARCEIRO)" &
                   " VALUES('" & pCFOP & "', '" & pTipoParceiro & "');"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Editar(ByVal pCFOP As String, ByVal pTipoParceiro As String)

        Dim strQuery As String

        strQuery = " UPDATE TBJ1B1N_CFOP_PARCEIRO SET TIPO_PARCEIRO = '" & pTipoParceiro & "' WHERE CFOP = '" & pCFOP & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Excluir(ByVal pCFOP As String)

        Dim strQuery As String

        strQuery = " DELETE FROM TBJ1B1N_CFOP_PARCEIRO WHERE CFOP = '" & pCFOP & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

End Class
