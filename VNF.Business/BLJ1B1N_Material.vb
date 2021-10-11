Public Class BLJ1B1N_Material

    Public Function GetAll() As DataTable
        Return modSQL.Fill("select * from TBJ1B1N_COD_MATERIAL_DESCRICAO order by ID_ITEM")
    End Function

    Public Function Adicionar(ByVal pValor As String)

        Dim strQuery As String

        strQuery = " INSERT INTO TBJ1B1N_COD_MATERIAL_DESCRICAO (COD_MATERIAL)" &
                   " VALUES('" & pValor & "');"

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Editar(ByVal pID As Integer, ByVal pValor As String)

        Dim strQuery As String

        strQuery = " UPDATE TBJ1B1N_COD_MATERIAL_DESCRICAO SET COD_MATERIAL='" & pValor & "' WHERE ID_ITEM=" & pID

        Return modSQL.ExecuteScalar(strQuery)
    End Function

    Public Function Excluir(ByVal pID As Integer)

        Dim strQuery As String

        strQuery = " DELETE FROM TBJ1B1N_COD_MATERIAL_DESCRICAO WHERE ID_ITEM = " & pID

        Return modSQL.ExecuteScalar(strQuery)
    End Function


End Class
