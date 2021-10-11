' Autor: Marcio Spinosa - 15/06/2018 - CR00008351
' Data : 15/06/2018
' OBS: Ajuste na regra de condição de pagamento para não ocorrer exception e validar data vencimento tanto maior como menor
' e criação da validação de duplicata.
Imports System.Data.SqlClient
Imports System.Xml
Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Net
Imports System.Globalization
Imports System.Configuration
Imports MetsoFramework.SAP
Imports MetsoFramework.Utils
Imports MetsoFramework.Core
Imports System.Text
Imports System.Linq
Imports MetsoFramework.SAP.SAP_RFC
Imports System.Net.Mail
Imports System.Net.Mime.MediaTypeNames




Public Class modVerificar

#Region " Fields "

    Public intTentativasProcessamento As Integer = 0
    Public objNF As New modNF
    Public arrPedidos As List(Of String)
    Dim strModoContingencia As String = "0"
    Dim SEND_TO As String = ""
    Const usuario_sistema = "SISTEMA"
    Const classificacao_remessa_futura = "ENTREGA FUTURA (REMESSA)"
    Const nenhum_email_cadastrado = "nenhum e-mail cadastrado"

    Public Enum TipoProcessamento
        Servico = 1
        Aplicacao = 2
        Ambos = 3
    End Enum

    Dim LogGUID As Guid = Guid.NewGuid
    Public isStopping As Boolean = False
#End Region

#Region " OnTimedEvent "
    Public Sub OnTimedEvent()
        Dim objNotasImportacao As List(Of TriangulusImportacaoViewModel) = New List(Of TriangulusImportacaoViewModel)
        Try
            '---> VERIFICAR SE ETÁ ATIVADO O MODO CONTIGÊNCIA
            strModoContingencia = modSQL.ExecuteScalar("SELECT VALOR FROM TBPAR WHERE PARAMETRO = 'ATIVAR_MODO_CONTINGENCIA'")

            If strModoContingencia = "1" Then
                RegistrarLog(TipoProcessamento.Servico, Nothing, "RUN MODE", "Contingência mode")
                Dim pastaContingencia = modSQL.ExecuteScalar("SELECT VALOR FROM TBPAR WHERE PARAMETRO = 'DIRETORIO_ARQUIVOS_CONTINGENCIA'")
                Dim strMensagem As String = ""

                'PERCORRER TODOS OS ARQUIVOS XML DESSA PASTA
                For Each file As String In Directory.GetFiles(pastaContingencia)
                    If file.ToUpper().EndsWith(".XML") Then
                        Validar(String.Empty, file, True, True, TipoProcessamento.Servico, usuario_sistema, strMensagem)
                    End If
                Next
            Else
                '--> LOG - Inicio de processamento
                RegistrarLog(TipoProcessamento.Servico, Nothing, "RUN MODE", "Normal mode")
                Dim dtNfsTriangulus As DataTable = New DataTable()

                '--> [01] /////////////////////////////////////////////////
                '     --> Busca paramentros do serviço (wraps the code [Ctrl+M, Ctrl+H] / Removes the outlining information for the currently selected user-defined region. Shortcut keys are [CTRL+M] and then [CTRL+U])
                Dim dtUltimaAlteracao As DateTime
                Dim idUltimoLista As Decimal

                GetParameters(dtUltimaAlteracao, idUltimoLista)

                '--> [02] /////////////////////////////////////////////////
                '     --> Busca por até 06 chaves de acesso para reprocessamento
                Dim objChavesAcessoReprocessamento As List(Of String) = GetChavesReprocessamentoList(6)

                '     --> Busca pelas notas para reprocessamento
                Dim objNotasEncontradasReprocessamento As List(Of TriangulusImportacaoViewModel) = GetNotasReprocessamentoList(objChavesAcessoReprocessamento)
                If (objChavesAcessoReprocessamento.Count > 0) Then
                    '     --> Caso existam notas marcadas como reprocessamento mas não existam no Triangulus (algo muito estranho) essas notas serão marcadas como já reprocessadas para serem ignoradas nas próximas execuções
                    SetNaoReprocessarSeNonEcziste(objChavesAcessoReprocessamento, objNotasEncontradasReprocessamento)
                End If

                '--> [03] /////////////////////////////////////////////////
                '     --> Busca por até 08 chaves de acesso de DOCUMENTOS que por falha do serviço antigo FALTARAM IMPORTAR do Triangulus e foram selecionados "manualmente" para forçar importação
                Dim quantity = 8 - objNotasEncontradasReprocessamento.Count
                Dim objChavesDocsFaltandoIntegracao As List(Of VnfDocumentosFaltandoIntegracao) = GetChavesDocsFaltandoIntegracao(quantity)

                '--> [04] /////////////////////////////////////////////////
                '     --> Busca por até 10 notas para importação (retirando a quantidade já reservada para reprocessamento e as importações forçadas)
                quantity = 10 - objNotasEncontradasReprocessamento.Count - objChavesDocsFaltandoIntegracao.Count
                objNotasImportacao = GetNotasImportacao(quantity, dtUltimaAlteracao, idUltimoLista, objChavesDocsFaltandoIntegracao)

                '--> [05] /////////////////////////////////////////////////
                '     --> Merge entre as NotasPendentes e as NotasReprocessamento
                Dim objNotasPendentes As List(Of TriangulusImportacaoViewModel) = New List(Of TriangulusImportacaoViewModel)
                If Not (objNotasImportacao Is Nothing) Then
                    objNotasPendentes = objNotasImportacao.[Select](Function(i) i.Clone()).ToList()
                    For Each item As TriangulusImportacaoViewModel In objNotasEncontradasReprocessamento
                        If Not objNotasPendentes.Any(Function(n) n.CHAVE_ACESSO = item.CHAVE_ACESSO) Then
                            objNotasPendentes.Add(item)
                        End If
                    Next
                Else
                    objNotasPendentes = objNotasEncontradasReprocessamento
                End If


                RegistrarLog(TipoProcessamento.Servico, Nothing, objNotasPendentes.Count.ToString() + " NFs LOCALIZADAS", "INICIANDO PROCESSAMENTO")

                '--> [06] /////////////////////////////////////////////////
                '     --> O PROCESSO SÓ DEVE SER EXECUTADO QUANDO HOUVEREM NOTAS FISCAIS PARA PROCESSAMENTO
                Dim strMensagem As String = String.Empty
                If objNotasPendentes.Count > 0 Then

                    '     --> VALIDAR NOTA FISCAL
                    For Each dtrLinha As TriangulusImportacaoViewModel In objNotasPendentes
                        If isStopping Then
                            Exit For
                        End If

                        strMensagem = ""
                        Dim strChaveAcessoNF As String = dtrLinha.CHAVE_ACESSO
                        If Not String.IsNullOrWhiteSpace(dtrLinha.XML) And dtrLinha.MODO <> TriangulusImportacaoViewModel.ModoType.IGNORAR_IMPORTACAO Then
                            RegistrarLog(TipoProcessamento.Servico, Nothing, "PROCESSANDO DOC-e", "Iniciando processamento da chave de acesso " + strChaveAcessoNF)
                            Validar(strChaveAcessoNF, String.Empty, True, True, TipoProcessamento.Servico, usuario_sistema, strMensagem, False, True, dtrLinha.IGNORE_EMAIL)
                        End If

                        '     --> Se for selecionado pela consulta para importação padrão
                        If dtrLinha.MODO = TriangulusImportacaoViewModel.ModoType.IMPORTACAO Or dtrLinha.MODO = TriangulusImportacaoViewModel.ModoType.IGNORAR_IMPORTACAO Then
                            '     --> Atualiza paramentros do serviço
                            If (dtrLinha.DT_ALT > dtUltimaAlteracao) Then
                                RegistrarLog(TipoProcessamento.Servico, Nothing, "TRACE VNF SERVICE", "Atualizando 'SERVICO.TRIANGULUS_ULTIMA_DT_ALT' para " + dtrLinha.DT_ALT.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                                modSQL.ExecuteNonQuery("UPDATE TBPAR SET VALOR = '" & dtrLinha.DT_ALT.ToString("yyyy-MM-dd HH:mm:ss.fff") & "' WHERE PARAMETRO = 'SERVICO.TRIANGULUS_ULTIMA_DT_ALT'")
                            End If
                            If Not String.IsNullOrWhiteSpace(dtrLinha.ID_DOC) Then
                                Dim idDoc As Decimal = Decimal.Parse(dtrLinha.ID_DOC)
                                RegistrarLog(TipoProcessamento.Servico, Nothing, "TRACE VNF SERVICE", "Atualizando 'SERVICO.TRIANGULUS_ULTIMO_LISTA' para " + idDoc.ToString())
                                modSQL.ExecuteNonQuery("UPDATE TBPAR SET VALOR = '" & idDoc.ToString() & "' WHERE PARAMETRO = 'SERVICO.TRIANGULUS_ULTIMO_LISTA'")
                            End If
                        ElseIf dtrLinha.MODO = TriangulusImportacaoViewModel.ModoType.REPROCESSAMENTO Then
                            '     --> Se for reprocessamento, atualiza a data para que volte ao fim da fila
                            SetDtReprocessamento(strChaveAcessoNF)
                        ElseIf dtrLinha.MODO = TriangulusImportacaoViewModel.ModoType.DOCUMENTO_FALTANTE_IMPORTACAO Then
                            '     --> Se for documento faltante de importação, atualiza a data para que volte ao fim da fila
                            SetDtReprocessamentoDocFaltanteImportacao(strChaveAcessoNF)
                        End If

                        If String.IsNullOrWhiteSpace(strMensagem) Then
                            If dtrLinha.MODO <> TriangulusImportacaoViewModel.ModoType.IGNORAR_IMPORTACAO Then
                                RegistrarLog(TipoProcessamento.Servico, Nothing, "PROCESSANDO DOC-e", "Processamento da chave de acesso " + strChaveAcessoNF + " concluido")
                            Else
                                RegistrarLog(TipoProcessamento.Servico, Nothing, "PROCESSANDO DOC-e", "Processamento da chave de acesso " + strChaveAcessoNF + " IGNORADO")
                            End If
                        Else
                            '     --> Caso ocorra um erro ao durante a IMPORTAÇÃO e caso não exista na fila de DOCUMENTOS FALTANTES IMPORTAÇÃO, insere para que não fique perdida devido a Data de Alteração
                            If dtrLinha.MODO = TriangulusImportacaoViewModel.ModoType.DOCUMENTO_FALTANTE_IMPORTACAO Or
                               dtrLinha.MODO = TriangulusImportacaoViewModel.ModoType.IMPORTACAO Then
                                InsereFilaDocumentoFaltante(strChaveAcessoNF)
                            End If
                            RegistrarLog(TipoProcessamento.Servico, Nothing, "PROCESSANDO DOC-e", "Processamento da chave de acesso " + strChaveAcessoNF + " retornou a mensagem: " + strMensagem)
                        End If
                    Next
                End If

                '     --> Atualiza a data dos REPROCESSAMENTOS para que volte ao fim da fila, caso não tenha sido localizado no Triangulus (filtrado)
                If (objChavesAcessoReprocessamento.Count > 0) Then
                    Dim lstChavesNaoEncontradas As List(Of String) = New List(Of String)
                    If Not (objNotasImportacao Is Nothing) Then
                        lstChavesNaoEncontradas = objChavesAcessoReprocessamento.Where(Function(i) Not objNotasPendentes.[Select](Function(x) x.CHAVE_ACESSO).ToList().Exists(Function(x) x = i)).ToList()
                    Else
                        lstChavesNaoEncontradas = objChavesAcessoReprocessamento.ToList()
                    End If

                    For Each strChaveAcessoNF As String In lstChavesNaoEncontradas
                        SetDtReprocessamento(strChaveAcessoNF)
                    Next
                End If

                '     --> Atualiza a data dos DOCUMENTOS FALTANTES DE IMPORTAÇÃO para que volte ao fim da fila, caso não tenha sido localizado no Triangulus (filtrado)
                If (objChavesDocsFaltandoIntegracao.Count > 0) Then
                    Dim lstChavesNaoEncontradas As List(Of String) = New List(Of String)
                    If Not (objNotasImportacao Is Nothing) Then
                        lstChavesNaoEncontradas = objChavesDocsFaltandoIntegracao.Where(Function(i) Not objNotasImportacao.[Select](Function(x) x.CHAVE_ACESSO).ToList().Exists(Function(x) x = i.CHAVE_ACESSO)).ToList() _
                                                                                 .[Select](Function(x) x.CHAVE_ACESSO).ToList()
                    Else
                        lstChavesNaoEncontradas = objChavesDocsFaltandoIntegracao.[Select](Function(x) x.CHAVE_ACESSO).ToList()
                    End If

                    For Each strChaveAcessoNF As String In lstChavesNaoEncontradas
                        SetDtReprocessamentoDocFaltanteImportacao(strChaveAcessoNF)
                    Next
                End If

                If Not isStopping Then
                    '--> VERIFICAR ASSOCIAÇÃO DO PORTAL
                    VerificarAssociacaoPortal()
                End If
            End If
        Catch ex As Exception
            RegistrarLog(TipoProcessamento.Servico, ex)
        End Try

        Try
            '--> [07] /////////////////////////////////////////////////
            '     --> ATUALIZAR NOTAS FISCAIS CANCELADAS NO TRIANGULUS
            Dim linhasCanceladas = objNotasImportacao.Where(Function(nota As TriangulusImportacaoViewModel) nota.STAT.Trim() = "101" And nota.MODO <> TriangulusImportacaoViewModel.ModoType.IGNORAR_IMPORTACAO).ToList()
            RegistrarLog(TipoProcessamento.Servico, Nothing, "CANCELANDO NOTAS FISCAIS", linhasCanceladas.Count() & " NFs CANCELADAS")
            CancelarNotasFiscais(linhasCanceladas)

            '--> PROCESSO CONCLUÍDO E SERVIÇO LIBERADO PARA NOVA EXECUÇÃO
            modSQL.ExecuteNonQuery("update TbPAR set VALOR = '' where PARAMETRO = 'SITVER'")
            RegistrarLog(TipoProcessamento.Servico, Nothing, "RUN COMPLETED", "The service was completed")
        Catch ex As Exception
            RegistrarLog(TipoProcessamento.Servico, ex)
        End Try
    End Sub

    '--> [01] /////////////////////////////////////////////////
    Private Sub GetParameters(ByRef dtUltimaAlteracao As DateTime, ByRef idUltimoLista As Decimal)
        '--> Busca paramentros do serviço (wraps the code [Ctrl+M, Ctrl+H] / Removes the outlining information for the currently selected user-defined region. Shortcut keys are [CTRL+M] and then [CTRL+U])
        Dim parametroValor As String = ""

        Try
            parametroValor = modSQL.ExecuteScalar("SELECT VALOR FROM TBPAR WHERE PARAMETRO = 'SERVICO.TRIANGULUS_ULTIMA_DT_ALT'")
            dtUltimaAlteracao = DateTime.ParseExact(parametroValor, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
            RegistrarLog(TipoProcessamento.Servico, Nothing, "TRACE VNF SERVICE", "Parametro SERVICO.TRIANGULUS_ULTIMA_DT_ALT: " + dtUltimaAlteracao.ToString("yyyy-MM-dd HH:mm:ss.fff"))
        Catch ex As Exception
            Throw New Exception(String.Format("O valor '{0}' do parametro 'SERVICO.TRIANGULUS_ULTIMA_DT_ALT' está em formato inválido", parametroValor))
        End Try

        Try
            parametroValor = modSQL.ExecuteScalar("SELECT VALOR FROM TBPAR WHERE PARAMETRO = 'SERVICO.TRIANGULUS_ULTIMO_LISTA'")
            idUltimoLista = Decimal.Parse(parametroValor)
            RegistrarLog(TipoProcessamento.Servico, Nothing, "TRACE VNF SERVICE", "Parametro SERVICO.TRIANGULUS_ULTIMO_LISTA: " + idUltimoLista.ToString())
        Catch ex As Exception
            Throw New Exception(String.Format("O valor '{0}' do parametro 'SERVICO.TRIANGULUS_ULTIMO_LISTA' está em formato inválido", parametroValor))
        End Try
    End Sub

    '--> [02] /////////////////////////////////////////////////
    ''' <summary>
    ''' Função para retornar as notas que tem que ser reprocessadas
    ''' </summary>
    ''' <param name="totalRecords"></param>
    ''' <returns></returns>
    ''' <example>Marcio Spinosa - 14/11/2018 - CR00009259 - Ajuste no select que gerava gargalo no serviço devido usar join com 02 views</example>
    Private Function GetChavesReprocessamentoList(ByVal totalRecords As Integer) As List(Of String)
        'Marcio Spinosa - 14/11/2018 - CR00009259
        'Dim query = String.Format("SELECT DISTINCT TOP {0} tbnfe.nfeid AS CHAVE_ACESSO, DTREPROCESSAMENTO " &
        '                          "  FROM TbNFE " &
        '                          "  LEFT OUTER JOIN TbDOC_CAB ON TbDOC_CAB.NFEID = TbNFE.NFEID " &
        '                          "  LEFT OUTER JOIN TbNFE_CAB ON TbNFE_CAB.NFEID = TbNFE.NFEID " &
        '                          "  LEFT OUTER JOIN vwRelatorioSAP f ON f.ChaveAcessoCTE = TbNFE.NFEID collate Finnish_Swedish_CI_AS " &
        '                          "  LEFT OUTER JOIN vwPodeModificarDocumento p ON p.NFEID = TbNFE.NFEID " &
        '                          " WHERE (tbnfe.reprocessar = 'S' " &
        '                          "   AND ISNULL(TbNFE_CAB.NF_TIPO_DOCUMENTO,'') <> 'INVALIDO' " &
        '                          "   AND ISNULL(TbDOC_CAB.vnf_tipo_documento,'') <> 'INVALIDO' " &
        '                          "   AND ISNULL(TipoFrete,'') <> 'Importação' " &
        '                          "   AND ISNULL(PODE_MODIFICAR,1) <> 0 " &
        '                          "   AND (TbDOC_CAB.vnf_tipo_documento = 'NFE' OR TbNFE_CAB.NF_TIPO_DOCUMENTO = 'NFE' " &
        '                          "     OR TbDOC_CAB.VNF_TIPO_DOCUMENTO = 'CTE' OR TbNFE_CAB.NF_TIPO_DOCUMENTO = 'CTE') ) " &
        '                          " ORDER BY DTREPROCESSAMENTO ", totalRecords) ' MARCIO QTDE_REPROCESSAMENTO XXXX  "   AND QTDE_REPROCESSAMENTO <= 4 " &
        Dim pstrConnectionFretes = System.Configuration.ConfigurationSettings.AppSettings("ConnectionStringFretes").ToString()
        Dim pstDataBase = pstrConnectionFretes
        For Each str As String In pstrConnectionFretes.Split(";")
            If (str.Contains("Initial Catalog=")) Then
                pstDataBase = str.Replace("Initial Catalog=", "").ToString()
                Exit For
            End If
        Next

        Dim query = String.Format(" select cac_chave_acesso, cac_tipo_pagamento into #tmp from  ")
        '                                  " OPENDATASOURCE('SQLNCLI', 'Data Source=TCHKIPRA49-56.MDIR.CO\SQLTEST01;user id=FRETES_TEST;password=QA_FRETES;Pooling=false;').DTB_QA_BR_FRETES.dbo.cac_analise_cte " &
        query = query + String.Format(
                                  " OPENDATASOURCE('SQLNCLI', '" + pstrConnectionFretes + "')." + pstDataBase + ".dbo.cac_analise_cte ")
        query = query + String.Format(
                                  " where cac_tipo_pagamento <> 4 " &
                                  " And cac_id_status_cte >= 110 " &
                                  " SELECT DISTINCT TOP {0} tbnfe.nfeid AS CHAVE_ACESSO, DTREPROCESSAMENTO " &
                                  "  FROM TbNFE " &
                                  "  LEFT OUTER JOIN TbDOC_CAB ON TbDOC_CAB.NFEID = TbNFE.NFEID " &
                                  "  LEFT OUTER JOIN TbNFE_CAB ON TbNFE_CAB.NFEID = TbNFE.NFEID " &
                                  "  LEFT OUTER JOIN #TMP ON CAC_CHAVE_ACESSO = TBNFE.NFEID collate Finnish_Swedish_CI_AS   " &
                                  " WHERE (tbnfe.reprocessar = 'S' " &
                                  "   AND ISNULL(TbNFE_CAB.NF_TIPO_DOCUMENTO,'') <> 'INVALIDO' " &
                                  "   AND ISNULL(TbDOC_CAB.vnf_tipo_documento,'') <> 'INVALIDO' " &
                                  "   AND SITUACAO <> 'RECUSADA' and " &
                                  "     SITUACAO <> 'CANCELADA' " &
                                  "   AND SAP_STATUS_INTEGRACAO <> 'CONCLUÍDO' " &
                                  "   AND (TbDOC_CAB.vnf_tipo_documento = 'NFE' OR TbNFE_CAB.NF_TIPO_DOCUMENTO = 'NFE' " &
                                  "     OR TbDOC_CAB.VNF_TIPO_DOCUMENTO = 'CTE' OR TbNFE_CAB.NF_TIPO_DOCUMENTO = 'CTE') ) " &
                                  " ORDER BY DTREPROCESSAMENTO " &
                                  " drop table #tmp ", totalRecords) ' MARCIO QTDE_REPROCESSAMENTO XXXX  "   AND QTDE_REPROCESSAMENTO <= 4 " &
        'Marcio Spinosa - 14/11/2018 - CR00009259 - Fim
        Dim objChavesAcessoReprocessamento As DataTable = modSQL.Fill(query)

        RegistrarLog(TipoProcessamento.Servico, Nothing, "TRACE VNF SERVICE", objChavesAcessoReprocessamento.Rows.Count.ToString() + " nfs marcadas para reprocessamento no VNF serão pesquisadas no Triangulus")
        If (objChavesAcessoReprocessamento.Rows.Count > 0) Then
            Return objChavesAcessoReprocessamento.Rows.OfType(Of DataRow)().ToList().Select(Function(r) r("CHAVE_ACESSO").ToString()).Distinct().ToList()
        Else
            Return New List(Of String)
        End If
    End Function

    '--> [02] /////////////////////////////////////////////////
    ''' <summary>
    ''' Método de coletas das notas para reprocessamento
    ''' </summary>
    ''' <param name="lstChaves"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>'Marcio Spinosa - 10/09/2018 - SR00201608 - CR00009259 - Ajuste para ocorrer erro de importação de notas de homologação em produção</example>
    Private Function GetNotasReprocessamentoList(ByVal lstChaves As List(Of String)) As List(Of TriangulusImportacaoViewModel)
        If (lstChaves.Count > 0) Then
            Dim arrChaves = lstChaves _
                                .[Select](Function(c) "'" & c & "'") _
        .Aggregate(Function(a, b) a + "," + b)
            'Marcio Spinosa - 10/09/2018 - SR00201608 - CR00009259 
            'modSQL.CommandText = String.Format("SELECT CHAVE_ACESSO, XML, ID_DOC, STAT, DT_ALT " &
            '                                    "FROM NFF02_DOC WITH (NOLOCK) " &
            '                                    "WHERE CHAVE_ACESSO IN ({0}) AND XML is not null " &
            '                                    "AND DT_ALT IS NOT NULL " &
            '                                    "AND LEN(STAT) < 4 AND STAT <> 678 ", _
            '                                    arrChaves)
            modSQL.CommandText = String.Format("SELECT CHAVE_ACESSO, XML, ID_DOC, STAT, DT_ALT " &
                                                "FROM NFF02_DOC WITH (NOLOCK) " &
                                                "WHERE CHAVE_ACESSO IN ({0}) AND XML is not null " &
                                                "AND DT_ALT IS NOT NULL " &
                                                "AND LEN(STAT) < 4 AND STAT not in (678, 217) ",
                                                arrChaves)
            'Marcio Spinosa - 10/09/2018 - SR00201608 - CR00009259 - Fim

            Dim objNotas = modSQL.Fill(modSQL.CommandText, modSQL.connectionStringTriangulus)
            Dim lstNotasTriangulus = objNotas.AsEnumerable().[Select](Function(r) New TriangulusImportacaoViewModel() With {
                                                            .CHAVE_ACESSO = r("CHAVE_ACESSO").ToString(),
                                                            .XML = r("XML").ToString(),
                                                            .ID_DOC = r("ID_DOC").ToString(),
                                                            .STAT = r("STAT").ToString(),
                                                            .DT_ALT = CDate(r("DT_ALT")),
                                                            .MODO = TriangulusImportacaoViewModel.ModoType.REPROCESSAMENTO
                                                        }).ToList()

            If (objNotas.Rows.Count > 0) Then
                RegistrarLog(TipoProcessamento.Servico, Nothing, "TRACE VNF SERVICE", lstNotasTriangulus.Count.ToString() + " das nfs marcadas para reprocessamento no VNF foram encontradas no Triangulus")
                Return lstNotasTriangulus
            End If
        End If

        Return New List(Of TriangulusImportacaoViewModel)
    End Function

    '--> [02] /////////////////////////////////////////////////
    Private Sub SetNaoReprocessarSeNonEcziste(ByVal lstChavesVNF As List(Of String), ByVal lstEncontradasTriangulus As List(Of TriangulusImportacaoViewModel))
        Dim objNotasNonExcistentes = lstChavesVNF.Except(lstEncontradasTriangulus.AsEnumerable().[Select](Function(r) r.CHAVE_ACESSO))
        If (objNotasNonExcistentes.Count > 0) Then
            Dim arrNotasNonExcistentes = objNotasNonExcistentes.AsEnumerable().[Select](Function(r) "'" & r & "'").Aggregate(Function(a, b) a + "," + b)
            modSQL.CommandText = String.Format("UPDATE TbNFE " &
                                                "SET reprocessar = 'N' " &
                                                "WHERE nfeid IN ({0}) AND tbnfe.reprocessar = 'S'",
                                                arrNotasNonExcistentes)
            modSQL.ExecuteNonQuery(modSQL.CommandText)

            RegistrarLog(TipoProcessamento.Servico, Nothing, "TRACE VNF SERVICE",
                            objNotasNonExcistentes.Count.ToString() + " nfs marcadas para reprocessamento no VNF mas que não existem no Triangulus foram marcadas como NãoReprocessar: " + arrNotasNonExcistentes)
        End If
    End Sub

    '--> [03] /////////////////////////////////////////////////
    Private Function GetChavesDocsFaltandoIntegracao(ByVal quantity As Integer) As List(Of VnfDocumentosFaltandoIntegracao)
        If (quantity > 0) Then
            Dim query = String.Format("SELECT DISTINCT TOP {0} CHAVE_ACESSO, f.DTREPROCESSAMENTO, f.IGNORE_EMAIL " &
                                      "  FROM TbDocsFaltandoIntegracao f WITH (NOLOCK) " &
                                      "  LEFT OUTER JOIN TbNFE n WITH (NOLOCK) ON n.NFEID = f.CHAVE_ACESSO " &
                                      " WHERE n.NFEID IS NULL " &
                                      " ORDER BY f.DTREPROCESSAMENTO ", quantity)
            Dim objChavesDocsFaltandoIntegracao = modSQL.Fill(query)
            RegistrarLog(TipoProcessamento.Servico, Nothing, "TRACE VNF SERVICE", objChavesDocsFaltandoIntegracao.Rows.Count.ToString() + " nfs selecionadas para forçar importação faltante no VNF serão pesquisadas no Triangulus")

            If (objChavesDocsFaltandoIntegracao.Rows.Count > 0) Then
                Return objChavesDocsFaltandoIntegracao.Rows.OfType(Of DataRow)().AsEnumerable().[Select](Function(r) New VnfDocumentosFaltandoIntegracao() With {
                                                           .CHAVE_ACESSO = r("CHAVE_ACESSO").ToString(),
                                                           .IGNORE_EMAIL = CBool(r("IGNORE_EMAIL"))
                                                       }).Distinct().ToList()
            End If
        End If
        Return New List(Of VnfDocumentosFaltandoIntegracao)
    End Function

    '--> [04] /////////////////////////////////////////////////
    ''' <summary>
    ''' Método para selecionar as notas no triangulus para importação
    ''' </summary>
    ''' <param name="quantity"></param>
    ''' <param name="dtUltimaAlteracao"></param>
    ''' <param name="idUltimoLista"></param>
    ''' <param name="lstChavesDocsFaltandoIntegracao"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>'Marcio Spinosa - 10/09/2018 - SR00201608 - CR00009259 - Ajuste para ocorrer erro de importação de notas de homologação em produção</example>
    Private Function GetNotasImportacao(ByVal quantity As Integer, ByVal dtUltimaAlteracao As DateTime, ByVal idUltimoLista As Integer, ByVal lstChavesDocsFaltandoIntegracao As List(Of VnfDocumentosFaltandoIntegracao)) As List(Of TriangulusImportacaoViewModel)
        If (quantity < 1) Then
            quantity = 0
        End If

        'Marcio Spinosa - 10/09/2018 - SR00201608 - CR00009259
        'Dim query = String.Format("SELECT * FROM (" &
        '                          "    SELECT TOP {0} CHAVE_ACESSO, XML, ID_DOC, STAT, DT_ALT, 'S' AS IMPORTACAO_PADRAO " &
        '                          "    FROM NFF02_DOC WITH (NOLOCK) " &
        '                          "    WHERE CHAVE_ACESSO IS NOT NULL AND XML IS NOT NULL " &
        '                          "    AND ((DT_ALT = CAST('{1}' AS DATETIME) AND ID_DOC > {2}) OR DT_ALT > CAST('{1}' AS DATETIME)) " &
        '                          "    AND DT_ALT IS NOT NULL " &
        '                          "    AND LEN(STAT) < 4 AND STAT <> 678 " &
        '                          "    ORDER BY DT_ALT, ID_DOC) b ", _
        '                          quantity, dtUltimaAlteracao.ToString("yyyy-MM-dd HH:mm:ss.fff"), idUltimoLista)
        Dim query = String.Format("SELECT * FROM (" &
                                  "    SELECT TOP {0} CHAVE_ACESSO, XML, ID_DOC, STAT, DT_ALT, 'S' AS IMPORTACAO_PADRAO " &
                                  "    FROM NFF02_DOC WITH (NOLOCK) " &
                                  "    WHERE CHAVE_ACESSO IS NOT NULL AND XML IS NOT NULL " &
                                  "    AND ((DT_ALT = CAST('{1}' AS DATETIME) AND ID_DOC > {2}) OR DT_ALT > CAST('{1}' AS DATETIME)) " &
                                  "    AND DT_ALT IS NOT NULL " &
                                  "    AND LEN(STAT) < 4 AND STAT not in(678, 217) " &
                                  "    ORDER BY DT_ALT, ID_DOC) b ",
                                  quantity, dtUltimaAlteracao.ToString("yyyy-MM-dd HH:mm:ss.fff"), idUltimoLista)

        If (lstChavesDocsFaltandoIntegracao.Count > 0) Then
            'query = query &
            '        " UNION ALL " &
            '        String.Format("SELECT CHAVE_ACESSO, XML, ID_DOC, STAT, DT_ALT, '' AS IMPORTACAO_PADRAO " &
            '                "FROM NFF02_DOC WITH (NOLOCK) " &
            '                "WHERE CHAVE_ACESSO IS NOT NULL AND XML IS NOT NULL " &
            '                "AND ( CHAVE_ACESSO IN ({0}) ) " &
            '                "AND DT_ALT IS NOT NULL " &
            '                "AND LEN(STAT) < 4 AND STAT <> 678 ", _
            '                lstChavesDocsFaltandoIntegracao.[Select](Function(r) "'" & r.CHAVE_ACESSO & "'").Aggregate(Function(a, b) a + "," + b))
            query = query &
                    " UNION ALL " &
            String.Format("SELECT CHAVE_ACESSO, XML, ID_DOC, STAT, DT_ALT, '' AS IMPORTACAO_PADRAO " &
                            "FROM NFF02_DOC WITH (NOLOCK) " &
                            "WHERE CHAVE_ACESSO IS NOT NULL AND XML IS NOT NULL " &
                            "AND ( CHAVE_ACESSO IN ({0}) ) " &
                            "AND DT_ALT IS NOT NULL " &
                            "AND LEN(STAT) < 4 AND STAT not in(217, 678) ",
                            lstChavesDocsFaltandoIntegracao.[Select](Function(r) "'" & r.CHAVE_ACESSO & "'").Aggregate(Function(a, b) a + "," + b))
        End If
        'Marcio Spinosa - 10/09/2018 - SR00201608 - CR00009259 - Fim
        modSQL.CommandText = query
        Dim objNotasImportacao = modSQL.Fill(modSQL.CommandText, modSQL.connectionStringTriangulus)
        RegistrarLog(TipoProcessamento.Servico, Nothing, "TRACE VNF SERVICE", objNotasImportacao.Rows.Count.ToString() + " nfs novas ou atualizadas foram selecionadas no Triangulus")

        Dim result As List(Of TriangulusImportacaoViewModel) = objNotasImportacao.AsEnumerable().[Select](Function(r) New TriangulusImportacaoViewModel() With {
                                                            .CHAVE_ACESSO = r("CHAVE_ACESSO").ToString(),
                                                            .XML = r("XML").ToString(),
                                                            .ID_DOC = r("ID_DOC").ToString(),
                                                            .STAT = r("STAT").ToString(),
                                                            .DT_ALT = CDate(r("DT_ALT")),
                                                            .MODO = If(r("IMPORTACAO_PADRAO").ToString() = "S",
                                                                            TriangulusImportacaoViewModel.ModoType.IMPORTACAO,
                                                                            TriangulusImportacaoViewModel.ModoType.DOCUMENTO_FALTANTE_IMPORTACAO)
                                                        }).ToList()

        result.ForEach(Sub(n) n.IGNORE_EMAIL = If(lstChavesDocsFaltandoIntegracao.Where(Function(x) x.CHAVE_ACESSO = n.CHAVE_ACESSO).Select(Function(r) CType(r.IGNORE_EMAIL, Boolean?)).FirstOrDefault(), False))

        ' Verifica se as notas selecionadas no Triangulus para novas importações ou atualização são realmente novas ou atualizadas para o VNF
        If (result.Count > 0) Then
            Dim arrImportacao = result.AsEnumerable().[Select](Function(r) "'" & r.CHAVE_ACESSO & "'").Aggregate(Function(a, b) a + "," + b)
            query = String.Format("SELECT c.NFEID, ISNULL(NF_OUTROS_STATUS_CODE, '') NF_OUTROS_STATUS_CODE FROM TbDOC_CAB_NFE c INNER JOIN TbNfe n ON n.NFEID = c.NFEID WHERE c.NFEID IN ({0})", arrImportacao)

            Dim objNotasVNF = modSQL.Fill(query)
            If (objNotasVNF.Rows.Count > 0) Then
                For Each item As TriangulusImportacaoViewModel In result
                    Dim exists = objNotasVNF.AsEnumerable().Where(Function(r) r("NFEID").ToString() = item.CHAVE_ACESSO And r("NF_OUTROS_STATUS_CODE").ToString() = item.STAT).FirstOrDefault()
                    If Not (exists Is Nothing) Then
                        item.MODO = TriangulusImportacaoViewModel.ModoType.IGNORAR_IMPORTACAO
                    End If
                Next
            End If
        End If

        Return result
    End Function

    '--> [06] /////////////////////////////////////////////////
    Private Sub SetDtReprocessamento(ByVal chaveAcesso As String)
        If Not (String.IsNullOrWhiteSpace(chaveAcesso)) Then
            modSQL.CommandText = String.Format("UPDATE TbNFE " &
                                                "SET DTREPROCESSAMENTO = GetDate() " &
                                                "WHERE nfeid = '{0}'", chaveAcesso)
            modSQL.ExecuteNonQuery(modSQL.CommandText)
        End If
    End Sub

    '--> [06] /////////////////////////////////////////////////
    Private Sub SetDtReprocessamentoDocFaltanteImportacao(ByVal chaveAcesso As String)
        If Not (String.IsNullOrWhiteSpace(chaveAcesso)) Then
            modSQL.CommandText = String.Format("UPDATE TbDocsFaltandoIntegracao " &
                                                "SET DTREPROCESSAMENTO = GetDate() " &
                                                "WHERE CHAVE_ACESSO = '{0}'", chaveAcesso)
            modSQL.ExecuteNonQuery(modSQL.CommandText)
        End If
    End Sub

    '--> [08] /////////////////////////////////////////////////
    Private Sub InsereFilaDocumentoFaltante(ByVal chaveAcesso As String)
        If Not (String.IsNullOrWhiteSpace(chaveAcesso)) Then
            Dim query = String.Format(" SELECT n.NFEID FROM TbNfe n WHERE n.NFEID = '{0}' " &
                                      " UNION ALL " &
                                      " SELECT f.CHAVE_ACESSO FROM TbDocsFaltandoIntegracao f WHERE f.CHAVE_ACESSO = '{1}' " _
                                      , chaveAcesso, chaveAcesso)

            Dim objChavesEncontradas = modSQL.Fill(query)

            If (objChavesEncontradas.Rows.Count = 0) Then
                '     --> Insere com a Data "D + 2" para ter menor prioridade
                modSQL.CommandText = String.Format("INSERT INTO TbDocsFaltandoIntegracao(CHAVE_ACESSO, DTREPROCESSAMENTO, IGNORE_EMAIL) VALUES ('{0}', GETDATE() + 2, 0)", chaveAcesso)
                modSQL.ExecuteNonQuery(modSQL.CommandText)
            End If
        End If
    End Sub

    '--> [07] /////////////////////////////////////////////////
    Private Sub CancelarNotasFiscais(ByVal lstCanceladas As List(Of TriangulusImportacaoViewModel))
        Dim objBLNotaFiscal As New BLNotaFiscal()
        Dim strSituacaoNF As String
        For Each dtrLinha In lstCanceladas
            strSituacaoNF = objBLNotaFiscal.GetSituacaoDocumento(dtrLinha.CHAVE_ACESSO)
            If (Not strSituacaoNF Is Nothing And strSituacaoNF <> "CANCELADA") Then
                RegistrarLog(TipoProcessamento.Servico, Nothing, "CANCELANDO DOC-e", dtrLinha.CHAVE_ACESSO)
                objBLNotaFiscal.Cancelar(dtrLinha.CHAVE_ACESSO, String.Empty, dtrLinha.IGNORE_EMAIL)
            End If
        Next
    End Sub

#End Region

#Region " Public Methods "

#Region " Validar "
    Public Function Validar(ByVal CHAVE_ACESSO As String, ByVal pFileName As String, ByVal IsValidarNF As Boolean, ByVal IsConsultarPedido As String, ByVal pTipoProcessamento As TipoProcessamento, ByVal Usuario As String, ByRef Mensagem As String,
                            Optional ByVal isSomenteLeitura As Boolean = False, Optional ByVal isSempreImportaXml As Boolean = False, Optional ByVal IgnoreEmail As Boolean = False) As modNF
        Try

            Dim vObjNotaFiscal As BLNotaFiscal = New BLNotaFiscal()
            Dim vBlnHasException As Boolean
            objNF = New modNF()
            objNF.VNF_CHAVE_ACESSO = CHAVE_ACESSO
            objNF.IGNORE_EMAIL = IgnoreEmail

            '--> É Somente Leitura se a situacao for Cancelada

            If Not vObjNotaFiscal.PodeModificar(CHAVE_ACESSO) Then
                isSomenteLeitura = True
            End If

            '--> CARREGA AS INFORMAÇÕES DO XML
            Dim strResult = LerXml(pFileName, pTipoProcessamento, Usuario, IsValidarNF, isSomenteLeitura, isSempreImportaXml, vBlnHasException)
            Dim strRetorno = If(strResult.IndexOf("|") > 0, strResult.Substring(0, strResult.IndexOf("|")), strResult)

            '--> BUSCA INFORMAÇÕES DE NOTAS REFERENCIADAS CASO ESSA NOTA JÁ EXISTA NO VNF E POSSUA ESSAS INFORMAÇÕES SALVAS
            '    CASO NÃO TENHA SIDO PREENCHIDA MAS ESTEJA VINCULADA NO XML, RETORNA COM BASE NO XML
            modSQL.CommandText = " SELECT * FROM (                                                                            " &
                                 "   SELECT NF_NFREF_REFNNF                                                                   " &
                                 "         ,NF_NFREF_REFSerie                                                                 " &
                                 "         ,NF_NFREF_REFDHEMI                                                                 " &
                                 " 		,null AS NF_IDE_NNF, null AS NF_IDE_SERIE, null AS NF_IDE_DHEMI, null AS NF_EMIT_CNPJ " &
                                 " 		,NFEID                                                                                " &
                                 "     FROM TbDOC_CAB_NFE                                                                     " &
                                 "    WHERE NF_NFREF_REFDHEMI IS NOT NULL                                                     " &
                                 "      AND NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'                                          " &
                                 "    UNION ALL                                                                               " &
                                 "   SELECT null AS NF_NFREF_REFNNF                                                           " &
                                 "         ,null AS NF_NFREF_REFSerie                                                         " &
                                 "         ,null AS NF_NFREF_REFDHEMI                                                         " &
                                 "         ,NF_IDE_NNF, NF_IDE_SERIE, NF_IDE_DHEMI, NF_EMIT_CNPJ                              " &
                                 " 		,c.NFEID                                                                              " &
                                 "      FROM tbdoc_cab_nfe c                                                                  " &
                                 " 	INNER JOIN TbDOC_CAB_NFE_REF r                                                            " &
                                 " 	   ON r.NF_NFREF_REFNFE = c.NFEID                                                         " &
                                 " 	WHERE r.NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'                                          " &
                                 " ) a ORDER BY NF_NFREF_REFNNF DESC                                                          "

            Dim dt As DataTable = modSQL.Fill(modSQL.CommandText)
            If dt.Rows.Count > 0 Then
                Dim REFNNF As String = dt.Rows(0)("NF_NFREF_REFNNF").ToString
                Dim REFSerie As String = dt.Rows(0)("NF_NFREF_REFSerie").ToString
                Dim REFDHEMI As String = dt.Rows(0)("NF_NFREF_REFDHEMI").ToString
                Dim REFs As List(Of DataRow) = dt.Select("[NF_IDE_DHEMI] is not null").ToList

                If Not (String.IsNullOrWhiteSpace(REFNNF) Or String.IsNullOrWhiteSpace(REFSerie) Or String.IsNullOrWhiteSpace(REFDHEMI)) Then
                    objNF.NF_NFREF_REFNNF = REFNNF
                    objNF.NF_NFREF_REFSerie = REFSerie
                    objNF.NF_NFREF_REFDHEMI = CDate(REFDHEMI)
                ElseIf (REFs.Count = 1) Then
                    objNF.NF_NFREF_REFNNF = REFs(0)("NF_IDE_NNF").ToString
                    objNF.NF_NFREF_REFSerie = REFs(0)("NF_IDE_SERIE").ToString
                    objNF.NF_NFREF_REFDHEMI = CDate(REFs(0)("NF_IDE_DHEMI").ToString)
                End If
            End If

            '*****************************************************
            'Verifica se ocorreu erro na chamada da função LerXML
            '*****************************************************
            '--> SE A NOTA FOR RELEVANTE, COMPARA COM AS INFORMAÇÕES DO PEDIDO SAP
            If vBlnHasException Then
                Mensagem = "Ocorreu uma falha na leitura do XML: " + If(strResult.IndexOf("|") > 0, strResult.Substring(strResult.IndexOf("|") + 1), strResult)
                Throw New Exception
            ElseIf strRetorno = "RELEVANTE" Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_NFS Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_TLC Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_FAT Or (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario And IsConsultarPedido) Or (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte And objNF.VNF_CLASSIFICACAO = modNF.tipo_cte_debito_posterior And IsConsultarPedido) Then
                If Not isSomenteLeitura Then
                    If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_NFS Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_TLC Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_FAT) Then
                        Call VerificarNFS(Usuario, Mensagem, IsValidarNF, IsConsultarPedido)
                    Else
                        Call VerificarNF(Usuario, Mensagem, IsValidarNF, IsConsultarPedido)
                    End If
                End If
            ElseIf strRetorno = "VAZIO" Then
                objNF.VNF_STATUS = "NÃO ENCONTRADO"
                Return objNF
                'Marcio Spinosa - 29/05/2019 - CR00009165
                '            ElseIf strRetorno = "NÃO RELEVANTE" And ((objNF.NF_IDE_MOD = "55" And objNF.NF_IDE_FINNFE <> "2") Or (objNF.NF_IDE_MOD = "55" And objNF.NF_IDE_FINNFE = "2" And objNF.NF_ICMSTOT_VPROD = 0) Or objNF.NF_IDE_MOD = "57") Then
            ElseIf strRetorno = "NÃO RELEVANTE" Or ((objNF.NF_IDE_MOD = "55" And objNF.NF_IDE_FINNFE = "2" And objNF.NF_ICMSTOT_VPROD = 0) Or objNF.NF_IDE_MOD = "57") Then
                'Marcio Spinosa - 29/05/2019 - CR00009165 - Fim
                objNF.VNF_NOTA_MANUAL_J1B1N = True
                'Verifica se o valor do produto primeri item da nota é zero, caso seja o NF Type deverá ser E3 caso contrário deverá ser E2
                Dim PrimeiroItemNFe = objNF.ITENS_NF.Min(Function(x) x.NF_PROD_ITEM)
                Dim ValorProdutoItem1 = objNF.ITENS_NF.Where(Function(x) x.NF_PROD_ITEM = PrimeiroItemNFe).Select(Function(x) x.NF_PROD_VPROD).FirstOrDefault()

                If ValorProdutoItem1 = 0 Then
                    objNF.VNF_J1B1N_NFTYPE = "E3"
                Else
                    objNF.VNF_J1B1N_NFTYPE = "Z2"
                End If
            ElseIf (objNF.NF_IDE_MOD = "55" And objNF.NF_IDE_FINNFE = "2" And objNF.NF_ICMSTOT_VPROD > 0) Then
                If Not isSomenteLeitura Then
                    If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_NFS Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_TLC Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_FAT) Then
                        Call VerificarNFS(Usuario, Mensagem, IsValidarNF, IsConsultarPedido)
                    Else
                        Call VerificarNF(Usuario, Mensagem, IsValidarNF, IsConsultarPedido)
                    End If
                End If
            End If

            If Not isSomenteLeitura Then
                '--> APÓS FINALIZAR O PROCESSO, ARMAZENA TODAS AS INFORMAÇÕES DA NF e DO PEDIDO SAP
                If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_NFS Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_TLC Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_FAT) Then
                    GravarDadosNFS(Nothing, IsValidarNF, Usuario)
                Else
                    GravarDadosNF(Nothing, IsValidarNF, Usuario)
                End If

            End If
            objNF.VNF_STATUS = ObterStatusDocumento()

        Catch ex As Exception
            If Not isSomenteLeitura Then
                'RegistrarNotaProblema(CHAVE_ACESSO, ex.Message, "Validar") 'marcio xxxx
                ApagarDocumentoProcessado(CHAVE_ACESSO)
                RegistrarLog(pTipoProcessamento, ex)
            End If
            Mensagem = ex.Message
        End Try

        Return objNF
    End Function
#End Region

#Region " LerXml "
    Public Function LerXml(ByVal pFileName As String, ByVal pTipoProcessamento As TipoProcessamento, ByVal Usuario As String, ByVal IsValidarNF As Boolean,
                           ByVal isSomenteLeitura As Boolean, ByVal isSempreImportaXml As Boolean, Optional ByRef pObjHasException As Boolean = False) As String
        Try
            '--> BUSCA OS DOC-e E CRIA O ARQUIVO FISICAMENTE
            If String.IsNullOrEmpty(pFileName) Then

                '--> VERIFICA SE A NOTA É DO TIPO TALONARIO
                modSQL.CommandText = "SELECT IdTalonario FROM Talonario WHERE ChaveAcesso = '" & objNF.VNF_CHAVE_ACESSO & "'"
                Dim DtTalonario As DataTable = modSQL.Fill(modSQL.CommandText)
                If DtTalonario.Rows.Count > 0 Then
                    CarregarTalonario()

                    For Each objItemNF In objNF.ITENS_NF
                        DeterminarModoProcesso(objItemNF, Usuario, isSomenteLeitura)
                    Next

                    Return ("NÃO RELEVANTE")
                End If

                '--> VERIFICA SE A NOTA É DO TIPO NFS
                modSQL.CommandText = "SELECT IdImportacaoNotaFiscal FROM tbImportacaoNotaFiscal WHERE ChaveAcesso = '" & objNF.VNF_CHAVE_ACESSO & "'"
                Dim DtNFS As DataTable = modSQL.Fill(modSQL.CommandText)
                If DtNFS.Rows.Count > 0 Then
                    CarregarNFS()

                    For Each objItemNF In objNF.ITENS_NF
                        DeterminarModoProcesso(objItemNF, Usuario, isSomenteLeitura)
                    Next

                    Return ("NÃO RELEVANTE")
                End If

                '--> PRIMEIRO TENTA BUSCAR NO VNF (NF JÁ FOI PROCESSADA)
                modSQL.CommandText = "SELECT VNF_CONTEUDO_XML FROM TbDOC_CAB WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                Dim XML_STRING As String = modSQL.ExecuteScalar(modSQL.CommandText)

                '--> SE NÃO ENCONTRAR, BUSCA A NF NO TRIANGULUS
                If String.IsNullOrEmpty(XML_STRING) Or isSempreImportaXml Then
                    modSQL.CommandText = "SELECT top 1 XML FROM NFF02_DOC WITH (NOLOCK) where CHAVE_ACESSO = '" & objNF.VNF_CHAVE_ACESSO & "'"
                    XML_STRING = modSQL.ExecuteScalar(modSQL.CommandText, modSQL.connectionStringTriangulus)
                End If

                If (pTipoProcessamento = TipoProcessamento.Servico) Then
                    modSQL.CommandText = "select VALOR from TbPAR where PARAMETRO = 'PASTA_TEMPORARIA_XML_DO_SERVICO'"
                ElseIf (pTipoProcessamento = TipoProcessamento.Aplicacao) Then
                    modSQL.CommandText = "select VALOR from TbPAR where PARAMETRO = 'PASTA_TEMPORARIA_XML_DA_APLICACAO'"
                End If

                pFileName = modSQL.ExecuteScalar(modSQL.CommandText) & "\XML.XML"

                '#If (DEBUG) Then
                If (Debugger.IsAttached) Then
                    modSQL.CommandText = "select VALOR from TbPAR where PARAMETRO = 'PASTA_TEMPORARIA_XML_DO_USUARIO'"
                    pFileName = modSQL.ExecuteScalar(modSQL.CommandText) & "\XML.XML"
                    'pFileName = "C:\Temp\XML.XML"
                End If
                '#End If

                If File.Exists(pFileName) Then
                        File.Delete(pFileName)
                    End If

                    IO.File.WriteAllText(pFileName, XML_STRING)

                    Dim objFileInfo As New FileInfo(pFileName)
                    If objFileInfo.Length = 0 Then
                        Return "VAZIO"
                    End If
                End If

                '--> CARREGA O XML PARA MEMÓRIA
                Dim objXml As New XmlDocument
            objXml.Load(pFileName)

            '--> POR PADRÃO, ASSUME QUE O DOCUMENTO É INVÁLIDO. PARA MUDAR ESTE STATUS, É NECESSÁRIO QUE EXISTA A DEFINIÇÃO SE É NFe OU CTe
            objNF.VNF_CONTEUDO_XML = objXml.InnerXml.Replace("'", "")
            objNF.VNF_TIPO_DOCUMENTO = "INVALIDO"
            objNF.VNF_DANFE_ONLINE = "<html><head> " & Environment.NewLine &
                                     "<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">" & Environment.NewLine &
                                     "</head><body>" & Environment.NewLine &
                                     "<form action=""http://arvixe.webdanfe.com.br/danfe/GeraDanfe.php"" name=""one"" enctype=""multipart/form-data"" method=""post"">" & Environment.NewLine &
                                     "<textarea name=""arquivoXml"" cols=""150"" rows=""50"" style=""visibility:hidden"" >" & objNF.VNF_CONTEUDO_XML & Environment.NewLine &
                                     "</textarea></form><script>document.one.submit();</script></body></html>"

            Dim objRatear As Object = modSQL.ExecuteScalar("SELECT VNF_RATEAR FROM TbDOC_CAB WHERE nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'")
            If (objRatear Is Nothing OrElse String.IsNullOrEmpty(objRatear)) Then
                objNF.VNF_RATEAR = True
            Else
                objNF.VNF_RATEAR = objRatear.ToString().ToBoolean()
            End If

            '--> RECUPERA O FLAG DE MATERIAL RECEBIDO CASO O PROCESSO JÁ TENHA OCORRIDO PARA EVITAR A PERDA DA INFORMAÇÃO DURANTE O PROCESSO DE ASSOCIAR ONDE
            ' OS DADOS SÃO APAGADOS
            Dim objFlagMaterialRecebido As Object = modSQL.ExecuteScalar("SELECT VNF_MATERIAL_RECEBIDO FROM TbDOC_CAB WHERE nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'")
            If (objFlagMaterialRecebido Is Nothing OrElse String.IsNullOrEmpty(objFlagMaterialRecebido)) Then
                objNF.VNF_MATERIAL_RECEBIDO = False
            Else
                objNF.VNF_MATERIAL_RECEBIDO = objFlagMaterialRecebido.ToString().ToBoolean()
            End If



            Call LerCabecalhoXml(objXml, objNF)

            Dim objClassificacao
            If objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte Then
                objClassificacao = modSQL.ExecuteScalar("SELECT TipoFrete FROM vwRelatorioSAP WHERE ChaveAcessoCTE = '" & objNF.VNF_CHAVE_ACESSO & "'", modSQL.connectionStringFretes)
                If (objClassificacao <> Nothing And Not String.IsNullOrEmpty(objClassificacao)) Then
                    objNF.VNF_CLASSIFICACAO = objClassificacao.ToUpper()
                End If
            End If

            Call LerItemXml(objXml, objNF, isSomenteLeitura)

            If (objNF.VNF_TIPO_DOCUMENTO = "INVALIDO") Then
                Return "ERRO|Tipo de documento no VNF é INVALIDO, o xml pode não estar no formato esperado"

            ElseIf (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte And (objNF.VNF_CLASSIFICACAO = modNF.tipo_cte_debito_posterior Or objNF.VNF_CLASSIFICACAO = modNF.tipo_cte_exportacao Or objNF.VNF_CLASSIFICACAO = modNF.tipo_cte_importacao)) Then
                If Not isSomenteLeitura Then
                    modSQL.CommandText = "select count(*) from TbNFE where nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'"
                    If modSQL.ExecuteScalar(modSQL.CommandText) = 0 Then
                        modSQL.CommandText = "select ISNULL(ID_DOC, 0) from NFF02_DOC WITH (NOLOCK) where CHAVE_ACESSO = '" & objNF.VNF_CHAVE_ACESSO & "'"
                        Dim ID_LISTA As String = modSQL.ExecuteScalar(modSQL.CommandText, modSQL.connectionStringTriangulus)
                        If ID_LISTA Is Nothing Then
                            ID_LISTA = 0
                        End If
                        modSQL.CommandText = "insert into TbNFE values ('" & objNF.VNF_CHAVE_ACESSO & "', 'S', 'N', " & ID_LISTA & ", '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', 'N', '', 'N', 'ACEITA', 'N', '" & objNF.NF_DEST_CNPJ.Trim & "', '', null, 'PENDENTE', null)"
                    Else
                        modSQL.CommandText = "update TbNFE set SITUACAO = 'ACEITA', NFEVAL = 'S', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', NFEREL = 'N', REPROCESSAR = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                    End If

                    modSQL.ExecuteNonQuery(modSQL.CommandText)
                End If

                '--> DEFINIR O MODO / PROCESSO DO DOCUMENTO
                For Each objItemNF In objNF.ITENS_NF
                    DeterminarModoProcesso(objItemNF, Usuario, isSomenteLeitura)
                Next

                Return "NÃO RELEVANTE"
                ''---> MATEUS: NOVA VERIFICAÇÃO DE CTE INTEGRADO AO SISTEMA DE FRETES
                'modVerificarCTE.VerificarStatusCTe(objNF, strModoContingencia)
                'Return ("NÃO RELEVANTE")
            ElseIf objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte And objNF.NF_IDE_FINNFE <> 2 Then
                If Not isSomenteLeitura Then
                    '--> VERIFICA SE A NOTA ESTÁ NA TABELA TBNFE
                    modSQL.CommandText = "SELECT isnull(count(*),0) FROM TbNFE where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                    If modSQL.ExecuteScalar(modSQL.CommandText) = "0" Then
                        modSQL.CommandText = "select ISNULL(ID_DOC, 0) from NFF02_DOC WITH (NOLOCK) where CHAVE_ACESSO = '" & objNF.VNF_CHAVE_ACESSO & "'"
                        Dim ID_LISTA As String = modSQL.ExecuteScalar(modSQL.CommandText, modSQL.connectionStringTriangulus)
                        If ID_LISTA Is Nothing Then
                            ID_LISTA = 0
                        End If

                        modSQL.CommandText = "insert into TbNFE values ('" & objNF.VNF_CHAVE_ACESSO & "', 'N', 'N', " & ID_LISTA & ", null, '', '', 'N', 'PENDENTE', 'N', " & IIf(String.IsNullOrEmpty(objNF.NF_DEST_CNPJ), "NULL", objNF.NF_DEST_CNPJ).ToString().Trim() & ", '" & strModoContingencia & "', null, 'PENDENTE', null)"
                        modSQL.ExecuteNonQuery(modSQL.CommandText)
                    Else
                        modSQL.ExecuteNonQuery("update TbNFE set REPROCESSAR = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'")
                    End If
                End If

                '--> VERIFICA SE A NOTA ESTÁ NA TABELA TBJUN
                modSQL.CommandText = "SELECT isnull(count(*),0) FROM TbJUN where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                If modSQL.ExecuteScalar(modSQL.CommandText) = "0" Then
                    If Not isSomenteLeitura Then
                        For Each objItemNF In objNF.ITENS_NF
                            Dim strQuery As String = "INSERT INTO TbJUN " &
                                                     "( " &
                                                     "	 PEDCOM " &
                                                     "	,ITEPED " &
                                                     "	,NFEID " &
                                                     "	,ITENFE " &
                                                     "	,ITEVAL " &
                                                     "	,CODUSU " &
                                                     "	,CODCOM " &
                                                     "	,MOTIVO " &
                                                     "	,NCMNFE " &
                                                     "	,NCMPED " &
                                                     ") " &
                                                     "VALUES " &
                                                     "( " &
                                                     "	 @PEDCOM " &
                                                     "	,@ITEPED " &
                                                     "	,@NFEID " &
                                                     "	,@ITENFE " &
                                                     "	,@ITEVAL " &
                                                     "	,@CODUSU " &
                                                     "	,@CODCOM " &
                                                     "	,@MOTIVO " &
                                                     "	,@NCMNFE " &
                                                     "	,@NCMPED " &
                                                     "); SELECT @@identity "

                            Dim sqlparams As New List(Of SqlClient.SqlParameter)
                            sqlparams.Add(modSQL.AddSqlParameter("PEDCOM", SqlDbType.VarChar, objItemNF.NF_PROD_XPED.RemoveLetters()))
                            sqlparams.Add(modSQL.AddSqlParameter("ITEPED", SqlDbType.Int, objItemNF.NF_PROD_NITEMPED))
                            sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, objNF.VNF_CHAVE_ACESSO))
                            sqlparams.Add(modSQL.AddSqlParameter("ITENFE", SqlDbType.Int, objItemNF.NF_PROD_ITEM))
                            sqlparams.Add(modSQL.AddSqlParameter("ITEVAL", SqlDbType.VarChar, "N"))
                            sqlparams.Add(modSQL.AddSqlParameter("CODUSU", SqlDbType.VarChar, Usuario))
                            sqlparams.Add(modSQL.AddSqlParameter("CODCOM", SqlDbType.VarChar, objNF.SAP_DETAILS.PURCHASING_GROUP))
                            sqlparams.Add(modSQL.AddSqlParameter("MOTIVO", SqlDbType.VarChar, String.Empty))
                            sqlparams.Add(modSQL.AddSqlParameter("NCMNFE", SqlDbType.VarChar, objItemNF.NF_PROD_NCM))
                            sqlparams.Add(modSQL.AddSqlParameter("NCMPED", SqlDbType.VarChar, objItemNF.SAP_ITEM_DETAILS.NCM_CODE))
                            objItemNF.VNF_CODJUN = Convert.ToDecimal(modSQL.ExecuteScalarParams(strQuery, sqlparams))
                        Next
                    End If
                Else
                    modSQL.CommandText = "select ITENFE, PEDCOM, ITEPED, CODJUN from TbJUN where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' order by ITENFE"
                    Dim dttDadosPedido As DataTable = modSQL.Fill(modSQL.CommandText)

                    For Each dtrLinha As DataRow In dttDadosPedido.Rows
                        For indexItemNF As Integer = 0 To objNF.ITENS_NF.Count() - 1

                            Dim intItemNfe As Integer = IIf(String.IsNullOrEmpty(dtrLinha("ITENFE").ToString), 0, Convert.ToInt32(dtrLinha("ITENFE").ToString))
                            Dim strPedido As String = dtrLinha("PEDCOM").ToString
                            Dim intItemPedido As Integer = 0
                            If Not String.IsNullOrEmpty(dtrLinha("ITEPED").ToString) Then
                                intItemPedido = Convert.ToInt32(dtrLinha("ITEPED").ToString)
                            End If

                            If objNF.ITENS_NF(indexItemNF).NF_PROD_ITEM = intItemNfe Or (intItemNfe = 0 And objNF.ITENS_NF(indexItemNF).NF_PROD_XPED = strPedido And objNF.ITENS_NF(indexItemNF).NF_PROD_NITEMPED = intItemPedido) Then

                                '--> VERIFICA SE O ITEM POSSUI INFORMAÇÕES DO PEDIDO E ITEM PEDIDO DEFINIDOS NO SISTEMA
                                objNF.ITENS_NF(indexItemNF).VNF_CODJUN = dtrLinha("CODJUN").ToString

                                '--> BUSCA O PEDIDO INFORMADO NO SISTEMA, SE NÃO ENCONTRAR, UTILIZA DA NOTA FISCAL
                                If Not String.IsNullOrEmpty(dtrLinha("PEDCOM").ToString) AndAlso dtrLinha("PEDCOM").ToString <> "0" Then
                                    objNF.ITENS_NF(indexItemNF).NF_PROD_XPED = dtrLinha("PEDCOM").ToString
                                End If

                                '--> BUSCA O ITEM DO PEDIDO INFORMADO NO SISTEMA, SE NÃO ENCONTRAR, UTILIZA DA NOTA FISCAL
                                If Not String.IsNullOrEmpty(dtrLinha("ITEPED").ToString) AndAlso dtrLinha("ITEPED").ToString <> "0" Then
                                    objNF.ITENS_NF(indexItemNF).NF_PROD_NITEMPED = dtrLinha("ITEPED").ToString
                                End If
                            End If
                        Next
                    Next
                End If
            End If

            '--> DEFINIR O MODO / PROCESSO DO DOCUMENTO
            For Each objItemNF In objNF.ITENS_NF
                If objNF.NF_IDE_FINNFE = 2 Then
                    DeterminarModoProcesso(objItemNF, Usuario, isSomenteLeitura, True)
                Else
                    DeterminarModoProcesso(objItemNF, Usuario, isSomenteLeitura)
                End If
            Next

            If NaoRelevante() Then
                If Not isSomenteLeitura Then
                    modSQL.CommandText = "update TbNFE set SITUACAO = 'ACEITA', NFEVAL = 'S', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', NFEREL = 'N', REPROCESSAR = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                    modSQL.ExecuteNonQuery(modSQL.CommandText)

                    If (IsValidarNF) Then
                        Dim cmdText As String = "update TbLOG set SITUACAO = 'INATIVO', MOTIVO = 'NF IRRELEVANTE' where SITUACAO = 'ATIVO' and NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                        modSQL.ExecuteNonQuery(cmdText)

                        If Not objNF.IGNORE_EMAIL And (Not objNF.VNF_TIPO_DOCUMENTO = "NFS" Or Not objNF.VNF_TIPO_DOCUMENTO = "FAT" Or Not objNF.VNF_TIPO_DOCUMENTO = "TLC") Then
                            Call EnviarMensagemParaFornecedor(objNF.VNF_CHAVE_ACESSO, "ACEITA")
                        End If
                    End If
                End If
                Return "NÃO RELEVANTE"
            End If

            Return "RELEVANTE"
        Catch ex As Exception
            pObjHasException = True
            If Not isSomenteLeitura Then
                RegistrarLog(pTipoProcessamento, ex)
            End If
            Return "ERRO|" + ex.Message
        End Try
    End Function

    Public Function ParseModNF(ByVal pFileName As String) As modNF
        Dim _objNf = New modNF()

        Dim objXml As New XmlDocument
        objXml.Load(pFileName)

        '--> POR PADRÃO, ASSUME QUE O DOCUMENTO É INVÁLIDO. PARA MUDAR ESTE STATUS, É NECESSÁRIO QUE EXISTA A DEFINIÇÃO SE É NFe OU CTe
        _objNf.VNF_CONTEUDO_XML = objXml.InnerXml.Replace("'", "")
        _objNf.VNF_TIPO_DOCUMENTO = "INVALIDO"
        _objNf.VNF_DANFE_ONLINE = "<html><head> " & Environment.NewLine &
                                 "<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">" & Environment.NewLine &
                                 "</head><body>" & Environment.NewLine &
                                 "<form action=""http://arvixe.webdanfe.com.br/danfe/GeraDanfe.php"" name=""one"" enctype=""multipart/form-data"" method=""post"">" & Environment.NewLine &
                                 "<textarea name=""arquivoXml"" cols=""150"" rows=""50"" style=""visibility:hidden"" >" & _objNf.VNF_CONTEUDO_XML & Environment.NewLine &
                                 "</textarea></form><script>document.one.submit();</script></body></html>"

        Dim objRatear As Object = modSQL.ExecuteScalar("SELECT VNF_RATEAR FROM TbDOC_CAB WHERE nfeid = '" & _objNf.VNF_CHAVE_ACESSO & "'")
        If (objRatear Is Nothing OrElse String.IsNullOrEmpty(objRatear)) Then
            _objNf.VNF_RATEAR = True
        Else
            _objNf.VNF_RATEAR = objRatear.ToString().ToBoolean()
        End If

        Call LerCabecalhoXml(objXml, _objNf)
        Call LerItemXml(objXml, _objNf)

        Dim objClassificacao
        If _objNf.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte Then
            objClassificacao = modSQL.ExecuteScalar("SELECT TipoFrete FROM vwRelatorioSAP WHERE ChaveAcessoCTE = '" & _objNf.VNF_CHAVE_ACESSO & "'", modSQL.connectionStringFretes)
            If (objClassificacao <> Nothing And Not String.IsNullOrEmpty(objClassificacao)) Then
                _objNf.VNF_CLASSIFICACAO = objClassificacao.ToUpper()
            End If
        End If

        Return _objNf
    End Function
#End Region

#Region " LerSAPNFS "
    Public Sub LerSAPNFS(ByRef pNF As modNF, ByVal pItemNF As modNFItem, ByVal Usuario As String, ByRef Mensagem As String)
        Dim strNfeId As String = ""
        Try
            '--> SE ALGUM DOCUMENTO ESTIVER EM PROCESSAMENTO, O SISTEMA DEVE AGUARDAR A CONCLUSÃO
            Dim datRequisicao As DateTime = DateTime.Now
            strNfeId = objNF.VNF_CHAVE_ACESSO
            modSQL.ExecuteNonQuery("INSERT INTO TbIntegracaoPostagens (ipo_nfeid, ipo_usuario, ipo_data_inclusao) VALUES ('" & strNfeId & "', '" & Usuario & "', '" & datRequisicao.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Dim intIdPostagem As Integer = modSQL.ExecuteScalar("SELECT id_integracao_postagem FROM TbIntegracaoPostagens WHERE ipo_nfeid = '" & strNfeId & "'").ToString().ToInt()
            'Do
            '    Threading.Thread.Sleep(2000)
            'Loop While modSQL.ExecuteScalar("SELECT count(*) FROM TbIntegracaoPostagens WHERE ipo_data_conclusao is null and id_integracao_postagem < " & intIdPostagem).ToString().ToInt() > 0

            Dim strCnpjNf As String = ""
            strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()

            Dim objFilter As New SAP_RFC.PurchaseOrderFilter()
            objFilter.PurchaseOrderNumber = pItemNF.NF_PROD_XPED
            objFilter.PurchaseOrderItemNumber = pItemNF.NF_PROD_NITEMPED
            objFilter.NFCreationDate = pNF.NF_IDE_DHEMI
            objFilter.NFItemQuantity = pItemNF.NF_PROD_QCOM
            objFilter.NFItemValue = pItemNF.NF_PROD_VPROD
            objFilter.NFModel = objNF.NF_IDE_MOD
            objFilter.NFCategory = pItemNF.MDP_TIPO_NF
            objFilter.NFCnpjMetso = strCnpjNf
            objFilter.NFFreightAmount = pItemNF.NF_PROD_VFRETE
            objFilter.NFInsuranceAmount = pItemNF.NF_PROD_VSEG
            objFilter.NFDiscountAmount = pItemNF.NF_PROD_VDESC
            objFilter.NFOtherExpensesAmount = pItemNF.NF_PROD_VOUTRO
            objFilter.CTStateOfOrigin = pNF.NF_EMIT_UF
            objFilter.CTStateOfFinalDestination = pNF.NF_DEST_UF
            objFilter.MovementType = pItemNF.MDP_TIPO_MOVIMENTO_MIGO
            objFilter.NFCfop = pItemNF.NF_PROD_CFOP.RemoveLetters()
            objFilter.GetRequesterInfo = False

            objFilter.ItemTaxes = New List(Of SAP_RFC.PurchaseOrderItemsTaxes)
            objFilter.GoodsMovementTypes = New List(Of SAP_RFC.GoodsMovementType)
            objFilter.DeliveryNotes = New List(Of SAP_RFC.PurchaseOrderDeliveryNoteSearchFilter)
            objFilter.ComponentsHeader = New List(Of SAP_RFC.PurchaseOrderSearchFilterHeaderComponents)
            objFilter.ComponentListCollection = New List(Of SAP_RFC.PurchaseOrderComponentList)

            CarregarTiposMovimento(objFilter)

            Dim objRfcReturn As New SAP_RFC.RfcReturn
            Dim objPurchaseOrder As SAP_RFC.PurchaseOrder
            objPurchaseOrder = SAP_RFC.getPurchaseOrder(objFilter, objRfcReturn)

            If (Not objRfcReturn.Success) Then
                Dim strMessage As String = ""
                For Each objItem In objRfcReturn.BapiMessage
                    strMessage = "- " & objItem.MESSAGE
                Next

                If (Not objRfcReturn.Exception Is Nothing) Then
                    modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SAP_EXCEPTION', 'PEDIDO: " & pItemNF.NF_PROD_XPED &
                                           " - CONNECTION: " & modSAP.RfcConStr() & "', '" & objRfcReturn.Exception.Message.Replace("'", "") &
                                           " - " & objRfcReturn.Exception.StackTrace.Replace("'", "") & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
                End If
                Mensagem = "Ocorreu uma falha na consulta. " & strMessage
                modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
                Return
            ElseIf (String.IsNullOrEmpty(objPurchaseOrder.PO_NUMBER) AndAlso (objNF.VNF_TIPO_DOCUMENTO <> modNF.tipo_doc_cte OrElse objNF.VNF_CLASSIFICACAO <> modNF.tipo_cte_debito_posterior)) Then
                Mensagem = "Pedido não foi encontrado no SAP"
                modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
                Return
            End If

            '--> PREENCHE AS INFORMAÇÕES DO CABEÇALHO / ITEN
            pNF.SAP_DETAILS = objPurchaseOrder
            pItemNF.SAP_PO_NUMBER = objPurchaseOrder.PO_NUMBER.ToString
            '     pItemNF.VNF_CODJUN = Convert.ToInt32(modSQL.ExecuteScalar("select codjun from tbjun where nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'")) ' marcio wwwwww
            pItemNF.SAP_ITEM_DETAILS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault()
            pItemNF.SAP_ITEM_DETAILS.CNPJ_VENDOR = pNF.SAP_DETAILS.VENDOR_CNPJ 'Marcio Spinosa - 19/09/2018 - SR00221755
            pItemNF.VNF_INBOUND = GetInboundDeliveries(pItemNF)

            '--> SE FOR UM FORNECEDOR NOVO, CADASTRA NO VNF
            modSQL.CommandText = "select count(*) from TbFOR where CODFOR = '" & pNF.SAP_DETAILS.VENDOR_CODE & "'"
            If modSQL.ExecuteScalar(modSQL.CommandText) = "0" Then
                modSQL.CommandText = "insert into TbFOR(CODFOR, RAZFOR, CNPJ) values ('" & pNF.SAP_DETAILS.VENDOR_CODE & "', '" & pNF.NF_EMIT_XNOME & "', '" & pNF.NF_EMIT_CNPJ & "' )"
                modSQL.ExecuteNonQuery(modSQL.CommandText)
            End If

        Catch ex As Exception
            RegistrarLog(Nothing, ex)
            Throw New Exception("Ocorreu um erro ao consultar o pedido no SAP")
        Finally
            modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
        End Try
    End Sub
#End Region

#Region " VerificarNFS "
    Public Sub VerificarNFS(ByVal Usuario As String, ByRef Mensagem As String, ByVal IsValidarNF As String, ByVal IsConsultarPedido As String)
        Try
            '--> ATUALIZA A NOTA FISCAL PARA RELEVANTE, E MARCA PARA NÃO REPROCESSAR A NOTA
            modSQL.CommandText = "update TbNFE set NFEREL = 'S', REPROCESSAR = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
            modSQL.ExecuteNonQuery(modSQL.CommandText)

            Dim AllItensAssociated = True
            For indItemNF As Integer = 0 To objNF.ITENS_NF.Count() - 1
                If objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "X" Then
                    modSQL.CommandText = "update TbJUN set ITEVAL = 'X' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' and ITENFE = " & objNF.ITENS_NF(indItemNF).NF_PROD_ITEM
                    modSQL.ExecuteNonQuery(modSQL.CommandText)
                ElseIf IsConsultarPedido Then
                    '--> INATIVA AS DIVERGÊNCIAS DA NOTA FISCAL
                    modSQL.CommandText = "update TbLOG set SITUACAO = 'INATIVO', MOTIVO = 'ERRO ASSOCIACAO' " &
                                             " where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' and ITENFE = " & objNF.ITENS_NF(indItemNF).NF_PROD_ITEM &
                                             " and (PEDCOM <> '" & objNF.ITENS_NF(indItemNF).NF_PROD_XPED & "' or ITEPED <> " & objNF.ITENS_NF(indItemNF).NF_PROD_NITEMPED & ")"

                    modSQL.ExecuteNonQuery(modSQL.CommandText)

                    '--> BUSCAR AS INFORMAÇÕES DO SAP. OBS: SE MUDAR O PEDIDO OU ITEM DENTRO DA NOTA FISCAL, BUSCA NOVAMENTE
                    LerSAPNFS(objNF, objNF.ITENS_NF(indItemNF), Usuario, Mensagem)

                    '--> SE LOCALIZOU O PEDIDO E ITEM DO PEDIDO, O ITEM É ATUALIZADO PARA O STATUS VÁLIDO
                    If Not String.IsNullOrEmpty(objNF.ITENS_NF(indItemNF).SAP_PO_NUMBER) And Not String.IsNullOrEmpty(objNF.ITENS_NF(indItemNF).SAP_ITEM_DETAILS.ITEM_NUMBER) Then
                        objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "S"
                        modSQL.CommandText = "update TbJUN set ITEVAL = 'S' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' and ITENFE = " & objNF.ITENS_NF(indItemNF).NF_PROD_ITEM
                        modSQL.ExecuteNonQuery(modSQL.CommandText)
                    Else
                        objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "N"
                        modSQL.CommandText = "update TbJUN set ITEVAL = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' and ITENFE = " & objNF.ITENS_NF(indItemNF).NF_PROD_ITEM
                        modSQL.ExecuteNonQuery(modSQL.CommandText)
                        AllItensAssociated = False
                    End If
                End If
            Next

            '--> SE ALGUM ITEM NÃO FOI ASSOCIADO, A NOTA FISCAL RECEBE O STATUS PENDENTE E É ENVIADA UMA MENSAGEM PARA O FORNECEDOR
            'If IsValidarNF AndAlso Not AllItensAssociated Then
            '    modSQL.CommandText = "update TbNFE set SITUACAO = 'PENDENTE', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
            '    modSQL.ExecuteNonQuery(modSQL.CommandText)
            '    EnviarMensagemParaFornecedor(objNF.VNF_CHAVE_ACESSO, "PENDENTE")
            '    Exit Sub
            'End If

            '--> FAZ A COMPARAÇÃO DA NOTA FISCAL COM AS INFORMAÇÕES DO PEDIDO DO SAP
            Dim strStatus = ObterStatusDocumento()
            Dim strIntegracao = modSQL.ExecuteScalar("SELECT STATUS_INTEGRACAO FROM vwStatusIntegracao WHERE nfeid = '" + objNF.VNF_CHAVE_ACESSO + "'")
            If IsValidarNF AndAlso strIntegracao <> "CONCLUÍDO" AndAlso (strStatus <> "CANCELADA" And strStatus <> "RECUSADA") Then
                CompararPedidoSAP(Usuario)
            End If
        Catch ex As Exception
            RegistrarLog(Nothing, ex)
            Mensagem = ex.Message
        End Try
    End Sub
#End Region

#Region " VerificarNF "
    Public Sub VerificarNF(ByVal Usuario As String, ByRef Mensagem As String, ByVal IsValidarNF As String, ByVal IsConsultarPedido As String)
        Dim vBlnIsSubContratacao As Boolean = False
        Try
            If IsConsultarPedido And objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte And objNF.VNF_CLASSIFICACAO = modNF.tipo_cte_debito_posterior Then
                LerSAP(objNF, objNF.ITENS_NF.FirstOrDefault(), Usuario, Mensagem)
            Else
                '--> ATUALIZA A NOTA FISCAL PARA RELEVANTE, E MARCA PARA NÃO REPROCESSAR A NOTA
                modSQL.CommandText = "update TbNFE set NFEREL = 'S', REPROCESSAR = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                modSQL.ExecuteNonQuery(modSQL.CommandText)

                Dim AllItensAssociated = True
                For indItemNF As Integer = 0 To objNF.ITENS_NF.Count() - 1

                    If (objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "X" And Not vBlnIsSubContratacao) Then
                        modSQL.CommandText = "update TbJUN set ITEVAL = 'X' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' and ITENFE = " & objNF.ITENS_NF(indItemNF).NF_PROD_ITEM
                        modSQL.ExecuteNonQuery(modSQL.CommandText)
                    ElseIf IsConsultarPedido Or objNF.ITENS_NF(indItemNF).VNF_IS_SUBCONTRATACAO Then
                        '--> INATIVA AS DIVERGÊNCIAS DA NOTA FISCAL
                        modSQL.CommandText = "update TbLOG set SITUACAO = 'INATIVO', MOTIVO = 'ERRO ASSOCIACAO' " &
                                             " where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' and ITENFE = " & objNF.ITENS_NF(indItemNF).NF_PROD_ITEM &
                                             " and (PEDCOM <> '" & objNF.ITENS_NF(indItemNF).NF_PROD_XPED & "' or ITEPED <> " & objNF.ITENS_NF(indItemNF).NF_PROD_NITEMPED & ")"

                        modSQL.ExecuteNonQuery(modSQL.CommandText)

                        '--> BUSCAR AS INFORMAÇÕES DO SAP. OBS: SE MUDAR O PEDIDO OU ITEM DENTRO DA NOTA FISCAL, BUSCA NOVAMENTE
                        LerSAP(objNF, objNF.ITENS_NF(indItemNF), Usuario, Mensagem)

                        Dim ValidateDevolucao As String = modSQL.ExecuteScalar("select valor from tbpar where parametro = 'CFOP_DEVOLUCAO'") 'Marcio Spinosa - 16/09/2019 - SR00282181
                        '--> SE LOCALIZOU O PEDIDO E ITEM DO PEDIDO, O ITEM É ATUALIZADO PARA O STATUS VÁLIDO
                        'If Not String.IsNullOrEmpty(objNF.ITENS_NF(indItemNF).SAP_PO_NUMBER) And Not String.IsNullOrEmpty(objNF.ITENS_NF(indItemNF).SAP_ITEM_DETAILS.ITEM_NUMBER) Then
                        If Not String.IsNullOrEmpty(objNF.ITENS_NF(indItemNF).SAP_PO_NUMBER) And Not String.IsNullOrEmpty(objNF.ITENS_NF(indItemNF).SAP_ITEM_DETAILS.ITEM_NUMBER) Then 'Marcio Spinosa - 16/09/2019 - SR00282181
                            objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "S"
                            modSQL.CommandText = "update TbJUN set ITEVAL = 'S' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' and ITENFE = " & objNF.ITENS_NF(indItemNF).NF_PROD_ITEM
                            modSQL.ExecuteNonQuery(modSQL.CommandText)


                        Else
                            objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "N"
                            modSQL.CommandText = "update TbJUN set ITEVAL = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' and ITENFE = " & objNF.ITENS_NF(indItemNF).NF_PROD_ITEM
                            modSQL.ExecuteNonQuery(modSQL.CommandText)
                            AllItensAssociated = False
                        End If
                    End If
                Next

                '--> SE ALGUM ITEM NÃO FOI ASSOCIADO, A NOTA FISCAL RECEBE O STATUS PENDENTE E É ENVIADA UMA MENSAGEM PARA O FORNECEDOR
                If IsValidarNF AndAlso Not AllItensAssociated Then
                    modSQL.CommandText = "update TbNFE set SITUACAO = 'PENDENTE', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                    modSQL.ExecuteNonQuery(modSQL.CommandText)
                    If Not objNF.IGNORE_EMAIL And (Not objNF.VNF_TIPO_DOCUMENTO = "NFS" Or Not objNF.VNF_TIPO_DOCUMENTO = "FAT" Or Not objNF.VNF_TIPO_DOCUMENTO = "TLC") Then
                        EnviarMensagemParaFornecedor(objNF.VNF_CHAVE_ACESSO, "PENDENTE")
                    End If
                    Exit Sub
                End If

                '--> Conforme acordado com o Pedro, o sistema não agrupa serviços e materiais utilizados no beneficiamento da peça
                '--> VERIFICAR A RELEVÂNCIA DOS ITENS (insumos + serviços) 
                'VerificarRelevanciaItens()

                '--> FAZ A COMPARAÇÃO DA NOTA FISCAL COM AS INFORMAÇÕES DO PEDIDO DO SAP
                Dim strStatus = ObterStatusDocumento()
                Dim strIntegracao = modSQL.ExecuteScalar("SELECT STATUS_INTEGRACAO FROM vwStatusIntegracao WHERE nfeid = '" + objNF.VNF_CHAVE_ACESSO + "'")
                If IsValidarNF AndAlso strIntegracao <> "CONCLUÍDO" AndAlso (strStatus <> "CANCELADA" And strStatus <> "RECUSADA") Then
                    CompararPedidoSAP(Usuario)
                End If
            End If
        Catch ex As Exception
            'RegistrarNotaProblema(objNF.VNF_CHAVE_ACESSO, ex.Message, "VerificaNF") 'marcio xxxx
            RegistrarLog(Nothing, ex)
            Mensagem = ex.Message
        End Try
    End Sub
#End Region

#Region " LerSAP "
    Public Sub LerSAP(ByRef pNF As modNF, ByVal pItemNF As modNFItem, ByVal Usuario As String, ByRef Mensagem As String)
        Dim strNfeId As String = ""
        Dim objRetorno As Object

        Try
            If (objNF.VNF_TIPO_DOCUMENTO <> modNF.tipo_doc_cte OrElse objNF.VNF_CLASSIFICACAO <> modNF.tipo_cte_debito_posterior) Then
                If String.IsNullOrEmpty(pItemNF.NF_PROD_XPED) Or pItemNF.NF_PROD_NITEMPED = 0 Then
                    Return
                End If
            End If

            '--> SE ALGUM DOCUMENTO ESTIVER EM PROCESSAMENTO, O SISTEMA DEVE AGUARDAR A CONCLUSÃO.
            Dim datRequisicao As DateTime = DateTime.Now
            strNfeId = objNF.VNF_CHAVE_ACESSO
            modSQL.ExecuteNonQuery("INSERT INTO TbIntegracaoPostagens (ipo_nfeid, ipo_usuario, ipo_data_inclusao) VALUES ('" & strNfeId & "', '" & Usuario & "', '" & datRequisicao.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Dim intIdPostagem As Integer = modSQL.ExecuteScalar("SELECT id_integracao_postagem FROM TbIntegracaoPostagens WHERE ipo_nfeid = '" & strNfeId & "'").ToString().ToInt()
            'Do
            '    Threading.Thread.Sleep(2000)
            'Loop While modSQL.ExecuteScalar("SELECT count(*) FROM TbIntegracaoPostagens WHERE ipo_data_conclusao is null and id_integracao_postagem < " & intIdPostagem).ToString().ToInt() > 0

            Dim strCnpjNf As String = ""
            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) Then
                strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
            Else
                If (objNF.CT_IDE_TOMA = "0") Then
                    strCnpjNf = objNF.NF_REM_CNPJ.ToString().RemoveLetters()
                ElseIf objNF.CT_IDE_TOMA = "1" Then
                    strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
                ElseIf objNF.CT_IDE_TOMA = "2" Then
                    strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
                ElseIf objNF.CT_IDE_TOMA = "3" Then
                    strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
                ElseIf objNF.CT_IDE_TOMA = "4" Then
                    strCnpjNf = objNF.CT_TOMA_CNPJ.ToString().RemoveLetters()
                End If
            End If

            Dim objFilter As New SAP_RFC.PurchaseOrderFilter()
            objFilter.PurchaseOrderNumber = pItemNF.NF_PROD_XPED
            objFilter.PurchaseOrderItemNumber = pItemNF.NF_PROD_NITEMPED
            objFilter.NFCreationDate = pNF.NF_IDE_DHEMI
            objFilter.NFItemQuantity = pItemNF.NF_PROD_QCOM
            objFilter.NFItemValue = pItemNF.NF_PROD_VPROD
            objFilter.NFModel = objNF.NF_IDE_MOD
            objFilter.NFCategory = pItemNF.MDP_TIPO_NF
            objFilter.NFCnpjMetso = strCnpjNf
            objFilter.NFFreightAmount = pItemNF.NF_PROD_VFRETE
            objFilter.NFInsuranceAmount = pItemNF.NF_PROD_VSEG
            objFilter.NFDiscountAmount = pItemNF.NF_PROD_VDESC

            If IsFaturaEntregaFutOUConsignacao(pItemNF.NF_PROD_CFOP, "ID_MODO_PROCESSO_FATURA_CONSIGNACAO", objNF.NF_IDE_FINNFE) Then
                objFilter.NFOtherExpensesAmount = 0
            Else
                objFilter.NFOtherExpensesAmount = pItemNF.NF_PROD_VOUTRO
            End If

            objFilter.CTStateOfOrigin = pNF.NF_EMIT_UF
            objFilter.CTStateOfFinalDestination = pNF.NF_DEST_UF
            objFilter.MovementType = pItemNF.MDP_TIPO_MOVIMENTO_MIGO
            objFilter.NFCfop = pItemNF.NF_PROD_CFOP.RemoveLetters()
            objFilter.GetRequesterInfo = False

            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte AndAlso objNF.VNF_CLASSIFICACAO = modNF.tipo_cte_debito_posterior) Then
                'ToDo: Adicionar trava quando não houver vendor code definido e quando não encontrar company code. Adicionar também tabela para determinar o tax code a nível de header
                Dim objBLNotaFiscal As New BLNotaFiscal()
                objFilter.VendorCnpj = objNF.NF_EMIT_CNPJ
                objFilter.VendorCode = objBLNotaFiscal.GetVendorCodeFretes(objNF.VNF_CHAVE_ACESSO, objNF.NF_EMIT_CNPJ)
                objFilter.TaxCode = IIf(objNF.ITENS_NF.FirstOrDefault().NF_ICMS_VICMS > 0, "F2", "F1")
                objFilter.CompanyCode = modSQL.ExecuteScalar("SELECT top 1 company_code FROM TbPlantaCnpj WHERE cnpj = '" & objNF.CT_TOMA_CNPJ & "'").ToString()
                objFilter.Plant = modSQL.ExecuteScalar("SELECT top 1 planta FROM TbPlantaCnpj WHERE cnpj = '" & objNF.CT_TOMA_CNPJ & "'").ToString()
            End If

            objFilter.ItemTaxes = New List(Of SAP_RFC.PurchaseOrderItemsTaxes)
            objFilter.GoodsMovementTypes = New List(Of SAP_RFC.GoodsMovementType)
            objFilter.DeliveryNotes = New List(Of SAP_RFC.PurchaseOrderDeliveryNoteSearchFilter)
            objFilter.ComponentsHeader = New List(Of SAP_RFC.PurchaseOrderSearchFilterHeaderComponents)
            objFilter.ComponentListCollection = New List(Of SAP_RFC.PurchaseOrderComponentList)

            Dim objIcms As New SAP_RFC.PurchaseOrderItemsTaxes()
            objIcms.TAX_NAME = "ICMS"
            objIcms.TAX_BASE_CALCULATION_AMOUNT = pItemNF.NF_ICMS_VBC
            objIcms.TAX_AMOUNT = pItemNF.NF_ICMS_VICMS
            objIcms.TAX_PERCENTAGE = pItemNF.NF_ICMS_PICMS
            objIcms.OTHER_TAX_BASE_CALCULATION_AMUNT = 0
            objIcms.OTHER_EXCLUDED_TAX_BASE_CALCULATION_AMOUNT = 0
            objIcms.TAX_PERCENTAGE_REDUCTION = 0
            objFilter.ItemTaxes.Add(objIcms)

            Dim objPis As New SAP_RFC.PurchaseOrderItemsTaxes()
            objPis.TAX_NAME = "PIS"
            objPis.TAX_BASE_CALCULATION_AMOUNT = pItemNF.NF_PIS_VBC
            objPis.TAX_AMOUNT = pItemNF.NF_PIS_VPIS
            objPis.TAX_PERCENTAGE = pItemNF.NF_PIS_PPIS
            objPis.OTHER_TAX_BASE_CALCULATION_AMUNT = 0
            objPis.OTHER_EXCLUDED_TAX_BASE_CALCULATION_AMOUNT = 0
            objPis.TAX_PERCENTAGE_REDUCTION = 0
            objFilter.ItemTaxes.Add(objPis)

            Dim objCofins As New SAP_RFC.PurchaseOrderItemsTaxes()
            objCofins.TAX_NAME = "COFINS"
            objCofins.TAX_BASE_CALCULATION_AMOUNT = pItemNF.NF_COFINS_VBC
            objCofins.TAX_AMOUNT = pItemNF.NF_COFINS_VCOFINS
            objCofins.TAX_PERCENTAGE = pItemNF.NF_COFINS_PCOFINS
            objCofins.OTHER_TAX_BASE_CALCULATION_AMUNT = 0
            objCofins.OTHER_EXCLUDED_TAX_BASE_CALCULATION_AMOUNT = 0
            objCofins.TAX_PERCENTAGE_REDUCTION = 0
            objFilter.ItemTaxes.Add(objCofins)

            Dim objIpi As New SAP_RFC.PurchaseOrderItemsTaxes()
            objIpi.TAX_NAME = "IPI"
            objIpi.TAX_BASE_CALCULATION_AMOUNT = pItemNF.NF_IPI_VBC
            objIpi.TAX_AMOUNT = pItemNF.NF_IPI_VIPI
            objIpi.TAX_PERCENTAGE = pItemNF.NF_IPI_PIPI
            objIpi.OTHER_TAX_BASE_CALCULATION_AMUNT = 0
            objIpi.OTHER_EXCLUDED_TAX_BASE_CALCULATION_AMOUNT = 0
            objIpi.TAX_PERCENTAGE_REDUCTION = 0
            objFilter.ItemTaxes.Add(objIpi)

            CarregarTiposMovimento(objFilter)

            If IsFaturaEntregaFutOUConsignacao(pItemNF.NF_PROD_CFOP, "ID_MODO_PROCESSO_FATURA_CONSIGNACAO", objNF.NF_IDE_FINNFE) Then
                CarregarFiltroDeliveryNotes(objFilter, pNF.VNF_CHAVE_ACESSO, pItemNF.NF_PROD_ITEM, pNF.NF_EMIT_CNPJ, 55)
            End If

            ''Verifica se é uma subcontratação, caso seja irá carregar os dados para busca dos componentes
            If pItemNF.VNF_IS_SUBCONTRATACAO Then
                CarregarListaComponentes(objFilter, pNF.VNF_CHAVE_ACESSO, pItemNF.NF_PROD_ITEM, pNF.NF_EMIT_CNPJ, 55)
            End If

            'Verifica se é uma nota complementar
            If objNF.NF_IDE_FINNFE = 2 Then
                CarregarFiltroNotaComplementar(objFilter, pNF.VNF_CHAVE_ACESSO, pItemNF.NF_PROD_ITEM, pNF.NF_EMIT_CNPJ, 55)
            End If

            Dim objRfcReturn As New SAP_RFC.RfcReturn
            Dim objPurchaseOrder As SAP_RFC.PurchaseOrder
            objPurchaseOrder = SAP_RFC.getPurchaseOrder(objFilter, objRfcReturn)


            If (Not objRfcReturn.Success) Then
                Dim strMessage As String = ""
                If Not objRfcReturn.BapiMessage Is Nothing Then
                    For Each objItem In objRfcReturn.BapiMessage
                        strMessage = "- " & objItem.MESSAGE
                    Next
                Else
                    strMessage = objRfcReturn.Message
                End If

                If (Not objRfcReturn.Exception Is Nothing) Then
                    modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('SAP_EXCEPTION', 'PEDIDO: " & pItemNF.NF_PROD_XPED &
                                           " - CONNECTION: " & modSAP.RfcConStr() & "', '" & objRfcReturn.Exception.Message.Replace("'", "") &
                                           " - " & objRfcReturn.Exception.StackTrace.Replace("'", "") & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
                End If
                Mensagem = "Ocorreu uma falha na consulta. " & strMessage
                modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
                Return
            ElseIf (String.IsNullOrEmpty(objPurchaseOrder.PO_NUMBER) AndAlso (objNF.VNF_TIPO_DOCUMENTO <> modNF.tipo_doc_cte OrElse objNF.VNF_CLASSIFICACAO <> modNF.tipo_cte_debito_posterior)) Then
                Mensagem = "O Pedido/Item " & objFilter.PurchaseOrderNumber & "/" & objFilter.PurchaseOrderItemNumber & " não foi encontrado no SAP. Item da NF:" & pItemNF.NF_PROD_ITEM
                modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
                Return
            End If

            '--> PREENCHE AS INFORMAÇÕES DO CABEÇALHO / ITENS
            pNF.SAP_DETAILS = objPurchaseOrder

            If pNF.SAP_PO_HEADER_COLLECTION.Where(Function(x) x.PO_NUMBER = objPurchaseOrder.PO_NUMBER).ToArray().Count() = 0 Then
                pNF.SAP_PO_HEADER_COLLECTION.Add(objPurchaseOrder)
            End If

            pItemNF.SAP_PO_NUMBER = objPurchaseOrder.PO_NUMBER.ToString
            pItemNF.SAP_ITEM_DETAILS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault()
            pItemNF.SAP_ITEM_DETAILS.CNPJ_VENDOR = pNF.SAP_DETAILS.VENDOR_CNPJ 'Marcio Spinosa - 19/09/2018 - SR00221755
            pItemNF.VNF_INBOUND = GetInboundDeliveries(pItemNF)

            ''********************************************************
            'Caso seja um cenário de Simples Nacional, ou entrega futura ou consignação , ou fixar as o valor do ICMS, a alíquota e também a base de calculo que vieram do SAP
            objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'ICMS_ESTATISTICO' ")

            If objRetorno.ToString().Contains(objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().TAX_CODE) AndAlso Not IsFaturaEntregaFutOUConsignacao(pItemNF.NF_PROD_CFOP, "ID_MODO_PROCESSO_REMESSA_CONSIGNACAO", objNF.NF_IDE_FINNFE) AndAlso Not IsFaturaEntregaFutOUConsignacao(pItemNF.NF_PROD_CFOP, "ID_MODO_PROCESSO_REMESSA_ENTREGA_FUTURA", objNF.NF_IDE_FINNFE) AndAlso Not IsFaturaEntregaFutOUConsignacao(pItemNF.NF_PROD_CFOP, "ID_MODO_PROCESSO_FATURA_CONSIGNACAO", objNF.NF_IDE_FINNFE) Then
                pItemNF.NF_ICMS_VBC = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "ICMS").Select(Function(Y) Y.TAX_BASE_CALCULATION_AMOUNT).FirstOrDefault()
                pItemNF.NF_ICMS_VICMS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "ICMS").Select(Function(Y) Y.TAX_AMOUNT).FirstOrDefault()
                pItemNF.NF_ICMS_PICMS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "ICMS").Select(Function(Y) Y.TAX_PERCENTAGE).FirstOrDefault()
                pItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE = pItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE - pItemNF.NF_ICMS_VICMS
            End If

            '**********************************************************************
            'Regra utilizada pra identificar o cenário de fatura da consignação
            If IsFaturaEntregaFutOUConsignacao(pItemNF.NF_PROD_CFOP, "ID_MODO_PROCESSO_FATURA_CONSIGNACAO", objNF.NF_IDE_FINNFE) Then
                If pItemNF.NF_PROD_VOUTRO > 0 Then ''***IPI estatístico destacado na tag outras despesas (vOutro) do XML da NF-e
                    '***************
                    'IPI
                    'Verifica se existe base reduzida, caso exista irá utilizar o valor retornado pelo SAP, caso contrário irá utilizar po campo vprod do XML
                    pItemNF.NF_IPI_VBC = pItemNF.NF_PROD_VPROD
                    pItemNF.NF_IPI_PIPI = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "IPI").Select(Function(Y) Y.TAX_PERCENTAGE).FirstOrDefault()
                    pItemNF.NF_IPI_VIPI = Math.Round((objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "IPI").Select(Function(Y) Y.TAX_PERCENTAGE).FirstOrDefault() / 100) * pItemNF.NF_PROD_VPROD, 2)
                    pItemNF.NF_PROD_VOUTRO = 0
                End If
                '********
                'ICMS estatístico para escrituração da nota no SAP
                'Verifica se existe base reduzida, caso exista irá utilizar o valor retornado pelo SAP, caso contrário irá utilizar po campo vprod do XML
                If objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "ICMS").Select(Function(Y) Y.OTHER_EXCLUDED_TAX_BASE_CALCULATION_AMOUNT).FirstOrDefault() > 0 _
                        And objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "ICMS").Select(Function(Y) Y.TAX_BASE_CALCULATION_AMOUNT).FirstOrDefault() > 0 Then
                    pItemNF.NF_ICMS_VBC = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "ICMS").Select(Function(Y) Y.TAX_BASE_CALCULATION_AMOUNT).FirstOrDefault()
                    pItemNF.NF_ICMS_PICMS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "ICMS").Select(Function(Y) Y.TAX_PERCENTAGE).FirstOrDefault()
                    pItemNF.NF_ICMS_VICMS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "ICMS").Select(Function(Y) Y.TAX_AMOUNT).FirstOrDefault()
                Else
                    pItemNF.NF_ICMS_VBC = pItemNF.NF_PROD_VPROD
                    pItemNF.NF_ICMS_PICMS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "ICMS").Select(Function(Y) Y.TAX_PERCENTAGE).FirstOrDefault()
                    pItemNF.NF_ICMS_VICMS = Math.Round((objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "ICMS").Select(Function(Y) Y.TAX_PERCENTAGE).FirstOrDefault() / 100) * pItemNF.NF_PROD_VPROD, 2)
                End If



                '********
                'PIS
                pItemNF.NF_PIS_VPIS = Math.Round((objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "PIS").Select(Function(Y) Y.TAX_PERCENTAGE).FirstOrDefault() / 100) * pItemNF.NF_PROD_VPROD, 2)
                'pItemNF.NF_PIS_VBC = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "PIS").Select(Function(Y) Y.TAX_BASE_CALCULATION_AMOUNT).FirstOrDefault()
                'pItemNF.NF_PIS_VPIS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "PIS").Select(Function(Y) Y.TAX_AMOUNT).FirstOrDefault()
                'pItemNF.NF_PIS_PPIS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "PIS").Select(Function(Y) Y.TAX_PERCENTAGE).FirstOrDefault()

                '*********
                'COFINS
                pItemNF.NF_COFINS_VCOFINS = Math.Round((objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "COFINS").Select(Function(Y) Y.TAX_PERCENTAGE).FirstOrDefault() / 100) * pItemNF.NF_PROD_VPROD, 2)
                'pItemNF.NF_COFINS_VBC = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "COFINS").Select(Function(Y) Y.TAX_BASE_CALCULATION_AMOUNT).FirstOrDefault()
                'pItemNF.NF_COFINS_VCOFINS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "COFINS").Select(Function(Y) Y.TAX_AMOUNT).FirstOrDefault()
                'pItemNF.NF_COFINS_PCOFINS = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault().ITEM_TAXES.Where(Function(X) X.TAX_NAME = "COFINS").Select(Function(Y) Y.TAX_PERCENTAGE).FirstOrDefault()


                '**********
                'Valor líquido 
                pItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE = pItemNF.NF_PROD_VPROD - pItemNF.NF_ICMS_VICMS - pItemNF.NF_PIS_VPIS - pItemNF.NF_COFINS_VCOFINS
            End If

            '--> Ajsute de valores para diferenças de R$0,01 para o cenário de Remessa de Consignação
            If IsFaturaEntregaFutOUConsignacao(pItemNF.NF_PROD_CFOP, "ID_MODO_PROCESSO_REMESSA_CONSIGNACAO", objNF.NF_IDE_FINNFE) Then
                Dim diff = pItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE - pItemNF.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE
                If (diff) = 0.01 Or (diff) = -0.01 Then
                    Dim objTax = New PurchaseOrderItemsTaxes()
                    objTax = pItemNF.SAP_ITEM_DETAILS.ITEM_TAXES.Where(Function(x) x.TAX_NAME = "COFINS").FirstOrDefault
                    pItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE = pItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE + -diff
                    objTax.TAX_AMOUNT = objTax.TAX_AMOUNT + -diff
                    ' '' ''If diff < 0 Then
                    ' '' ''    pItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE = pItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE + diff
                    ' '' ''    objTax.TAX_AMOUNT = objTax.TAX_AMOUNT + diff
                    ' '' ''Else
                    ' '' ''    pItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE = pItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE + -diff
                    ' '' ''    objTax.TAX_AMOUNT = objTax.TAX_AMOUNT + -diff
                    ' '' ''End If
                    pItemNF.SAP_ITEM_DETAILS.ITEM_TAXES(pItemNF.SAP_ITEM_DETAILS.ITEM_TAXES.FindIndex(Function(x) x.TAX_NAME = "COFINS")) = objTax
                End If
            End If








            '--> SE FOR UM FORNECEDOR NOVO, CADASTRA NO VNF
            modSQL.CommandText = "select count(*) from TbFOR where CODFOR = '" & pNF.SAP_DETAILS.VENDOR_CODE & "'"
            If modSQL.ExecuteScalar(modSQL.CommandText) = "0" Then
                modSQL.CommandText = "insert into TbFOR(CODFOR, RAZFOR, CNPJ) values ('" & pNF.SAP_DETAILS.VENDOR_CODE & "', '" & pNF.NF_EMIT_XNOME & "', '" & pNF.NF_EMIT_CNPJ & "' )"
                modSQL.ExecuteNonQuery(modSQL.CommandText)
            End If

        Catch ex As Exception
            'RegistrarNotaProblema(pNF.VNF_CHAVE_ACESSO, ex.Message, "LerSAP") 'marcio xxxx
            RegistrarLog(Nothing, ex)
            Throw New Exception("Ocorreu um erro ao consultar o pedido no SAP")
        Finally
            modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
        End Try
    End Sub

    '*****************************************************
    'Ler dados do SAP para geração de nota manual (J1B1N)
    '*****************************************************

    Public Function LerSAP(ByVal pStrCNPJ As String, ByVal pStrPartnerType As String, Optional ByRef pObjJ1B1NFilter As SAP_RFC.MainDataJ1B1NFilter = Nothing) As SAP_RFC.MainDataJ1B1N

        Dim objRfcReturn As New SAP_RFC.RfcReturn
        Dim vObjJ1B1NFilter As New SAP_RFC.MainDataJ1B1NFilter


        Try


            vObjJ1B1NFilter.SearchItems = pObjJ1B1NFilter.SearchItems

            vObjJ1B1NFilter.CNPJEmitente = pStrCNPJ
            vObjJ1B1NFilter.FuncaoNFEmitente = pStrPartnerType

            Dim vObjMainData = SAP_RFC.getMainDataJ1B1N(vObjJ1B1NFilter, objRfcReturn)


            If Not objRfcReturn.Success Then
                Throw New Exception(objRfcReturn.Message)
            End If


            Return vObjMainData
        Catch ex As Exception
            RegistrarLog(TipoProcessamento.Aplicacao, ex, "Erro na função LerSAP (J1B1N)", "Erro ao executar a função LerSAP para leitura de dados para gerar nota manual J1B1N.")
            Throw
        End Try
    End Function

#End Region

#Region " CompararPedidoSAP "
    ''' <summary>
    ''' Compara o pedido do SAP com os dados da nota
    ''' </summary>
    ''' <param name="Usuario"></param>
    ''' <remarks></remarks>
    '''<example>Marcio Spinosa - 15/06/2018 - CR00008351 - adicionado o método validarDuplicata na regra de validações</example> 
    Private Sub CompararPedidoSAP(ByVal Usuario As String)
        Try
            Dim dataComparacao As DateTime = DateTime.Now
            Dim vLisColectionPOProcessed As List(Of Long) = New List(Of Long)
            Dim vLisColectionPOPWithDeliveryrocessed As List(Of Long) = New List(Of Long)
            Dim vStrMovmentTypes As String
            Dim vstrCFOPComp As String


            '********************************
            'Carrega tipos de movimentos a serem utilizados para o cálculo da quandidade de saldo na PO
            vStrMovmentTypes = CarregarTiposMovimento()

            'Carrega os CFOPS dos parametros de componentes para regras de 5 e não 46
            vstrCFOPComp = modSQL.ExecuteScalar("SELECT VALOR FROM TBPAR WHERE PARAMETRO = 'CFOP_COMP'") 'Marcio Spinosa - 18/04/2018 - CR00008351

            '--> INICIA O PROCESSO DE VALIDAÇÃO DOS ITENS ASSOCIADOS, QUE SEJAM RELEVANTES E VÁLIDOS

            For indItemNF As Integer = 0 To objNF.ITENS_NF.Count() - 1

                '--> VALIDAÇÕES DE HEADER
                'Marcio Spinosa - 14/08/2018 - CR00008351
                'If (indItemNF = 0 And Not objNF.NF_IDE_FINNFE = 2) Then
                'If (Not vstrCFOPComp.Contains(objNF.ITENS_NF(indItemNF).NF_PROD_CFOP) And Not objNF.NF_IDE_FINNFE = 2) Then


                If (Not vstrCFOPComp.Contains(objNF.ITENS_NF(indItemNF).NF_PROD_CFOP) And Not objNF.NF_IDE_FINNFE = 2 And objNF.VNF_TIPO_DOCUMENTO <> "CTE") Or (objNF.VNF_TIPO_DOCUMENTO = "CTE" And indItemNF = 0) Then

                    'Marcio Spinosa - 14/08/2018 - CR00008351 - Fim
                    ValidarSituacaoSefaz(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)
                    'Marcio Spinosa - data - 22/07/2019 - SR00291704
                    'If (objNF.NF_STATUS_TRIANGULUS <> "101") Or (objNF.NF_STATUS_TRIANGULUS <> "155") Then ' Marcio Spinosa - 29/11/2018 - CR00009165
                    If (objNF.NF_STATUS_TRIANGULUS <> "101") And (objNF.NF_STATUS_TRIANGULUS <> "155") Then ' Marcio Spinosa - 29/11/2018 - SR00291704
                        'Marcio Spinosa - data - 22/07/2019 - SR00291704 -Fim
                        ValidarValorNotaFiscal(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        ValidarNotaFiscalReferenciada(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario, "REMESSA_ENTREGA_FUTURA")

                        'Marcio Spinosa - 31/07/2019 - SR00293593
                        'If (objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "S") Then
                        '--> Validação do código UF e código do município da Metso 
                        ValidarUFeCodigoDoMunicipioMetso(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> Validação do código UF e código do município da Fornecedor 
                        ValidarUFeCodigoDoMunicipioFornecedor(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)
                        'End If
                        'Marcio Spinosa - 31/07/2019 - SR00293593 - Fim
                    End If ' Marcio Spinosa - 29/11/2018 - CR00009165
                End If

                'Marcio Spinosa - 18/04/2018 - CR00008351
                'If ((objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "S" And Not objNF.NF_IDE_FINNFE = 2 And Not objNF.ITENS_NF(indItemNF).VNF_IS_SUBCONTRATACAO) _
                '    Or (objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "S" And objNF.ITENS_NF(indItemNF).VNF_IS_SUBCONTRATACAO And objNF.ITENS_NF(indItemNF).NF_PROD_CFOP = "5124" And Not objNF.NF_IDE_FINNFE = 2)) Then
                'Marcio Spinosa - data - 22/07/2019 - SR00291704
                'If (objNF.NF_STATUS_TRIANGULUS <> "101") And (objNF.NF_STATUS_TRIANGULUS <> "155") Then 'Marcio Spinosa - 29/11/2018 - CR00009165 
                If (objNF.NF_STATUS_TRIANGULUS <> "101") And (objNF.NF_STATUS_TRIANGULUS <> "155") Then ' Marcio Spinosa - 29/11/2018 - SR00291704
                    'Marcio Spinosa - data - 22/07/2019 - SR00291704 - Fim
                    If ((objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "S" And Not objNF.NF_IDE_FINNFE = 2 And Not objNF.ITENS_NF(indItemNF).VNF_IS_SUBCONTRATACAO) _
                Or (objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "S" And objNF.ITENS_NF(indItemNF).VNF_IS_SUBCONTRATACAO And Not vstrCFOPComp.Contains(objNF.ITENS_NF(indItemNF).NF_PROD_CFOP) And Not objNF.NF_IDE_FINNFE = 2)) Then
                        '    'Marcio Spinosa - 18/04/2018 - CR00008351 - Fim
                        '--> 01. NCMS
                        ValidarNCM(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 02. REMESSA FINAL
                        ValidarRemessaFinal(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 03. DELETADO
                        ValidarItemDeletado(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 04. QUANTIDADE
                        If vLisColectionPOProcessed.Count > 0 Then
                            If Not vLisColectionPOProcessed.Contains(Long.Parse(objNF.ITENS_NF(indItemNF).NF_PROD_XPED & objNF.ITENS_NF(indItemNF).NF_PROD_NITEMPED.ToString())) Then
                                ValidarQuantidade(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario, objNF, vLisColectionPOProcessed, vStrMovmentTypes)
                            End If
                        Else
                            ValidarQuantidade(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario, objNF, vLisColectionPOProcessed, vStrMovmentTypes)
                        End If

                        '--> 05. VALOR BRUTO
                        ValidarValorBruto(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 06. CNPJ METSO
                        ValidarCnpjMetso(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 07. CNPJ DO EMITENTE DA NF
                        ValidarCnpjEmitente(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 08. REGIME ESPECIAL
                        'ValidarRegimeEspecial(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 09. CONDIÇÃO DE PAGAMENTO
                        ValidarCondicaoPagamento(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 10. ESTRATÉGIA DE LIBERAÇÃO DO PEDIDO
                        ValidarAprovacaoPedido(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 11. CONFIRMAÇÃO DO PEDIDO - POOL 4 TOOL
                        'ValidarConfirmacaoPedido(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 12. PRAZO DE ENTREGA ANTECIPADO EM ATÉ 10 DIAS
                        ValidarEntregaAntecipada(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 13. REMESSA FUTURA
                        'ValidarEntregaFutura(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 15. DEPÓSITO NO PEDIDO
                        ValidarDepositoPedido(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 16. DEPÓSITO NA INBOUND DELIVERY
                        ValidarDepositoInboundDelivery(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 17. VERSÃO NO PEDIDO
                        ValidarVersaoPedido(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 18. INDICADOR DE RECEBIMENTO DE MARCADORIA
                        ValidarIndicadorRecebimentoMercadoria(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 19. INDICADOR DE RECEBIMENTO DE FATURA
                        ValidarIndicadorRecebimentoFatura(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 20. TAX CODE
                        ValidarTaxCode(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 21. INBOUND DELIVERY
                        ''ValidarInboundDelivery(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        If vLisColectionPOPWithDeliveryrocessed.Count > 0 Then
                            If Not vLisColectionPOPWithDeliveryrocessed.Contains(Long.Parse(objNF.ITENS_NF(indItemNF).NF_PROD_XPED & objNF.ITENS_NF(indItemNF).NF_PROD_NITEMPED.ToString())) Then
                                ValidarInboundDelivery(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario, objNF, vLisColectionPOPWithDeliveryrocessed)
                            End If
                        Else
                            ValidarInboundDelivery(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario, objNF, vLisColectionPOPWithDeliveryrocessed)
                        End If

                        ''--> 22. SALDO DO ITEM PARA PEDIDOS SEM CONTROLE DE CONFIRMAÇÕES
                        'ValidarSaldoItem(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 23. STATUS DO MATERIAL A NÍVEL GLOBAL
                        ValidarStatusMaterialGlobal(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 24. STATUS DO MATERIAL A NÍVEL DE PLANTA
                        ValidarStatusMaterialPlanta(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 25. STATUS DO MATERIAL A NÍVEL DE ORGANIZAÇÃO DE VENDAS
                        'ValidarStatusMaterialOrganizacaoVendas(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 26. MATERIAL DELETADO A NÍVEL DE PLANTA
                        ValidarMaterialDeletadoPlanta(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 27. MATERIAL DELETADO A NÍVEL GLOBAL
                        ValidarMaterialDeletadoGlobal(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 28. MATERIAL DELETADO A NÍVEL DE DEPÓSITO
                        ValidarMaterialDeletadoDeposito(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        ''--> 29. NF-E RECEBIDO EM CONTINGÊNCIA
                        'ValidarRecebidoContingencia(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 30. VALOR LÍQUIDO DA MERCADORIA
                        ValidarValorLiquido(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 31. VALOR BRUTO DO ITEM DA NOTA FISCAL + IPI
                        ValidarValorBrutoComIpi(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 32. ICMS
                        ValidarIcms(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 33. IPI
                        ValidarIpi(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '--> 34. INCOTERMS
                        ValidarIncoterms(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '-->35. Validar IE da Metso
                        ValidarIEMetso(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '-->36. Validar IE do emitente
                        ValidarIEEmitente(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '-->37. Fatura baseada em entrada de mercadorias (Indicator: GR-Based Invoice Verification)
                        ValidarIndicadorFaturaBaseadaEmEntradaDeMercadorias(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '-->38. Código de material na ordem de compra
                        ValidarCodigoDeMaterialNaOrdemDeCompra(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '-->39. Para os cenário de Fatura da Consignação, verificar se a nota não está cancelada no SAP
                        ' e se a quantidade recebida pelas remessas tem saldo o suficiente para o lançamento da fatura
                        If IsFaturaEntregaFutOUConsignacao(objNF.ITENS_NF(indItemNF).NF_PROD_CFOP, "ID_MODO_PROCESSO_FATURA_CONSIGNACAO", objNF.NF_IDE_FINNFE) Then
                            ValidarNFdeRemessaConsignacaoCancelada(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)
                            ValidarSaldodaRemessadaConsignacao(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)
                            ValidarNotaFiscalReferenciada(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario, "REMESSA_CONSIGNACAO")
                        End If

                        '-->40. Verificar se o material recebido/comprado está vinculado a uma ordem de produção (F) e seu uso é 2 ou 3
                        ValidarMaterialemOrdemdeProducao(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)

                        '-->41. Verificar se o material no pedido de compras não é um serviço
                        ValidarTipoDeMaterialComoServico(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)


                        '-->42. Verificar se o CFOPS de subcontratacao esta na regra de CFOPs na base
                        'Marcio Spinosa - 18/04/2018 - CR00008351
                        'If (objNF.ITENS_NF(indItemNF).VNF_IS_SUBCONTRATACAO) Then
                        ValidarSubContratacao(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario) 'Marcio Spinosa - 18/04/2018 - CR00008351
                        'End If
                        'Marcio Spinosa - 18/04/2018 - CR00008351 - Fim

                        '-->43. Validar se o xml possui duplicata
                        ValidarDuplicata(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario) 'Marcio Spinosa - 15/06/2018 - CR00008351

                        'Marcio Spinosa - 23/07/2018 - CR00008351
                        'ElseIf (objNF.ITENS_NF(indItemNF).VNF_ITEM_VALIDO = "S" And objNF.ITENS_NF(indItemNF).VNF_IS_SUBCONTRATACAO And objNF.ITENS_NF(indItemNF).NF_PROD_CFOP <> "5124" And Not objNF.NF_IDE_FINNFE = 2) Then
                    ElseIf (objNF.ITENS_NF(indItemNF).VNF_IS_SUBCONTRATACAO And vstrCFOPComp.Contains(objNF.ITENS_NF(indItemNF).NF_PROD_CFOP) And Not objNF.NF_IDE_FINNFE = 2) Then
                        'Marcio Spinosa - 23/07/2018 - CR00008351 - Fim
                        ValidarNFdeRemessaSubcontratacaoCancelada(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)
                        ValidarSaldodaRemessadaSubcontratacao(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario)
                        If IsCFOPSubContratacao(objNF.ITENS_NF(indItemNF).NF_PROD_CFOP) Then
                            ValidarNotaFiscalReferenciada(objNF.ITENS_NF(indItemNF), dataComparacao, Usuario, "REMESSA_SUBCONTRATACAO")
                        End If


                    End If
                End If ' Marcio Spinosa - 29/11/2018 - CR00009165
            Next

            ' --> BUSCA A SITUAÇÃO ANTERIOR DA NOTA FISCAL
            modSQL.CommandText = "select SITUACAO from TbNFE where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
            Dim strSituacao As String = modSQL.ExecuteScalar(modSQL.CommandText)

            '---> NOTA FISCAL CANCELADA NÃO PODE TER O STATUS ALTERADO
            If strSituacao <> "CANCELADA" Then
                '--> SE NÃO HOUVER NENHUMA DIVERGÊNCIA ATIVA, A NOTA FISCAL ESTÁ APROVADA
                modSQL.CommandText = "select isnull(count(*),0) from TbLOG where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' and SITUACAO = 'ATIVO'"
                If modSQL.ExecuteScalar(modSQL.CommandText) = "0" Then
                    '--> ATUALIZA O STATUS DA NOTA FISCAL PARA ACEITA E ENVIA UM COMUNICADO PARA O FORNECEDOR
                    modSQL.CommandText = "update TbNFE set SITUACAO = 'ACEITA', NFEVAL = 'S', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                    modSQL.ExecuteNonQuery(modSQL.CommandText)
                    If Not objNF.IGNORE_EMAIL And (objNF.VNF_TIPO_DOCUMENTO <> "NFS" And objNF.VNF_TIPO_DOCUMENTO <> "FAT" And objNF.VNF_TIPO_DOCUMENTO <> "TLC") Then
                        Call EnviarMensagemParaFornecedor(objNF.VNF_CHAVE_ACESSO, "ACEITA")
                    End If
                Else
                    '--> ATUALIZA O STATUS DA NOTA FISCAL PARA REJEITADA E ENVIA UM COMUNICADO PARA O FORNECEDOR + COMPRADOR
                    modSQL.CommandText = "update TbNFE set SITUACAO = 'REJEITADA', NFEVAL = 'S', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                    modSQL.ExecuteNonQuery(modSQL.CommandText)
                    If Not objNF.IGNORE_EMAIL And (Not objNF.VNF_TIPO_DOCUMENTO = "NFS" Or Not objNF.VNF_TIPO_DOCUMENTO = "FAT" Or Not objNF.VNF_TIPO_DOCUMENTO = "TLC") Then
                        Call EnviarMensagemParaFornecedor(objNF.VNF_CHAVE_ACESSO, "PENDENTE")
                        Call EnviarMensagemComprador(objNF.VNF_CHAVE_ACESSO, objNF.ITENS_NF(0).NF_PROD_XPED, objNF.SAP_DETAILS.PURCHASING_GROUP)
                    End If
                End If
                'Marcio Spinosa - 20/06/2018 - CR00008351 - Fim
            End If
        Catch ex As Exception
            RegistrarLog(Nothing, ex)
            Throw ex
        End Try
    End Sub
#End Region

#Region " VerificarAssociacaoPortal "
    Public Sub VerificarAssociacaoPortal()
        Try

            '---> Busca os dados no banco de dados online
            Dim objDataTable As New DataTable()
            objDataTable = modSQL.Fill("SELECT asv_numero_nf, asv_serie, asv_cnpj, asv_item_nf, asv_pedido, asv_item_pedido, asv_atualizado, id_associacao_vnf, asv_email FROM asv_associacao_vnf WHERE asv_atualizado = 0 ", modSQL.connectionStringVnfFornecedor)

            Dim strNfeId As String = String.Empty
            Dim strMensagem As String

            RegistrarLog(TipoProcessamento.Servico, Nothing, "PROCESSAMENTO DAS ASSOCIAÇÕES", objDataTable.Rows.Count.ToString() + " ASSOCIAÇÕES LOCALIZADAS NO PORTAL")

            Dim strSQL As String = ""
            For Each dtrLinha As DataRow In objDataTable.Rows
                Try
                    If dtrLinha(4).Trim.ToString().Length = 10 Then

                        strSQL = "SELECT NFEID FROM TbDOC_CAB_NFE WHERE NF_IDE_NNF = '" & dtrLinha(0).Trim & "' and NF_IDE_SERIE = '" & dtrLinha(1).Trim & "' and NF_EMIT_CNPJ = '" & dtrLinha(2).Trim & "'"
                        strNfeId = modSQL.ExecuteScalar(strSQL)

                        If (Not String.IsNullOrEmpty(strNfeId) AndAlso modSQL.ExecuteScalar("select situacao from tbnfe where nfeid = '" & strNfeId & "'") <> "ACEITA") Then
                            strSQL = "update tbjun set ITEVAL = 'N', PEDCOM = '" & dtrLinha(4).Trim & "', ITEPED = " & dtrLinha(5).Trim & " WHERE NFEID = '" & strNfeId & "' and ITENFE = " & dtrLinha(3).Trim
                            modSQL.ExecuteNonQuery(strSQL)

                            strSQL = "UPDATE TBNFE SET REPROCESSAR = 'S' WHERE NFEID = '" & strNfeId & "'"
                            modSQL.ExecuteNonQuery(strSQL)

                            strSQL = "INSERT INTO TbMEN VALUES (" &
                                     "   '" & strNfeId & "', " &
                                     "   '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', " &
                                     "   'Portal VNF', " &
                                     "   'PORTAL', " &
                                     "   'Esta NF foi associada através do Portal VNF. Esse processo não garante que a nota foi associada corretamente. Os dados inseridos são de responsabilidade do fornecedor.', 'C' )"
                            modSQL.ExecuteNonQuery(strSQL)

                            'Atualiza o campo email na tabela de fornecedores
                            strSQL = " UPDATE TbFOR SET EMAIL_NFE = '" & dtrLinha(8) & "' WHERE CNPJ = '" & dtrLinha(2) & "'"
                            modSQL.ExecuteNonQuery(strSQL)


                            '           Validar(strNfeId, String.Empty, True, True, TipoProcessamento.Servico, String.Empty, strMensagem)
                        End If


                        '---> Busca os dados no banco de dados online
                        modSQL.ExecuteNonQuery("UPDATE asv_associacao_vnf SET asv_atualizado = 1 " &
                                               "WHERE asv_atualizado = 0 and " &
                                               "    asv_numero_nf = '" & dtrLinha("asv_numero_nf").Trim & "' and " &
                                               "    asv_serie = '" & dtrLinha("asv_serie").Trim & "' and " &
                                               "    asv_cnpj = '" & dtrLinha("asv_cnpj").Trim & "' and " &
                                               "    asv_item_nf = '" & dtrLinha("asv_item_nf").Trim & "'", modSQL.connectionStringVnfFornecedor)
                    Else
                        '---> atualiza os dados que estão incorretos como 1 para que não sobrecarregue o serviço na associação
                        modSQL.ExecuteNonQuery("UPDATE asv_associacao_vnf SET asv_atualizado = 1 " &
                                               "WHERE asv_atualizado = 0 and " &
                                               "    asv_numero_nf = '" & dtrLinha("asv_numero_nf").Trim & "' and " &
                                               "    asv_serie = '" & dtrLinha("asv_serie").Trim & "' and " &
                                               "    asv_cnpj = '" & dtrLinha("asv_cnpj").Trim & "' and " &
                                               "    asv_item_nf = '" & dtrLinha("asv_item_nf").Trim & "'", modSQL.connectionStringVnfFornecedor)
                    End If

                Catch ex As Exception
                    modSQL.ExecuteNonQuery("insert into TbLOGService(log_titulo,log_descricao,log_trace,log_data) values('PORTAL_EXCEPTION', '" & objNF.VNF_CHAVE_ACESSO & "', '" & ex.Message.Replace("'", "") & " - " & ex.StackTrace.Replace("'", "") & " - comando SQL: " & strSQL.Replace("'", "") & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "')")
                End Try
            Next

        Catch ex As Exception
            RegistrarLog(Nothing, ex)
            Throw New Exception("Ocorreu um erro ao verificar associações no portal")
        End Try
    End Sub
#End Region

#Region " GravaAnexoDB "
    Public Sub GravaAnexoDB(ByVal Arquivo As String)
        Try
            Dim file() As String = My.Computer.FileSystem.ReadAllText(Arquivo, System.Text.Encoding.UTF7).Split(vbNewLine)
            Dim linha() As String

            Dim strSQL As String = ""

            For i As Integer = 1 To (file.Length - 1)

                linha = file(i).Split("|")


                If linha.Length = 1 Then
                    Exit Sub
                End If

                If linha(4).Trim <> "" And linha(5).Trim <> "" Then
                    strSQL = "update tbjun set ITEVAL = 'N', PEDCOM = '" & linha(4).Trim & "', ITEPED = " & linha(5).Trim &
                                        " WHERE NFEID = " &
                                        "(" &
                                        "select top 1 nfeid from vwnotfis " &
                                        "where numero = " & linha(0).Trim &
                                        " and serie = " & linha(1).Trim &
                                        " and cnpj_emitente = " & linha(2).Trim &
                                        ") " &
                                        "and ITENFE = " & linha(3).Trim

                    modSQL.ExecuteNonQuery(strSQL)

                    strSQL = "UPDATE TBNFE SET REPROCESSAR = 'S' WHERE NFEID = (" &
                    "select nfeid from vwnotfis " &
                                        "where numero = " & linha(0).Trim &
                                        " and serie = " & linha(1).Trim &
                                        " and cnpj_emitente = " & linha(2).Trim &
                                        ") "

                    modSQL.ExecuteNonQuery(strSQL)
                End If
            Next
        Catch ex As Exception
            RegistrarLog(Nothing, ex)
        End Try

    End Sub
#End Region

#Region " EnviarMensagemParaFornecedor "
    Public Sub EnviarMensagemParaFornecedor(ByVal CHAVE_ACESSO As String, ByVal SITUACAO As String)
        Try
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_001: Inicio função enviar e-mail para fornecedor", "Situação da nota para envio de email: " & SITUACAO & " NFEID: " & CHAVE_ACESSO)
            If SITUACAO = "ACEITA" Then Call EnviarMsgAceita(CHAVE_ACESSO)
            If SITUACAO = "REJEITADA" Then Call EnviarMsgRejeitada(CHAVE_ACESSO)
            If SITUACAO = "PENDENTE" Then Call EnviarMsgPendente(CHAVE_ACESSO)
        Catch ex As Exception
            RegistrarLog(Nothing, ex)
            Throw New Exception("Ocorreu um erro ao enviar e-mail para o fornecedor")
        End Try

    End Sub
#End Region

#Region " EnviarMensagem "
    Public Sub EnviarMensagem(ByVal send_to As String, ByVal subject As String, ByVal body As String, Optional ByVal CHAVE_ACESSO As String = "11111")
        Try
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_010: Função: EnviarMensagem.", "Verificando se existe um endereço de email cadastrado para realizar o envio da mensagem. NFEID: " & CHAVE_ACESSO)
            If (send_to <> nenhum_email_cadastrado) Then
                RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_011: Função: EnviarMensagem.", "Removendo espaçõs em brando do início e do final do endereço de email. Email cadastrado: " & send_to & " NFEID: " & CHAVE_ACESSO)
                send_to = Trim(send_to)

                RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_012: Função: EnviarMensagem.", "Removendo ultima caracter caso seja uma vírgula.  Email cadastrado: " & send_to & " NFEID: " & CHAVE_ACESSO)
                If send_to.EndsWith(",") Then
                    send_to = send_to.Substring(0, send_to.Length - 1)
                End If

                RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_013: Função: EnviarMensagem.", "Substituindo , por ;. Email cadastrado: " & send_to & " NFEID: " & CHAVE_ACESSO)
                Dim objMailMessage As New Net.Mail.MailMessage("nfefornecedor.mctbr@metso.com", send_to.Replace(";", ","), subject, body)
                objMailMessage.IsBodyHtml = True

                RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_014: Função: EnviarMensagem.", "Chamando função nativa do framework .net para envio do e-mail. NFEID: " & CHAVE_ACESSO)
                'Dim objSmtpClient As New Net.Mail.SmtpClient("smtp.metso.com")

                'Marcio Spinosa - 25/09/2018
                Dim isAnexo As Attachment
                isAnexo = VerificarComunicado(body)
                If Not (isAnexo Is Nothing) Then
                    objMailMessage.Attachments.Add(isAnexo)
                End If

                Dim MailFrom As String = modSQL.ExecuteScalar("select valor from tbpar where parametro = 'EMAIL_VNF_FORNECEDOR'")


                Uteis.SendMailAttachment(MailFrom, send_to, "", "", subject, body, isAnexo)

                'Marcio Spinosa - 25/09/2018 - Fim
                'objSmtpClient.Send(objMailMessage)
            End If
        Catch ex As Exception
            RegistrarLog(TipoProcessamento.Ambos, ex, "Erro ao enviar e-mail.", "Falha na tentativa de enviar e-mail. Message error: " & ex.Message & ". NFEID:" & CHAVE_ACESSO, "StackTrace: " & ex.StackTrace)
            'Gravando mensagem para notificar o usuário sobre o erro ao enviar o e-mail
            Dim cmdText As String = "insert into TbMEN values ('" & CHAVE_ACESSO & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '', 'ERRO TÉCNICO AO ENVIAR E-MAIL', 'Mensagem técnica:" & ex.Message & ". StackTrace: " & ex.StackTrace & "','')"
            modSQL.ExecuteNonQuery(cmdText)
        End Try
    End Sub
#End Region

#Region "VerificaComunicado"
    'Marcio Spinosa - 27/09/2018
    ''' <summary>
    ''' Método que acessa a tabela parametros e verifica se há comunicado em anexo.
    ''' o parametro tem seguir a ordem "dtIni"|"dtFim"|"endereço completo do arquivo com o nome"|"tipo da nota(55,57,etc)"
    ''' </summary>
    ''' <param name="pstrBody"></param>
    ''' <returns></returns>
    ''' <example>Marcio Spinosa - 27/09/2018 - SR00223130 - Envio de comunicado em anexo</example>
    Public Function VerificarComunicado(ByVal pstrBody As String) As Attachment

        Dim anexado As Attachment

        If Not pstrBody.Contains("Prezado comprador") Then
            Dim pStrDataIni = ""
            Dim pstrDataFim = ""
            Dim pstrCaminhoAnexo = ""
            Dim pstrTipo = ""

            Dim dtComunicados As DataTable = New DataTable()
            dtComunicados = modSQL.Fill("select top 6 * from tbpar where upper(parametro) = 'COMUNICADOS' ")

            If dtComunicados.Rows.Count > 0 Then
                For Each dtrLinha As DataRow In dtComunicados.Rows
                    For Each item As String In dtrLinha("VALOR").ToString.Split("|")

                        If Not String.IsNullOrWhiteSpace(pstrCaminhoAnexo) Then
                            If pstrTipo = "" Then
                                pstrTipo = item
                            End If
                        End If

                        If Not String.IsNullOrWhiteSpace(pstrDataFim) Then
                            If pstrCaminhoAnexo = "" Then
                                pstrCaminhoAnexo = item
                            End If
                        End If

                        If Not String.IsNullOrWhiteSpace(pStrDataIni) Then
                            If pstrDataFim = "" Then
                                pstrDataFim = item
                            End If
                        End If

                        If pStrDataIni = "" Then
                            pStrDataIni = item
                        End If
                    Next
                Next
            End If

            If pstrTipo = objNF.VNF_TIPO_DOCUMENTO Then
                If (DateTime.Now >= Convert.ToDateTime(pStrDataIni) And DateTime.Now <= Convert.ToDateTime(pstrDataFim)) Then
                    If Not (String.IsNullOrEmpty(pstrCaminhoAnexo)) Then
                        anexado = New Attachment(pstrCaminhoAnexo, Application.Octet)
                    End If
                End If
            Else
                anexado = Nothing
            End If
        Else
            anexado = Nothing
        End If

        Return anexado

    End Function
    'Marcio Spinosa - 27/09/2018 - Fim
#End Region

#Region " GravarDadosNFS "
    ''' <summary>
    ''' Este metodo grava as informaçoes que vem do Portal de Servicos.
    ''' </summary>
    ''' <param name="NotaFiscal"></param>
    ''' <param name="IsValidarNF"></param>
    ''' <param name="Usuario"></param>
    Public Sub GravarDadosNFS(ByVal NotaFiscal As modNF, ByVal IsValidarNF As Boolean, ByVal Usuario As String)
        Try
            Dim intIndexItemNF As Integer = 0
            If (NotaFiscal Is Nothing) Then
                NotaFiscal = objNF
            End If

            If (IsValidarNF) Then
                modSQL.CommandText = "exec dbo.SP_DELETE_DOC_INFORMATION '" & NotaFiscal.VNF_CHAVE_ACESSO & "'"
                modSQL.ExecuteNonQuery(modSQL.CommandText)
            End If

            Dim sqlparams As New List(Of SqlClient.SqlParameter)
            '--> SE FOR UMA NOTA DE TALONÁRIO, É PRECISO VERIFICAR SE JÁ FOI INSERIDA NA TABELA NOTF

            Thread.CurrentThread.CurrentCulture = New CultureInfo("pt-Br")
            modSQL.CommandText = "select count(*) from TbNFE where nfeid = '" & NotaFiscal.VNF_CHAVE_ACESSO & "'"
            If (Convert.ToInt32(modSQL.ExecuteScalar(modSQL.CommandText)) = 0) Then
                sqlparams = New List(Of SqlClient.SqlParameter)
                sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                sqlparams.Add(modSQL.AddSqlParameter("NFEVAL", SqlDbType.VarChar, "S"))
                sqlparams.Add(modSQL.AddSqlParameter("NFECAN", SqlDbType.VarChar, "N"))
                sqlparams.Add(modSQL.AddSqlParameter("ID_LISTA", SqlDbType.Int, 0))
                sqlparams.Add(modSQL.AddSqlParameter("DATVAL", SqlDbType.DateTime, DateTime.Now))
                sqlparams.Add(modSQL.AddSqlParameter("NFEREL", SqlDbType.VarChar, "N"))
                sqlparams.Add(modSQL.AddSqlParameter("USUCAN", SqlDbType.VarChar, String.Empty))
                sqlparams.Add(modSQL.AddSqlParameter("JUNAUT", SqlDbType.VarChar, "N"))
                sqlparams.Add(modSQL.AddSqlParameter("SITUACAO", SqlDbType.VarChar, NotaFiscal.VNF_STATUS))
                sqlparams.Add(modSQL.AddSqlParameter("REPROCESSAR", SqlDbType.VarChar, "N"))
                sqlparams.Add(modSQL.AddSqlParameter("CNPJ_METSO", SqlDbType.VarChar, String.Empty))
                sqlparams.Add(modSQL.AddSqlParameter("CONTINGENCIA", SqlDbType.VarChar, "0"))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_STATUS_INTEGRACAO", SqlDbType.VarChar, "PENDENTE"))
                modSQL.ExecuteNonQueryParams(strInsertTbNFE, sqlparams)
            End If


            '--> VERIFICAR SE OS DADOS DESSE DOCUMENTO JÁ NÃO FORAM INSERIDOS NA TABELA
            modSQL.CommandText = "select count(*) from TbDOC_CAB where nfeid = '" & NotaFiscal.VNF_CHAVE_ACESSO & "'"
            If (Convert.ToInt32(modSQL.ExecuteScalar(modSQL.CommandText)) = 0) Then

                '--> CAMPOS CALCULADOS PELO VNF
                sqlparams = New List(Of SqlClient.SqlParameter)
                sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_TIPO_DOCUMENTO", SqlDbType.VarChar, NotaFiscal.VNF_TIPO_DOCUMENTO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_CONTEUDO_XML", SqlDbType.VarChar, NotaFiscal.VNF_CONTEUDO_XML))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_DANFE_ONLINE", SqlDbType.VarChar, NotaFiscal.VNF_DANFE_ONLINE))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_DATA_INSERT", SqlDbType.DateTime, DateTime.Now))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_USUARIO_INSERT", SqlDbType.VarChar, Usuario))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_ANEXO", SqlDbType.VarBinary, NotaFiscal.VNF_ANEXO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_ANEXO_NOME", SqlDbType.VarChar, NotaFiscal.VNF_ANEXO_NOME))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_ANEXO_EXTENSAO", SqlDbType.VarChar, NotaFiscal.VNF_ANEXO_EXTENSAO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_CODIGO_VERIFICACAO", SqlDbType.VarChar, NotaFiscal.VNF_CODIGO_VERIFICACAO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_MATERIAL_RECEBIDO", SqlDbType.Bit, NotaFiscal.VNF_MATERIAL_RECEBIDO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_CLASSIFICACAO", SqlDbType.VarChar, NotaFiscal.VNF_CLASSIFICACAO))
                modSQL.ExecuteNonQueryParams(strInsertTbDoc_Cab, sqlparams)

                '--> CAMPOS RETORNADOS PELO SAP
                sqlparams = New List(Of SqlClient.SqlParameter)
                sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_PO_NUMBER", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.PO_NUMBER))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_CREATED_ON", SqlDbType.DateTime, NotaFiscal.SAP_DETAILS.CREATED_ON))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_CREATED_BY", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.CREATED_BY))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_PAYMENT_TERMS", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.PAYMENT_TERMS))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_PURCHASING_GROUP", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.PURCHASING_GROUP))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_CURRENCY", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.CURRENCY))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_VENDOR_CODE", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.VENDOR_CODE))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_VENDOR_NAME", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.VENDOR_NAME))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_VENDOR_CNPJ", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.VENDOR_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_INCOTERMS1", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.INCOTERMS1))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_VERSION_COMPLETE", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.VERSION_COMPLETE))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_METSO_CNPJ", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.METSO_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_RELEASE_INDIC", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.RELEASE_INDIC))
                modSQL.ExecuteNonQueryParams(strInsertTbDoc_Cab_Sap, sqlparams)

                '--> CAMPOS DE CABEÇALHO DA NFE
                sqlparams = New List(Of SqlClient.SqlParameter)
                sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_OUTROS_SIGNATURE", SqlDbType.Bit, NotaFiscal.NF_OUTROS_SIGNATURE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_OUTROS_INFORMACAO_ADICIONAL", SqlDbType.VarChar, NotaFiscal.NF_OUTROS_INFORMACAO_ADICIONAL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_OUTROS_VERSAO", SqlDbType.VarChar, NotaFiscal.NF_OUTROS_VERSAO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_OUTROS_STATUS_CODE", SqlDbType.VarChar, NotaFiscal.NF_OUTROS_STATUS_CODE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_OUTROS_STATUS_DESC", SqlDbType.VarChar, NotaFiscal.NF_OUTROS_STATUS_DESC))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_MODFRETE", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_MODFRETE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_XNOME", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_XNOME))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_CNPJ", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_IE", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_IE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_XENDER", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_XENDER))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_XMUN", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_XMUN))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_UF", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_UF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_CUF", SqlDbType.Int, NotaFiscal.NF_IDE_CUF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_CNF", SqlDbType.Int, NotaFiscal.NF_IDE_CNF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_INDPAG", SqlDbType.Int, NotaFiscal.NF_IDE_INDPAG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_NATOP", SqlDbType.VarChar, NotaFiscal.NF_IDE_NATOP))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_MOD", SqlDbType.Int, NotaFiscal.NF_IDE_MOD))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_SERIE", SqlDbType.VarChar, NotaFiscal.NF_IDE_SERIE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_NNF", SqlDbType.VarChar, NotaFiscal.NF_IDE_NNF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_DHEMI", SqlDbType.DateTime, NotaFiscal.NF_IDE_DHEMI))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_TPNF", SqlDbType.Int, NotaFiscal.NF_IDE_TPNF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_IDDEST", SqlDbType.Int, NotaFiscal.NF_IDE_IDDEST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_CMUNFG", SqlDbType.VarChar, NotaFiscal.NF_IDE_CMUNFG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_TPEMISS", SqlDbType.Int, NotaFiscal.NF_IDE_TPEMISS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_TPAMB", SqlDbType.Int, NotaFiscal.NF_IDE_TPAMB))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_FINNFE", SqlDbType.Int, NotaFiscal.NF_IDE_FINNFE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_INDFINAL", SqlDbType.Int, NotaFiscal.NF_IDE_INDFINAL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_INDPRES", SqlDbType.Int, NotaFiscal.NF_IDE_INDPRES))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_PROCEMI", SqlDbType.Int, NotaFiscal.NF_IDE_PROCEMI))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_DHCONT", SqlDbType.VarChar, NotaFiscal.NF_IDE_DHCONT))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_XJUST", SqlDbType.VarChar, NotaFiscal.NF_IDE_XJUST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_NFREF", SqlDbType.VarChar, NotaFiscal.NF_IDE_NFREF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_MODAL", SqlDbType.VarChar, NotaFiscal.NF_IDE_MODAL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CNPJ", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_XNOME", SqlDbType.VarChar, NotaFiscal.NF_EMIT_XNOME))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_XLGR", SqlDbType.VarChar, NotaFiscal.NF_EMIT_XLGR))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_NRO", SqlDbType.VarChar, NotaFiscal.NF_EMIT_NRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_XCPL", SqlDbType.VarChar, NotaFiscal.NF_EMIT_XCPL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_XBAIRRO", SqlDbType.VarChar, NotaFiscal.NF_EMIT_XBAIRRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CMUN", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CMUN))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_UF", SqlDbType.VarChar, NotaFiscal.NF_EMIT_UF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CEP", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CEP))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CPAIS", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CPAIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_XPAIS", SqlDbType.VarChar, NotaFiscal.NF_EMIT_XPAIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_FONE", SqlDbType.VarChar, NotaFiscal.NF_EMIT_FONE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_IE", SqlDbType.VarChar, NotaFiscal.NF_EMIT_IE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_IEST", SqlDbType.VarChar, NotaFiscal.NF_EMIT_IEST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_IM", SqlDbType.VarChar, NotaFiscal.NF_EMIT_IM))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CNAE", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CNAE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CRT", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CRT))
                sqlparams.Add(modSQL.AddSqlParameter("NF_REM_XNOME", SqlDbType.VarChar, NotaFiscal.NF_REM_XNOME))
                sqlparams.Add(modSQL.AddSqlParameter("NF_REM_CNPJ", SqlDbType.VarChar, NotaFiscal.NF_REM_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_REM_CMUN", SqlDbType.VarChar, NotaFiscal.NF_REM_CMUN))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XNOME", SqlDbType.VarChar, NotaFiscal.NF_DEST_XNOME))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_CNPJ", SqlDbType.VarChar, NotaFiscal.NF_DEST_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XLGR", SqlDbType.VarChar, NotaFiscal.NF_DEST_XLGR))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_NRO", SqlDbType.VarChar, NotaFiscal.NF_DEST_NRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XCPL", SqlDbType.VarChar, NotaFiscal.NF_DEST_XCPL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XBAIRRO", SqlDbType.VarChar, NotaFiscal.NF_DEST_XBAIRRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_CMUN", SqlDbType.VarChar, NotaFiscal.NF_DEST_CMUN))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XMUN", SqlDbType.VarChar, NotaFiscal.NF_DEST_XMUN))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_UF", SqlDbType.VarChar, NotaFiscal.NF_DEST_UF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_CEP", SqlDbType.VarChar, NotaFiscal.NF_DEST_CEP))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_CPAIS", SqlDbType.VarChar, NotaFiscal.NF_DEST_CPAIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XPAIS", SqlDbType.VarChar, NotaFiscal.NF_DEST_XPAIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_FONE", SqlDbType.VarChar, NotaFiscal.NF_DEST_FONE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_INDIEDEST", SqlDbType.VarChar, NotaFiscal.NF_DEST_INDIEDEST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_IE", SqlDbType.VarChar, NotaFiscal.NF_DEST_IE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_ISUF", SqlDbType.VarChar, NotaFiscal.NF_DEST_ISUF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_IM", SqlDbType.VarChar, NotaFiscal.NF_DEST_IM))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VBC", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VBC))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VICMS", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VICMS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VBCST", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VBCST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VST", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VPROD", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VPROD))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VFRETE", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VFRETE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VSEG", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VSEG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VDESC", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VDESC))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VII", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VII))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VIPI", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VIPI))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VPIS", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VPIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VCOFINS", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VCOFINS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VOUTRO", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VOUTRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VNF", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VNF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VTOTTRIB", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VTOTTRIB))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VICMSDESON", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VICMSDESON))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VICMSUFDEST", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VICMSUFDEST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VICMSUFREMET", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VICMSUFREMET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VFCPUFDEST", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VFCPUFDEST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VSERV", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VSERV))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VBC", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VBC))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VISS", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VISS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VPIS", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VPIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VCOFINS", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VCOFINS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_DTCOMPET", SqlDbType.VarChar, NotaFiscal.NF_ISSQNTOT_DTCOMPET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VDEDUCAO", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VDEDUCAO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VOUTRO", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VOUTRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VDESCINCOD", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VDESCINCOD))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VDESCCOND", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VDESCCOND))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VISSRET", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VISSRET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_CREGTRIB", SqlDbType.VarChar, NotaFiscal.NF_ISSQNTOT_CREGTRIB))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VRETPIS", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VRETPIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VRETCOFINS", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VRETCOFINS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VRETCSLL", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VRETCSLL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VBCIRRF", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VBCIRRF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VIRRF", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VIRRF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VBCRETPREV", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VBCRETPREV))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VRETPREV", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VRETPREV))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_VSERV", SqlDbType.Decimal, NotaFiscal.NF_RETTRANSP_VSERV))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_VBCRET", SqlDbType.Decimal, NotaFiscal.NF_RETTRANSP_VBCRET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_PICMSRET", SqlDbType.Decimal, NotaFiscal.NF_RETTRANSP_PICMSRET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_VICMSRET", SqlDbType.Decimal, NotaFiscal.NF_RETTRANSP_VICMSRET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_CFOP", SqlDbType.Int, NotaFiscal.NF_RETTRANSP_CFOP))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_CMUNFG", SqlDbType.VarChar, NotaFiscal.NF_RETTRANSP_CMUNFG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_NFAT", SqlDbType.VarChar, NotaFiscal.NF_COBR_NFAT))
                sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_VORIG", SqlDbType.Decimal, NotaFiscal.NF_COBR_VORIG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_VDESC", SqlDbType.Decimal, NotaFiscal.NF_COBR_VDESC))
                sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_VLIQ", SqlDbType.Decimal, NotaFiscal.NF_COBR_VLIQ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_TPAG", SqlDbType.VarChar, NotaFiscal.NF_PAG_TPAG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_VPAG", SqlDbType.Decimal, NotaFiscal.NF_PAG_VPAG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_CNPJ", SqlDbType.VarChar, NotaFiscal.NF_PAG_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_TBAND", SqlDbType.VarChar, NotaFiscal.NF_PAG_TBAND))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_CAUT", SqlDbType.VarChar, NotaFiscal.NF_PAG_CAUT))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_TPINTEGRA", SqlDbType.VarChar, NotaFiscal.NF_PAG_TPINTEGRA))
                sqlparams.Add(modSQL.AddSqlParameter("NF_NFREF_REFNNF", SqlDbType.VarChar, NotaFiscal.NF_NFREF_REFNNF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_NFREF_REFSerie", SqlDbType.VarChar, NotaFiscal.NF_NFREF_REFSerie))
                sqlparams.Add(modSQL.AddSqlParameter("NF_NFREF_REFDHEMI", SqlDbType.DateTime, NotaFiscal.NF_NFREF_REFDHEMI))
                modSQL.ExecuteNonQueryParams(strInsertTbDoc_Cab_Nfe, sqlparams)

                intIndexItemNF = 0
                For Each item As modNFItem In NotaFiscal.ITENS_NF
                    modSQL.CommandText = "select count(*) from TbJUN where nfeid = '" & NotaFiscal.VNF_CHAVE_ACESSO & "' and ITENFE = " & item.NF_PROD_ITEM
                    If (Convert.ToInt32(modSQL.ExecuteScalar(modSQL.CommandText)) = 0) Then
                        sqlparams = New List(Of SqlClient.SqlParameter)
                        sqlparams.Add(modSQL.AddSqlParameter("PEDCOM", SqlDbType.VarChar, item.NF_PROD_XPED.RemoveLetters()))
                        sqlparams.Add(modSQL.AddSqlParameter("ITEPED", SqlDbType.Int, item.NF_PROD_NITEMPED))
                        sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                        sqlparams.Add(modSQL.AddSqlParameter("ITENFE", SqlDbType.Int, item.NF_PROD_ITEM))
                        sqlparams.Add(modSQL.AddSqlParameter("ITEVAL", SqlDbType.VarChar, item.VNF_ITEM_VALIDO))
                        sqlparams.Add(modSQL.AddSqlParameter("CODUSU", SqlDbType.VarChar, Usuario))
                        sqlparams.Add(modSQL.AddSqlParameter("CODCOM", SqlDbType.VarChar, objNF.SAP_DETAILS.PURCHASING_GROUP))
                        sqlparams.Add(modSQL.AddSqlParameter("MOTIVO", SqlDbType.VarChar, String.Empty))
                        sqlparams.Add(modSQL.AddSqlParameter("NCMNFE", SqlDbType.VarChar, item.NF_PROD_NCM))
                        sqlparams.Add(modSQL.AddSqlParameter("NCMPED", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.NCM_CODE))
                        item.VNF_CODJUN = Convert.ToDecimal(modSQL.ExecuteScalarParams(strInsertTbJun, sqlparams))
                    Else
                        item.VNF_CODJUN = Convert.ToInt32(modSQL.ExecuteScalar("select codjun from tbjun where nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'"))
                        modSQL.ExecuteNonQuery("UPDATE TbJUN SET ITEVAL = '" & item.VNF_ITEM_VALIDO & "' WHERE CODJUN = " & item.VNF_CODJUN)
                    End If

                    sqlparams = New List(Of SqlClient.SqlParameter)
                    sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                    sqlparams.Add(modSQL.AddSqlParameter("VNF_CODJUN", SqlDbType.Decimal, item.VNF_CODJUN))
                    sqlparams.Add(modSQL.AddSqlParameter("VNF_ITEM_VALIDO", SqlDbType.VarChar, item.VNF_ITEM_VALIDO))
                    sqlparams.Add(modSQL.AddSqlParameter("VNF_CONFIRMADO_PORTAL", SqlDbType.Bit, item.VNF_CONFIRMADO_PORTAL))
                    sqlparams.Add(modSQL.AddSqlParameter("VNF_ID_MODO_PROCESSO", SqlDbType.Int, IIf(item.VNF_ID_MODO_PROCESSO = 0, Nothing, item.VNF_ID_MODO_PROCESSO)))
                    sqlparams.Add(modSQL.AddSqlParameter("VNF_INBOUND_DELIVERY_NUMBER", SqlDbType.VarChar, item.VNF_INBOUND_DELIVERY_NUMBER))
                    sqlparams.Add(modSQL.AddSqlParameter("VNF_INBOUND_DELIVERY_ITEM_NUMBER", SqlDbType.Int, item.VNF_INBOUND_DELIVERY_ITEM_NUMBER))
                    modSQL.ExecuteNonQueryParams(strInsertTbDoc_Item, sqlparams)

                    sqlparams = New List(Of SqlClient.SqlParameter)
                    sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_PO_NUMBER", SqlDbType.VarChar, item.SAP_PO_NUMBER))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_ITEM_NUMBER", SqlDbType.Int, item.SAP_ITEM_DETAILS.ITEM_NUMBER))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_MATERIAL", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.MATERIAL))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_PO_QUANTITY", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.PO_QUANTITY))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_UNIT_OF_MEASURE", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.UNIT_OF_MEASURE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_PRICE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NET_PRICE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_TAX_CODE", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.TAX_CODE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_DELIVERY_COMPLETED", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.DELIVERY_COMPLETED))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_FINAL_INVOICE", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.FINAL_INVOICE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NCM_CODE", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.NCM_CODE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_PLANT", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.PLANT))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_PRICE_UNIT", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.PRICE_UNIT))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_DELETION_INDICATOR", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.DELETION_INDICATOR))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_MATERIAL_DESCRIPTION", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.MATERIAL_DESCRIPTION))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_PO_ITEM_SHORT_TEXT", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.PO_ITEM_SHORT_TEXT))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_CONFIRMATION_CONTROL_KEY", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.CONFIRMATION_CONTROL_KEY))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_OVERDELIVERY_TOLERANCE_LIMIT", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.OVERDELIVERY_TOLERANCE_LIMIT))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_UNLIMITED_OVERDELIVERY_ALLOWED", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.UNLIMITED_OVERDELIVERY_ALLOWED))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_UNDERDELIVERY_TOLERANCE_LIMIT", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.UNDERDELIVERY_TOLERANCE_LIMIT))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_OPEN_QUANTITY", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.OPEN_QUANTITY))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_STORAGE_LOCATION", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.STORAGE_LOCATION))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_ACCOUNT_ASSIGNMENT_CATEGORY", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.ACCOUNT_ASSIGNMENT_CATEGORY))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_ITEM_CATEGORY", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.ITEM_CATEGORY))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_GOODS_RECEIPT", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.GOODS_RECEIPT))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_INVOICE_RECEIPT", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.INVOICE_RECEIPT))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_NET_ITEM_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_ITEM_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_ITEM_VALUE_WITH_TAXES", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_TOTAL_ITEM", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_TOTAL_ITEM))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_NET_FREIGHT_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_NET_FREIGHT_VALUE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_FREIGHT_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_NET_FREIGHT_VALUE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_NET_INSURANCE_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_NET_INSURANCE_VALUE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_INSURANCE_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_NET_INSURANCE_VALUE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_NET_OTHER_EXPENSES_VALUES", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_NET_OTHER_EXPENSES_VALUES))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_OTHER_EXPENSES_VALUES", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_NET_OTHER_EXPENSES_VALUES))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_NET_DISCOUNT_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_NET_DISCOUNT_VALUE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_DISCOUNT_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_NET_DISCOUNT_VALUE))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_DISCOUNT_VALUE_WITH_TAXES", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_DISCOUNT_VALUE_WITH_TAXES))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_ITEM_CFOP", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.SAP_ITEM_CFOP))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_CROSS_PLANT_MATERIAL_STATUS", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.CROSS_PLANT_MATERIAL_STATUS))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_PLANT_MATERIAL_STATUS", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.PLANT_MATERIAL_STATUS))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_CROSS_DISTRIBUTION_CHAIN_STATUS", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.CROSS_DISTRIBUTION_CHAIN_STATUS))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_DELETION_PLANT_LEVEL", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.DELETION_PLANT_LEVEL))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_DELETION_CLIENT_LEVEL", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.DELETION_CLIENT_LEVEL))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_DELETION_STORAGE_LOCATION", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.DELETION_STORAGE_LOCATION))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_USAGE_MATERIAL", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.USAGE_MATERIAL))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_ORIGIN_MATERIAL", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.ORIGIN_MATERIAL))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_TAX_SPLIT", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.TAX_SPLIT))
                    modSQL.ExecuteNonQueryParams(strInsertTbDoc_Item_Sap, sqlparams)

                    sqlparams = New List(Of SqlClient.SqlParameter)
                    sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_ITEM", SqlDbType.Int, item.NF_PROD_ITEM))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_CPROD", SqlDbType.VarChar, item.NF_PROD_CPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_CEAN", SqlDbType.VarChar, item.NF_PROD_CEAN))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_XPROD", SqlDbType.VarChar, item.NF_PROD_XPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_NCM", SqlDbType.VarChar, item.NF_PROD_NCM))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_CFOP", SqlDbType.VarChar, item.NF_PROD_CFOP))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_CFOP_DESC", SqlDbType.VarChar, item.NF_PROD_CFOP_DESC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_UCOM", SqlDbType.VarChar, item.NF_PROD_UCOM))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_QCOM", SqlDbType.Float, item.NF_PROD_QCOM))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VUNCOM", SqlDbType.Float, item.NF_PROD_VUNCOM))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VPROD", SqlDbType.Float, item.NF_PROD_VPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_UTRIB", SqlDbType.VarChar, item.NF_PROD_UTRIB))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_QTRIB", SqlDbType.Float, item.NF_PROD_QTRIB))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VDESC", SqlDbType.Float, item.NF_PROD_VDESC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_INF_ADICIONAL_ITEM", SqlDbType.VarChar, item.NF_PROD_INF_ADICIONAL_ITEM))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_NVE", SqlDbType.VarChar, item.NF_PROD_NVE))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_EXTIPI", SqlDbType.VarChar, item.NF_PROD_EXTIPI))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VFRETE", SqlDbType.Decimal, item.NF_PROD_VFRETE))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VSEG", SqlDbType.Decimal, item.NF_PROD_VSEG))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VOUTRO", SqlDbType.Decimal, item.NF_PROD_VOUTRO))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_INDTOT", SqlDbType.Int, item.NF_PROD_INDTOT))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_DI", SqlDbType.VarChar, item.NF_PROD_DI))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_DETESPECIFICO", SqlDbType.VarChar, item.NF_PROD_DETESPECIFICO))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_XPED", SqlDbType.VarChar, item.NF_PROD_XPED))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_NITEMPED", SqlDbType.Int, item.NF_PROD_NITEMPED))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_FCI", SqlDbType.VarChar, item.NF_PROD_FCI))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PICMS", SqlDbType.Decimal, item.NF_ICMS_PICMS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_ORIG", SqlDbType.VarChar, item.NF_ICMS_ORIG))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_CST", SqlDbType.VarChar, item.NF_ICMS_CST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_MODBC", SqlDbType.Int, item.NF_ICMS_MODBC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PREDBC", SqlDbType.Decimal, item.NF_ICMS_PREDBC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VBC", SqlDbType.Decimal, item.NF_ICMS_VBC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMS", SqlDbType.Decimal, item.NF_ICMS_VICMS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_MODBCST", SqlDbType.Int, item.NF_ICMS_MODBCST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_MVAST", SqlDbType.Decimal, item.NF_ICMS_MVAST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSREDBCST", SqlDbType.Decimal, item.NF_ICMSREDBCST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VBCST", SqlDbType.Decimal, item.NF_ICMS_VBCST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PICMSST", SqlDbType.Decimal, item.NF_ICMS_PICMSST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMSST", SqlDbType.Decimal, item.NF_ICMS_VICMSST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VBCSTRET", SqlDbType.Decimal, item.NF_ICMS_VBCSTRET))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VBCSTDEST", SqlDbType.Decimal, item.NF_ICMS_VBCSTDEST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMSSTDEST", SqlDbType.Decimal, item.NF_ICMS_VICMSSTDEST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_MOTDESICMS", SqlDbType.Int, item.NF_ICMS_MOTDESICMS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PBCOP", SqlDbType.Decimal, item.NF_ICMS_PBCOP))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_UFST", SqlDbType.VarChar, item.NF_ICMS_UFST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PCREDSN", SqlDbType.Decimal, item.NF_ICMS_PCREDSN))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VCREICMSSN", SqlDbType.Decimal, item.NF_ICMS_VCREICMSSN))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMSDESON", SqlDbType.Decimal, item.NF_ICMS_VICMSDESON))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMSOP", SqlDbType.Decimal, item.NF_ICMS_VICMSOP))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PDIF", SqlDbType.Decimal, item.NF_ICMS_PDIF))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMSDIF", SqlDbType.Decimal, item.NF_ICMS_VICMSDIF))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_CLENQ", SqlDbType.VarChar, item.NF_IPI_CLENQ))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_CNPJPROD", SqlDbType.VarChar, item.NF_IPI_CNPJPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_CSELO", SqlDbType.VarChar, item.NF_IPI_CSELO))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_QSELO", SqlDbType.Decimal, item.NF_IPI_QSELO))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_CENQ", SqlDbType.VarChar, item.NF_IPI_CENQ))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_CST", SqlDbType.VarChar, item.NF_IPI_CST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_VBC", SqlDbType.Decimal, item.NF_IPI_VBC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_PIPI", SqlDbType.Decimal, item.NF_IPI_PIPI))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_VIPI", SqlDbType.Decimal, item.NF_IPI_VIPI))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_QUNID", SqlDbType.Decimal, item.NF_IPI_QUNID))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_VUNID", SqlDbType.Decimal, item.NF_IPI_VUNID))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_II_VBC", SqlDbType.Decimal, item.NF_II_VBC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_II_VDESPADU", SqlDbType.Decimal, item.NF_II_VDESPADU))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_II_VII", SqlDbType.Decimal, item.NF_II_VII))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_II_VIOF", SqlDbType.Decimal, item.NF_II_VIOF))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_CST", SqlDbType.VarChar, item.NF_PIS_CST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_VBC", SqlDbType.Decimal, item.NF_PIS_VBC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_PPIS", SqlDbType.Decimal, item.NF_PIS_PPIS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_VPIS", SqlDbType.Decimal, item.NF_PIS_VPIS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_QBCPROD", SqlDbType.Decimal, item.NF_PIS_QBCPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_VALIQPROD", SqlDbType.Decimal, item.NF_PIS_VALIQPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PISST_VBC", SqlDbType.Decimal, item.NF_PISST_VBC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PISST_PPIS", SqlDbType.Decimal, item.NF_PISST_PPIS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PISST_VPIS", SqlDbType.Decimal, item.NF_PISST_VPIS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PISST_QBCPROD", SqlDbType.Decimal, item.NF_PISST_QBCPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_PISST_VALIQPROD", SqlDbType.Decimal, item.NF_PISST_VALIQPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_CST", SqlDbType.VarChar, item.NF_COFINS_CST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_VBC", SqlDbType.Decimal, item.NF_COFINS_VBC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_PCOFINS", SqlDbType.Decimal, item.NF_COFINS_PCOFINS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_VCOFINS", SqlDbType.Decimal, item.NF_COFINS_VCOFINS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_QBCPROD", SqlDbType.Decimal, item.NF_COFINS_QBCPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_VALIQPROD", SqlDbType.Decimal, item.NF_COFINS_VALIQPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINSST_VBC", SqlDbType.Decimal, item.NF_COFINSST_VBC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINSST_PCOFINS", SqlDbType.Decimal, item.NF_COFINSST_PCOFINS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINSST_VCOFINS", SqlDbType.Decimal, item.NF_COFINSST_VCOFINS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINSST_QBCPROD", SqlDbType.Decimal, item.NF_COFINSST_QBCPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_COFINSST_VALIQPROD", SqlDbType.Decimal, item.NF_COFINSST_VALIQPROD))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VBC", SqlDbType.Decimal, item.NF_ISSQN_VBC))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VALIQ", SqlDbType.Decimal, item.NF_ISSQN_VALIQ))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VISSQN", SqlDbType.Decimal, item.NF_ISSQN_VISSQN))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_CMUNFG", SqlDbType.VarChar, item.NF_ISSQN_CMUNFG))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_CLISTSERV", SqlDbType.VarChar, item.NF_ISSQN_CLISTSERV))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VDEDUCAO", SqlDbType.Decimal, item.NF_ISSQN_VDEDUCAO))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VOUTRO", SqlDbType.Decimal, item.NF_ISSQN_VOUTRO))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VDESCINCOND", SqlDbType.Decimal, item.NF_ISSQN_VDESCINCOND))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VDESCCOND", SqlDbType.Decimal, item.NF_ISSQN_VDESCCOND))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VISSRET", SqlDbType.Decimal, item.NF_ISSQN_VISSRET))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_INDISS", SqlDbType.Int, item.NF_ISSQN_INDISS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_CSERVICO", SqlDbType.VarChar, item.NF_ISSQN_CSERVICO))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_CMUN", SqlDbType.VarChar, item.NF_ISSQN_CMUN))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_CPAIS", SqlDbType.VarChar, item.NF_ISSQN_CPAIS))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_NPROCESSO", SqlDbType.VarChar, item.NF_ISSQN_NPROCESSO))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_INDINCENTIVO", SqlDbType.Int, item.NF_ISSQN_INDINCENTIVO))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_VBCUFDEST", SqlDbType.Decimal, item.NF_ICMSUFDEST_VBCUFDEST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_PFCPUDEST", SqlDbType.Decimal, item.NF_ICMSUFDEST_PFCPUDEST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_PICMSUFDEST", SqlDbType.Decimal, item.NF_ICMSUFDEST_PICMSUFDEST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_PICMSINTER", SqlDbType.Decimal, item.NF_ICMSUFDEST_PICMSINTER))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_PICMSINTERPART", SqlDbType.Decimal, item.NF_ICMSUFDEST_PICMSINTERPART))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_VFCPUFDEST", SqlDbType.Decimal, item.NF_ICMSUFDEST_VFCPUFDEST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_VICMSUFDEST", SqlDbType.Decimal, item.NF_ICMSUFDEST_VICMSUFDEST))
                    sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_VICMSUFREMET", SqlDbType.Decimal, item.NF_ICMSUFDEST_VICMSUFREMET))

                    modSQL.ExecuteNonQueryParams(strInsertTbDoc_Item_Nfe, sqlparams)

                    intIndexItemNF += 1
                Next

            End If
        Catch ex As Exception
            'RegistrarNotaProblema(NotaFiscal.VNF_CHAVE_ACESSO, ex.Message, "GravarDadosNFS") 'Marcio Spinosa - CR00009259
            Throw New Exception("Ocorreu um erro ao gravar os dados desta nota fiscal")
        End Try
    End Sub
#End Region

#Region " GravarDadosNF "
    Public Sub GravarDadosNF(ByVal NotaFiscal As modNF, ByVal IsValidarNF As Boolean, ByVal Usuario As String)
        Try
            Dim intIndexItemNF As Integer = 0
            If (NotaFiscal Is Nothing) Then
                NotaFiscal = objNF
            End If

            If (IsValidarNF) Then
                modSQL.CommandText = "exec dbo.SP_DELETE_DOC_INFORMATION '" & NotaFiscal.VNF_CHAVE_ACESSO & "'"
                modSQL.ExecuteNonQuery(modSQL.CommandText)
            End If

            Dim sqlparams As New List(Of SqlClient.SqlParameter)
            '--> SE FOR UMA NOTA DE TALONÁRIO, É PRECISO VERIFICAR SE JÁ FOI INSERIDA NA TABELA NOTF
            If (NotaFiscal.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) Then
                modSQL.CommandText = "select count(*) from TbNFE where nfeid = '" & NotaFiscal.VNF_CHAVE_ACESSO & "'"
                If (Convert.ToInt32(modSQL.ExecuteScalar(modSQL.CommandText)) = 0) Then
                    sqlparams = New List(Of SqlClient.SqlParameter)
                    sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                    sqlparams.Add(modSQL.AddSqlParameter("NFEVAL", SqlDbType.VarChar, "S"))
                    sqlparams.Add(modSQL.AddSqlParameter("NFECAN", SqlDbType.VarChar, "N"))
                    sqlparams.Add(modSQL.AddSqlParameter("ID_LISTA", SqlDbType.Int, 0))
                    sqlparams.Add(modSQL.AddSqlParameter("DATVAL", SqlDbType.DateTime, DateTime.Now))
                    sqlparams.Add(modSQL.AddSqlParameter("NFEREL", SqlDbType.VarChar, "N"))
                    sqlparams.Add(modSQL.AddSqlParameter("USUCAN", SqlDbType.VarChar, String.Empty))
                    sqlparams.Add(modSQL.AddSqlParameter("JUNAUT", SqlDbType.VarChar, "N"))
                    sqlparams.Add(modSQL.AddSqlParameter("SITUACAO", SqlDbType.VarChar, NotaFiscal.VNF_STATUS))
                    sqlparams.Add(modSQL.AddSqlParameter("REPROCESSAR", SqlDbType.VarChar, "N"))
                    sqlparams.Add(modSQL.AddSqlParameter("CNPJ_METSO", SqlDbType.VarChar, String.Empty))
                    sqlparams.Add(modSQL.AddSqlParameter("CONTINGENCIA", SqlDbType.VarChar, "0"))
                    sqlparams.Add(modSQL.AddSqlParameter("SAP_STATUS_INTEGRACAO", SqlDbType.VarChar, "PENDENTE"))
                    modSQL.ExecuteNonQueryParams(strInsertTbNFE, sqlparams)
                End If
            End If

            '--> VERIFICAR SE OS DADOS DESSE DOCUMENTO JÁ NÃO FORAM INSERIDOS NA TABELA
            modSQL.CommandText = "select count(*) from TbDOC_CAB where nfeid = '" & NotaFiscal.VNF_CHAVE_ACESSO & "'"
            If (Convert.ToInt32(modSQL.ExecuteScalar(modSQL.CommandText)) = 0) Then

                '--> CAMPOS CALCULADOS PELO VNF
                sqlparams = New List(Of SqlClient.SqlParameter)
                sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_TIPO_DOCUMENTO", SqlDbType.VarChar, NotaFiscal.VNF_TIPO_DOCUMENTO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_CONTEUDO_XML", SqlDbType.VarChar, NotaFiscal.VNF_CONTEUDO_XML))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_DANFE_ONLINE", SqlDbType.VarChar, NotaFiscal.VNF_DANFE_ONLINE))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_DATA_INSERT", SqlDbType.DateTime, DateTime.Now))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_USUARIO_INSERT", SqlDbType.VarChar, Usuario))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_ANEXO", SqlDbType.VarBinary, NotaFiscal.VNF_ANEXO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_ANEXO_NOME", SqlDbType.VarChar, NotaFiscal.VNF_ANEXO_NOME))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_ANEXO_EXTENSAO", SqlDbType.VarChar, NotaFiscal.VNF_ANEXO_EXTENSAO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_CODIGO_VERIFICACAO", SqlDbType.VarChar, NotaFiscal.VNF_CODIGO_VERIFICACAO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_MATERIAL_RECEBIDO", SqlDbType.Bit, NotaFiscal.VNF_MATERIAL_RECEBIDO))
                sqlparams.Add(modSQL.AddSqlParameter("VNF_CLASSIFICACAO", SqlDbType.VarChar, NotaFiscal.VNF_CLASSIFICACAO))
                modSQL.ExecuteNonQueryParams(strInsertTbDoc_Cab, sqlparams)

                '--> CAMPOS RETORNADOS PELO SAP
                sqlparams = New List(Of SqlClient.SqlParameter)
                sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_PO_NUMBER", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.PO_NUMBER))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_CREATED_ON", SqlDbType.DateTime, NotaFiscal.SAP_DETAILS.CREATED_ON))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_CREATED_BY", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.CREATED_BY))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_PAYMENT_TERMS", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.PAYMENT_TERMS))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_PURCHASING_GROUP", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.PURCHASING_GROUP))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_CURRENCY", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.CURRENCY))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_VENDOR_CODE", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.VENDOR_CODE))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_VENDOR_NAME", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.VENDOR_NAME))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_VENDOR_CNPJ", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.VENDOR_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_INCOTERMS1", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.INCOTERMS1))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_VERSION_COMPLETE", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.VERSION_COMPLETE))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_METSO_CNPJ", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.METSO_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("SAP_RELEASE_INDIC", SqlDbType.VarChar, NotaFiscal.SAP_DETAILS.RELEASE_INDIC))
                modSQL.ExecuteNonQueryParams(strInsertTbDoc_Cab_Sap, sqlparams)

                '--> CAMPOS DE CABEÇALHO DA NFE
                sqlparams = New List(Of SqlClient.SqlParameter)
                sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_OUTROS_SIGNATURE", SqlDbType.Bit, NotaFiscal.NF_OUTROS_SIGNATURE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_OUTROS_INFORMACAO_ADICIONAL", SqlDbType.VarChar, NotaFiscal.NF_OUTROS_INFORMACAO_ADICIONAL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_OUTROS_VERSAO", SqlDbType.VarChar, NotaFiscal.NF_OUTROS_VERSAO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_OUTROS_STATUS_CODE", SqlDbType.VarChar, NotaFiscal.NF_OUTROS_STATUS_CODE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_OUTROS_STATUS_DESC", SqlDbType.VarChar, NotaFiscal.NF_OUTROS_STATUS_DESC))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_MODFRETE", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_MODFRETE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_XNOME", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_XNOME))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_CNPJ", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_IE", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_IE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_XENDER", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_XENDER))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_XMUN", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_XMUN))
                sqlparams.Add(modSQL.AddSqlParameter("NF_TRANSP_UF", SqlDbType.VarChar, NotaFiscal.NF_TRANSP_UF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_CUF", SqlDbType.Int, NotaFiscal.NF_IDE_CUF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_CNF", SqlDbType.Int, NotaFiscal.NF_IDE_CNF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_INDPAG", SqlDbType.Int, NotaFiscal.NF_IDE_INDPAG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_NATOP", SqlDbType.VarChar, NotaFiscal.NF_IDE_NATOP))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_MOD", SqlDbType.Int, NotaFiscal.NF_IDE_MOD))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_SERIE", SqlDbType.VarChar, NotaFiscal.NF_IDE_SERIE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_NNF", SqlDbType.VarChar, NotaFiscal.NF_IDE_NNF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_DHEMI", SqlDbType.DateTime, NotaFiscal.NF_IDE_DHEMI))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_TPNF", SqlDbType.Int, NotaFiscal.NF_IDE_TPNF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_IDDEST", SqlDbType.Int, NotaFiscal.NF_IDE_IDDEST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_CMUNFG", SqlDbType.VarChar, NotaFiscal.NF_IDE_CMUNFG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_TPEMISS", SqlDbType.Int, NotaFiscal.NF_IDE_TPEMISS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_TPAMB", SqlDbType.Int, NotaFiscal.NF_IDE_TPAMB))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_FINNFE", SqlDbType.Int, NotaFiscal.NF_IDE_FINNFE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_INDFINAL", SqlDbType.Int, NotaFiscal.NF_IDE_INDFINAL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_INDPRES", SqlDbType.Int, NotaFiscal.NF_IDE_INDPRES))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_PROCEMI", SqlDbType.Int, NotaFiscal.NF_IDE_PROCEMI))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_DHCONT", SqlDbType.VarChar, NotaFiscal.NF_IDE_DHCONT))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_XJUST", SqlDbType.VarChar, NotaFiscal.NF_IDE_XJUST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_NFREF", SqlDbType.VarChar, NotaFiscal.NF_IDE_NFREF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_IDE_MODAL", SqlDbType.VarChar, NotaFiscal.NF_IDE_MODAL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CNPJ", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_XNOME", SqlDbType.VarChar, NotaFiscal.NF_EMIT_XNOME))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_XLGR", SqlDbType.VarChar, NotaFiscal.NF_EMIT_XLGR))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_NRO", SqlDbType.VarChar, NotaFiscal.NF_EMIT_NRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_XCPL", SqlDbType.VarChar, NotaFiscal.NF_EMIT_XCPL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_XBAIRRO", SqlDbType.VarChar, NotaFiscal.NF_EMIT_XBAIRRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CMUN", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CMUN))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_UF", SqlDbType.VarChar, NotaFiscal.NF_EMIT_UF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CEP", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CEP))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CPAIS", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CPAIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_XPAIS", SqlDbType.VarChar, NotaFiscal.NF_EMIT_XPAIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_FONE", SqlDbType.VarChar, NotaFiscal.NF_EMIT_FONE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_IE", SqlDbType.VarChar, NotaFiscal.NF_EMIT_IE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_IEST", SqlDbType.VarChar, NotaFiscal.NF_EMIT_IEST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_IM", SqlDbType.VarChar, NotaFiscal.NF_EMIT_IM))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CNAE", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CNAE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_EMIT_CRT", SqlDbType.VarChar, NotaFiscal.NF_EMIT_CRT))
                sqlparams.Add(modSQL.AddSqlParameter("NF_REM_XNOME", SqlDbType.VarChar, NotaFiscal.NF_REM_XNOME))
                sqlparams.Add(modSQL.AddSqlParameter("NF_REM_CNPJ", SqlDbType.VarChar, NotaFiscal.NF_REM_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_REM_CMUN", SqlDbType.VarChar, NotaFiscal.NF_REM_CMUN))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XNOME", SqlDbType.VarChar, NotaFiscal.NF_DEST_XNOME))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_CNPJ", SqlDbType.VarChar, NotaFiscal.NF_DEST_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XLGR", SqlDbType.VarChar, NotaFiscal.NF_DEST_XLGR))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_NRO", SqlDbType.VarChar, NotaFiscal.NF_DEST_NRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XCPL", SqlDbType.VarChar, NotaFiscal.NF_DEST_XCPL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XBAIRRO", SqlDbType.VarChar, NotaFiscal.NF_DEST_XBAIRRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_CMUN", SqlDbType.VarChar, NotaFiscal.NF_DEST_CMUN))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XMUN", SqlDbType.VarChar, NotaFiscal.NF_DEST_XMUN))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_UF", SqlDbType.VarChar, NotaFiscal.NF_DEST_UF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_CEP", SqlDbType.VarChar, NotaFiscal.NF_DEST_CEP))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_CPAIS", SqlDbType.VarChar, NotaFiscal.NF_DEST_CPAIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_XPAIS", SqlDbType.VarChar, NotaFiscal.NF_DEST_XPAIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_FONE", SqlDbType.VarChar, NotaFiscal.NF_DEST_FONE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_INDIEDEST", SqlDbType.VarChar, NotaFiscal.NF_DEST_INDIEDEST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_IE", SqlDbType.VarChar, NotaFiscal.NF_DEST_IE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_ISUF", SqlDbType.VarChar, NotaFiscal.NF_DEST_ISUF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_DEST_IM", SqlDbType.VarChar, NotaFiscal.NF_DEST_IM))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VBC", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VBC))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VICMS", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VICMS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VBCST", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VBCST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VST", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VPROD", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VPROD))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VFRETE", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VFRETE))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VSEG", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VSEG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VDESC", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VDESC))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VII", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VII))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VIPI", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VIPI))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VPIS", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VPIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VCOFINS", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VCOFINS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VOUTRO", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VOUTRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VNF", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VNF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VTOTTRIB", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VTOTTRIB))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VICMSDESON", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VICMSDESON))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VICMSUFDEST", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VICMSUFDEST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VICMSUFREMET", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VICMSUFREMET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSTOT_VFCPUFDEST", SqlDbType.Decimal, NotaFiscal.NF_ICMSTOT_VFCPUFDEST))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VSERV", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VSERV))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VBC", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VBC))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VISS", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VISS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VPIS", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VPIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VCOFINS", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VCOFINS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_DTCOMPET", SqlDbType.VarChar, NotaFiscal.NF_ISSQNTOT_DTCOMPET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VDEDUCAO", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VDEDUCAO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VOUTRO", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VOUTRO))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VDESCINCOD", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VDESCINCOD))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VDESCCOND", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VDESCCOND))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_VISSRET", SqlDbType.Decimal, NotaFiscal.NF_ISSQNTOT_VISSRET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQNTOT_CREGTRIB", SqlDbType.VarChar, NotaFiscal.NF_ISSQNTOT_CREGTRIB))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VRETPIS", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VRETPIS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VRETCOFINS", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VRETCOFINS))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VRETCSLL", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VRETCSLL))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VBCIRRF", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VBCIRRF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VIRRF", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VIRRF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VBCRETPREV", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VBCRETPREV))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRIN_VRETPREV", SqlDbType.Decimal, NotaFiscal.NF_RETTRIN_VRETPREV))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_VSERV", SqlDbType.Decimal, NotaFiscal.NF_RETTRANSP_VSERV))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_VBCRET", SqlDbType.Decimal, NotaFiscal.NF_RETTRANSP_VBCRET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_PICMSRET", SqlDbType.Decimal, NotaFiscal.NF_RETTRANSP_PICMSRET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_VICMSRET", SqlDbType.Decimal, NotaFiscal.NF_RETTRANSP_VICMSRET))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_CFOP", SqlDbType.Int, NotaFiscal.NF_RETTRANSP_CFOP))
                sqlparams.Add(modSQL.AddSqlParameter("NF_RETTRANSP_CMUNFG", SqlDbType.VarChar, NotaFiscal.NF_RETTRANSP_CMUNFG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_NFAT", SqlDbType.VarChar, NotaFiscal.NF_COBR_NFAT))
                sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_VORIG", SqlDbType.Decimal, NotaFiscal.NF_COBR_VORIG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_VDESC", SqlDbType.Decimal, NotaFiscal.NF_COBR_VDESC))
                sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_VLIQ", SqlDbType.Decimal, NotaFiscal.NF_COBR_VLIQ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_TPAG", SqlDbType.VarChar, NotaFiscal.NF_PAG_TPAG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_VPAG", SqlDbType.Decimal, NotaFiscal.NF_PAG_VPAG))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_CNPJ", SqlDbType.VarChar, NotaFiscal.NF_PAG_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_TBAND", SqlDbType.VarChar, NotaFiscal.NF_PAG_TBAND))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_CAUT", SqlDbType.VarChar, NotaFiscal.NF_PAG_CAUT))
                sqlparams.Add(modSQL.AddSqlParameter("NF_PAG_TPINTEGRA", SqlDbType.VarChar, NotaFiscal.NF_PAG_TPINTEGRA))
                sqlparams.Add(modSQL.AddSqlParameter("NF_NFREF_REFNNF", SqlDbType.VarChar, NotaFiscal.NF_NFREF_REFNNF))
                sqlparams.Add(modSQL.AddSqlParameter("NF_NFREF_REFSerie", SqlDbType.VarChar, NotaFiscal.NF_NFREF_REFSerie))
                sqlparams.Add(modSQL.AddSqlParameter("NF_NFREF_REFDHEMI", SqlDbType.DateTime, NotaFiscal.NF_NFREF_REFDHEMI))
                modSQL.ExecuteNonQueryParams(strInsertTbDoc_Cab_Nfe, sqlparams)

                '--> DUPLICATAS DA NFE
                If Not NotaFiscal.DUPLICATAS Is Nothing Then
                    For Each duplicata As modNFDuplicata In NotaFiscal.DUPLICATAS
                        sqlparams = New List(Of SqlClient.SqlParameter)
                        sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_DUP_NDUP", SqlDbType.VarChar, duplicata.NF_COBR_DUP_NDUP))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_DUP_DVENC", SqlDbType.DateTime, duplicata.NF_COBR_DUP_DVENC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COBR_DUP_VDUP", SqlDbType.Decimal, duplicata.NF_COBR_DUP_VDUP))
                        modSQL.ExecuteNonQueryParams(strInsertTbDoc_Cab_Dup, sqlparams)
                    Next
                End If


                '--> NOTAS REFERENCIADAS DA NFE
                If Not NotaFiscal.NF_REFERENCIADAS Is Nothing Then
                    For Each notas_referenciadas As modNFReferenciada In NotaFiscal.NF_REFERENCIADAS
                        sqlparams = New List(Of SqlClient.SqlParameter)
                        sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_NFREF_REFNFE", SqlDbType.VarChar, notas_referenciadas.NF_NFREF_REFNFE))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_NFREF_REFCTE", SqlDbType.VarChar, notas_referenciadas.NF_NFREF_REFCTE))
                        modSQL.ExecuteNonQueryParams(strInsertTbDoc_Cab_Ref, sqlparams)
                    Next
                End If


                '--> CAMPOS DE CABEÇALHO DO CTE
                sqlparams = New List(Of SqlClient.SqlParameter)
                sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                sqlparams.Add(modSQL.AddSqlParameter("CT_IDE_CMUNINI", SqlDbType.VarChar, NotaFiscal.CT_IDE_CMUNINI))
                sqlparams.Add(modSQL.AddSqlParameter("CT_IDE_XMUNINI", SqlDbType.VarChar, NotaFiscal.CT_IDE_XMUNINI))
                sqlparams.Add(modSQL.AddSqlParameter("CT_IDE_UFINI", SqlDbType.VarChar, NotaFiscal.CT_IDE_UFINI))
                sqlparams.Add(modSQL.AddSqlParameter("CT_IDE_CMUNFIM", SqlDbType.VarChar, NotaFiscal.CT_IDE_CMUNFIM))
                sqlparams.Add(modSQL.AddSqlParameter("CT_IDE_XMUNFIM", SqlDbType.VarChar, NotaFiscal.CT_IDE_XMUNFIM))
                sqlparams.Add(modSQL.AddSqlParameter("CT_IDE_UFFIM", SqlDbType.VarChar, NotaFiscal.CT_IDE_UFFIM))
                sqlparams.Add(modSQL.AddSqlParameter("CT_IDE_RETIRA", SqlDbType.VarChar, NotaFiscal.CT_IDE_RETIRA))
                sqlparams.Add(modSQL.AddSqlParameter("CT_IDE_TOMA", SqlDbType.VarChar, NotaFiscal.CT_IDE_TOMA))
                sqlparams.Add(modSQL.AddSqlParameter("CT_IDE_TPCTE", SqlDbType.Int, NotaFiscal.CT_IDE_TPCTE))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_CNPJ", SqlDbType.VarChar, NotaFiscal.CT_TOMA_CNPJ))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_IE", SqlDbType.VarChar, NotaFiscal.CT_TOMA_IE))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_XNOME", SqlDbType.VarChar, NotaFiscal.CT_TOMA_XNOME))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_XLGR", SqlDbType.VarChar, NotaFiscal.CT_TOMA_XLGR))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_NRO", SqlDbType.VarChar, NotaFiscal.CT_TOMA_NRO))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_XBAIRRO", SqlDbType.VarChar, NotaFiscal.CT_TOMA_XBAIRRO))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_CMUN", SqlDbType.VarChar, NotaFiscal.CT_TOMA_CMUN))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_XMUN", SqlDbType.VarChar, NotaFiscal.CT_TOMA_XMUN))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_CEP", SqlDbType.VarChar, NotaFiscal.CT_TOMA_CEP))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_UF", SqlDbType.VarChar, NotaFiscal.CT_TOMA_UF))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_CPAIS", SqlDbType.VarChar, NotaFiscal.CT_TOMA_CPAIS))
                sqlparams.Add(modSQL.AddSqlParameter("CT_TOMA_XPAIS", SqlDbType.VarChar, NotaFiscal.CT_TOMA_XPAIS))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_VTPREST", SqlDbType.Decimal, NotaFiscal.CT_VPREST_VTPREST))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_VREC", SqlDbType.Decimal, NotaFiscal.CT_VPREST_VREC))
                sqlparams.Add(modSQL.AddSqlParameter("CT_INFCARGA_VCARGA", SqlDbType.Decimal, NotaFiscal.CT_INFCARGA_VCARGA))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_FRETE_PESO", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_FRETE_PESO))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_FRETE_VALOR", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_FRETE_VALOR))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_SEC_CAT", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_SEC_CAT))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_ADEME", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_ADEME))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_PEDAGIO", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_PEDAGIO))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_GRIS", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_GRIS))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_TAXAEMICTRC", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_TAXAEMICTRC))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_COLETAENTREGA", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_COLETAENTREGA))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_OUTROSVALORES", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_OUTROSVALORES))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_FRETE", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_FRETE))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_DESCONTO", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_DESCONTO))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_DESPACHO", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_DESPACHO))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_ENTREGA", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_ENTREGA))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_OUTROS", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_OUTROS))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_ESCOLTA", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_ESCOLTA))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_COLETA", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_COLETA))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_SEGURO", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_SEGURO))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_PERNOITE", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_PERNOITE))
                sqlparams.Add(modSQL.AddSqlParameter("CT_VPREST_COMP_REDESPACHO", SqlDbType.Decimal, NotaFiscal.CT_VPREST_COMP_REDESPACHO))
                modSQL.ExecuteNonQueryParams(strInsertTbDoc_Cab_Cte, sqlparams)

                intIndexItemNF = 0
                For Each item As modNFItem In NotaFiscal.ITENS_NF
                    modSQL.CommandText = "select count(*) from TbJUN where nfeid = '" & NotaFiscal.VNF_CHAVE_ACESSO & "' and ITENFE = " & item.NF_PROD_ITEM
                    If (Convert.ToInt32(modSQL.ExecuteScalar(modSQL.CommandText)) = 0) Then
                        sqlparams = New List(Of SqlClient.SqlParameter)
                        sqlparams.Add(modSQL.AddSqlParameter("PEDCOM", SqlDbType.VarChar, item.NF_PROD_XPED.RemoveLetters()))
                        sqlparams.Add(modSQL.AddSqlParameter("ITEPED", SqlDbType.Int, item.NF_PROD_NITEMPED))
                        sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                        sqlparams.Add(modSQL.AddSqlParameter("ITENFE", SqlDbType.Int, item.NF_PROD_ITEM))
                        sqlparams.Add(modSQL.AddSqlParameter("ITEVAL", SqlDbType.VarChar, item.VNF_ITEM_VALIDO))
                        sqlparams.Add(modSQL.AddSqlParameter("CODUSU", SqlDbType.VarChar, Usuario))
                        sqlparams.Add(modSQL.AddSqlParameter("CODCOM", SqlDbType.VarChar, objNF.SAP_DETAILS.PURCHASING_GROUP))
                        sqlparams.Add(modSQL.AddSqlParameter("MOTIVO", SqlDbType.VarChar, String.Empty))
                        sqlparams.Add(modSQL.AddSqlParameter("NCMNFE", SqlDbType.VarChar, item.NF_PROD_NCM))
                        sqlparams.Add(modSQL.AddSqlParameter("NCMPED", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.NCM_CODE))
                        item.VNF_CODJUN = Convert.ToDecimal(modSQL.ExecuteScalarParams(strInsertTbJun, sqlparams))
                    Else
                        item.VNF_CODJUN = Convert.ToInt32(modSQL.ExecuteScalar("select codjun from tbjun where nfeid = '" & NotaFiscal.VNF_CHAVE_ACESSO & "'  and ITENFE = " & item.NF_PROD_ITEM))
                        modSQL.ExecuteNonQuery("UPDATE TbJUN SET ITEVAL = '" & item.VNF_ITEM_VALIDO & "' WHERE CODJUN = " & item.VNF_CODJUN)
                    End If

                    '--> CAMPOS CALCULADOS PELO VNF
                    If ((NotaFiscal.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe Or NotaFiscal.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) OrElse intIndexItemNF = 0) Then
                        sqlparams = New List(Of SqlClient.SqlParameter)
                        sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                        sqlparams.Add(modSQL.AddSqlParameter("VNF_CODJUN", SqlDbType.Decimal, item.VNF_CODJUN))
                        sqlparams.Add(modSQL.AddSqlParameter("VNF_ITEM_VALIDO", SqlDbType.VarChar, item.VNF_ITEM_VALIDO))
                        sqlparams.Add(modSQL.AddSqlParameter("VNF_CONFIRMADO_PORTAL", SqlDbType.Bit, item.VNF_CONFIRMADO_PORTAL))
                        sqlparams.Add(modSQL.AddSqlParameter("VNF_ID_MODO_PROCESSO", SqlDbType.Int, IIf(item.VNF_ID_MODO_PROCESSO = 0, Nothing, item.VNF_ID_MODO_PROCESSO)))
                        sqlparams.Add(modSQL.AddSqlParameter("VNF_INBOUND_DELIVERY_NUMBER", SqlDbType.VarChar, item.VNF_INBOUND_DELIVERY_NUMBER))
                        sqlparams.Add(modSQL.AddSqlParameter("VNF_INBOUND_DELIVERY_ITEM_NUMBER", SqlDbType.Int, item.VNF_INBOUND_DELIVERY_ITEM_NUMBER))
                        modSQL.ExecuteNonQueryParams(strInsertTbDoc_Item, sqlparams)
                    End If

                    '--> CAMPOS RETORNADOS PELO SAP
                    If ((NotaFiscal.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe Or NotaFiscal.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) OrElse intIndexItemNF = 0) Then
                        sqlparams = New List(Of SqlClient.SqlParameter)
                        sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_PO_NUMBER", SqlDbType.VarChar, item.SAP_PO_NUMBER))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_ITEM_NUMBER", SqlDbType.Int, item.SAP_ITEM_DETAILS.ITEM_NUMBER))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_MATERIAL", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.MATERIAL))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_PO_QUANTITY", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.PO_QUANTITY))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_UNIT_OF_MEASURE", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.UNIT_OF_MEASURE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_PRICE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NET_PRICE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_TAX_CODE", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.TAX_CODE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_DELIVERY_COMPLETED", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.DELIVERY_COMPLETED))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_FINAL_INVOICE", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.FINAL_INVOICE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NCM_CODE", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.NCM_CODE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_PLANT", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.PLANT))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_PRICE_UNIT", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.PRICE_UNIT))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_DELETION_INDICATOR", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.DELETION_INDICATOR))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_MATERIAL_DESCRIPTION", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.MATERIAL_DESCRIPTION))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_PO_ITEM_SHORT_TEXT", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.PO_ITEM_SHORT_TEXT))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_CONFIRMATION_CONTROL_KEY", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.CONFIRMATION_CONTROL_KEY))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_OVERDELIVERY_TOLERANCE_LIMIT", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.OVERDELIVERY_TOLERANCE_LIMIT))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_UNLIMITED_OVERDELIVERY_ALLOWED", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.UNLIMITED_OVERDELIVERY_ALLOWED))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_UNDERDELIVERY_TOLERANCE_LIMIT", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.UNDERDELIVERY_TOLERANCE_LIMIT))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_OPEN_QUANTITY", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.OPEN_QUANTITY))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_STORAGE_LOCATION", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.STORAGE_LOCATION))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_ACCOUNT_ASSIGNMENT_CATEGORY", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.ACCOUNT_ASSIGNMENT_CATEGORY))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_ITEM_CATEGORY", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.ITEM_CATEGORY))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_GOODS_RECEIPT", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.GOODS_RECEIPT))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_INVOICE_RECEIPT", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.INVOICE_RECEIPT))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_NET_ITEM_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_ITEM_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_ITEM_VALUE_WITH_TAXES", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_TOTAL_ITEM", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_TOTAL_ITEM))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_NET_FREIGHT_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_NET_FREIGHT_VALUE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_FREIGHT_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_NET_FREIGHT_VALUE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_NET_INSURANCE_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_NET_INSURANCE_VALUE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_INSURANCE_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_NET_INSURANCE_VALUE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_NET_OTHER_EXPENSES_VALUES", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_NET_OTHER_EXPENSES_VALUES))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_OTHER_EXPENSES_VALUES", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_NET_OTHER_EXPENSES_VALUES))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NF_NET_DISCOUNT_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.NF_NET_DISCOUNT_VALUE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_NET_DISCOUNT_VALUE", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_NET_DISCOUNT_VALUE))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_DISCOUNT_VALUE_WITH_TAXES", SqlDbType.Decimal, item.SAP_ITEM_DETAILS.SAP_DISCOUNT_VALUE_WITH_TAXES))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_ITEM_CFOP", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.SAP_ITEM_CFOP))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_CROSS_PLANT_MATERIAL_STATUS", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.CROSS_PLANT_MATERIAL_STATUS))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_PLANT_MATERIAL_STATUS", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.PLANT_MATERIAL_STATUS))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_CROSS_DISTRIBUTION_CHAIN_STATUS", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.CROSS_DISTRIBUTION_CHAIN_STATUS))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_DELETION_PLANT_LEVEL", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.DELETION_PLANT_LEVEL))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_DELETION_CLIENT_LEVEL", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.DELETION_CLIENT_LEVEL))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_DELETION_STORAGE_LOCATION", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.DELETION_STORAGE_LOCATION))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_USAGE_MATERIAL", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.USAGE_MATERIAL))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_ORIGIN_MATERIAL", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.ORIGIN_MATERIAL))
                        sqlparams.Add(modSQL.AddSqlParameter("SAP_TAX_SPLIT", SqlDbType.VarChar, item.SAP_ITEM_DETAILS.TAX_SPLIT))
                        modSQL.ExecuteNonQueryParams(strInsertTbDoc_Item_Sap, sqlparams)
                    End If

                    '--> CAMPOS DE ITENS DA NFE
                    If ((NotaFiscal.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe Or NotaFiscal.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) OrElse intIndexItemNF = 0) Then
                        sqlparams = New List(Of SqlClient.SqlParameter)
                        sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_ITEM", SqlDbType.Int, item.NF_PROD_ITEM))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_CPROD", SqlDbType.VarChar, item.NF_PROD_CPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_CEAN", SqlDbType.VarChar, item.NF_PROD_CEAN))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_XPROD", SqlDbType.VarChar, item.NF_PROD_XPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_NCM", SqlDbType.VarChar, item.NF_PROD_NCM))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_CFOP", SqlDbType.VarChar, item.NF_PROD_CFOP))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_CFOP_DESC", SqlDbType.VarChar, item.NF_PROD_CFOP_DESC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_UCOM", SqlDbType.VarChar, item.NF_PROD_UCOM))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_QCOM", SqlDbType.Float, item.NF_PROD_QCOM))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VUNCOM", SqlDbType.Float, item.NF_PROD_VUNCOM))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VPROD", SqlDbType.Float, item.NF_PROD_VPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_UTRIB", SqlDbType.VarChar, item.NF_PROD_UTRIB))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_QTRIB", SqlDbType.Float, item.NF_PROD_QTRIB))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VDESC", SqlDbType.Float, item.NF_PROD_VDESC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_INF_ADICIONAL_ITEM", SqlDbType.VarChar, item.NF_PROD_INF_ADICIONAL_ITEM))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_NVE", SqlDbType.VarChar, item.NF_PROD_NVE))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_EXTIPI", SqlDbType.VarChar, item.NF_PROD_EXTIPI))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VFRETE", SqlDbType.Decimal, item.NF_PROD_VFRETE))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VSEG", SqlDbType.Decimal, item.NF_PROD_VSEG))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_VOUTRO", SqlDbType.Decimal, item.NF_PROD_VOUTRO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_INDTOT", SqlDbType.Int, item.NF_PROD_INDTOT))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_DI", SqlDbType.VarChar, item.NF_PROD_DI))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_DETESPECIFICO", SqlDbType.VarChar, item.NF_PROD_DETESPECIFICO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_XPED", SqlDbType.VarChar, item.NF_PROD_XPED))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_NITEMPED", SqlDbType.Int, item.NF_PROD_NITEMPED))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PROD_FCI", SqlDbType.VarChar, item.NF_PROD_FCI))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PICMS", SqlDbType.Decimal, item.NF_ICMS_PICMS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_ORIG", SqlDbType.VarChar, item.NF_ICMS_ORIG))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_CST", SqlDbType.VarChar, item.NF_ICMS_CST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_MODBC", SqlDbType.Int, item.NF_ICMS_MODBC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PREDBC", SqlDbType.Decimal, item.NF_ICMS_PREDBC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VBC", SqlDbType.Decimal, item.NF_ICMS_VBC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMS", SqlDbType.Decimal, item.NF_ICMS_VICMS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_MODBCST", SqlDbType.Int, item.NF_ICMS_MODBCST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_MVAST", SqlDbType.Decimal, item.NF_ICMS_MVAST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSREDBCST", SqlDbType.Decimal, item.NF_ICMSREDBCST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VBCST", SqlDbType.Decimal, item.NF_ICMS_VBCST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PICMSST", SqlDbType.Decimal, item.NF_ICMS_PICMSST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMSST", SqlDbType.Decimal, item.NF_ICMS_VICMSST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VBCSTRET", SqlDbType.Decimal, item.NF_ICMS_VBCSTRET))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VBCSTDEST", SqlDbType.Decimal, item.NF_ICMS_VBCSTDEST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMSSTDEST", SqlDbType.Decimal, item.NF_ICMS_VICMSSTDEST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_MOTDESICMS", SqlDbType.Int, item.NF_ICMS_MOTDESICMS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PBCOP", SqlDbType.Decimal, item.NF_ICMS_PBCOP))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_UFST", SqlDbType.VarChar, item.NF_ICMS_UFST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PCREDSN", SqlDbType.Decimal, item.NF_ICMS_PCREDSN))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VCREICMSSN", SqlDbType.Decimal, item.NF_ICMS_VCREICMSSN))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMSDESON", SqlDbType.Decimal, item.NF_ICMS_VICMSDESON))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMSOP", SqlDbType.Decimal, item.NF_ICMS_VICMSOP))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_PDIF", SqlDbType.Decimal, item.NF_ICMS_PDIF))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMS_VICMSDIF", SqlDbType.Decimal, item.NF_ICMS_VICMSDIF))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_CLENQ", SqlDbType.VarChar, item.NF_IPI_CLENQ))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_CNPJPROD", SqlDbType.VarChar, item.NF_IPI_CNPJPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_CSELO", SqlDbType.VarChar, item.NF_IPI_CSELO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_QSELO", SqlDbType.Decimal, item.NF_IPI_QSELO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_CENQ", SqlDbType.VarChar, item.NF_IPI_CENQ))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_CST", SqlDbType.VarChar, item.NF_IPI_CST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_VBC", SqlDbType.Decimal, item.NF_IPI_VBC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_PIPI", SqlDbType.Decimal, item.NF_IPI_PIPI))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_VIPI", SqlDbType.Decimal, item.NF_IPI_VIPI))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_QUNID", SqlDbType.Decimal, item.NF_IPI_QUNID))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_IPI_VUNID", SqlDbType.Decimal, item.NF_IPI_VUNID))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_II_VBC", SqlDbType.Decimal, item.NF_II_VBC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_II_VDESPADU", SqlDbType.Decimal, item.NF_II_VDESPADU))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_II_VII", SqlDbType.Decimal, item.NF_II_VII))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_II_VIOF", SqlDbType.Decimal, item.NF_II_VIOF))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_CST", SqlDbType.VarChar, item.NF_PIS_CST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_VBC", SqlDbType.Decimal, item.NF_PIS_VBC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_PPIS", SqlDbType.Decimal, item.NF_PIS_PPIS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_VPIS", SqlDbType.Decimal, item.NF_PIS_VPIS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_QBCPROD", SqlDbType.Decimal, item.NF_PIS_QBCPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PIS_VALIQPROD", SqlDbType.Decimal, item.NF_PIS_VALIQPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PISST_VBC", SqlDbType.Decimal, item.NF_PISST_VBC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PISST_PPIS", SqlDbType.Decimal, item.NF_PISST_PPIS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PISST_VPIS", SqlDbType.Decimal, item.NF_PISST_VPIS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PISST_QBCPROD", SqlDbType.Decimal, item.NF_PISST_QBCPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_PISST_VALIQPROD", SqlDbType.Decimal, item.NF_PISST_VALIQPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_CST", SqlDbType.VarChar, item.NF_COFINS_CST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_VBC", SqlDbType.Decimal, item.NF_COFINS_VBC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_PCOFINS", SqlDbType.Decimal, item.NF_COFINS_PCOFINS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_VCOFINS", SqlDbType.Decimal, item.NF_COFINS_VCOFINS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_QBCPROD", SqlDbType.Decimal, item.NF_COFINS_QBCPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINS_VALIQPROD", SqlDbType.Decimal, item.NF_COFINS_VALIQPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINSST_VBC", SqlDbType.Decimal, item.NF_COFINSST_VBC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINSST_PCOFINS", SqlDbType.Decimal, item.NF_COFINSST_PCOFINS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINSST_VCOFINS", SqlDbType.Decimal, item.NF_COFINSST_VCOFINS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINSST_QBCPROD", SqlDbType.Decimal, item.NF_COFINSST_QBCPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_COFINSST_VALIQPROD", SqlDbType.Decimal, item.NF_COFINSST_VALIQPROD))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VBC", SqlDbType.Decimal, item.NF_ISSQN_VBC))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VALIQ", SqlDbType.Decimal, item.NF_ISSQN_VALIQ))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VISSQN", SqlDbType.Decimal, item.NF_ISSQN_VISSQN))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_CMUNFG", SqlDbType.VarChar, item.NF_ISSQN_CMUNFG))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_CLISTSERV", SqlDbType.VarChar, item.NF_ISSQN_CLISTSERV))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VDEDUCAO", SqlDbType.Decimal, item.NF_ISSQN_VDEDUCAO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VOUTRO", SqlDbType.Decimal, item.NF_ISSQN_VOUTRO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VDESCINCOND", SqlDbType.Decimal, item.NF_ISSQN_VDESCINCOND))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VDESCCOND", SqlDbType.Decimal, item.NF_ISSQN_VDESCCOND))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_VISSRET", SqlDbType.Decimal, item.NF_ISSQN_VISSRET))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_INDISS", SqlDbType.Int, item.NF_ISSQN_INDISS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_CSERVICO", SqlDbType.VarChar, item.NF_ISSQN_CSERVICO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_CMUN", SqlDbType.VarChar, item.NF_ISSQN_CMUN))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_CPAIS", SqlDbType.VarChar, item.NF_ISSQN_CPAIS))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_NPROCESSO", SqlDbType.VarChar, item.NF_ISSQN_NPROCESSO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ISSQN_INDINCENTIVO", SqlDbType.Int, item.NF_ISSQN_INDINCENTIVO))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_VBCUFDEST", SqlDbType.Decimal, item.NF_ICMSUFDEST_VBCUFDEST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_PFCPUDEST", SqlDbType.Decimal, item.NF_ICMSUFDEST_PFCPUDEST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_PICMSUFDEST", SqlDbType.Decimal, item.NF_ICMSUFDEST_PICMSUFDEST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_PICMSINTER", SqlDbType.Decimal, item.NF_ICMSUFDEST_PICMSINTER))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_PICMSINTERPART", SqlDbType.Decimal, item.NF_ICMSUFDEST_PICMSINTERPART))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_VFCPUFDEST", SqlDbType.Decimal, item.NF_ICMSUFDEST_VFCPUFDEST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_VICMSUFDEST", SqlDbType.Decimal, item.NF_ICMSUFDEST_VICMSUFDEST))
                        sqlparams.Add(modSQL.AddSqlParameter("NF_ICMSUFDEST_VICMSUFREMET", SqlDbType.Decimal, item.NF_ICMSUFDEST_VICMSUFREMET))
                        modSQL.ExecuteNonQueryParams(strInsertTbDoc_Item_Nfe, sqlparams)
                    End If

                    '--> CAMPOS DE ITENS DO CTE
                    sqlparams = New List(Of SqlClient.SqlParameter)
                    sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                    sqlparams.Add(modSQL.AddSqlParameter("CT_INFNFE_CHAVE", SqlDbType.VarChar, item.CT_INFNFE_CHAVE))
                    modSQL.ExecuteNonQueryParams(strInsertTbDoc_Item_Cte, sqlparams)

                    '--> NOTAS DE CONSIGNAÇÃO REFERENCIADAS CASO EXISTAM
                    If Not item.VNF_NFREF_CONSIGNACAO Is Nothing Then
                        For Each itemf In item.VNF_NFREF_CONSIGNACAO
                            sqlparams = New List(Of SqlClient.SqlParameter)
                            sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                            sqlparams.Add(modSQL.AddSqlParameter("ITEM_NF", SqlDbType.Int, item.NF_PROD_ITEM))
                            sqlparams.Add(modSQL.AddSqlParameter("NUMERO_REFNF", SqlDbType.BigInt, itemf.NUMERO_REFNF))
                            sqlparams.Add(modSQL.AddSqlParameter("SERIE_REFNF", SqlDbType.Int, itemf.SERIE_REFNF))
                            modSQL.ExecuteNonQueryParams(strInsertTBDOC_CONSIGNACAO_REFNF, sqlparams)
                        Next
                    End If

                    '--> NOTAS DE SUBCONTRATAÇÃO REFERENCIADAS CASO EXISTAM
                    If Not item.VNF_NFREF_SUBCONTRATACAO Is Nothing Then
                        For Each itemf In item.VNF_NFREF_SUBCONTRATACAO
                            sqlparams = New List(Of SqlClient.SqlParameter)
                            sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                            sqlparams.Add(modSQL.AddSqlParameter("ITEM_NF", SqlDbType.Int, item.NF_PROD_ITEM))
                            sqlparams.Add(modSQL.AddSqlParameter("NUMERO_REFNF", SqlDbType.BigInt, itemf.NUMERO_REFNF))
                            sqlparams.Add(modSQL.AddSqlParameter("SERIE_REFNF", SqlDbType.Int, itemf.SERIE_REFNF))
                            sqlparams.Add(modSQL.AddSqlParameter("ITEM_REFNF", SqlDbType.Int, itemf.ITEM_REFNF))
                            modSQL.ExecuteNonQueryParams(strInsertTBDOC_SUBCONTRATACAO_REFNF, sqlparams)
                        Next
                    End If

                    '--> NOTAS COMPLEMENTARES CASO EXISTAM
                    If Not item.VNF_NFREF_NOTA_COMPLEMENTAR Is Nothing Then
                        For Each itemf In item.VNF_NFREF_NOTA_COMPLEMENTAR
                            sqlparams = New List(Of SqlClient.SqlParameter)
                            sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, NotaFiscal.VNF_CHAVE_ACESSO))
                            sqlparams.Add(modSQL.AddSqlParameter("ITEM_NF", SqlDbType.Int, item.NF_PROD_ITEM))
                            sqlparams.Add(modSQL.AddSqlParameter("NUMERO_REFNF", SqlDbType.BigInt, itemf.NUMERO_REFNF))
                            sqlparams.Add(modSQL.AddSqlParameter("SERIE_REFNF", SqlDbType.Int, itemf.SERIE_REFNF))
                            modSQL.ExecuteNonQueryParams(strInsertTBDOC_NOTA_COMPLEMENTAR_REFNF, sqlparams)
                        Next
                    End If

                    intIndexItemNF += 1
                Next

            End If
        Catch ex As Exception
            'RegistrarNotaProblema(NotaFiscal.VNF_CHAVE_ACESSO, ex.Message, "GravarDadosNF")
            Throw New Exception("Ocorreu um erro ao gravar os dados desta nota fiscal: " + ex.Message)
        End Try
    End Sub
#End Region

#Region " EnviarMsgAceita "
    ''' <summary>
    ''' Rotina de envio de e-mail aceita aos fornecedores
    ''' </summary>
    ''' <param name="CHAVE_ACESSO"></param>
    ''' <example>Marcio Spinosa - 27/09/2018 - CR00009259 Ajuste para diferenciar emails de fornecedores de compradores</example>
    Public Sub EnviarMsgAceita(ByVal CHAVE_ACESSO As String)
        Try
            '--> SE JÁ FOI ENVIADA MENSAGEM PARA O FORNECEDOR NO MESMO DIA COM O MESMO STATUS, NÃO ENVIA NOVAMENTE
            'Marcio Spinosa - 27/09/2018 - CR00009259 
            'modSQL.CommandText = "select count(*) from tbMen where nfeid = '" & CHAVE_ACESSO & "' and SITUACAO = 'AUTORIZADA' and datenv > cast('" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' as datetime) - 1"
            modSQL.CommandText = "select count(*) from tbMen where nfeid = '" & CHAVE_ACESSO & "' and SITUACAO = 'AUTORIZADA' and datenv > cast('" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' as datetime) - 1 and TIPOMEN = 'F'"
            'Marcio Spinosa - 27/09/2018 - CR00009259 - Fim
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_002: Função: EnviarMsgAceita.", "Verificando se já não foi enviada alguma mensagem. NFEID:" & CHAVE_ACESSO)
            If Integer.Parse(modSQL.ExecuteScalar(modSQL.CommandText)) > 0 Then
                Return
            End If

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_003: Função: EnviarMsgAceita.", "Chamando função validar para obter dados da nota. NFEID: " & CHAVE_ACESSO)
            Dim strMensagem As String
            If String.IsNullOrEmpty(objNF.NF_IDE_NNF) Then
                objNF = Validar(CHAVE_ACESSO, String.Empty, False, False, TipoProcessamento.Aplicacao, String.Empty, strMensagem)
            End If

            Dim NUMERO As String = ""
            Dim SERIE As String = ""
            Dim CNPJ_EMITENTE As String = ""
            Dim RAZFOR As String = ""
            Dim DT_EMISSAO As String = ""
            Dim SEND_TO As String = ""

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_004: Função: EnviarMsgAceita.", "Atribuindo valores recuperados as variáveis local. NFEID: " & CHAVE_ACESSO)
            NUMERO = Format(Long.Parse(objNF.NF_IDE_NNF), "000000")
            SERIE = objNF.NF_IDE_SERIE
            CNPJ_EMITENTE = objNF.NF_EMIT_CNPJ
            RAZFOR = objNF.NF_EMIT_XNOME
            DT_EMISSAO = Format(Date.Parse(objNF.NF_IDE_DHEMI), "dd/MM/yyyy")

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_005: Função: EnviarMsgAceita.", "Recuperando endereço de e-mail para envio da mensgem. NFEID: " & CHAVE_ACESSO)

            modSQL.CommandText = "select count(*) from TbFOR where EMAIL_NFE is not null and EMAIL_NFE <> '' and CNPJ = " & CNPJ_EMITENTE
            If modSQL.ExecuteScalar(modSQL.CommandText) <> "0" Then
                modSQL.CommandText = "select EMAIL_NFE from TbFOR where EMAIL_NFE is not null and EMAIL_NFE <> '' and CNPJ = " & CNPJ_EMITENTE
                SEND_TO = modSQL.ExecuteScalar(modSQL.CommandText)
            Else
                SEND_TO = nenhum_email_cadastrado
            End If

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_006: Função: EnviarMsgAceita.", "Criando corpo da mensagem(body). NFEID: " & CHAVE_ACESSO)
            Dim BODY As String = "<p style='font-family:verdana; font-size:10pt; line-height:150%;'>" & Chr(13) &
                                     "<b>Prezado fornecedor,</b> <br/>" & Chr(13) &
                                    "   Está <b>autorizado</b> o embarque da nota fiscal " & NUMERO & " série " & SERIE & ".<br/><br/><br/>" & Chr(13) & Chr(13) &
                                    "<b>Referências:</b><br/> " & Chr(13) &
                                     "   - CNPJ emissor: " & Convert.ToUInt64(CNPJ_EMITENTE).ToString("00\.000\.000\.0000\-00") & "<br/>" & Chr(13) &
                                     "   - Emissor: " & RAZFOR & "<br/>" & Chr(13) &
                                     "   - Data de emissão: " & DT_EMISSAO & "<br/>" & Chr(13) &
                                     "   - Chave de acesso: " & CHAVE_ACESSO & "<br/><br/><br/>" & Chr(13) & Chr(13) &
                                     "Atenciosamente, <br/>" & Chr(13) &
                                     "Neles Brasil Industria e Comércio LTDA" &
                                 "</p>"

            Dim SUBJECT As String = "NF " & NUMERO & " - SÉRIE " & SERIE & " FOI ACEITA"

            '--> REMOVE A ESTRUTURA DO HTML PARA GRAVAR NO LOG A MENSAGEM QUE FOI ENVIADA PARA O FORNECEDOR
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_007: Função: EnviarMsgAceita.", "Removendo estrutura de tags html para gravar log no banco de dados. NFEID: " & CHAVE_ACESSO)
            Dim strBodyText As String = BODY.Replace("<p style='font-family:verdana; font-size:10pt; line-height:150%;'>", "").Replace("<b>", "").Replace("</b>", "").Replace("<br/>", "").Replace("</p>", "").Replace("'", """")

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_008: Função: EnviarMsgAceita.", "Inserindo registro de controle na tabela de mensagens (TBMEN) . NFEID: " & CHAVE_ACESSO)
            'Marcio Spinosa - 27/09/2018 - CR00009259 
            'modSQL.CommandText = "insert into TbMEN values ('" & CHAVE_ACESSO & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '" & SEND_TO & "', 'AUTORIZADA', '" & strBodyText & "')"
            modSQL.CommandText = "insert into TbMEN values ('" & CHAVE_ACESSO & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '" & SEND_TO & "', 'AUTORIZADA', '" & strBodyText & "', 'F')"
            'Marcio Spinosa - 27/09/2018 - CR00009259  - Fim
            modSQL.ExecuteNonQuery(modSQL.CommandText)

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_009: Função: EnviarMsgAceita.", "Chamando função (EnviarMensagem) para envio de email. . NFEID: " & CHAVE_ACESSO)
            EnviarMensagem(SEND_TO, SUBJECT, BODY, CHAVE_ACESSO)

        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region

#Region " EnviarMsgRejeitada "
    ''' <summary>
    ''' Rotina de envio de emails rejeitados para os fornecedores
    ''' </summary>
    ''' <param name="CHAVE_ACESSO"></param>
    ''' <example>Marcio Spinosa - 27/09/2018 - CR00009259 Ajuste para diferenciar emails de fornecedores de compradores</example>
    Public Sub EnviarMsgRejeitada(ByVal CHAVE_ACESSO As String)
        Try
            '--> SE JÁ FOI ENVIADA MENSAGEM PARA O FORNECEDOR NO MESMO DIA COM O MESMO STATUS, NÃO ENVIA NOVAMENTE
            'Marcio Spinosa - 27/09/2018 - CR00009259
            'modSQL.CommandText = "select count(*) from tbMen where nfeid = '" & CHAVE_ACESSO & "' and SITUACAO = 'NAO AUTORIZADA' and datenv > cast('" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' as datetime) - 1"
            modSQL.CommandText = "select count(1) from tbMen where nfeid = '" & CHAVE_ACESSO & "' and SITUACAO = 'NAO AUTORIZADA' and datenv > cast('" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' as datetime) - 1 and TIPOMEN = 'F'"
            'Marcio Spinosa - 27/09/2018 - CR00009259 - Fim
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_015: Função: EnviarMsgRejeitada.", "Verificando se já não foi enviada alguma mensagem. NFEID:" & CHAVE_ACESSO)
            If modSQL.ExecuteScalar(modSQL.CommandText).ToInt() > 0 Then
                Return
            End If

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_016: Função: EnviarMsgRejeitada.", "Chamando função validar para obter dados da nota. NFEID: " & CHAVE_ACESSO)
            Dim strMensagem As String
            If String.IsNullOrEmpty(objNF.NF_IDE_NNF) Then
                objNF = Validar(CHAVE_ACESSO, String.Empty, False, False, TipoProcessamento.Aplicacao, String.Empty, strMensagem)
            End If

            '--> NAO BUSCA OS DADOS DA NOTA FISCAL NO TRIANGULUS, UTILIZA O OBJETO NOTA FISCAL PREENCHIDO
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_017: Função: EnviarMsgRejeitada.", "Recuperando dados da view vwNOTFIS. NFEID: " & CHAVE_ACESSO)
            modSQL.CommandText = "select NF_IDE_NNF, NF_IDE_SERIE, NF_EMIT_CNPJ, NF_EMIT_XNOME, NF_IDE_DHEMI, NOMCOM, EMAIL_COMPRADOR, TELCOM from vwNOTFIS where NFEID = '" & CHAVE_ACESSO & "'"
            Dim DataTable As DataTable = modSQL.Fill(modSQL.CommandText)
            'If (DataTable.Rows.Count = 0) Then
            '    Exit Sub
            'End If

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_018: Função: EnviarMsgRejeitada.", "Atribuindo valores às variáveis local. NFEID: " & CHAVE_ACESSO)
            Dim NUMERO As String = ""
            Dim SERIE As String = ""
            Dim CNPJ_EMITENTE As String = ""
            Dim RAZFOR As String = ""
            Dim DT_EMISSAO As String = ""
            'Dim NOMCOM As String = ""
            'Dim EMAIL_COMPRADOR As String = ""
            'Dim TELCOM As String = ""
            Dim SEND_TO As String = ""
            Dim QUERY As String = ""

            NUMERO = Format(Long.Parse(objNF.NF_IDE_NNF), "000000")
            SERIE = objNF.NF_IDE_SERIE
            CNPJ_EMITENTE = objNF.NF_EMIT_CNPJ
            RAZFOR = objNF.NF_EMIT_XNOME
            DT_EMISSAO = Format(Date.Parse(objNF.NF_IDE_DHEMI), "dd/MM/yyyy")
            'NOMCOM = DataTable.Rows(0).Item("NOMCOM").ToString
            'EMAIL_COMPRADOR = DataTable.Rows(0).Item("EMAIL_COMPRADOR").ToString
            'TELCOM = DataTable.Rows(0).Item("TELCOM").ToString.Trim

            '            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_019: Função: EnviarMsgRejeitada.", "Verificando se existe alguma vírgula no endereço de email do comprador. NFEID: " & CHAVE_ACESSO & ", email comprador: " & EMAIL_COMPRADOR)
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_019: Função: EnviarMsgRejeitada.", "Verificando se existe alguma vírgula no endereço de email do comprador. NFEID: " & CHAVE_ACESSO & ", email comprador: ")
            'If InStr(EMAIL_COMPRADOR, ",") > 0 Then
            '    EMAIL_COMPRADOR = Mid(EMAIL_COMPRADOR, 1, InStr(EMAIL_COMPRADOR, ",") - 1)
            'End If

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_020: Função: EnviarMsgRejeitada.", "Recuperando endereço de e-mail do fornecedor. NFEID: " & CHAVE_ACESSO)
            QUERY = "select count(*) from TbFOR where EMAIL_NFE is not null and EMAIL_NFE <> '' and CNPJ = " & CNPJ_EMITENTE
            If modSQL.ExecuteScalar(QUERY) <> "0" Then
                QUERY = "select EMAIL_NFE from TbFOR where EMAIL_NFE is not null and EMAIL_NFE <> '' and CNPJ = " & CNPJ_EMITENTE
                SEND_TO = modSQL.ExecuteScalar(QUERY)
            Else
                SEND_TO = nenhum_email_cadastrado
            End If

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_021: Função: EnviarMsgRejeitada.", "Recuperando telefone da ouvidoria na tabela TbPAR sobre o parametro (OUVIDORIA). NFEID: " & CHAVE_ACESSO)
            QUERY = "select VALOR from TbPAR where PARAMETRO = 'OUVIDORIA'"
            Dim TELEFONE_OUVIDORIA As String = modSQL.ExecuteScalar(QUERY)

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_022: Função: EnviarMsgRejeitada.", "Recuperando lista de divergências. NFEID: " & CHAVE_ACESSO)
            QUERY = "select CAMPO, ITENFE, VALOR_NFE, PEDCOM, ITEPED, VALOR_PED from TbLOG where SITUACAO = 'ATIVO' and NFEID = '" & CHAVE_ACESSO & "'"
            DataTable = modSQL.Fill(QUERY)

            Dim DIVERGENCIAS As String = ""
            Dim strTextoEmail As String = ""
            Dim RowIndex As Integer

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_023: Função: EnviarMsgRejeitada.", "Criando textos de divergências. NFEID: " & CHAVE_ACESSO)
            For RowIndex = 1 To DataTable.Rows.Count

                QUERY = "SELECT val_texto_email_fornecedor FROM tbvalidacoes WHERE val_titulo_usuario = '" & DataTable.Rows(RowIndex - 1).Item("CAMPO").ToString & "'"
                strTextoEmail = modSQL.ExecuteScalar(QUERY).ToString()

                '--> PREENCHE COM AS INFORMAÇÕES DO ITEM
                strTextoEmail = strTextoEmail.Replace("%ITEM_NF%", Format(Integer.Parse(DataTable.Rows(RowIndex - 1).Item("ITENFE").ToString), "000"))
                strTextoEmail = strTextoEmail.Replace("%VALOR_NF%", "<b>" & DataTable.Rows(RowIndex - 1).Item("VALOR_NFE").ToString() & "</b>")
                strTextoEmail = strTextoEmail.Replace("%NUMERO_PO%", DataTable.Rows(RowIndex - 1).Item("PEDCOM").ToString())
                strTextoEmail = strTextoEmail.Replace("%ITEM_PO%", DataTable.Rows(RowIndex - 1).Item("ITEPED").ToString())
                strTextoEmail = strTextoEmail.Replace("%VALOR_PO%", "<b>" & DataTable.Rows(RowIndex - 1).Item("VALOR_PED").ToString() & "</b>")

                DIVERGENCIAS = DIVERGENCIAS & "   " & Format(RowIndex, "00") & ". " & strTextoEmail & "<br/>" & Chr(13)

            Next

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_024: Função: EnviarMsgRejeitada.", "Criando corpo da mensagem (body). NFEID: " & CHAVE_ACESSO)
            Dim BODY As String = "<p style='font-family:verdana; font-size:10pt; line-height:150%;'>" & Chr(13) &
                                     "<b>Prezado fornecedor,</b> <br/>" & Chr(13) &
                                     "   Os dados da nota fiscal " & NUMERO & " série " & SERIE & " não conferem com o pedido de compra, o embarque da nota fiscal <b>NÃO está autorizado.</b> <br/><br/><br/> " & Chr(13) & Chr(13) &
                                     "<b>Referências:</b><br/> " & Chr(13) &
                                     "   - CNPJ emissor: " & Convert.ToUInt64(CNPJ_EMITENTE).ToString("00\.000\.000\.0000\-00") & "<br/>" & Chr(13) &
                                     "   - Emissor: " & RAZFOR & "<br/>" & Chr(13) &
                                     "   - Data de emissão: " & DT_EMISSAO & "<br/>" & Chr(13) &
                                     "   - Chave de acesso: " & CHAVE_ACESSO & "<br/><br/><br/>" & Chr(13) & Chr(13) &
                                     "<b>Divergências:</b><br/> " & Chr(13) & DIVERGENCIAS & "<br/><br/>" & Chr(13) & Chr(13) &
                                     "Para maiores informações entrar em contato com o comprador responsável pelo pedido de compras <br/><br/>" & Chr(13) & Chr(13) &
                                     "Atenciosamente, <br/>" & Chr(13) &
                                     "Neles Brasil Industria e Comércio LTDA" &
                                 "</p>"

            Dim SUBJECT As String = "NF " & NUMERO & " - SÉRIE " & SERIE & " FOI REJEITADA"

            '--> REMOVE A ESTRUTURA DO HTML PARA GRAVAR NO LOG A MENSAGEM QUE FOI ENVIADA PARA O FORNECEDOR
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_025: Função: EnviarMsgRejeitada.", "Removendo tags html para gravar log da mensagem. NFEID: " & CHAVE_ACESSO)
            Dim strBodyText As String = BODY.Replace("<p style='font-family:verdana; font-size:10pt; line-height:150%;'>", "").Replace("<b>", "").Replace("</b>", "").Replace("<br/>", "").Replace("</p>", "")
            strBodyText = strBodyText.Replace("<a href='https://www.pool4tool.com/portal/metso/' target='_blank'>", "").Replace("</a>", "").Replace("'", """")

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_026: Função: EnviarMsgRejeitada.", "Gravando mensagem na tabela de controle TBMEN. NFEID: " & CHAVE_ACESSO)
            'Marcio Spinosa - 27/09/2018 - CR00009259
            'QUERY = "insert into TbMEN values ('" & CHAVE_ACESSO & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '" & SEND_TO & "', 'NAO AUTORIZADA', '" & strBodyText & "')"
            QUERY = "insert into TbMEN values ('" & CHAVE_ACESSO & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '" & SEND_TO & "', 'NAO AUTORIZADA', '" & strBodyText & "', 'F')"
            'Marcio Spinosa - 27/09/2018 - CR00009259 - Fim
            modSQL.ExecuteNonQuery(QUERY)

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_027: Função: EnviarMsgRejeitada.", "Chamando função(EnviarMensagem) para envio da mensagem. NFEID: " & CHAVE_ACESSO)
            EnviarMensagem(SEND_TO, SUBJECT, BODY, CHAVE_ACESSO)

        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region

#Region " EnviarMsgPendente "
    ''' <summary>
    ''' Rotina de envio de e-mails de nota pendente ao fornecedor
    ''' </summary>
    ''' <param name="CHAVE_ACESSO"></param>
    ''' <example>Marcio Spinosa - 27/09/2018 - CR00009259 ajuste para diferenciar e-mail de fornecedor e comprador</example>
    Public Sub EnviarMsgPendente(ByVal CHAVE_ACESSO As String)
        Try
            '--> SE JÁ FOI ENVIADA MENSAGEM PARA O FORNECEDOR NO MESMO DIA COM O MESMO STATUS, NÃO ENVIA NOVAMENTE
            'Marcio Spinosa - 27/09/2018 - CR00009259
            'modSQL.CommandText = "select count(*) from tbMen where nfeid = '" & CHAVE_ACESSO & "' and SITUACAO = 'NAO AUTORIZADA' and datenv > cast('" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' as datetime) - 1"
            modSQL.CommandText = "select count(1) from tbMen where nfeid = '" & CHAVE_ACESSO & "' and SITUACAO = 'NAO AUTORIZADA' and datenv > cast('" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' as datetime) - 1 and TIPOMEN = 'F'"
            'Marcio Spinosa - 27/09/2018 - CR00009259 - Fim
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_028: Função: EnviarMsgPendente.", "Verificando se já não foi enviada alguma mensagem. NFEID:" & CHAVE_ACESSO)
            If Integer.Parse(modSQL.ExecuteScalar(modSQL.CommandText)) > 0 Then
                Return
            End If

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_029: Função: EnviarMsgPendente.", "Chamando função (Validar) para obter dados do documento. NFEID:" & CHAVE_ACESSO)
            Dim strMensagem As String
            If String.IsNullOrEmpty(objNF.NF_IDE_NNF) Then
                objNF = Validar(CHAVE_ACESSO, String.Empty, False, False, TipoProcessamento.Aplicacao, String.Empty, strMensagem)
            End If

            Dim NUMERO As String = ""
            Dim SERIE As String = ""
            Dim CNPJ_EMITENTE As String = ""
            Dim RAZFOR As String = ""
            Dim DT_EMISSAO As String = ""
            Dim SEND_TO As String = ""

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_030: Função: EnviarMsgPendente.", "Atribuindo valores as variáveis local com os dados retornados da função Validar. NFEID:" & CHAVE_ACESSO)
            NUMERO = Format(Long.Parse(objNF.NF_IDE_NNF), "000000")
            SERIE = objNF.NF_IDE_SERIE
            CNPJ_EMITENTE = objNF.NF_EMIT_CNPJ
            RAZFOR = objNF.NF_EMIT_XNOME
            DT_EMISSAO = Format(Date.Parse(objNF.NF_IDE_DHEMI), "dd/MM/yyyy")

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_031: Função: EnviarMsgPendente.", "Recuperando e-mail do fornecedor com base no CNPJ. NFEID:" & CHAVE_ACESSO & " CNPJ: " & CNPJ_EMITENTE)
            modSQL.CommandText = "select count(*) from TbFOR where EMAIL_NFE is not null and EMAIL_NFE <> '' and CNPJ = " & CNPJ_EMITENTE
            If modSQL.ExecuteScalar(modSQL.CommandText) <> "0" Then
                modSQL.CommandText = "select EMAIL_NFE from TbFOR where EMAIL_NFE is not null and EMAIL_NFE <> '' and CNPJ = " & CNPJ_EMITENTE
                SEND_TO = modSQL.ExecuteScalar(modSQL.CommandText)
            Else
                SEND_TO = nenhum_email_cadastrado
            End If

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_032: Função: EnviarMsgPendente.", "Recuperando telefone da ouvidoria. NFEID:" & CHAVE_ACESSO)
            modSQL.CommandText = "select VALOR from TbPAR where PARAMETRO = 'OUVIDORIA'"
            Dim TELEFONE_OUVIDORIA As String = modSQL.ExecuteScalar(modSQL.CommandText)

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_033: Função: EnviarMsgPendente.", "Recuperando parametro da TBPAR  onde o parametro seja igual a (PORTAL). NFEID:" & CHAVE_ACESSO)
            modSQL.CommandText = "select VALOR from TbPAR where PARAMETRO = 'PORTAL'"
            Dim PORTAL As String = modSQL.ExecuteScalar(modSQL.CommandText)

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_034: Função: EnviarMsgPendente.", "Criando corpo da mensagem (Body). NFEID:" & CHAVE_ACESSO)
            Dim BODY As String = "<p style='font-family:verdana; font-size:10pt; line-height:150%;'>" & Chr(13) &
                                     "<b>Prezado fornecedor,</b> <br/>" & Chr(13) &
                                     "Pedido de compra e item não foram informados ou são inválidos, o embarque da nota fiscal <b>não está autorizado</b>.<br/> " & Chr(13) & Chr(13) &
                                     "Favor associar a nota fiscal em questão via portal: " & Chr(13) & Chr(13) &
                                     "<a href='" & PORTAL & "' target='_blank'>" & PORTAL & "</a><br/><br/>" & Chr(13) & Chr(13) &
                                     "<b>Referências:</b><br/> " & Chr(13) &
                                     "   - CNPJ emissor: " & Convert.ToUInt64(CNPJ_EMITENTE).ToString("00\.000\.000\.0000\-00") & "<br/>" & Chr(13) &
                                     "   - Emissor: " & RAZFOR & "<br/>" & Chr(13) &
                                     "   - Data de emissão: " & DT_EMISSAO & "<br/>" & Chr(13) &
                                     "   - Chave de acesso: " & CHAVE_ACESSO & "<br/><br/><br/>" & Chr(13) & Chr(13) &
                                     "Para maiores informações entrar em contato com o comprador responsável pelo pedido de compras <br/><br/>" & Chr(13) & Chr(13) &
                                     "Atenciosamente, <br/>" & Chr(13) &
                                     "Neles Brasil Industria e Comércio LTDA" &
                                 "</p>"

            Dim SUBJECT As String = "NF " & NUMERO & " - SÉRIE " & SERIE & " FOI REJEITADA"

            '--> REMOVE A ESTRUTURA DO HTML PARA GRAVAR NO LOG A MENSAGEM QUE FOI ENVIADA PARA O FORNECEDOR
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_035: Função: EnviarMsgPendente.", "Removendo tags html para gravar log da mensagem. NFEID:" & CHAVE_ACESSO)
            Dim strBodyText As String = BODY.Replace("<p style='font-family:verdana; font-size:10pt; line-height:150%;'>", "").Replace("<b>", "").Replace("</b>", "").Replace("<br/>", "").Replace("</p>", "")
            strBodyText = strBodyText.Replace("<a href='" & PORTAL & "' target='_blank'>", "").Replace("</a>", "").Replace("'", """")

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_036: Função: EnviarMsgPendente.", "Gravando log na tabela de mensagens TBMEN. NFEID:" & CHAVE_ACESSO)
            'Marcio Spinosa - 27/09/2018 - CR00009259
            'modSQL.CommandText = "insert into TbMEN values ('" & CHAVE_ACESSO & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '" & SEND_TO & "', 'NAO AUTORIZADA', '" & strBodyText & "')"
            modSQL.CommandText = "insert into TbMEN values ('" & CHAVE_ACESSO & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '" & SEND_TO & "', 'NAO AUTORIZADA', '" & strBodyText & "', 'F')"
            'Marcio Spinosa - 27/09/2018 - CR00009259 - Fim
            modSQL.ExecuteNonQuery(modSQL.CommandText)

            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_037: Função: EnviarMsgPendente.", "Chamando função para envio da mensagem (EnviarMensagem). NFEID:" & CHAVE_ACESSO)
            EnviarMensagem(SEND_TO, SUBJECT, BODY, CHAVE_ACESSO)

        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region

#Region " GetRegrasValidacao "
    Public Function GetRegrasValidacao(ByVal codigo As String, ByVal cfop As String, ByVal pStrDeposito As String, ByVal pStrCodMaterial As String) As modRegrasValidacao

        Dim strQuery As String = "SELECT " &
                                 "	 id_validacao " &
                                 "	,val_codigo " &
                                 "	,val_titulo_usuario " &
                                 "	,val_texto_reprovacao " &
                                 "	,val_notificar_compras " &
                                 "	,val_notificar_fornecedor " &
                                 "	,val_permitir_anulacao_compras " &
                                 "	,val_permitir_anulacao_fiscal " &
                                 "	,val_validar_nfe " &
                                 "	,val_validar_cte " &
                                 "	,val_validar_tal " &
                                 "  ,val_validar_fat" &
                                 "  ,val_validar_tcom" &
                                 "  ,val_validar_nfs" &
                                 "	,vex_cfop " &
                                 "	,vex_deposito " &
                                 "	,vex_cod_material " &
                                 "FROM  " &
                                 "	TbValidacoes " &
                                 "	left join TbValidacoesExcecoes " &
                                 "	on id_validacao = vex_id_validacao " &
                                 "	and (vex_cfop = '" & cfop & "' or vex_deposito = '" & pStrDeposito & "' or vex_cod_material = '" & pStrCodMaterial & "' )" &
                                 "WHERE " &
                                 "	val_codigo = '" & codigo & "' " &
                                 "	and ((vex_cfop is null or vex_cfop = '" & cfop & "') or (vex_deposito is null or vex_deposito = '" & pStrDeposito & "') or (vex_cod_material is null or vex_cod_material = '" & pStrCodMaterial & "'))"

        Dim dttDados As DataTable = modSQL.Fill(strQuery)
        Dim objRetorno As New modRegrasValidacao()
        objRetorno.IdValidacao = Convert.ToInt32(dttDados.Rows(0)("id_validacao").ToString())
        objRetorno.Codigo = dttDados.Rows(0)("val_codigo").ToString()
        objRetorno.TituloUsuario = dttDados.Rows(0)("val_titulo_usuario").ToString()
        objRetorno.TextoReprovacao = dttDados.Rows(0)("val_texto_reprovacao").ToString()
        objRetorno.NotificarCompras = Convert.ToBoolean(dttDados.Rows(0)("val_notificar_compras").ToString())
        objRetorno.NotificarFornecedor = Convert.ToBoolean(dttDados.Rows(0)("val_notificar_fornecedor").ToString())
        objRetorno.PermitirAnulacaoCompras = Convert.ToBoolean(dttDados.Rows(0)("val_permitir_anulacao_compras").ToString())
        objRetorno.PermitirAnulacaoFiscal = Convert.ToBoolean(dttDados.Rows(0)("val_permitir_anulacao_fiscal").ToString())
        objRetorno.IsExcecao = dttDados.Rows(0)("vex_cfop").ToString() <> Nothing

        'Verifica se existe alguma exceção para algum depósito
        If Not objRetorno.IsExcecao Then
            objRetorno.IsExcecao = dttDados.Rows(0)("vex_deposito").ToString() <> Nothing
        End If

        'Verifica se existe alguma exceção para material
        If Not objRetorno.IsExcecao Then
            objRetorno.IsExcecao = dttDados.Rows(0)("vex_cod_material").ToString() <> Nothing
        End If

        If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe) Then
            objRetorno.IsValidar = Convert.ToBoolean(dttDados.Rows(0)("val_validar_nfe").ToString())
        ElseIf (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte) Then
            objRetorno.IsValidar = Convert.ToBoolean(dttDados.Rows(0)("val_validar_cte").ToString())
        ElseIf (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) Then
            objRetorno.IsValidar = Convert.ToBoolean(dttDados.Rows(0)("val_validar_tal").ToString())
            'Marcio Spinosa - 11/09/2018 - CR00009259
        ElseIf (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_FAT) Then
            objRetorno.IsValidar = Convert.ToBoolean(dttDados.Rows(0)("val_validar_fat").ToString())
        ElseIf (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_TLC) Then
            objRetorno.IsValidar = Convert.ToBoolean(dttDados.Rows(0)("val_validar_tcom").ToString())
        ElseIf (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_NFS) Then
            objRetorno.IsValidar = Convert.ToBoolean(dttDados.Rows(0)("val_validar_nfs").ToString())
        End If
        'Marcio Spinosa - 11/09/2018 - CR00009259 - Fim


        Return objRetorno
    End Function
#End Region

    Private Sub CarregarTalonario()
        modSQL.CommandText = "SELECT * FROM TbDOC_CAB_NFE WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
        Dim pdttTalonario As DataTable = modSQL.Fill(modSQL.CommandText)
        If pdttTalonario.Rows.Count > 0 Then

            objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario
            objNF.NF_IDE_NNF = pdttTalonario.Rows(0)("NF_IDE_NNF").ToString
            objNF.NF_IDE_SERIE = pdttTalonario.Rows(0)("NF_IDE_SERIE").ToString
            objNF.NF_EMIT_CNPJ = pdttTalonario.Rows(0)("NF_EMIT_CNPJ").ToString()
            objNF.NF_EMIT_IE = pdttTalonario.Rows(0)("NF_EMIT_IE").ToString()
            objNF.NF_EMIT_XNOME = pdttTalonario.Rows(0)("NF_EMIT_XNOME").ToString
            objNF.NF_DEST_CNPJ = pdttTalonario.Rows(0)("NF_DEST_CNPJ").ToString()
            objNF.NF_DEST_IE = pdttTalonario.Rows(0)("NF_DEST_IE").ToString()
            objNF.NF_DEST_XNOME = pdttTalonario.Rows(0)("NF_DEST_XNOME").ToString
            objNF.NF_OUTROS_INFORMACAO_ADICIONAL = pdttTalonario.Rows(0)("NF_OUTROS_INFORMACAO_ADICIONAL").ToString
            objNF.NF_IDE_DHEMI = Convert.ToDateTime(pdttTalonario.Rows(0)("NF_IDE_DHEMI").ToString)
            objNF.NF_ICMSTOT_VNF = Convert.ToDecimal(pdttTalonario.Rows(0)("NF_ICMSTOT_VNF").ToString)

            modSQL.CommandText = "SELECT * FROM TbDOC_CAB WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
            Dim DtNfeCab As DataTable = modSQL.Fill(modSQL.CommandText)
            If DtNfeCab.Rows.Count > 0 Then
                objNF.VNF_CODIGO_VERIFICACAO = DtNfeCab.Rows(0)("VNF_CODIGO_VERIFICACAO").ToString
                objNF.VNF_DANFE_ONLINE = DtNfeCab.Rows(0)("VNF_DANFE_ONLINE").ToString
                objNF.VNF_ANEXO_EXTENSAO = DtNfeCab.Rows(0)("VNF_ANEXO_EXTENSAO").ToString
                objNF.VNF_ANEXO_NOME = DtNfeCab.Rows(0)("VNF_ANEXO_NOME").ToString
                objNF.VNF_CLASSIFICACAO = DtNfeCab.Rows(0)("VNF_CLASSIFICACAO").ToString
            End If


            objNF.DUPLICATAS = New List(Of modNFDuplicata)
            'CARREGA AS DUPLICATAS
            modSQL.CommandText = "SELECT * FROM TbDOC_CAB_NFE_DUP WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
            Dim DtNfeDup As DataTable = modSQL.Fill(modSQL.CommandText)
            If DtNfeDup.Rows.Count > 0 Then
                For Each dr As DataRow In DtNfeDup.Rows
                    If Not dr("NF_COBR_DUP_DVENC") Is DBNull.Value Then
                        Dim Dup As New modNFDuplicata()
                        Dup.NF_COBR_DUP_DVENC = Convert.ToDateTime(dr("NF_COBR_DUP_DVENC"))
                        objNF.DUPLICATAS.Add(Dup)
                    End If
                Next
            End If

            'CARREGA OS ITENS
            objNF.ITENS_NF = New List(Of modNFItem)
            'Marcio Spinosa - 02/12/2019
            modSQL.CommandText = "SELECT DISTINCT " &
                                 "	VNF_ITEM_VALIDO, " &
                                 "	NF_PROD_ITEM, " &
                                 "	NF_PROD_VUNCOM," &
                                 "	NF_PROD_VPROD," &
                                 "	NF_PROD_XPED," &
                                 "	NF_PROD_NITEMPED," &
                                 "	NF_PROD_CPROD," &
                                 "	NF_PROD_XPROD," &
                                 "	NF_PROD_NCM," &
                                 "	NF_PROD_QCOM," &
                                 "	NF_PROD_QTRIB," &
                                 "	NF_PROD_CFOP," &
                                 "	NF_ICMS_VICMS," &
                                 "	NF_IPI_VIPI," &
                                 "	NF_PROD_VFRETE," &
                                 "	NF_PROD_VSEG," &
                                 "	NF_PROD_VDESC," &
                                 "	NF_PROD_VOUTRO, " &
                                 "  CODJUN " &
                                 "FROM " &
                                 "	TbDOC_ITEM_NFE " &
                                 "	INNER JOIN TbDOC_ITEM " &
                                 "	ON TbDOC_ITEM_NFE.NFEID = TbDOC_ITEM.NFEID " &
                                 " INNER JOIN TBJUN ON TBJUN.NFEID = TbDOC_ITEM.NFEID AND TBJUN.PEDCOM = NF_PROD_XPED AND tbjun.ITEPED = NF_PROD_NITEMPED " &
                                 "WHERE " &
                                 "	TbDOC_ITEM_NFE.NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
            'Marcio Spinosa - 02/12/2019 - Fim
            Dim DtNfeItens As DataTable = modSQL.Fill(modSQL.CommandText)
            If DtNfeItens.Rows.Count > 0 Then
                For Each dri As DataRow In DtNfeItens.Rows
                    Dim Item As New modNFItem()

                    If Not dri("VNF_ITEM_VALIDO") Is DBNull.Value Then
                        Item.VNF_ITEM_VALIDO = dri("VNF_ITEM_VALIDO")
                    End If

                    If Not dri("NF_PROD_ITEM") Is DBNull.Value Then
                        Item.NF_PROD_ITEM = dri("NF_PROD_ITEM")
                    End If

                    If Not dri("NF_PROD_VUNCOM") Is DBNull.Value Then
                        Item.NF_PROD_VUNCOM = dri("NF_PROD_VUNCOM")
                    End If

                    If Not dri("NF_PROD_VPROD") Is DBNull.Value Then
                        Item.NF_PROD_VPROD = dri("NF_PROD_VPROD")
                    End If

                    If Not dri("NF_PROD_XPED") Is DBNull.Value Then
                        Item.NF_PROD_XPED = IIf(dri("NF_PROD_XPED") Is Nothing, "", dri("NF_PROD_XPED"))
                    End If

                    If Not dri("NF_PROD_NITEMPED") Is DBNull.Value Then
                        Item.NF_PROD_NITEMPED = IIf(dri("NF_PROD_NITEMPED") Is Nothing, "", Convert.ToInt32(dri("NF_PROD_NITEMPED")))
                    End If

                    If Not dri("NF_PROD_CPROD") Is DBNull.Value Then
                        Item.NF_PROD_CPROD = IIf(dri("NF_PROD_CPROD") Is Nothing, "", dri("NF_PROD_CPROD"))
                    End If

                    If Not dri("NF_PROD_XPROD") Is DBNull.Value Then
                        Item.NF_PROD_XPROD = IIf(dri("NF_PROD_XPROD") Is Nothing, "", dri("NF_PROD_XPROD"))
                    End If

                    If Not dri("NF_PROD_NCM") Is DBNull.Value Then
                        Item.NF_PROD_NCM = IIf(dri("NF_PROD_NCM").ToString = "", "", dri("NF_PROD_NCM"))
                    End If

                    If Not dri("NF_PROD_QTRIB") Is DBNull.Value Then
                        Item.NF_PROD_QCOM = IIf(dri("NF_PROD_QTRIB") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_QTRIB")))
                    End If

                    If Not dri("NF_PROD_QCOM") Is DBNull.Value Then
                        Item.NF_PROD_QTRIB = IIf(dri("NF_PROD_QCOM") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_QCOM")))
                    End If

                    If Not dri("NF_PROD_CFOP") Is DBNull.Value Then
                        Item.NF_PROD_CFOP = IIf(dri("NF_PROD_CFOP").ToString = "", "", dri("NF_PROD_CFOP"))
                    End If

                    If Not dri("NF_ICMS_VICMS") Is DBNull.Value Then
                        Item.NF_ICMS_VICMS = IIf(dri("NF_ICMS_VICMS") Is Nothing, 0, Convert.ToDecimal(dri("NF_ICMS_VICMS")))
                    End If

                    If Not dri("NF_IPI_VIPI") Is DBNull.Value Then
                        Item.NF_IPI_VIPI = IIf(dri("NF_IPI_VIPI") Is Nothing, 0, Convert.ToDecimal(dri("NF_IPI_VIPI")))
                    End If

                    If Not dri("NF_PROD_VFRETE") Is DBNull.Value Then
                        Item.NF_PROD_VFRETE = IIf(dri("NF_PROD_VFRETE") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_VFRETE")))
                    End If

                    If Not dri("NF_PROD_VSEG") Is DBNull.Value Then
                        Item.NF_PROD_VSEG = IIf(dri("NF_PROD_VSEG") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_VSEG")))
                    End If

                    If Not dri("NF_PROD_VDESC") Is DBNull.Value Then
                        Item.NF_PROD_VDESC = IIf(dri("NF_PROD_VDESC") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_VDESC")))
                    End If

                    If Not dri("NF_PROD_VOUTRO") Is DBNull.Value Then
                        Item.NF_PROD_VOUTRO = IIf(dri("NF_PROD_VOUTRO") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_VOUTRO")))
                    End If
                    'Marcio Spinosa - 02/12/2019
                    If Not dri("NF_PROD_VOUTRO") Is DBNull.Value Then
                        Item.VNF_CODJUN = IIf(dri("CODJUN") Is Nothing, 0, Convert.ToDecimal(dri("CODJUN")))
                    End If
                    'Marcio Spinosa - 02/12/2019 - Fim
                    objNF.ITENS_NF.Add(Item)
                Next
            End If
        End If
    End Sub

    Private Sub CarregarNFS()
        modSQL.CommandText = "SELECT * FROM TbDOC_CAB_NFE WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
        Dim pdttTalonario As DataTable = modSQL.Fill(modSQL.CommandText)

        If pdttTalonario.Rows.Count > 0 Then

            objNF.NF_IDE_NNF = pdttTalonario.Rows(0)("NF_IDE_NNF").ToString
            objNF.NF_IDE_SERIE = pdttTalonario.Rows(0)("NF_IDE_SERIE").ToString
            objNF.NF_EMIT_CNPJ = pdttTalonario.Rows(0)("NF_EMIT_CNPJ").ToString()
            objNF.NF_EMIT_XNOME = pdttTalonario.Rows(0)("NF_EMIT_XNOME").ToString
            objNF.NF_DEST_CNPJ = pdttTalonario.Rows(0)("NF_DEST_CNPJ").ToString()
            objNF.NF_DEST_XNOME = pdttTalonario.Rows(0)("NF_DEST_XNOME").ToString
            objNF.NF_OUTROS_INFORMACAO_ADICIONAL = pdttTalonario.Rows(0)("NF_OUTROS_INFORMACAO_ADICIONAL").ToString
            objNF.NF_IDE_DHEMI = Convert.ToDateTime(pdttTalonario.Rows(0)("NF_IDE_DHEMI").ToString)
            objNF.NF_ICMSTOT_VNF = Convert.ToDecimal(pdttTalonario.Rows(0)("NF_ICMSTOT_VNF").ToString)

            modSQL.CommandText = "SELECT * FROM TbDOC_CAB WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
            Dim DtNfeCab As DataTable = modSQL.Fill(modSQL.CommandText)
            If DtNfeCab.Rows.Count > 0 Then
                objNF.VNF_TIPO_DOCUMENTO = DtNfeCab.Rows(0)("VNF_TIPO_DOCUMENTO").ToString
                objNF.VNF_CODIGO_VERIFICACAO = DtNfeCab.Rows(0)("VNF_CODIGO_VERIFICACAO").ToString
                objNF.VNF_DANFE_ONLINE = DtNfeCab.Rows(0)("VNF_DANFE_ONLINE").ToString
                objNF.VNF_ANEXO_EXTENSAO = DtNfeCab.Rows(0)("VNF_ANEXO_EXTENSAO").ToString
                objNF.VNF_ANEXO_NOME = DtNfeCab.Rows(0)("VNF_ANEXO_NOME").ToString
                objNF.VNF_CLASSIFICACAO = DtNfeCab.Rows(0)("VNF_CLASSIFICACAO").ToString
            End If

            objNF.DUPLICATAS = New List(Of modNFDuplicata)
            'CARREGA AS DUPLICATAS
            modSQL.CommandText = "SELECT * FROM TbDOC_CAB_NFE_DUP WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
            Dim DtNfeDup As DataTable = modSQL.Fill(modSQL.CommandText)
            If DtNfeDup.Rows.Count > 0 Then
                For Each dr As DataRow In DtNfeDup.Rows
                    If Not dr("NF_COBR_DUP_DVENC") Is DBNull.Value Then
                        Dim Dup As New modNFDuplicata()
                        Dup.NF_COBR_DUP_DVENC = Convert.ToDateTime(dr("NF_COBR_DUP_DVENC"))
                        objNF.DUPLICATAS.Add(Dup)
                    End If
                Next
            End If

            'CARREGA OS ITENS
            objNF.ITENS_NF = New List(Of modNFItem)

            modSQL.CommandText = "SELECT DISTINCT " &
                                 "	VNF_ITEM_VALIDO, " &
                                 "	NF_PROD_ITEM, " &
                                 "	NF_PROD_VUNCOM," &
                                 "	NF_PROD_VPROD," &
                                 "	NF_PROD_XPED," &
                                 "	NF_PROD_NITEMPED," &
                                 "	NF_PROD_CPROD," &
                                 "	NF_PROD_XPROD," &
                                 "	NF_PROD_NCM," &
                                 "	NF_PROD_QCOM," &
                                 "	NF_PROD_QTRIB," &
                                 "	NF_PROD_CFOP," &
                                 "	NF_ICMS_VICMS," &
                                 "	NF_IPI_VIPI," &
                                 "	NF_PROD_VFRETE," &
                                 "	NF_PROD_VSEG," &
                                 "	NF_PROD_VDESC," &
                                 "	NF_PROD_VOUTRO " &
                                 "FROM " &
                                 "	TbDOC_ITEM_NFE " &
                                 "	INNER JOIN TbDOC_ITEM " &
                                 "	ON TbDOC_ITEM_NFE.NFEID = TbDOC_ITEM.NFEID " &
                                 "WHERE " &
                                 "	TbDOC_ITEM_NFE.NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"

            Dim DtNfeItens As DataTable = modSQL.Fill(modSQL.CommandText)
            If DtNfeItens.Rows.Count > 0 Then
                For Each dri As DataRow In DtNfeItens.Rows
                    Dim Item As New modNFItem()

                    If Not dri("VNF_ITEM_VALIDO") Is DBNull.Value Then
                        Item.VNF_ITEM_VALIDO = dri("VNF_ITEM_VALIDO")
                    End If

                    If Not dri("NF_PROD_ITEM") Is DBNull.Value Then
                        Item.NF_PROD_ITEM = dri("NF_PROD_ITEM")
                    End If

                    If Not dri("NF_PROD_VUNCOM") Is DBNull.Value Then
                        Item.NF_PROD_VUNCOM = dri("NF_PROD_VUNCOM")
                    End If

                    If Not dri("NF_PROD_VPROD") Is DBNull.Value Then
                        Item.NF_PROD_VPROD = dri("NF_PROD_VPROD")
                    End If

                    If Not dri("NF_PROD_XPED") Is DBNull.Value Then
                        Item.NF_PROD_XPED = IIf(dri("NF_PROD_XPED") Is Nothing, "", dri("NF_PROD_XPED"))
                    End If

                    If Not dri("NF_PROD_NITEMPED") Is DBNull.Value Then
                        Item.NF_PROD_NITEMPED = IIf(dri("NF_PROD_NITEMPED") Is Nothing, "", Convert.ToInt32(dri("NF_PROD_NITEMPED")))
                    End If

                    If Not dri("NF_PROD_CPROD") Is DBNull.Value Then
                        Item.NF_PROD_CPROD = IIf(dri("NF_PROD_CPROD") Is Nothing, "", dri("NF_PROD_CPROD"))
                    End If

                    If Not dri("NF_PROD_XPROD") Is DBNull.Value Then
                        Item.NF_PROD_XPROD = IIf(dri("NF_PROD_XPROD") Is Nothing, "", dri("NF_PROD_XPROD"))
                    End If

                    If Not dri("NF_PROD_NCM") Is DBNull.Value Then
                        Item.NF_PROD_NCM = IIf(dri("NF_PROD_NCM").ToString = "", "", dri("NF_PROD_NCM"))
                    End If

                    If Not dri("NF_PROD_QCOM") Is DBNull.Value Then
                        Item.NF_PROD_QCOM = IIf(dri("NF_PROD_QCOM") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_QCOM")))
                    End If

                    If Not dri("NF_PROD_QTRIB") Is DBNull.Value Then
                        Item.NF_PROD_QTRIB = IIf(dri("NF_PROD_QTRIB") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_QTRIB")))
                    End If

                    If Not dri("NF_PROD_CFOP") Is DBNull.Value Then
                        Item.NF_PROD_CFOP = IIf(dri("NF_PROD_CFOP").ToString = "", "", dri("NF_PROD_CFOP"))
                    End If

                    If Not dri("NF_ICMS_VICMS") Is DBNull.Value Then
                        Item.NF_ICMS_VICMS = IIf(dri("NF_ICMS_VICMS") Is Nothing, 0, Convert.ToDecimal(dri("NF_ICMS_VICMS")))
                    End If

                    If Not dri("NF_IPI_VIPI") Is DBNull.Value Then
                        Item.NF_IPI_VIPI = IIf(dri("NF_IPI_VIPI") Is Nothing, 0, Convert.ToDecimal(dri("NF_IPI_VIPI")))
                    End If

                    If Not dri("NF_PROD_VFRETE") Is DBNull.Value Then
                        Item.NF_PROD_VFRETE = IIf(dri("NF_PROD_VFRETE") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_VFRETE")))
                    End If

                    If Not dri("NF_PROD_VSEG") Is DBNull.Value Then
                        Item.NF_PROD_VSEG = IIf(dri("NF_PROD_VSEG") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_VSEG")))
                    End If

                    If Not dri("NF_PROD_VDESC") Is DBNull.Value Then
                        Item.NF_PROD_VDESC = IIf(dri("NF_PROD_VDESC") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_VDESC")))
                    End If

                    If Not dri("NF_PROD_VOUTRO") Is DBNull.Value Then
                        Item.NF_PROD_VOUTRO = IIf(dri("NF_PROD_VOUTRO") Is Nothing, 0, Convert.ToDecimal(dri("NF_PROD_VOUTRO")))
                    End If

                    objNF.ITENS_NF.Add(Item)
                Next
            End If
        End If
    End Sub

#End Region

#Region " Private Methods "

#Region " DeterminarModoProcesso "
    Private Sub DeterminarModoProcesso(ByRef item As modNFItem, ByVal Usuario As String, Optional ByVal isSomenteLeitura As Boolean = False, Optional ByVal IsNFComplementar As Boolean = False)
        Dim strNfeId As String = ""
        item.VNF_IS_SUBCONTRATACAO = False
        Try
            If (item.VNF_ITEM_VALIDO = "X" And Not (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte And objNF.VNF_CLASSIFICACAO = modNF.tipo_cte_debito_posterior)) Then
                Return
            End If

            '--> SE ALGUM DOCUMENTO ESTIVER EM PROCESSAMENTO, O SISTEMA DEVE AGUARDAR A CONCLUSÃO
            Dim datRequisicao As DateTime = DateTime.Now
            strNfeId = objNF.VNF_CHAVE_ACESSO

            If Not isSomenteLeitura Then
                modSQL.ExecuteNonQuery("INSERT INTO TbIntegracaoPostagens (ipo_nfeid, ipo_usuario, ipo_data_inclusao) VALUES ('" & strNfeId & "', '" & Usuario & "', '" & datRequisicao.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            End If

            Dim intIdPostagem As Integer = modSQL.ExecuteScalar("SELECT id_integracao_postagem FROM TbIntegracaoPostagens WHERE ipo_nfeid = '" & strNfeId & "'").ToString().ToInt()
            'Do
            '    Threading.Thread.Sleep(2000)
            'Loop While modSQL.ExecuteScalar("SELECT count(*) FROM TbIntegracaoPostagens WHERE ipo_data_conclusao is null and id_integracao_postagem < " & intIdPostagem).ToString().ToInt() > 0

            Dim strCnpjNf As String = ""
            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) Then
                strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
            Else
                If (objNF.CT_IDE_TOMA = "0") Then
                    strCnpjNf = objNF.NF_REM_CNPJ.ToString().RemoveLetters()
                ElseIf objNF.CT_IDE_TOMA = "1" Then
                    strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
                ElseIf objNF.CT_IDE_TOMA = "2" Then
                    strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
                ElseIf objNF.CT_IDE_TOMA = "3" Then
                    strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
                ElseIf objNF.CT_IDE_TOMA = "4" Then
                    strCnpjNf = objNF.CT_TOMA_CNPJ.ToString().RemoveLetters()
                End If
            End If

            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_NFS) Then
                strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
            End If

            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_FAT) Then
                strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
            End If

            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_TLC) Then
                strCnpjNf = objNF.NF_DEST_CNPJ.ToString().RemoveLetters()
            End If
            '--> O MÉTODO PRECISA IR NO SAP PARA BUSCAR INFORMAÇÕES BÁSICAS PARA DEFINIR O MODO e PROCESSO
            Dim objFilter As New SAP_RFC.PurchaseOrderFilter()
            objFilter.PurchaseOrderNumber = item.NF_PROD_XPED
            objFilter.PurchaseOrderItemNumber = item.NF_PROD_NITEMPED
            objFilter.NFCreationDate = objNF.NF_IDE_DHEMI
            objFilter.NFItemQuantity = item.NF_PROD_QCOM
            objFilter.NFItemValue = item.NF_PROD_VPROD
            objFilter.NFModel = objNF.NF_IDE_MOD
            objFilter.NFCategory = ""
            objFilter.NFCnpjMetso = strCnpjNf
            objFilter.NFFreightAmount = item.NF_PROD_VFRETE
            objFilter.NFInsuranceAmount = item.NF_PROD_VSEG
            objFilter.NFDiscountAmount = item.NF_PROD_VDESC
            objFilter.NFOtherExpensesAmount = item.NF_PROD_VOUTRO
            objFilter.CTStateOfOrigin = objNF.NF_EMIT_UF
            objFilter.CTStateOfFinalDestination = objNF.NF_DEST_UF
            objFilter.MovementType = String.Empty
            objFilter.NFCfop = item.NF_PROD_CFOP.RemoveLetters()
            objFilter.GetRequesterInfo = False

            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte AndAlso objNF.VNF_CLASSIFICACAO = modNF.tipo_cte_debito_posterior) Then
                'ToDo: Adicionar trava quando não houver vendor code definido e quando não encontrar company code
                Dim objBLNotaFiscal As New BLNotaFiscal()
                objFilter.VendorCnpj = objNF.NF_EMIT_CNPJ
                objFilter.VendorCode = objBLNotaFiscal.GetVendorCodeFretes(objNF.VNF_CHAVE_ACESSO, objNF.NF_EMIT_CNPJ)
                objFilter.TaxCode = IIf(objNF.NF_ICMSTOT_VICMS > 0, "F2", "F1")
                objFilter.CompanyCode = modSQL.ExecuteScalar("SELECT top 1 company_code FROM TbPlantaCnpj WHERE cnpj = '" & objNF.CT_TOMA_CNPJ & "'").ToString()
                objFilter.Plant = modSQL.ExecuteScalar("SELECT top 1 planta FROM TbPlantaCnpj WHERE cnpj = '" & objNF.CT_TOMA_CNPJ & "'").ToString()
            End If

            objFilter.ItemTaxes = New List(Of SAP_RFC.PurchaseOrderItemsTaxes)
            objFilter.GoodsMovementTypes = New List(Of SAP_RFC.GoodsMovementType)
            objFilter.DeliveryNotes = New List(Of SAP_RFC.PurchaseOrderDeliveryNoteSearchFilter)
            objFilter.ComponentsHeader = New List(Of SAP_RFC.PurchaseOrderSearchFilterHeaderComponents)
            objFilter.ComponentListCollection = New List(Of SAP_RFC.PurchaseOrderComponentList)

            CarregarTiposMovimento(objFilter)

            Dim objRfcReturn As New SAP_RFC.RfcReturn
            Dim objPurchaseOrder As SAP_RFC.PurchaseOrder
            If Not (String.IsNullOrWhiteSpace(item.NF_PROD_XPED)) Then
                objPurchaseOrder = SAP_RFC.getPurchaseOrder(objFilter, objRfcReturn)

                If Not isSomenteLeitura Then
                    If (Not objRfcReturn.Success) Then
                        If (Not objRfcReturn.Exception Is Nothing) Then
                            RegistrarLog(TipoProcessamento.Servico, Nothing, "SAP_EXCEPTION",
                                                                            "PEDIDO: " & item.NF_PROD_XPED & " - CONNECTION: " & modSAP.RfcConStr(),
                                                                            objRfcReturn.Exception.Message.Replace("'", "") & " - " & objRfcReturn.Exception.StackTrace.Replace("'", ""))
                        End If
                        modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
                    End If
                End If
            End If

            Dim objPurchaseOrderItem As SAP_RFC.PuchaseOrderItems
            If (Not objPurchaseOrder.PURCHASE_ORDER_ITEMS Is Nothing) Then
                objPurchaseOrderItem = objPurchaseOrder.PURCHASE_ORDER_ITEMS.FirstOrDefault()
            End If

            Dim strMoeda = IIf(objPurchaseOrder.CURRENCY Is Nothing OrElse String.IsNullOrEmpty(objPurchaseOrder.CURRENCY), "BRL", objPurchaseOrder.CURRENCY).ToString()
            Dim strPlanta = IIf(objPurchaseOrderItem.PLANT Is Nothing OrElse String.IsNullOrEmpty(objPurchaseOrderItem.PLANT), "", objPurchaseOrderItem.PLANT).ToString()
            Dim strMaterial = IIf(objPurchaseOrderItem.MATERIAL Is Nothing OrElse String.IsNullOrEmpty(objPurchaseOrderItem.MATERIAL), "", objPurchaseOrderItem.MATERIAL).ToString()
            Dim strCategoriaContabil = IIf(objPurchaseOrderItem.ACCOUNT_ASSIGNMENT_CATEGORY Is Nothing OrElse String.IsNullOrEmpty(objPurchaseOrderItem.ACCOUNT_ASSIGNMENT_CATEGORY), "", objPurchaseOrderItem.ACCOUNT_ASSIGNMENT_CATEGORY).ToString()
            Dim strCFOP = IIf(item.NF_PROD_CFOP Is Nothing, "", item.NF_PROD_CFOP).ToString()
            Dim strItemCategory = IIf(objPurchaseOrderItem.ITEM_CATEGORY Is Nothing OrElse String.IsNullOrEmpty(objPurchaseOrderItem.ITEM_CATEGORY), "", objPurchaseOrderItem.ITEM_CATEGORY).ToString()

            Dim strQuery As String = "SELECT DISTINCT " &
                                     "   id_modo_processo, " &
                                     "   mpd_modo, " &
                                     "   mdp_processo, " &
                                     "   mdp_tipo_movimento_migo, " &
                                     "   mdp_tipo_miro, " &
                                     "   mdp_tipo_nf, " &
                                     "   mod_tipo_documento, " &
                                     "   mod_moeda, " &
                                     "   mod_planta, " &
                                     "   mod_tipo_material, " &
                                     "   mod_max_caracteres_num_nf, " &
                                     "   mod_prioridade, " &
                                     "   mod_tipo_frete, " &
                                     "   isnull(mdp_aguardar_liberacao_migo, 0) as 'mdp_aguardar_liberacao_migo', " &
                                     "   isnull(mdp_criar_miro, 0) as 'mdp_criar_miro', " &
                                     "   isnull(mdp_debito_posterior, 0) as 'mdp_debito_posterior', " &
                                     "   mod_item_category, " &
                                     "   mod_nf_complementar " &
                                     " FROM " &
                                     "   TbModoProcesso " &
                                     "   inner join TbModoProcessoDetalhe on mod_id_modo_processo_detalhe = id_modo_processo_detalhe "

            Dim intIdModoProcesso As Integer = 0
            Dim intPrioridade As Integer
            Dim intPrioridadeAtual As Integer = 0
            Dim intQtdMaxMatching As Integer = 0
            Dim intQtdAtualMatching As Integer = 0
            Dim dttModoProcesso As New DataTable()
            dttModoProcesso = modSQL.Fill(strQuery)
            Dim CondicaoAtivada As Boolean

            Dim dttModoProcessoCfop As New DataTable()
            dttModoProcessoCfop = modSQL.Fill("SELECT * FROM TbModoProcessoCfop")

            Dim dttModoProcessoCategoriaContabil As New DataTable()
            dttModoProcessoCategoriaContabil = modSQL.Fill("SELECT * FROM TbModoProcessoCategoriaContabil")

            For Each dtrLinha As DataRow In dttModoProcesso.Rows
                CondicaoAtivada = True
                intQtdAtualMatching = 0
                intPrioridadeAtual = 0

                If Not String.IsNullOrEmpty(dtrLinha("mod_tipo_documento").ToString()) Then
                    If (objNF.VNF_TIPO_DOCUMENTO <> dtrLinha("mod_tipo_documento").ToString()) Then
                        CondicaoAtivada = False
                        Continue For
                    Else
                        intQtdAtualMatching += 1
                    End If
                End If

                If (Not String.IsNullOrEmpty(dtrLinha("mod_moeda").ToString())) Then
                    If (strMoeda <> dtrLinha("mod_moeda").ToString()) Then
                        CondicaoAtivada = False
                        Continue For
                    Else
                        intQtdAtualMatching += 1
                    End If
                End If

                If (Not String.IsNullOrEmpty(dtrLinha("mod_planta").ToString())) Then
                    If (strPlanta <> dtrLinha("mod_planta").ToString()) Then
                        CondicaoAtivada = False
                        Continue For
                    Else
                        intQtdAtualMatching += 1
                    End If
                End If

                If (Not String.IsNullOrEmpty(dtrLinha("mod_tipo_material").ToString())) Then
                    If (Not strMaterial.StartsWith(dtrLinha("mod_tipo_material").ToString())) Then
                        CondicaoAtivada = False
                        Continue For
                    Else
                        intQtdAtualMatching += 1
                    End If
                End If

                If strItemCategory = "3" Then

                    If (Not String.IsNullOrEmpty(dtrLinha("mod_item_category").ToString())) Then
                        If (strItemCategory.Replace("3", "L") <> dtrLinha("mod_item_category").ToString() Or objNF.NF_IDE_FINNFE = 2) Then
                            CondicaoAtivada = False
                            Continue For
                        Else
                            intQtdAtualMatching += 1
                            item.VNF_IS_SUBCONTRATACAO = True
                        End If
                    End If
                End If
                If IsNFComplementar Then
                    If (Not String.IsNullOrEmpty(dtrLinha("mod_nf_complementar").ToString())) Then
                        If (dtrLinha("mod_nf_complementar").ToString() <> "S") Then
                            CondicaoAtivada = False
                            Continue For
                        Else
                            intQtdAtualMatching += 1
                        End If
                    End If
                End If


                If (Not String.IsNullOrEmpty(dtrLinha("mod_max_caracteres_num_nf").ToString())) Then
                    If (objNF.NF_IDE_NNF.Length > Convert.ToInt32(dtrLinha("mod_max_caracteres_num_nf").ToString())) Then
                        CondicaoAtivada = False
                        Continue For
                    Else
                        intQtdAtualMatching += 1
                    End If
                End If

                If (Not String.IsNullOrEmpty(dtrLinha("mod_tipo_frete").ToString())) Then
                    If (objNF.VNF_CLASSIFICACAO <> dtrLinha("mod_tipo_frete").ToString()) Then
                        CondicaoAtivada = False
                        Continue For
                    Else
                        intQtdAtualMatching += 1
                    End If
                End If

                If (dttModoProcessoCfop.Select("mpc_id_modo_processo = " & dtrLinha("id_modo_processo").ToString()).ToList().Count > 0) Then
                    If (dttModoProcessoCfop.Select("mpc_id_modo_processo = " & dtrLinha("id_modo_processo").ToString() & " and mpc_cfop_codigo = '" & strCFOP & "' ").ToList().Count = 0) Then
                        CondicaoAtivada = False
                        Continue For
                    Else
                        intQtdAtualMatching += 1
                    End If
                End If

                If (dttModoProcessoCategoriaContabil.Select("mcc_id_modo_processo = " & dtrLinha("id_modo_processo").ToString()).ToList().Count > 0) Then
                    If (dttModoProcessoCategoriaContabil.Select("mcc_id_modo_processo = " & dtrLinha("id_modo_processo").ToString() & " and mcc_categoria_contabil = '" & strCategoriaContabil & "' ").ToList().Count = 0) Then
                        CondicaoAtivada = False
                        Continue For
                    Else
                        intQtdAtualMatching += 1
                    End If
                End If

                If (dtrLinha("mod_prioridade").ToString() <> "") Then
                    intPrioridadeAtual = Convert.ToInt32(dtrLinha("mod_prioridade").ToString())
                Else
                    intPrioridadeAtual = 0
                End If

                Dim auxPrioridade As Integer = 0
                If (dtrLinha("mod_prioridade").ToString() <> "") Then
                    auxPrioridade = Convert.ToInt32(dtrLinha("mod_prioridade").ToString())
                End If

                If (intQtdAtualMatching > intQtdMaxMatching) Then
                    intQtdMaxMatching = intQtdAtualMatching
                    intIdModoProcesso = Convert.ToInt32(dtrLinha("id_modo_processo").ToString())
                    intPrioridade = auxPrioridade
                ElseIf (intPrioridadeAtual > intPrioridade And intQtdAtualMatching = intQtdMaxMatching) Then
                    intQtdMaxMatching = intQtdAtualMatching
                    intIdModoProcesso = Convert.ToInt32(dtrLinha("id_modo_processo").ToString())
                    intPrioridade = Convert.ToInt32(dtrLinha("mod_prioridade").ToString())
                ElseIf (intPrioridadeAtual = intPrioridade And intQtdAtualMatching = intQtdMaxMatching) Then
                    intIdModoProcesso = 0
                    Exit For
                End If
            Next

            If (intIdModoProcesso > 0) Then
                Dim dttModoProcessoDetalhe As New DataTable
                dttModoProcessoDetalhe = modSQL.Fill("SELECT * FROM TbModoProcesso inner join TbModoProcessoDetalhe on mod_id_modo_processo_detalhe = id_modo_processo_detalhe WHERE id_modo_processo = " & intIdModoProcesso)
                item.VNF_ID_MODO_PROCESSO = dttModoProcessoDetalhe.Rows(0)("id_modo_processo").ToString().ToInt()
                item.MDP_MODO = dttModoProcessoDetalhe.Rows(0)("mpd_modo").ToString()
                item.MDP_PROCESSO = dttModoProcessoDetalhe.Rows(0)("mdp_processo").ToString()
                item.MDP_TIPO_MOVIMENTO_MIGO = dttModoProcessoDetalhe.Rows(0)("mdp_tipo_movimento_migo").ToString()
                item.MDP_AGUARDAR_LIBERACAO_MIGO = dttModoProcessoDetalhe.Rows(0)("mdp_aguardar_liberacao_migo").ToString().ToBoolean()
                item.MDP_CRIAR_MIRO = dttModoProcessoDetalhe.Rows(0)("mdp_criar_miro").ToString().ToBoolean()
                'Marcio Spinosa - 15/04/2019 - CR00009165
                'item.MDP_TIPO_MIRO = dttModoProcessoDetalhe.Rows(0)("mdp_tipo_miro").ToString()
                item.MDP_TIPO_MIRO = If(analisarDivergencias(objNF.VNF_CHAVE_ACESSO) >= 1, "M", dttModoProcessoDetalhe.Rows(0)("mdp_tipo_miro").ToString())
                'Marcio Spinosa - 15/04/2019 - CR00009165 - Fim
                item.MDP_DEBITO_POSTERIOR = dttModoProcessoDetalhe.Rows(0)("mdp_debito_posterior").ToString().ToBoolean()
                item.MDP_TIPO_NF = dttModoProcessoDetalhe.Rows(0)("mdp_tipo_nf").ToString()
                item.MDP_ENVIAR_TAXCODE_MIGO = dttModoProcessoDetalhe.Rows(0)("mdp_enviar_taxcode_migo").ToString().ToBoolean()
            End If
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao determinar o modo/processo")
        Finally
            If Not isSomenteLeitura Then
                modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
            End If
        End Try
    End Sub
#End Region

    'Marcio Spinosa - 15/04/2019 - CR00009165
    Private Function analisarDivergencias(ByRef pNfeid As String) As Integer
        Try
            Dim dtCount As DataTable = modSQL.Fill("select  count(1) from tblog where (campo = 'ICMS' or 
                                                                                       campo = 'IPI' or
                                                                                       campo = 'VALOR BRUTO' or 
                                                                                       campo = 'VALOR BRUTO COM IMPOSTOS' or
                                                                                       campo = 'VALOR LÍQUIDO' or
                                                                                       campo = 'VALOR DA NOTA FISCAL') and nfeid= '" & pNfeid & "' AND motivo = 'ANULADO' ")
            If (dtCount.Rows.Count > 0) Then
                Return Convert.ToInt32(dtCount.Rows(0)(0).ToString())
            Else
                Return 0
            End If
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao determinar o modo/processo")
        Finally
            'Return 0
        End Try

    End Function
    'Marcio Spinosa - 15/04/2019 - CR00009165 - Fim


#Region " CarregarTiposMovimento "
    Private Sub CarregarTiposMovimento(ByRef pPurchaseOrderFilter As SAP_RFC.PurchaseOrderFilter)
        Try
            Dim objMovementType As New SAP_RFC.GoodsMovementType()
            Dim dttGMType As New DataTable
            dttGMType = modSQL.Fill("SELECT * FROM TbGoodsMovementType")
            For Each dtrGMType As DataRow In dttGMType.Rows
                objMovementType = New SAP_RFC.GoodsMovementType()
                objMovementType.MOVEMENT_TYPE = dtrGMType("GoodsMovementType").ToString()
                pPurchaseOrderFilter.GoodsMovementTypes.Add(objMovementType)
            Next
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao carregar os tipos de movimento de MIGO")
        End Try
    End Sub
#End Region

#Region " CarregarDeliveryNotesFilter "
    Private Sub CarregarFiltroDeliveryNotes(ByRef pPurchaseOrderFilter As SAP_RFC.PurchaseOrderFilter, ByVal pStrNFEID As String, ByVal pIntItemNF As Integer, ByVal pStrCNPJ As String, ByVal pIntModelo As Integer)
        Try
            Dim objSearchFilter As New SAP_RFC.PurchaseOrderDeliveryNoteSearchFilter()
            Dim dttGMType As New DataTable
            dttGMType = modSQL.Fill("SELECT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_CONSIGNACAO_REFNF WHERE NFEID = '" & pStrNFEID & "' AND ITEM_NF = '" & pIntItemNF & "'")
            For Each dtrGMType As DataRow In dttGMType.Rows
                objSearchFilter = New SAP_RFC.PurchaseOrderDeliveryNoteSearchFilter()
                objSearchFilter.NFNUM = dtrGMType("NUMERO_REFNF")
                objSearchFilter.SERIES = dtrGMType("SERIE_REFNF")
                objSearchFilter.CGC = pStrCNPJ
                objSearchFilter.MODEL = pIntModelo
                pPurchaseOrderFilter.DeliveryNotes.Add(objSearchFilter)
            Next
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao carregar os dados do cabeçalho de busca das DeliveryNotes")
        End Try
    End Sub
#End Region

#Region " CarregarNotaComplementar "
    Private Sub CarregarFiltroNotaComplementar(ByRef pPurchaseOrderFilter As SAP_RFC.PurchaseOrderFilter, ByVal pStrNFEID As String, ByVal pIntItemNF As Integer, ByVal pStrCNPJ As String, ByVal pIntModelo As Integer)
        Try
            Dim objSearchFilter As New SAP_RFC.PurchaseOrderDeliveryNoteSearchFilter()
            Dim dttGMType As New DataTable
            dttGMType = modSQL.Fill("SELECT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_NOTA_COMPLEMENTAR_REFNF WHERE NFEID = '" & pStrNFEID & "' AND ITEM_NF = '" & pIntItemNF & "'")
            For Each dtrGMType As DataRow In dttGMType.Rows
                objSearchFilter = New SAP_RFC.PurchaseOrderDeliveryNoteSearchFilter()
                objSearchFilter.NFNUM = dtrGMType("NUMERO_REFNF")
                objSearchFilter.SERIES = dtrGMType("SERIE_REFNF")
                objSearchFilter.CGC = pStrCNPJ
                objSearchFilter.MODEL = pIntModelo
                pPurchaseOrderFilter.DeliveryNotes.Add(objSearchFilter)
            Next
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao carregar os dados do cabeçalho de busca das DeliveryNotes")
        End Try
    End Sub
#End Region


#Region " CarregaListadeComponentesSubcontratacao "
    Private Sub CarregarListaComponentes(ByRef pPurchaseOrderFilter As SAP_RFC.PurchaseOrderFilter, ByVal pStrNFEID As String, ByVal pIntItemNF As Integer, ByVal pStrCNPJ As String, ByVal pIntModelo As Integer)
        Try
            'Carregando filtros de busca de cabeçalhos da remessa para identificação dos componentes e validação se o documento (nota) encontra-se válida
            Dim objSearchFilter As New SAP_RFC.PurchaseOrderSearchFilterHeaderComponents()
            Dim objSearchFilterRemessa As New SAP_RFC.PurchaseOrderDeliveryNoteSearchFilter()
            Dim dttGMType As New DataTable
            dttGMType = modSQL.Fill("SELECT DISTINCT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_SUBCONTRATACAO_REFNF WHERE NFEID = '" & pStrNFEID & "' AND ITEM_NF = '" & pIntItemNF & "'")
            For Each dtrGMType As DataRow In dttGMType.Rows
                objSearchFilter = New SAP_RFC.PurchaseOrderSearchFilterHeaderComponents()
                objSearchFilterRemessa = New SAP_RFC.PurchaseOrderDeliveryNoteSearchFilter()

                objSearchFilter.NFNUM = dtrGMType("NUMERO_REFNF")
                objSearchFilterRemessa.NFNUM = dtrGMType("NUMERO_REFNF")
                objSearchFilter.SERIES = dtrGMType("SERIE_REFNF")
                objSearchFilterRemessa.SERIES = dtrGMType("SERIE_REFNF")
                objSearchFilter.CGC = pStrCNPJ
                objSearchFilterRemessa.CGC = pStrCNPJ
                objSearchFilter.MODEL = pIntModelo
                objSearchFilterRemessa.MODEL = pIntModelo
                pPurchaseOrderFilter.ComponentsHeader.Add(objSearchFilter)
                pPurchaseOrderFilter.DeliveryNotes.Add(objSearchFilterRemessa)
            Next

            'Itens para busca
            Dim vObjComponentListItemSearch As New SAP_RFC.PurchaseOrderComponentList()
            Dim dttGMTypeItens As New DataTable
            dttGMTypeItens = modSQL.Fill("SELECT NUMERO_REFNF, SERIE_REFNF, ITEM_REFNF FROM TBDOC_SUBCONTRATACAO_REFNF WHERE NFEID = '" & pStrNFEID & "' AND ITEM_NF = '" & pIntItemNF & "'")
            For Each drItem As DataRow In dttGMTypeItens.Rows
                vObjComponentListItemSearch = New SAP_RFC.PurchaseOrderComponentList()
                vObjComponentListItemSearch.NF_REMESSA_XBLNR = drItem("NUMERO_REFNF") & "-" & drItem("SERIE_REFNF")
                vObjComponentListItemSearch.NFE_ITEM_ITMNUM = drItem("ITEM_REFNF")
                pPurchaseOrderFilter.ComponentListCollection.Add(vObjComponentListItemSearch)
            Next

        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao carregar o filtro de busca de componetes da subcontratação")
        End Try
    End Sub
#End Region


#Region " CarregarTiposMovimento Overload "
    Private Function CarregarTiposMovimento() As String
        Try
            Dim objMovementType As New SAP_RFC.GoodsMovementType()
            Dim vObjRetorno As Object

            vObjRetorno = modSQL.ExecuteScalar("SELECT GoodsMovementType + ',' AS 'data()' FROM TbGoodsMovementType FOR XML PATH('')", modSQL.connectionString)
            Return vObjRetorno.ToString()
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao carregar os tipos de movimento de MIGO")
        End Try
    End Function
#End Region

#Region " VerificarRelevanciaItens "
    ''' <summary>
    ''' Este é para os casos de compras de serviço de industrialização. O fornecedor pode aplicar o próprio serviço e materiais neste processo
    ''' Alguns fornecedores faturam tudo no mesmo item da nota e alguns faturam em 2 itens da nota, onde um item é o serviço e o outro, o material aplicado
    ''' O valor unitário, deve ser somado, pois no pedido de compra, este valor não é separado em 2 itens.
    ''' </summary>
    ''' Método não está sendo utilizado no sistema.
    Private Sub VerificarRelevanciaItens()
        Try
            For i As Integer = 0 To objNF.ITENS_NF.Count() - 1
                For y As Integer = i + 1 To objNF.ITENS_NF.Count() - 1
                    If objNF.ITENS_NF(i).SAP_PO_NUMBER = objNF.ITENS_NF(y).SAP_PO_NUMBER And objNF.ITENS_NF(i).SAP_ITEM_DETAILS.ITEM_NUMBER = objNF.ITENS_NF(y).SAP_ITEM_DETAILS.ITEM_NUMBER Then

                        If (objNF.ITENS_NF(i).NF_PROD_CFOP = "5124" And objNF.ITENS_NF(y).NF_PROD_CFOP = "5124") Or (objNF.ITENS_NF(i).NF_PROD_CFOP = "6124" And objNF.ITENS_NF(y).NF_PROD_CFOP = "6124") Then
                            modSQL.CommandText = "update TbLOG set SITUACAO = 'INATIVO', MOTIVO = 'CORRIGIDO PELO SISTEMA. CFOP: " & objNF.ITENS_NF(i).NF_PROD_CFOP & "', DATA_CORRECAO = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' " & " where CODJUN = " & objNF.ITENS_NF(i).VNF_CODJUN & " and CAMPO = 'VALOR'"
                            modSQL.ExecuteNonQuery(modSQL.CommandText)


                            modSQL.CommandText = "update TbLOG set SITUACAO = 'INATIVO', MOTIVO = 'CORRIGIDO PELO SISTEMA. CFOP: " & objNF.ITENS_NF(i).NF_PROD_CFOP & "', DATA_CORRECAO = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' " & " where CODJUN = " & objNF.ITENS_NF(y).VNF_CODJUN & " and CAMPO = 'VALOR'"
                            modSQL.ExecuteNonQuery(modSQL.CommandText)

                            objNF.ITENS_NF(y).VNF_ITEM_VALIDO = "N"
                            objNF.ITENS_NF(i).NF_PROD_VUNCOM = objNF.ITENS_NF(i).NF_PROD_VUNCOM + objNF.ITENS_NF(y).NF_PROD_VUNCOM
                            Exit For
                        End If

                    End If
                Next
            Next
        Catch ex As Exception
            RegistrarLog(Nothing, ex)
            Throw New Exception("Ocorreu um erro ao verificar relevância dos itens")
        End Try
    End Sub
#End Region

#Region " NaoRelevante "
    ''' <summary>
    ''' S: ITEM VÁLIDO
    ''' N: ITEM INVÁLIDO
    ''' X: ITEM NÃO RELEVANTE
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function NaoRelevante() As Boolean
        Try
            For Each objItemNF As modNFItem In objNF.ITENS_NF
                If (objItemNF.VNF_ITEM_VALIDO = "S" Or objItemNF.VNF_ITEM_VALIDO = "N") Then
                    Return False
                End If
            Next

            Return True
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao verificar a relevância dos itens")
        End Try
    End Function
#End Region

#Region " InserirDivergencia "
    Public Sub InserirDivergencia(ByVal pItemNF As modNFItem, ByVal pCampo As String, ByVal pValorNF As String, ByVal pValorSAP As String, Optional ByVal pDetalhe As String = "")
        Try
            If JaExisteLog(pItemNF.VNF_CODJUN, pCampo, objNF.VNF_CHAVE_ACESSO) Then
                Dim strQuery As String = "UPDATE " &
                                         "	TbLOG " &
                                         "SET " &
                                         "	 NFEID = ISNULL(@NFEID, NFEID) " &
                                         "	,ITENFE = ISNULL(@ITENFE, ITENFE) " &
                                         "	,DATLOG = ISNULL(@DATLOG, DATLOG) " &
                                         "	,VALOR_NFE = ISNULL(@VALOR_NFE, VALOR_NFE) " &
                                         "	,VALOR_PED = ISNULL(@VALOR_PED, VALOR_PED) " &
                                         "	,CODCOM = ISNULL(@CODCOM, CODCOM) " &
                                         "	,CODFOR = ISNULL(@CODFOR, CODFOR) " &
                                         "	,SITUACAO = ISNULL(@SITUACAO, SITUACAO) " &
                                         "	,MOTIVO = ISNULL(@MOTIVO, MOTIVO) " &
                                         "	,DATA_CORRECAO = ISNULL(@DATA_CORRECAO, DATA_CORRECAO) " &
                                         "	,PEDCOM = ISNULL(@PEDCOM, PEDCOM) " &
                                         "	,ITEPED = ISNULL(@ITEPED, ITEPED) " &
                                         "	,JUSTIFICATIVA = ISNULL(@JUSTIFICATIVA, JUSTIFICATIVA) " &
                                         "	,CODMAT = ISNULL(@CODMAT, CODMAT) " &
                                         "	,DESMAT = ISNULL(@DESMAT, DESMAT) " &
                                         "	,DETALHE = ISNULL(@DETALHE, DETALHE) " &
                                         "	,ANEXO = ISNULL(@ANEXO, ANEXO) " &
                                         "	,ANEXONOME = ISNULL(@ANEXONOME, ANEXONOME) " &
                                         "	,ANEXOEXTENSAO = ISNULL(@ANEXOEXTENSAO, ANEXOEXTENSAO) " &
                                         "WHERE " &
                                         "	CODJUN = @CODJUN and " &
                                         "	CAMPO = @CAMPO and " &
                                         "	isnull(ACECODUSU, '') = '' "

                Dim sqlparams As New List(Of SqlClient.SqlParameter)
                sqlparams.Add(modSQL.AddSqlParameter("CODJUN", SqlDbType.Decimal, pItemNF.VNF_CODJUN))
                sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, objNF.VNF_CHAVE_ACESSO))
                sqlparams.Add(modSQL.AddSqlParameter("ITENFE", SqlDbType.Int, pItemNF.NF_PROD_ITEM))
                sqlparams.Add(modSQL.AddSqlParameter("CAMPO", SqlDbType.VarChar, pCampo))
                sqlparams.Add(modSQL.AddSqlParameter("DATLOG", SqlDbType.DateTime, DateTime.Now))
                sqlparams.Add(modSQL.AddSqlParameter("VALOR_NFE", SqlDbType.VarChar, pValorNF))
                sqlparams.Add(modSQL.AddSqlParameter("VALOR_PED", SqlDbType.VarChar, pValorSAP))
                sqlparams.Add(modSQL.AddSqlParameter("CODCOM", SqlDbType.VarChar, objNF.SAP_DETAILS.PURCHASING_GROUP))
                sqlparams.Add(modSQL.AddSqlParameter("CODFOR", SqlDbType.VarChar, objNF.SAP_DETAILS.VENDOR_CODE))
                sqlparams.Add(modSQL.AddSqlParameter("SITUACAO", SqlDbType.VarChar, "ATIVO"))
                sqlparams.Add(modSQL.AddSqlParameter("MOTIVO", SqlDbType.VarChar, String.Empty))
                sqlparams.Add(modSQL.AddSqlParameter("DATA_CORRECAO", SqlDbType.DateTime, Nothing))
                sqlparams.Add(modSQL.AddSqlParameter("PEDCOM", SqlDbType.VarChar, pItemNF.NF_PROD_XPED))
                sqlparams.Add(modSQL.AddSqlParameter("ITEPED", SqlDbType.Int, pItemNF.NF_PROD_NITEMPED))
                sqlparams.Add(modSQL.AddSqlParameter("JUSTIFICATIVA", SqlDbType.VarChar, String.Empty))
                sqlparams.Add(modSQL.AddSqlParameter("CODMAT", SqlDbType.VarChar, pItemNF.SAP_ITEM_DETAILS.MATERIAL))
                sqlparams.Add(modSQL.AddSqlParameter("DESMAT", SqlDbType.VarChar, pItemNF.SAP_ITEM_DETAILS.MATERIAL_DESCRIPTION))
                sqlparams.Add(modSQL.AddSqlParameter("DETALHE", SqlDbType.VarChar, pDetalhe))
                sqlparams.Add(modSQL.AddSqlParameter("ANEXO", SqlDbType.VarBinary, Nothing))
                sqlparams.Add(modSQL.AddSqlParameter("ANEXONOME", SqlDbType.VarChar, Nothing))
                sqlparams.Add(modSQL.AddSqlParameter("ANEXOEXTENSAO", SqlDbType.VarChar, Nothing))
                modSQL.ExecuteNonQueryParams(strQuery, sqlparams)

            Else
                Dim strQuery As String = "INSERT INTO TbLOG " &
                                         "( " &
                                         "	 CODJUN " &
                                         "	,NFEID " &
                                         "	,ITENFE " &
                                         "	,CAMPO " &
                                         "	,DATLOG " &
                                         "	,VALOR_NFE " &
                                         "	,VALOR_PED " &
                                         "	,CODCOM " &
                                         "	,CODFOR " &
                                         "	,SITUACAO " &
                                         "	,MOTIVO " &
                                         "	,DATA_CORRECAO " &
                                         "	,PEDCOM " &
                                         "	,ITEPED " &
                                         "	,acecodusu " &
                                         "	,JUSTIFICATIVA " &
                                         "	,CODMAT " &
                                         "	,DESMAT " &
                                         "	,DETALHE " &
                                         "	,ANEXO " &
                                         "	,ANEXONOME " &
                                         "	,ANEXOEXTENSAO " &
                                         ") " &
                                         "VALUES " &
                                         "( " &
                                         "	 @CODJUN " &
                                         "	,@NFEID " &
                                         "	,@ITENFE " &
                                         "	,@CAMPO " &
                                         "	,@DATLOG " &
                                         "	,@VALOR_NFE " &
                                         "	,@VALOR_PED " &
                                         "	,@CODCOM " &
                                         "	,@CODFOR " &
                                         "	,@SITUACAO " &
                                         "	,@MOTIVO " &
                                         "	,@DATA_CORRECAO " &
                                         "	,@PEDCOM " &
                                         "	,@ITEPED " &
                                         "	,@acecodusu " &
                                         "	,@JUSTIFICATIVA " &
                                         "	,@CODMAT " &
                                         "	,@DESMAT " &
                                         "	,@DETALHE " &
                                         "	,@ANEXO " &
                                         "	,@ANEXONOME " &
                                         "	,@ANEXOEXTENSAO " &
                                         ") "

                Dim sqlparams As New List(Of SqlClient.SqlParameter)
                sqlparams.Add(modSQL.AddSqlParameter("CODJUN", SqlDbType.Decimal, pItemNF.VNF_CODJUN))
                sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, objNF.VNF_CHAVE_ACESSO))
                sqlparams.Add(modSQL.AddSqlParameter("ITENFE", SqlDbType.Int, pItemNF.NF_PROD_ITEM))
                sqlparams.Add(modSQL.AddSqlParameter("CAMPO", SqlDbType.VarChar, pCampo))
                sqlparams.Add(modSQL.AddSqlParameter("DATLOG", SqlDbType.DateTime, DateTime.Now))
                sqlparams.Add(modSQL.AddSqlParameter("VALOR_NFE", SqlDbType.VarChar, pValorNF))
                sqlparams.Add(modSQL.AddSqlParameter("VALOR_PED", SqlDbType.VarChar, pValorSAP))
                sqlparams.Add(modSQL.AddSqlParameter("CODCOM", SqlDbType.VarChar, objNF.SAP_DETAILS.PURCHASING_GROUP))
                sqlparams.Add(modSQL.AddSqlParameter("CODFOR", SqlDbType.VarChar, objNF.SAP_DETAILS.VENDOR_CODE))
                sqlparams.Add(modSQL.AddSqlParameter("SITUACAO", SqlDbType.VarChar, "ATIVO"))
                sqlparams.Add(modSQL.AddSqlParameter("MOTIVO", SqlDbType.VarChar, String.Empty))
                sqlparams.Add(modSQL.AddSqlParameter("DATA_CORRECAO", SqlDbType.DateTime, Nothing))
                sqlparams.Add(modSQL.AddSqlParameter("PEDCOM", SqlDbType.VarChar, pItemNF.NF_PROD_XPED))
                sqlparams.Add(modSQL.AddSqlParameter("ITEPED", SqlDbType.Int, pItemNF.NF_PROD_NITEMPED))
                sqlparams.Add(modSQL.AddSqlParameter("acecodusu", SqlDbType.VarChar, String.Empty))
                sqlparams.Add(modSQL.AddSqlParameter("JUSTIFICATIVA", SqlDbType.VarChar, String.Empty))
                sqlparams.Add(modSQL.AddSqlParameter("CODMAT", SqlDbType.VarChar, pItemNF.SAP_ITEM_DETAILS.MATERIAL))
                sqlparams.Add(modSQL.AddSqlParameter("DESMAT", SqlDbType.VarChar, pItemNF.SAP_ITEM_DETAILS.MATERIAL_DESCRIPTION))
                sqlparams.Add(modSQL.AddSqlParameter("DETALHE", SqlDbType.VarChar, pDetalhe))
                sqlparams.Add(modSQL.AddSqlParameter("ANEXO", SqlDbType.VarBinary, Nothing))
                sqlparams.Add(modSQL.AddSqlParameter("ANEXONOME", SqlDbType.VarChar, Nothing))
                sqlparams.Add(modSQL.AddSqlParameter("ANEXOEXTENSAO", SqlDbType.VarChar, Nothing))
                modSQL.ExecuteNonQueryParams(strQuery, sqlparams)
            End If
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao inserir a divergência")
        End Try
    End Sub
#End Region

#Region " AnularDivergencia "
    Public Sub AnularDivergencia(ByVal pItemNF As modNFItem, ByVal pCampo As String, ByVal IsExcecao As Boolean, ByRef TextoReprovacao As String)
        Try
            If Not TextoReprovacao Is Nothing Then
                TextoReprovacao = String.Empty
            End If

            If JaExisteLog(pItemNF.VNF_CODJUN, pCampo, objNF.VNF_CHAVE_ACESSO) Then
                If pCampo = "CONDICAO PAGAMENTO" Or pCampo = "APROVADO" Or pCampo = "REGIME ESPECIAL" Then
                    modSQL.CommandText = "update TbLOG set SITUACAO = 'INATIVO', MOTIVO = 'CORRIGIDO', DATA_CORRECAO = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' " & " where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' and CAMPO = '" & pCampo & "'"
                Else
                    modSQL.CommandText = "update TbLOG set SITUACAO = 'INATIVO', MOTIVO = 'CORRIGIDO', DATA_CORRECAO = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' " & " where CODJUN = " & pItemNF.VNF_CODJUN & " and CAMPO = '" & pCampo & "'"
                End If

                modSQL.ExecuteNonQuery(modSQL.CommandText)
            End If
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao anular a divergência")
        End Try
    End Sub
#End Region

#Region " JaExisteLog "
    Private Function JaExisteLog(ByVal CODJUN As String, ByVal CAMPO As String, ByVal NFEID As String) As Boolean
        Try
            If CAMPO = "CONDICAO PAGAMENTO" Or CAMPO = "APROVADO" Or CAMPO = "REGIME ESPECIAL" Then
                modSQL.CommandText = "select isnull(count(*),0) from TbLOG where NFEID = '" & NFEID & "' and CAMPO = '" & CAMPO & "'"
            Else
                modSQL.CommandText = "select isnull(count(*),0) from TbLOG where CODJUN = " & CODJUN & " and CAMPO = '" & CAMPO & "'"
            End If

            Return (modSQL.ExecuteScalar(modSQL.CommandText) <> "0")
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao verificar se já existe divergência")
        End Try
    End Function
#End Region

#Region " CfopRelevante "
    Private Function CfopRelevante(ByVal itemNf As modNFItem) As Boolean
        Try
            '---> 1. BUSCA A DEFINIÇÃO DE RELEVANTE/IRRELEVANTE PELA DEFINIÇÃO DO USUÁRIO
            Dim strRelevate As String = modSQL.ExecuteScalar("SELECT " &
                                                             "	VNF_ITEM_VALIDO " &
                                                             "FROM TbDOC_ITEM " &
                                                             "	inner join tbjun " &
                                                             "	on TbDOC_ITEM.vnf_codjun = tbjun.codjun " &
                                                             "WHERE " &
                                                             "	tbjun.nfeid = '" & objNF.VNF_CHAVE_ACESSO & "' " &
                                                             "	and ITENFE = " & itemNf.NF_PROD_ITEM)
            If (Not String.IsNullOrEmpty(strRelevate)) Then
                Return IIf(strRelevate = "X", False, True)
            End If

            '---> 2. SE NÃO HOUVER DEFINIÇÃO DO USUÁRIO, BUSCA A DEFINIÇÃO DO CNPJ DA EMPRESA

            '---> 3. SE NÃO HOUVER DEFINIÇÃO DO CNPJ, BUSCA A DEFINIÇÃO PELA CFOP DO ITEM
            modSQL.CommandText = "select count(*) from TbPAR where PARAMETRO = 'CFOP' and VALOR = '" & itemNf.NF_PROD_CFOP & "'"
            Return (modSQL.ExecuteScalar(modSQL.CommandText) = "0")
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro verificar se a CFOP é relevante")
        End Try
    End Function
#End Region

#Region " ValidarNCM "
    Private Function ValidarNCM(ByVal MATERIAL As String) As Boolean
        Try
            If Double.TryParse(MATERIAL, Nothing) = True Then
                MATERIAL = Double.Parse(MATERIAL)
            End If

            modSQL.CommandText = "select count(*) from TbPAR where PARAMETRO = 'MATERIAL_NCM' and VALOR = '" & MATERIAL & "'"

            Return (modSQL.ExecuteScalar(modSQL.CommandText) = "0")
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro verificar o material e NCM ")
        End Try
    End Function
#End Region

#Region " AtualizaNCM "
    Private Sub AtualizaNCM(ByVal pItemNF As modNFItem)
        Try
            'Função criada para futura análise de NCM. Trabalho a ser realizado pelo CADASTRAMENTO e FISCAL
            modSQL.CommandText = "update TbJUN set NCMPED = '" & pItemNF.SAP_ITEM_DETAILS.NCM_CODE & "', NCMNFE = '" & pItemNF.NF_PROD_NCM & "' " &
                                 "where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' AND ITENFE = " & pItemNF.NF_PROD_ITEM

            modSQL.ExecuteNonQuery(modSQL.CommandText)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao registrar a NCM do material")
        End Try
    End Sub
#End Region

#Region " ObterStatusDocumento "
    Private Function ObterStatusDocumento() As String
        Try
            modSQL.CommandText = "select top 1 situacao from TbNFE where nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'"
            Return modSQL.ExecuteScalar(modSQL.CommandText)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao obter o status do documento")
        End Try
    End Function
#End Region

#Region " Inserir comparação "
    Private Sub InserirComparacao(ByVal NF_CHAVE_ACESSO As String, ByVal pItemNF As modNFItem, ByVal pCampo As String, ByVal pValorNF As String, ByVal pValorSAP As String, ByVal pDataComparacao As DateTime, ByVal Usuario As String)
        Try
            modSQL.CommandText = "insert into com_comparacao(com_data_hora,com_nfe_id,com_item_id,com_campo,com_valor_df,com_valor_sap, com_usuario, pedcom, iteped) " &
                                         "values('" & pDataComparacao.ToString("yyyy-MM-dd HH:mm:ss") & "','" & NF_CHAVE_ACESSO & "','" & pItemNF.NF_PROD_ITEM & "','" & pCampo & "','" & pValorNF & "','" & pValorSAP & "', '" & Usuario & "', '" & pItemNF.NF_PROD_XPED & "', '" & pItemNF.NF_PROD_NITEMPED & "')"

            modSQL.ExecuteNonQuery(modSQL.CommandText)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao inserir comparação")
        End Try
    End Sub
#End Region

#Region " LerCabecalhoXml "
    Private Sub LerCabecalhoXml(ByVal objXml As XmlDocument, ByRef pObjNf As modNF)
        Try
            Dim objXmlNodeList As XmlNodeList
            Dim objDuplicata As New modNFDuplicata()
            Dim objNotaReferenciada As New modNFReferenciada()
            Dim objNFReferenciada As New modNFReferenciada()

            '--> INFORMAÇÕES DO CABEÇALHO
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("ide")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("nNF") Is Nothing) Then
                    pObjNf.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe
                    pObjNf.NF_IDE_NNF = objXmlNode.Item("nNF").InnerText
                    pObjNf.NF_IDE_SERIE = objXmlNode.Item("serie").InnerText
                    If String.IsNullOrEmpty(pObjNf.VNF_CHAVE_ACESSO) Then pObjNf.VNF_CHAVE_ACESSO = objXml.DocumentElement.GetElementsByTagName("infNFe")(0).Attributes("Id").Value.Substring(3)
                End If
                If (Not objXmlNode.Item("nCT") Is Nothing) Then
                    pObjNf.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte
                    pObjNf.NF_IDE_NNF = objXmlNode.Item("nCT").InnerText
                    pObjNf.NF_IDE_SERIE = objXmlNode.Item("serie").InnerText
                    pObjNf.NF_IDE_MODAL = objXmlNode.Item("modal").InnerText
                    If String.IsNullOrEmpty(pObjNf.VNF_CHAVE_ACESSO) Then pObjNf.VNF_CHAVE_ACESSO = objXml.DocumentElement.GetElementsByTagName("infCte")(0).Attributes("Id").Value.Substring(3)
                End If
                If (Not objXmlNode.Item("dEmi") Is Nothing) Then
                    pObjNf.NF_IDE_DHEMI = DateTimeOffset.Parse(objXmlNode.Item("dEmi").InnerText).DateTime
                End If
                If (Not objXmlNode.Item("dhEmi") Is Nothing) Then
                    pObjNf.NF_IDE_DHEMI = DateTimeOffset.Parse(objXmlNode.Item("dhEmi").InnerText).DateTime
                End If
                If (Not objXmlNode.Item("NFref") Is Nothing) Then
                    pObjNf.NF_IDE_NFREF = objXmlNode.Item("NFref").FirstChild().InnerText()
                End If
                If (Not objXmlNode.Item("cMunIni") Is Nothing) Then
                    pObjNf.CT_IDE_CMUNINI = objXmlNode.Item("cMunIni").InnerText
                End If
                If (Not objXmlNode.Item("xMunIni") Is Nothing) Then
                    pObjNf.CT_IDE_XMUNINI = objXmlNode.Item("xMunIni").InnerText
                End If
                If (Not objXmlNode.Item("UFIni") Is Nothing) Then
                    pObjNf.CT_IDE_UFINI = objXmlNode.Item("UFIni").InnerText
                End If
                If (Not objXmlNode.Item("cMunFim") Is Nothing) Then
                    pObjNf.CT_IDE_CMUNFIM = objXmlNode.Item("cMunFim").InnerText
                End If
                If (Not objXmlNode.Item("xMunFim") Is Nothing) Then
                    pObjNf.CT_IDE_XMUNFIM = objXmlNode.Item("xMunFim").InnerText
                End If
                If (Not objXmlNode.Item("UFFim") Is Nothing) Then
                    pObjNf.CT_IDE_UFFIM = objXmlNode.Item("UFFim").InnerText
                End If
                If (Not objXmlNode.Item("retira") Is Nothing) Then
                    pObjNf.CT_IDE_RETIRA = objXmlNode.Item("retira").InnerText
                End If
                If (Not objXmlNode.Item("tpCTe") Is Nothing) Then
                    pObjNf.CT_IDE_TPCTE = objXmlNode.Item("tpCTe").InnerText
                End If

                'caso seja um cte verifica se o tomado é tipo 4 para preenchimendo dos dados cadastrais
                'caso contrário pega seomente o CNPJ
                If pObjNf.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte Then
                    Dim nsmgr As New XmlNamespaceManager(objXml.NameTable)
                    nsmgr.AddNamespace("xmlnscte", "http://www.portalfiscal.inf.br/cte")

                    If Not objXmlNode.SelectSingleNode("xmlnscte:toma4", nsmgr) Is Nothing Then
                        pObjNf.CT_IDE_TOMA = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:toma", nsmgr).InnerText
                        pObjNf.CT_TOMA_CNPJ = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:CNPJ", nsmgr).InnerText
                        pObjNf.CT_TOMA_IE = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:IE", nsmgr).InnerText
                        pObjNf.CT_TOMA_XNOME = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:xNome", nsmgr).InnerText

                        pObjNf.CT_TOMA_XLGR = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:xLgr", nsmgr).InnerText
                        pObjNf.CT_TOMA_NRO = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:nro", nsmgr).InnerText
                        pObjNf.CT_TOMA_XBAIRRO = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:xBairro", nsmgr).InnerText
                        pObjNf.CT_TOMA_CMUN = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:cMun", nsmgr).InnerText
                        pObjNf.CT_TOMA_XMUN = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:xMun", nsmgr).InnerText

                        'Marcio Spinosa - 21/08/2019 - SR00297427
                        If (Not objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:CEP", nsmgr) Is Nothing) Then
                            pObjNf.CT_TOMA_CEP = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:CEP", nsmgr).InnerText
                        Else
                            pObjNf.CT_TOMA_CEP = ""
                        End If
                        'Marcio Spinosa - 21/08/2019 - SR00297427 - Fim

                        pObjNf.CT_TOMA_UF = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:UF", nsmgr).InnerText

                        If (Not objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:cPais", nsmgr) Is Nothing) Then
                            pObjNf.CT_TOMA_CPAIS = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:cPais", nsmgr).InnerText
                        End If
                        If (Not objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:xPais", nsmgr) Is Nothing) Then
                            pObjNf.CT_TOMA_XPAIS = objXmlNode.SelectSingleNode("xmlnscte:toma4/xmlnscte:enderToma/xmlnscte:xPais", nsmgr).InnerText
                        End If
                    ElseIf (Not objXmlNode.SelectSingleNode("//xmlnscte:toma", nsmgr) Is Nothing) Then
                        pObjNf.CT_IDE_TOMA = objXmlNode.SelectSingleNode("//xmlnscte:toma", nsmgr).InnerText
                    End If

                    Select Case pObjNf.CT_IDE_TOMA
                        Case "0"
                            pObjNf.CT_IDE_TOMA_DESC = "(0) REMETENTE"
                        Case "1"
                            pObjNf.CT_IDE_TOMA_DESC = "(1) EXPEDIDOR"
                        Case "2"
                            pObjNf.CT_IDE_TOMA_DESC = "(2) RECEBEDOR"
                        Case "3"
                            pObjNf.CT_IDE_TOMA_DESC = "(3) DESTINATÁRIO"
                        Case "4"
                            pObjNf.CT_IDE_TOMA_DESC = "(4) OUTROS"
                    End Select
                End If



                If (Not objXmlNode.Item("cUF") Is Nothing) Then
                    pObjNf.NF_IDE_CUF = Convert.ToInt32(objXmlNode.Item("cUF").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("cNF") Is Nothing) Then
                    pObjNf.NF_IDE_CNF = Convert.ToInt32(objXmlNode.Item("cNF").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("indPag") Is Nothing) Then
                    pObjNf.NF_IDE_INDPAG = Convert.ToInt32(objXmlNode.Item("indPag").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("natOp") Is Nothing) Then
                    pObjNf.NF_IDE_NATOP = objXmlNode.Item("natOp").InnerText
                End If
                If (Not objXmlNode.Item("mod") Is Nothing) Then
                    pObjNf.NF_IDE_MOD = Convert.ToInt32(objXmlNode.Item("mod").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("tpNF") Is Nothing) Then
                    pObjNf.NF_IDE_TPNF = Convert.ToInt32(objXmlNode.Item("tpNF").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("idDest") Is Nothing) Then
                    pObjNf.NF_IDE_IDDEST = Convert.ToInt32(objXmlNode.Item("idDest").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("cMunFG") Is Nothing) Then
                    pObjNf.NF_IDE_CMUNFG = objXmlNode.Item("cMunFG").InnerText
                End If
                If (Not objXmlNode.Item("tpEmis") Is Nothing) Then
                    pObjNf.NF_IDE_TPEMISS = Convert.ToInt32(objXmlNode.Item("tpEmis").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("tpAmb") Is Nothing) Then
                    pObjNf.NF_IDE_TPAMB = Convert.ToInt32(objXmlNode.Item("tpAmb").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("finNFe") Is Nothing) Then
                    pObjNf.NF_IDE_FINNFE = Convert.ToInt32(objXmlNode.Item("finNFe").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("indFinal") Is Nothing) Then
                    pObjNf.NF_IDE_INDFINAL = Convert.ToInt32(objXmlNode.Item("indFinal").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("indPres") Is Nothing) Then
                    pObjNf.NF_IDE_INDPRES = Convert.ToInt32(objXmlNode.Item("indPres").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("procEmi") Is Nothing) Then
                    pObjNf.NF_IDE_PROCEMI = Convert.ToInt32(objXmlNode.Item("procEmi").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("dhCont") Is Nothing) Then
                    pObjNf.NF_IDE_DHCONT = objXmlNode.Item("dhCont").InnerText
                End If
                If (Not objXmlNode.Item("xJust") Is Nothing) Then
                    pObjNf.NF_IDE_XJUST = objXmlNode.Item("xJust").InnerText
                End If
            Next

            '--> INFORMAÇÕES DO EMITENTE
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("emit")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("CNPJ") Is Nothing) Then
                    pObjNf.NF_EMIT_CNPJ = objXmlNode.Item("CNPJ").InnerText
                End If
                If (Not objXmlNode.Item("xNome") Is Nothing) Then
                    pObjNf.NF_EMIT_XNOME = objXmlNode.Item("xNome").InnerText.Replace("'", "")
                End If

                Dim objEnderEmit As XmlNode = objXmlNode.Item("enderEmit")

                If (Not objEnderEmit.Item("xLgr") Is Nothing) Then
                    pObjNf.NF_EMIT_XLGR = objEnderEmit.Item("xLgr").InnerText
                End If
                If (Not objEnderEmit.Item("nro") Is Nothing) Then
                    pObjNf.NF_EMIT_NRO = objEnderEmit.Item("nro").InnerText
                End If
                If (Not objEnderEmit.Item("xCpl") Is Nothing) Then
                    pObjNf.NF_EMIT_XCPL = objEnderEmit.Item("xCpl").InnerText
                End If
                If (Not objEnderEmit.Item("xBairro") Is Nothing) Then
                    pObjNf.NF_EMIT_XBAIRRO = objEnderEmit.Item("xBairro").InnerText
                End If
                If (Not objEnderEmit.Item("cMun") Is Nothing) Then
                    pObjNf.NF_EMIT_CMUN = objEnderEmit.Item("cMun").InnerText
                End If
                If (Not objEnderEmit.Item("UF") Is Nothing) Then
                    pObjNf.NF_EMIT_UF = objEnderEmit.Item("UF").InnerText
                End If
                If (Not objEnderEmit.Item("CEP") Is Nothing) Then
                    pObjNf.NF_EMIT_CEP = objEnderEmit.Item("CEP").InnerText
                End If
                If (Not objEnderEmit.Item("cPais") Is Nothing) Then
                    pObjNf.NF_EMIT_CPAIS = objEnderEmit.Item("cPais").InnerText
                End If
                If (Not objEnderEmit.Item("xPais") Is Nothing) Then
                    pObjNf.NF_EMIT_XPAIS = objEnderEmit.Item("xPais").InnerText
                End If
                If (Not objEnderEmit.Item("fone") Is Nothing) Then
                    pObjNf.NF_EMIT_FONE = objEnderEmit.Item("fone").InnerText
                End If

                If (Not objXmlNode.Item("IE") Is Nothing) Then
                    pObjNf.NF_EMIT_IE = objXmlNode.Item("IE").InnerText
                End If
                If (Not objXmlNode.Item("IEST") Is Nothing) Then
                    pObjNf.NF_EMIT_IEST = objXmlNode.Item("IEST").InnerText
                End If
                If (Not objXmlNode.Item("IM") Is Nothing) Then
                    pObjNf.NF_EMIT_IM = objXmlNode.Item("IM").InnerText
                End If
                If (Not objXmlNode.Item("CNAE") Is Nothing) Then
                    pObjNf.NF_EMIT_CNAE = objXmlNode.Item("CNAE").InnerText
                End If
                If (Not objXmlNode.Item("CRT") Is Nothing) Then
                    pObjNf.NF_EMIT_CRT = objXmlNode.Item("CRT").InnerText
                End If
            Next

            '--> INFORMAÇÕES DO REMETENTE
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("rem")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("CNPJ") Is Nothing) Then
                    pObjNf.NF_REM_CNPJ = objXmlNode.Item("CNPJ").InnerText
                End If
                If (Not objXmlNode.Item("xNome") Is Nothing) Then
                    pObjNf.NF_REM_XNOME = objXmlNode.Item("xNome").InnerText.Replace("'", "")
                End If
                If (Not objXmlNode.Item("IE") Is Nothing) Then
                    pObjNf.NF_REM_IE = objXmlNode.Item("IE").InnerText
                End If

                Dim objEnderReme As XmlNode = objXmlNode.Item("enderReme")

                If (Not objEnderReme.Item("xLgr") Is Nothing) Then
                    pObjNf.NF_REM_XLGR = objEnderReme.Item("xLgr").InnerText
                End If
                If (Not objEnderReme.Item("nro") Is Nothing) Then
                    pObjNf.NF_REM_NRO = objEnderReme.Item("nro").InnerText
                End If
                If (Not objEnderReme.Item("xCpl") Is Nothing) Then
                    pObjNf.NF_REM_XCPL = objEnderReme.Item("xCpl").InnerText
                End If
                If (Not objEnderReme.Item("xBairro") Is Nothing) Then
                    pObjNf.NF_REM_XBAIRRO = objEnderReme.Item("xBairro").InnerText
                End If
                If (Not objEnderReme.Item("cMun") Is Nothing) Then
                    pObjNf.NF_REM_CMUN = objEnderReme.Item("cMun").InnerText
                End If
                If (Not objEnderReme.Item("UF") Is Nothing) Then
                    pObjNf.NF_REM_UF = objEnderReme.Item("UF").InnerText
                End If
                If (Not objEnderReme.Item("CEP") Is Nothing) Then
                    pObjNf.NF_REM_CEP = objEnderReme.Item("CEP").InnerText
                End If
                If (Not objEnderReme.Item("cPais") Is Nothing) Then
                    pObjNf.NF_REM_CPAIS = objEnderReme.Item("cPais").InnerText
                End If
                If (Not objEnderReme.Item("xPais") Is Nothing) Then
                    pObjNf.NF_REM_XPAIS = objEnderReme.Item("xPais").InnerText
                End If
            Next

            '--> INFORMAÇÕES DO DESTINATÁRIO
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("dest")
            For Each objXmlNode As XmlNode In objXmlNodeList
                'Marcio Spinosa - 18/03/2019 - SR00261534
                If (Not objXmlNode.Item("CNPJ") Is Nothing) Then
                    pObjNf.NF_DEST_CNPJ = objXmlNode.Item("CNPJ").InnerText
                    ' ElseIf (Not objXmlNode.Item("CPF") Is Nothing) Then
                    '   pObjNf.NF_DEST_CNPJ = objXmlNode.Item("CPF").InnerText
                End If
                'Marcio Spinosa - 18/03/2019 - SR00261534 - Fim

                If (Not objXmlNode.Item("IE") Is Nothing) Then
                    pObjNf.NF_DEST_IE = objXmlNode.Item("IE").InnerText
                End If

                If (Not objXmlNode.Item("xNome") Is Nothing) Then
                    pObjNf.NF_DEST_XNOME = objXmlNode.Item("xNome").InnerText.Replace("'", "")
                End If

                'Campos novos
                Dim objEnderDest As XmlNode = objXmlNode.Item("enderDest")
                If (Not objEnderDest.Item("xLgr") Is Nothing) Then
                    pObjNf.NF_DEST_XLGR = objEnderDest.Item("xLgr").InnerText
                End If
                If (Not objEnderDest.Item("nro") Is Nothing) Then
                    pObjNf.NF_DEST_NRO = objEnderDest.Item("nro").InnerText
                End If
                If (Not objEnderDest.Item("xCpl") Is Nothing) Then
                    pObjNf.NF_DEST_XCPL = objEnderDest.Item("xCpl").InnerText
                End If
                If (Not objEnderDest.Item("xBairro") Is Nothing) Then
                    pObjNf.NF_DEST_XBAIRRO = objEnderDest.Item("xBairro").InnerText
                End If
                If (Not objEnderDest.Item("cMun") Is Nothing) Then
                    pObjNf.NF_DEST_CMUN = objEnderDest.Item("cMun").InnerText
                End If
                If (Not objEnderDest.Item("xMun") Is Nothing) Then
                    pObjNf.NF_DEST_XMUN = objEnderDest.Item("xMun").InnerText
                End If
                If (Not objEnderDest.Item("UF") Is Nothing) Then
                    pObjNf.NF_DEST_UF = objEnderDest.Item("UF").InnerText
                End If
                If (Not objEnderDest.Item("CEP") Is Nothing) Then
                    pObjNf.NF_DEST_CEP = objEnderDest.Item("CEP").InnerText
                End If
                If (Not objEnderDest.Item("cPais") Is Nothing) Then
                    pObjNf.NF_DEST_CPAIS = objEnderDest.Item("cPais").InnerText
                End If
                If (Not objEnderDest.Item("xPais") Is Nothing) Then
                    pObjNf.NF_DEST_XPAIS = objEnderDest.Item("xPais").InnerText
                End If
                If (Not objEnderDest.Item("fone") Is Nothing) Then
                    pObjNf.NF_DEST_FONE = objEnderDest.Item("fone").InnerText
                End If
                If (Not objEnderDest.Item("indIEDest") Is Nothing) Then
                    pObjNf.NF_DEST_INDIEDEST = objEnderDest.Item("indIEDest").InnerText
                End If
                If (Not objEnderDest.Item("ISUF") Is Nothing) Then
                    pObjNf.NF_DEST_ISUF = objEnderDest.Item("ISUF").InnerText
                End If
                If (Not objEnderDest.Item("IM") Is Nothing) Then
                    pObjNf.NF_DEST_IM = objEnderDest.Item("IM").InnerText
                End If
            Next

            '--> INFORMAÇÕES DO EXPEDIDOR
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("exped")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("CNPJ") Is Nothing) Then
                    pObjNf.NF_EXPED_CNPJ = objXmlNode.Item("CNPJ").InnerText
                End If
                If (Not objXmlNode.Item("xNome") Is Nothing) Then
                    pObjNf.NF_EXPED_XNOME = objXmlNode.Item("xNome").InnerText.Replace("'", "")
                End If
                If (Not objXmlNode.Item("IE") Is Nothing) Then
                    pObjNf.NF_EXPED_IE = objXmlNode.Item("IE").InnerText
                End If

                Dim objEnderExped As XmlNode = objXmlNode.Item("enderExped")

                If (Not objEnderExped.Item("xLgr") Is Nothing) Then
                    pObjNf.NF_EXPED_XLGR = objEnderExped.Item("xLgr").InnerText
                End If
                If (Not objEnderExped.Item("nro") Is Nothing) Then
                    pObjNf.NF_EXPED_NRO = objEnderExped.Item("nro").InnerText
                End If
                If (Not objEnderExped.Item("xCpl") Is Nothing) Then
                    pObjNf.NF_EXPED_XCPL = objEnderExped.Item("xCpl").InnerText
                End If
                If (Not objEnderExped.Item("xBairro") Is Nothing) Then
                    pObjNf.NF_EXPED_XBAIRRO = objEnderExped.Item("xBairro").InnerText
                End If
                If (Not objEnderExped.Item("cMun") Is Nothing) Then
                    pObjNf.NF_EXPED_CMUN = objEnderExped.Item("cMun").InnerText
                End If
                If (Not objEnderExped.Item("xMun") Is Nothing) Then
                    pObjNf.NF_EXPED_XMUN = objEnderExped.Item("xMun").InnerText
                End If
                If (Not objEnderExped.Item("UF") Is Nothing) Then
                    pObjNf.NF_EXPED_UF = objEnderExped.Item("UF").InnerText
                End If
                If (Not objEnderExped.Item("CEP") Is Nothing) Then
                    pObjNf.NF_EXPED_CEP = objEnderExped.Item("CEP").InnerText
                End If
                If (Not objEnderExped.Item("cPais") Is Nothing) Then
                    pObjNf.NF_EXPED_CPAIS = objEnderExped.Item("cPais").InnerText
                End If
                If (Not objEnderExped.Item("xPais") Is Nothing) Then
                    pObjNf.NF_EXPED_XPAIS = objEnderExped.Item("xPais").InnerText
                End If
            Next

            '--> INFORMAÇÕES DO RECEBEDOR
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("receb")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("CNPJ") Is Nothing) Then
                    pObjNf.NF_RECEB_CNPJ = objXmlNode.Item("CNPJ").InnerText
                End If
                If (Not objXmlNode.Item("xNome") Is Nothing) Then
                    pObjNf.NF_RECEB_XNOME = objXmlNode.Item("xNome").InnerText.Replace("'", "")
                End If
                If (Not objXmlNode.Item("IE") Is Nothing) Then
                    pObjNf.NF_RECEB_IE = objXmlNode.Item("IE").InnerText
                End If

                Dim objEnderReceb As XmlNode = objXmlNode.Item("enderReceb")

                If (Not objEnderReceb.Item("xLgr") Is Nothing) Then
                    pObjNf.NF_RECEB_XLGR = objEnderReceb.Item("xLgr").InnerText
                End If
                If (Not objEnderReceb.Item("nro") Is Nothing) Then
                    pObjNf.NF_RECEB_NRO = objEnderReceb.Item("nro").InnerText
                End If
                If (Not objEnderReceb.Item("xCpl") Is Nothing) Then
                    pObjNf.NF_RECEB_XCPL = objEnderReceb.Item("xCpl").InnerText
                End If
                If (Not objEnderReceb.Item("xBairro") Is Nothing) Then
                    pObjNf.NF_RECEB_XBAIRRO = objEnderReceb.Item("xBairro").InnerText
                End If
                If (Not objEnderReceb.Item("cMun") Is Nothing) Then
                    pObjNf.NF_RECEB_CMUN = objEnderReceb.Item("cMun").InnerText
                End If
                If (Not objEnderReceb.Item("xMun") Is Nothing) Then
                    pObjNf.NF_RECEB_XMUN = objEnderReceb.Item("xMun").InnerText
                End If
                If (Not objEnderReceb.Item("UF") Is Nothing) Then
                    pObjNf.NF_RECEB_UF = objEnderReceb.Item("UF").InnerText
                End If
                If (Not objEnderReceb.Item("CEP") Is Nothing) Then
                    pObjNf.NF_RECEB_CEP = objEnderReceb.Item("CEP").InnerText
                End If
                If (Not objEnderReceb.Item("cPais") Is Nothing) Then
                    pObjNf.NF_RECEB_CPAIS = objEnderReceb.Item("cPais").InnerText
                End If
                If (Not objEnderReceb.Item("xPais") Is Nothing) Then
                    pObjNf.NF_RECEB_XPAIS = objEnderReceb.Item("xPais").InnerText
                End If
            Next

            '--> INFORMAÇÕES DA TRANSPORTADORA
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("transp")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("modFrete") Is Nothing) Then
                    pObjNf.NF_TRANSP_MODFRETE = objXmlNode.Item("modFrete").InnerText
                End If

                objXmlNodeList = objXmlNode.ChildNodes()
                For Each objXmlSubNode As XmlNode In objXmlNodeList
                    If (Not objXmlSubNode.Item("CNPJ") Is Nothing) Then
                        If Not objXmlSubNode.Item("CNPJ") Is Nothing Then
                            pObjNf.NF_TRANSP_CNPJ = objXmlSubNode.Item("CNPJ").InnerText
                        End If
                        If Not objXmlSubNode.Item("xNome") Is Nothing Then
                            pObjNf.NF_TRANSP_XNOME = objXmlSubNode.Item("xNome").InnerText
                        End If
                        If Not objXmlSubNode.Item("IE") Is Nothing Then
                            pObjNf.NF_TRANSP_IE = objXmlSubNode.Item("IE").InnerText
                        End If
                        If Not objXmlSubNode.Item("xEnder") Is Nothing Then
                            pObjNf.NF_TRANSP_XENDER = objXmlSubNode.Item("xEnder").InnerText
                        End If
                        If Not objXmlSubNode.Item("xMun") Is Nothing Then
                            pObjNf.NF_TRANSP_XMUN = objXmlSubNode.Item("xMun").InnerText
                        End If
                        If Not objXmlSubNode.Item("UF") Is Nothing Then
                            pObjNf.NF_TRANSP_UF = objXmlSubNode.Item("UF").InnerText
                        End If
                    End If
                Next
            Next

            '--> INFORMAÇÕES DE COBRANÇA
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("cobr")
            For Each objXmlSubNode As XmlNode In objXmlNodeList
                For Each objCobranca As XmlNode In objXmlSubNode.ChildNodes()
                    If (objCobranca.Name = "fat") Then
                        If (Not objCobranca.Item("nFat") Is Nothing) Then
                            pObjNf.NF_COBR_NFAT = objCobranca.Item("nFat").InnerText
                        End If
                        If (Not objCobranca.Item("vOrig") Is Nothing) Then
                            pObjNf.NF_COBR_VORIG = Convert.ToDecimal(objCobranca.Item("vOrig").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objCobranca.Item("vDesc") Is Nothing) Then
                            pObjNf.NF_COBR_VDESC = Convert.ToDecimal(objCobranca.Item("vDesc").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objCobranca.Item("vLiq") Is Nothing) Then
                            pObjNf.NF_COBR_VLIQ = Convert.ToDecimal(objCobranca.Item("vLiq").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                    ElseIf (objCobranca.Name = "dup") Then
                        If (Not objCobranca.Item("nDup") Is Nothing) Then
                            objDuplicata.NF_COBR_DUP_NDUP = objCobranca.Item("nDup").InnerText
                        End If
                        If (Not objCobranca.Item("dVenc") Is Nothing) Then
                            objDuplicata.NF_COBR_DUP_DVENC = Convert.ToDateTime(objCobranca.Item("dVenc").InnerText.ToString())
                        End If
                        If (Not objCobranca.Item("dhVenc") Is Nothing) Then
                            objDuplicata.NF_COBR_DUP_DVENC = Convert.ToDateTime(objCobranca.Item("dhVenc").InnerText.ToString())
                        End If
                        If (Not objCobranca.Item("vDup") Is Nothing) Then
                            objDuplicata.NF_COBR_DUP_VDUP = Convert.ToDecimal(objCobranca.Item("vDup").InnerText.Replace("..", ".").Replace(".", ","))
                        End If

                        pObjNf.DUPLICATAS.Add(objDuplicata)
                    End If
                Next
            Next

            '--> NOTAS REFERENCIADAS
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("NFref")
            For Each objXmlSubNode As XmlNode In objXmlNodeList
                For Each objNotaRef As XmlNode In objXmlSubNode.ChildNodes()
                    If (objNotaRef.Name = "refNFe") Then
                        objNotaReferenciada = New modNFReferenciada()
                        objNotaReferenciada.NF_NFREF_REFNFE = objNotaRef.InnerText
                        pObjNf.NF_REFERENCIADAS.Add(objNotaReferenciada)
                    ElseIf (objNotaRef.Name = "refCTe") Then
                        objNotaReferenciada = New modNFReferenciada()
                        objNotaReferenciada.NF_NFREF_REFCTE = objNotaRef.InnerText
                        pObjNf.NF_REFERENCIADAS.Add(objNotaReferenciada)
                    End If
                Next
            Next

            'Totais da Nota Fiscal
            Dim objTotais As XmlNodeList = objXml.DocumentElement.GetElementsByTagName("ICMSTot")
            ' Dim objICMSTot As XmlNodeList = objTotais.Item("ICMSTot").ChildNodes
            For Each IcmsTotais As XmlNode In objTotais
                If (Not IcmsTotais.Item("vBC") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VBC = Convert.ToDecimal(IcmsTotais.Item("vBC").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vICMS") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VICMS = Convert.ToDecimal(IcmsTotais.Item("vICMS").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vBCST") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VBCST = Convert.ToDecimal(IcmsTotais.Item("vBCST").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vST") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VST = Convert.ToDecimal(IcmsTotais.Item("vST").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vProd") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VPROD = Convert.ToDecimal(IcmsTotais.Item("vProd").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vFrete") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VFRETE = Convert.ToDecimal(IcmsTotais.Item("vFrete").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vSeg") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VSEG = Convert.ToDecimal(IcmsTotais.Item("vSeg").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vDesc") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VDESC = Convert.ToDecimal(IcmsTotais.Item("vDesc").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vII") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VII = Convert.ToDecimal(IcmsTotais.Item("vII").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vIPI") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VIPI = Convert.ToDecimal(IcmsTotais.Item("vIPI").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vPIS") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VPIS = Convert.ToDecimal(IcmsTotais.Item("vPIS").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vCOFINS") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VCOFINS = Convert.ToDecimal(IcmsTotais.Item("vCOFINS").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vOutro") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VOUTRO = Convert.ToDecimal(IcmsTotais.Item("vOutro").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vNF") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VNF = Convert.ToDecimal(IcmsTotais.Item("vNF").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vTotTrib") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VTOTTRIB = Convert.ToDecimal(IcmsTotais.Item("vTotTrib").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vICMSDeson") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VICMSDESON = Convert.ToDecimal(IcmsTotais.Item("vICMSDeson").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vICMSUFDest") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VICMSUFDEST = Convert.ToDecimal(IcmsTotais.Item("vICMSUFDest").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vICMSUFRemet") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VICMSUFREMET = Convert.ToDecimal(IcmsTotais.Item("vICMSUFRemet").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not IcmsTotais.Item("vFCPUFDest") Is Nothing) Then
                    pObjNf.NF_ICMSTOT_VFCPUFDEST = Convert.ToDecimal(IcmsTotais.Item("vFCPUFDest").InnerText.Replace("..", ".").Replace(".", ","))
                End If
            Next

            '--> Valores de ISSQN
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("ISSQNtot")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("vServ") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_VSERV = Convert.ToDecimal(objXmlNode.Item("vServ").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vBC") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_VBC = Convert.ToDecimal(objXmlNode.Item("vBC").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vISS") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_VISS = Convert.ToDecimal(objXmlNode.Item("vISS").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vPIS") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_VPIS = Convert.ToDecimal(objXmlNode.Item("vPIS").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vCOFINS") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_VCOFINS = Convert.ToDecimal(objXmlNode.Item("vCOFINS").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("dCompet") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_DTCOMPET = objXmlNode.Item("dCompet").InnerText
                End If
                If (Not objXmlNode.Item("vDeducao") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_VDEDUCAO = Convert.ToDecimal(objXmlNode.Item("vDeducao").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vOutro") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_VOUTRO = Convert.ToDecimal(objXmlNode.Item("vOutro").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vDescIncond") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_VDESCINCOD = Convert.ToDecimal(objXmlNode.Item("vDescIncond").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vDescCond") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_VDESCCOND = Convert.ToDecimal(objXmlNode.Item("vDescCond").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vISSRet") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_VISSRET = Convert.ToDecimal(objXmlNode.Item("vISSRet").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("cRegTrib") Is Nothing) Then
                    pObjNf.NF_ISSQNTOT_CREGTRIB = objXmlNode.Item("cRegTrib").InnerText
                End If
            Next

            'RETENÇÃO DE IMPOSTOS
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("retTrib")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("vRetPIS") Is Nothing) Then
                    pObjNf.NF_RETTRIN_VRETPIS = Convert.ToDecimal(objXmlNode.Item("vRetPIS").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vRetCOFINS") Is Nothing) Then
                    pObjNf.NF_RETTRIN_VRETCOFINS = Convert.ToDecimal(objXmlNode.Item("vRetCOFINS").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vRetCSLL") Is Nothing) Then
                    pObjNf.NF_RETTRIN_VRETCSLL = Convert.ToDecimal(objXmlNode.Item("vRetCSLL").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vBCIRRF") Is Nothing) Then
                    pObjNf.NF_RETTRIN_VBCIRRF = Convert.ToDecimal(objXmlNode.Item("vBCIRRF").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vIRRF") Is Nothing) Then
                    pObjNf.NF_RETTRIN_VIRRF = Convert.ToDecimal(objXmlNode.Item("vIRRF").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vBCRetPrev") Is Nothing) Then
                    pObjNf.NF_RETTRIN_VBCRETPREV = Convert.ToDecimal(objXmlNode.Item("vBCRetPrev").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vRetPrev") Is Nothing) Then
                    pObjNf.NF_RETTRIN_VRETPREV = Convert.ToDecimal(objXmlNode.Item("vRetPrev").InnerText.Replace("..", ".").Replace(".", ","))
                End If
            Next

            'Retenção transportadora
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("retTransp")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("vServ") Is Nothing) Then
                    pObjNf.NF_RETTRANSP_VSERV = Convert.ToDecimal(objXmlNode.Item("vServ").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vBCRet") Is Nothing) Then
                    pObjNf.NF_RETTRANSP_VBCRET = Convert.ToDecimal(objXmlNode.Item("vBCRet").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("pICMSRet") Is Nothing) Then
                    pObjNf.NF_RETTRANSP_PICMSRET = Convert.ToDecimal(objXmlNode.Item("pICMSRet").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("vICMSRet") Is Nothing) Then
                    pObjNf.NF_RETTRANSP_VICMSRET = Convert.ToDecimal(objXmlNode.Item("vICMSRet").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("CFOP") Is Nothing) Then
                    pObjNf.NF_RETTRANSP_CFOP = Convert.ToInt32(objXmlNode.Item("CFOP").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("cMunFG") Is Nothing) Then
                    pObjNf.NF_RETTRANSP_CMUNFG = objXmlNode.Item("cMunFG").InnerText
                End If
            Next

            '--> Pagamento
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("pag")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("tPag") Is Nothing) Then
                    pObjNf.NF_PAG_TPAG = objXmlNode.Item("tPag").InnerText
                End If
                If (Not objXmlNode.Item("vPag") Is Nothing) Then
                    pObjNf.NF_PAG_VPAG = Convert.ToDecimal(objXmlNode.Item("vPag").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objXmlNode.Item("CNPJ") Is Nothing) Then
                    pObjNf.NF_PAG_CNPJ = objXmlNode.Item("CNPJ").InnerText
                End If
                If (Not objXmlNode.Item("tBand") Is Nothing) Then
                    pObjNf.NF_PAG_TBAND = objXmlNode.Item("tBand").InnerText
                End If
                If (Not objXmlNode.Item("cAut") Is Nothing) Then
                    pObjNf.NF_PAG_CAUT = objXmlNode.Item("cAut").InnerText
                End If
                If (Not objXmlNode.Item("tpIntegra") Is Nothing) Then
                    pObjNf.NF_PAG_TPINTEGRA = objXmlNode.Item("tpIntegra").InnerText
                End If
            Next

            '--> INFORMAÇÃO ADICIONAL
            Dim strAttribute As String = ""
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("infAdic")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("infCpl") Is Nothing) Then
                    strAttribute = ""
                    If (objXmlNode.Attributes.Count > 0) Then
                        strAttribute = objXmlNode.Attributes(0).Value & ": "
                    End If
                    pObjNf.NF_OUTROS_INFORMACAO_ADICIONAL &= strAttribute & objXmlNode.Item("infCpl").InnerText.Replace("'", "").Replace("  ", "") & Environment.NewLine
                End If
            Next

            For Each objXmlNode As XmlNode In objXml.DocumentElement.GetElementsByTagName("compl")
                If (Not objXmlNode.Item("xObs") Is Nothing) Then
                    strAttribute = ""
                    If (objXmlNode.Attributes.Count > 0) Then
                        strAttribute = objXmlNode.Attributes(0).Value & ": "
                    End If
                    pObjNf.NF_OUTROS_INFORMACAO_ADICIONAL &= strAttribute & objXmlNode.Item("xObs").FirstChild().InnerText.Replace("'", "").Replace("  ", "") & Environment.NewLine
                End If
            Next

            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("ObsCont")
            For Each objXmlNode As XmlNode In objXmlNodeList
                strAttribute = ""
                If (objXmlNode.Attributes.Count > 0) Then
                    strAttribute = objXmlNode.Attributes(0).Value & ": "
                End If
                pObjNf.NF_OUTROS_INFORMACAO_ADICIONAL &= strAttribute & objXmlNode.InnerText.Replace("'", "").Replace("  ", "") & Environment.NewLine
            Next

            '--> SIGNATURE
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("Signature")
            pObjNf.NF_OUTROS_SIGNATURE = objXmlNodeList.Count > 0

            '--> RETORNO SEFAZ
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("protNFe")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("infProt") Is Nothing) Then
                    pObjNf.NF_OUTROS_VERSAO = objXml.DocumentElement.GetElementsByTagName("protNFe")(0).Attributes("versao").Value

                    objXmlNodeList = objXmlNode.ChildNodes()
                    For Each objXmlSubNode As XmlNode In objXmlNodeList
                        If (Not objXmlSubNode.Item("cStat") Is Nothing) Then
                            pObjNf.NF_OUTROS_STATUS_CODE = objXmlSubNode.Item("cStat").InnerText
                            pObjNf.NF_OUTROS_STATUS_DESC = objXmlSubNode.Item("xMotivo").InnerText
                        End If
                    Next
                End If
            Next

            '--> STATUS
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("protCTe")
            For Each objXmlNode As XmlNode In objXmlNodeList
                If (Not objXmlNode.Item("infProt") Is Nothing) Then
                    pObjNf.NF_OUTROS_VERSAO = objXml.DocumentElement.GetElementsByTagName("protCTe")(0).Attributes("versao").Value

                    objXmlNodeList = objXmlNode.ChildNodes()
                    For Each objXmlSubNode As XmlNode In objXmlNodeList
                        If (Not objXmlSubNode.Item("cStat") Is Nothing) Then
                            pObjNf.NF_OUTROS_STATUS_CODE = objXmlSubNode.Item("cStat").InnerText
                            pObjNf.NF_OUTROS_STATUS_DESC = objXmlSubNode.Item("xMotivo").InnerText
                        End If
                    Next
                End If
            Next

            '--> VALORES CTE
            Dim strCteChave As String = ""
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("vPrest")
            For Each objXmlNode As XmlNode In objXmlNodeList
                For i As Int32 = 0 To objXmlNode.ChildNodes.Count
                    If (objXmlNode.ChildNodes(0).Name = "vTPrest") Then
                        pObjNf.CT_VPREST_VTPREST = Convert.ToDecimal(objXmlNode.ChildNodes(0).InnerText.Replace("..", ".").Replace(".", ","))
                    End If
                    If (objXmlNode.ChildNodes(1).Name = "vRec") Then
                        pObjNf.CT_VPREST_VREC = Convert.ToDecimal(objXmlNode.ChildNodes(1).InnerText.Replace("..", ".").Replace(".", ","))
                    End If

                    If (Not objXmlNode.ChildNodes(i) Is Nothing AndAlso objXmlNode.ChildNodes(i).Name = "Comp") Then
                        For Each objXmlField As XmlNode In objXmlNode.ChildNodes(i)
                            If String.IsNullOrEmpty(strCteChave) Then
                                strCteChave = objXmlField.InnerText.ToUpper()
                            Else
                                If strCteChave = "FRETE PESO" Then
                                    pObjNf.CT_VPREST_COMP_FRETE_PESO = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "FRETE VALOR" Then
                                    pObjNf.CT_VPREST_COMP_FRETE_VALOR = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "FRETE" Then
                                    pObjNf.CT_VPREST_COMP_FRETE = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "SEC/CAT" Or strCteChave = "CAT" Or strCteChave = "SEC CAT" Then
                                    pObjNf.CT_VPREST_COMP_SEC_CAT = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "ADEME" Then
                                    pObjNf.CT_VPREST_COMP_ADEME = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "PEDAGIO" Or strCteChave = "PEDÁGIO" Then
                                    pObjNf.CT_VPREST_COMP_PEDAGIO = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "GRIS" Then
                                    pObjNf.CT_VPREST_COMP_GRIS = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "TAXAEMICTRC" Then
                                    pObjNf.CT_VPREST_COMP_TAXAEMICTRC = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "COLETAENTREGA" Then
                                    pObjNf.CT_VPREST_COMP_COLETAENTREGA = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "OUTROSVALORES" Then
                                    pObjNf.CT_VPREST_COMP_OUTROSVALORES = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "DESCONTO" Then
                                    pObjNf.CT_VPREST_COMP_DESCONTO = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "DESPACHO" Then
                                    pObjNf.CT_VPREST_COMP_DESPACHO = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "ENTREGA" Then
                                    pObjNf.CT_VPREST_COMP_ENTREGA = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "OUTROS" Then
                                    pObjNf.CT_VPREST_COMP_OUTROS = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "ESCOLTA" Then
                                    pObjNf.CT_VPREST_COMP_ESCOLTA = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "COLETA" Then
                                    pObjNf.CT_VPREST_COMP_COLETA = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "SEGURO" Or strCteChave = "% SEGURO" Then
                                    pObjNf.CT_VPREST_COMP_SEGURO = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "PERNOITE" Or strCteChave = "PERNOITES" Then
                                    pObjNf.CT_VPREST_COMP_PERNOITE = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                ElseIf strCteChave = "REDESPACHO" Then
                                    pObjNf.CT_VPREST_COMP_REDESPACHO = Convert.ToDecimal(objXmlField.InnerText.Replace("..", ".").Replace(".", ","))
                                End If

                                strCteChave = String.Empty
                            End If
                        Next
                    End If
                Next
            Next

            '--> VALORE DA CARGA
            objXmlNodeList = objXml.DocumentElement.GetElementsByTagName("infCTeNorm")
            For Each objXmlNode As XmlNode In objXmlNodeList
                For i As Int32 = 0 To objXmlNode.ChildNodes.Count
                    If (Not objXmlNode.ChildNodes(i) Is Nothing AndAlso objXmlNode.ChildNodes(i).Name = "infCarga") Then
                        If (objXmlNode.ChildNodes(i).ChildNodes(0).Name = "vCarga") Then
                            pObjNf.CT_INFCARGA_VCARGA = Convert.ToDecimal(objXmlNode.ChildNodes(i).ChildNodes(0).InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                    End If
                Next
            Next

            '--> QUANDO O TOMADOR FOR DIFERENTE DE OUTROS, PREENCHE OS DADOS BÁSICOS DO TOMADOR
            Select Case pObjNf.CT_IDE_TOMA
                Case "0"
                    pObjNf.CT_TOMA_CNPJ = pObjNf.NF_REM_CNPJ
                    pObjNf.CT_TOMA_XNOME = pObjNf.NF_REM_XNOME
                    pObjNf.CT_TOMA_UF = pObjNf.NF_REM_UF
                Case "1"
                    pObjNf.CT_TOMA_CNPJ = pObjNf.NF_EXPED_CNPJ
                    pObjNf.CT_TOMA_XNOME = pObjNf.NF_EXPED_XNOME
                    pObjNf.CT_TOMA_UF = pObjNf.NF_EXPED_UF
                Case "2"
                    pObjNf.CT_TOMA_CNPJ = pObjNf.NF_RECEB_CNPJ
                    pObjNf.CT_TOMA_XNOME = pObjNf.NF_RECEB_XNOME
                    pObjNf.CT_TOMA_UF = pObjNf.NF_RECEB_UF
                Case "3"
                    pObjNf.CT_TOMA_CNPJ = pObjNf.NF_DEST_CNPJ
                    pObjNf.CT_TOMA_XNOME = pObjNf.NF_DEST_XNOME
                    pObjNf.CT_TOMA_UF = pObjNf.NF_DEST_UF
            End Select
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao ler o cabeçalho do XML")
        End Try
    End Sub
#End Region

#Region " LerItemXml "
    Private Sub LerItemXml(ByVal pXmlDocument As XmlDocument, ByRef pObjNf As modNF, Optional ByVal isSomenteLeitura As Boolean = False)
        Try
            Dim arrDet As XmlNodeList
            pObjNf.ITENS_NF = New List(Of modNFItem)
            Dim IsEnviarEmailCfop As Boolean = False
            Dim intIndex As Integer = 0
            Dim objItem As New modNFItem()
            Dim objBLNotaFiscal As New BLNotaFiscal()

            If (pObjNf.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte AndAlso pObjNf.VNF_CLASSIFICACAO = modNF.tipo_cte_frete_pedido) Then
                objItem = New modNFItem()
                objItem.NF_PROD_ITEM = 1
                objItem.CT_INFNFE_CHAVE = ""
                objItem.NF_PROD_QTRIB = 1
                objItem.NF_PROD_QCOM = 1
                objItem.NF_PROD_UCOM = "UN"
                objItem.NF_PROD_UTRIB = "UN"
                objItem.VNF_ITEM_VALIDO = "S"
                objItem.NF_PROD_CPROD = "---"
                objItem.NF_PROD_XPROD = "---"
                objItem.NF_PROD_VUNCOM = IIf(pObjNf.CT_VPREST_VREC > 0, pObjNf.CT_VPREST_VREC, pObjNf.CT_VPREST_VTPREST)
                objItem.NF_PROD_VPROD = IIf(pObjNf.CT_VPREST_VREC > 0, pObjNf.CT_VPREST_VREC, pObjNf.CT_VPREST_VTPREST)
                pObjNf.NF_ICMSTOT_VNF = IIf(pObjNf.CT_VPREST_VREC > 0, pObjNf.CT_VPREST_VREC, pObjNf.CT_VPREST_VTPREST)
                objItem.NF_PROD_CFOP = objBLNotaFiscal.GetCfopEntrada(pObjNf)

                modSQL.CommandText = "select cfop_descricao from TbCFOP where cfop_codigo = '" & objItem.NF_PROD_CFOP.Substring(0, 4) & "'"
                objItem.NF_PROD_CFOP_DESC = modSQL.ExecuteScalar(modSQL.CommandText)
                pObjNf.ITENS_NF.Add(objItem)
            End If

            If pXmlDocument.DocumentElement.GetElementsByTagName("det").Count = 0 Then
                '--> NFE REFERENCIADA NO CTE
                Dim strCteChave As String = ""
                For Each objXmlNode As XmlNode In pXmlDocument.DocumentElement.GetElementsByTagName("infDoc")
                    For i As Int32 = 0 To objXmlNode.ChildNodes.Count
                        If (Not objXmlNode.ChildNodes(i) Is Nothing AndAlso objXmlNode.ChildNodes(i).Name = "infNFe") Then
                            strCteChave &= objXmlNode.ChildNodes(i).FirstChild().InnerText.ToUpper() + ","
                        End If
                    Next
                Next

                '--> CFOP
                Dim strCfopCte As String = ""
                For Each objXmlNode As XmlNode In pXmlDocument.DocumentElement.GetElementsByTagName("ide")
                    For i As Int32 = 0 To objXmlNode.ChildNodes.Count
                        If (Not objXmlNode.ChildNodes(i) Is Nothing AndAlso objXmlNode.ChildNodes(i).Name = "CFOP") Then
                            strCfopCte = objXmlNode.ChildNodes(i).FirstChild().InnerText.ToUpper()
                            Exit For
                        End If
                    Next
                Next

                If strCteChave.Length > 0 Then
                    strCteChave = strCteChave.Substring(0, strCteChave.Length - 1)

                    For i As Integer = 0 To strCteChave.Split(",").Count - 1
                        objItem = New modNFItem()
                        objItem.NF_PROD_ITEM = i + 2
                        objItem.CT_INFNFE_CHAVE = strCteChave.Split(",")(i)
                        objItem.NF_PROD_QTRIB = 1
                        objItem.NF_PROD_QCOM = 1
                        objItem.NF_PROD_UCOM = "UN"
                        objItem.NF_PROD_UTRIB = "UN"
                        objItem.VNF_ITEM_VALIDO = "X"
                        objItem.NF_PROD_VUNCOM = IIf(pObjNf.CT_VPREST_VREC > 0, pObjNf.CT_VPREST_VREC, pObjNf.CT_VPREST_VTPREST)
                        objItem.NF_PROD_VPROD = IIf(pObjNf.CT_VPREST_VREC > 0, pObjNf.CT_VPREST_VREC, pObjNf.CT_VPREST_VTPREST)
                        pObjNf.NF_ICMSTOT_VNF = IIf(pObjNf.CT_VPREST_VREC > 0, pObjNf.CT_VPREST_VREC, pObjNf.CT_VPREST_VTPREST)
                        objItem.NF_PROD_CFOP = strCfopCte
                        pObjNf.ITENS_NF.Add(objItem)
                    Next
                End If

                If (pObjNf.ITENS_NF.Count < intIndex + 1) Then
                    pObjNf.ITENS_NF.Add(New modNFItem())
                End If

                If (pXmlDocument.DocumentElement.GetElementsByTagName("imp").Count > 0) Then
                    Dim objImpostosProduto As XmlNode = pXmlDocument.DocumentElement.GetElementsByTagName("imp").Item(0)
                    If (Not objImpostosProduto.Item("ICMS") Is Nothing) Then
                        Dim objImposto As XmlNode = objImpostosProduto.Item("ICMS")
                        PreencheICMSItem(objImpostosProduto, "ICMS00", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS10", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS20", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS30", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS40", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS51", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS60", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS70", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS90", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSPart", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSST", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN101", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN102", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN201", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN202", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN500", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN900", intIndex, pObjNf)
                    End If
                End If

                Return
            End If

            '--> CARREGA AS INFORMAÇÕES DO XML PARA MODEL
            arrDet = pXmlDocument.DocumentElement.GetElementsByTagName("det")
            For Each objDet As XmlNode In arrDet
                pObjNf.ITENS_NF.Add(New modNFItem())
                arrDet = objDet.ChildNodes()
                pObjNf.ITENS_NF(intIndex) = New modNFItem
                pObjNf.ITENS_NF(intIndex).NF_PROD_ITEM = intIndex + 1
                If (Not objDet.Item("prod") Is Nothing) Then

                    'arrDet = objDet.ChildNodes()
                    'pObjNf.ITENS_NF(intIndex) = New modNFItem
                    'pObjNf.ITENS_NF(intIndex).NF_ITEM = intIndex + 1

                    Dim descontoSetado As Boolean = False

                    'For Each objProduto As XmlNode In objDet.Item("prod").ChildNodes 'arrDet
                    Dim objAux As XmlNode = objDet.Item("prod")
                    'For Each objProduto As XmlNode In objAux
                    '---> NF_CODMAT
                    If (Not objAux.Item("cProd") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_CPROD = objAux.Item("cProd").InnerText
                    End If

                    '---> NF_DESMAT
                    If (Not objAux.Item("xProd") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_XPROD = objAux.Item("xProd").InnerText
                    End If

                    '---> NF_NCM
                    If (Not objAux.Item("NCM") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_NCM = objAux.Item("NCM").InnerText
                    End If

                    '---> NF_CFOP
                    If (Not objAux.Item("CFOP") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_CFOP = objAux.Item("CFOP").InnerText

                        '---> BUSCA A DESCRICAÇÃO DA CFOP
                        modSQL.CommandText = "select cfop_descricao from TbCFOP where cfop_codigo = '" & pObjNf.ITENS_NF(intIndex).NF_PROD_CFOP & "'"
                        pObjNf.ITENS_NF(intIndex).NF_PROD_CFOP_DESC = modSQL.ExecuteScalar(modSQL.CommandText)

                        '---> VERIFICA SE A CFOP É RELEVANTE, E SE PRECISA NOTIFICAR A CHEGADA DESTA NOTA FISCAL
                        pObjNf.ITENS_NF(intIndex).VNF_ITEM_VALIDO = IIf(CfopRelevante(pObjNf.ITENS_NF(intIndex)), "N", "X")
                        IsEnviarEmailCfop = IIf(pObjNf.ITENS_NF(intIndex).NF_PROD_CFOP = "5915" Or pObjNf.ITENS_NF(intIndex).NF_PROD_CFOP = "6915", True, False)
                    End If

                    '---> NF_UN
                    If (Not objAux.Item("uCom") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_UCOM = objAux.Item("uCom").InnerText
                    End If

                    '---> NF_QUANTIDADE
                    If (Not objAux.Item("qCom") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_QCOM = Convert.ToDecimal(objAux.Item("qCom").InnerText.Replace("..", ".").Replace(".", ","))
                    End If

                    If (Not objAux.Item("qTrib") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_QTRIB = Convert.ToDecimal(objAux.Item("qTrib").InnerText.Replace("..", ".").Replace(".", ","))
                    End If

                    '---> NF_VALOR_UNITARIO
                    If (Not objAux.Item("vUnCom") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_VUNCOM = Convert.ToDecimal(objAux.Item("vUnCom").InnerText.Replace("..", ".").Replace(".", ","))
                    End If

                    If (Not objAux.Item("vProd") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_VPROD = Convert.ToDecimal(objAux.Item("vProd").InnerText.Replace("..", ".").Replace(".", ","))
                    End If

                    '---> NF_VALOR_DESCONTO
                    If (Not objAux.Item("vDesc") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_VDESC = Convert.ToDecimal(objAux.Item("vDesc").InnerText.Replace("..", ".").Replace(".", ","))
                    End If

                    '---> NF_PEDIDO
                    If (Not objAux.Item("xPed") Is Nothing) Then
                        If objAux.Item("xPed").InnerText.RemoveLetters().Length <> 10 Then
                            pObjNf.ITENS_NF(intIndex).NF_PROD_XPED = ""
                            pObjNf.ITENS_NF(intIndex).NF_PROD_NITEMPED = 0
                        Else
                            pObjNf.ITENS_NF(intIndex).NF_PROD_XPED = objAux.Item("xPed").InnerText.RemoveLetters()
                            '---> NF_ITEM_PEDIDO
                            If (Not objAux.Item("nItemPed") Is Nothing) Then
                                If objAux.Item("nItemPed").InnerText.RemoveLetters().Length > 6 Then
                                    pObjNf.ITENS_NF(intIndex).NF_PROD_NITEMPED = 0
                                Else
                                    pObjNf.ITENS_NF(intIndex).NF_PROD_NITEMPED = Convert.ToInt32(objAux.Item("nItemPed").InnerText.RemoveLetters())
                                End If
                            End If
                        End If
                    End If

                    'campos novos
                    If (Not objAux.Item("cEAN") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_CEAN = objAux.Item("cEAN").InnerText
                    End If
                    If (Not objAux.Item("NVE") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_NVE = objAux.Item("NVE").InnerText
                    End If
                    If (Not objAux.Item("EXTIPI") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_EXTIPI = objAux.Item("EXTIPI").InnerText
                    End If
                    If (Not objAux.Item("vFrete") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_VFRETE = Convert.ToDecimal(objAux.Item("vFrete").InnerText.Replace("..", ".").Replace(".", ","))
                    End If
                    If (Not objAux.Item("vSeg") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_VSEG = Convert.ToDecimal(objAux.Item("vSeg").InnerText.Replace("..", ".").Replace(".", ","))
                    End If
                    If (Not objAux.Item("vDesc") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_VDESC = Convert.ToDecimal(objAux.Item("vDesc").InnerText.Replace("..", ".").Replace(".", ","))
                    End If
                    If (Not objAux.Item("vOutro") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_VOUTRO = Convert.ToDecimal(objAux.Item("vOutro").InnerText.Replace("..", ".").Replace(".", ","))
                    End If
                    If (Not objAux.Item("indTot") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_INDTOT = Convert.ToInt32(objAux.Item("indTot").InnerText.Replace("..", ".").Replace(".", ","))
                    End If
                    If (Not objAux.Item("DI") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_DI = objAux.Item("DI").InnerText
                    End If
                    If (Not objAux.Item("DetEspecifico") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_DETESPECIFICO = objAux.Item("DetEspecifico").InnerText
                    End If
                End If

                If (objDet.Name = "infAdProd") Then
                    If (Not objDet.Item("infAdProd") Is Nothing) Then
                        pObjNf.ITENS_NF(intIndex).NF_PROD_INF_ADICIONAL_ITEM = objDet.InnerText
                        If objDet.InnerText.Replace("#M#", "|").Split("|")(1).Split("-").Count() > 0 Then
                            Dim vObjNFREFSubContratacao As modTBDOC_SUBCONTRATACAO_REFNF
                            vObjNFREFSubContratacao = New modTBDOC_SUBCONTRATACAO_REFNF()
                            vObjNFREFSubContratacao.ITEM_NF = pObjNf.ITENS_NF(intIndex).NF_PROD_ITEM
                            vObjNFREFSubContratacao.NFEID = pObjNf.VNF_CHAVE_ACESSO
                            vObjNFREFSubContratacao.NUMERO_REFNF = objDet.InnerText.Replace("#M#", "|").Split("|")(1).Split("-")(0)
                            vObjNFREFSubContratacao.SERIE_REFNF = objDet.InnerText.Replace("#M#", "|").Split("|")(1).Split("-")(1)
                            vObjNFREFSubContratacao.ITEM_REFNF = objDet.InnerText.Replace("#M#", "|").Split("|")(1).Split("-")(2)
                        End If
                    End If
                End If

                If objNF.NF_IDE_FINNFE = 2 Then
                    Dim vObjNFREFComplementarRef As modTBDOC_NOTA_COMPLEMENTAR_REFNF
                    pObjNf.ITENS_NF(intIndex).VNF_NFREF_NOTA_COMPLEMENTAR = New List(Of modTBDOC_NOTA_COMPLEMENTAR_REFNF)
                    vObjNFREFComplementarRef = New modTBDOC_NOTA_COMPLEMENTAR_REFNF()
                    vObjNFREFComplementarRef.ITEM_NF = pObjNf.ITENS_NF(intIndex).NF_PROD_ITEM
                    vObjNFREFComplementarRef.NFEID = pObjNf.VNF_CHAVE_ACESSO
                    vObjNFREFComplementarRef.NUMERO_REFNF = objNF.NF_IDE_NFREF.Substring(28, 6)
                    vObjNFREFComplementarRef.SERIE_REFNF = objNF.NF_IDE_NFREF.Substring(22, 3)
                    pObjNf.ITENS_NF(intIndex).VNF_NFREF_NOTA_COMPLEMENTAR.Add(vObjNFREFComplementarRef)
                End If


                'LE OS IMPOSTOS
                If (Not objDet.Item("imposto") Is Nothing) Then
                    Dim objImpostosProduto As XmlNode = objDet.Item("imposto")
                    '---> NF_ALIQ_ICMS
                    If (Not objImpostosProduto.Item("ICMS") Is Nothing) Then
                        Dim objImposto As XmlNode = objImpostosProduto.Item("ICMS")
                        PreencheICMSItem(objImpostosProduto, "ICMS00", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS10", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS20", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS30", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS40", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS51", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS60", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS70", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMS90", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSPart", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSST", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN101", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN102", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN201", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN202", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN500", intIndex, pObjNf)
                        PreencheICMSItem(objImpostosProduto, "ICMSSN900", intIndex, pObjNf)
                        ' Next
                    End If

                    '---> NF IPI
                    If (Not objImpostosProduto.Item("IPI") Is Nothing) Then
                        Dim objImpostoIPI As XmlNode = objImpostosProduto.Item("IPI")

                        'For Each objImpostoIPI As XmlNode In objImpostosProduto.Item("IPI").ChildNodes()
                        If (Not objImpostoIPI.Item("clEnq") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_IPI_CLENQ = objImpostoIPI.Item("clEnq").InnerText
                        End If
                        If (Not objImpostoIPI.Item("CNPJProd") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_IPI_CNPJPROD = objImpostoIPI.Item("CNPJProd").InnerText
                        End If
                        If (Not objImpostoIPI.Item("cSelo") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_IPI_CSELO = objImpostoIPI.Item("cSelo").InnerText
                        End If
                        If (Not objImpostoIPI.Item("qSelo") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_IPI_QSELO = Convert.ToDecimal(objImpostoIPI.Item("qSelo").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoIPI.Item("cEnq") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_IPI_CENQ = objImpostoIPI.Item("cEnq").InnerText
                        End If

                        If (Not objImpostoIPI.Item("IPITrib") Is Nothing) Then
                            Dim objIPITrin As XmlNode = objImpostoIPI.Item("IPITrib")
                            If (Not objIPITrin.Item("CST") Is Nothing) Then
                                pObjNf.ITENS_NF(intIndex).NF_IPI_CST = objIPITrin.Item("CST").InnerText
                            End If
                            If (Not objIPITrin.Item("vBC") Is Nothing) Then
                                pObjNf.ITENS_NF(intIndex).NF_IPI_VBC = Convert.ToDecimal(objIPITrin.Item("vBC").InnerText.Replace("..", ".").Replace(".", ","))
                            End If
                            If (Not objIPITrin.Item("pIPI") Is Nothing) Then
                                pObjNf.ITENS_NF(intIndex).NF_IPI_PIPI = Convert.ToDecimal(objIPITrin.Item("pIPI").InnerText.Replace("..", ".").Replace(".", ","))
                            End If
                            If (Not objIPITrin.Item("vIPI") Is Nothing) Then
                                pObjNf.ITENS_NF(intIndex).NF_IPI_VIPI = Convert.ToDecimal(objIPITrin.Item("vIPI").InnerText.Replace("..", ".").Replace(".", ","))
                            End If
                            If (Not objIPITrin.Item("qUnid") Is Nothing) Then
                                pObjNf.ITENS_NF(intIndex).NF_IPI_QUNID = Convert.ToDecimal(objIPITrin.Item("qUnid").InnerText.Replace("..", ".").Replace(".", ","))
                            End If
                            If (Not objIPITrin.Item("vUnid") Is Nothing) Then
                                pObjNf.ITENS_NF(intIndex).NF_IPI_VUNID = Convert.ToDecimal(objIPITrin.Item("vUnid").InnerText.Replace("..", ".").Replace(".", ","))
                            End If
                            'Next
                        End If
                        'Next
                    End If

                    'IMPOSTO DE IMPORTAÇÃO
                    If (Not objImpostosProduto.Item("II") Is Nothing) Then
                        'For Each objImpostoII As XmlNode In objImpostosProduto.Item("II").ChildNodes()
                        Dim objImpostoII As XmlNode = objImpostosProduto.Item("II")
                        If (Not objImpostoII.Item("vBC") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_II_VBC = Convert.ToDecimal(objImpostoII.Item("vBC").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoII.Item("vDespAdu") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_II_VDESPADU = Convert.ToDecimal(objImpostoII.Item("vDespAdu").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoII.Item("vII") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_II_VII = Convert.ToDecimal(objImpostoII.Item("vII").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoII.Item("vIOF") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_II_VIOF = Convert.ToDecimal(objImpostoII.Item("vIOF").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        ' Next
                    End If

                    'PIS
                    If (Not objImpostosProduto.Item("PIS") Is Nothing) Then
                        Dim objImpostoPIS As XmlNode = objImpostosProduto.Item("PIS")
                        Dim objImpostoPISAux As XmlNode = objImpostosProduto.Item("PIS")
                        Dim objPISAliq As XmlNode = objImpostoPISAux.Item("PISAliq")
                        If (Not objImpostoPISAux.Item("PISAliq") Is Nothing) Then
                            objImpostoPIS = objImpostoPISAux.Item("PISAliq")
                        End If
                        If (Not objImpostoPISAux.Item("PISQtde") Is Nothing) Then
                            objImpostoPIS = objImpostoPISAux.Item("PISQtde")
                        End If
                        If (Not objImpostoPISAux.Item("PISNT") Is Nothing) Then
                            objImpostoPIS = objImpostoPISAux.Item("PISNT")
                        End If
                        If (Not objImpostoPISAux.Item("PISOutr") Is Nothing) Then
                            objImpostoPIS = objImpostoPISAux.Item("PISOutr")
                        End If

                        If (Not objImpostoPIS.Item("CST") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PIS_CST = objImpostoPIS.Item("CST").InnerText
                        End If
                        If (Not objImpostoPIS.Item("vBC") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PIS_VBC = Convert.ToDecimal(objImpostoPIS.Item("vBC").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoPIS.Item("pPIS") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PIS_PPIS = Convert.ToDecimal(objImpostoPIS.Item("pPIS").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoPIS.Item("vPIS") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PIS_VPIS = Convert.ToDecimal(objImpostoPIS.Item("vPIS").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoPIS.Item("qBCProd") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PIS_QBCPROD = Convert.ToDecimal(objImpostoPIS.Item("qBCProd").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoPIS.Item("vAliqProd") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PIS_VALIQPROD = Convert.ToDecimal(objImpostoPIS.Item("vAliqProd").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        ' Next
                    End If

                    'PIS ST
                    If (Not objImpostosProduto.Item("PISST") Is Nothing) Then
                        Dim objImpostoPISST As XmlNode = objImpostosProduto.Item("PISST")
                        If (Not objImpostoPISST.Item("vBC") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PISST_VBC = Convert.ToDecimal(objImpostoPISST.Item("vBC").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoPISST.Item("pPIS") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PISST_PPIS = Convert.ToDecimal(objImpostoPISST.Item("pPIS").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoPISST.Item("vPIS") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PISST_VPIS = Convert.ToDecimal(objImpostoPISST.Item("vPIS").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoPISST.Item("qBCProd") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PISST_QBCPROD = Convert.ToDecimal(objImpostoPISST.Item("qBCProd").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoPISST.Item("vAliqProd") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_PISST_VALIQPROD = Convert.ToDecimal(objImpostoPISST.Item("vAliqProd").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        'Next
                    End If

                    If (Not objImpostosProduto.Item("COFINS") Is Nothing) Then

                        Dim objImpostoCOFINS As XmlNode = objImpostosProduto.Item("COFINS")
                        Dim objImpostoCOFINSAux As XmlNode = objImpostosProduto.Item("COFINS")
                        If (Not objImpostoCOFINSAux.Item("COFINSAliq") Is Nothing) Then
                            objImpostoCOFINS = objImpostoCOFINSAux.Item("COFINSAliq")
                        End If
                        If (Not objImpostoCOFINSAux.Item("COFINSQtde") Is Nothing) Then
                            objImpostoCOFINS = objImpostoCOFINSAux.Item("COFINSQtde")
                        End If
                        If (Not objImpostoCOFINSAux.Item("COFINSNT") Is Nothing) Then
                            objImpostoCOFINS = objImpostoCOFINSAux.Item("COFINSNT")
                        End If
                        If (Not objImpostoCOFINSAux.Item("COFINSOutr") Is Nothing) Then
                            objImpostoCOFINS = objImpostoCOFINSAux.Item("COFINSOutr")
                        End If

                        If (Not objImpostoCOFINS.Item("CST") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINS_CST = objImpostoCOFINS.Item("CST").InnerText
                        End If
                        If (Not objImpostoCOFINS.Item("vBC") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINS_VBC = Convert.ToDecimal(objImpostoCOFINS.Item("vBC").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoCOFINS.Item("pCOFINS") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINS_PCOFINS = Convert.ToDecimal(objImpostoCOFINS.Item("pCOFINS").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoCOFINS.Item("vCOFINS") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINS_VCOFINS = Convert.ToDecimal(objImpostoCOFINS.Item("vCOFINS").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoCOFINS.Item("qBCProd") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINS_QBCPROD = Convert.ToDecimal(objImpostoCOFINS.Item("qBCProd").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoCOFINS.Item("vAliqProd") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINS_VALIQPROD = Convert.ToDecimal(objImpostoCOFINS.Item("vAliqProd").InnerText.Replace("..", ".").Replace(".", ","))
                        End If

                        'Next
                    End If

                    If (Not objImpostosProduto.Item("COFINSST") Is Nothing) Then
                        Dim objImpostoCOFINSST As XmlNode = objImpostosProduto.Item("COFINSST")
                        If (Not objImpostoCOFINSST.Item("vBC") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINSST_VBC = Convert.ToDecimal(objImpostoCOFINSST.Item("vBC").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoCOFINSST.Item("pCOFINS") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINSST_PCOFINS = Convert.ToDecimal(objImpostoCOFINSST.Item("pCOFINS").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoCOFINSST.Item("vCOFINS") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINSST_VCOFINS = Convert.ToDecimal(objImpostoCOFINSST.Item("vCOFINS").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoCOFINSST.Item("qBCProd") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINSST_QBCPROD = Convert.ToDecimal(objImpostoCOFINSST.Item("qBCProd").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoCOFINSST.Item("vAliqProd") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_COFINSST_VALIQPROD = Convert.ToDecimal(objImpostoCOFINSST.Item("vAliqProd").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        ' Next
                    End If

                    If (Not objImpostosProduto.Item("ISSQN") Is Nothing) Then
                        Dim objImpostoISSQN As XmlNode = objImpostosProduto.Item("ISSQN")
                        If (Not objImpostoISSQN.Item("vBC") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_VBC = Convert.ToDecimal(objImpostoISSQN.Item("vBC").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoISSQN.Item("vAliq") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_VALIQ = Convert.ToDecimal(objImpostoISSQN.Item("vAliq").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoISSQN.Item("vISSQN") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_VISSQN = Convert.ToDecimal(objImpostoISSQN.Item("vISSQN").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoISSQN.Item("cMunFG") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_CMUNFG = objImpostoISSQN.Item("cMunFG").InnerText
                        End If
                        If (Not objImpostoISSQN.Item("cListServ") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_CLISTSERV = objImpostoISSQN.Item("cListServ").InnerText
                        End If
                        If (Not objImpostoISSQN.Item("vDeducao") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_VDEDUCAO = Convert.ToDecimal(objImpostoISSQN.Item("vDeducao").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoISSQN.Item("vOutro") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_VOUTRO = Convert.ToDecimal(objImpostoISSQN.Item("vOutro").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoISSQN.Item("vDescIncond") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_VDESCINCOND = Convert.ToDecimal(objImpostoISSQN.Item("vDescIncond").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoISSQN.Item("vDescCond") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_VDESCCOND = Convert.ToDecimal(objImpostoISSQN.Item("vDescCond").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoISSQN.Item("vISSRet") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_VISSRET = Convert.ToDecimal(objImpostoISSQN.Item("vISSRet").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoISSQN.Item("indISS") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_INDISS = Convert.ToInt32(objImpostoISSQN.Item("indISS").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoISSQN.Item("cServico") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_CSERVICO = objImpostoISSQN.Item("cServico").InnerText
                        End If
                        If (Not objImpostoISSQN.Item("cMun") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_CMUN = objImpostoISSQN.Item("cMun").InnerText
                        End If
                        If (Not objImpostoISSQN.Item("cPais") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_CPAIS = objImpostoISSQN.Item("cPais").InnerText
                        End If
                        If (Not objImpostoISSQN.Item("nProcesso") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_NPROCESSO = objImpostoISSQN.Item("nProcesso").InnerText
                        End If
                        If (Not objImpostoISSQN.Item("indIncentivo") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ISSQN_INDINCENTIVO = Convert.ToInt32(objImpostoISSQN.Item("indIncentivo").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        'Next
                    End If

                    'PIS ST
                    If (Not objImpostosProduto.Item("ICMSUFDEST") Is Nothing) Then
                        Dim objImpostoICMSUFDEST As XmlNode = objImpostosProduto.Item("ICMSUFDEST")
                        If (Not objImpostoICMSUFDEST.Item("vBCUFDest") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ICMSUFDEST_VBCUFDEST = Convert.ToDecimal(objImpostoICMSUFDEST.Item("vBCUFDest").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoICMSUFDEST.Item("pFCPUFDest") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ICMSUFDEST_PFCPUDEST = Convert.ToDecimal(objImpostoICMSUFDEST.Item("pFCPUFDest").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoICMSUFDEST.Item("pICMSUFDest") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ICMSUFDEST_PICMSUFDEST = Convert.ToDecimal(objImpostoICMSUFDEST.Item("pICMSUFDest").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoICMSUFDEST.Item("pICMSInter") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ICMSUFDEST_PICMSINTER = Convert.ToDecimal(objImpostoICMSUFDEST.Item("pICMSInter").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoICMSUFDEST.Item("pICMSInterPart") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ICMSUFDEST_PICMSINTERPART = Convert.ToDecimal(objImpostoICMSUFDEST.Item("pICMSInterPart").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoICMSUFDEST.Item("vFCPUFDest") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ICMSUFDEST_VFCPUFDEST = Convert.ToDecimal(objImpostoICMSUFDEST.Item("vFCPUFDest").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoICMSUFDEST.Item("vICMSUFDest") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ICMSUFDEST_VICMSUFDEST = Convert.ToDecimal(objImpostoICMSUFDEST.Item("vICMSUFDest").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        If (Not objImpostoICMSUFDEST.Item("vICMSUFRemet") Is Nothing) Then
                            pObjNf.ITENS_NF(intIndex).NF_ICMSUFDEST_VICMSUFREMET = Convert.ToDecimal(objImpostoICMSUFDEST.Item("vICMSUFRemet").InnerText.Replace("..", ".").Replace(".", ","))
                        End If
                        'Next
                    End If

                    '--> VERIFICA SE EXISTEM REFERÊNCIAS DE NOTAS DE CONSIGNAÇÃO CASO O USUÁRIO JÁ TENHA DIGITADO
                    Dim dttGMType As New DataTable
                    Dim vObjNFREFConsignacao As modTBDOC_CONSIGNACAO_REFNF
                    pObjNf.ITENS_NF(intIndex).VNF_NFREF_CONSIGNACAO = New List(Of modTBDOC_CONSIGNACAO_REFNF)
                    dttGMType = modSQL.Fill("SELECT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_CONSIGNACAO_REFNF WHERE NFEID = '" & pObjNf.VNF_CHAVE_ACESSO & "' AND ITEM_NF = '" & pObjNf.ITENS_NF(intIndex).NF_PROD_ITEM & "'")
                    For Each dtrGMType As DataRow In dttGMType.Rows
                        vObjNFREFConsignacao = New modTBDOC_CONSIGNACAO_REFNF()
                        vObjNFREFConsignacao.ITEM_NF = pObjNf.ITENS_NF(intIndex).NF_PROD_ITEM
                        vObjNFREFConsignacao.NFEID = pObjNf.VNF_CHAVE_ACESSO
                        vObjNFREFConsignacao.NUMERO_REFNF = dtrGMType("NUMERO_REFNF")
                        vObjNFREFConsignacao.SERIE_REFNF = dtrGMType("SERIE_REFNF")
                        pObjNf.ITENS_NF(intIndex).VNF_NFREF_CONSIGNACAO.Add(vObjNFREFConsignacao)
                    Next

                    '--> VERIFICA SE EXISTEM REFERÊNCIAS DE NOTAS DE SUBCONTRATAÇÃO CASO O USUÁRIO JÁ TENHA DIGITADO
                    Dim dttGMTypeSubContratacao As New DataTable
                    Dim vObjNFREFSubContratacao As modTBDOC_SUBCONTRATACAO_REFNF

                    dttGMTypeSubContratacao = modSQL.Fill("SELECT NUMERO_REFNF, SERIE_REFNF, ITEM_REFNF FROM TBDOC_SUBCONTRATACAO_REFNF WHERE NFEID = '" & pObjNf.VNF_CHAVE_ACESSO & "' AND ITEM_NF = '" & pObjNf.ITENS_NF(intIndex).NF_PROD_ITEM & "'")
                    If dttGMTypeSubContratacao.Rows.Count > 0 Then
                        pObjNf.ITENS_NF(intIndex).VNF_NFREF_SUBCONTRATACAO = New List(Of modTBDOC_SUBCONTRATACAO_REFNF)
                        For Each dtrGMTypeSub As DataRow In dttGMTypeSubContratacao.Rows
                            vObjNFREFSubContratacao = New modTBDOC_SUBCONTRATACAO_REFNF()
                            vObjNFREFSubContratacao.ITEM_NF = pObjNf.ITENS_NF(intIndex).NF_PROD_ITEM
                            vObjNFREFSubContratacao.NFEID = pObjNf.VNF_CHAVE_ACESSO
                            vObjNFREFSubContratacao.NUMERO_REFNF = dtrGMTypeSub("NUMERO_REFNF")
                            vObjNFREFSubContratacao.SERIE_REFNF = dtrGMTypeSub("SERIE_REFNF")
                            vObjNFREFSubContratacao.ITEM_REFNF = dtrGMTypeSub("ITEM_REFNF")
                            pObjNf.ITENS_NF(intIndex).VNF_NFREF_SUBCONTRATACAO.Add(vObjNFREFSubContratacao)
                        Next
                    End If



                    '--> VERIFICA SE EXISTEM REFERÊNCIAS DE NOTAS COMPLEMENTARES CASO O USUÁRIO JÁ TENHA DIGITADO
                    Dim dttGMTypeNFComplementar As New DataTable
                    Dim vObjNFREFComplementar As modTBDOC_NOTA_COMPLEMENTAR_REFNF
                    dttGMTypeNFComplementar = modSQL.Fill("SELECT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_NOTA_COMPLEMENTAR_REFNF WHERE NFEID = '" & pObjNf.VNF_CHAVE_ACESSO & "' AND ITEM_NF = '" & pObjNf.ITENS_NF(intIndex).NF_PROD_ITEM & "'")
                    If dttGMTypeNFComplementar.Rows.Count > 0 Then
                        pObjNf.ITENS_NF(intIndex).VNF_NFREF_NOTA_COMPLEMENTAR = New List(Of modTBDOC_NOTA_COMPLEMENTAR_REFNF)
                        For Each dtrGMType As DataRow In dttGMTypeNFComplementar.Rows
                            vObjNFREFComplementar = New modTBDOC_NOTA_COMPLEMENTAR_REFNF()
                            vObjNFREFComplementar.ITEM_NF = pObjNf.ITENS_NF(intIndex).NF_PROD_ITEM
                            vObjNFREFComplementar.NFEID = pObjNf.VNF_CHAVE_ACESSO
                            vObjNFREFComplementar.NUMERO_REFNF = dtrGMType("NUMERO_REFNF")
                            vObjNFREFComplementar.SERIE_REFNF = dtrGMType("SERIE_REFNF")
                            pObjNf.ITENS_NF(intIndex).VNF_NFREF_NOTA_COMPLEMENTAR.Add(vObjNFREFComplementar)
                        Next
                    End If









                End If
                intIndex += 1
            Next

            If Not isSomenteLeitura Then
                '---> ENVIA E-MAIL SE CFOP IGUAL A 5915 ou 6915
                If Not pObjNf.IGNORE_EMAIL And IsEnviarEmailCfop Then
                    modSQL.CommandText = "select VALOR from TbPAR where PARAMETRO = 'RESPONSAVEL_CFOP'"
                    SEND_TO = modSQL.ExecuteScalar(modSQL.CommandText)
                    EnviarMensagem(SEND_TO, "Nota Fiscal: CFOP 5915 ou 6915", "Uma nota fiscal da CFOP 5915 ou 6915 foi recebida pelo VNF e requer a sua atenção: " & Environment.NewLine & Environment.NewLine &
                                            "Número da NF: " & pObjNf.NF_IDE_NNF & Environment.NewLine &
                                            "Chave de acesso: " & pObjNf.VNF_CHAVE_ACESSO & Environment.NewLine &
                                            "Fornecedor: " & pObjNf.NF_EMIT_XNOME & Environment.NewLine &
                                            "Data de emissão: " & pObjNf.NF_IDE_DHEMI)
                End If
            End If
        Catch ex As Exception
            If Not isSomenteLeitura Then
                RegistrarLog(Nothing, ex)
            End If
            Throw New Exception("Ocorreu um erro ao ler os itens do XML")
        End Try
    End Sub
#End Region

#Region " Preenche ICMS Item "
    Private Sub PreencheICMSItem(ByVal objImposto As XmlNode, ByVal strIcmsName As String, ByVal intIndex As Integer, ByRef _objNF As modNF)
        Try
            Dim objICMSGrupo As XmlNode = objImposto.Item("ICMS")
            Dim objICMS = objICMSGrupo.Item(strIcmsName)

            If (Not objICMS Is Nothing) Then
                If (Not objICMS.Item("pICMS") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_PICMS = Convert.ToDecimal(objICMS.Item("pICMS").InnerText.Replace("..", ".").Replace(".", ","))
                End If

                'Campos novos
                If (Not objICMS.Item("orig") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_ORIG = objICMS.Item("orig").InnerText
                End If
                If (Not objICMS.Item("CST") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_CST = objICMS.Item("CST").InnerText
                End If
                If (Not objICMS.Item("modBC") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_MODBC = Convert.ToInt32(objICMS.Item("modBC").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("pRedBC") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_PREDBC = Convert.ToDecimal(objICMS.Item("pRedBC").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vBC") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VBC = Convert.ToDecimal(objICMS.Item("vBC").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vICMS") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VICMS = Convert.ToDecimal(objICMS.Item("vICMS").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("modBCST") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_MODBCST = Convert.ToInt32(objICMS.Item("modBCST").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("pMVAST") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_MVAST = Convert.ToDecimal(objICMS.Item("pMVAST").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("pRedBCST") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMSREDBCST = Convert.ToDecimal(objICMS.Item("pRedBCST").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vBCST") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VBCST = Convert.ToDecimal(objICMS.Item("vBCST").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("pICMSST") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_PICMSST = Convert.ToDecimal(objICMS.Item("pICMSST").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vICMSST") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VICMSST = Convert.ToDecimal(objICMS.Item("vICMSST").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vBCSTRet") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VBCSTRET = Convert.ToDecimal(objICMS.Item("vBCSTRet").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vBCSTDest") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VBCSTDEST = Convert.ToDecimal(objICMS.Item("vBCSTDest").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vICMSSTDest") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VICMSSTDEST = Convert.ToDecimal(objICMS.Item("vICMSSTDest").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("motDesICMS") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_MOTDESICMS = Convert.ToInt32(objICMS.Item("motDesICMS").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("pBCOp") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_PBCOP = Convert.ToDecimal(objICMS.Item("pBCOp").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("UFST") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_UFST = objICMS.Item("UFST").InnerText
                End If
                If (Not objICMS.Item("pCredSN") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_PCREDSN = Convert.ToDecimal(objICMS.Item("pCredSN").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vCredICMSSN") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VCREICMSSN = Convert.ToDecimal(objICMS.Item("vCredICMSSN").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vICMSDeson") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VICMSDESON = Convert.ToDecimal(objICMS.Item("vICMSDeson").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vICMSOp") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VICMSOP = Convert.ToDecimal(objICMS.Item("vICMSOp").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("pDif") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_PDIF = Convert.ToDecimal(objICMS.Item("pDif").InnerText.Replace("..", ".").Replace(".", ","))
                End If
                If (Not objICMS.Item("vICMSDif") Is Nothing) Then
                    _objNF.ITENS_NF(intIndex).NF_ICMS_VICMSDIF = Convert.ToDecimal(objICMS.Item("vICMSDif").InnerText.Replace("..", ".").Replace(".", ","))
                End If
            End If
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao ler o ICMS dos itens do XML")
        End Try
    End Sub
#End Region

#Region " EnviarMensagemComprador "
    ''' <summary>
    ''' Rotina de envio de e-mail ao comprador
    ''' </summary>
    ''' <param name="CHAVE_ACESSO"></param>
    ''' <param name="PEDCOM"></param>
    ''' <param name="CODCOM"></param>
    ''' <example>'Marcio Spinosa - 27/09/2018 - CR00009259 - Ajuste para diferenciar e-mail de comprador e fornecedor</example>
    Private Sub EnviarMensagemComprador(ByVal CHAVE_ACESSO As String, ByVal PEDCOM As String, ByVal CODCOM As String)
        Try
            '--> SE JÁ FOI ENVIADA MENSAGEM PARA O FORNECEDOR NO MESMO DIA COM O MESMO STATUS, NÃO ENVIA NOVAMENTE PARA O COMPRADOR TAMBÉM
            'Marcio Spinosa - 27/09/2018 - CR00009259
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_038: Função: EnviarMensagemComprador.", "verificando se o comprador já recebeu mensagem. NFEID:" & CHAVE_ACESSO)
            'modSQL.CommandText = "select count(*) from tbMen where nfeid = '" & CHAVE_ACESSO & "' and SITUACAO = 'NAO AUTORIZADA' and datenv > cast('" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' as datetime) - 1 "
            modSQL.CommandText = "select count(1) from tbMen where nfeid = '" & CHAVE_ACESSO & "' and SITUACAO = 'NAO AUTORIZADA' and datenv > cast('" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' as datetime) - 1 and TIPOMEN = 'C'"
            'Marcio Spinosa - 27/09/2018 - CR00009259 - Fim
            If Integer.Parse(modSQL.ExecuteScalar(modSQL.CommandText)) > 0 Then
                Return
            End If
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_039: Função: EnviarMensagemComprador.", "Montando corpo do e-mail para o comprador. NFEID:" & CHAVE_ACESSO)  'Marcio Spinosa - 27/09/2018 - CR00009259
            Dim BODY As String = "<p style='font-family:verdana; font-size:10pt; line-height:150%;'>" & Chr(13) &
                                     "<b>Prezado comprador,</b> <br/>" & Chr(13) &
                                     "O fornecedor " & Replace(objNF.NF_EMIT_XNOME, ".", "").ToUpper & " emitiu a nota " &
                                     "fiscal eletrônica número <b>" & objNF.NF_IDE_NNF & "</b>, esta encontra-se em não conformidade com o pedido <b>" & PEDCOM & "</b>. <br/>" &
                                     "Você deverá verificar no Validador de Notas Fiscais quais não conformidades foram encontradas.<br/><br/>" & Chr(13) & Chr(13) &
                                     "Chave de Acesso <b>" & Chr(13) & CHAVE_ACESSO & "</b><br/><br/><br/>" & Chr(13) & Chr(13) &
                                     "Atenciosamente, <br/>" & Chr(13) &
                                     "Neles Brasil Industria e Comércio LTDA" &
                                 "</p>"

            Dim strBodyText As String = BODY.Replace("<p style='font-family:verdana; font-size:10pt; line-height:150%;'>", "").Replace("<b>", "").Replace("</b>", "").Replace("<br/>", "").Replace("</p>", "")
            'strBodyText = strBodyText.Replace("<a href='" & PORTAL & "' target='_blank'>", "").Replace("</a>", "").Replace("'", """")

            Dim FORNECEDOR As String = IIf(objNF.NF_EMIT_XNOME.Split(" ").Length > 1, objNF.NF_EMIT_XNOME.Split(" ")(0), objNF.NF_EMIT_XNOME).ToUpper
            Dim SEND_TO As String = modSQL.ExecuteScalar("select isnull(EMAIL,'') from tbCOM where CODCOM = '" & CODCOM & "'")
            Dim SUBJECT As String = "NF " & objNF.NF_IDE_NNF & " REJEITADA (" & FORNECEDOR & ") - PEDIDO " & PEDCOM

            'Marcio Spinosa - 27/09/2018 - CR00009259
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_040: Função: EnviarMensagemComprador.", "Inserindo a mensagem na TBMen. NFEID:" & CHAVE_ACESSO)  'Marcio Spinosa - 27/09/2018 - CR00009259
            Dim QUERY As String
            'QUERY = "insert into TbMEN values ('" & CHAVE_ACESSO & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '" & SEND_TO & "', 'NAO AUTORIZADA', '" & strBodyText & "')"
            QUERY = "insert into TbMEN values ('" & CHAVE_ACESSO & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '" & SEND_TO & "', 'NAO AUTORIZADA', '" & strBodyText & "', 'C')"
            modSQL.ExecuteNonQuery(QUERY)
            RegistrarLog(TipoProcessamento.Ambos, Nothing, "Tracelog_041: Função: EnviarMensagemComprador.", "Chamando função para envio da mensagem (EnviarMensagem). NFEID:" & CHAVE_ACESSO)  'Marcio Spinosa - 27/09/2018 - CR00009259
            'Marcio Spinosa - 27/09/2018 - CR00009259 - Fim

            EnviarMensagem(SEND_TO, SUBJECT, BODY)

        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao enviar mensagem para o comprador")
        End Try
    End Sub
#End Region

#Region " ApagarDocumentoProcessado "
    Private Sub ApagarDocumentoProcessado(ByVal CHAVE_ACESSO As String)
        Dim vIntRowsAffected As Integer

        Try
            '--> Se for talonario nao limpa os dados.
            modSQL.CommandText = "SELECT IdTalonario FROM Talonario WHERE ChaveAcesso = '" & CHAVE_ACESSO & "'"
            Dim DtTalonario As DataTable = modSQL.Fill(modSQL.CommandText)
            If DtTalonario.Rows.Count = 0 Then
                'Marcio Spinosa - 24/07/2019 - SR00292583 - Problema de voltar as divergências ativas
                'modSQL.ExecuteNonQuery("DELETE FROM tblog WHERE nfeid = '" & CHAVE_ACESSO & "'")
                modSQL.ExecuteNonQuery("DELETE FROM tblog WHERE nfeid = '" & CHAVE_ACESSO & "' and isnull(ltrim(rtrim(acecodusu)), '') = '' and motivo <> 'CORRIGIDO' ")
                'Marcio Spinosa - 24/07/2019 - SR00292583 - Fim
                vIntRowsAffected = modSQL.ExecuteNonQuery("UPDATE tbnfe SET reprocessar = 'S' WHERE nfeid = '" & CHAVE_ACESSO & "'")
                '*****************************************************************************************************
                'Verifica se algum registro foi atualizado, caso sim não faz nada caso contrário verifica a tabela de 
                'documentos faltantes para avaliar a necessidade de isnerir o documento para reprocessamento
                '*****************************************************************************************************
                If vIntRowsAffected <= 0 & CHAVE_ACESSO.Length = 44 Then
                    Dim fObjRetorno = modSQL.ExecuteScalar("SELECT COUNT(NFEID) AS QTD FROM TbDocsFaltandoIntegracao WHERE nfeid = '" & CHAVE_ACESSO & "'")
                    If Integer.Parse(fObjRetorno) <= 0 Then
                        modSQL.ExecuteNonQuery("INSERT INTO TbDocsFaltandoIntegracao VALUES('" & CHAVE_ACESSO & "', GETDATE())")
                    End If
                End If

            End If
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro durante a validação")
        End Try
    End Sub
#End Region

#Region " RegistrarLog "
    Public Sub RegistrarLog(ByVal pTipoProcessamento As TipoProcessamento, ByVal pException As Exception, Optional ByVal pTitulo As String = "", Optional ByVal pDescricao As String = "", Optional ByVal pTrace As String = "")
        Try
            Dim strQuery As String = "INSERT INTO tblogservice (" &
                                 "   log_titulo " &
                                 "  ,log_descricao " &
                                 "  ,log_trace " &
                                 "  ,log_guid " &
                                 "  ,log_data " &
                                 ") VALUES ( " &
                                 "   '@log_titulo' " &
                                 "  ,'@log_descricao' " &
                                 "  ,'@log_trace' " &
                                 "  ,'@log_guid' " &
                                 "  ,'" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' " &
                                 ")"

            Dim strTipoProcessamento As String = IIf(pTipoProcessamento = Nothing, "", pTipoProcessamento.ToString().Replace("'", "") & " - ")

            If (String.IsNullOrEmpty(pTitulo) AndAlso String.IsNullOrEmpty(pDescricao)) Then

                strQuery = strQuery.Replace("@log_titulo", strTipoProcessamento & objNF.VNF_CHAVE_ACESSO)
                If Not (pException Is Nothing) Then
                    strQuery = strQuery.Replace("@log_descricao", "MESSAGE: " & pException.Message.Replace("'", "") & Environment.NewLine &
                                                                  "STRACK TRACE: " & pException.StackTrace.Replace("'", ""))
                End If
                If Not (pException.InnerException Is Nothing) Then
                    strQuery = strQuery.Replace("@log_trace", "INNER EXCEPTION: " & pException.InnerException.Message.Replace("'", ""))
                    '"SQL COMMAND: " & modSQL.CommandText.Replace("'", ""))
                Else
                    strQuery = strQuery.Replace("@log_trace", "")
                End If
            Else
                strQuery = strQuery.Replace("@log_titulo", pTitulo.Replace("'", ""))
                strQuery = strQuery.Replace("@log_descricao", pDescricao.Replace("'", ""))
                strQuery = strQuery.Replace("@log_trace", pTrace)
            End If

            strQuery = strQuery.Replace("@log_guid", If(LogGUID = Guid.Empty, "", LogGUID.ToString()))

            modSQL.ExecuteNonQuery(strQuery)
        Catch ex As Exception
            Dim qqcoisa As String = ex.Message
        End Try
    End Sub
#End Region

#Region " RegistrarNotaProblema "
    Private Sub RegistrarNotaProblema(ByVal pstrChaveAcesso As String, ByVal pStrEx As String, ByVal pMetodo As String)
        Try
            If VariaveisGlobais.ptipoProcssamento = 1 Then
                Dim strQuery As String = "INSERT INTO tbnotasproblemas values(" &
                                       "   nfeid " &
                                       "  ,data " &
                                       "  ,exception " &
                                       "  ,nomeMetodo" &
                                       ") VALUES ( " &
                                       "   '@nfeid' " &
                                       "  ,'@data' " &
                                       "  ,'@exception' " &
                                       "  , '@nomeMetodo' " &
                                       ")"

                strQuery = strQuery.Replace("@nfeid", pstrChaveAcesso)
                strQuery = strQuery.Replace("@data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                strQuery = strQuery.Replace("@exception", pStrEx)
                strQuery = strQuery.Replace("@nomeMetodo", pMetodo)
                modSQL.ExecuteNonQuery(strQuery)

                strQuery = String.Empty

                strQuery = "update tbnfe set qtdeReprocessamento = QTDE_REPROCESSAMENTO + 1 where nfeid = '" & pstrChaveAcesso & "'"

                modSQL.ExecuteNonQuery(strQuery)
            End If
        Catch ex As Exception
            Dim qqcoisa As String = ex.Message
        End Try
    End Sub
#End Region



#Region " GetInboundDeliveries "
    Private Function GetInboundDeliveries(ByVal item As modNFItem, Optional ByRef dttNotasInbound As DataTable = Nothing) As List(Of modInboundDelivery)
        Try
            '--> SE NÃO TIVER O NÚMERO DA PO, NÃO EXECUTA A CONSULTA
            If String.IsNullOrEmpty(item.SAP_PO_NUMBER) Then
                Return New List(Of modInboundDelivery)()
            End If

            'Marcio Spinosa - 08/01/2020 - 
            'Dim strQuery As String = "SELECT  distinct " &
            '                         "	tbdoc_item_nfe.nfeid, " &
            '                         "	tbdoc_cab_nfe.nf_ide_nnf, " &
            '                         "	tbdoc_cab_nfe.nf_ide_serie, " &
            '                         "	tbdoc_cab_nfe.nf_ide_dhemi, " &
            '                         "	tbdoc_item_nfe.nf_prod_item, " &
            '                         "	tbdoc_item_nfe.nf_prod_cfop, " &
            '                         "	tbnfe.situacao, " &
            '                         "	tbdoc_item.vnf_inbound_delivery_number, " &
            '                         "	tbdoc_item.vnf_inbound_delivery_item_number,	 " &
            '                         "	isnull(nf_prod_qcom, 0) as nf_prod_qcom " &
            '                         "FROM 	 " &
            '                         "	tbdoc_item_nfe " &
            '                         "	left join tbdoc_cab_nfe on tbdoc_item_nfe.nfeid = tbdoc_cab_nfe.nfeid " &
            '                         "	left join tbjun on tbdoc_item_nfe.nfeid = tbjun.nfeid and  tbdoc_item_nfe.nf_prod_item = tbjun.itenfe " &
            '                         "	left join tbdoc_item on tbjun.codjun = tbdoc_item.vnf_codjun " &
            '                         "	left join tbintegracao on tbjun.nfeid = tbintegracao.nfeid and  tbjun.itenfe = tbintegracao.INT_MIGO_NF_ITEM_NUMBER " &
            '                         "	left join tblog on tbdoc_item_nfe.nfeid = tblog.nfeid and nf_prod_item = tblog.itenfe and tblog.campo = 'INBOUND DELIVERY' and tblog.valor_ped = 'SALDO INSUFICIENTE' " &
            '                         "	left join tbnfe on tbdoc_item_nfe.nfeid = tbnfe.nfeid " &
            '                         "WHERE  	 " &
            '                         "	tbjun.pedcom = '" & item.SAP_PO_NUMBER & "' and  " &
            '                         "	tbjun.iteped = '" & item.SAP_ITEM_DETAILS.ITEM_NUMBER & "' and  " &
            '                         "	tbintegracao.nfeid is null and " &
            '                         "	tbnfe.situacao <> 'CANCELADA' and  " &
            '                         "	tbnfe.situacao <> 'RECUSADA' and  " &
            '                         "	nf_prod_cfop not in (SELECT vex_cfop  " &
            '                         "						 FROM tbValidacoesExcecoes inner join tbValidacoes on vex_id_validacao = id_validacao  " &
            '                         "						 WHERE val_codigo = 'INBOUND_DELIVERY')  " &
            '                         "ORDER BY " &
            '                         "	tbdoc_cab_nfe.nf_ide_dhemi "

            Dim strQuery As String = "SELECT  distinct " &
                                     "	tbdoc_item_nfe.nfeid, " &
                                     "	tbdoc_cab_nfe.nf_ide_nnf, " &
                                     "	tbdoc_cab_nfe.nf_ide_serie, " &
                                     "	tbdoc_cab_nfe.nf_ide_dhemi, " &
                                     "	tbdoc_item_nfe.nf_prod_item, " &
                                     "	tbdoc_item_nfe.nf_prod_cfop, " &
                                     "	tbnfe.situacao, " &
                                     "	tbdoc_item.vnf_inbound_delivery_number, " &
                                     "	tbdoc_item.vnf_inbound_delivery_item_number,	 " &
                                     "	isnull(nf_prod_qcom, 0) as nf_prod_qcom " &
                                     "FROM 	 " &
                                     "	tbdoc_item_nfe " &
                                     "	left join tbdoc_cab_nfe on tbdoc_item_nfe.nfeid = tbdoc_cab_nfe.nfeid " &
                                     "	left join tbjun on tbdoc_item_nfe.nfeid = tbjun.nfeid and  tbdoc_item_nfe.nf_prod_item = tbjun.itenfe " &
                                     "	left join tbdoc_item on tbjun.codjun = tbdoc_item.vnf_codjun and tbjun.ITEPED = TbDOC_ITEM.VNF_INBOUND_DELIVERY_ITEM_NUMBER	" &
                                     "	left join tbintegracao on tbjun.nfeid = tbintegracao.nfeid and  tbjun.itenfe = tbintegracao.INT_MIGO_NF_ITEM_NUMBER " &
                                     "	left join tblog on tbdoc_item_nfe.nfeid = tblog.nfeid and nf_prod_item = tblog.itenfe and tblog.campo = 'INBOUND DELIVERY' and tblog.valor_ped = 'SALDO INSUFICIENTE' " &
                                     "	left join tbnfe on tbdoc_item_nfe.nfeid = tbnfe.nfeid " &
                                     "WHERE  	 " &
                                     "	tbjun.pedcom = '" & item.SAP_PO_NUMBER & "' and  " &
                                     "	tbjun.iteped = '" & item.SAP_ITEM_DETAILS.ITEM_NUMBER & "' and  " &
                                     "	tbintegracao.nfeid is null and " &
                                     "	tbnfe.situacao <> 'CANCELADA' and  " &
                                     "	tbnfe.situacao <> 'RECUSADA' and  " &
                                     "	nf_prod_cfop not in (SELECT vex_cfop  " &
                                     "						 FROM tbValidacoesExcecoes inner join tbValidacoes on vex_id_validacao = id_validacao  " &
                                     "						 WHERE val_codigo = 'INBOUND_DELIVERY')  " &
                                     "ORDER BY " &
                                     "	tbdoc_cab_nfe.nf_ide_dhemi "
            'Marcio Spinosa - 08/01/2020 - Fim
            If (dttNotasInbound Is Nothing) Then
                dttNotasInbound = New DataTable()
            End If

            dttNotasInbound = modSQL.Fill(strQuery)

            If item.SAP_ITEM_DETAILS.ITEM_CONFIRMATION Is Nothing Then
                Return New List(Of modInboundDelivery)()
            End If

            '--> 1. PERCORRE TODAS AS INBOUNDS DO PEDINDO E ASSOCIA COM AS NOTAS FISCAIS PROCESSADAS
            Dim arrSapInbound As New List(Of SAP_RFC.PurchaseOrderItemsConfirmations)()
            arrSapInbound = item.SAP_ITEM_DETAILS.ITEM_CONFIRMATION.Where(Function(x) x.CONFIRMATION_CATEGORY = "LA" And Not String.IsNullOrEmpty(x.INBOUND_DELIVERY_NUMBER)).ToList()
            arrSapInbound = arrSapInbound.OrderBy(Function(x) x.DELIVERY_DATE).ToList().OrderBy(Function(x) x.OPEN_QTY).ToList()
            'Dim datFirstDeliveryDate As DateTime = arrSapInbound.Where(Function(x) x.OPEN_QTY > 0).FirstOrDefault().DELIVERY_DATE
            'arrSapInbound = arrSapInbound.Where(Function(x) x.DELIVERY_DATE = datFirstDeliveryDate).ToList()

            Dim arrVnfInbound As New List(Of modInboundDelivery)()
            Dim objVnfInbound As modInboundDelivery
            For Each itemSapInbound In arrSapInbound
                objVnfInbound = New modInboundDelivery()
                objVnfInbound.SAP_INBOUND_DELIVERY_NUMBER = itemSapInbound.INBOUND_DELIVERY_NUMBER
                objVnfInbound.SAP_INBOUND_DELIVERY_ITEM_NUMBER = itemSapInbound.INBOUND_DELIVERY_ITEM_NUMBER
                objVnfInbound.SAP_DELIVERY_DATE = itemSapInbound.DELIVERY_DATE
                objVnfInbound.SAP_QTY = itemSapInbound.OPEN_QTY
                objVnfInbound.OPEN_QTY = objVnfInbound.SAP_QTY
                objVnfInbound.NOTAS_FISCAIS = New List(Of modInboundDeliveryNFs)()
                arrVnfInbound.Add(objVnfInbound)
            Next

            Dim objInboundNf As modInboundDeliveryNFs
            For Each itemVnfInbound As modInboundDelivery In arrVnfInbound
                For Each dtrNotaFiscal As DataRow In dttNotasInbound.Select("vnf_inbound_delivery_number <> ''")
                    If itemVnfInbound.SAP_INBOUND_DELIVERY_NUMBER = dtrNotaFiscal("vnf_inbound_delivery_number").ToString And itemVnfInbound.SAP_INBOUND_DELIVERY_ITEM_NUMBER = Convert.ToDecimal(dtrNotaFiscal("vnf_inbound_delivery_item_number").ToString) Then
                        objInboundNf = New modInboundDeliveryNFs()
                        objInboundNf.VNF_NFEID = dtrNotaFiscal("nfeid").ToString
                        objInboundNf.VNF_NUMERO = dtrNotaFiscal("nf_ide_nnf").ToString
                        objInboundNf.VNF_SERIE = dtrNotaFiscal("nf_ide_serie").ToString
                        objInboundNf.VNF_EMISSAO = Convert.ToDateTime(dtrNotaFiscal("nf_ide_dhemi").ToString)
                        objInboundNf.VNF_ITEM = Convert.ToInt32(dtrNotaFiscal("nf_prod_item").ToString)
                        objInboundNf.VNF_CFOP = dtrNotaFiscal("nf_prod_cfop").ToString
                        objInboundNf.VNF_SITUACAO = dtrNotaFiscal("situacao").ToString
                        objInboundNf.VNF_QTY = Convert.ToDecimal(dtrNotaFiscal("nf_prod_qcom").ToString)

                        If objNF.VNF_CHAVE_ACESSO = dtrNotaFiscal("nfeid").ToString And item.NF_PROD_ITEM = Convert.ToInt32(dtrNotaFiscal("nf_prod_item").ToString) Then
                            item.VNF_INBOUND_DELIVERY_NUMBER = dtrNotaFiscal("vnf_inbound_delivery_number").ToString
                            item.VNF_INBOUND_DELIVERY_ITEM_NUMBER = Convert.ToDecimal(dtrNotaFiscal("vnf_inbound_delivery_item_number").ToString)
                        End If

                        itemVnfInbound.OPEN_QTY = itemVnfInbound.OPEN_QTY - objInboundNf.VNF_QTY
                        itemVnfInbound.NOTAS_FISCAIS.Add(objInboundNf)
                    End If
                Next
            Next

            Return arrVnfInbound
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro ao carregar as inbounds deliveries")
        End Try
    End Function
#End Region

#End Region

#Region " Validações da NFe "

    Private Sub ValidarSituacaoSefaz(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("SITUACAO_TRIANGULUS", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim strDescricaoStatus As String
            Dim strQuery As String = "SELECT stat FROM NFF02_DOC WITH (NOLOCK) WHERE CHAVE_ACESSO = '" & objNF.VNF_CHAVE_ACESSO & "'"
            Dim dttTriangulus As DataTable = modSQL.Fill(strQuery, modSQL.connectionStringTriangulus)

            If (dttTriangulus.Rows.Count = 0) Then
                strDescricaoStatus = "NÃO ENCONTRADO NO TRIANGULUS"
                InserirDivergencia(item, objRegras.TituloUsuario, strDescricaoStatus, "")
            Else
                Dim strStatusTriangulus = dttTriangulus.Rows(0)("stat").ToString
                strDescricaoStatus = "(" & strStatusTriangulus & ") " & modSQL.ExecuteScalar("SELECT descricao_status FROM TbStatusTriangulus WHERE id_status_triangulus = " & strStatusTriangulus).ToString()

                objNF.NF_STATUS_TRIANGULUS = strStatusTriangulus 'Marcio Spinosa - 29/11/2018 - CR00009165
                If strStatusTriangulus = "101" Or strStatusTriangulus = "155" Then 'Marcio Spinosa - 29/11/2018 - CR00009165
                    Dim objBLNotaFiscal As New BLNotaFiscal()
                    objBLNotaFiscal.Cancelar(objNF.VNF_CHAVE_ACESSO, String.Empty, objNF.IGNORE_EMAIL)
                    For Each itemNF As modNFItem In objNF.ITENS_NF
                        itemNF.VNF_ITEM_VALIDO = "N"
                    Next
                ElseIf Not objRegras.IsExcecao AndAlso strStatusTriangulus <> "100" Then
                    InserirDivergencia(item, objRegras.TituloUsuario, strDescricaoStatus, "")
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, "")
                End If
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strDescricaoStatus, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do status do documento no Triangulus")
        End Try
    End Sub

    Private Sub ValidarIncoterms(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("INCOTERMS", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            '--> MOD FRETE (NOTA FISCAL)
            '------| 0: EMITENTE
            '------| 1: DESTINATÁRIO
            '------| 2: TERCEIROS
            '------| 9: SEM FRETE
            Dim strCnpjResponsavel As String = ""
            Dim strValorNf As String = ""
            If (objNF.NF_TRANSP_MODFRETE = "0") Then
                strValorNf = "(0) EMITENTE"
                strCnpjResponsavel = objNF.NF_EMIT_CNPJ
            ElseIf (objNF.NF_TRANSP_MODFRETE = "1") Then
                strValorNf = "(1) DESTINATÁRIO"
                strCnpjResponsavel = objNF.NF_DEST_CNPJ
            ElseIf (objNF.NF_TRANSP_MODFRETE = "2") Then
                strValorNf = "(2) TERCEIROS"
                strCnpjResponsavel = ""
            ElseIf (objNF.NF_TRANSP_MODFRETE = "9") Then
                strValorNf = "(9) SEM FRETE"
                strCnpjResponsavel = ""
            End If

            Dim strResponsabilidadeNf As String = ""
            If (Not String.IsNullOrEmpty(strCnpjResponsavel)) Then
                If (Convert.ToInt32(modSQL.ExecuteScalar("SELECT isnull(count(*), 0) FROM TbPlantaCnpj WHERE CNPJ = '" & strCnpjResponsavel & "'").ToString()) = 0) Then
                    strResponsabilidadeNf = "Fornecedor"
                Else
                    strResponsabilidadeNf = "Comprador"
                End If
            End If

            '--> INCOTERMS (PEDIDO DE COMPRA)
            '------| CIF: FORNECEDOR
            '------| FOB: METSO
            Dim strResponsabilidadePo As String = ""
            Dim strValorPo As String = ""
            If (objNF.SAP_DETAILS.INCOTERMS1 = "CIF") Then
                strValorPo = "(CIF) FORNECEDOR"
                strResponsabilidadePo = "Fornecedor"
            ElseIf (objNF.SAP_DETAILS.INCOTERMS1 = "DAP") Then
                strValorPo = "(DAP) FORNECEDOR"
                strResponsabilidadePo = "Fornecedor"
            ElseIf (objNF.SAP_DETAILS.INCOTERMS1 = "FOB") Then
                strValorPo = "(FOB) COMPRADOR"
                strResponsabilidadePo = "Comprador"
            ElseIf (objNF.SAP_DETAILS.INCOTERMS1 = "FCA") Then
                strValorPo = "(FCA) COMPRADOR"
                strResponsabilidadePo = "Comprador"
            End If

            If Not objRegras.IsExcecao AndAlso Not String.IsNullOrEmpty(strResponsabilidadeNf) AndAlso Not String.IsNullOrEmpty(strResponsabilidadePo) AndAlso strResponsabilidadeNf <> strResponsabilidadePo Then
                InserirDivergencia(item, objRegras.TituloUsuario, strValorNf, strValorPo)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, strValorNf, strValorPo, dataComparacao, usuario)

        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de incoterms")
        End Try
    End Sub
    ''' <summary>
    ''' Método para validação do NCM da nota
    ''' </summary>
    ''' <param name="item"></param>
    ''' <param name="dataComparacao"></param>
    ''' <param name="usuario"></param>
    ''' <remarks></remarks>
    ''' <example>Marcio Spniosa - 22/08/2018 - CR00008351 - Ajuste efetuado retirando a validação do material se esta vazio</example>
    Private Sub ValidarNCM(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("NCM", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim decNcm As Long
            Long.TryParse(item.NF_PROD_NCM, decNcm)
            Dim strNcm As String = decNcm.ToString("0000\.00\.00")

            'Marcio Spniosa - 22/08/2018 - CR00008351
            'If Not objRegras.IsExcecao AndAlso (strNcm.RemoveLetters() <> item.SAP_ITEM_DETAILS.NCM_CODE.RemoveLetters() And item.SAP_ITEM_DETAILS.MATERIAL <> "" And ValidarNCM(item.SAP_ITEM_DETAILS.MATERIAL)) Then
            If Not objRegras.IsExcecao AndAlso (strNcm.RemoveLetters() <> item.SAP_ITEM_DETAILS.NCM_CODE.RemoveLetters() And ValidarNCM(item.SAP_ITEM_DETAILS.MATERIAL)) Then
                'Marcio Spniosa - 22/08/2018 - CR00008351 - Fim
                InserirDivergencia(item, objRegras.TituloUsuario, strNcm, item.SAP_ITEM_DETAILS.NCM_CODE)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, strNcm, item.SAP_ITEM_DETAILS.NCM_CODE, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de NCM")
        End Try
    End Sub

    Private Sub ValidarRemessaFinal(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("REMESSA_FINAL", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso item.SAP_ITEM_DETAILS.DELIVERY_COMPLETED = "X" Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strTextoReprovacao, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de remessa final")
        End Try
    End Sub

    Private Sub ValidarItemDeletado(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("DELETADO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso item.SAP_ITEM_DETAILS.DELETION_INDICATOR = "L" Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", item.SAP_ITEM_DETAILS.DELETION_INDICATOR, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de item deletado")
        End Try
    End Sub

    Private Sub ValidarQuantidade(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String, Optional ByVal objNF As modNF = Nothing, Optional ByRef pLisColectionPOProcessed As List(Of Long) = Nothing, Optional ByVal pStrMovementTypes As String = "")
        Dim vBlnMultiplosItensUnicoPedido = False
        Dim SomaTotalQuantPedidoItem As Decimal
        Dim objRetorno As Object
        Dim vDecReceipts As Decimal '' Baixa saldo do PO
        Dim vDecIssues As Decimal  ''Retorna saldo no PO (estornos/devoluções)
        Dim vDevOPEN_QUANTITY As Decimal
        Dim vlistMovement As String '' Lista de movimentos para validação da quantidade 'Marcio Spinosa - 25/02/2019 - CR00009165

        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("QUANTIDADE", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            'Realiza o cálculo da quantidade disponivel de saldo conforme histórico de tipo de movimentos do pedido
            'Marcio Spinosa - 25/02/2019 - CR00009165
            'vDecReceipts = item.SAP_ITEM_DETAILS.ITEM_HISTORY.Where(Function(x) x.HIST_DEBIT_CREDIT_CODE_SHKZG = "S" And pStrMovementTypes.Contains(x.HIST_MOVEMENT_TYPE_BWART)).Sum(Function(x) x.HIST_QUANTITY_BAMNG)
            'vDecIssues = item.SAP_ITEM_DETAILS.ITEM_HISTORY.Where(Function(x) x.HIST_DEBIT_CREDIT_CODE_SHKZG = "H" And pStrMovementTypes.Contains(x.HIST_MOVEMENT_TYPE_BWART)).Sum(Function(x) x.HIST_QUANTITY_BAMNG)
            vlistMovement = modSQL.ExecuteScalar("Select VALOR from tbpar where parametro = 'VALIDAR_QUANTIDADE' ")

            vDecReceipts = item.SAP_ITEM_DETAILS.ITEM_HISTORY.Where(Function(x) x.HIST_DEBIT_CREDIT_CODE_SHKZG = "S" And x.HIST_MOVEMENT_TYPE_BWART <> "" And vlistMovement.Contains(x.HIST_MOVEMENT_TYPE_BWART)).Sum(Function(x) x.HIST_QUANTITY_MENGE)
            vDecIssues = item.SAP_ITEM_DETAILS.ITEM_HISTORY.Where(Function(x) x.HIST_DEBIT_CREDIT_CODE_SHKZG = "H" And x.HIST_MOVEMENT_TYPE_BWART <> "" And vlistMovement.Contains(x.HIST_MOVEMENT_TYPE_BWART)).Sum(Function(x) x.HIST_QUANTITY_MENGE)
            'Marcio Spinosa - 25/02/2019 - CR00009165 - Fim
            vDevOPEN_QUANTITY = item.SAP_ITEM_DETAILS.PO_QUANTITY - vDecReceipts + vDecIssues

            'Identifica e agrupa todos os itens da nota do mesmo pedido/item do SAP e pesiste dentro em um objeto de controle para que o mesmo
            'pedido item não seja processado novametne caso a nota possua varios itens com vários pedidos/itens
            'Caso só exista um ou o CFOP de algum deles não atenda a aos critérios não faz nada
            If objNF IsNot Nothing Then
                'Recupera os CFOPs que não podem ser agrupados, caso algum dos itens tenha esse CFOP não poderão ser agrupados
                objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'CFOPS_EXCECAO_AGRUPAMENTO_ITENS_NF' ")
                If objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED And Not objRetorno.ToString().Contains(x.NF_PROD_CFOP)).Count() > 1 Then
                    'Marcio Spinosa - 25/03/2019 - Problemas de Cte com mais de uma nota - CR00009165
                    'SomaTotalQuantPedidoItem = objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED).Sum(Function(x As modNFItem) x.NF_PROD_QCOM)
                    SomaTotalQuantPedidoItem = objNF.ITENS_NF.Where(Function(x As modNFItem) x.CT_INFNFE_CHAVE = item.CT_INFNFE_CHAVE And x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED).Sum(Function(x As modNFItem) x.NF_PROD_QCOM)
                    'Marcio Spinosa - 25/03/2019 - Problemas de Cte com mais de uma nota - CR00009165 - Fim
                    pLisColectionPOProcessed.Add(Long.Parse(item.NF_PROD_XPED & item.NF_PROD_NITEMPED.ToString()))
                    vBlnMultiplosItensUnicoPedido = True
                End If
            End If


            '----> BUSCA O FATOR DE CONVERSÃO DA UNIDADE DE MEDIDA PARA FAZER A COMPARAÇÃO DE QUANTIDADE COM A MESMA UNIDADE EM QUE FOI CRIADO O PEDIDO
            If Not item.NF_PROD_UCOM Is Nothing Then
                modSQL.CommandText = "select count(*) from TbUM where UM_BASE = '" & item.NF_PROD_UCOM.Replace("'", "") & "' " & " and UM_REFERENCIA = '" & item.SAP_ITEM_DETAILS.UNIT_OF_MEASURE & "'"
                If modSQL.ExecuteScalar(modSQL.CommandText) <> "0" Then
                    modSQL.CommandText = "select isnull(fator, 0) from TbUM where UM_BASE = '" & item.NF_PROD_UCOM.Replace("'", "") & "' " & " and UM_REFERENCIA = '" & item.SAP_ITEM_DETAILS.UNIT_OF_MEASURE & "'"
                    item.VNF_FATOR = modSQL.ExecuteScalar(modSQL.CommandText)
                    If item.VNF_FATOR > 0 Then
                        If vBlnMultiplosItensUnicoPedido Then
                            SomaTotalQuantPedidoItem = SomaTotalQuantPedidoItem * item.VNF_FATOR
                        Else
                            item.NF_PROD_QCOM = item.NF_PROD_QCOM * item.VNF_FATOR
                        End If
                    End If
                End If
            End If

            '----> BUSCA AS MARGENS DE TOLERÂNCIA DA QUANTIDADE
            Dim intDiferencaQtd As Double = 0
            Dim intDiferencaSup As Double = 0
            Dim intDiferencaInf As Double = 0
            If item.SAP_ITEM_DETAILS.UNLIMITED_OVERDELIVERY_ALLOWED <> "X" And item.SAP_ITEM_DETAILS.DELIVERY_COMPLETED <> "X" And item.SAP_ITEM_DETAILS.DELETION_INDICATOR <> "L" Then
                If vBlnMultiplosItensUnicoPedido Then
                    If SomaTotalQuantPedidoItem > vDevOPEN_QUANTITY Then
                        intDiferencaQtd = SomaTotalQuantPedidoItem - vDevOPEN_QUANTITY
                        'Marcio Spinosa - 22/08/2018 - CR00008351
                        'intDiferencaSup = Math.Round((intDiferencaQtd * 100) / vDevOPEN_QUANTITY, 2)
                        intDiferencaSup = Math.Round((intDiferencaQtd * 100) / vDevOPEN_QUANTITY, 3)
                        'Marcio Spinosa - 22/08/2018 - CR00008351 - Fim
                    Else
                        'Apos identificar que se trata de uma nota com mais de um intem onde todos os items pertence ao mesmo pedido/item do SAP, realizar a soma 
                        'de toas as quantidades dos itens para fazer a comparação
                        intDiferencaQtd = vDevOPEN_QUANTITY - SomaTotalQuantPedidoItem
                        'Marcio Spinosa - 22/08/2018 - CR00008351
                        'intDiferencaInf = Math.Round((intDiferencaQtd * 100) / vDevOPEN_QUANTITY, 2)
                        intDiferencaInf = Math.Round((intDiferencaQtd * 100) / vDevOPEN_QUANTITY, 3)
                        'Marcio Spinosa - 22/08/2018 - CR00008351 - Fim
                    End If
                Else
                    If item.NF_PROD_QCOM > vDevOPEN_QUANTITY Then
                        intDiferencaQtd = item.NF_PROD_QCOM - vDevOPEN_QUANTITY
                        'Marcio Spinosa - 22/08/2018 - CR00008351
                        'intDiferencaSup = Math.Round((intDiferencaQtd * 100) / vDevOPEN_QUANTITY, 2)
                        intDiferencaSup = Math.Round((intDiferencaQtd * 100) / vDevOPEN_QUANTITY, 3)
                        'Marcio Spinosa - 22/08/2018 - CR00008351 - Fim
                    Else
                        'Apos identificar que se trata de uma nota com mais de um intem onde todos os items pertence ao mesmo pedido/item do SAP, realizar a soma 
                        'de toas as quantidades dos itens para fazer a comparação
                        intDiferencaQtd = vDevOPEN_QUANTITY - item.NF_PROD_QCOM
                        'Marcio Spinosa - 22/08/2018 - CR00008351
                        'intDiferencaInf = Math.Round((intDiferencaQtd * 100) / vDevOPEN_QUANTITY, 2)
                        intDiferencaInf = Math.Round((intDiferencaQtd * 100) / vDevOPEN_QUANTITY, 3)
                        'Marcio Spinosa - 22/08/2018 - CR00008351 - Fim
                    End If
                End If


            End If

            '----> SE A DIFERENÇA FOR MAIOR QUE A TOLERÂNCIA, CRIA A DIVERGÊNCIA
            If Not objRegras.IsExcecao AndAlso (intDiferencaQtd > 0 And intDiferencaSup > item.SAP_ITEM_DETAILS.OVERDELIVERY_TOLERANCE_LIMIT) Then
                If vBlnMultiplosItensUnicoPedido Then
                    For Each i As modNFItem In objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED)
                        'Marcio Spinosa - 22/08/2018 - CR00008351
                        'InserirDivergencia(i, objRegras.TituloUsuario, SomaTotalQuantPedidoItem.ToString("N2"), vDevOPEN_QUANTITY.ToString("N2"))
                        InserirDivergencia(i, objRegras.TituloUsuario, SomaTotalQuantPedidoItem.ToString("N3"), vDevOPEN_QUANTITY.ToString("N3"))
                        'Marcio Spinosa - 22/08/2018 - CR00008351 - Fim
                    Next
                Else
                    'Marcio Spinosa - 22/08/2018 - CR00008351
                    'InserirDivergencia(item, objRegras.TituloUsuario, item.NF_PROD_QCOM.ToString("N2"), vDevOPEN_QUANTITY.ToString("N2"))
                    InserirDivergencia(item, objRegras.TituloUsuario, item.NF_PROD_QCOM.ToString("N3"), vDevOPEN_QUANTITY.ToString("N3"))
                    'Marcio Spinosa - 22/08/2018 - CR00008351 - Fim
                End If
            Else
                If vBlnMultiplosItensUnicoPedido Then
                    For Each i As modNFItem In objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED)
                        AnularDivergencia(i, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                    Next
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                End If

            End If

            If vBlnMultiplosItensUnicoPedido Then
                For Each i As modNFItem In objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED)
                    'Marcio Spinosa - 22/08/2018 - CR00008351
                    'InserirComparacao(objNF.VNF_CHAVE_ACESSO, i, objRegras.TituloUsuario, SomaTotalQuantPedidoItem.ToString("N2"), i.SAP_ITEM_DETAILS.OPEN_QUANTITY.ToString("N2"), dataComparacao, usuario)
                    InserirComparacao(objNF.VNF_CHAVE_ACESSO, i, objRegras.TituloUsuario, SomaTotalQuantPedidoItem.ToString("N3"), i.SAP_ITEM_DETAILS.OPEN_QUANTITY.ToString("N3"), dataComparacao, usuario)
                Next
            Else
                'InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, item.NF_PROD_QCOM.ToString("N2"), vDevOPEN_QUANTITY.ToString("N2"), dataComparacao, usuario)
                InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, item.NF_PROD_QCOM.ToString("N3"), vDevOPEN_QUANTITY.ToString("N3"), dataComparacao, usuario)
                'Marcio Spinosa - 22/08/2018 - CR00008351 - Fim
            End If

        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de quantidade")
        End Try
    End Sub

    Private Sub ValidarCnpjMetso(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("CNPJ_METSO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim strCnpjNf As String = ""
            Dim strCnpjPo As String = ""

            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) Then
                strCnpjNf = objNF.NF_DEST_CNPJ.ToString().ToCnpj()
            Else
                If (objNF.CT_IDE_TOMA = "0") Then
                    strCnpjNf = objNF.NF_REM_CNPJ.ToString().ToCnpj()
                ElseIf objNF.CT_IDE_TOMA = "1" Then
                    strCnpjNf = objNF.NF_EXPED_CNPJ.ToString().ToCnpj()
                ElseIf objNF.CT_IDE_TOMA = "2" Then
                    strCnpjNf = objNF.NF_DEST_CNPJ.ToString().ToCnpj()
                ElseIf objNF.CT_IDE_TOMA = "3" Then
                    strCnpjNf = objNF.NF_DEST_CNPJ.ToString().ToCnpj()
                ElseIf objNF.CT_IDE_TOMA = "4" Then
                    strCnpjNf = objNF.CT_TOMA_CNPJ.ToString().ToCnpj()
                End If
            End If


            If (Not String.IsNullOrEmpty(item.SAP_ITEM_DETAILS.CNPJ_METSO)) Then
                strCnpjPo = item.SAP_ITEM_DETAILS.CNPJ_METSO.ToString().ToCnpj()
            End If

            If Not objRegras.IsExcecao AndAlso strCnpjNf <> strCnpjPo Then
                InserirDivergencia(item, objRegras.TituloUsuario, strCnpjNf, strCnpjPo)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, strCnpjNf, strCnpjPo, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de CNPJ Neles")
        End Try
    End Sub

    Private Sub ValidarCnpjEmitente(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("CNPJ_EMITENTE", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim strCnpjNf As String = ""
            Dim strCnpjPo As String = ""

            If (Not String.IsNullOrEmpty(objNF.NF_EMIT_CNPJ)) Then
                strCnpjNf = Long.Parse(objNF.NF_EMIT_CNPJ).ToString("00\.000\.000\/0000\-00")
            End If
            'Marcio Spinosa - 19/09/2018 - SR00221755
            'If (Not String.IsNullOrEmpty(objNF.SAP_DETAILS.VENDOR_CNPJ)) Then
            If (Not String.IsNullOrEmpty(item.SAP_ITEM_DETAILS.CNPJ_VENDOR)) Then

                'strCnpjPo = Long.Parse(objNF.SAP_DETAILS.VENDOR_CNPJ).ToString("00\.000\.000\.0000\-00")
                strCnpjPo = Long.Parse(item.SAP_ITEM_DETAILS.CNPJ_VENDOR).ToString("00\.000\.000\/0000\-00")
                'Marcio Spinosa - 19/09/2018 - SR00221755 - Fim
            End If

            'If Not objRegras.IsExcecao AndAlso objNF.NF_EMIT_CNPJ <> objNF.SAP_DETAILS.VENDOR_CNPJ Then
            If Not objRegras.IsExcecao AndAlso strCnpjNf <> strCnpjPo Then
                InserirDivergencia(item, objRegras.TituloUsuario, strCnpjNf, strCnpjPo)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, strCnpjNf, strCnpjPo, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de CNPJ do emitente")
        End Try
    End Sub

    Private Sub ValidarRegimeEspecial(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("REGIME_ESPECIAL", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso (item.NF_PROD_CFOP = "5101" Or item.NF_PROD_CFOP = "5102") Then
                Dim CountNCMRegimeEspecial As Integer = modSQL.ExecuteScalar("SELECT ISNULL(0, COUNT(*)) FROM REGIME_ESPECIAL WHERE CNPJ = " & objNF.NF_EMIT_CNPJ & " AND VERIFICA_NCM = " & item.NF_PROD_NCM)

                '----> VERIFICAR SE EXISTE REGIME ESPECIAL PARA FORNECEDOR E NCM DO ITEM DA NOTA
                If CountNCMRegimeEspecial > 0 And item.NF_ICMS_PICMS > 0 Then
                    InserirDivergencia(item, objRegras.TituloUsuario, "", "")
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                End If
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If
            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, item.NF_ICMS_PICMS, strTextoReprovacao, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de regime especial")
        End Try
    End Sub
    ''' <summary>
    ''' Verifica a condição de pagamento da nota
    ''' </summary>
    ''' <param name="item"></param>
    ''' <param name="dataComparacao"></param>
    ''' <param name="usuario"></param>
    ''' <remarks></remarks>
    ''' <example>Marcio Spinosa - 14/06/2018 - CR00008351 - ajuste na coleta da data de vencimento para não ocorrer exception</example>
    ''' <example>Marcio Spinosa - 14/06/2018 - CR00008351 - ajuste na validação para inserir divergencia quando a data de vencimento do SAP for diferente da NF</example>
    Private Sub ValidarCondicaoPagamento(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("CONDICAO_PAGAMENTO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim datEmissao As Date = Date.Parse(Date.Parse(objNF.NF_IDE_DHEMI).ToShortDateString())
            Dim datVencimento As Date

            Dim intDiasVencimentoNF As Integer = 0
            Dim intDiasVencimentoSAP As Integer = 0

            If (objNF.DUPLICATAS.Count() > 0 AndAlso Not String.IsNullOrEmpty(objNF.DUPLICATAS.FirstOrDefault().NF_COBR_DUP_DVENC)) Then
                datVencimento = objNF.DUPLICATAS.FirstOrDefault().NF_COBR_DUP_DVENC
                intDiasVencimentoNF = DateDiff(DateInterval.Day, datEmissao, datVencimento)
                modSQL.CommandText = "select count(*) from TbCON where CONPAG = '" & objNF.SAP_DETAILS.PAYMENT_TERMS & "'"

                If Not objRegras.IsExcecao AndAlso modSQL.ExecuteScalar(modSQL.CommandText) = "0" Then
                    InserirDivergencia(item, objRegras.TituloUsuario, intDiasVencimentoNF.ToString + " DIAS", "CONDIÇÃO DE PAGAMENTO NÃO CADASTRADA NO VNF")
                Else
                    'Marcio Spinosa - 14/06/2018 - CR00008351
                    Try
                        Dim pStrDias As String
                        pStrDias = modSQL.ExecuteScalar("select ISNULL(NUMDIA,0) from TbCON where CONPAG = '" & objNF.SAP_DETAILS.PAYMENT_TERMS & "'")

                        If Not String.IsNullOrEmpty(pStrDias) And Convert.ToInt32(pStrDias) >= 0 Then
                            intDiasVencimentoSAP = Convert.ToInt32(pStrDias)
                        End If
                    Catch ex As Exception
                        intDiasVencimentoSAP = 1
                    End Try
                    'Marcio Spinosa - 14/06/2018 - CR00008351 - Fim

                    '----> SE A QUANTIDADE DE DIAS DO PEDIDO FOR MAIOR QUE A QUANTIDADE DE DIAS DA NOTA FISCAL, GERA A DIVERGÊNCIA
                    'Marcio Spinosa - 14/06/2018 - CR00008351
                    If Not objRegras.IsExcecao AndAlso objNF.DUPLICATAS.FirstOrDefault().NF_COBR_DUP_DVENC <> New DateTime And Not String.IsNullOrEmpty(objNF.NF_IDE_DHEMI) And intDiasVencimentoSAP > 0 And intDiasVencimentoSAP > intDiasVencimentoNF Then
                        'If Not objRegras.IsExcecao AndAlso objNF.DUPLICATAS.FirstOrDefault().NF_COBR_DUP_DVENC <> New DateTime And Not String.IsNullOrEmpty(objNF.NF_IDE_DHEMI) And intDiasVencimentoSAP > 0 And intDiasVencimentoSAP <> intDiasVencimentoNF Then
                        'Marcio Spinosa - 14/06/2018 - CR00008351 - Fim
                        InserirDivergencia(item, objRegras.TituloUsuario, intDiasVencimentoNF.ToString + " DIAS", intDiasVencimentoSAP.ToString + " DIAS")
                    Else
                        AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                    End If
                End If

                InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, intDiasVencimentoNF.ToString + " DIAS", intDiasVencimentoSAP.ToString + " DIAS", dataComparacao, usuario)
            End If
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de condição de pagamento")
        End Try
    End Sub

    Private Sub ValidarAprovacaoPedido(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("APROVACAO_PEDIDO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso objNF.SAP_PO_HEADER_COLLECTION.Where(Function(X) X.PO_NUMBER = item.SAP_PO_NUMBER).Select(Function(x) x.RELEASE_INDIC).FirstOrDefault() = "X" Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", objNF.SAP_DETAILS.RELEASE_INDIC, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de aprovação de pedido")
        End Try
    End Sub
    'Marcio Spinosa - 18/04/2018 - CR00008351
    ''' <summary>
    ''' Metodo para validar a subcontratação atraves do CFOP
    ''' </summary>
    ''' <param name="item"></param>
    ''' <param name="dataComparacao"></param>
    ''' <param name="usuario"></param>
    ''' <remarks></remarks>
    ''' <example>Marcio Spinosa - 18/04/2018 - CR00008351 - criação do método de validação da subcontratação</example>
    Private Sub ValidarSubContratacao(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("SUBCONTRATACAO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim objCFOP As DataTable = modSQL.Fill("select VALOR from tbpar where parametro  = 'CFOP'")

            'Marcio Spinosa - 20/08/2019 
            'If Not objRegras.IsExcecao And Not objCFOP.ToList().Contains(item.NF_PROD_CFOP) Then
            If Not objRegras.IsExcecao And Not objCFOP.ToList().Contains(item.NF_PROD_CFOP) And (item.VNF_IS_SUBCONTRATACAO) Then
                InserirDivergencia(item, objRegras.TituloUsuario, item.NF_PROD_CFOP, strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If
            'Marcio Spinosa - 20/08/2019 - Fim
            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", objNF.SAP_DETAILS.RELEASE_INDIC, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de Subcontratação")
        End Try
    End Sub
    'Marcio Spinosa - 18/04/2018 - CR00008351 - Fim

    Private Sub ValidarConfirmacaoPedido(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("CONFIRMACAO_PEDIDO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim IsInboundCreated As Boolean = False
            If (Not objRegras.IsExcecao AndAlso item.SAP_ITEM_DETAILS.CONFIRMATION_CONTROL_KEY = "0001") Then
                For Each objInbound As SAP_RFC.PurchaseOrderItemsConfirmations In item.SAP_ITEM_DETAILS.ITEM_CONFIRMATION
                    If (objInbound.CONFIRMATION_CATEGORY = "AB" And Not String.IsNullOrEmpty(objInbound.INBOUND_DELIVERY_NUMBER) And Not String.IsNullOrEmpty(objInbound.INBOUND_DELIVERY_ITEM_NUMBER)) Then
                        IsInboundCreated = True
                        item.VNF_CONFIRMADO_PORTAL = True
                        Exit For
                    End If
                Next

                If Not IsInboundCreated Then
                    InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                End If
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strTextoReprovacao, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de confirmação de pedido")
        End Try
    End Sub

    Private Sub ValidarEntregaAntecipada(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("ENTREGA_ANTECIPADA", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim CompradorIndireto As Boolean = Convert.ToInt32(modSQL.ExecuteScalar("SELECT isnull(count(*), 0) FROM TbPAR WHERE PARAMETRO = 'COMPRADOR_INDIRETO' AND VALOR = '" & objNF.SAP_DETAILS.VENDOR_CODE & "'")) = 0

            Dim objDelivery As SAP_RFC.PurchaseOrderItemsDeliverySchedule
            objDelivery = item.SAP_ITEM_DETAILS.ITEM_DELIVERY_SCHEDULE.FirstOrDefault()

            Dim intAntecipacaoPedido As Integer = DateDiff(DateInterval.Day, DateTime.Today, objDelivery.DELIVERY_DATE)

            Dim vIntDiffMonth As Integer = DateDiff(DateInterval.Month, DateTime.Today, objDelivery.DELIVERY_DATE)

            'Marcio Spinosa - 01/03/2019 - SR00255261 - CR00009165
            'If Not objRegras.IsExcecao AndAlso item.SAP_ITEM_DETAILS.PLANT = "BR01" AndAlso CompradorIndireto AndAlso (intAntecipacaoPedido > 10 And vIntDiffMonth <> 0) Then
            If Not objRegras.IsExcecao AndAlso CompradorIndireto AndAlso (intAntecipacaoPedido > 10 And vIntDiffMonth <> 0) Then
                'Marcio Spinosa - 01/03/2019 - SR00255261 - CR00009165 - Fim
                InserirDivergencia(item, objRegras.TituloUsuario, DateTime.Today, objDelivery.DELIVERY_DATE)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, DateTime.Today, objDelivery.DELIVERY_DATE, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de entrega antecipada")
        End Try
    End Sub

    Public Function IsMandatoryNotaFiscalReferenciada(ByVal cfop As String) As Boolean
        modSQL.CommandText = "SELECT mpc_cfop_codigo FROM TbModoProcessoCfop m INNER JOIN TbPar p ON p.Valor = m.mpc_id_modo_processo AND p.PARAMETRO IN ('ID_MODO_PROCESSO_REMESSA_ENTREGA_FUTURA')"
        Dim dt As DataTable = modSQL.Fill(modSQL.CommandText)

        Dim cfopList As String = String.Empty
        If dt.Rows.Count > 0 Then
            cfopList = dt.AsEnumerable().Select(Function(r) r("mpc_cfop_codigo")).ToList().Aggregate(Function(a, b) a & "|" & b)
        End If
        If Not (cfop = Nothing) Then
            Return cfopList.Contains(cfop)
        Else
            Return Nothing
        End If
    End Function

    Public Function IsFaturaEntregaFutOUConsignacao(ByVal pStrCFOP As String, ByVal pStrParametroTBPAR As String, ByVal pIntFinNFe As Integer) As Boolean
        Dim objRetorno As Object

        '--> Caso seja uma nota compelmentar já retorna falso
        If pIntFinNFe = 2 Then
            Return False
        End If

        objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_CFOP_ENTREGA_FUT_CONSIG '" & pStrParametroTBPAR & "'")
        If Not (pStrCFOP = Nothing) Then
            Return objRetorno.ToString().Contains(pStrCFOP)
        Else
            Return Nothing
        End If
    End Function

    Public Function IsCFOPSubContratacao(ByVal pStrCFOP As String) As Boolean
        Dim objRetorno As Object
        Dim vBlnIsSubContratacao As Boolean = False

        objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'CFOPS_SUBCONTRATACAO' ")

        Return objRetorno.ToString().Contains(pStrCFOP)

    End Function

    Private Sub ValidarNotaFiscalReferenciada(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String, ByVal pStrTipoNota As String)
        '--> VALIDA, CASO CONFIGURADO, SE A NOTA FISCAL REFERENCIADA NO CABEÇALHO DA NOTA NO VNF (VIA XML OU TELA DE ASSOCIAÇÃO) EXISTE NO SAP E SE É DO TIPO FATURA (LI)
        Try
            Dim strResultadoValidacao As String = String.Empty
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("REQUER_NOTA_FISCAL_REFERENCIADA", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If
            If pStrTipoNota = "REMESSA_ENTREGA_FUTURA" Then
                '--> VERIFICA SE A NOTA FISCAL QUE ESTÁ SENDO VALIDADA É DO TIPO REMESSA FUTURA (ÚNICA SITUAÇÃO ONDE É PREVISTO NOTA FISCAL REFERENCIADA ATÉ O MOMENTO)


                Dim strClassificacaoCfop As String = String.Empty
                Dim strNfeReferenciada As String = String.Empty


                '--> SE FOR REMESSA FUTURA VERIFICA A SITUACAO DA NOTA REFERENCIADA
                If (IsMandatoryNotaFiscalReferenciada(item.NF_PROD_CFOP)) Then
                    Dim NF_Numero As String = String.Empty
                    Dim NF_Serie As String = String.Empty
                    Dim NF_Data_Emissao As DateTime = Nothing
                    Dim CNPJ_EMITENTE As String = String.Empty

                    If Not (String.IsNullOrWhiteSpace(objNF.NF_NFREF_REFNNF) Or String.IsNullOrWhiteSpace(objNF.NF_NFREF_REFSerie)) Then
                        '--> CASO O COMPRADOR TENHA INSERIDO A NOTA REFERENCIADA MANUALMENTE NO VNF, VALIDA OS CAMPOS (TEM PRIORIDADE SOBRE O INFORMADO NO XML)

                        NF_Numero = objNF.NF_NFREF_REFNNF
                        NF_Serie = objNF.NF_NFREF_REFSerie
                        NF_Data_Emissao = objNF.NF_NFREF_REFDHEMI
                        CNPJ_EMITENTE = objNF.NF_EMIT_CNPJ
                    Else
                        '--> CASO O COMPRADOR NÃO TENHA INSERIDO A NOTA REFERENCIADA MANUALMENTE NO VNF, VALIDA O VALOR INFORMADO PELO XML

                        modSQL.CommandText = "SELECT isnull(NF_NFREF_REFNFE, '') as NF_NFREF_REFNFE FROM TbDOC_CAB_NFE_REF WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
                        Dim dt = modSQL.Fill(modSQL.CommandText)

                        If dt.Rows.Count = 1 Then
                            strNfeReferenciada = dt.Rows(0)("NF_NFREF_REFNFE")
                            If strNfeReferenciada <> String.Empty Then
                                modSQL.CommandText = "SELECT NF_IDE_NNF, NF_IDE_SERIE, NF_IDE_DHEMI, NF_EMIT_CNPJ FROM tbdoc_cab_nfe WHERE NFEID = '" & strNfeReferenciada & "'"
                                dt = modSQL.Fill(modSQL.CommandText)

                                If dt.Rows.Count = 1 Then
                                    NF_Numero = dt.Rows(0)("NF_IDE_NNF").ToString().ToUpper()
                                    NF_Serie = dt.Rows(0)("NF_IDE_SERIE").ToString().ToUpper()
                                    NF_Data_Emissao = Convert.ToDateTime(dt.Rows(0)("NF_IDE_DHEMI").ToString)
                                    CNPJ_EMITENTE = dt.Rows(0)("NF_EMIT_CNPJ").ToString().ToUpper()
                                Else
                                    strResultadoValidacao = "Nota fiscal referenciada informada no XML não foi encontrada no VNF"
                                End If
                            Else
                                strResultadoValidacao = "Nota fiscal referenciada está vazia no XML"
                            End If
                        Else
                            strResultadoValidacao = "Nota fiscal referenciada não foi informada"
                            If dt.Rows.Count > 1 Then
                                strResultadoValidacao = "Existem múltiplas nota fiscal referenciadas no XML"
                            End If
                        End If
                    End If

                    If (String.IsNullOrWhiteSpace(strResultadoValidacao)) Then
                        '--> SE NÃO OCORREU ALGUM ERRO AO RECUPERAR A NOTA REFERENCIADA (FATURA) NO VNF, BUSCA NO SAP (TIPO LI)
                        Dim objBLNotaFiscal As New BLNotaFiscal()
                        If Not objBLNotaFiscal.MIROExists(objNF.NF_NFREF_REFNNF, objNF.NF_NFREF_REFSerie, objNF.NF_NFREF_REFDHEMI, objNF.NF_EMIT_CNPJ) Then
                            strResultadoValidacao = "Nota fiscal referenciada não foi localizada no SAP" 'não é do tipo Fatura (LI) no SAP"
                        End If
                    End If
                End If

                If Not objRegras.IsExcecao AndAlso strResultadoValidacao <> "" Then
                    InserirDivergencia(item, objRegras.TituloUsuario, "", strResultadoValidacao)
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                End If

                InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strResultadoValidacao, dataComparacao, usuario)
            ElseIf pStrTipoNota = "REMESSA_CONSIGNACAO" Then

                Dim vObjDR As SqlDataReader = Nothing

                modSQL.CommandText = "SELECT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_CONSIGNACAO_REFNF WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' AND ITEM_NF ='" & item.NF_PROD_ITEM & "'"

                vObjDR = modSQL.ExecuteReader(modSQL.CommandText, modSQL.connectionString)

                If Not vObjDR.HasRows Then
                    strResultadoValidacao = "Nota fiscal referenciada não foi informada"
                    InserirDivergencia(item, objRegras.TituloUsuario, "", strResultadoValidacao)
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                    'InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strResultadoValidacao, dataComparacao, usuario)

                End If
                InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strResultadoValidacao, dataComparacao, usuario)
            ElseIf pStrTipoNota = "REMESSA_SUBCONTRATACAO" Then

                Dim vObjDR As SqlDataReader = Nothing

                modSQL.CommandText = "SELECT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_SUBCONTRATACAO_REFNF WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' AND ITEM_NF ='" & item.NF_PROD_ITEM & "'"

                vObjDR = modSQL.ExecuteReader(modSQL.CommandText, modSQL.connectionString)

                If Not vObjDR.HasRows Then
                    strResultadoValidacao = "Nota fiscal referenciada não foi informada"
                    InserirDivergencia(item, objRegras.TituloUsuario, "", strResultadoValidacao)
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                    'InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strResultadoValidacao, dataComparacao, usuario) 'Marcio Spinosa - 14/08/2018 - CR00008351
                End If
                InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strResultadoValidacao, dataComparacao, usuario) 'Marcio Spinosa - 14/08/2018 - CR00008351
            End If


        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de entrega futura")
        End Try
    End Sub

    Private Sub ValidarUFeCodigoDoMunicipioMetso(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("UF_CODIGO_MUNICIPIO_METSO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim valorPED As String = item.SAP_ITEM_DETAILS.STATE_COUNTY_CODE_TAXJUR_METSO
            Dim valorNFE As String = ""
            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) Then
                valorNFE = String.Format("{0} {1}", objNF.NF_DEST_UF, objNF.NF_DEST_CMUN)
            Else
                If (objNF.CT_IDE_TOMA = "0") Then
                    valorNFE = String.Format("{0} {1}", objNF.NF_REM_UF, objNF.NF_REM_CMUN)
                ElseIf objNF.CT_IDE_TOMA = "1" Then
                    valorNFE = String.Format("{0} {1}", objNF.NF_EXPED_UF, objNF.NF_EXPED_CMUN)
                ElseIf objNF.CT_IDE_TOMA = "2" Then
                    valorNFE = String.Format("{0} {1}", objNF.NF_DEST_UF, objNF.NF_DEST_CMUN)
                ElseIf objNF.CT_IDE_TOMA = "3" Then
                    valorNFE = String.Format("{0} {1}", objNF.NF_DEST_UF, objNF.NF_DEST_CMUN)
                ElseIf objNF.CT_IDE_TOMA = "4" Then
                    valorNFE = String.Format("{0} {1}", objNF.CT_TOMA_UF, objNF.CT_TOMA_CMUN)
                End If
            End If

            If Not objRegras.IsExcecao AndAlso (valorNFE <> valorPED) Then
                InserirDivergencia(item, objRegras.TituloUsuario, valorNFE, valorPED)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, valorNFE, valorPED, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de uf e codigo do municipio Neles")
        End Try
    End Sub

    Private Sub ValidarUFeCodigoDoMunicipioFornecedor(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("UF_CODIGO_MUNICIPIO_FORNECEDOR", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim valorNFE As String = String.Format("{0} {1}", objNF.NF_EMIT_UF, objNF.NF_EMIT_CMUN)
            Dim valorPED As String = item.SAP_ITEM_DETAILS.STATE_COUNTY_CODE_TAXJUR_VENDOR

            If Not objRegras.IsExcecao AndAlso (valorNFE <> valorPED) Then
                InserirDivergencia(item, objRegras.TituloUsuario, valorNFE, valorPED)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, valorNFE, valorPED, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de uf e codigo do municipio fornecedor")
        End Try
    End Sub

    Private Sub ValidarDepositoPedido(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("DEPOSITO_PEDIDO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso String.IsNullOrEmpty(item.SAP_ITEM_DETAILS.STORAGE_LOCATION) Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                strTextoReprovacao = item.SAP_ITEM_DETAILS.STORAGE_LOCATION
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", item.SAP_ITEM_DETAILS.STORAGE_LOCATION, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do depósito no pedido")
        End Try
    End Sub

    Private Sub ValidarVersaoPedido(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("VERSAO_PEDIDO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso String.IsNullOrEmpty(objNF.SAP_DETAILS.VERSION_COMPLETE) Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", objNF.SAP_DETAILS.VERSION_COMPLETE, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de versão do pedido")
        End Try
    End Sub

    Private Sub ValidarTaxCode(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("TAX_CODE", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso String.IsNullOrEmpty(item.SAP_ITEM_DETAILS.TAX_CODE) Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", item.SAP_ITEM_DETAILS.TAX_CODE, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de tax code")
        End Try
    End Sub

    Private Sub ValidarInboundDelivery(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String, Optional ByVal objNF As modNF = Nothing, Optional ByRef pLisColectionPOPWithDeliveryrocessed As List(Of Long) = Nothing)
        Dim vBlnMultiplosItensUnicoPedido As Boolean = False
        Dim vDecInboundOPEN_QTY As Decimal
        Dim vDecInboundSAP_QTY As Decimal
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("INBOUND_DELIVERY", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim objRetorno As Object
            If Not objRegras.IsValidar Then
                Return
            End If

            If objNF IsNot Nothing Then
                'Identifica e agrupa todos os itens da nota do mesmo pedido/item do SAP e pesiste dentro em um objeto de controle para que o mesmo
                'pedido item não seja processado novametne caso a nota possua varios itens com vários pedidos/itens
                'Caso só exista um ou o CFOP de algum deles não atenda a aos critérios não faz nada
                'Recupera os CFOPs que não podem ser agrupados, caso algum dos itens tenha esse CFOP não poderão ser agrupados
                objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'CFOPS_EXCECAO_AGRUPAMENTO_ITENS_NF' ")
                If objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED And Not objRetorno.ToString().Contains(x.NF_PROD_CFOP)).Count() > 1 Then
                    pLisColectionPOPWithDeliveryrocessed.Add(Long.Parse(item.NF_PROD_XPED & item.NF_PROD_NITEMPED.ToString()))
                    vBlnMultiplosItensUnicoPedido = True
                End If
            End If


            Dim IsInboundCreated As Boolean = False
            Dim IsEnoughOpenQty As Boolean = False
            Dim strTituloUsuario = objRegras.TituloUsuario
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            'Dim decQtdNotaFiscal As Decimal = IIf(item.NF_PROD_QCOM > 0, item.NF_PROD_QCOM, item.NF_PROD_QTRIB)
            Dim decQtdNotaFiscal As Decimal = item.NF_PROD_QCOM
            Dim strValorPo As String = "0"
            Dim dttNotasInbound As New DataTable
            Dim arrVnfInbound As List(Of modInboundDelivery)
            arrVnfInbound = GetInboundDeliveries(item, dttNotasInbound)

            If arrVnfInbound.Count > 0 Then
                IsInboundCreated = True
            End If

            If vBlnMultiplosItensUnicoPedido Then
                'Marcio Spinosa - 04/04/2019 - SR00267351 - CR00009165
                'decQtdNotaFiscal = objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED).Sum(Function(y As modNFItem) y.NF_PROD_QTRIB).ToString().ToDecimal()
                decQtdNotaFiscal = objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED).Sum(Function(y As modNFItem) y.NF_PROD_QCOM).ToString().ToDecimal()
                'Marcio Spinosa - 04/04/2019 - SR00267351 - CR00009165 - Fim
            Else
                'decQtdNotaFiscal = IIf(item.NF_PROD_QCOM > 0, item.NF_PROD_QCOM, item.NF_PROD_QTRIB)
                decQtdNotaFiscal = item.NF_PROD_QCOM
            End If


            '--> 1. VERIFICA SE A NOTA FISCAL QUE ESTÁ SENDO VALIDADA JÁ ESTÁ DEFINIDA PARA ALGUMA INBOUND
            If dttNotasInbound.Select("nfeid = '" & objNF.VNF_CHAVE_ACESSO & "' and nf_prod_item = " & item.NF_PROD_ITEM).Length > 0 AndAlso
               Not dttNotasInbound.Select("nfeid = '" & objNF.VNF_CHAVE_ACESSO & "' and nf_prod_item = " & item.NF_PROD_ITEM)(0)("vnf_inbound_delivery_number") Is DBNull.Value Then
                Dim dtrNotaFiscalInbound As DataRow = dttNotasInbound.Select("nfeid = '" & objNF.VNF_CHAVE_ACESSO & "' and nf_prod_item = " & item.NF_PROD_ITEM)(0)

                Dim strInboundNf As String = dtrNotaFiscalInbound("vnf_inbound_delivery_number").ToString
                Dim decInboundItemNf As Decimal = dtrNotaFiscalInbound("vnf_inbound_delivery_item_number").ToString.ToDecimal()

                '*********
                'Caso a nota tenha mais de um item para o mesmo pedido, somar a quantidade dos itens 
                Dim decQtyNf As Decimal
                If vBlnMultiplosItensUnicoPedido Then
                    decQtyNf = objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED).Sum(Function(y As modNFItem) y.NF_PROD_QCOM).ToString().ToDecimal()
                Else
                    decQtyNf = dtrNotaFiscalInbound("nf_prod_qcom").ToString.ToDecimal()
                End If


                If Not String.IsNullOrEmpty(strInboundNf) And decInboundItemNf > 0 Then
                    Dim objInbound As modInboundDelivery = arrVnfInbound.Where(Function(x) x.SAP_INBOUND_DELIVERY_NUMBER = strInboundNf And x.SAP_INBOUND_DELIVERY_ITEM_NUMBER = decInboundItemNf).FirstOrDefault()
                    '''''If vBlnMultiplosItensUnicoPedido Then
                    '''''    vDecInboundOPEN_QTY = arrVnfInbound.Where(Function(x) x.SAP_INBOUND_DELIVERY_NUMBER = strInboundNf And x.SAP_INBOUND_DELIVERY_ITEM_NUMBER = decInboundItemNf).Sum(Function(y) y.OPEN_QTY)
                    '''''    vDecInboundSAP_QTY = arrVnfInbound.Where(Function(x) x.SAP_INBOUND_DELIVERY_NUMBER = strInboundNf And x.SAP_INBOUND_DELIVERY_ITEM_NUMBER = decInboundItemNf).Sum(Function(y) y.SAP_QTY)
                    '''''Else
                    '''''    objInbound = arrVnfInbound.Where(Function(x) x.SAP_INBOUND_DELIVERY_NUMBER = strInboundNf And x.SAP_INBOUND_DELIVERY_ITEM_NUMBER = decInboundItemNf).FirstOrDefault()
                    '''''    vDecInboundOPEN_QTY = objInbound.OPEN_QTY
                    '''''    vDecInboundSAP_QTY = objInbound.SAP_QTY
                    '''''End If

                    '---> 1.1 SE O OBJETO INBOUND FOR NULO, SIGNIFICA QUE A INBOUND NÃO EXISTE MAIS
                    If Not objInbound Is Nothing Then
                        vDecInboundOPEN_QTY = objInbound.OPEN_QTY
                        vDecInboundSAP_QTY = objInbound.SAP_QTY

                        '-3 Criar objeto de referencia que persiste o valor já consumido da inbound
                        'objInbound.OPEN_QTY = objInbound.OPEN_QTY - objeto de referencia com o valor consumido
                        '---> OPEN QTY INFERIOR A ZERO, SIGNIFICA QUE A INBOUND FOI ALTERADA NO SAP, REDUZINDO SUA QUANTIDADE
                        strValorPo = vDecInboundOPEN_QTY.ToString("N2")
                        If (vDecInboundOPEN_QTY >= 0 And vDecInboundSAP_QTY >= decQtyNf) Then
                            IsInboundCreated = True
                            IsEnoughOpenQty = True
                            strValorPo = vDecInboundSAP_QTY.ToString("N2")

                            If vBlnMultiplosItensUnicoPedido Then
                                For Each i As modNFItem In objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED)
                                    i.VNF_INBOUND_DELIVERY_ITEM_NUMBER = objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED).FirstOrDefault().VNF_INBOUND_DELIVERY_ITEM_NUMBER
                                    i.VNF_INBOUND_DELIVERY_NUMBER = objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED).FirstOrDefault().VNF_INBOUND_DELIVERY_NUMBER

                                    AnularDivergencia(i, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                                Next
                            Else
                                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                            End If



                        End If
                    End If
                End If
            End If

            'Local para preenchimento da matriz (objeto de referencia)

            '--> 2. SE NÃO TIVER NENHUMA INBOUND ASSOCIADA COM A NOTA FISCAL, BUSCA UMA INBOUND COM SALDO DISPONÍVEL
            If (Not IsInboundCreated Or Not IsEnoughOpenQty) Then
                If (Not objRegras.IsExcecao AndAlso item.SAP_ITEM_DETAILS.CONFIRMATION_CONTROL_KEY = "0001") Then

                    '--> 3. BUSCA AS INBOUNDS PARA ASSOCIAR
                    For Each objInbound As modInboundDelivery In arrVnfInbound
                        If decQtdNotaFiscal <= objInbound.OPEN_QTY Then


                            modSQL.ExecuteNonQuery("UPDATE tbdoc_item SET vnf_inbound_delivery_number = '" & objInbound.SAP_INBOUND_DELIVERY_NUMBER & "', vnf_inbound_delivery_item_number = " & objInbound.SAP_INBOUND_DELIVERY_ITEM_NUMBER & " WHERE nfeid = '" & objNF.VNF_CHAVE_ACESSO & "' ")



                            Dim objInboundNf As New modInboundDeliveryNFs()
                            objInboundNf.VNF_NFEID = objNF.VNF_CHAVE_ACESSO
                            objInboundNf.VNF_QTY = decQtdNotaFiscal

                            objInbound.OPEN_QTY = objInbound.OPEN_QTY - objInboundNf.VNF_QTY
                            objInbound.NOTAS_FISCAIS.Add(objInboundNf)

                            IsEnoughOpenQty = True
                            If vBlnMultiplosItensUnicoPedido Then
                                For Each i As modNFItem In objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED)
                                    i.VNF_INBOUND_DELIVERY_NUMBER = objInbound.SAP_INBOUND_DELIVERY_NUMBER
                                    i.VNF_INBOUND_DELIVERY_ITEM_NUMBER = objInbound.SAP_INBOUND_DELIVERY_ITEM_NUMBER
                                Next

                            Else
                                item.VNF_INBOUND_DELIVERY_NUMBER = objInbound.SAP_INBOUND_DELIVERY_NUMBER
                                item.VNF_INBOUND_DELIVERY_ITEM_NUMBER = objInbound.SAP_INBOUND_DELIVERY_ITEM_NUMBER
                            End If

                            strValorPo = objInbound.SAP_QTY.ToString("N2")
                            Exit For
                        End If
                    Next

                    If Not IsInboundCreated Then
                        If vBlnMultiplosItensUnicoPedido Then
                            For Each i As modNFItem In objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED)
                                InserirDivergencia(i, strTituloUsuario, decQtdNotaFiscal, strTextoReprovacao, "NÃO FOI CRIADO")
                            Next
                        Else
                            InserirDivergencia(item, strTituloUsuario, decQtdNotaFiscal, strTextoReprovacao, "NÃO FOI CRIADO")
                        End If
                    ElseIf Not IsEnoughOpenQty Then
                        If vBlnMultiplosItensUnicoPedido Then
                            For Each i As modNFItem In objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED)
                                InserirDivergencia(i, strTituloUsuario, decQtdNotaFiscal, strValorPo, "SALDO INSUFICIENTE")
                            Next
                        Else
                            InserirDivergencia(item, strTituloUsuario, decQtdNotaFiscal, strValorPo, "SALDO INSUFICIENTE")
                        End If
                    Else
                        If vBlnMultiplosItensUnicoPedido Then
                            For Each i As modNFItem In objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED)
                                AnularDivergencia(i, strTituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                            Next
                        Else
                            AnularDivergencia(item, strTituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                        End If
                    End If
                Else
                    If vBlnMultiplosItensUnicoPedido Then
                        For Each i As modNFItem In objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED)
                            AnularDivergencia(i, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)

                        Next
                    Else
                        AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                    End If
                End If
            End If

            If vBlnMultiplosItensUnicoPedido Then
                For Each i As modNFItem In objNF.ITENS_NF.Where(Function(x As modNFItem) x.NF_PROD_XPED = item.NF_PROD_XPED And x.NF_PROD_NITEMPED = item.NF_PROD_NITEMPED)
                    InserirComparacao(objNF.VNF_CHAVE_ACESSO, i, objRegras.TituloUsuario, decQtdNotaFiscal.ToString("N2"), strValorPo, dataComparacao, usuario)
                Next
            Else
                InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, decQtdNotaFiscal.ToString("N2"), strValorPo, dataComparacao, usuario)
            End If

        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de inbound delivery")
        End Try
    End Sub

    Private Sub ValidarDepositoInboundDelivery(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("DEPOSITO_INBOUND_DELIVERY", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTituloUsuario = objRegras.TituloUsuario
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If (Not objRegras.IsExcecao AndAlso item.SAP_ITEM_DETAILS.CONFIRMATION_CONTROL_KEY = "0001") Then
                For Each objInbound As SAP_RFC.PurchaseOrderItemsConfirmations In item.SAP_ITEM_DETAILS.ITEM_CONFIRMATION
                    If (objInbound.CONFIRMATION_CATEGORY = "LA" And Not String.IsNullOrEmpty(objInbound.INBOUND_DELIVERY_NUMBER) And Not String.IsNullOrEmpty(objInbound.INBOUND_DELIVERY_ITEM_NUMBER)) Then
                        strTituloUsuario = objRegras.TituloUsuario

                        If (String.IsNullOrEmpty(objInbound.STORAGE_LOCATION)) Then
                            InserirDivergencia(item, strTituloUsuario, "", objInbound.STORAGE_LOCATION, "NÃO INFORMADO")
                        ElseIf (objInbound.STORAGE_LOCATION <> item.SAP_ITEM_DETAILS.STORAGE_LOCATION) Then
                            InserirDivergencia(item, strTituloUsuario, "", objInbound.STORAGE_LOCATION, "DIFERENTE DO ITEM DO PEDIDO")
                        Else
                            AnularDivergencia(item, strTituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                            strTextoReprovacao = objInbound.STORAGE_LOCATION
                        End If
                    End If
                Next
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strTextoReprovacao, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do depósito na inbound delivery")
        End Try
    End Sub

    Private Sub ValidarIndicadorRecebimentoMercadoria(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("INDICADOR_RECEBIMENTO_MERCADORIA", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso String.IsNullOrEmpty(item.SAP_ITEM_DETAILS.GOODS_RECEIPT) Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", item.SAP_ITEM_DETAILS.GOODS_RECEIPT, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de indicador de recebimento de mercadoria")
        End Try
    End Sub

    Private Sub ValidarIndicadorRecebimentoFatura(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("INDICADOR_RECEBIMENTO_FATURA", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso String.IsNullOrEmpty(item.SAP_ITEM_DETAILS.INVOICE_RECEIPT) Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", item.SAP_ITEM_DETAILS.INVOICE_RECEIPT, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de indicador de recebimento de fatura")
        End Try
    End Sub

    Private Sub ValidarStatusMaterialGlobal(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("STATUS_MATERIAL_GLOBAL", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim strStatusMaterial = item.SAP_ITEM_DETAILS.CROSS_PLANT_MATERIAL_STATUS
            Dim IsBlocked As Boolean = Convert.ToInt32(modSQL.ExecuteScalar("select count(*) from TbStatusMateriaisBloqueados where smb_descricao = '" & strStatusMaterial & "'").ToString) > 0

            If Not objRegras.IsExcecao AndAlso IsBlocked Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strStatusMaterial)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, Nothing)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strStatusMaterial, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do status do material a nível global")
        End Try
    End Sub

    Private Sub ValidarStatusMaterialPlanta(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("STATUS_MATERIAL_PLANTA", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim strStatusMaterial = item.SAP_ITEM_DETAILS.PLANT_MATERIAL_STATUS
            Dim IsBlocked As Boolean = Convert.ToInt32(modSQL.ExecuteScalar("select count(*) from TbStatusMateriaisBloqueados where smb_descricao = '" & strStatusMaterial & "'").ToString) > 0

            If Not objRegras.IsExcecao AndAlso IsBlocked Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strStatusMaterial)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, Nothing)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strStatusMaterial, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do status do material a nível de planta")
        End Try
    End Sub

    Private Sub ValidarStatusMaterialOrganizacaoVendas(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("STATUS_MATERIAL_ORG_VENDAS", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim strStatusMaterial = item.SAP_ITEM_DETAILS.CROSS_DISTRIBUTION_CHAIN_STATUS
            Dim IsBlocked As Boolean = Convert.ToInt32(modSQL.ExecuteScalar("select count(*) from TbStatusMateriaisBloqueados where smb_descricao = '" & strStatusMaterial & "'").ToString) > 0

            If Not objRegras.IsExcecao AndAlso IsBlocked Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strStatusMaterial)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, Nothing)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strStatusMaterial, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do status do material a nível de organização de vendas")
        End Try
    End Sub

    Private Sub ValidarMaterialDeletadoPlanta(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("MATERIAL_DELETADO_PLANTA", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso item.SAP_ITEM_DETAILS.DELETION_PLANT_LEVEL = "X" Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", item.SAP_ITEM_DETAILS.DELETION_PLANT_LEVEL, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na verificação se o material está deletado a nível de planta")
        End Try
    End Sub

    Private Sub ValidarMaterialDeletadoGlobal(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("MATERIAL_DELETADO_GLOBAL", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso item.SAP_ITEM_DETAILS.DELETION_CLIENT_LEVEL = "X" Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", item.SAP_ITEM_DETAILS.DELETION_CLIENT_LEVEL, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na verificação se o material está deletado a nível global")
        End Try
    End Sub

    Private Sub ValidarMaterialDeletadoDeposito(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("MATERIAL_DELETADO_DEPOSITO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            If Not objRegras.IsExcecao AndAlso item.SAP_ITEM_DETAILS.DELETION_STORAGE_LOCATION = "X" Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strTextoReprovacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", item.SAP_ITEM_DETAILS.DELETION_STORAGE_LOCATION, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na verificação se o material está deletado a nível de depósito")
        End Try
    End Sub

    Private Sub ValidarValorNotaFiscal(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("VALOR_NOTA_FISCAL", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            Dim decValorTotalPo As Decimal = 0
            If Not objRegras.IsValidar Then
                Return
            End If

            '----> SOMA O VALOR DE TODOS OS ITENS DA NOTA FISCAL PARA VERIFICAR SE AS DIVERGÊNCIAS DE VALORES DOS ITENS NÃO INFLUENCIAM NO VALOR FINAL
            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte And objNF.VNF_CLASSIFICACAO = modNF.tipo_cte_frete_pedido) Then
                Dim itemNf As New modNFItem()
                itemNf = objNF.ITENS_NF.FirstOrDefault()
                decValorTotalPo = IIf(itemNf.VNF_ITEM_VALIDO = "X", itemNf.NF_PROD_VPROD + itemNf.NF_IPI_VIPI, itemNf.SAP_ITEM_DETAILS.NF_TOTAL_ITEM)
            Else
                For Each itemNf As modNFItem In objNF.ITENS_NF
                    decValorTotalPo += IIf(itemNf.VNF_ITEM_VALIDO = "X", itemNf.NF_PROD_VPROD + itemNf.NF_IPI_VIPI, itemNf.SAP_ITEM_DETAILS.NF_TOTAL_ITEM)
                Next
            End If

            '----> CALCULA A DIFERENÇA DE VALOR ENTRE A NOTA FISCAL E O PEDIDO
            Dim intDiferencaValor As Double = 0
            If objNF.NF_ICMSTOT_VNF > decValorTotalPo Then
                intDiferencaValor = objNF.NF_ICMSTOT_VNF - decValorTotalPo
            Else
                intDiferencaValor = decValorTotalPo - objNF.NF_ICMSTOT_VNF
            End If

            '----> CALCULA O PERCENTUAL DE DIFERENÇA ENTRE O VALOR NA NOTA FISCAL E O PEDIDO
            Dim intDiferencaPercentual As Double = 0
            If intDiferencaValor > 0 Then
                intDiferencaPercentual = Math.Round((intDiferencaValor * 100) / decValorTotalPo, 2)
            End If

            '----> BUSCA AS TOLERÂNCIAS DE DIFERENÇAS DE VALORES E PERCENTUAL PARA VER SE O ITEM PODE SER ACEITO
            Dim intLimitePercentual As Double = modSQL.ExecuteScalar("SELECT TOP 1 ISNULL(PERCENTUAL, 0) FROM TOLERANCIAVALIDACAO WHERE ValorDe <= " & objNF.NF_ICMSTOT_VNF.ToString.Replace(",", ".") & " AND ValorAte >= " & objNF.NF_ICMSTOT_VNF.ToString.Replace(",", "."))

            If Not objRegras.IsExcecao AndAlso intDiferencaPercentual >= intLimitePercentual Then
                InserirDivergencia(objNF.ITENS_NF(0), objRegras.TituloUsuario, objNF.NF_ICMSTOT_VNF.ToString("C2"), decValorTotalPo.ToString("C2"))
            Else
                AnularDivergencia(objNF.ITENS_NF(0), objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, objNF.ITENS_NF(0), objRegras.TituloUsuario, objNF.NF_ICMSTOT_VNF.ToString("C2"), decValorTotalPo.ToString("C2"), dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do valor da nota fiscal")
        End Try
    End Sub

    Private Sub ValidarValorLiquido(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("VALOR_LIQUIDO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            '---> PARA ITENS COM TAX CODE "F0", SOMAR O VALOR DE ICMS NO VALOR LÍQUIDO
            Dim decValorLiquidoNf As Decimal = item.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE
            If (item.SAP_ITEM_DETAILS.TAX_CODE = "F0") Then
                decValorLiquidoNf += item.NF_ICMS_VICMS
            End If

            '----> CALCULA A DIFERENÇA DE VALOR ENTRE A NOTA FISCAL E O PEDIDO
            Dim intDiferencaValor As Double = 0
            If decValorLiquidoNf > item.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE Then
                intDiferencaValor = decValorLiquidoNf - item.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE
            Else
                intDiferencaValor = item.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE - decValorLiquidoNf
            End If

            '----> CALCULA O PERCENTUAL DE DIFERENÇA ENTRE O VALOR NA NOTA FISCAL E O PEDIDO
            Dim intDiferencaPercentual As Double = 0
            If intDiferencaValor > 0 Then
                intDiferencaPercentual = Math.Round((intDiferencaValor * 100) / item.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE, 2)
            End If

            '----> BUSCA AS TOLERÂNCIAS DE DIFERENÇAS DE VALORES E PERCENTUAL PARA VER SE O ITEM PODE SER ACEITO
            Dim intLimitePercentual As Double = modSQL.ExecuteScalar("SELECT TOP 1 ISNULL(PERCENTUAL, 0) FROM TOLERANCIAVALIDACAO WHERE ValorDe <= " & decValorLiquidoNf.ToString.Replace(",", ".") & " AND ValorAte >= " & decValorLiquidoNf.ToString.Replace(",", "."))

            If Not objRegras.IsExcecao AndAlso intDiferencaPercentual >= intLimitePercentual Then
                InserirDivergencia(item, objRegras.TituloUsuario, decValorLiquidoNf.ToString("C2"), item.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE.ToString("C2"))
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, decValorLiquidoNf.ToString("C2"), item.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE.ToString("C2"), dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do valor líquido")
        End Try
    End Sub

    Private Sub ValidarValorBruto(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("VALOR_BRUTO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            Dim decValorItem As Decimal = item.NF_PROD_VPROD
            If Not objRegras.IsValidar Then
                Return
            End If

            '----> CALCULA A DIFERENÇA DE VALOR ENTRE A NOTA FISCAL E O PEDIDO
            Dim intDiferencaValor As Double = 0
            If decValorItem > item.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES Then
                intDiferencaValor = decValorItem - item.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES
            Else
                intDiferencaValor = item.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES - decValorItem
            End If

            '----> CALCULA O % DE DIFERENÇA ENTRE O VALOR NA NOTA FISCAL E O PEDIDO
            Dim intDiferencaPercentual As Double = 0
            If intDiferencaValor > 0 Then
                intDiferencaPercentual = Math.Round((intDiferencaValor * 100) / item.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES, 2)
            End If

            '----> BUSCA AS TOLERÂNCIAS DE DIFERENÇAS DE VALORES E PERCENTUAL PARA VER SE O ITEM PODE SER ACEITO
            Dim intLimitePercentual As Double = modSQL.ExecuteScalar("SELECT TOP 1 ISNULL(PERCENTUAL, 0) FROM TOLERANCIAVALIDACAO WHERE ValorDe <= " & decValorItem.ToString.Replace(",", ".") & " AND ValorAte >= " & decValorItem.ToString.Replace(",", "."))

            If Not objRegras.IsExcecao AndAlso intDiferencaPercentual >= intLimitePercentual Then

                '----> REFAZ A VERIFICAÇÃO, CONVERTENDO O VALOR DO PEDIDO POR 1000
                Dim intSapValorUnitarioMil As Double = item.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES / 1000

                '----> CALCULA A DIFERENÇA DE VALOR ENTRE A NOTA FISCAL E O PEDIDO
                If decValorItem > intSapValorUnitarioMil Then
                    intDiferencaValor = decValorItem - intSapValorUnitarioMil
                Else
                    intDiferencaValor = intSapValorUnitarioMil - decValorItem
                End If

                '----> CALCULA O % DE DIFERENÇA ENTRE O VALOR NA NOTA FISCAL E O PEDIDO
                intDiferencaPercentual = 0
                If intDiferencaValor > 0 Then
                    intDiferencaPercentual = Math.Round((intDiferencaValor * 100) / intSapValorUnitarioMil, 2)
                End If

                If intDiferencaPercentual >= intLimitePercentual Then
                    InserirDivergencia(item, objRegras.TituloUsuario, decValorItem.ToString("C2"), item.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES.ToString("C2"))
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                End If
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, decValorItem.ToString("C2"), item.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES.ToString("C2"), dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do valor bruto")
        End Try
    End Sub

    Private Sub ValidarValorBrutoComIpi(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("VALOR_BRUTO_IPI", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim decValorBrutoNF As Decimal = (item.NF_PROD_VPROD + item.NF_IPI_VIPI + item.NF_PROD_VFRETE + item.NF_PROD_VOUTRO + item.NF_PROD_VSEG + item.NF_ICMS_VICMSST) - item.NF_PROD_VDESC

            '---> O valor bruto com IPI do SAP deve somar o valor de desconto para representar o valor total do item (e não o valor total da nota fiscal)
            Dim decValorBrutoPO As Decimal = item.SAP_ITEM_DETAILS.NF_TOTAL_ITEM

            '----> CALCULA A DIFERENÇA DE VALOR ENTRE A NOTA FISCAL E O PEDIDO
            Dim intDiferencaValor As Double = 0
            If decValorBrutoNF > decValorBrutoPO Then
                intDiferencaValor = decValorBrutoNF - decValorBrutoPO
            Else
                intDiferencaValor = decValorBrutoPO - decValorBrutoNF
            End If

            '----> CALCULA O PERCENTUAL DE DIFERENÇA ENTRE O VALOR NA NOTA FISCAL E O PEDIDO
            Dim intDiferencaPercentual As Double = 0
            If intDiferencaValor > 0 Then
                intDiferencaPercentual = Math.Round((intDiferencaValor * 100) / decValorBrutoPO, 2)
            End If

            '----> BUSCA AS TOLERÂNCIAS DE DIFERENÇAS DE VALORES E PERCENTUAL PARA VER SE O ITEM PODE SER ACEITO
            Dim intLimitePercentual As Double = modSQL.ExecuteScalar("SELECT TOP 1 ISNULL(PERCENTUAL, 0) FROM TOLERANCIAVALIDACAO WHERE ValorDe <= " & decValorBrutoNF.ToString.Replace(",", ".") & " AND ValorAte >= " & decValorBrutoNF.ToString.Replace(",", "."))

            If Not objRegras.IsExcecao AndAlso intDiferencaPercentual >= intLimitePercentual Then
                InserirDivergencia(item, objRegras.TituloUsuario, decValorBrutoNF.ToString("C2"), decValorBrutoPO.ToString("C2"))
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, decValorBrutoNF.ToString("C2"), decValorBrutoPO.ToString("C2"), dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do valor bruto com IPI")
        End Try
    End Sub

    Private Sub ValidarIcms(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("ICMS", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim objIcms As SAP_RFC.PurchaseOrderItemsTaxes
            For Each objTax As SAP_RFC.PurchaseOrderItemsTaxes In item.SAP_ITEM_DETAILS.ITEM_TAXES
                If (objTax.TAX_NAME.ToUpper() = "ICMS") Then
                    objIcms = objTax
                    Exit For
                End If
            Next

            '----> SE FOR TAX CODE "F0", REMOVER A DIVERGÊNCIA
            If (item.SAP_ITEM_DETAILS.TAX_CODE = "F0") Then
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            Else
                '----> CALCULA A DIFERENÇA DE VALOR ENTRE A NOTA FISCAL E O PEDIDO
                Dim intDiferencaValor As Double = 0
                If item.NF_ICMS_VICMS > objIcms.TAX_AMOUNT Then
                    intDiferencaValor = item.NF_ICMS_VICMS - objIcms.TAX_AMOUNT
                Else
                    intDiferencaValor = objIcms.TAX_AMOUNT - item.NF_ICMS_VICMS
                End If

                '----> CALCULA O PERCENTUAL DE DIFERENÇA ENTRE O VALOR NA NOTA FISCAL E O PEDIDO
                Dim intDiferencaPercentual As Double = 0
                If intDiferencaValor > 0 Then
                    intDiferencaPercentual = Math.Round((intDiferencaValor * 100) / objIcms.TAX_AMOUNT, 2)
                End If

                Dim intLimitePercentual As Double = modSQL.ExecuteScalar("SELECT TOP 1 ISNULL(PERCENTUAL, 0) FROM TOLERANCIAVALIDACAO WHERE ValorDe <= " & item.NF_ICMS_VICMS.ToString.Replace(",", ".") & " AND ValorAte >= " & item.NF_ICMS_VICMS.ToString.Replace(",", "."))


                If Not objRegras.IsExcecao AndAlso intDiferencaPercentual >= intLimitePercentual Then
                    InserirDivergencia(item, objRegras.TituloUsuario, item.NF_ICMS_VICMS.ToString("C2"), objIcms.TAX_AMOUNT.ToString("C2"))
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                End If
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, item.NF_ICMS_VICMS.ToString("C2"), objIcms.TAX_AMOUNT.ToString("C2"), dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do valor de ICMS")
        End Try
    End Sub

    Private Sub ValidarIEMetso(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("IE_METSO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim strIENf As String = ""
            Dim strIEPo As String = ""

            If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) Then
                strIENf = objNF.NF_DEST_IE
            Else
                If (objNF.CT_IDE_TOMA = "0") Then
                    strIENf = objNF.NF_REM_IE
                ElseIf objNF.CT_IDE_TOMA = "1" Then
                    strIENf = objNF.NF_EXPED_IE
                ElseIf objNF.CT_IDE_TOMA = "2" Then
                    strIENf = objNF.NF_DEST_IE
                ElseIf objNF.CT_IDE_TOMA = "3" Then
                    strIENf = objNF.NF_DEST_IE
                ElseIf objNF.CT_IDE_TOMA = "4" Then
                    strIENf = objNF.CT_TOMA_IE
                End If
            End If


            If (Not String.IsNullOrEmpty(item.SAP_ITEM_DETAILS.IE_METSO)) Then
                strIEPo = item.SAP_ITEM_DETAILS.IE_METSO
            End If

            If Not objRegras.IsExcecao AndAlso strIENf <> strIEPo Then
                InserirDivergencia(item, objRegras.TituloUsuario, strIENf, strIEPo)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, strIENf, strIEPo, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação da IE Neles")
        End Try
    End Sub
    Private Sub ValidarNFdeRemessaConsignacaoCancelada(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)

        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("REMESSA_CANCELADA", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim vObjDR As SqlDataReader = Nothing

            modSQL.CommandText = "SELECT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_CONSIGNACAO_REFNF WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' AND ITEM_NF ='" & item.NF_PROD_ITEM & "'"

            vObjDR = modSQL.ExecuteReader(modSQL.CommandText, modSQL.connectionString)

            If vObjDR.HasRows Then
                While vObjDR.Read()
                    'Verifica se existe uma nota com a categoria R1 sem o flag de cancelar e também
                    'se existe algum outro lancamento de nota como NF Type diferente de R1, A1 ou X2
                    If (Not objRegras.IsExcecao And
                           (item.SAP_ITEM_DETAILS.ITEM_DELIVERYNOTE.Where(Function(x) x.NFTYPE = "R1" And String.IsNullOrWhiteSpace(x.CANCEL) And (x.NFNUM = Integer.Parse(vObjDR.Item("NUMERO_REFNF").ToString) Or x.NFENUM = Integer.Parse(vObjDR.Item("NUMERO_REFNF").ToString)) And x.SERIES = Integer.Parse(vObjDR.Item("SERIE_REFNF").ToString)).Count = 0 _
                        Or item.SAP_ITEM_DETAILS.ITEM_DELIVERYNOTE.Where(Function(x) (x.NFNUM = Integer.Parse(vObjDR.Item("NUMERO_REFNF").ToString) Or x.NFENUM = Integer.Parse(vObjDR.Item("NUMERO_REFNF").ToString)) And x.SERIES = Integer.Parse(vObjDR.Item("SERIE_REFNF").ToString) And (x.NFTYPE <> "R1" And x.NFTYPE <> "A1" And x.NFTYPE <> "X2")).Count() > 0)) Then
                        InserirDivergencia(item, objRegras.TituloUsuario, vObjDR.Item("NUMERO_REFNF").ToString & "-" & vObjDR.Item("NUMERO_REFNF").ToString, "X")
                    Else
                        AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                    End If
                    InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, vObjDR.Item("NUMERO_REFNF").ToString & "-" & vObjDR.Item("NUMERO_REFNF").ToString, "X", dataComparacao, usuario)
                End While
            End If


        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação consulta de remessa de consignação cancelada.")
        End Try
    End Sub
    Private Sub ValidarSaldodaRemessadaSubcontratacao(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)

        Dim vDecSaldo541 As Decimal
        Dim vDecSaldo542 As Decimal
        Dim vDecSaldo543 As Decimal
        Dim vDecSaldo544 As Decimal
        Dim vDecDiferenca As Decimal

        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("SALDO_REMESSA_SUBCONTRATACAO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim vObjDR As SqlDataReader = Nothing

            modSQL.CommandText = "SELECT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_SUBCONTRATACAO_REFNF WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' AND ITEM_NF ='" & item.NF_PROD_ITEM & "'"

            vObjDR = modSQL.ExecuteReader(modSQL.CommandText, modSQL.connectionString)

            If vObjDR.HasRows Then
                While vObjDR.Read()
                    If Not (item.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST Is Nothing) Then 'Marcio Spinosa - 16/08/2018 - CR00008351
                        'Verifica se o saldo das remessas é o suficiente para o recebimento 
                        vDecSaldo541 = item.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST.Where(Function(x) x.MOVEMENT_TYPE_BWART = "541").Sum(Function(x) x.QUANTITY_MENGE)
                        vDecSaldo542 = item.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST.Where(Function(x) x.MOVEMENT_TYPE_BWART = "542").Sum(Function(x) x.QUANTITY_MENGE)
                        vDecSaldo543 = item.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST.Where(Function(x) x.MOVEMENT_TYPE_BWART = "543").Sum(Function(x) x.QUANTITY_MENGE)
                        vDecSaldo544 = item.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST.Where(Function(x) x.MOVEMENT_TYPE_BWART = "544").Sum(Function(x) x.QUANTITY_MENGE)
                        vDecDiferenca += (vDecSaldo544 + vDecSaldo541) - (vDecSaldo542 + vDecSaldo543)
                    End If
                End While
            End If
            If Not (item.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST Is Nothing) Then 'Marcio Spinosa - 16/08/2018 - CR00008351
                If Not objRegras.IsExcecao _
                        AndAlso item.NF_PROD_QCOM > vDecDiferenca Then
                    InserirDivergencia(item, objRegras.TituloUsuario, item.NF_PROD_QCOM, vDecDiferenca)
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                End If
                InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, item.NF_PROD_QCOM, vDecDiferenca, dataComparacao, usuario)
            End If
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação consulta de remessa de consignação cancelada.")
        End Try
    End Sub
    Private Sub ValidarSaldodaRemessadaConsignacao(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)

        Dim vDecSaldoRemessa As Decimal
        Dim vDecSaldoFatura As Decimal
        Dim vDecDiferenca As Decimal
        Dim vDecMvt821 As Decimal
        Dim vDecMvt822 As Decimal

        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("SALDO_REMESSA_CONSIGNACAO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim vObjDR As SqlDataReader = Nothing

            modSQL.CommandText = "SELECT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_CONSIGNACAO_REFNF WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' AND ITEM_NF ='" & item.NF_PROD_ITEM & "'"

            vObjDR = modSQL.ExecuteReader(modSQL.CommandText, modSQL.connectionString)

            If vObjDR.HasRows Then
                While vObjDR.Read()
                    'Verifica se o saldo das remessas é o suficiente para o recebimento da fatura
                    vDecSaldoRemessa = item.SAP_ITEM_DETAILS.ITEM_DELIVERYNOTE_J_1BNFLIN.Where(Function(x) x.NF_REMESSA.TrimStart("0"c) = (vObjDR.Item("NUMERO_REFNF").ToString.TrimStart("0"c) & "-" & vObjDR.Item("SERIE_REFNF").ToString.TrimStart("0"c)) And x.TAX_CODE = "K5").Sum(Function(x) x.MENGE)
                    vDecSaldoFatura = item.SAP_ITEM_DETAILS.ITEM_DELIVERYNOTE_J_1BNFLIN.Where(Function(x) x.NF_REMESSA.TrimStart("0"c) = (vObjDR.Item("NUMERO_REFNF").ToString.TrimStart("0"c) & "-" & vObjDR.Item("SERIE_REFNF").ToString.TrimStart("0"c)) And x.TAX_CODE = "K6").Sum(Function(x) x.MENGE)
                    vDecDiferenca += vDecSaldoRemessa - vDecSaldoFatura

                    'Caso não exista fatura(k6) para a remessa pegar a quantidade total da remessa (mseg) para fazer o calculo
                    If vDecSaldoFatura = 0 Then
                        vDecMvt821 = item.SAP_ITEM_DETAILS.ITEM_MSEG.Where(Function(x) x.MOVIMENTO_BWART = "821" And x.NF_REMESSA_XBLNR_MKPF.TrimStart("0"c) = (vObjDR.Item("NUMERO_REFNF").ToString.TrimStart("0"c) & "-" & vObjDR.Item("SERIE_REFNF").ToString.TrimStart("0"c))).Sum(Function(x) x.QTD_REMESSA_MSEG_ERFMG)
                        vDecMvt822 = item.SAP_ITEM_DETAILS.ITEM_MSEG.Where(Function(x) x.MOVIMENTO_BWART = "822" And x.NF_REMESSA_XBLNR_MKPF.TrimStart("0"c) = (vObjDR.Item("NUMERO_REFNF").ToString.TrimStart("0"c) & "-" & vObjDR.Item("SERIE_REFNF").ToString.TrimStart("0"c))).Sum(Function(x) x.QTD_REMESSA_MSEG_ERFMG)
                        vDecDiferenca += vDecMvt821 - vDecMvt822
                    End If

                End While

                If Not objRegras.IsExcecao _
                        AndAlso item.NF_PROD_QCOM > vDecDiferenca Then
                    InserirDivergencia(item, objRegras.TituloUsuario, item.NF_PROD_QCOM, vDecDiferenca)
                Else
                    AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                End If
                InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, item.NF_PROD_QCOM, vDecDiferenca, dataComparacao, usuario)

            End If


        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de saldo da remessa de consignação.")
        End Try
    End Sub
    Private Sub ValidarMaterialemOrdemdeProducao(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)

        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("MATERIAL_ORDEM_PRODUCAO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            'Verificando se o material recebido não está vinculado a uma ordem de produção conforme regra abaixo
            'Se o tipo de ordem for igal a F e seu número iniciar com 1 e a utilização do material for igual a 2 ou 3 
            'então está errado o uso do material

            If Not objRegras.IsExcecao _
               AndAlso item.SAP_ITEM_DETAILS.ACCOUNT_ASSIGNMENT_CATEGORY = "F" _
               AndAlso (item.SAP_ITEM_DETAILS.USAGE_MATERIAL = "2" Or item.SAP_ITEM_DETAILS.USAGE_MATERIAL = "3") _
               AndAlso item.SAP_ITEM_DETAILS.ORDEM_DE_COMPRA_AUFNR.TrimStart("0").Substring(0, 1) = "1" Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", "CAT. F, USO " & item.SAP_ITEM_DETAILS.USAGE_MATERIAL)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If
            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", "CAT. F, USO " & item.SAP_ITEM_DETAILS.USAGE_MATERIAL, dataComparacao, usuario)


        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação de uso do material.")
        End Try
    End Sub

    Private Sub ValidarTipoDeMaterialComoServico(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)

        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("TIPO_MATERIAL_SERVICO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            'Verificando se o material do pedido ná é um serviço

            If Not objRegras.IsExcecao _
               AndAlso item.SAP_ITEM_DETAILS.MATERIAL_TYPE_MTART = "YDIE" Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", "YDIE")
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If
            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", "YDIE", dataComparacao, usuario)


        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do tipo de material.")
        End Try
    End Sub

    'Marcio Spinosa - 15/06/2018 - CR00008351
    ''' <summary>
    ''' Método para validar o XML se possui duplicata conforme necessário
    ''' </summary>
    ''' <param name="item"></param>
    ''' <param name="dataComparacao"></param>
    ''' <param name="usuario"></param>
    ''' <remarks></remarks>
    ''' <example>Marcio Spinosa - 15/06/2018 - CR00008351 - criado o metodo de validação de duplicata</example>
    Private Sub ValidarDuplicata(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            'Marcio Spinosa - 12/09/2018 - CR00009259
            'Dim objRegras As modRegrasValidacao = GetRegrasValidacao("POSSUI_DUPLICATA", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("POSSUI_DUPLICATA", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            'Marcio Spinosa - 12/09/2018 - CR00009259 - Fim
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            'Verificando se o material do pedido ná é um serviço

            If Not objRegras.IsExcecao _
               AndAlso objNF.DUPLICATAS.Count() = 0 Then
                InserirDivergencia(item, objRegras.TituloUsuario, "XML não possui duplicatas", "")
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If
            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "XML/DUPLICATAS", "", dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação da duplicata.")
        End Try
    End Sub
    'Marcio Spinosa - 15/06/2018 - CR00008351 - Fim

    Private Sub ValidarIpi(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("IPI", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim decValorIpiPo As Decimal
            Dim objIpi As SAP_RFC.PurchaseOrderItemsTaxes
            For Each objTax As SAP_RFC.PurchaseOrderItemsTaxes In item.SAP_ITEM_DETAILS.ITEM_TAXES
                If (objTax.TAX_NAME.ToUpper() = "IPI") Then
                    objIpi = objTax
                    Exit For
                End If
            Next

            Dim strTaxSplitCode As String = modSQL.ExecuteScalar("SELECT VALOR FROM TbPAR WHERE PARAMETRO = 'TAX_SPLIT_CODE'").ToString()
            Dim objIpiTaxSplit As New SAP_RFC.PurchaseOrderItemsTaxes
            For Each objTax As SAP_RFC.PurchaseOrderItemsTaxes In item.SAP_ITEM_DETAILS.ITEM_TAXES
                If (objTax.TAX_NAME.ToUpper() = strTaxSplitCode) Then
                    objIpiTaxSplit = objTax
                    Exit For
                End If
            Next

            decValorIpiPo = IIf(objIpiTaxSplit.TAX_NAME = String.Empty, objIpi.TAX_AMOUNT, objIpi.TAX_AMOUNT - objIpiTaxSplit.TAX_AMOUNT)
            If (item.SAP_ITEM_DETAILS.TAX_SPLIT = "X") Then
                decValorIpiPo = 0
            End If

            Dim intDiferencaValor As Double = 0
            If item.NF_IPI_VIPI > decValorIpiPo Then
                intDiferencaValor = item.NF_IPI_VIPI - decValorIpiPo
            Else
                intDiferencaValor = decValorIpiPo - item.NF_IPI_VIPI
            End If

            '----> CALCULA O PERCENTUAL DE DIFERENÇA ENTRE O VALOR NA NOTA FISCAL E O PEDIDO
            Dim intDiferencaPercentual As Double = 0
            If intDiferencaValor > 0 Then
                intDiferencaPercentual = Math.Round((intDiferencaValor * 100) / decValorIpiPo, 2)
            End If

            Dim intLimitePercentual As Double = modSQL.ExecuteScalar("SELECT TOP 1 ISNULL(PERCENTUAL, 0) FROM TOLERANCIAVALIDACAO WHERE ValorDe <= " & item.NF_IPI_VIPI.ToString.Replace(",", ".") & " AND ValorAte >= " & item.NF_IPI_VIPI.ToString.Replace(",", "."))

            If Not objRegras.IsExcecao AndAlso intDiferencaPercentual >= intLimitePercentual Then
                InserirDivergencia(item, objRegras.TituloUsuario, item.NF_IPI_VIPI.ToString("C2"), objIpi.TAX_AMOUNT.ToString("C2"))
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, item.NF_IPI_VIPI.ToString("C2"), objIpi.TAX_AMOUNT.ToString("C2"), dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do valor de IPI")
        End Try
    End Sub

    Private Sub ValidarIEEmitente(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("IE_EMITENTE", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim strIENf As String = ""
            Dim strIEPo As String = ""

            If (Not String.IsNullOrEmpty(objNF.NF_EMIT_CNPJ)) Then
                strIENf = objNF.NF_EMIT_IE
            End If

            If (Not String.IsNullOrEmpty(objNF.SAP_DETAILS.VENDOR_CNPJ)) Then
                strIEPo = objNF.SAP_DETAILS.VENDOR_IE
            End If

            If Not objRegras.IsExcecao AndAlso objNF.NF_EMIT_IE <> objNF.SAP_DETAILS.VENDOR_IE Then
                InserirDivergencia(item, objRegras.TituloUsuario, strIENf, strIEPo)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, strIENf, strIEPo, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação da IE do emitente")
        End Try
    End Sub

    Public Sub ValidarIndicadorFaturaBaseadaEmEntradaDeMercadorias(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("FATURA_BASEADA_ENTRADA_MERCADORIAS", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim strResultadoValidacao As String = String.Empty

            '--> SE O INDICADOR GOODS_RECEIPT_BASED_INVOICE ESTIVER MARCADO NO PEDIDO NO SAP
            If (item.SAP_ITEM_DETAILS.GOODS_RECEIPT_BASED_INVOICE_WEBRE) Then
                modSQL.CommandText = "SELECT mpc_cfop_codigo FROM TbModoProcessoCfop m INNER JOIN TbPar p ON p.Valor = m.mpc_id_modo_processo AND p.PARAMETRO IN ('ID_MODO_PROCESSO_FATURA_ENTREGA_FUTURA', 'ID_MODO_PROCESSO_REMESSA_ENTREGA_FUTURA')"
                Dim dt As DataTable = modSQL.Fill(modSQL.CommandText)

                Dim cfopList As String = String.Empty
                If dt.Rows.Count > 0 Then
                    cfopList = dt.AsEnumerable().Select(Function(r) r("mpc_cfop_codigo")).ToList().Aggregate(Function(a, b) a & "|" & b)
                End If

                '--> E SE FOR FATURA OU REMESSA DA ENTREGA FUTURA, GERA EXCEÇÃO
                If (cfopList.Contains(item.NF_PROD_CFOP)) Then
                    strResultadoValidacao = objRegras.TextoReprovacao
                End If
            End If

            If Not objRegras.IsExcecao AndAlso strResultadoValidacao <> "" Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strResultadoValidacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strResultadoValidacao, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do indicador de fatura baseada em entrada de mercadorias")
        End Try
    End Sub

    Public Sub ValidarCodigoDeMaterialNaOrdemDeCompra(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)
        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("CODIGO_MATERIAL_PEDIDO", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.PLANT & item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim strResultadoValidacao As String = String.Empty

            ' 
            If ((objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_talonario) AndAlso
                    (String.IsNullOrWhiteSpace(item.SAP_ITEM_DETAILS.MATERIAL))) Then

                strResultadoValidacao = objRegras.TextoReprovacao
            End If
            ' 

            If Not objRegras.IsExcecao AndAlso strResultadoValidacao <> "" Then
                InserirDivergencia(item, objRegras.TituloUsuario, "", strResultadoValidacao)
            Else
                AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
            End If

            InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, "", strResultadoValidacao, dataComparacao, usuario)
        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação do código de material na ordem de compra")
        End Try
    End Sub

    Private Sub ValidarNFdeRemessaSubcontratacaoCancelada(ByVal item As modNFItem, ByVal dataComparacao As DateTime, ByVal usuario As String)

        Try
            Dim objRegras As modRegrasValidacao = GetRegrasValidacao("REMESSA_SUBCONTRATACAO_CANCELADA", item.NF_PROD_CFOP, item.SAP_ITEM_DETAILS.STORAGE_LOCATION, item.SAP_ITEM_DETAILS.MATERIAL)
            Dim strTextoReprovacao = objRegras.TextoReprovacao
            If Not objRegras.IsValidar Then
                Return
            End If

            Dim vObjDR As SqlDataReader = Nothing

            modSQL.CommandText = "SELECT NUMERO_REFNF, SERIE_REFNF FROM TBDOC_SUBCONTRATACAO_REFNF WHERE NFEID = '" & objNF.VNF_CHAVE_ACESSO & "' AND ITEM_NF ='" & item.NF_PROD_ITEM & "'"

            vObjDR = modSQL.ExecuteReader(modSQL.CommandText, modSQL.connectionString)

            If vObjDR.HasRows Then
                While vObjDR.Read()
                    'Verifica se existe uma nota com a categoria G1 sem o flag de cancelar e também
                    'se existe algum outro lancamento de nota como NF Type diferente de  A1 ou G4
                    If Not (item.SAP_ITEM_DETAILS.ITEM_DELIVERYNOTE Is Nothing) Then 'Marcio Spinosa - 16/08/2018 - CR00008351
                        If (Not objRegras.IsExcecao And (
                              item.SAP_ITEM_DETAILS.ITEM_DELIVERYNOTE.Where(Function(x) x.NFTYPE = "G1" And String.IsNullOrWhiteSpace(x.CANCEL) And (x.NFNUM = Integer.Parse(vObjDR.Item("NUMERO_REFNF").ToString) Or x.NFENUM = Integer.Parse(vObjDR.Item("NUMERO_REFNF").ToString)) And x.SERIES = Integer.Parse(vObjDR.Item("SERIE_REFNF").ToString)).Count = 0 _
                            Or item.SAP_ITEM_DETAILS.ITEM_DELIVERYNOTE.Where(Function(x) (x.NFNUM = Integer.Parse(vObjDR.Item("NUMERO_REFNF").ToString) Or x.NFENUM = Integer.Parse(vObjDR.Item("NUMERO_REFNF").ToString)) And x.SERIES = Integer.Parse(vObjDR.Item("SERIE_REFNF").ToString) And (x.NFTYPE <> "G1" And x.NFTYPE <> "A1" And x.NFTYPE <> "G4")).Count() > 0)) Then
                            InserirDivergencia(item, objRegras.TituloUsuario, vObjDR.Item("NUMERO_REFNF").ToString & "-" & vObjDR.Item("NUMERO_REFNF").ToString, "X")
                        Else
                            AnularDivergencia(item, objRegras.TituloUsuario, objRegras.IsExcecao, strTextoReprovacao)
                        End If
                        InserirComparacao(objNF.VNF_CHAVE_ACESSO, item, objRegras.TituloUsuario, vObjDR.Item("NUMERO_REFNF").ToString & "-" & vObjDR.Item("NUMERO_REFNF").ToString, "X", dataComparacao, usuario)
                    End If
                End While
            End If


        Catch ex As Exception
            Throw New Exception("Ocorreu um erro na validação consulta de remessa de consignação cancelada.")
        End Try
    End Sub

    Public Sub EnviarMsgCancelamento(ByVal NFeId As String)
        Dim vObjDR As SqlDataReader = Nothing
        Dim vStrNumeroNF As String = String.Empty
        Dim vStrSerieNF As String = String.Empty
        Dim vStrNomeEmitente As String = String.Empty
        Dim vStrCNPJEmitente As String = String.Empty
        Dim vStrDtEmissao As String = String.Empty
        Dim vStrCNPJDestinatario As String = String.Empty
        Dim objRetorno As Object
        Dim vStrModDoc As String = String.Empty
        Dim vStrMsgSubject As String
        Dim vStrMSgBody As String = String.Empty
        Dim vStrMensagemLog As String = String.Empty

        Try

            'Executa comando para recuperar dados de cabeçalho da NF-e
            modSQL.CommandText = "SELECT NF_IDE_NNF, NF_IDE_SERIE, NF_EMIT_XNOME, NF_EMIT_CNPJ, NF_IDE_DHEMI, NF_DEST_CNPJ, NF_IDE_MOD FROM TbDOC_CAB_NFE WHERE NFEID = '" & NFeId & "'"

            vObjDR = modSQL.ExecuteReader(modSQL.CommandText, modSQL.connectionString)

            If vObjDR.HasRows Then
                While vObjDR.Read()
                    vStrNumeroNF = vObjDR.Item("NF_IDE_NNF").ToString()
                    vStrSerieNF = vObjDR.Item("NF_IDE_SERIE").ToString()
                    vStrNomeEmitente = vObjDR.Item("NF_EMIT_XNOME").ToString()
                    vStrCNPJEmitente = Convert.ToUInt64(vObjDR.Item("NF_EMIT_CNPJ")).ToString("00\.000\.000\.0000\-00")
                    vStrDtEmissao = Format(DateTime.Parse(vObjDR.Item("NF_IDE_DHEMI")), "dd/MM/yyyy")
                    vStrCNPJDestinatario = Convert.ToUInt64(vObjDR.Item("NF_DEST_CNPJ")).ToString("00\.000\.000\.0000\-00")
                    vStrModDoc = vObjDR.Item("NF_IDE_MOD").ToString()
                End While

                'Recupera parametro que contem os endereços de email para mensagem ser enviada
                objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'EMAIL_CANCELAMENTO' ")
                SEND_TO = objRetorno.ToString()

                ''Cria mensagem customizada para NF-e ou CT-e
                If vStrModDoc = "55" Then
                    vStrMsgSubject = "Cancelamento de NF-e já integrada, número " & vStrNumeroNF & " - SÉRIE " & vStrSerieNF
                    vStrMSgBody = "<p style='font-family:verdana; font-size:10pt; line-height:150%;'>" & Chr(13) &
                                     "<b>Atenção,</b> <br/>" & Chr(13) &
                                     "a NF-e de número " & vStrNumeroNF & ", série " & vStrSerieNF & " emitida por " & vStrNomeEmitente & ", CNPJ " & vStrCNPJEmitente & " na data de emissão " & vStrDtEmissao & " <br/> " & Chr(13) & Chr(13) &
                                     "para a filial Neles de CNPJ " & vStrCNPJDestinatario & ", foi cancelada e encontra-se com o status de integração <b>CONCLUÍDA</b> no VNF junto ao SAP." & Chr(13) & Chr(13) &
                                 "</p>"
                    vStrMensagemLog = "Atenção,a NF-e de número " & vStrNumeroNF & ", série " & vStrSerieNF & " emitida por " & vStrNomeEmitente & ", CNPJ " & vStrCNPJEmitente & "para a filial Neles de CNPJ " & vStrCNPJDestinatario & ", foi cancelada e encontra-se com o status de integração concluída no VNF junto ao SAP"
                Else
                    vStrMsgSubject = "Cancelamento de CT-e já integrado, número " & vStrNumeroNF & " - SÉRIE " & vStrSerieNF
                    vStrMSgBody = "<p style='font-family:verdana; font-size:10pt; line-height:150%;'>" & Chr(13) &
                                     "<b>Atenção,</b> <br/>" & Chr(13) &
                                     "o CT-e de número " & vStrNumeroNF & ", série " & vStrSerieNF & " emitido por " & vStrNomeEmitente & ", CNPJ " & vStrCNPJEmitente & " na data de emissão " & vStrDtEmissao & " <br/> " & Chr(13) & Chr(13) &
                                     "para a filial Neles de CNPJ " & vStrCNPJDestinatario & ", foi cancelado e encontra-se com o status de integração <b>CONCLUÍDA</b> no VNF junto ao SAP." & Chr(13) & Chr(13) &
                                 "</p>"
                    vStrMensagemLog = "Atenção,o CT-e de número " & vStrNumeroNF & ", série " & vStrSerieNF & " emitido por " & vStrNomeEmitente & ", CNPJ " & vStrCNPJEmitente & "para a filial Neles de CNPJ " & vStrCNPJDestinatario & ", foi cancelado e encontra-se com o status de integração concluída no VNF junto ao SAP"
                End If

                EnviarMensagem(SEND_TO, vStrMsgSubject, vStrMSgBody)

                'Grava log na tabela de mensagens

                modSQL.ExecuteNonQuery("insert into TbMEN values ('" & NFeId & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '" & SEND_TO & "', 'CANCELADA', '" & vStrMensagemLog & "', '')")

            End If

        Catch ex As Exception
            Throw ex
        Finally
            If vObjDR IsNot Nothing Then
                vObjDR.Close()
                vObjDR = Nothing
            End If
        End Try
    End Sub

#End Region

#Region " SQL Instructions "

#Region " TbNFE "
    Private strInsertTbNFE As String = "INSERT INTO TbNFE " &
                                       "( " &
                                       "	 NFEID " &
                                       "	,NFEVAL " &
                                       "	,NFECAN " &
                                       "	,ID_LISTA " &
                                       "	,DATVAL " &
                                       "	,NFEREL " &
                                       "	,USUCAN " &
                                       "	,JUNAUT " &
                                       "	,SITUACAO " &
                                       "	,REPROCESSAR " &
                                       "	,CNPJ_METSO " &
                                       "	,CONTINGENCIA " &
                                       "    ,SAP_STATUS_INTEGRACAO " &
                                       ") " &
                                       "VALUES " &
                                       "( " &
                                       "	 @NFEID " &
                                       "	,@NFEVAL " &
                                       "	,@NFECAN " &
                                       "	,@ID_LISTA " &
                                       "	,@DATVAL " &
                                       "	,@NFEREL " &
                                       "	,@USUCAN " &
                                       "	,@JUNAUT " &
                                       "	,@SITUACAO " &
                                       "	,@REPROCESSAR " &
                                       "	,@CNPJ_METSO " &
                                       "	,@CONTINGENCIA " &
                                       "    ,@SAP_STATUS_INTEGRACAO " &
                                       ") "
#End Region


#Region " TbJUN "
    Private strInsertTbJun As String = "INSERT INTO TbJUN " &
                                       "( " &
                                       "	 PEDCOM " &
                                       "	,ITEPED " &
                                       "	,NFEID " &
                                       "	,ITENFE " &
                                       "	,ITEVAL " &
                                       "	,CODUSU " &
                                       "	,CODCOM " &
                                       "	,MOTIVO " &
                                       "	,NCMNFE " &
                                       "	,NCMPED " &
                                       ") " &
                                       "VALUES " &
                                       "( " &
                                       "	 @PEDCOM " &
                                       "	,@ITEPED " &
                                       "	,@NFEID " &
                                       "	,@ITENFE " &
                                       "	,@ITEVAL " &
                                       "	,@CODUSU " &
                                       "	,@CODCOM " &
                                       "	,@MOTIVO " &
                                       "	,@NCMNFE " &
                                       "	,@NCMPED " &
                                       "); SELECT @@identity "
#End Region

#Region " TbDOC_CAB "
    Private strInsertTbDoc_Cab As String = "INSERT INTO TbDOC_CAB " &
                                           "( " &
                                           "	NFEID, " &
                                           "	VNF_TIPO_DOCUMENTO, " &
                                           "	VNF_CONTEUDO_XML, " &
                                           "	VNF_DANFE_ONLINE, " &
                                           "	VNF_DATA_INSERT, " &
                                           "	VNF_USUARIO_INSERT, " &
                                           "	VNF_ANEXO, " &
                                           "	VNF_ANEXO_NOME, " &
                                           "	VNF_ANEXO_EXTENSAO, " &
                                           "	VNF_CODIGO_VERIFICACAO, " &
                                           "	VNF_MATERIAL_RECEBIDO, " &
                                           "	VNF_CLASSIFICACAO " &
                                           ") " &
                                           "VALUES " &
                                           "( " &
                                           "	@NFEID, " &
                                           "	@VNF_TIPO_DOCUMENTO, " &
                                           "	@VNF_CONTEUDO_XML, " &
                                           "	@VNF_DANFE_ONLINE, " &
                                           "	@VNF_DATA_INSERT, " &
                                           "	@VNF_USUARIO_INSERT, " &
                                           "	@VNF_ANEXO, " &
                                           "	@VNF_ANEXO_NOME, " &
                                           "	@VNF_ANEXO_EXTENSAO, " &
                                           "	@VNF_CODIGO_VERIFICACAO, " &
                                           "	@VNF_MATERIAL_RECEBIDO, " &
                                           "	@VNF_CLASSIFICACAO " &
                                           ") "
#End Region

#Region " TbDOC_CAB_SAP "
    Private strInsertTbDoc_Cab_Sap As String = "INSERT INTO TbDOC_CAB_SAP " &
                                               "( " &
                                               "	NFEID, " &
                                               "	SAP_PO_NUMBER, " &
                                               "	SAP_CREATED_ON, " &
                                               "	SAP_CREATED_BY, " &
                                               "	SAP_PAYMENT_TERMS, " &
                                               "	SAP_PURCHASING_GROUP, " &
                                               "	SAP_CURRENCY, " &
                                               "	SAP_VENDOR_CODE, " &
                                               "	SAP_VENDOR_NAME, " &
                                               "	SAP_VENDOR_CNPJ, " &
                                               "	SAP_INCOTERMS1, " &
                                               "	SAP_VERSION_COMPLETE, " &
                                               "	SAP_METSO_CNPJ, " &
                                               "	SAP_RELEASE_INDIC " &
                                               ") " &
                                               "VALUES " &
                                               "( " &
                                               "	@NFEID, " &
                                               "	@SAP_PO_NUMBER, " &
                                               "	@SAP_CREATED_ON, " &
                                               "	@SAP_CREATED_BY, " &
                                               "	@SAP_PAYMENT_TERMS, " &
                                               "	@SAP_PURCHASING_GROUP, " &
                                               "	@SAP_CURRENCY, " &
                                               "	@SAP_VENDOR_CODE, " &
                                               "	@SAP_VENDOR_NAME, " &
                                               "	@SAP_VENDOR_CNPJ, " &
                                               "	@SAP_INCOTERMS1, " &
                                               "	@SAP_VERSION_COMPLETE, " &
                                               "	@SAP_METSO_CNPJ, " &
                                               "	@SAP_RELEASE_INDIC " &
                                               ") "
#End Region

#Region " TbDOC_CAB_NFE "
    Private strInsertTbDoc_Cab_Nfe As String = "INSERT INTO TbDOC_CAB_NFE " &
                                               "( " &
                                               "	NFEID, " &
                                               "	NF_OUTROS_SIGNATURE, " &
                                               "	NF_OUTROS_INFORMACAO_ADICIONAL, " &
                                               "	NF_OUTROS_VERSAO, " &
                                               "	NF_OUTROS_STATUS_CODE, " &
                                               "	NF_OUTROS_STATUS_DESC, " &
                                               "	NF_TRANSP_MODFRETE, " &
                                               "	NF_TRANSP_XNOME, " &
                                               "	NF_TRANSP_CNPJ, " &
                                               "	NF_TRANSP_IE, " &
                                               "	NF_TRANSP_XENDER, " &
                                               "	NF_TRANSP_XMUN, " &
                                               "	NF_TRANSP_UF, " &
                                               "	NF_IDE_CUF, " &
                                               "	NF_IDE_CNF, " &
                                               "	NF_IDE_INDPAG, " &
                                               "	NF_IDE_NATOP, " &
                                               "	NF_IDE_MOD, " &
                                               "	NF_IDE_SERIE, " &
                                               "	NF_IDE_NNF, " &
                                               "	NF_IDE_DHEMI, " &
                                               "	NF_IDE_TPNF, " &
                                               "	NF_IDE_IDDEST, " &
                                               "	NF_IDE_CMUNFG, " &
                                               "	NF_IDE_TPEMISS, " &
                                               "	NF_IDE_TPAMB, " &
                                               "	NF_IDE_FINNFE, " &
                                               "	NF_IDE_INDFINAL, " &
                                               "	NF_IDE_INDPRES, " &
                                               "	NF_IDE_PROCEMI, " &
                                               "	NF_IDE_DHCONT, " &
                                               "	NF_IDE_XJUST, " &
                                               "	NF_IDE_NFREF, " &
                                               "	NF_IDE_MODAL, " &
                                               "	NF_EMIT_CNPJ, " &
                                               "	NF_EMIT_XNOME, " &
                                               "	NF_EMIT_XLGR, " &
                                               "	NF_EMIT_NRO, " &
                                               "	NF_EMIT_XCPL, " &
                                               "	NF_EMIT_XBAIRRO, " &
                                               "	NF_EMIT_CMUN, " &
                                               "	NF_EMIT_UF, " &
                                               "	NF_EMIT_CEP, " &
                                               "	NF_EMIT_CPAIS, " &
                                               "	NF_EMIT_XPAIS, " &
                                               "	NF_EMIT_FONE, " &
                                               "	NF_EMIT_IE, " &
                                               "	NF_EMIT_IEST, " &
                                               "	NF_EMIT_IM, " &
                                               "	NF_EMIT_CNAE, " &
                                               "	NF_EMIT_CRT, " &
                                               "	NF_REM_XNOME, " &
                                               "	NF_REM_CNPJ, " &
                                               "	NF_REM_CMUN, " &
                                               "	NF_DEST_XNOME, " &
                                               "	NF_DEST_CNPJ, " &
                                               "	NF_DEST_XLGR, " &
                                               "	NF_DEST_NRO, " &
                                               "	NF_DEST_XCPL, " &
                                               "	NF_DEST_XBAIRRO, " &
                                               "	NF_DEST_CMUN, " &
                                               "	NF_DEST_XMUN, " &
                                               "	NF_DEST_UF, " &
                                               "	NF_DEST_CEP, " &
                                               "	NF_DEST_CPAIS, " &
                                               "	NF_DEST_XPAIS, " &
                                               "	NF_DEST_FONE, " &
                                               "	NF_DEST_INDIEDEST, " &
                                               "	NF_DEST_IE, " &
                                               "	NF_DEST_ISUF, " &
                                               "	NF_DEST_IM, " &
                                               "	NF_ICMSTOT_VBC, " &
                                               "	NF_ICMSTOT_VICMS, " &
                                               "	NF_ICMSTOT_VBCST, " &
                                               "	NF_ICMSTOT_VST, " &
                                               "	NF_ICMSTOT_VPROD, " &
                                               "	NF_ICMSTOT_VFRETE, " &
                                               "	NF_ICMSTOT_VSEG, " &
                                               "	NF_ICMSTOT_VDESC, " &
                                               "	NF_ICMSTOT_VII, " &
                                               "	NF_ICMSTOT_VIPI, " &
                                               "	NF_ICMSTOT_VPIS, " &
                                               "	NF_ICMSTOT_VCOFINS, " &
                                               "	NF_ICMSTOT_VOUTRO, " &
                                               "	NF_ICMSTOT_VNF, " &
                                               "	NF_ICMSTOT_VTOTTRIB, " &
                                               "	NF_ICMSTOT_VICMSDESON, " &
                                               "	NF_ICMSTOT_VICMSUFDEST, " &
                                               "	NF_ICMSTOT_VICMSUFREMET, " &
                                               "	NF_ICMSTOT_VFCPUFDEST, " &
                                               "	NF_ISSQNTOT_VSERV, " &
                                               "	NF_ISSQNTOT_VBC, " &
                                               "	NF_ISSQNTOT_VISS, " &
                                               "	NF_ISSQNTOT_VPIS, " &
                                               "	NF_ISSQNTOT_VCOFINS, " &
                                               "	NF_ISSQNTOT_DTCOMPET, " &
                                               "	NF_ISSQNTOT_VDEDUCAO, " &
                                               "	NF_ISSQNTOT_VOUTRO, " &
                                               "	NF_ISSQNTOT_VDESCINCOD, " &
                                               "	NF_ISSQNTOT_VDESCCOND, " &
                                               "	NF_ISSQNTOT_VISSRET, " &
                                               "	NF_ISSQNTOT_CREGTRIB, " &
                                               "	NF_RETTRIN_VRETPIS, " &
                                               "	NF_RETTRIN_VRETCOFINS, " &
                                               "	NF_RETTRIN_VRETCSLL, " &
                                               "	NF_RETTRIN_VBCIRRF, " &
                                               "	NF_RETTRIN_VIRRF, " &
                                               "	NF_RETTRIN_VBCRETPREV, " &
                                               "	NF_RETTRIN_VRETPREV, " &
                                               "	NF_RETTRANSP_VSERV, " &
                                               "	NF_RETTRANSP_VBCRET, " &
                                               "	NF_RETTRANSP_PICMSRET, " &
                                               "	NF_RETTRANSP_VICMSRET, " &
                                               "	NF_RETTRANSP_CFOP, " &
                                               "	NF_RETTRANSP_CMUNFG, " &
                                               "	NF_COBR_NFAT, " &
                                               "	NF_COBR_VORIG, " &
                                               "	NF_COBR_VDESC, " &
                                               "	NF_COBR_VLIQ, " &
                                               "	NF_PAG_TPAG, " &
                                               "	NF_PAG_VPAG, " &
                                               "	NF_PAG_CNPJ, " &
                                               "	NF_PAG_TBAND, " &
                                               "	NF_PAG_CAUT, " &
                                               "	NF_PAG_TPINTEGRA, " &
                                               "	NF_NFREF_REFNNF, " &
                                               "	NF_NFREF_REFSerie, " &
                                               "	NF_NFREF_REFDHEMI " &
                                               ") " &
                                               "VALUES " &
                                               "( " &
                                               "	@NFEID, " &
                                               "	@NF_OUTROS_SIGNATURE, " &
                                               "	@NF_OUTROS_INFORMACAO_ADICIONAL, " &
                                               "	@NF_OUTROS_VERSAO, " &
                                               "	@NF_OUTROS_STATUS_CODE, " &
                                               "	@NF_OUTROS_STATUS_DESC, " &
                                               "	@NF_TRANSP_MODFRETE, " &
                                               "	@NF_TRANSP_XNOME, " &
                                               "	@NF_TRANSP_CNPJ, " &
                                               "	@NF_TRANSP_IE, " &
                                               "	@NF_TRANSP_XENDER, " &
                                               "	@NF_TRANSP_XMUN, " &
                                               "	@NF_TRANSP_UF, " &
                                               "	@NF_IDE_CUF, " &
                                               "	@NF_IDE_CNF, " &
                                               "	@NF_IDE_INDPAG, " &
                                               "	@NF_IDE_NATOP, " &
                                               "	@NF_IDE_MOD, " &
                                               "	@NF_IDE_SERIE, " &
                                               "	@NF_IDE_NNF, " &
                                               "	@NF_IDE_DHEMI, " &
                                               "	@NF_IDE_TPNF, " &
                                               "	@NF_IDE_IDDEST, " &
                                               "	@NF_IDE_CMUNFG, " &
                                               "	@NF_IDE_TPEMISS, " &
                                               "	@NF_IDE_TPAMB, " &
                                               "	@NF_IDE_FINNFE, " &
                                               "	@NF_IDE_INDFINAL, " &
                                               "	@NF_IDE_INDPRES, " &
                                               "	@NF_IDE_PROCEMI, " &
                                               "	@NF_IDE_DHCONT, " &
                                               "	@NF_IDE_XJUST, " &
                                               "	@NF_IDE_NFREF, " &
                                               "	@NF_IDE_MODAL, " &
                                               "	@NF_EMIT_CNPJ, " &
                                               "	@NF_EMIT_XNOME, " &
                                               "	@NF_EMIT_XLGR, " &
                                               "	@NF_EMIT_NRO, " &
                                               "	@NF_EMIT_XCPL, " &
                                               "	@NF_EMIT_XBAIRRO, " &
                                               "	@NF_EMIT_CMUN, " &
                                               "	@NF_EMIT_UF, " &
                                               "	@NF_EMIT_CEP, " &
                                               "	@NF_EMIT_CPAIS, " &
                                               "	@NF_EMIT_XPAIS, " &
                                               "	@NF_EMIT_FONE, " &
                                               "	@NF_EMIT_IE, " &
                                               "	@NF_EMIT_IEST, " &
                                               "	@NF_EMIT_IM, " &
                                               "	@NF_EMIT_CNAE, " &
                                               "	@NF_EMIT_CRT, " &
                                               "	@NF_REM_XNOME, " &
                                               "	@NF_REM_CNPJ, " &
                                               "	@NF_REM_CMUN, " &
                                               "	@NF_DEST_XNOME, " &
                                               "	@NF_DEST_CNPJ, " &
                                               "	@NF_DEST_XLGR, " &
                                               "	@NF_DEST_NRO, " &
                                               "	@NF_DEST_XCPL, " &
                                               "	@NF_DEST_XBAIRRO, " &
                                               "	@NF_DEST_CMUN, " &
                                               "	@NF_DEST_XMUN, " &
                                               "	@NF_DEST_UF, " &
                                               "	@NF_DEST_CEP, " &
                                               "	@NF_DEST_CPAIS, " &
                                               "	@NF_DEST_XPAIS, " &
                                               "	@NF_DEST_FONE, " &
                                               "	@NF_DEST_INDIEDEST, " &
                                               "	@NF_DEST_IE, " &
                                               "	@NF_DEST_ISUF, " &
                                               "	@NF_DEST_IM, " &
                                               "	@NF_ICMSTOT_VBC, " &
                                               "	@NF_ICMSTOT_VICMS, " &
                                               "	@NF_ICMSTOT_VBCST, " &
                                               "	@NF_ICMSTOT_VST, " &
                                               "	@NF_ICMSTOT_VPROD, " &
                                               "	@NF_ICMSTOT_VFRETE, " &
                                               "	@NF_ICMSTOT_VSEG, " &
                                               "	@NF_ICMSTOT_VDESC, " &
                                               "	@NF_ICMSTOT_VII, " &
                                               "	@NF_ICMSTOT_VIPI, " &
                                               "	@NF_ICMSTOT_VPIS, " &
                                               "	@NF_ICMSTOT_VCOFINS, " &
                                               "	@NF_ICMSTOT_VOUTRO, " &
                                               "	@NF_ICMSTOT_VNF, " &
                                               "	@NF_ICMSTOT_VTOTTRIB, " &
                                               "	@NF_ICMSTOT_VICMSDESON, " &
                                               "	@NF_ICMSTOT_VICMSUFDEST, " &
                                               "	@NF_ICMSTOT_VICMSUFREMET, " &
                                               "	@NF_ICMSTOT_VFCPUFDEST, " &
                                               "	@NF_ISSQNTOT_VSERV, " &
                                               "	@NF_ISSQNTOT_VBC, " &
                                               "	@NF_ISSQNTOT_VISS, " &
                                               "	@NF_ISSQNTOT_VPIS, " &
                                               "	@NF_ISSQNTOT_VCOFINS, " &
                                               "	@NF_ISSQNTOT_DTCOMPET, " &
                                               "	@NF_ISSQNTOT_VDEDUCAO, " &
                                               "	@NF_ISSQNTOT_VOUTRO, " &
                                               "	@NF_ISSQNTOT_VDESCINCOD, " &
                                               "	@NF_ISSQNTOT_VDESCCOND, " &
                                               "	@NF_ISSQNTOT_VISSRET, " &
                                               "	@NF_ISSQNTOT_CREGTRIB, " &
                                               "	@NF_RETTRIN_VRETPIS, " &
                                               "	@NF_RETTRIN_VRETCOFINS, " &
                                               "	@NF_RETTRIN_VRETCSLL, " &
                                               "	@NF_RETTRIN_VBCIRRF, " &
                                               "	@NF_RETTRIN_VIRRF, " &
                                               "	@NF_RETTRIN_VBCRETPREV, " &
                                               "	@NF_RETTRIN_VRETPREV, " &
                                               "	@NF_RETTRANSP_VSERV, " &
                                               "	@NF_RETTRANSP_VBCRET, " &
                                               "	@NF_RETTRANSP_PICMSRET, " &
                                               "	@NF_RETTRANSP_VICMSRET, " &
                                               "	@NF_RETTRANSP_CFOP, " &
                                               "	@NF_RETTRANSP_CMUNFG, " &
                                               "	@NF_COBR_NFAT, " &
                                               "	@NF_COBR_VORIG, " &
                                               "	@NF_COBR_VDESC, " &
                                               "	@NF_COBR_VLIQ, " &
                                               "	@NF_PAG_TPAG, " &
                                               "	@NF_PAG_VPAG, " &
                                               "	@NF_PAG_CNPJ, " &
                                               "	@NF_PAG_TBAND, " &
                                               "	@NF_PAG_CAUT, " &
                                               "	@NF_PAG_TPINTEGRA, " &
                                               "	@NF_NFREF_REFNNF, " &
                                               "	@NF_NFREF_REFSerie, " &
                                               "	@NF_NFREF_REFDHEMI " &
                                               ") "
#End Region

#Region " TbDOC_CAB_NFE_DUP "
    Private strInsertTbDoc_Cab_Dup As String = "INSERT INTO TbDOC_CAB_NFE_DUP " &
                                               "( " &
                                               "	NFEID, " &
                                               "	NF_COBR_DUP_NDUP, " &
                                               "	NF_COBR_DUP_DVENC, " &
                                               "	NF_COBR_DUP_VDUP " &
                                               ") " &
                                               "VALUES " &
                                               "( " &
                                               "	@NFEID, " &
                                               "	@NF_COBR_DUP_NDUP, " &
                                               "	@NF_COBR_DUP_DVENC, " &
                                               "	@NF_COBR_DUP_VDUP " &
                                               ") "
#End Region

#Region " TbDOC_CAB_NFE_REF "
    Private strInsertTbDoc_Cab_Ref As String = "INSERT INTO TbDOC_CAB_NFE_REF " &
                                               "( " &
                                               "	NFEID, " &
                                               "	NF_NFREF_REFNFE, " &
                                               "	NF_NFREF_REFCTE " &
                                               ") " &
                                               "VALUES " &
                                               "( " &
                                               "	@NFEID, " &
                                               "	@NF_NFREF_REFNFE, " &
                                               "	@NF_NFREF_REFCTE " &
                                               ") "
#End Region

#Region " TbDOC_CAB_CTE "
    Private strInsertTbDoc_Cab_Cte As String = "INSERT INTO TbDOC_CAB_CTE " &
                                               "( " &
                                               "	 NFEID " &
                                               "	,CT_IDE_CMUNINI " &
                                               "	,CT_IDE_XMUNINI " &
                                               "	,CT_IDE_UFINI " &
                                               "	,CT_IDE_CMUNFIM " &
                                               "	,CT_IDE_XMUNFIM " &
                                               "	,CT_IDE_UFFIM " &
                                               "	,CT_IDE_RETIRA " &
                                               "	,CT_IDE_TOMA " &
                                               "	,CT_IDE_TPCTE " &
                                               "	,CT_TOMA_CNPJ " &
                                               "	,CT_TOMA_IE " &
                                               "	,CT_TOMA_XNOME " &
                                               "	,CT_TOMA_XLGR " &
                                               "	,CT_TOMA_NRO " &
                                               "	,CT_TOMA_XBAIRRO " &
                                               "	,CT_TOMA_CMUN " &
                                               "	,CT_TOMA_XMUN " &
                                               "	,CT_TOMA_CEP " &
                                               "	,CT_TOMA_UF " &
                                               "	,CT_TOMA_CPAIS " &
                                               "	,CT_TOMA_XPAIS " &
                                               "	,CT_VPREST_VTPREST " &
                                               "	,CT_VPREST_VREC " &
                                               "	,CT_INFCARGA_VCARGA " &
                                               "	,CT_VPREST_COMP_FRETE_PESO " &
                                               "	,CT_VPREST_COMP_FRETE_VALOR " &
                                               "	,CT_VPREST_COMP_SEC_CAT " &
                                               "	,CT_VPREST_COMP_ADEME " &
                                               "	,CT_VPREST_COMP_PEDAGIO " &
                                               "	,CT_VPREST_COMP_GRIS " &
                                               "	,CT_VPREST_COMP_TAXAEMICTRC " &
                                               "	,CT_VPREST_COMP_COLETAENTREGA " &
                                               "	,CT_VPREST_COMP_OUTROSVALORES " &
                                               "	,CT_VPREST_COMP_FRETE " &
                                               "	,CT_VPREST_COMP_DESCONTO " &
                                               "	,CT_VPREST_COMP_DESPACHO " &
                                               "	,CT_VPREST_COMP_ENTREGA " &
                                               "	,CT_VPREST_COMP_OUTROS " &
                                               "	,CT_VPREST_COMP_ESCOLTA " &
                                               "	,CT_VPREST_COMP_COLETA " &
                                               "	,CT_VPREST_COMP_SEGURO " &
                                               "	,CT_VPREST_COMP_PERNOITE " &
                                               "	,CT_VPREST_COMP_REDESPACHO " &
                                               ") " &
                                               "VALUES " &
                                               "( " &
                                               "	 @NFEID " &
                                               "	,@CT_IDE_CMUNINI " &
                                               "	,@CT_IDE_XMUNINI " &
                                               "	,@CT_IDE_UFINI " &
                                               "	,@CT_IDE_CMUNFIM " &
                                               "	,@CT_IDE_XMUNFIM " &
                                               "	,@CT_IDE_UFFIM " &
                                               "	,@CT_IDE_RETIRA " &
                                               "	,@CT_IDE_TOMA " &
                                               "	,@CT_IDE_TPCTE " &
                                               "	,@CT_TOMA_CNPJ " &
                                               "	,@CT_TOMA_IE " &
                                               "	,@CT_TOMA_XNOME " &
                                               "	,@CT_TOMA_XLGR " &
                                               "	,@CT_TOMA_NRO " &
                                               "	,@CT_TOMA_XBAIRRO " &
                                               "	,@CT_TOMA_CMUN " &
                                               "	,@CT_TOMA_XMUN " &
                                               "	,@CT_TOMA_CEP " &
                                               "	,@CT_TOMA_UF " &
                                               "	,@CT_TOMA_CPAIS " &
                                               "	,@CT_TOMA_XPAIS " &
                                               "	,@CT_VPREST_VTPREST " &
                                               "	,@CT_VPREST_VREC " &
                                               "	,@CT_INFCARGA_VCARGA " &
                                               "	,@CT_VPREST_COMP_FRETE_PESO " &
                                               "	,@CT_VPREST_COMP_FRETE_VALOR " &
                                               "	,@CT_VPREST_COMP_SEC_CAT " &
                                               "	,@CT_VPREST_COMP_ADEME " &
                                               "	,@CT_VPREST_COMP_PEDAGIO " &
                                               "	,@CT_VPREST_COMP_GRIS " &
                                               "	,@CT_VPREST_COMP_TAXAEMICTRC " &
                                               "	,@CT_VPREST_COMP_COLETAENTREGA " &
                                               "	,@CT_VPREST_COMP_OUTROSVALORES " &
                                               "	,@CT_VPREST_COMP_FRETE " &
                                               "	,@CT_VPREST_COMP_DESCONTO " &
                                               "	,@CT_VPREST_COMP_DESPACHO " &
                                               "	,@CT_VPREST_COMP_ENTREGA " &
                                               "	,@CT_VPREST_COMP_OUTROS " &
                                               "	,@CT_VPREST_COMP_ESCOLTA " &
                                               "	,@CT_VPREST_COMP_COLETA " &
                                               "	,@CT_VPREST_COMP_SEGURO " &
                                               "	,@CT_VPREST_COMP_PERNOITE " &
                                               "	,@CT_VPREST_COMP_REDESPACHO " &
                                               ") "
#End Region


#Region " TbDOC_ITEM "
    Private strInsertTbDoc_Item As String = "INSERT INTO TbDOC_ITEM " &
                                            "( " &
                                            "	NFEID, " &
                                            "	VNF_CODJUN, " &
                                            "	VNF_ITEM_VALIDO, " &
                                            "	VNF_CONFIRMADO_PORTAL, " &
                                            "	VNF_ID_MODO_PROCESSO, " &
                                            "	VNF_INBOUND_DELIVERY_NUMBER, " &
                                            "	VNF_INBOUND_DELIVERY_ITEM_NUMBER " &
                                            ") " &
                                            "VALUES " &
                                            "( " &
                                            "	@NFEID, " &
                                            "	@VNF_CODJUN, " &
                                            "	@VNF_ITEM_VALIDO, " &
                                            "	@VNF_CONFIRMADO_PORTAL, " &
                                            "	@VNF_ID_MODO_PROCESSO, " &
                                            "	@VNF_INBOUND_DELIVERY_NUMBER, " &
                                            "	@VNF_INBOUND_DELIVERY_ITEM_NUMBER " &
                                            ") "
#End Region

#Region " TbDOC_ITEM_SAP "
    Private strInsertTbDoc_Item_Sap As String = "INSERT INTO TbDOC_ITEM_SAP " &
                                                "( " &
                                                "	NFEID, " &
                                                "	SAP_PO_NUMBER, " &
                                                "	SAP_ITEM_NUMBER, " &
                                                "	SAP_MATERIAL, " &
                                                "	SAP_PO_QUANTITY, " &
                                                "	SAP_UNIT_OF_MEASURE, " &
                                                "	SAP_NET_PRICE, " &
                                                "	SAP_TAX_CODE, " &
                                                "	SAP_DELIVERY_COMPLETED, " &
                                                "	SAP_FINAL_INVOICE, " &
                                                "	SAP_NCM_CODE, " &
                                                "	SAP_PLANT, " &
                                                "	SAP_PRICE_UNIT, " &
                                                "	SAP_DELETION_INDICATOR, " &
                                                "	SAP_MATERIAL_DESCRIPTION, " &
                                                "	SAP_PO_ITEM_SHORT_TEXT, " &
                                                "	SAP_CONFIRMATION_CONTROL_KEY, " &
                                                "	SAP_OVERDELIVERY_TOLERANCE_LIMIT, " &
                                                "	SAP_UNLIMITED_OVERDELIVERY_ALLOWED, " &
                                                "	SAP_UNDERDELIVERY_TOLERANCE_LIMIT, " &
                                                "	SAP_OPEN_QUANTITY, " &
                                                "	SAP_STORAGE_LOCATION, " &
                                                "	SAP_ACCOUNT_ASSIGNMENT_CATEGORY, " &
                                                "	SAP_ITEM_CATEGORY, " &
                                                "	SAP_GOODS_RECEIPT, " &
                                                "	SAP_INVOICE_RECEIPT, " &
                                                "	SAP_NF_NET_ITEM_VALUE, " &
                                                "	SAP_NET_ITEM_VALUE, " &
                                                "	SAP_ITEM_VALUE_WITH_TAXES, " &
                                                "	SAP_NF_TOTAL_ITEM, " &
                                                "	SAP_NF_NET_FREIGHT_VALUE, " &
                                                "	SAP_NET_FREIGHT_VALUE, " &
                                                "	SAP_NF_NET_INSURANCE_VALUE, " &
                                                "	SAP_NET_INSURANCE_VALUE, " &
                                                "	SAP_NF_NET_OTHER_EXPENSES_VALUES, " &
                                                "	SAP_NET_OTHER_EXPENSES_VALUES, " &
                                                "	SAP_NF_NET_DISCOUNT_VALUE, " &
                                                "	SAP_NET_DISCOUNT_VALUE, " &
                                                "	SAP_DISCOUNT_VALUE_WITH_TAXES, " &
                                                "	SAP_ITEM_CFOP, " &
                                                "	SAP_CROSS_PLANT_MATERIAL_STATUS, " &
                                                "	SAP_PLANT_MATERIAL_STATUS, " &
                                                "	SAP_CROSS_DISTRIBUTION_CHAIN_STATUS, " &
                                                "	SAP_DELETION_PLANT_LEVEL, " &
                                                "	SAP_DELETION_CLIENT_LEVEL, " &
                                                "	SAP_DELETION_STORAGE_LOCATION, " &
                                                "	SAP_USAGE_MATERIAL, " &
                                                "	SAP_ORIGIN_MATERIAL, " &
                                                "	SAP_TAX_SPLIT " &
                                                ") " &
                                                "VALUES " &
                                                "( " &
                                                "	@NFEID, " &
                                                "	@SAP_PO_NUMBER, " &
                                                "	@SAP_ITEM_NUMBER, " &
                                                "	@SAP_MATERIAL, " &
                                                "	@SAP_PO_QUANTITY, " &
                                                "	@SAP_UNIT_OF_MEASURE, " &
                                                "	@SAP_NET_PRICE, " &
                                                "	@SAP_TAX_CODE, " &
                                                "	@SAP_DELIVERY_COMPLETED, " &
                                                "	@SAP_FINAL_INVOICE, " &
                                                "	@SAP_NCM_CODE, " &
                                                "	@SAP_PLANT, " &
                                                "	@SAP_PRICE_UNIT, " &
                                                "	@SAP_DELETION_INDICATOR, " &
                                                "	@SAP_MATERIAL_DESCRIPTION, " &
                                                "	@SAP_PO_ITEM_SHORT_TEXT, " &
                                                "	@SAP_CONFIRMATION_CONTROL_KEY, " &
                                                "	@SAP_OVERDELIVERY_TOLERANCE_LIMIT, " &
                                                "	@SAP_UNLIMITED_OVERDELIVERY_ALLOWED, " &
                                                "	@SAP_UNDERDELIVERY_TOLERANCE_LIMIT, " &
                                                "	@SAP_OPEN_QUANTITY, " &
                                                "	@SAP_STORAGE_LOCATION, " &
                                                "	@SAP_ACCOUNT_ASSIGNMENT_CATEGORY, " &
                                                "	@SAP_ITEM_CATEGORY, " &
                                                "	@SAP_GOODS_RECEIPT, " &
                                                "	@SAP_INVOICE_RECEIPT, " &
                                                "	@SAP_NF_NET_ITEM_VALUE, " &
                                                "	@SAP_NET_ITEM_VALUE, " &
                                                "	@SAP_ITEM_VALUE_WITH_TAXES, " &
                                                "	@SAP_NF_TOTAL_ITEM, " &
                                                "	@SAP_NF_NET_FREIGHT_VALUE, " &
                                                "	@SAP_NET_FREIGHT_VALUE, " &
                                                "	@SAP_NF_NET_INSURANCE_VALUE, " &
                                                "	@SAP_NET_INSURANCE_VALUE, " &
                                                "	@SAP_NF_NET_OTHER_EXPENSES_VALUES, " &
                                                "	@SAP_NET_OTHER_EXPENSES_VALUES, " &
                                                "	@SAP_NF_NET_DISCOUNT_VALUE, " &
                                                "	@SAP_NET_DISCOUNT_VALUE, " &
                                                "	@SAP_DISCOUNT_VALUE_WITH_TAXES, " &
                                                "	@SAP_ITEM_CFOP, " &
                                                "	@SAP_CROSS_PLANT_MATERIAL_STATUS, " &
                                                "	@SAP_PLANT_MATERIAL_STATUS, " &
                                                "	@SAP_CROSS_DISTRIBUTION_CHAIN_STATUS, " &
                                                "	@SAP_DELETION_PLANT_LEVEL, " &
                                                "	@SAP_DELETION_CLIENT_LEVEL, " &
                                                "	@SAP_DELETION_STORAGE_LOCATION, " &
                                                "	@SAP_USAGE_MATERIAL, " &
                                                "	@SAP_ORIGIN_MATERIAL, " &
                                                "	@SAP_TAX_SPLIT " &
                                                ")"
#End Region

#Region " TbDOC_ITEM_NFE "
    Private strInsertTbDoc_Item_Nfe As String = "INSERT INTO TbDOC_ITEM_NFE " &
                                                "( " &
                                                "	NFEID, " &
                                                "	NF_PROD_ITEM, " &
                                                "	NF_PROD_CPROD, " &
                                                "	NF_PROD_CEAN, " &
                                                "	NF_PROD_XPROD, " &
                                                "	NF_PROD_NCM, " &
                                                "	NF_PROD_CFOP, " &
                                                "	NF_PROD_CFOP_DESC, " &
                                                "	NF_PROD_UCOM, " &
                                                "	NF_PROD_QCOM, " &
                                                "	NF_PROD_VUNCOM, " &
                                                "	NF_PROD_VPROD, " &
                                                "	NF_PROD_UTRIB, " &
                                                "	NF_PROD_QTRIB, " &
                                                "	NF_PROD_VDESC, " &
                                                "	NF_PROD_INF_ADICIONAL_ITEM, " &
                                                "	NF_PROD_NVE, " &
                                                "	NF_PROD_EXTIPI, " &
                                                "	NF_PROD_VFRETE, " &
                                                "	NF_PROD_VSEG, " &
                                                "	NF_PROD_VOUTRO, " &
                                                "	NF_PROD_INDTOT, " &
                                                "	NF_PROD_DI, " &
                                                "	NF_PROD_DETESPECIFICO, " &
                                                "	NF_PROD_XPED, " &
                                                "	NF_PROD_NITEMPED, " &
                                                "	NF_PROD_FCI, " &
                                                "	NF_ICMS_PICMS, " &
                                                "	NF_ICMS_ORIG, " &
                                                "	NF_ICMS_CST, " &
                                                "	NF_ICMS_MODBC, " &
                                                "	NF_ICMS_PREDBC, " &
                                                "	NF_ICMS_VBC, " &
                                                "	NF_ICMS_VICMS, " &
                                                "	NF_ICMS_MODBCST, " &
                                                "	NF_ICMS_MVAST, " &
                                                "	NF_ICMSREDBCST, " &
                                                "	NF_ICMS_VBCST, " &
                                                "	NF_ICMS_PICMSST, " &
                                                "	NF_ICMS_VICMSST, " &
                                                "	NF_ICMS_VBCSTRET, " &
                                                "	NF_ICMS_VBCSTDEST, " &
                                                "	NF_ICMS_VICMSSTDEST, " &
                                                "	NF_ICMS_MOTDESICMS, " &
                                                "	NF_ICMS_PBCOP, " &
                                                "	NF_ICMS_UFST, " &
                                                "	NF_ICMS_PCREDSN, " &
                                                "	NF_ICMS_VCREICMSSN, " &
                                                "	NF_ICMS_VICMSDESON, " &
                                                "	NF_ICMS_VICMSOP, " &
                                                "	NF_ICMS_PDIF, " &
                                                "	NF_ICMS_VICMSDIF, " &
                                                "	NF_IPI_CLENQ, " &
                                                "	NF_IPI_CNPJPROD, " &
                                                "	NF_IPI_CSELO, " &
                                                "	NF_IPI_QSELO, " &
                                                "	NF_IPI_CENQ, " &
                                                "	NF_IPI_CST, " &
                                                "	NF_IPI_VBC, " &
                                                "	NF_IPI_PIPI, " &
                                                "	NF_IPI_VIPI, " &
                                                "	NF_IPI_QUNID, " &
                                                "	NF_IPI_VUNID, " &
                                                "	NF_II_VBC, " &
                                                "	NF_II_VDESPADU, " &
                                                "	NF_II_VII, " &
                                                "	NF_II_VIOF, " &
                                                "	NF_PIS_CST, " &
                                                "	NF_PIS_VBC, " &
                                                "	NF_PIS_PPIS, " &
                                                "	NF_PIS_VPIS, " &
                                                "	NF_PIS_QBCPROD, " &
                                                "	NF_PIS_VALIQPROD, " &
                                                "	NF_PISST_VBC, " &
                                                "	NF_PISST_PPIS, " &
                                                "	NF_PISST_VPIS, " &
                                                "	NF_PISST_QBCPROD, " &
                                                "	NF_PISST_VALIQPROD, " &
                                                "	NF_COFINS_CST, " &
                                                "	NF_COFINS_VBC, " &
                                                "	NF_COFINS_PCOFINS, " &
                                                "	NF_COFINS_VCOFINS, " &
                                                "	NF_COFINS_QBCPROD, " &
                                                "	NF_COFINS_VALIQPROD, " &
                                                "	NF_COFINSST_VBC, " &
                                                "	NF_COFINSST_PCOFINS, " &
                                                "	NF_COFINSST_VCOFINS, " &
                                                "	NF_COFINSST_QBCPROD, " &
                                                "	NF_COFINSST_VALIQPROD, " &
                                                "	NF_ISSQN_VBC, " &
                                                "	NF_ISSQN_VALIQ, " &
                                                "	NF_ISSQN_VISSQN, " &
                                                "	NF_ISSQN_CMUNFG, " &
                                                "	NF_ISSQN_CLISTSERV, " &
                                                "	NF_ISSQN_VDEDUCAO, " &
                                                "	NF_ISSQN_VOUTRO, " &
                                                "	NF_ISSQN_VDESCINCOND, " &
                                                "	NF_ISSQN_VDESCCOND, " &
                                                "	NF_ISSQN_VISSRET, " &
                                                "	NF_ISSQN_INDISS, " &
                                                "	NF_ISSQN_CSERVICO, " &
                                                "	NF_ISSQN_CMUN, " &
                                                "	NF_ISSQN_CPAIS, " &
                                                "	NF_ISSQN_NPROCESSO, " &
                                                "	NF_ISSQN_INDINCENTIVO, " &
                                                "	NF_ICMSUFDEST_VBCUFDEST, " &
                                                "	NF_ICMSUFDEST_PFCPUDEST, " &
                                                "	NF_ICMSUFDEST_PICMSUFDEST, " &
                                                "	NF_ICMSUFDEST_PICMSINTER, " &
                                                "	NF_ICMSUFDEST_PICMSINTERPART, " &
                                                "	NF_ICMSUFDEST_VFCPUFDEST, " &
                                                "	NF_ICMSUFDEST_VICMSUFDEST, " &
                                                "	NF_ICMSUFDEST_VICMSUFREMET " &
                                                ") " &
                                                "VALUES " &
                                                "( " &
                                                "	@NFEID, " &
                                                "	@NF_PROD_ITEM, " &
                                                "	@NF_PROD_CPROD, " &
                                                "	@NF_PROD_CEAN, " &
                                                "	@NF_PROD_XPROD, " &
                                                "	@NF_PROD_NCM, " &
                                                "	@NF_PROD_CFOP, " &
                                                "	@NF_PROD_CFOP_DESC, " &
                                                "	@NF_PROD_UCOM, " &
                                                "	@NF_PROD_QCOM, " &
                                                "	@NF_PROD_VUNCOM, " &
                                                "	@NF_PROD_VPROD, " &
                                                "	@NF_PROD_UTRIB, " &
                                                "	@NF_PROD_QTRIB, " &
                                                "	@NF_PROD_VDESC, " &
                                                "	@NF_PROD_INF_ADICIONAL_ITEM, " &
                                                "	@NF_PROD_NVE, " &
                                                "	@NF_PROD_EXTIPI, " &
                                                "	@NF_PROD_VFRETE, " &
                                                "	@NF_PROD_VSEG, " &
                                                "	@NF_PROD_VOUTRO, " &
                                                "	@NF_PROD_INDTOT, " &
                                                "	@NF_PROD_DI, " &
                                                "	@NF_PROD_DETESPECIFICO, " &
                                                "	@NF_PROD_XPED, " &
                                                "	@NF_PROD_NITEMPED, " &
                                                "	@NF_PROD_FCI, " &
                                                "	@NF_ICMS_PICMS, " &
                                                "	@NF_ICMS_ORIG, " &
                                                "	@NF_ICMS_CST, " &
                                                "	@NF_ICMS_MODBC, " &
                                                "	@NF_ICMS_PREDBC, " &
                                                "	@NF_ICMS_VBC, " &
                                                "	@NF_ICMS_VICMS, " &
                                                "	@NF_ICMS_MODBCST, " &
                                                "	@NF_ICMS_MVAST, " &
                                                "	@NF_ICMSREDBCST, " &
                                                "	@NF_ICMS_VBCST, " &
                                                "	@NF_ICMS_PICMSST, " &
                                                "	@NF_ICMS_VICMSST, " &
                                                "	@NF_ICMS_VBCSTRET, " &
                                                "	@NF_ICMS_VBCSTDEST, " &
                                                "	@NF_ICMS_VICMSSTDEST, " &
                                                "	@NF_ICMS_MOTDESICMS, " &
                                                "	@NF_ICMS_PBCOP, " &
                                                "	@NF_ICMS_UFST, " &
                                                "	@NF_ICMS_PCREDSN, " &
                                                "	@NF_ICMS_VCREICMSSN, " &
                                                "	@NF_ICMS_VICMSDESON, " &
                                                "	@NF_ICMS_VICMSOP, " &
                                                "	@NF_ICMS_PDIF, " &
                                                "	@NF_ICMS_VICMSDIF, " &
                                                "	@NF_IPI_CLENQ, " &
                                                "	@NF_IPI_CNPJPROD, " &
                                                "	@NF_IPI_CSELO, " &
                                                "	@NF_IPI_QSELO, " &
                                                "	@NF_IPI_CENQ, " &
                                                "	@NF_IPI_CST, " &
                                                "	@NF_IPI_VBC, " &
                                                "	@NF_IPI_PIPI, " &
                                                "	@NF_IPI_VIPI, " &
                                                "	@NF_IPI_QUNID, " &
                                                "	@NF_IPI_VUNID, " &
                                                "	@NF_II_VBC, " &
                                                "	@NF_II_VDESPADU, " &
                                                "	@NF_II_VII, " &
                                                "	@NF_II_VIOF, " &
                                                "	@NF_PIS_CST, " &
                                                "	@NF_PIS_VBC, " &
                                                "	@NF_PIS_PPIS, " &
                                                "	@NF_PIS_VPIS, " &
                                                "	@NF_PIS_QBCPROD, " &
                                                "	@NF_PIS_VALIQPROD, " &
                                                "	@NF_PISST_VBC, " &
                                                "	@NF_PISST_PPIS, " &
                                                "	@NF_PISST_VPIS, " &
                                                "	@NF_PISST_QBCPROD, " &
                                                "	@NF_PISST_VALIQPROD, " &
                                                "	@NF_COFINS_CST, " &
                                                "	@NF_COFINS_VBC, " &
                                                "	@NF_COFINS_PCOFINS, " &
                                                "	@NF_COFINS_VCOFINS, " &
                                                "	@NF_COFINS_QBCPROD, " &
                                                "	@NF_COFINS_VALIQPROD, " &
                                                "	@NF_COFINSST_VBC, " &
                                                "	@NF_COFINSST_PCOFINS, " &
                                                "	@NF_COFINSST_VCOFINS, " &
                                                "	@NF_COFINSST_QBCPROD, " &
                                                "	@NF_COFINSST_VALIQPROD, " &
                                                "	@NF_ISSQN_VBC, " &
                                                "	@NF_ISSQN_VALIQ, " &
                                                "	@NF_ISSQN_VISSQN, " &
                                                "	@NF_ISSQN_CMUNFG, " &
                                                "	@NF_ISSQN_CLISTSERV, " &
                                                "	@NF_ISSQN_VDEDUCAO, " &
                                                "	@NF_ISSQN_VOUTRO, " &
                                                "	@NF_ISSQN_VDESCINCOND, " &
                                                "	@NF_ISSQN_VDESCCOND, " &
                                                "	@NF_ISSQN_VISSRET, " &
                                                "	@NF_ISSQN_INDISS, " &
                                                "	@NF_ISSQN_CSERVICO, " &
                                                "	@NF_ISSQN_CMUN, " &
                                                "	@NF_ISSQN_CPAIS, " &
                                                "	@NF_ISSQN_NPROCESSO, " &
                                                "	@NF_ISSQN_INDINCENTIVO, " &
                                                "	@NF_ICMSUFDEST_VBCUFDEST, " &
                                                "	@NF_ICMSUFDEST_PFCPUDEST, " &
                                                "	@NF_ICMSUFDEST_PICMSUFDEST, " &
                                                "	@NF_ICMSUFDEST_PICMSINTER, " &
                                                "	@NF_ICMSUFDEST_PICMSINTERPART, " &
                                                "	@NF_ICMSUFDEST_VFCPUFDEST, " &
                                                "	@NF_ICMSUFDEST_VICMSUFDEST, " &
                                                "	@NF_ICMSUFDEST_VICMSUFREMET " &
                                                ")"
#End Region

#Region " TbDOC_ITEM_CTE "
    Private strInsertTbDoc_Item_Cte As String = "INSERT INTO TbDOC_ITEM_CTE " &
                                                "( " &
                                                "	 NFEID " &
                                                "	,CT_INFNFE_CHAVE " &
                                                ") " &
                                                "VALUES " &
                                                "(	  " &
                                                "	 @NFEID " &
                                                "	,@CT_INFNFE_CHAVE " &
                                                ") "
#End Region

#Region " TBDOC_CONSIGNACAO_REFNF "
    Private strInsertTBDOC_CONSIGNACAO_REFNF As String = "INSERT INTO TBDOC_CONSIGNACAO_REFNF " &
                                                "( " &
                                                "	 NFEID " &
                                                "	,ITEM_NF " &
                                                "	,NUMERO_REFNF " &
                                                "	,SERIE_REFNF " &
                                                ") " &
                                                "VALUES " &
                                                "(	  " &
                                                "	 @NFEID " &
                                                "	,@ITEM_NF " &
                                                "	,@NUMERO_REFNF " &
                                                "	,@SERIE_REFNF " &
                                                ") "
#End Region


#Region " TBDOC_SUBCONTRATACAO_REFNF "
    Private strInsertTBDOC_SUBCONTRATACAO_REFNF As String = "INSERT INTO TBDOC_SUBCONTRATACAO_REFNF " &
                                                "( " &
                                                "	 NFEID " &
                                                "	,ITEM_NF " &
                                                "	,NUMERO_REFNF " &
                                                "	,SERIE_REFNF " &
                                                "	,ITEM_REFNF " &
                                                ") " &
                                                "VALUES " &
                                                "(	  " &
                                                "	 @NFEID " &
                                                "	,@ITEM_NF " &
                                                "	,@NUMERO_REFNF " &
                                                "	,@SERIE_REFNF " &
                                                "	,@ITEM_REFNF " &
                                                ") "
#End Region

#Region " TBDOC_NOTA_COMPLEMENTAR_REFNF "
    Private strInsertTBDOC_NOTA_COMPLEMENTAR_REFNF As String = "INSERT INTO TBDOC_NOTA_COMPLEMENTAR_REFNF " &
                                                "( " &
                                                "	 NFEID " &
                                                "	,ITEM_NF " &
                                                "	,NUMERO_REFNF " &
                                                "	,SERIE_REFNF " &
                                                ") " &
                                                "VALUES " &
                                                "(	  " &
                                                "	 @NFEID " &
                                                "	,@ITEM_NF " &
                                                "	,@NUMERO_REFNF " &
                                                "	,@SERIE_REFNF " &
                                                ") "
#End Region

#End Region



End Class
