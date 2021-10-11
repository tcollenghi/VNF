'Autor: Marcio Spinosa - CR00008351
'Data : 28/05/2018
' OBS : Ajuste na tela de parâmetros para inclusão, alteração e exclusão
'==========================================================================
Public Class BLParametros

    'Marcio Spinosa - 21/02/2019 - CR00009165
    Public Function GetAll(ByVal pType As String) As DataTable

        If (String.IsNullOrEmpty(pType)) Then
            Return modSQL.Fill("select * from TbPAR order by PARAMETRO")
        Else
            Return modSQL.Fill("select * from TbPAR where parametro= 'NF_TYPES' order by PARAMETRO")
        End If
        'Marcio Spinosa - 21/02/2019 - CR00009165 - Fim
    End Function

    ''' <summary>
    ''' Adiciona um novo parametro
    ''' </summary>
    ''' <param name="pParametro"></param>
    ''' <param name="pValor"></param>
    ''' <param name="pDescricao"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>Marcio Spinosa - 28/05/2018 - CR00008351 - Ajuste na tela de parâmetros para inclusão, alteração e exclusão</example>
    Public Function Adicionar(ByVal pParametro As String, ByVal pValor As String, ByVal pDescricao As String)

        Dim strQuery As String
        ' Marcio Spinosa - 28/05/2018 - CR00008351
        strQuery = " INSERT INTO TbPAR (PARAMETRO, VALOR, DESCRICAO)" &
                   " VALUES('" & pParametro & "', '" & pValor & "', '" & pDescricao & "');"
        ' Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
        Return modSQL.ExecuteScalar(strQuery)
    End Function
    ''' <summary>
    ''' Metdod para edição do parâmetro
    ''' </summary>
    ''' <param name="pParametro"></param>
    ''' <param name="pValorAtual"></param>
    ''' <param name="pValorNovo"></param>
    ''' <param name="pDescricao"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>Marcio Spinosa - 28/05/2018 - CR00008351 - Ajuste na tela de parâmetros para inclusão, alteração e exclusão</example>
    Public Function Editar(ByVal pParametro As String, ByVal pValorAtual As String, ByVal pValorNovo As String, ByVal pDescricao As String)

        Dim strQuery As String
        ' Marcio Spinosa - 28/05/2018 - CR00008351
        strQuery = " UPDATE TbPAR SET VALOR='" & pValorNovo & "', DESCRICAO = '" & pDescricao & "' WHERE PARAMETRO='" & pParametro & "' and VALOR='" & pValorAtual & "'"
        ' Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Excluir(ByVal pParametro As String, ByVal pValor As String)

        Dim strQuery As String

        strQuery = " DELETE FROM TbPAR WHERE PARAMETRO='" & pParametro & "' AND VALOR='" & pValor & "'"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function GetByParametro(ByVal pstrParametro As String) As String
        Return modSQL.ExecuteScalar("select valor from TbPAR where parametro= '" & pstrParametro & "' order by PARAMETRO").ToString()
    End Function

End Class
