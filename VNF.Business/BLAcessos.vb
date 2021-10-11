Public Class BLAcessos

    'Public Function Consultar(ByVal CodigoTela As String) As DataSet
    '    Dim dttBloqueado As New DataTable("dttBloqueado")
    '    Dim dttLiberado As New DataTable("dttLiberado")
    '    Dim dtsAcessos As New DataSet()

    '    Dim cmdText As String = "select " & _
    '                            "  acecodtel, usucodusu, usunomusu, acesitace " & _
    '                            "from " & _
    '                            "  TbAcesso, TbUsuario " & _
    '                            "where " & _
    '                            "  acecodusu = usucodusu and acesitace = 'BLOQUEADO' and " & _
    '                            "  acecodtel = '" & CodigoTela & "' " & _
    '                            "order by " & _
    '                            "  usunomusu"

    '    dttBloqueado = modSQL.Fill(cmdText)
    '    dttBloqueado.TableName = "dttBloqueado"

    '    cmdText = "select " & _
    '              "  acecodtel, usucodusu, usunomusu, acesitace, acedatace " & _
    '              "from " & _
    '              "  TbAcesso, TbUsuario " & _
    '              "where " & _
    '              "  acecodusu = usucodusu and acesitace = 'LIBERADO' and " & _
    '              "  acecodtel = '" & CodigoTela & "' " & _
    '              "order by " & _
    '              "  acedatace desc, usunomusu"

    '    dttLiberado = modSQL.Fill(cmdText)
    '    dttLiberado.TableName = "dttLiberado"

    '    dtsAcessos.Tables.Add(dttBloqueado)
    '    dtsAcessos.Tables.Add(dttLiberado)

    '    Return dtsAcessos
    'End Function

    Public Function ConsultarBloqueados(ByVal CodigoTela As String) As DataSet
        Dim dttBloqueado As New DataTable("dttBloqueado")
        Dim dtsAcessos As New DataSet()

        Dim cmdText As String = "select " & _
                                "  acecodtel, usucodusu, usunomusu, acesitace " & _
                                "from " & _
                                "  TbAcesso, TbUsuario " & _
                                "where " & _
                                "  acecodusu = usucodusu and acesitace = 'BLOQUEADO' and " & _
                                "  acecodtel = '" & CodigoTela & "' " & _
                                "order by " & _
                                "  usunomusu"

        dttBloqueado = modSQL.Fill(cmdText)
        dttBloqueado.TableName = "dttBloqueado"

        dtsAcessos.Tables.Add(dttBloqueado)

        Return dtsAcessos
    End Function
    Public Function ConsultarUsuarioBloqueados(ByVal pstrUser As String, ByVal ptxtCodigoTela As String) As DataSet
        Dim dttBloqueado As New DataTable("dttBloqueado")
        Dim dtsAcessos As New DataSet()

        'Dim cmdText As String = "select " &
        '                        "  acecodtel, usucodusu, usunomusu, acesitace " &
        '                        "from " &
        '                        "  TbAcesso, TbUsuario " &
        '                        "where " &
        '                        "  acecodusu = usucodusu and acesitace = 'BLOQUEADO' and " &
        '                        "  acecodusu = '" & pstrUser & "' " &
        '                        "order by " &
        '                        "  usunomusu"
        Dim cmdText As String = "select " &
                                "  acecodtel, usucodusu, usunomusu, acesitace, descricao " &
                                "from " &
                                "  TbAcesso " &
                                " inner join TbUsuario on acecodusu = usucodusu " &
                                " left join TbTelas on Codtelas = acecodtel " &
                                "where " &
                                "  acesitace = 'BLOQUEADO' and " &
                                "  acecodusu = '" & pstrUser & "' " &
                                "order by " &
                                "  usunomusu"

        If (String.IsNullOrEmpty(pstrUser)) Then
            cmdText = cmdText.Replace("and   acecodusu = ''", "")
        End If

        If Not (String.IsNullOrWhiteSpace(ptxtCodigoTela)) Then
            cmdText = cmdText.Replace("order by ", " and acecodtel = '" + ptxtCodigoTela + "' order by ")
        End If


        dttBloqueado = modSQL.Fill(cmdText)
        dttBloqueado.TableName = "dttBloqueado"

        dtsAcessos.Tables.Add(dttBloqueado)

        Return dtsAcessos
    End Function

    Public Function ConsultarLiberados(ByVal CodigoTela As String) As DataSet
        Dim dttLiberado As New DataTable("dttLiberado")
        Dim dtsAcessos As New DataSet()

        'Dim cmdText = "select " & _
        '          "  acecodtel, usucodusu, usunomusu, acesitace, acedatace " & _
        '          "from " & _
        '          "  TbAcesso, TbUsuario " & _
        '          "where " & _
        '          "  acecodusu = usucodusu and acesitace = 'LIBERADO' and " & _
        '          "  acecodtel = '" & CodigoTela & "' " & _
        '          "order by " & _
        '          "  acedatace desc, usunomusu"

        Dim cmdText = "select " &
                  "  acecodtel, usucodusu, usunomusu, acesitace, acedatace, descricao " &
                  "from " &
                  "  TbAcesso  " &
                  " inner join TbUsuario on acecodusu = usucodusu " &
                  " left join codtelas on codtela = acecodtel " &
                  "where " &
                  "  acesitace = 'LIBERADO' and " &
                  "  acecodtel = '" & CodigoTela & "' " &
                  "order by " &
                  "  acedatace desc, usunomusu"


        dttLiberado = modSQL.Fill(cmdText)
        dttLiberado.TableName = "dttLiberado"

        dtsAcessos.Tables.Add(dttLiberado)

        Return dtsAcessos
    End Function

    Public Function ConsultarUsuariosLiberados(ByVal pstrUser As String, ByVal ptxtCodigoTela As String) As DataSet
        Dim dttLiberado As New DataTable("dttLiberado")
        Dim dtsAcessos As New DataSet()

        'Dim cmdText = "select " &
        '          "  acecodtel, usucodusu, usunomusu, acesitace, acedatace " &
        '          "from " &
        '          "  TbAcesso, TbUsuario " &
        '          "where " &
        '          "  acecodusu = usucodusu and acesitace = 'LIBERADO' and " &
        '          "  acecodusu = '" & pstrUser & "' " &
        '          "order by " &
        '          "  acedatace desc, usunomusu"
        Dim cmdText = "select " &
                  "  acecodtel, usucodusu, usunomusu, acesitace, acedatace, descricao " &
                  "from " &
                  "  TbAcesso " &
                  " inner join TbUsuario on acecodusu = usucodusu " &
                  " left join Tbtelas on Codtelas = acecodtel " &
                  "where " &
                  "  acesitace = 'LIBERADO' and " &
                  "  acecodusu = '" & pstrUser & "' " &
                  "order by " &
                  "  acedatace desc, usunomusu"


        If (String.IsNullOrEmpty(pstrUser)) Then
            cmdText = cmdText.Replace("and   acecodusu = ''", "")
        End If

        If Not (String.IsNullOrEmpty(ptxtCodigoTela)) Then
            cmdText = cmdText.Replace("order by ", " and acecodtel ='" + ptxtCodigoTela + "' order by ")
        End If

        dttLiberado = modSQL.Fill(cmdText)
        dttLiberado.TableName = "dttLiberado"

        dtsAcessos.Tables.Add(dttLiberado)

        Return dtsAcessos
    End Function
    'Marcio Spinosa - 15/06/2018
    ''' <summary>
    ''' Método criado para liberação de telas mais prático
    ''' </summary>
    ''' <returns>retorna uma lista de todas as telas do sistema VNF</returns>
    ''' <remarks></remarks>
    Public Function ConsultarTelas(Optional ByVal pStrUsuario As String = "") As DataSet
        Dim dttTelas As New DataTable("dttTelas")
        Dim dtsTelas As New DataSet()

        Dim cmdText = "select distinct acecodtel from TbAcesso"

        If Not (String.IsNullOrEmpty(pStrUsuario)) Then
            cmdText += " Where acecodusu = '" & pStrUsuario & "'"
        End If

        dttTelas = modSQL.Fill(cmdText)

        If (dttTelas.Rows.Count = 0) And String.IsNullOrEmpty(pStrUsuario) Then
            dttTelas = modSQL.Fill("select codtelas as acecodtel from tbtelas")
        End If

        dttTelas.TableName = "dttTelas"
        dtsTelas.Tables.Add(dttTelas)

            Return dtsTelas
    End Function
    'Marcio Spinosa - 15/06/2018 - Fim

    Public Sub LiberarAcesso(ByVal CodigoTela As String, ByVal Usuario As String)
        Dim cmdText As String = "update TbAcesso set acesitace = 'LIBERADO' where acecodtel = '" & CodigoTela & "' and acecodusu = '" & Usuario & "'"
        modSQL.ExecuteNonQuery(cmdText)
    End Sub

    Public Sub BloquearAcesso(ByVal CodigoTela As String, ByVal Usuario As String)
        Dim cmdText As String = "update TbAcesso set acesitace = 'BLOQUEADO' where acecodtel = '" & CodigoTela & "' and acecodusu = '" & Usuario & "'"
        modSQL.ExecuteNonQuery(cmdText)
    End Sub

    Public Function ConsultaAcesso(ByVal CodigoTela As String, ByVal Usuario As String) As Boolean
        Dim cmdText As String = "SELECT acesitace FROM TbAcesso WHERE acecodtel = '" & CodigoTela & "' and acecodusu = '" & Usuario & "'"
        Dim dt As DataTable = modSQL.Fill(cmdText)

        If dt.Rows.Count > 0 Then
            If (dt.Rows(0)("acesitace").ToString() = "LIBERADO") Then
                Return True
            End If
        Else
            modSQL.ExecuteNonQuery("INSERT INTO TbAcesso (acecodtel, acecodusu, acedatace, acesitace) VALUES ('" & CodigoTela & "', '" & Usuario & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', 'BLOQUEADO')")
        End If

        Return False
    End Function

End Class
