Imports System.Data.SqlClient

Public MustInherit Class modSQLFretes

    Public Shared connectionString As String = "Data Source=SORSSQLDB02.am1.mnet;Initial Catalog=DTB_FRETES;User Id=FRETES;Password=FRETES;Connect Timeout=0;Pooling=false;"
    Public Shared CommandText As String = ""

    Public Shared Sub ExecuteNonQuery(ByVal cmdText As String)
        Try
            Dim SqlConnection As New SqlConnection(connectionString)
            SqlConnection.Open()

            Dim SqlCommand As New SqlCommand(cmdText, SqlConnection)
            SqlCommand.CommandTimeout = 99999
            SqlCommand.ExecuteNonQuery()

            SqlConnection.Close()
        Catch ex As Exception
            Dim SqlConnection As New SqlConnection(connectionString)
            SqlConnection.Open()
            Dim SqlCommand As New SqlCommand("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')", SqlConnection)
            SqlCommand.CommandTimeout = 99999
            SqlCommand.ExecuteNonQuery()
            SqlConnection.Close()
            Throw ex
        End Try
    End Sub

    Public Shared Sub ExecuteNonQuery(ByVal cmdText As String, ByVal connString As String)
        Try
            Dim SqlConnection As New SqlConnection(connString)
            SqlConnection.Open()

            Dim SqlCommand As New SqlCommand(cmdText, SqlConnection)
            SqlCommand.CommandTimeout = 99999
            SqlCommand.ExecuteNonQuery()

            SqlConnection.Close()
        Catch ex As Exception
            Dim SqlConnection As New SqlConnection(connectionString)
            SqlConnection.Open()
            Dim SqlCommand As New SqlCommand("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')", SqlConnection)
            SqlCommand.CommandTimeout = 99999
            SqlCommand.ExecuteNonQuery()
            SqlConnection.Close()
            Throw ex
        End Try
    End Sub


    Public Shared Function Fill(ByVal cmdText As String) As DataTable
        Try
            Dim DataTable As New DataTable()
            Dim SqlDataAdapter As New SqlDataAdapter(cmdText, connectionString)
            SqlDataAdapter.SelectCommand.CommandTimeout = 99999

            SqlDataAdapter.Fill(DataTable)
            Return DataTable
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        End Try
    End Function

    Public Shared Function Fill(ByVal cmdText As String, ByVal connString As String) As DataTable
        Try
            Dim DataTable As New DataTable()
            Dim SqlDataAdapter As New SqlDataAdapter(cmdText, connString)
            SqlDataAdapter.SelectCommand.CommandTimeout = 99999

            SqlDataAdapter.Fill(DataTable)
            Return DataTable
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        End Try
    End Function

    Public Shared Function ExecuteReader(ByVal cmdText As String) As SqlDataReader
        Try
            Dim SqlConnection As New SqlConnection(connectionString)
            SqlConnection.Open()

            Dim SqlDataReader As SqlDataReader = New SqlCommand(cmdText, SqlConnection).ExecuteReader
            SqlConnection.Close()
            Return SqlDataReader
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        End Try
    End Function

    Public Shared Function ExecuteScalar(ByVal cmdText As String) As String
        Try
            Dim SqlConnection As New SqlConnection(connectionString)
            SqlConnection.Open()

            Dim Value As Object = New SqlCommand(cmdText, SqlConnection).ExecuteScalar()

            SqlConnection.Close()

            If (Value Is DBNull.Value) Then
                Value = ""
            End If

            Return Value
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        End Try
    End Function

End Class
