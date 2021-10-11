Public Class BLJ1B1N_Direito_FIscal

    Public Function GetAll() As DataTable
        Return modSQL.Fill("select * from TBJ1B1N_DIREITO_FISCAL order by TIPO_IMPOSTO")
    End Function

    Public Function Adicionar(ByVal pTipoImposto As String, ByVal pCFOP As String, ByVal pUF As String, ByVal pDireito As String, ByVal pValor As String)

        Dim strQuery As String

        strQuery = " INSERT INTO TBJ1B1N_DIREITO_FISCAL (TIPO_IMPOSTO, CFOP, UF_DEST, DIREITO_FISCAL, VALOR_PADRAO)" &
                   " VALUES('" & pTipoImposto & "', '" & pCFOP & "', '" & pUF & "', '" & pDireito & "', '" & pValor & "');"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Editar(ByVal pTipoImposto As String, ByVal pCFOP As String, ByVal pUF As String, ByVal pDireito As String, ByVal pValor As String, ByVal pDireitoAntigo As String)

        Dim strQuery As String
        'Marcio Spinosa - 21/02/2019 - CR00009165
        strQuery = " UPDATE TBJ1B1N_DIREITO_FISCAL SET VALOR_PADRAO = '" & pValor & "', DIREITO_FISCAL = '" & pDireito & "'  WHERE TIPO_IMPOSTO = '" & pTipoImposto & "' AND UF_DEST = '" & pUF & "' AND CFOP = '" & pCFOP & "' AND DIREITO_FISCAL = '" & pDireitoAntigo & "'"
        'Marcio Spinosa - 21/02/2019 - CR00009165 - Fim
        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Excluir(ByVal pTipoImposto As String, ByVal pUF As String, ByVal pCFOP As String)

        Dim strQuery As String

        strQuery = " DELETE FROM TBJ1B1N_DIREITO_FISCAL WHERE TIPO_IMPOSTO = '" & pTipoImposto & "' AND UF_DEST = '" & pUF & "' AND CFOP = '" & pCFOP & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function
End Class
