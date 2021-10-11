Public Class BLCFOP_MIRO

    Public Function GetAll() As DataTable
        Return modSQL.Fill("select * from TBAJUSTE_CFOP_MIRO_ESCRITURAR order by CFOP_XML, CFOP_SAP")
    End Function

    Public Function Adicionar(ByVal pCFOP_XML As String, ByVal pCFOP_SAP As String, ByVal pCFOP_ESCRITURAR As String)

        Dim strQuery As String

        strQuery = " INSERT INTO TBAJUSTE_CFOP_MIRO_ESCRITURAR (CFOP_XML, CFOP_SAP, CFOP_ESCRITURAR)" &
                   " VALUES('" & pCFOP_XML & "', '" & pCFOP_SAP & "', '" & pCFOP_ESCRITURAR & "');"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Editar(ByVal pCFOP_XML As String, ByVal pCFOP_SAP As String, ByVal pCFOP_ESCRITURAR As String)

        Dim strQuery As String

        strQuery = " UPDATE TBAJUSTE_CFOP_MIRO_ESCRITURAR SET CFOP_ESCRITURAR = '" & pCFOP_ESCRITURAR & "' WHERE CFOP_XML = '" & pCFOP_XML & "'  AND CFOP_SAP = '" & pCFOP_SAP & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Excluir(ByVal pCFOP_XML As String, ByVal pCFOP_SAP As String, ByVal pCFOP_ESCRITURAR As String)

        Dim strQuery As String

        strQuery = " DELETE FROM TBAJUSTE_CFOP_MIRO_ESCRITURAR WHERE CFOP_XML = '" & pCFOP_XML & "' AND CFOP_SAP = '" & pCFOP_SAP & "' AND CFOP_ESCRITURAR = '" & pCFOP_ESCRITURAR & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function
End Class
