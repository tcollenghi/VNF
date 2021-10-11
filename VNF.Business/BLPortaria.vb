Imports System.Text
Imports MetsoFramework.Utils

Public Class BLPortaria

    Public Function GetByFilter(ByVal Unidade As String, ByVal Pasta As String) As DataTable

        Dim strFiltroPasta As String = ""
        If (Not String.IsNullOrEmpty(Pasta)) Then
            strFiltroPasta = " AND IDPASTA = '" & Pasta & "'"
        End If

        Return modSQL.Fill("SELECT IDPASTA, DATLAN, PLACA, NOMTRA, NOMMOT, HORCHE, HORENT, SETOR, " &
                           "dbo.sp_get_situacao_pasta('" & Unidade & "', IDPASTA) AS 'SITUACAO', " &
                           "dbo.sp_check_prioridade_alta_pasta('" & Unidade & "', IDPASTA) AS 'PRIORIDADE_ALTA', " &
                           "COUNT(NFEID) AS QTD_NOTAS " &
                           "FROM TBPORT_EF " &
                           "WHERE HORSAI IS NULL AND PLANTA = '" & Unidade & "' " & strFiltroPasta &
                           "GROUP BY IDPASTA, DATLAN, PLACA, NOMTRA, NOMMOT, HORCHE, HORENT, SETOR")
    End Function

    Public Function GetNotasFiscais(Optional ByVal NfeId As String = Nothing, Optional ByVal Unidade As String = Nothing, Optional ByVal Pasta As String = Nothing, Optional ByVal NumeroNF As String = Nothing) As DataTable
        Dim strSQLLista As String = ""

        If NfeId = Nothing And Not String.IsNullOrEmpty(Pasta) Then
            '---> CONSULTAR DETALHES DA PASTA
            strSQLLista = "SELECT DISTINCT  " &
                          "	    IDPASTA,  " &
                          "	    TBNFE.NFEID,  " &
                          "	    NF_IDE_DHEMI,  " &
                          " 	NF_IDE_NNF,  " &
                          "	    NF_IDE_SERIE, " &
                          "	    VNF_TIPO_DOCUMENTO, " &
                          "	    VNF_MATERIAL_RECEBIDO, " &
                          "	    /*dbo.sp_limitar_string(NF_EMIT_XNOME, 25) AS 'NF_EMIT_XNOME',*/ " &
                          "     CASE WHEN LEN(NF_EMIT_XNOME) <= 25 " &
                          "          THEN NF_EMIT_XNOME " &
                          "          ELSE LEFT(NF_EMIT_XNOME, 25) + '...' " &
                          "     END AS 'NF_EMIT_XNOME', " &
                          "	    NF_EMIT_CNPJ,  " &
                          "	    CASE TBNFE.NFEREL WHEN 'N' THEN 'IRRELEVANTE' ELSE 'RELEVANTE' END AS 'REL',  " &
                          "	    CASE PLANTA WHEN 'EQUI' THEN 'Equipamentos' ELSE 'Fundição' END AS 'PLANTA',  " &
                          "	    UPPER(CASE WHEN TBNFE.SITUACAO = '' THEN 'Pendente' ELSE TBNFE.SITUACAO END) AS 'SITUACAO',  " &
                          "	    /*dbo.sp_check_prioridade_alta_documento(TBNFE.NFEID) as 'PRIORIDADE_ALTA' ,*/  " &
                          "     (SELECT " &
                          "		    CASE WHEN count(1) = 0 THEN 'NÃO' ELSE 'SIM' END " &
                          "      FROM " &
                          "		    TbDOC_ITEM with (nolock) " &
                          "		    inner join TbJUN with (nolock) " &
                          "		    on TbDOC_ITEM.nfeid = TbJUN.nfeid " &
                          "		    inner join pid_priorizacao_item_pedido with (nolock) " &
                          "		    on TbJUN.PEDCOM = pid_pedido and TbJUN.ITEPED = pid_item " &
                          "	      WHERE " &
                          "		    TbDOC_ITEM.nfeid = TBNFE.NFEID) as 'PRIORIDADE_ALTA', " &
                          "	    (SELECT COUNT(*) FROM OCORRENCIAS WHERE NFEID = TBNFE.NFEID) AS 'QTDE_OCORRENCIAS'  " &
                          "FROM  " &
                          "	    TBPORT_EF " &
                          "	    LEFT JOIN TBNFE ON TBPORT_EF.NFEID = TBNFE.NFEID " &
                          "	    LEFT JOIN TbDOC_CAB ON TBNFE.NFEID = TbDOC_CAB.NFEID " &
                          "	    LEFT JOIN TbDOC_CAB_NFE ON TBNFE.NFEID = TbDOC_CAB_NFE.NFEID " &
                          "	    LEFT JOIN TbIntegracao ON TBNFE.NFEID = TbIntegracao.NFEID " &
                          "WHERE " &
                          "	    (TBPORT_EF.HORCHE Is Not NULL And (TBPORT_EF.HORENT Is NULL Or TBPORT_EF.HORSAI Is NULL)) "

            '--> FILTRO POR UNIDADE
            If Unidade <> Nothing And Unidade <> "(TODAS)" Then
                If (Unidade.Length > 4) Then
                    Unidade = Unidade.Substring(0, 4)
                End If
                strSQLLista = strSQLLista & " AND PLANTA = '" & Unidade & "'"
            End If

            '--> FILTRO POR PASTA
            If Not Pasta = Nothing And Not String.IsNullOrEmpty(Pasta) Then
                strSQLLista = strSQLLista & " AND IDPASTA = " & Pasta
            End If

            '--> FILTRO POR NÚMERO DA NF
            If Not NumeroNF = Nothing And Not String.IsNullOrEmpty(NumeroNF) Then
                strSQLLista = strSQLLista & " AND NF_IDE_NNF = '" & NumeroNF & "'"
            End If

            '--> TRAZ AS NOTAS MAIS RECENTES NO TOPO
            strSQLLista = strSQLLista & " ORDER BY NF_IDE_DHEMI DESC, IDPASTA, PLANTA"

        ElseIf Not String.IsNullOrEmpty(NfeId) Then

            '---> ADICIONAR DOCUMENTO NA PASTA ATRAVÉS DA CHAVE DA NOTA FISCAL
            strSQLLista = "SELECT DISTINCT " &
                          "	    NF_IDE_DHEMI, " &
                          "	    TBNFE.DATVAL, " &
                          "	    NF_IDE_NNF, " &
                          "	    NF_IDE_SERIE, " &
                          "	    VNF_TIPO_DOCUMENTO, " &
                          "	    VNF_MATERIAL_RECEBIDO, " &
                          "	    /*dbo.sp_limitar_string(NF_EMIT_XNOME, 25) AS 'NF_EMIT_XNOME',*/ " &
                          "     CASE WHEN LEN(NF_EMIT_XNOME) <= 25 " &
                          "          THEN NF_EMIT_XNOME " &
                          "          ELSE LEFT(NF_EMIT_XNOME, 25) + '...' " &
                          "     END AS 'NF_EMIT_XNOME', " &
                          "	    NF_EMIT_CNPJ, " &
                          "	    CASE TBNFE.NFEREL WHEN 'N' THEN 'IRRELEVANTE' ELSE 'RELEVANTE' END AS 'REL', " &
                          "	    TBNFE.NFEID, UPPER(CASE WHEN TBNFE.SITUACAO = '' THEN 'Pendente' ELSE TBNFE.SITUACAO END ) AS 'SITUACAO', " &
                          "	    /*dbo.sp_check_prioridade_alta_documento(TBNFE.NFEID) as 'PRIORIDADE_ALTA',*/ " &
                          "     (SELECT " &
                          "		    CASE WHEN count(1) = 0 THEN 'NÃO' ELSE 'SIM' END " &
                          "      FROM " &
                          "		    TbDOC_ITEM with (nolock) " &
                          "		    inner join TbJUN with (nolock) " &
                          "		    on TbDOC_ITEM.nfeid = TbJUN.nfeid " &
                          "		    inner join pid_priorizacao_item_pedido with (nolock) " &
                          "		    on TbJUN.PEDCOM = pid_pedido and TbJUN.ITEPED = pid_item " &
                          "	      WHERE " &
                          "		    TbDOC_ITEM.nfeid = TBNFE.NFEID) as 'PRIORIDADE_ALTA', " &
                          "	    (SELECT COUNT(*) FROM OCORRENCIAS WHERE NFEID = TBNFE.NFEID) AS 'QTDE_OCORRENCIAS' " &
                          "FROM " &
                          "	    TBNFE " &
                          "	    LEFT JOIN TbDOC_CAB ON TBNFE.NFEID = TbDOC_CAB.NFEID " &
                          "	    LEFT JOIN TbDOC_CAB_NFE ON TBNFE.NFEID = TbDOC_CAB_NFE.NFEID " &
                          "	    LEFT JOIN TbIntegracao ON TBNFE.NFEID = TbIntegracao.NFEID " &
                          "WHERE " &
                          "	    TBNFE.NFEID = '" & NfeId & "' "

        ElseIf Not NumeroNF = Nothing And Not String.IsNullOrEmpty(NumeroNF) Then
            strSQLLista = "SELECT DISTINCT " &
                          "	    TBNFE.NFEID, " &
                          "	    NF_IDE_NNF, " &
                          "	    NF_IDE_SERIE, " &
                          "	    NF_IDE_DHEMI, " &
                          "	    VNF_TIPO_DOCUMENTO, " &
                          "	    VNF_MATERIAL_RECEBIDO, " &
                          "	    /*dbo.sp_limitar_string(NF_EMIT_XNOME, 25) AS 'NF_EMIT_XNOME',*/ " &
                          "     CASE WHEN LEN(NF_EMIT_XNOME) <= 25 " &
                          "          THEN NF_EMIT_XNOME " &
                          "          ELSE LEFT(NF_EMIT_XNOME, 25) + '...' " &
                          "     END AS 'NF_EMIT_XNOME', " &
                          "	    NF_EMIT_CNPJ, " &
                          "	    CASE TBNFE.NFEREL WHEN 'N' THEN 'IRRELEVANTE' ELSE 'RELEVANTE' END AS 'REL', " &
                          "	    UPPER(CASE WHEN TBNFE.SITUACAO = '' THEN 'Pendente' ELSE TBNFE.SITUACAO END ) AS 'SITUACAO', " &
                          "	    /*dbo.sp_check_prioridade_alta_documento(TBNFE.NFEID) as 'PRIORIDADE_ALTA',*/ " &
                          "     (SELECT " &
                          "		    CASE WHEN count(1) = 0 THEN 'NÃO' ELSE 'SIM' END " &
                          "      FROM " &
                          "		    TbDOC_ITEM with (nolock) " &
                          "		    inner join TbJUN with (nolock) " &
                          "		    on TbDOC_ITEM.nfeid = TbJUN.nfeid " &
                          "		    inner join pid_priorizacao_item_pedido with (nolock) " &
                          "		    on TbJUN.PEDCOM = pid_pedido and TbJUN.ITEPED = pid_item " &
                          "	      WHERE " &
                          "		    TbDOC_ITEM.nfeid = TBNFE.NFEID) as 'PRIORIDADE_ALTA', " &
                          "	    (SELECT COUNT(*) FROM OCORRENCIAS WHERE NFEID = TBNFE.NFEID) AS 'QTDE_OCORRENCIAS', " &
                          "	    CASE  " &
                          "		    WHEN VNF_TIPO_DOCUMENTO = 'CTE' and count(distinct INT_MIRO_MAT_DOC_NUMBER) = 1 THEN " &
                          "		    'CONCLUÍDO' " &
                          "		    WHEN count(distinct int_migo_nf_item_number) = 0 THEN " &
                          "		    'PENDENTE' " &
                          "		    WHEN count(distinct int_migo_nf_item_number) < (select count(distinct nf_prod_item) from tbdoc_item_nfe where tbdoc_item_nfe.nfeid = TbIntegracao.nfeid) THEN " &
                          "		    'INCOMPLETA' " &
                          "		    ELSE " &
                          "		    'CONCLUÍDO' " &
                          "	    END as 'STATUS_INTEGRACAO'" &
                          "FROM " &
                          "	    TBNFE " &
                          "	    LEFT JOIN TbDOC_CAB ON TBNFE.NFEID = TbDOC_CAB.NFEID " &
                          "	    LEFT JOIN TbDOC_CAB_NFE ON TBNFE.NFEID = TbDOC_CAB_NFE.NFEID " &
                          "	    LEFT JOIN TbIntegracao ON TBNFE.NFEID = TbIntegracao.NFEID and int_migo_mat_doc_number <> '' and int_miro_mat_doc_number <> '' " &
                          "WHERE " &
                          "	    NF_IDE_NNF = '" & NumeroNF & "' " &
                          "GROUP BY " &
                          "	    TBNFE.NFEID, " &
                          "	    NF_IDE_NNF, " &
                          "	    NF_IDE_SERIE, " &
                          "	    NF_IDE_DHEMI, " &
                          "	    VNF_TIPO_DOCUMENTO, " &
                          "	    VNF_MATERIAL_RECEBIDO, " &
                          "	    /*dbo.sp_limitar_string(NF_EMIT_XNOME, 25),*/ " &
                          "     CASE WHEN LEN(NF_EMIT_XNOME) <= 25 " &
                          "          THEN NF_EMIT_XNOME " &
                          "          ELSE LEFT(NF_EMIT_XNOME, 25) + '...' " &
                          "     END, " &
                          "	    NF_EMIT_CNPJ, " &
                          "	    TBNFE.NFEREL, " &
                          "	    TBNFE.SITUACAO, " &
                          "	    TbIntegracao.nfeid " &
                          "ORDER BY " &
                          "	    NF_IDE_DHEMI DESC "
        End If

        If Not String.IsNullOrEmpty(strSQLLista) Then
            Return modSQL.Fill(strSQLLista)
        Else
            Return New DataTable()
        End If

    End Function

    Public Function RegistrarChegada(ByVal Unidade As String, ByVal Pasta As String, ByVal Motorista As String, ByVal Placa As String, ByVal Setor As String, ByVal Transportadora As String, ByVal DataChegada As String, ByVal NotaFiscal As String()) As String

        Dim dtNFE As New DataTable
        Dim NumeroNF As String = "null"
        Dim Serie As String = "null"
        Dim RAZFOR As String = ""
        Dim datChegada As DateTime = IIf(String.IsNullOrEmpty(DataChegada), DateTime.Now, DataChegada)

        '---> 1. VALIDAR INPUTS
        Dim strMessage As StringBuilder = New StringBuilder()
        If String.IsNullOrEmpty(Pasta) Then
            strMessage.AppendLine("- Preencha o campo PASTA")
        End If
        If String.IsNullOrEmpty(Motorista) Then
            strMessage.AppendLine("- Preencha o campo MOTORISTA")
        End If
        If String.IsNullOrEmpty(Placa) Then
            strMessage.AppendLine("- Preencha o campo PLACA")
        End If
        If String.IsNullOrEmpty(Setor) Then
            strMessage.AppendLine("- Preencha o campo SETOR")
        End If
        If String.IsNullOrEmpty(Transportadora) Then
            strMessage.AppendLine("- Preencha o campo TRANSPORTADORA")
        End If
        If NotaFiscal.Count = 0 Then
            strMessage.AppendLine("- Insira uma ou mais DANFEs")
        End If
        If strMessage.Length <> 0 Then
            Return strMessage.ToString()
        End If

        '---> 2. VALIDAR SITUAÇÃO DA PASTA
        If Not PastaParaChegada(Unidade, Pasta, DataChegada) Then
            Return "A pasta já está em uso. Selecione outra numeração!"
        End If

        '---> 3. LIMPA A TABELA DE ITENS DA NF PARA INSERIR NOVAMENTE
        modSQL.ExecuteNonQuery("DELETE TBPORT_EF WHERE PLANTA = '" & Unidade & "' AND IDPASTA = '" & Pasta & "' AND HORCHE = '" & datChegada.ToString("yyyy-MM-dd HH:mm:ss") & "'")

        '---> 4. INSERE OS DOCUMENTOS FISCAIS
        For i As Integer = 0 To NotaFiscal.Count - 1
            modSQL.CommandText = "SELECT " &
                                 "	tbnfe.NFEID, " &
                                 "	TbDOC_CAB_NFE.NF_IDE_NNF, " &
                                 "	TbDOC_CAB_NFE.NF_IDE_SERIE,  " &
                                 "	TbDOC_CAB_NFE.NF_EMIT_XNOME,  " &
                                 "	TbDOC_CAB.VNF_TIPO_DOCUMENTO,  " &
                                 "	tbnfe.SITUACAO " &
                                 "FROM  " &
                                 "	tbnfe  " &
                                 "	left join TbDOC_CAB on tbnfe.NFEID = TbDOC_CAB.NFEID " &
                                 "	left join TbDOC_CAB_NFE on tbnfe.NFEID = TbDOC_CAB_NFE.NFEID " &
                                 "WHERE  " &
                                 "	tbnfe.NFEID = '" & NotaFiscal(i) & "' "
            dtNFE = modSQL.Fill(modSQL.CommandText)

            If dtNFE.Rows.Count > 0 Then

                NumeroNF = dtNFE.Rows(0)("NF_IDE_NNF").ToString
                Serie = dtNFE.Rows(0)("NF_IDE_SERIE").ToString
                RAZFOR = dtNFE.Rows(0)("NF_EMIT_XNOME").ToString.Trim

                If (dtNFE.Rows(0)("SITUACAO").ToString = "REJEITADA") Then
                    BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.PortariaRejeitada, "Registrou a chegada da " & dtNFE.Rows(0)("VNF_TIPO_DOCUMENTO").ToString & dtNFE.Rows(0)("NF_IDE_NNF").ToString & " na portaria, porém o material está com divergências pendentes.", NotaFiscal(i))
                ElseIf (dtNFE.Rows(0)("SITUACAO").ToString = "PENDENTE") Then
                    BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.PortariaPendente, "Registrou a chegada da " & dtNFE.Rows(0)("VNF_TIPO_DOCUMENTO").ToString & dtNFE.Rows(0)("NF_IDE_NNF").ToString & " na portaria, porém o material não está associado.", NotaFiscal(i))
                Else
                    BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.PortariaAceita, "Registrou a chegada da " & dtNFE.Rows(0)("VNF_TIPO_DOCUMENTO").ToString & dtNFE.Rows(0)("NF_IDE_NNF").ToString & " na portaria.", NotaFiscal(i))
                End If

            End If

            modSQL.CommandText = "INSERT INTO TbPORT_EF " &
                                 "( " &
                                 "	 IDPASTA " &
                                 "	,DATLAN " &
                                 "	,PLACA " &
                                 "	,NOMTRA " &
                                 "	,NOMMOT " &
                                 "	,NFEID " &
                                 "	,NFNUM " &
                                 "	,NFSERIE " &
                                 "	,RAZFOR " &
                                 "	,HORCHE " &
                                 "	,HORENT " &
                                 "	,HORSAI " &
                                 "	,SETOR " &
                                 "	,PLANTA " &
                                 "	,OBS " &
                                 ") " &
                                 "VALUES " &
                                 "( " &
                                 "	 @IDPASTA " &
                                 "	,@DATLAN " &
                                 "	,@PLACA " &
                                 "	,@NOMTRA " &
                                 "	,@NOMMOT " &
                                 "	,@NFEID " &
                                 "	,@NFNUM " &
                                 "	,@NFSERIE " &
                                 "	,@RAZFOR " &
                                 "	,@HORCHE " &
                                 "	,@HORENT " &
                                 "	,@HORSAI " &
                                 "	,@SETOR " &
                                 "	,@PLANTA " &
                                 "	,@OBS " &
                                 ") "

            Dim sqlparams As New List(Of SqlClient.SqlParameter)
            sqlparams.Add(modSQL.AddSqlParameter("IDPASTA", SqlDbType.Int, Pasta))
            sqlparams.Add(modSQL.AddSqlParameter("DATLAN", SqlDbType.DateTime, datChegada))
            sqlparams.Add(modSQL.AddSqlParameter("PLACA", SqlDbType.VarChar, Placa))
            sqlparams.Add(modSQL.AddSqlParameter("NOMTRA", SqlDbType.VarChar, Transportadora))
            sqlparams.Add(modSQL.AddSqlParameter("NOMMOT", SqlDbType.VarChar, Motorista))
            sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal(i)))
            sqlparams.Add(modSQL.AddSqlParameter("NFNUM", SqlDbType.VarChar, NumeroNF))
            sqlparams.Add(modSQL.AddSqlParameter("NFSERIE", SqlDbType.VarChar, Serie))
            sqlparams.Add(modSQL.AddSqlParameter("RAZFOR", SqlDbType.VarChar, RAZFOR))
            sqlparams.Add(modSQL.AddSqlParameter("HORCHE", SqlDbType.DateTime, datChegada))
            sqlparams.Add(modSQL.AddSqlParameter("HORENT", SqlDbType.DateTime, Nothing))
            sqlparams.Add(modSQL.AddSqlParameter("HORSAI", SqlDbType.DateTime, Nothing))
            sqlparams.Add(modSQL.AddSqlParameter("SETOR", SqlDbType.VarChar, Setor))
            sqlparams.Add(modSQL.AddSqlParameter("PLANTA", SqlDbType.VarChar, Unidade))
            sqlparams.Add(modSQL.AddSqlParameter("OBS", SqlDbType.VarChar, Nothing))
            modSQL.ExecuteNonQueryParams(modSQL.CommandText, sqlparams)

            'Verificar os itens de alta prioridade
            Dim dtAltaPrioridade As New DataTable
            dtAltaPrioridade = modSQL.Fill("SELECT DISTINCT " &
                                                "id_priorizacao_item_pedido, " &
                                                "PEDCOM as pedido, " &
                                                "ITEPED as item_pedido, " &
                                                "pid_recebido, " &
                                                "pid_data_limite " &
                                            "FROM " &
                                                "TbJun " &
                                            "INNER JOIN pid_priorizacao_item_pedido " &
                                                "ON PEDCOM = pid_pedido " &
                                            "WHERE " &
                                                "NFEID = '" & NotaFiscal(i) & "'" &
                                            "ORDER BY " &
                                                "pedido, item_pedido ")

            Dim pedidosItens As String() = New String(dtAltaPrioridade.Rows.Count - 1) {}

            If dtAltaPrioridade.Rows.Count > 0 Then
                For y As Integer = 0 To dtAltaPrioridade.Rows.Count - 1
                    If dtAltaPrioridade.Rows(y)("pid_recebido").ToString().ToUpper() <> "TRUE" And datChegada.Date <= Convert.ToDateTime(dtAltaPrioridade.Rows(y)("pid_data_limite").ToString()).Date Then
                        'ATUALIZAR DADOS DA TABELA PID_PRIORIZACAO_ITEM_PEDIDO
                        modSQL.ExecuteNonQuery("UPDATE pid_priorizacao_item_pedido set pid_recebido = 1, pid_recebido_em = '" + datChegada.ToString("yyyy/MM/dd") + "' WHERE id_priorizacao_item_pedido = " & dtAltaPrioridade.Rows(y)("id_priorizacao_item_pedido").ToString())
                        pedidosItens(y) = dtAltaPrioridade.Rows(y)("pedido").ToString() & ";" & dtAltaPrioridade.Rows(y)("item_pedido").ToString()
                    End If
                Next

                'ENVIAR EMAIL PARA OS USUÁRIOS INFORMANDO QUE OS ITENS DE ALTA PRIORIDADE ENTRARAM
                Dim corpoEmail As String = "A Pasta " & Pasta & " contém a NF " & NumeroNF & " que possui alguns itens que entraram com alta prioridade:" & Environment.NewLine

                For y As Integer = 0 To pedidosItens.Length - 1
                    If y = 0 Then
                        corpoEmail &= Environment.NewLine & "Pedido: " & pedidosItens(y).Split(";")(0) & Environment.NewLine & "Itens:" & Environment.NewLine
                    ElseIf pedidosItens(y).Split(";")(0) <> pedidosItens(y - 1).Split(";")(0) Then
                        corpoEmail &= Environment.NewLine & "Pedido: " & pedidosItens(y).Split(";")(0) & Environment.NewLine & "Itens:" & Environment.NewLine
                    End If
                    corpoEmail &= pedidosItens(y).Split(";")(1) & Environment.NewLine
                Next

                Dim emails As String = modSQL.Fill("SELECT VALOR FROM TbPAR WHERE PARAMETRO = 'PRIORIZACAO_EMAIL'").Rows(0)(0)
                Dim strFrom As String = modSQL.ExecuteScalar("select valor from tbpar where parametro = 'EMAIL_VALIDADOR_NF'")
                'Uteis.SendMail("validador.notas.fiscais@metso.com", emails, Nothing, Nothing, "Itens de alta prioridade - PASTA " & Pasta & " / NF " & NotaFiscal(i), corpoEmail, Nothing)
                Uteis.SendMail(strFrom, emails, Nothing, Nothing, "Itens de alta prioridade - PASTA " & Pasta & " / NF " & NotaFiscal(i), corpoEmail, Nothing)
            End If
        Next

        BLLog.Insert(BLLog.LogType.Service, String.Empty, BLLog.LogTitle.CaminhaoChegada, "Caminhão com a pasta " & Pasta & " registrou a chegada na portaria.")
        Return String.Empty
    End Function

    Public Sub RegistrarEntrada(ByVal Unidade As String, ByVal Pasta As String)
        modSQL.ExecuteNonQuery("update tbPORT_EF set horent = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' where IDPASTA = " & Pasta & " and PLANTA = '" & Unidade & "' and horent is null")

        Dim intQtdNotasFiscais = Convert.ToInt32(modSQL.ExecuteScalar("SELECT count(*) FROM TBPORT_EF WHERE (PLANTA = '" & Unidade & "' AND IDPASTA = '" & Pasta & "' AND HORCHE IS NOT NULL AND HORENT IS NULL AND HORSAI IS NULL) " &
                                                                      " OR (PLANTA = '" & Unidade & "' AND IDPASTA = '" & Pasta & "' AND HORCHE IS NOT NULL AND HORENT IS NOT NULL AND HORSAI IS NULL)"))

        Dim dttNotasFiscais As New DataTable
        dttNotasFiscais = modSQL.Fill("SELECT TBPORT_EF.NFEID, VNF_TIPO_DOCUMENTO, NFNUM, NFSERIE " &
                                      "FROM TBPORT_EF left join tbdoc_cab on TBPORT_EF.nfeid = tbdoc_cab.nfeid " &
                                      "WHERE (PLANTA = '" & Unidade & "' AND IDPASTA = '" & Pasta & "' AND HORCHE IS NOT NULL AND HORENT IS NULL AND HORSAI IS NULL) " &
                                        " OR (PLANTA = '" & Unidade & "' AND IDPASTA = '" & Pasta & "' AND HORCHE IS NOT NULL AND HORENT IS NOT NULL AND HORSAI IS NULL)")

        For Each dtrLinha As DataRow In dttNotasFiscais.Rows
            BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.PortariaEntrada, "Registrou a entrada na portaria da " & dtrLinha("VNF_TIPO_DOCUMENTO").ToString() & dtrLinha("NFNUM").ToString(), dtrLinha("NFEID").ToString())
        Next

        BLLog.Insert(BLLog.LogType.Service, String.Empty, BLLog.LogTitle.CaminhaoEntrada, "Caminhão com a pasta " & Pasta & " entrou para descarregar " & intQtdNotasFiscais & " notas fiscais.")
    End Sub

    Public Sub RegistrarSaida(ByVal Unidade As String, ByVal Pasta As String)
        Dim dttNotasFiscais As New DataTable
        dttNotasFiscais = modSQL.Fill("SELECT TBPORT_EF.NFEID, VNF_TIPO_DOCUMENTO, NFNUM, NFSERIE " &
                                      "FROM TBPORT_EF left join tbdoc_cab on TBPORT_EF.nfeid = tbdoc_cab.nfeid " &
                                      "WHERE (PLANTA = '" & Unidade & "' AND IDPASTA = '" & Pasta & "' AND HORCHE IS NOT NULL AND HORENT IS NOT NULL AND HORSAI IS NULL)")

        For Each dtrLinha As DataRow In dttNotasFiscais.Rows
            BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.PortariaSaida, "Registrou a saída na portaria da " & dtrLinha("VNF_TIPO_DOCUMENTO").ToString() & dtrLinha("NFNUM").ToString(), dtrLinha("NFEID").ToString())
        Next

        modSQL.ExecuteNonQuery("update tbPORT_EF set horsai = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' where idpasta = " & Pasta & " and PLANTA = '" & Unidade & "' and horsai is null")
        BLLog.Insert(BLLog.LogType.Service, String.Empty, BLLog.LogTitle.CaminhaoSaida, "Caminhão com a pasta " & Pasta & " registrou a saída na portaria. ")
    End Sub

    Public Function GetRelatorioDivergencias(ByVal arrNfeId As String()) As DataTable
        Dim objBLNotaFiscal As New BLNotaFiscal()
        Dim objNotasFiscaisRelatorio As StringBuilder = New StringBuilder()
        Dim strStatus As String
        Dim strNfeId As String

        For Each item As String In arrNfeId
            strNfeId = item.Replace("'", "")
            If (Not String.IsNullOrEmpty(strNfeId)) Then
                strStatus = objBLNotaFiscal.GetSituacaoDocumento(strNfeId)
                If strStatus = "PENDENTE" Or strStatus = "REJEITADA" Then
                    objNotasFiscaisRelatorio.Append("'" & strNfeId & "',")
                End If
            End If
        Next

        If (String.IsNullOrEmpty(objNotasFiscaisRelatorio.ToString())) Then
            Return New DataTable()
        End If

        Dim dtReport As DataTable = New DataTable()
        dtReport = modSQL.Fill("select * from vwRPT_PORTARIA WHERE CHAVE_ACESSO IN (" & objNotasFiscaisRelatorio.ToString().Remove(objNotasFiscaisRelatorio.ToString().Length - 1) & ")")


        For Each item As String In arrNfeId
            If (Not String.IsNullOrEmpty(item)) Then
                strNfeId = item.Replace("'", "")
                strStatus = objBLNotaFiscal.GetSituacaoDocumento(strNfeId)

                If strStatus = "NÃO ENCONTRADO" Then

                    If dtReport.Columns.Count = 0 Then
                        dtReport.Columns.Add("NFEID")
                        dtReport.Columns.Add("NUMERO")
                        dtReport.Columns.Add("SERIE")
                        dtReport.Columns.Add("DT_EMISSAO")
                        dtReport.Columns.Add("SITUACAO")
                        dtReport.Columns.Add("PEDCOM")
                        dtReport.Columns.Add("ITEPED")
                        dtReport.Columns.Add("CAMPO")
                        dtReport.Columns.Add("VALOR_PED")
                        dtReport.Columns.Add("VALOR_NFE")
                        dtReport.Columns.Add("CODCOM")
                        dtReport.Columns.Add("NOMCOM")
                        dtReport.Columns.Add("RAZFOR")
                        dtReport.Columns.Add("CNPJ")
                    End If


                    dtReport.Rows.Add(strNfeId, strNfeId.Substring(26, 6), strNfeId.Substring(34, 1), "", "Ñ ENCONTRADA", "XML INEXISTENTE", "", "", "XML INEXISTENTE", "XML INEXISTENTE", "", "", "", "")

                End If
            End If
        Next

        Return dtReport
    End Function

    Public Function GetQtdItensBloqueadosEmProcessamento() As Integer
        Dim strQuery As String = "SELECT  " &
                                 "	count(*) " &
                                 "FROM " &
                                 "	TbPORT_EF " &
                                 "	inner join tbnfe " &
                                 "	on TbPORT_EF.nfeid = tbnfe.nfeid " &
                                 "WHERE " &
                                 "	situacao <> 'ACEITA' " &
                                 "	and horsai is null "

        Return Convert.ToInt32(modSQL.ExecuteScalar(strQuery))
    End Function

    Public Function GetQtdItensPrioritariosEmProcessamento() As Integer
        Dim strQuery As String = "SELECT  " &
                                 "	count(*) " &
                                 "FROM " &
                                 "	TbPORT_EF " &
                                 "WHERE " &
                                 "	dbo.sp_check_prioridade_alta_documento(NFEID) = 'SIM' " &
                                 "	and horsai is null "

        Return Convert.ToInt32(modSQL.ExecuteScalar(strQuery))
    End Function


    Public Function PastaParaSaida(ByVal Unidade As String, ByVal Pasta As String) As Boolean
        Dim sql As String =
            "Select IDPASTA FROM TBPORT_EF WHERE PLANTA = '" & Unidade & "' AND IDPASTA = '" & Pasta & "'" &
            "AND HORCHE IS NOT NULL AND HORENT IS NOT NULL AND HORSAI IS NULL"

        If modSQL.ExecuteScalar(sql) <> "" And modSQL.ExecuteScalar("select IDPASTA from tbpasta where planta = '" & Unidade & "'") Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function PastaParaEntrada(ByVal Unidade As String, ByVal Pasta As String) As Boolean
        Dim sql As String =
            "Select IDPASTA FROM TBPORT_EF WHERE PLANTA = '" & Unidade & "' AND IDPASTA = '" & Pasta & "'" &
            "AND HORCHE IS NOT NULL AND HORENT IS NULL AND HORSAI IS NULL"

        If modSQL.ExecuteScalar(sql) <> "" And modSQL.ExecuteScalar("select IDPASTA from tbpasta where planta = '" & Unidade & "'") <> "" Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function PastaParaChegada(ByVal Unidade As String, ByVal Pasta As String, ByVal DataChegada As String) As Boolean
        Dim HoraChegada As DateTime = IIf(String.IsNullOrEmpty(DataChegada), DateTime.Now, DataChegada)
        Dim dttNfsPasta As New DataTable
        dttNfsPasta = modSQL.Fill("SELECT " &
                                  "     IDPASTA, HORCHE, HORENT, DATLAN FROM TBPORT_EF " &
                                  "WHERE " &
                                  "     (PLANTA = '" & Unidade & "' AND IDPASTA = '" & Pasta & "' AND convert(varchar, HORCHE, 120) <> '" & HoraChegada.ToString("yyyy-MM-dd HH:mm:ss") & "' AND HORENT IS NULL AND HORSAI IS NULL) " &
                                  "     OR (PLANTA = '" & Unidade & "' AND IDPASTA = '" & Pasta & "' AND convert(varchar, HORCHE, 120) <> '" & HoraChegada.ToString("yyyy-MM-dd HH:mm:ss") & "' AND HORENT IS NOT NULL AND HORSAI IS NULL)")

        Return dttNfsPasta.Rows.Count = 0
    End Function

End Class
