Public Class BLPriorizacao

    Public Function GetByFilter(ByVal ItensRecebidos As String, ByVal ItensVencidos As String, ByVal NumeroPedido As String) As DataTable
        Dim commandSelect As String
        commandSelect = "SELECT id_priorizacao_item_pedido," &
                          " pid_pedido," &
                          " pid_item," &
                          " convert(varchar(10), pid_data_insercao, 103) as pid_data_insercao," &
                          " convert(varchar(10), pid_data_limite, 103) as pid_data_limite," &
                          " case when pid_recebido = 1 then 'SIM' else 'NÃO' end as pid_recebido," &
                          " convert(varchar(10), pid_recebido_em, 103) as pid_recebido_em" &
                        " FROM" &
                          " pid_priorizacao_item_pedido"

        Dim commandWhere As String = ""

        'Itens que já foram recebidos
        If Not String.IsNullOrEmpty(ItensRecebidos) Then
            If ItensRecebidos = "SIM" Then
                commandWhere &= " WHERE pid_recebido = 1 "
            ElseIf ItensRecebidos = "NÃO" Then
                commandWhere &= " WHERE (pid_recebido is null OR pid_recebido = 0) "
            Else
                commandWhere &= " WHERE (pid_recebido is null OR pid_recebido = 0 OR pid_recebido = 1) "
            End If
        End If

        'Itens que já estão vencidos
        If Not String.IsNullOrEmpty(ItensRecebidos) Then
            If ItensRecebidos = "SIM" Then
                commandWhere &= " AND pid_data_limite < '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' "
            ElseIf ItensRecebidos = "NÃO" Then
                commandWhere &= " AND pid_data_limite >= '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' "
            End If
        End If

        'Pedido
        If Not String.IsNullOrEmpty(NumeroPedido) Then
            commandWhere &= " AND pid_pedido like '%" & NumeroPedido & "%'"
        End If

        commandSelect &= commandWhere

        Return modSQL.Fill(commandSelect)
    End Function

    Public Function Adicionar(ByVal Pedido As String, ByVal Item As String, ByVal Dias As Integer, ByVal Usuario As String) As Integer

        Dim strQuery As String
        Dim dataLimite As DateTime
        dataLimite = DateTime.Now.AddDays(Dias)

        strQuery = " INSERT INTO pid_priorizacao_item_pedido (pid_pedido, pid_item, pid_data_limite, pid_data_insercao, pid_usuario_insercao)" &
                   " VALUES('" & Pedido & "', '" & Item & "', '" & dataLimite.ToString("yyyy-MM-dd") & "', '" & DateTime.Now.ToString("yyyy-MM-dd") & "', '" & Usuario & "');" &
                   " SELECT @@identity "

        Return Convert.ToInt32(modSQL.ExecuteScalar(strQuery))
    End Function

End Class
