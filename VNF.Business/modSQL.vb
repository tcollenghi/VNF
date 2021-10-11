Imports System.Configuration
Imports System.Data.SqlClient
Imports MetsoFramework.Utils

Public MustInherit Class modSQL

    Public Shared connectionString As String = System.Configuration.ConfigurationSettings.AppSettings("ConnectionString").ToString()
    Public Shared connectionStringTriangulus As String = System.Configuration.ConfigurationSettings.AppSettings("ConnectionStringTriangulus").ToString()
    Public Shared ConnectionStringTriangulusCancelamento As String = System.Configuration.ConfigurationSettings.AppSettings("ConnectionStringTriangulusCancelamento").ToString() 'Marcio Spinosa - 21/02/2019 - CR00009165
    Public Shared connectionStringVnfFornecedor As String = System.Configuration.ConfigurationSettings.AppSettings("ConnectionStringAlgar").ToString()
    Public Shared connectionStringFretes As String = System.Configuration.ConfigurationSettings.AppSettings("ConnectionStringFretes").ToString()

    Public Shared CommandText As String = ""


    Public Shared Function Fill(ByVal cmdText As String) As DataTable
        Dim SqlDataAdapter As SqlDataAdapter = Nothing
        Try
            Debug.WriteLine("executando instrução SQL: " & cmdText)
            Dim DataTable As New DataTable()
            SqlDataAdapter = New SqlDataAdapter(cmdText, connectionString)
            SqlDataAdapter.SelectCommand.CommandTimeout = 299999

            'Dim DataTable As New DataTable()
            'Dim dataSet As DataSet = New DataSet()
            'dataSet.EnforceConstraints = False
            'dataSet.Tables.Add(DataTable)

            'dataSet.Tables(0).BeginLoadData()
            'SqlDataAdapter.Fill(dataSet.Tables(0))
            'dataSet.Tables(0).EndLoadData()

            'Return dataSet.Tables(0)

            SqlDataAdapter.Fill(DataTable)
            Return DataTable
        Catch ex As Exception
            Dim errorMsg = ex.Message.Replace("'", "")
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & errorMsg & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        Finally
            Try
                SqlDataAdapter.Dispose()
            Catch
            End Try
        End Try
    End Function

    Public Shared Function Fill(ByVal cmdText As String, ByVal connString As String) As DataTable
        Dim SqlDataAdapter As SqlDataAdapter = Nothing
        Try
            Debug.WriteLine("executando instrução SQL (" & connString & "): " & cmdText)
            Dim DataTable As New DataTable()
            SqlDataAdapter = New SqlDataAdapter(cmdText, connString)
            SqlDataAdapter.SelectCommand.CommandTimeout = 99999

            SqlDataAdapter.Fill(DataTable)
            Return DataTable
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & ex.Message & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        Finally
            Try
                SqlDataAdapter.Dispose()
            Catch
            End Try
        End Try
    End Function


    Public Shared Function ExecuteReader(ByVal cmdText As String) As SqlDataReader
        Try
            Debug.WriteLine("executando instrução SQL: " & cmdText)
            Dim SqlConnection As New SqlConnection(connectionString)
            SqlConnection.Open()

            Dim SqlDataReader As SqlDataReader = New SqlCommand(cmdText, SqlConnection).ExecuteReader
            SqlConnection.Close()
            Return SqlDataReader
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & ex.Message & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        End Try
    End Function

    Public Shared Function ExecuteReader(ByVal cmdText As String, ByVal connString As String) As SqlDataReader
        Try
            Debug.WriteLine("executando instrução SQL (" & connString & "): " & cmdText)
            Dim SqlConnection As New SqlConnection(connString)
            SqlConnection.Open()

            Dim SqlDataReader As SqlDataReader = New SqlCommand(cmdText, SqlConnection).ExecuteReader
            ''SqlConnection.Close()
            Return SqlDataReader
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & ex.Message & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        End Try
    End Function

    Public Shared Function ExecuteScalarByte(ByVal cmdText As String) As Byte()
        Try
            Debug.WriteLine("executando instrução SQL: " & cmdText)
            Dim SqlConnection As New SqlConnection(connectionString)
            SqlConnection.Open()

            Dim Value As Object = New SqlCommand(cmdText, SqlConnection).ExecuteScalar()

            SqlConnection.Close()

            Return Value
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & ex.Message & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        End Try
    End Function


    Public Shared Function ExecuteScalar(ByVal cmdText As String) As String
        Try
            Debug.WriteLine("executando instrução SQL: " & cmdText)
            Dim SqlConnection As New SqlConnection(connectionString)
            SqlConnection.Open()

            Dim Value As Object = New SqlCommand(cmdText, SqlConnection).ExecuteScalar()

            SqlConnection.Close()

            If (Value Is DBNull.Value) Then
                Value = ""
            End If

            Return Value
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & ex.Message & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        End Try
    End Function

    Public Shared Function ExecuteScalarParams(ByVal cmdText As String, ByVal SQLParams As List(Of SqlParameter)) As String
        Try
            Debug.WriteLine("executando instrução SQL: " & cmdText)
            Dim SqlConnection As New SqlConnection(connectionString)
            SqlConnection.Open()
            Dim SqlCommand As New SqlCommand(cmdText, SqlConnection)
            For Each p In SQLParams
                SqlCommand.Parameters.Add(p)
            Next
            Dim Value As Object = SqlCommand.ExecuteScalar()

            SqlConnection.Close()

            If (Value Is DBNull.Value) Then
                Value = ""
            End If

            Return Value
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & ex.Message & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        End Try
    End Function

    Public Shared Function ExecuteScalar(ByVal cmdText As String, ByVal connString As String) As String
        Try
            Debug.WriteLine("executando instrução SQL (" & connString & "): " & cmdText)
            Dim SqlConnection As New SqlConnection(connString)
            SqlConnection.Open()

            Dim Value As Object = New SqlCommand(cmdText, SqlConnection).ExecuteScalar()

            SqlConnection.Close()

            If (Value Is DBNull.Value) Then
                Value = ""
            End If

            Return Value
        Catch ex As Exception
            modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & ex.Message & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Throw ex
        End Try
    End Function


    Public Shared Sub ExecuteNonQueryParams(ByVal cmdText As String, ByVal SQLParams As List(Of SqlParameter))
        Dim SqlConnection As SqlConnection = Nothing
        Try
            Debug.WriteLine("executando instrução SQL: " & cmdText)
            SqlConnection = New SqlConnection(connectionString)
            SqlConnection.Open()

            Dim SqlCommand As New SqlCommand(cmdText, SqlConnection)
            SqlCommand.CommandTimeout = 99999
            SqlCommand.Parameters.Clear()

            For Each p As SqlParameter In SQLParams
                SqlCommand.Parameters.Add(p)
            Next
            SqlCommand.ExecuteNonQuery()
            SqlConnection.Close()
        Catch ex As Exception
            SqlConnection = New SqlConnection(connectionString)
            SqlConnection.Open()
            Dim SqlCommand As New SqlCommand("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & ex.Message.ToString() & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')", SqlConnection)
            SqlCommand.CommandTimeout = 99999
            SqlCommand.ExecuteNonQuery()
            SqlConnection.Close()
            Throw ex
        Finally
            Try
                SqlConnection.Close()
            Catch
            End Try
        End Try
    End Sub

    Public Shared Function ExecuteNonQuery(ByVal cmdText As String) As Integer
        Dim SqlConnection As SqlConnection = Nothing
        Dim vIntRowsAffected As Integer = 0
        Try
            Debug.WriteLine("executando instrução SQL: " & cmdText)
            SqlConnection = New SqlConnection(connectionString)
            SqlConnection.Open()

            Dim SqlCommand As New SqlCommand(cmdText, SqlConnection)
            SqlCommand.CommandTimeout = 199999
            vIntRowsAffected = SqlCommand.ExecuteNonQuery()

            SqlConnection.Close()
            Return vIntRowsAffected
        Catch ex As Exception
            SqlConnection = New SqlConnection(connectionString)
            Try
                SqlConnection.Open()
                Dim SqlCommand As New SqlCommand("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & ex.Message.ToString() & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')", SqlConnection)
                SqlCommand.CommandTimeout = 199999
                SqlCommand.ExecuteNonQuery()
            Catch
            End Try
            SqlConnection.Close()
            Return vIntRowsAffected
            Throw ex
        Finally
            Try
                SqlConnection.Close()
            Catch
            End Try
        End Try
    End Function

    Public Shared Sub ExecuteNonQuery(ByVal cmdText As String, ByVal connString As String)
        Dim SqlConnection As SqlConnection = Nothing
        Try
            Debug.WriteLine("executando instrução SQL (" & connString & "): " & cmdText)
            SqlConnection = New SqlConnection(connString)
            SqlConnection.Open()

            Dim SqlCommand As New SqlCommand(cmdText, SqlConnection)
            SqlCommand.CommandTimeout = 99999
            SqlCommand.ExecuteNonQuery()

            SqlConnection.Close()
        Catch ex As Exception
            SqlConnection = New SqlConnection(connectionString)
            SqlConnection.Open()
            Dim SqlCommand As New SqlCommand("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SQL_EXCEPTION', '" & cmdText.Replace("'", "") & "', '" & ex.Message.ToString() & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')", SqlConnection)
            SqlCommand.CommandTimeout = 99999
            SqlCommand.ExecuteNonQuery()
            SqlConnection.Close()
            Throw ex
        Finally
            Try
                SqlConnection.Close()
            Catch
            End Try
        End Try
    End Sub


    Public Shared Function AddSqlParameter(ByVal name As String, ByVal type As SqlDbType, ByVal value As Object) As SqlParameter
        Dim objParameter As New SqlParameter(name, type)

        If (value Is Nothing) Then
            objParameter.Value = DBNull.Value
        ElseIf (type = SqlDbType.DateTime AndAlso value = New DateTime) Then
            objParameter.Value = DBNull.Value
        ElseIf (type = SqlDbType.DateTime AndAlso value <> New DateTime) Then
            objParameter.Value = Convert.ToDateTime(Convert.ToDateTime(value).ToString("dd/MM/yyyy HH:mm:ss"))
        Else
            If (type <> SqlDbType.VarBinary AndAlso (value Is Nothing OrElse String.IsNullOrEmpty(value.ToString()))) Then
                objParameter.Value = String.Empty
            Else
                objParameter.Value = value
            End If
        End If

        Return objParameter
    End Function
    ''' <summary>
    ''' Registrar os acessos de telas de cada usuário
    ''' </summary>
    ''' <param name="pacecodtel"></param>
    ''' <remarks></remarks>
    ''' <example>Marcio Spinosa - 28/05/2018 - CR00008351 - ajuste para não utilizar os dados do AD</example>
    Public Shared Sub RegistrarAcesso(ByVal pacecodtel As String)
        Dim SqlConnection As SqlConnection = Nothing
        Try
            SqlConnection = New SqlConnection(modSQL.connectionString)
            SqlConnection.Open()

            Dim cmdText As String = "select IsNull(count(*),0) from TbAcesso where acecodusu = '" & Uteis.LogonName() & "' and acecodtel = '" & pacecodtel & "'"

            Dim acesitace As String = "LIBERADO"

            If New SqlCommand(cmdText, SqlConnection).ExecuteScalar().ToString = "0" Then

                cmdText = "select IsNull(count(*),0) from TbUsuario where usucodusu = '" & Uteis.LogonName() & "'"

                If New SqlCommand(cmdText, SqlConnection).ExecuteScalar().ToString = "0" Then
                    'Marcio Spinosa - 28/05/2018 - CR00008351
                    '                    cmdText = "insert into TbUsuario values('" & Uteis.LogonName() & "', '" & Uteis.UserName() & "')"
                    cmdText = "insert into TbUsuario values('" & Uteis.LogonName() & "', '" & getUserNameByLogon(Uteis.LogonName()) & "')"
                    'Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
                    Call New SqlCommand(cmdText, SqlConnection).ExecuteNonQuery()
                End If

                'If (pacecodtel = "NOTF") And (pacecodtel = "FORN") And (pacecodtel = "ACES") Or (pacecodtel = "INTE") Or (pacecodtel = "ANFE") Or (pacecodtel = "DIVE") Or (pacecodtel = "ANU") Or (pacecodtel = "VNFE") Or (pacecodtel = "UPLO") Or (pacecodtel = "INDI") Or (pacecodtel = "PORT") Or (pacecodtel = "PORF") Or (pacecodtel = "COMP") Or (pacecodtel = "PARA") Or (pacecodtel = "RE01") Or (pacecodtel = "RANU") Or (pacecodtel = "CFRE") Or (pacecodtel = "PRIO") Then
                If Not (String.IsNullOrWhiteSpace(pacecodtel)) Then
                    acesitace = "BLOQUEADO"
                End If

                cmdText = "insert into TbAcesso (acecodtel, acecodusu, acedatace, acesitace) values('" &
                pacecodtel & "', '" & Uteis.LogonName() & "', '" & Format(Now, "yyyy-MM-dd HH:mm:ss") & "', '" & acesitace & "')"
                Call New SqlCommand(cmdText, SqlConnection).ExecuteNonQuery()
            Else
                cmdText = "update TbAcesso set acedatace = '" &
                Format(Now, "yyyy-MM-dd HH:mm:ss") & "' where acecodusu = '" &
                Uteis.LogonName() & "' and acecodtel = '" & pacecodtel & "'"
                Call New SqlCommand(cmdText, SqlConnection).ExecuteNonQuery()
            End If

            SqlConnection.Close()
        Catch ex As Exception
            Throw ex
        Finally
            Try
                SqlConnection.Close()
            Catch
            End Try
        End Try
    End Sub
    'Marcio Spinosa - 28/05/2018 - CR00008351
    ''' <summary>
    ''' Metodo que retorna o nome do usuário do banco
    ''' </summary>
    ''' <param name="pStrlogon"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>Marcio Spinosa - 28/05/2018 - CR00008351 - ajuste para não utilizar os dados do AD</example>
    Public Shared Function getUserNameByLogon(ByVal pStrlogon As String)
        Dim pstrUserName As String
        pstrUserName = modSQL.ExecuteScalar("SELECT usunomusu FROM TbUsuario where usucodusu = '" & Uteis.LogonName() & "'")
        Return pstrUserName
    End Function
    'Marcio Spinosa - 28/05/2018 - CR00008351 - Fim

End Class
