Imports MetsoFramework.Utils
Imports System.Data.SqlClient

Public Class BLDivergencias

    Public Function GetByFilter(ByVal DataLogInicial As String, ByVal DataLogFinal As String, ByVal DataNfInicial As String, ByVal DataNfFinal As String, ByVal NumeroNF As String,
                                ByVal Situacao As String, ByVal Motivo As String, ByVal PurchaseOrder As String, ByVal CodComprador As String, ByVal Fornecedor As String,
                                ByVal CondicaoPagamento As Boolean, ByVal RemessaFinal As Boolean, ByVal AvisoEmbarque As Boolean,
                                ByVal AntecipacaoPedido As Boolean, ByVal CnpjEmitente As Boolean, ByVal Quantidade As Boolean,
                                ByVal Aprovado As Boolean, ByVal Deletado As Boolean, ByVal Valor As Boolean, ByVal Planta As Boolean,
                                ByVal Ncm As Boolean, ByVal Re As Boolean) As DataTable

        Dim datDataLogInicial As String = DataLogInicial.Trim()
        If datDataLogInicial <> "/  /" Then
            Date.Parse(datDataLogInicial)
        End If

        Dim datDataLogFinal As String = DataLogFinal.Trim()
        If datDataLogFinal <> "/  /" Then
            Date.Parse(datDataLogFinal)
        End If

        Dim datDataNfInicial As String = DataNfInicial.Trim()
        If datDataNfInicial <> "/  /" Then
            Date.Parse(datDataNfInicial)
        End If

        Dim datDataNfFinal As String = DataNfFinal.Trim()
        If datDataNfFinal <> "/  /" Then
            Date.Parse(datDataNfFinal)
        End If

        Dim strQuery As String = "SELECT DISTINCT  " &
                                 "	   a.CODLOG,  " &
                                 "	   a.DATLOG,  " &
                                 "	   a.CODCOM,  " &
                                 "	   a.PEDCOM,  " &
                                 "	   a.ITEPED,  " &
                                 "	   a.CAMPO,  " &
                                 "	   a.VALOR_PED,  " &
                                 "	   a.VALOR_NFE,  " &
                                 "	   a.CODFOR,  " &
                                 "	   c.RAZFOR,  " &
                                 "	   a.NFEID,  " &
                                 "	   a.ITENFE + 1 as ITENFE,  " &
                                 "	   h.VNF_TIPO_DOCUMENTO,  " &
                                 "	   e.NF_IDE_NNF,  " &
                                 "	   e.NF_IDE_SERIE,  " &
                                 "	   e.NF_IDE_DHEMI,  " &
                                 "	   e.NF_EMIT_XNOME,  " &
                                 "	   d.DATVAL,  " &
                                 "	   a.SITUACAO,  " &
                                 "	   a.MOTIVO,  " &
                                 "	   a.DATA_CORRECAO,  " &
                                 "	   a.JUSTIFICATIVA,  " &
                                 "	   a.CODMAT,  " &
                                 "	   a.DESMAT,  " &
                                 "	   c.CNPJ,  " &
                                 "	   c.INTEGRADO,  " &
                                 "	   g.NOMCOM,  " &
                                 "	   f.DATVER,  " &
                                 "	   f.SITUACAO as SITUACAO_CHEGADA,  " &
                                 "	   DATEDIFF(dd,  e.NF_IDE_DHEMI, d.DATVAL) as TEMPO_TRANSMISSAO,  " &
                                 "	   DATEDIFF(dd,  e.NF_IDE_DHEMI, a.DATLOG) as TEMPO_VERIFICACAO_DD,  " &
                                 "	   DATEDIFF(hh,  e.NF_IDE_DHEMI, a.DATLOG) as TEMPO_VERIFICACAO_HH,  " &
                                 "	   DATEDIFF(mi,  e.NF_IDE_DHEMI, a.DATLOG) as TEMPO_VERIFICACAO_MM,  " &
                                 "	   DATEDIFF(dd,  a.DATLOG, a.DATA_CORRECAO) as TEMPO_CORRECAO_DD,  " &
                                 "	   DATEDIFF(hh,  a.DATLOG, a.DATA_CORRECAO) as TEMPO_CORRECAO_HH,  " &
                                 "	   DATEDIFF(mi,  a.DATLOG, a.DATA_CORRECAO) as TEMPO_CORRECAO_MM,  " &
                                 "	   DATEDIFF(dd,  a.DATLOG, f.DATVER) as TEMPO_COMPRADOR_DD,  " &
                                 "	   DATEDIFF(hh,  a.DATLOG, f.DATVER) as TEMPO_COMPRADOR_HH,  " &
                                 "	   DATEDIFF(mi,  a.DATLOG, f.DATVER) as TEMPO_COMPRADOR_MM  " &
                                 "FROM  " &
                                 "	   TbLOG a  " &
                                 "	   left join TbJUN b on a.NFEID = b.NFEID  " &
                                 "	   left join TbFOR c on a.CODFOR = c.CODFOR  " &
                                 "	   left join TbNFE d on a.NFEID = d.NFEID  " &
                                 "	   left join TbDOC_CAB_NFE e on d.NFEID = e.NFEID  " &
                                 "	   left join TbVER f on a.NFEID = f.NFEID  " &
                                 "	   left join TbCOM g on a.CODCOM = g.CODCOM  " &
                                 "	   left join TbDOC_CAB h on a.NFEID = h.NFEID  " &
                                 "WHERE  " &
                                 "	   a.CODLOG > 0  "

        If Situacao <> "(TODOS)" AndAlso Situacao <> "(TODAS)" Then
            strQuery = strQuery & " and a.SITUACAO = '" & Situacao & "' "
        End If

        If Motivo <> "(TODOS)" Then
            strQuery = strQuery & " and a.MOTIVO = '" & Motivo & "' "
        End If

        If Not String.IsNullOrEmpty(Fornecedor) Then
            strQuery = strQuery & " and c.RAZFOR LIKE '%' + '" & Fornecedor.Trim() & "' + '%' "
        End If

        Dim CODCOM As String = CodComprador.Trim()
        Dim PEDCOM As String = PurchaseOrder.Trim()
        Dim NUMNF As String = NumeroNF.Trim()

        If CODCOM <> "" Then strQuery = strQuery & " and a.CODCOM = '" & CODCOM & "' "
        If PEDCOM <> "" Then strQuery = strQuery & " and b.PEDCOM = '" & PEDCOM & "' "
        If NUMNF <> "" Then strQuery = strQuery & " and e.NF_IDE_NNF = '" & NUMNF & "' "

        If datDataLogInicial <> "/  /" Then
            strQuery = strQuery & " and a.DATLOG >= '" & Format(Date.Parse(datDataLogInicial), "yyyy-MM-dd") & " 00:00:00' "
        End If

        If datDataLogFinal <> "/  /" Then
            strQuery = strQuery & " and a.DATLOG <= '" & Format(Date.Parse(datDataLogFinal), "yyyy-MM-dd") & " 23:59:59' "
        End If


        If datDataNfInicial <> "/  /" Then
            strQuery = strQuery & " and e.NF_IDE_DHEMI >= '" & Format(Date.Parse(datDataNfInicial), "yyyy-MM-dd") & " 00:00:00' "
        End If

        If datDataNfFinal <> "/  /" Then
            strQuery = strQuery & " and e.NF_IDE_DHEMI <= '" & Format(Date.Parse(datDataNfFinal), "yyyy-MM-dd") & " 23:59:59' "
        End If


        If Valor Or Quantidade Or CondicaoPagamento Or Aprovado Or Deletado Or RemessaFinal Or Ncm Or Planta Or CnpjEmitente Or Re Or AvisoEmbarque Or AntecipacaoPedido Then

            strQuery = strQuery & " and ("
            If Valor Then strQuery = strQuery & " a.CAMPO = 'VALOR' or "
            If Quantidade Then strQuery = strQuery & " a.CAMPO = 'QUANTIDADE' or "
            If CondicaoPagamento Then strQuery = strQuery & " a.CAMPO = 'CONDICAO PAGAMENTO' or "
            If Aprovado Then strQuery = strQuery & " a.CAMPO = 'APROVADO' or "
            If Deletado Then strQuery = strQuery & " a.CAMPO = 'DELETADO' or "
            If RemessaFinal Then strQuery = strQuery & " a.CAMPO = 'REMESSA FINAL' or "
            If Ncm Then strQuery = strQuery & " a.CAMPO = 'NCM' or "
            If Planta Then strQuery = strQuery & " a.CAMPO = 'PLANTA' or "
            If CnpjEmitente Then strQuery = strQuery & " a.CAMPO = 'CNPJ' or "
            If Re Then strQuery = strQuery & " a.CAMPO = 'REGIME ESPECIAL' or "
            If AvisoEmbarque Then strQuery = strQuery & " a.CAMPO = 'AVISO DE EMBARQUE' or "
            If AntecipacaoPedido Then strQuery = strQuery & " a.CAMPO = 'ANTECIPAÇÃO DO PEDIDO' or "

            strQuery = Mid(strQuery, 1, Len(strQuery) - 4)
            strQuery = strQuery & ")"
        End If

        strQuery = strQuery & " order by a.DATLOG desc"

        Return modSQL.Fill(strQuery)

    End Function

    Public Function DownloadAnexoDivergencia(ByVal CodLog) As DataTable
        Dim cmd = String.Format("SELECT ANEXO, ANEXONOME, ANEXOEXTENSAO FROM TBLOG WHERE CODLOG = {0}", CodLog)
        Dim dt As DataTable = modSQL.Fill(cmd)
        Return dt
    End Function

    Public Function Anular(ByVal NfeID As String, ByVal CodLog As Integer, ByVal Justificativa As String, ByVal Detalhe As String, ByVal Anexo As Byte(), ByVal AnexoNome As String, AnexoExtemsao As String) As String ', PermissaoModulo As String) As String
        Dim query As String = "SELECT CASE WHEN COUNT(*) in (2, 3) AND SUM(LiberadoComponente) > 2 AND COUNT(distinct id_validacao) = 1 THEN 'LIBERADO' ELSE 'BLOQUEADO' END AS resultado " &
                              "FROM ( " &
                              "     SELECT a.acecodtel, " &
                              "     	   a.acesitace, " &
                              "     	   CASE WHEN a.acesitace = 'LIBERADO' THEN (CASE WHEN a.acecodtel = 'ANUL' THEN 2 ELSE 1 END) ELSE 0 END AS LiberadoComponente " &
                              "     	   ,l.CAMPO " &
                              "     	   ,ISNULL(v.id_validacao, 0) as id_validacao " &
                              "     	   ,ISNULL(v.val_permitir_anulacao_compras, 0) AS val_permitir_anulacao_compras " &
                              "     	   ,ISNULL(v.val_permitir_anulacao_fiscal, 0)  AS val_permitir_anulacao_fiscal " &
                              "     	   ,v.val_titulo_usuario " &
                              "       FROM TbAcesso a " &
                              "      INNER JOIN TbLog l " &
                              "     	ON SITUACAO = 'ATIVO' " &
                              "        AND CODLOG = " & CodLog &
                              "      INNER JOIN TbValidacoes v " &
                              "     	ON v.val_titulo_usuario LIKE l.CAMPO " &
                              "      WHERE ((v.val_permitir_anulacao_compras = 1 AND a.acecodtel IN ('ANUL','COMP') ) " &
                              "     	 OR (v.val_permitir_anulacao_fiscal  = 1 AND a.acecodtel IN ('ANUL', 'VNFE'))) " &
                              "        AND a.acecodusu = '" & Uteis.LogonName() & "' " &
                              ") a "

        If modSQL.ExecuteScalar(query) <> "LIBERADO" Then
            modSQL.RegistrarAcesso("ANUL")
            Throw New AccessViolationException("Você não tem autorização para anular divergências!" & vbNewLine & "Favor entrar em contato com o seu supervisor para anular esta divergência.")
        End If

        Dim CmdText As String = String.Format("update TbLOG set SITUACAO = 'INATIVO', MOTIVO = 'ANULADO', JUSTIFICATIVA = '" & Justificativa & "', acecodusu = '" & Uteis.LogonName() & "', DETALHE = '" & Detalhe & "', Anexo = @Anexo, AnexoNome = '" & AnexoNome & "', AnexoExtensao = '" & AnexoExtemsao & "'  where SITUACAO = 'ATIVO' and CODLOG = " & CodLog)
        Dim sqlparams As New List(Of SqlClient.SqlParameter)
        Dim param As New SqlClient.SqlParameter("@Anexo", SqlDbType.VarBinary)
        If Anexo Is Nothing Then
            param.Value = DBNull.Value
        Else
            param.Value = Anexo
        End If
        sqlparams.Add(param)

        modSQL.ExecuteNonQueryParams(CmdText, sqlparams)

        '---> SE NÃO EXISTEM DIVERGÊNCIAS ATIVA, A NOTA FISCAL ESTÁ ACEITA
        CmdText = "select isnull(count(*),0) from TbLOG where NFEID = '" & NfeID & "' and SITUACAO = 'ATIVO'"
        If modSQL.ExecuteScalar(CmdText) = "0" Then
            CmdText = "update TbNFE set SITUACAO = 'ACEITA', NFEVAL = 'S', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' where NFEID = '" & NfeID & "'"
            modSQL.ExecuteNonQuery(CmdText)

            Dim objBLVerificar As New modVerificar()
            objBLVerificar.EnviarMensagemParaFornecedor(NfeID, "ACEITA")
        End If

        Dim objBLNotaFiscal As New BLNotaFiscal()
        BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.AnularDivergencia, "Anulou a divergência da " & objBLNotaFiscal.GetNumeroDocumento(NfeID) & ". O documento está com status " & objBLNotaFiscal.GetSituacaoDocumento(NfeID), NfeID)

        Return String.Empty
    End Function

    Public Function GetChartDivergenciasAtivas() As Dictionary(Of String, Object)
        Dim strQuery As String = "SELECT  " & _
                                 "	CAMPO, " & _
                                 "	count(*) as 'QTD' " & _
                                 "FROM " & _
                                 "	tblog " & _
                                 "WHERE " & _
                                 "	SITUACAO = 'ATIVO' " & _
                                 "GROUP BY " & _
                                 "	CAMPO " & _
                                 "ORDER BY " & _
                                 "	CAMPO "

        Dim dttDados = New DataTable()
        dttDados = modSQL.Fill(strQuery)

        Dim strChart As String = String.Empty
        Dim strLegenda As String = String.Empty
        For index = 0 To dttDados.Rows.Count - 1
            strChart &= "[" & index.ToString() & ", " & dttDados.Rows(index)("QTD") & "], "
            strLegenda &= "[" & index.ToString() & ", """ & dttDados.Rows(index)("CAMPO") & """], "
        Next
        strChart = "[" & strChart.Substring(0, strChart.Length - 2) & "]"
        strLegenda = "[" & strLegenda.Substring(0, strLegenda.Length - 2) & "]"

        Dim objRetorno = New Dictionary(Of String, Object)
        objRetorno.Add("dados", strChart)
        objRetorno.Add("legenda", strLegenda)

        Return objRetorno
    End Function

    Public Function GetQtdDivergenciasAtivasUsuario(ByVal Usuario As String) As Integer
        Dim strQuery As String = "SELECT  " & _
                                 "	count(*) " & _
                                 "FROM  " & _
                                 "	tbLog " & _
                                 "	inner join tbCom " & _
                                 "	on tbLog.codCom = tbCom.codCom " & _
                                 "	inner join tbUsuario " & _
                                 "	on tbCom.Email = tbUsuario.usuEmail " & _
                                 "WHERE " & _
                                 "	situacao = 'ATIVO' " & _
                                 "	and usucodusu = '" & Usuario & "' "

        Return Convert.ToInt32(modSQL.ExecuteScalar(strQuery))
    End Function

    Public Function GetDataMonitorDivergencais(ByVal pStrCategoriaComprador As Integer, ByVal pStrTopSQLCommand As String, ByVal pStrUnidadeMetso As String) As SqlDataReader

        Dim vObjDR As SqlDataReader = Nothing

        Try
            modSQL.CommandText = "EXEC SP_GET_RELATORIO_COMPRAS_INDIRETO " & pStrTopSQLCommand & ", '" & pStrCategoriaComprador & "','" & pStrUnidadeMetso & "'"
            vObjDR = modSQL.ExecuteReader(modSQL.CommandText, modSQL.connectionString)
            Return vObjDR
        Catch ex As Exception
            Return vObjDR
        End Try



    End Function

End Class
