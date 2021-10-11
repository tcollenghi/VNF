'Autor : Marcio Spinosa - CR00008351
'Data: 23/07/2018
'OBS: Ajuste para validar somente os cfops de componente
'**************************************************************************
'Autor : Marcio Spinosa - CR00008351
'Data: 28/05/2018
'OBS: Ajuste feito para o VNF não utilize o AD para trazer dados do usuário
' tendo os mesmos em base

Imports System.IO
Imports System.Text
Imports MetsoFramework.Utils
Imports MetsoFramework.SAP
Imports MetsoFramework.Core
Imports MetsoFramework.SAP.SAP_RFC
Imports System.Globalization

Public Class BLNotaFiscal

    Public Function GetByFilter(ByVal DataDe As String, ByVal DataAte As String, ByVal TipoData As modNF.TipoData, ByVal Numero As String,
                                ByVal Cnpj As String, ByVal Situacao As String, ByVal PurchaseOrder As String, ByVal Fornecedor As String,
                                ByVal TipoDocumento As String, ByVal TipoFrete As String, ByVal Unidade As String, ByVal MaterialRecebido As String,
                                ByVal StatusIntegracao As String, ByVal NfeID As String,
                                ByVal pTipoNFE As String) As DataTable 'Marcio Spinosa - 24/04/2019 - CR00009165
        Try
            Dim datDataDe As String
            Dim datDataAte As String
            Dim strNumero As String
            Dim strCnpj As String
            Dim strQueryBuscarNF As String
            Dim IsLimitarPeriodo As Boolean = True

            '---> VALIDA OS CAMPOS PREENCHIDOS
            datDataDe = DataDe.Trim()
            datDataDe = datDataDe.Replace("/  /", "")
            If datDataDe <> "" And datDataDe.ToString.Length > 8 Then
                Date.Parse(datDataDe)
            End If

            datDataAte = DataAte.Trim()
            datDataAte = datDataAte.Replace("/  /", "")
            If datDataAte <> "" And datDataAte.ToString.Length > 8 Then
                Date.Parse(datDataAte)
            End If

            strNumero = Numero.Trim()
            If strNumero <> "" Then
                Long.Parse(strNumero)
            End If

            strCnpj = Cnpj.RemoveLetters().Trim()
            If strCnpj <> "" Then
                Long.Parse(strCnpj)
            End If


            '---> SITUACAO, NUMERO NF, CNPJ e DATA 
            strQueryBuscarNF = ""
            If Not String.IsNullOrEmpty(Situacao) AndAlso Situacao <> "(TODAS)" Then
                strQueryBuscarNF &= strQueryBuscarNF & " and N.SITUACAO collate SQL_Latin1_General_Cp1251_CS_AS = '" & Situacao & "' "
            End If

            If strNumero <> "" Then
                strQueryBuscarNF &= strQueryBuscarNF & " and CN.NF_IDE_NNF = '" & strNumero & "' "
                IsLimitarPeriodo = False
            End If

            If strCnpj <> "" Then
                strQueryBuscarNF &= strQueryBuscarNF & " and CN.NF_EMIT_CNPJ = '" & strCnpj & "' "
                IsLimitarPeriodo = False
            End If

            If datDataDe <> "" AndAlso datDataAte <> "" Then
                strQueryBuscarNF &= strQueryBuscarNF & " and @DATA BETWEEN '" & Format(Date.Parse(datDataDe), "yyyy-MM-dd") & " 00:00:00' AND '" & Format(Date.Parse(datDataAte), "yyyy-MM-dd") & " 23:59:59' "
                IsLimitarPeriodo = False
            End If

            If TipoData = modNF.TipoData.Emissao Then
                strQueryBuscarNF = strQueryBuscarNF.Replace("@DATA", "CN.NF_IDE_DHEMI")
            ElseIf TipoData = modNF.TipoData.Chegada Then
                strQueryBuscarNF = strQueryBuscarNF.Replace("@DATA", "TbPORT_EF.HORCHE")
            ElseIf TipoData = modNF.TipoData.Integracao Then
                strQueryBuscarNF = strQueryBuscarNF.Replace("@DATA", "vwStatusIntegracao.DATA_INTEGRACAO")
            End If

            '---> PURCHASE ORDER
            If PurchaseOrder.Trim() <> "" Then
                strQueryBuscarNF &= " and J.PEDCOM = '" & PurchaseOrder.Trim() & "' "
                IsLimitarPeriodo = False
            End If

            '---> NFE ID
            If NfeID.Trim() <> "" Then
                strQueryBuscarNF &= " and C.nfeid = '" & NfeID.Trim() & "' "
                IsLimitarPeriodo = False
            End If

            'Marcio Spinosa - 24/04/2019 - CR00009165
            '---> Tipo NFe
            If pTipoNFE.Trim() <> "" Then
                strQueryBuscarNF &= " and CN.NF_IDE_TPNF = '" & pTipoNFE.Trim() & "' "
                strQueryBuscarNF &= " and C.VNF_TIPO_DOCUMENTO = 'NFE' "
                IsLimitarPeriodo = False
            End If
            'Marcio Spinosa - 24/04/2019 - CR00009165 - Fim

            '---> FORNECEDOR
            If Fornecedor.Trim() <> "" Then
                strQueryBuscarNF &= " and CN.NF_EMIT_XNOME LIKE '%' + '" & Fornecedor.Trim() & "' + '%' "
                IsLimitarPeriodo = False
            End If

            '---> TIPO DE DOCUMENTO
            If Not String.IsNullOrEmpty(TipoDocumento) Then
                strQueryBuscarNF &= " and VNF_TIPO_DOCUMENTO collate SQL_Latin1_General_Cp1251_CS_AS = '" & TipoDocumento & "'"
                IsLimitarPeriodo = False
            End If

            '---> TIPO DE FRETES
            If Not String.IsNullOrEmpty(TipoFrete) Then
                strQueryBuscarNF &= " and TipoFrete collate SQL_Latin1_General_Cp1251_CS_AS = '" & TipoFrete & "'"
                IsLimitarPeriodo = False
            End If

            '---> MATERIAL RECEBIDO
            If Not String.IsNullOrEmpty(MaterialRecebido) Then
                strQueryBuscarNF &= " and isnull(VNF_MATERIAL_RECEBIDO, 0) = " & MaterialRecebido & ""
                IsLimitarPeriodo = False
            End If

            '---> STATUS INTEGRAÇÃO
            If Not String.IsNullOrEmpty(StatusIntegracao) Then
                strQueryBuscarNF &= " and vwStatusIntegracao.STATUS_INTEGRACAO collate SQL_Latin1_General_Cp1251_CS_AS = '" & StatusIntegracao & "'"
                IsLimitarPeriodo = False
            End If

            '---> PLANTA
            If Not String.IsNullOrEmpty(Unidade) AndAlso Unidade <> "(TODAS)" Then
                Dim selectCNPJ As String = "SELECT distinct cnpj FROM TbPlantaCnpj where descricao collate SQL_Latin1_General_Cp1251_CS_AS = '" & Unidade & "' "
                Dim strPlantaCnpj As String = modSQL.ExecuteScalar(selectCNPJ)
                strQueryBuscarNF &= " and CN.NF_DEST_CNPJ = '" & strPlantaCnpj & "' "
                IsLimitarPeriodo = False
            End If

            If strQueryBuscarNF = "" Then
                strQueryBuscarNF &= " and CN.NF_IDE_DHEMI >= '" & Format(Now, "yyyy-MM-dd") & " 00:00:00'"
            ElseIf IsLimitarPeriodo Then
                strQueryBuscarNF &= " and CN.NF_IDE_DHEMI >= '" & Format(Now.AddMonths(-1), "yyyy-MM-dd") & " 00:00:00'"
            End If

            '---> MONTA A CONSULTA  " 	J.CODCOM, " &
            strQueryBuscarNF = " SELECT DISTINCT " &
                               " 	C.VNF_TIPO_DOCUMENTO, " &
                               " 	CN.NF_IDE_NNF, " &
                               " 	CN.NF_IDE_SERIE, " &
                               " 	CN.NFEID, " &
                               " 	CN.NF_IDE_DHEMI, " &
                               " 	N.DATVAL, " &
                               " 	SITUACAO = CASE WHEN ISNULL(N.SITUACAO, '') = '' THEN 'PENDENTE' ELSE N.SITUACAO END, " &
                               " 	CN.NF_EMIT_CNPJ, " &
                               " 	CN.NF_EMIT_XNOME, " &
                               " 	N.NFEREL, " &
                               " 	N.NFEVAL, " &
                               " 	CN.NF_DEST_CNPJ " &
                               " FROM " &
                               " 	TBNFE N " &
                               " 	LEFT JOIN TbDOC_CAB C ON (N.NFEID = C.NFEID)   " &
                               " 	LEFT JOIN TbDOC_CAB_NFE CN ON (N.NFEID = CN.NFEID) " &
                               " 	LEFT JOIN TbDOC_CAB_SAP CS ON (N.NFEID = CS.NFEID) " &
                               " 	LEFT JOIN TBJUN J ON (N.NFEID = J.NFEID) " &
                               "	LEFT JOIN TbPORT_EF ON (N.NFEID = TbPORT_EF.NFEID) " &
                               "	LEFT JOIN vwStatusIntegracao ON (N.NFEID = vwStatusIntegracao.NFEID) " &
                               "	LEFT JOIN vwRelatorioSAP ON (N.NFEID collate SQL_Latin1_General_CP1_CI_AS = vwRelatorioSAP.ChaveAcessoCTE) " &
                               " WHERE " &
                                    Mid(strQueryBuscarNF, 5, Len(strQueryBuscarNF)) &
                               " ORDER BY " &
                               " 	CN.NF_IDE_DHEMI desc "

            Return modSQL.Fill(strQueryBuscarNF)
        Catch ex As Exception
            Throw ex
        End Try
    End Function
    ''' <summary>
    ''' método para buscar as notas fiscais no Registro Fiscal
    ''' </summary>
    ''' <param name="pNumeroNf"></param>
    ''' <param name="pPasta"></param>
    ''' <param name="pUnidade"></param>
    ''' <param name="pFornecedor"></param>
    ''' <param name="pSituacao"></param>
    ''' <param name="pTipoDocumento"></param>
    ''' <param name="pTipoFrete"></param>
    ''' <param name="pRelevante"></param>
    ''' <param name="pMaterialRecebido"></param>
    ''' <param name="pStatusIntegracao"></param>
    ''' <param name="pTipoData"></param>
    ''' <param name="pDataDe"></param>
    ''' <param name="pDataAte"></param>
    ''' <param name="pCodFornecedor"></param>
    ''' <param name="pQtdRegistros"></param>
    ''' <param name="pExceedQtd"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>'Marcio Spinosa - 16/08/2018 - CR00008351 - Ajuste para buscar os fornecedores com like devido inserir na base sem o "0" inicial.</example>
    Public Function GetByFilterRegistroFiscal(ByVal pNumeroNf As String, ByVal pPasta As String, ByVal pUnidade As String, ByVal pFornecedor As String, ByVal pSituacao As String, ByVal pTipoDocumento As String, ByVal pTipoFrete As String, ByVal pRelevante As String, ByVal pMaterialRecebido As String, ByVal pStatusIntegracao As String, ByVal pTipoData As String, ByVal pDataDe As String, ByVal pDataAte As String, ByVal pCodFornecedor As String,
                                              ByVal pQtdRegistros As Integer, ByRef pExceedQtd As Boolean, ByVal pNfeid As String, 'Marcio Spinosa - 28/08/2018 - CRXXXX
                                              ByVal pTipoNotaFiscal As String) As DataTable 'Marcio Spinosa - 24/04/2019 - CR00009165

        Dim strWhere As String = ""
        If (Not String.IsNullOrEmpty(pNumeroNf)) Then
            strWhere &= " and NF_IDE_NNF = '" & pNumeroNf & "'"
        End If
        'Ajuste do filtro para portaria
        If (Not String.IsNullOrEmpty(pPasta)) Then
            strWhere &= " and IDPASTA = " & pPasta & ""
            strWhere &= " and SAIDA_PORTARIA = '0' "
        End If
        If (Not String.IsNullOrEmpty(pUnidade)) Then
            strWhere &= " and descricao collate SQL_Latin1_General_Cp1251_CS_AS = '" & pUnidade & "'"
        End If
        If (Not String.IsNullOrEmpty(pFornecedor)) Then
            strWhere &= " and NF_EMIT_CNPJ = '" & pFornecedor.RemoveLetters() & "'"
        End If
        If (Not String.IsNullOrEmpty(pSituacao)) Then
            strWhere &= " and SITUACAO collate SQL_Latin1_General_Cp1251_CS_AS = '" & pSituacao & "'"
        End If
        If (Not String.IsNullOrEmpty(pTipoDocumento)) Then
            strWhere &= " and VNF_TIPO_DOCUMENTO collate SQL_Latin1_General_Cp1251_CS_AS = '" & pTipoDocumento & "'"
        End If
        If (Not String.IsNullOrEmpty(pTipoFrete)) Then
            strWhere &= " and TipoFrete collate SQL_Latin1_General_Cp1251_CS_AS = '" & pTipoFrete & "'"
        End If
        If (Not String.IsNullOrEmpty(pRelevante)) Then
            strWhere &= " and NFEREL collate SQL_Latin1_General_Cp1251_CS_AS = '" & pRelevante & "'"
        End If
        If (Not String.IsNullOrEmpty(pMaterialRecebido)) Then
            strWhere &= " and isnull(VNF_MATERIAL_RECEBIDO, 0) = " & pMaterialRecebido & ""
        End If
        If (Not String.IsNullOrEmpty(pStatusIntegracao)) Then
            strWhere &= " and STATUS_INTEGRACAO collate SQL_Latin1_General_Cp1251_CS_AS = '" & pStatusIntegracao & "'"
        End If

        If (Not String.IsNullOrEmpty(pTipoData)) Then
            If (pTipoData = "E") Then
                If (Not String.IsNullOrEmpty(pDataDe)) Then
                    strWhere &= " and NF_IDE_DHEMI >= '" & Convert.ToDateTime(pDataDe).ToString("yyyy-MM-dd") & " 00:00:00" & "'"
                End If
                If (Not String.IsNullOrEmpty(pDataAte)) Then
                    strWhere &= " and NF_IDE_DHEMI <= '" & Convert.ToDateTime(pDataAte).ToString("yyyy-MM-dd") & " 23:59:59" & "'"
                End If
            End If
            If (pTipoData = "C") Then
                If (Not String.IsNullOrEmpty(pDataDe)) Then
                    strWhere &= " and HORCHE >= '" & Convert.ToDateTime(pDataDe).ToString("yyyy-MM-dd") & " 00:00:00" & "'"
                End If
                If (Not String.IsNullOrEmpty(pDataAte)) Then
                    strWhere &= " and HORCHE <= '" & Convert.ToDateTime(pDataAte).ToString("yyyy-MM-dd") & " 23:59:59" & "'"
                End If
            End If
            If (pTipoData = "I") Then
                If (Not String.IsNullOrEmpty(pDataDe)) Then
                    strWhere &= " and DATA_INTEGRACAO >= '" & Convert.ToDateTime(pDataDe).ToString("yyyy-MM-dd") & " 00:00:00" & "'"
                End If
                If (Not String.IsNullOrEmpty(pDataAte)) Then
                    strWhere &= " and DATA_INTEGRACAO <= '" & Convert.ToDateTime(pDataAte).ToString("yyyy-MM-dd") & " 23:59:59" & "'"
                End If
            End If
        End If

        If (Not String.IsNullOrEmpty(pCodFornecedor)) Then
            'Marcio Spinosa - 16/08/2018 - CR00008351
            '            strWhere &= " and NF_EMIT_CNPJ = (SELECT DISTINCT TOP 1 cast(CNPJ as varchar) FROM TBFOR with(nolock) WHERE CODFOR ='" & pCodFornecedor & "')"
            Dim strCnpj As String
            strCnpj &= modSQL.ExecuteScalar("SELECT DISTINCT TOP 1 cast(CNPJ as varchar) FROM TBFOR with(nolock) WHERE CODFOR ='" & pCodFornecedor & "'")
            strWhere &= " and NF_EMIT_CNPJ like '%" & strCnpj & "%'"
            'Marcio Spinosa - 16/08/2018 - CR00008351 - Fim
        End If

        'Marcio Spinosa - 28/08/2018 - CR00008351
        If (Not String.IsNullOrEmpty(pNfeid)) Then
            strWhere &= " and NFEID = '" & pNfeid & "'"
        End If
        'Marcio Spinosa - 28/08/2018 - CR00008351 - Fim


        'Marcio Spinosa - 24/04/2019 - CR00009165
        If (Not String.IsNullOrEmpty(pTipoNotaFiscal)) Then
            strWhere &= " and NF_IDE_TPNF = '" & pTipoNotaFiscal & "'"
        End If
        'Marcio Spinosa - 24/04/2019 - CR00009165 - Fim

        Dim strSQLLista As String = "SELECT * FROM ( " &
                                    "   SELECT %TOP% * FROM vwRegistroFiscal" &
                                    "   WHERE " &
                                    "	    %WHERE% " &
                                    ") asoiudhasklfhkasjfhasjkfh " &
                                    "ORDER BY NF_IDE_DHEMI DESC"

        If (strWhere.Length > 5) Then
            Dim query As String = strSQLLista.Replace("%WHERE%", strWhere.Substring(5))

            Dim dtResult = modSQL.Fill(query.Replace("%TOP%", "TOP " & (pQtdRegistros + 1)))

            pExceedQtd = False
            If dtResult.Rows.Count > pQtdRegistros Then
                pExceedQtd = True
                dtResult.Rows.RemoveAt(dtResult.Rows.Count - 1)
            End If

            Return dtResult
        Else
            Return New DataTable()
        End If
    End Function

    Public Function GetPendentes() As DataTable
        Return modSQL.Fill("select * from vwFILA order by DATA_ENVIO desc")
    End Function

    Public Function GetByID(ByVal NfeID As String, ByVal IsConsultarPedido As Boolean, Optional ByVal isSomenteLeitura As Boolean = False) As modNF
        Dim strMensagem As String = ""
        Return GetByID(NfeID, IsConsultarPedido, strMensagem, isSomenteLeitura)
    End Function

    Public Function GetByID(ByVal NfeID As String, ByVal IsConsultarPedido As Boolean, ByRef Mensagem As String, Optional ByVal isSomenteLeitura As Boolean = False) As modNF
        Dim objVerificar As New modVerificar()
        Return objVerificar.Validar(NfeID, String.Empty, False, IsConsultarPedido, modVerificar.TipoProcessamento.Aplicacao, Uteis.LogonName(), Mensagem, isSomenteLeitura)
    End Function

    Public Function GetDanfePath() As String
        modSQL.CommandText = "select VALOR from TbPAR where PARAMETRO = 'PASTA_TEMPORARIA_XML_DA_APLICACAO'"
        Dim pFileName = modSQL.ExecuteScalar(modSQL.CommandText)

        'Trecho comentado devido a bug no Visual Studio 2015.
        '#If (DEBUG) Then
        If (Debugger.IsAttached) Then
            pFileName = "C:\Temp"
        End If
        '#End If

        Return pFileName & "\"
    End Function

    Public Function GetUnidadeMetso() As DataTable
        Dim dtUnidades As New DataTable
        Dim selectUnidades As String = "SELECT DISTINCT DESCRICAO FROM TbPlantaCnpj"
        dtUnidades = modSQL.Fill(selectUnidades)
        Return dtUnidades
    End Function
    ''' <summary>
    ''' Retorna o historico das validações e o usuário que validou
    ''' </summary>
    ''' <param name="NfeID"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>Foi ajustado no sql o retorno dos dados do funcionário</example>
    ''' 
    Public Function GetHistoricoValidacoes(ByVal NfeID As String) As DataTable
        'Marcio Spinosa - 28/05/2018 - CR00008351
        'Dim cmdText As String = "SELECT DISTINCT TOP 10 C.com_data_hora, com_usuario  FROM com_comparacao C " &
        '"WHERE com_nfe_id = '" & NfeID & "' ORDER BY com_data_hora desc"

        Dim cmdText As String = "SELECT DISTINCT TOP 10 C.com_data_hora, U.USUNOMUSU FROM com_comparacao C " &
                                "INNER JOIN TBUSUARIO U ON U.USUCODUSU = C.COM_USUARIO WHERE com_nfe_id = '" & NfeID & "' ORDER BY com_data_hora desc"
        'Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
        Return modSQL.Fill(cmdText)
    End Function

    Public Function GetDetalheValidacoes(ByVal pData As DateTime, pNfeID As String) As DataTable
        Dim cmdText As String = "SELECT DISTINCT id_comparacao,com_data_hora,com_nfe_id, com_item_id,com_campo,com_valor_df,com_valor_sap, pedcom, iteped " &
                                "FROM com_comparacao " &
                                "WHERE com_nfe_id = '" & pNfeID & "' " &
                                " and com_data_hora = '" & pData.ToString("yyyy-MM-dd HH:mm:ss") & "' " &
                                "ORDER BY com_item_id,com_campo "

        If (pData = New DateTime) Then
            Return New DataTable()
        Else
            Return modSQL.Fill(cmdText)
        End If
    End Function

    Public Function GetTabelaValidacoes(ByVal pData As DateTime, pNfeID As String) As String
        Dim dt As New DataTable()
        dt = GetDetalheValidacoes(pData, pNfeID)

        Dim strGrupoComparacoes As String = "<div class=""panel panel-default""> " &
                                            "   <div class=""panel-heading""> " &
                                            "       <h4 class=""panel-title""> " &
                                            "           <a data-toggle=""collapse"" data-parent=""#accordion"" href=""#item_%ITEM%"" class=""collapsed""> " &
                                            "               <i class=""fa fa-lg fa-angle-down pull-right""></i> " &
                                            "               <i class=""fa fa-lg fa-angle-up pull-right""></i> " &
                                            "               %TITULO% " &
                                            "           </a> " &
                                            "       </h4> " &
                                            "   </div> " &
                                            "   <div id=""item_%ITEM%"" class=""panel-collapse collapse""> " &
                                            "       <div class=""panel-body no-padding"">  " &
                                            "           %TABELA% " &
                                            "       </div> " &
                                            "   </div> " &
                                            "</div> "

        Dim strTabelaComparacoes As String = "<table id=""dttComparacoes"" datatables_fixedheader=""top"" datatables_fixedheader_offsettop=""60"" class=""table table-striped table-bordered table-hover table-click"" width=""100%""> " &
                                             "     <thead> " &
                                             "         <tr> " &
                                             "             <th>Condição</th> " &
                                             "             <th>Nota Fiscal</th> " &
                                             "             <th>Pedido SAP</th> " &
                                             "         </tr> " &
                                             "     </thead> " &
                                             "     <tbody> " &
                                             "         %LINHAS% " &
                                             "     </tbody> " &
                                             "</table> "

        Dim strSemInformacao As String = "<tr><td colspan='3' class='center'>não existe informação</td></tr>"

        Dim intItemNf As Integer = 0
        Dim strTabela As String = ""
        Dim strGrupo As String = ""
        Dim stbLinha As New StringBuilder()
        Dim stbRetorno As New StringBuilder()

        If dt.Rows.Count = 0 Then
            Return "<span>Nenhum registro encontrado</span>"
        End If

        For i As Integer = 0 To dt.Rows.Count - 1
            If intItemNf = 0 OrElse intItemNf <> Convert.ToInt32(dt.Rows(i)("com_item_id").ToString()) Then
                intItemNf = Convert.ToInt32(dt.Rows(i)("com_item_id").ToString())
            End If

            stbLinha.Append("<tr>")
            stbLinha.Append("<td>" + dt.Rows(i)("com_campo").ToString() + "</td>")
            stbLinha.Append("<td>" + dt.Rows(i)("com_valor_df").ToString() + "</td>")
            stbLinha.Append("<td>" + dt.Rows(i)("com_valor_sap").ToString() + "</td>")
            stbLinha.Append("</tr>")

            If i = dt.Rows.Count - 1 OrElse intItemNf <> Convert.ToInt32(dt.Rows(i + 1)("com_item_id").ToString()) Then
                strTabela = strTabelaComparacoes.Replace("%LINHAS%", stbLinha.ToString())
                strGrupo = strGrupoComparacoes.Replace("%ITEM%", intItemNf.ToString().PadLeft(3, "0"))
                strGrupo = strGrupo.Replace("%TITULO%", "Item " + intItemNf.ToString().PadLeft(3, "0") & " - Pedido " & dt.Rows(i)("pedcom").ToString() + " item " + dt.Rows(i)("iteped").ToString())
                strGrupo = strGrupo.Replace("%TABELA%", strTabela)
                stbRetorno.Append(strGrupo)
                stbLinha.Clear()
            End If
        Next

        Return stbRetorno.ToString()
    End Function

    Public Function GetChartRecebimentoNF() As Dictionary(Of String, Object)
        Dim strQuery As String = "SELECT " &
                                 "	HORA, " &
                                 "	QTD " &
                                 "FROM " &
                                 "	( " &
                                 "		SELECT top 11 " &
                                 "			REPLACE(STR(datepart(HH, datval), 2), SPACE(1), '0') + ':00' as 'HORA', " &
                                 "			count(*) AS 'QTD' " &
                                 "		FROM  " &
                                 "			tbnfe " &
                                 "		WHERE " &
                                 "			datval >= CONVERT(date, '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "') " &
                                 "		GROUP BY " &
                                 "			datepart(HH, datval) " &
                                 "		ORDER BY " &
                                 "			datepart(HH, datval) desc " &
                                 "	) AS entrada_nf " &
                                 "ORDER BY " &
                                 "	HORA "

        Dim dttDados = New DataTable()
        dttDados = modSQL.Fill(strQuery)

        Dim strChart As String = String.Empty
        Dim strLegenda As String = String.Empty
        For index = 0 To dttDados.Rows.Count - 1
            strChart &= "[" & index.ToString() & ", " & dttDados.Rows(index)("QTD") & "], "
            strLegenda &= "[" & index.ToString() & ", """ & dttDados.Rows(index)("HORA") & """], "
        Next

        If (strChart.Length > 2) Then
            strChart = "[" & strChart.Substring(0, strChart.Length - 2) & "]"
        Else
            strChart = "[]"
        End If

        If (strLegenda.Length > 2) Then
            strLegenda = "[" & strLegenda.Substring(0, strLegenda.Length - 2) & "]"
        Else
            strLegenda = "[]"
        End If

        Dim objRetorno = New Dictionary(Of String, Object)
        objRetorno.Add("dados", strChart)
        objRetorno.Add("legenda", strLegenda)

        Return objRetorno
    End Function

    Public Function GetDocumentosRecebidos() As DataTable
        Dim strQuery As String = "SELECT top 8  " &
                                 "	DATVAL,  " &
                                 "	tbnfe.NFEID,  " &
                                 "	VNF_TIPO_DOCUMENTO,  " &
                                 "	NF_IDE_NNF,  " &
                                 "	/*dbo.sp_limitar_string(NF_EMIT_XNOME, 25) as 'NF_NOME_EMITENTE',*/  " &
                                 "  CASE WHEN LEN(NF_EMIT_XNOME) <= 25 " &
                                 "       THEN NF_EMIT_XNOME " &
                                 "       ELSE LEFT(NF_EMIT_XNOME, 25) + '...' " &
                                 "  END AS 'NF_NOME_EMITENTE', " &
                                 "	(select count(*) from tbdoc_item_nfe where tbdoc_item_nfe.nfeid = tbnfe.NFEID) as NF_QTD_ITENS,  " &
                                 "	SITUACAO,  " &
                                 "	/*dbo.sp_check_prioridade_alta_documento(tbnfe.NFEID) as 'PRIORIDADE_ALTA'*/  " &
                                 "   (SELECT " &
                                 "		CASE WHEN count(1) = 0 THEN 'NÃO' ELSE 'SIM' END " &
                                 "	FROM " &
                                 "		TbDOC_ITEM with (nolock) " &
                                 "		inner join TbJUN with (nolock) " &
                                 "		on TbDOC_ITEM.nfeid = TbJUN.nfeid " &
                                 "		inner join pid_priorizacao_item_pedido with (nolock) " &
                                 "		on TbJUN.PEDCOM = pid_pedido and TbJUN.ITEPED = pid_item " &
                                 "	WHERE " &
                                 "		TbDOC_ITEM.nfeid = TBNFE.NFEID) as 'PRIORIDADE_ALTA' " &
                                 "FROM  " &
                                 "	tbnfe  " &
                                 "	inner join tbdoc_cab on tbnfe.nfeid = tbdoc_cab.nfeid  " &
                                 "	inner join tbdoc_cab_nfe on tbnfe.nfeid = tbdoc_cab_nfe.nfeid  " &
                                 "GROUP BY  " &
                                 "	DATVAL,  " &
                                 "	tbnfe.NFEID,  " &
                                 "	VNF_TIPO_DOCUMENTO,  " &
                                 "	NF_IDE_NNF,  " &
                                 "	NF_EMIT_XNOME,  " &
                                 "	SITUACAO  " &
                                 "ORDER BY  " &
                                 "	DATVAL desc "

        Return modSQL.Fill(strQuery)
    End Function

    Public Function GetQtdNotasPendentes() As Integer
        Dim strQuery As String = "SELECT  " &
                                 "	count(*) " &
                                 "FROM " &
                                 "	tbNfe " &
                                 "	inner join tbdoc_cab_nfe " &
                                 "	on tbnfe.nfeid = tbdoc_cab_nfe.nfeid " &
                                 "WHERE " &
                                 "	situacao = 'PENDENTE' " &
                                 "	and NF_IDE_DHEMI >= dateadd(dd, -60, '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "') "

        Return Convert.ToInt32(modSQL.ExecuteScalar(strQuery))
    End Function

    Public Function GetNotasFiscaisCte(ByVal chave_cte As String) As DataTable
        Dim strQuery As String = " SELECT " &
                                 "      TbDOC_ITEM_CTE.CT_INFNFE_CHAVE as 'NFEID', " &
                                 "      isnull(TbDOC_CAB_NFE.NF_IDE_NNF, '---') as 'NF_IDE_NNF', " &
                                 "      /*dbo.sp_limitar_string(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) as 'NF_EMIT_XNOME',*/ " &
                                 "      CASE WHEN LEN(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---')) <= 40 " &
                                 "           THEN isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---') " &
                                 "           ELSE LEFT(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) + '...' " &
                                 "      END AS 'NF_EMIT_XNOME', " &
                                 "      isnull(convert(varchar(255), TbDOC_CAB_NFE.NF_IDE_DHEMI, 103), '---') as 'NF_IDE_DHEMI', " &
                                 "      isnull(TbNFE.SITUACAO, 'NÃO ENCONTRADO') as 'SITUACAO' " &
                                 " FROM " &
                                 "      TbDOC_ITEM_CTE " &
                                 "      left join TbNFE on TbDOC_ITEM_CTE.ct_infnfe_chave = TbNFE.nfeid " &
                                 "      left join TbDOC_CAB_NFE on TbNFE.nfeid = TbDOC_CAB_NFE.nfeid " &
                                 " WHERE " &
                                 "      TbDOC_ITEM_CTE.nfeid = '" & chave_cte & "'"

        Return modSQL.Fill(strQuery)
    End Function

    Public Function GetDocumentosRelacionados(ByVal nfeid As String) As DataTable
        'Marcio Spinosa - 23/01/2020 - Ajuste no desempenho da query que a procura esta pesada (inserção do inner no lugar do left) 
        'Dim strQuery As String = "/* CTE UTILIZADO PARA TRANSPORTAR A NOTA FISCAL */ " &
        '                         "SELECT " &
        '                         "	  TbDOC_CAB.VNF_TIPO_DOCUMENTO, " &
        '                         "	  TbDOC_ITEM_CTE.NFEID, " &
        '                         "	  TbDOC_CAB_NFE.NF_IDE_NNF, " &
        '                         "	  isnull(convert(varchar(255), TbDOC_CAB_NFE.NF_IDE_DHEMI, 103), '---') as 'NF_IDE_DHEMI', " &
        '                         "	  /*dbo.sp_limitar_string(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) as 'NF_EMIT_XNOME',*/ " &
        '                         "    CASE WHEN LEN(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---')) <= 40 " &
        '                         "         THEN isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---') " &
        '                         "         ELSE LEFT(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) + '...' " &
        '                         "    END AS 'NF_EMIT_XNOME', " &
        '                         "	  TbNFE.SITUACAO " &
        '                         "FROM " &
        '                         "	  TbDOC_ITEM_CTE " &
        '                         "	  left join TbNFE on TbDOC_ITEM_CTE.nfeid = TbNFE.nfeid " &
        '                         "	  left join TbDOC_CAB on TbNFE.nfeid = TbDOC_CAB.nfeid " &
        '                         "	  left join TbDOC_CAB_NFE on TbNFE.nfeid = TbDOC_CAB_NFE.nfeid  " &
        '                         "WHERE " &
        '                         "	  TbDOC_ITEM_CTE.CT_INFNFE_CHAVE = '" & nfeid & "' " &
        '                         "UNION ALL  " &
        '                         "/* NOTAS FISCAIS TRANSPORTADAS PELO CTE */ " &
        '                         "SELECT " &
        '                         "	  isnull(TbDOC_CAB.VNF_TIPO_DOCUMENTO, 'NFE'), " &
        '                         "	  TbDOC_ITEM_CTE.ct_infnfe_chave, " &
        '                         "	  isnull(TbDOC_CAB_NFE.NF_IDE_NNF, '---'),  " &
        '                         "	  isnull(convert(varchar(255), TbDOC_CAB_NFE.NF_IDE_DHEMI, 103), '---') as 'NF_IDE_DHEMI', " &
        '                         "	  /*dbo.sp_limitar_string(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) as 'NF_EMIT_XNOME',*/ " &
        '                         "    CASE WHEN LEN(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---')) <= 40 " &
        '                         "         THEN isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---') " &
        '                         "         ELSE LEFT(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) + '...' " &
        '                         "    END AS 'NF_EMIT_XNOME', " &
        '                         "	  isnull(TbNFE.SITUACAO, 'NÃO ENCONTRADO') as 'SITUACAO' " &
        '                         "FROM " &
        '                         "	  TbDOC_ITEM_CTE " &
        '                         "	  left join TbNFE on TbDOC_ITEM_CTE.ct_infnfe_chave = TbNFE.nfeid " &
        '                         "	  left join TbDOC_CAB on TbNFE.nfeid = TbDOC_CAB.nfeid " &
        '                         "	  left join TbDOC_CAB_NFE on TbNFE.nfeid = TbDOC_CAB_NFE.nfeid " &
        '                         "WHERE " &
        '                         "	  TbDOC_ITEM_CTE.nfeid = '" & nfeid & "' " &
        '                         "UNION ALL " &
        '                         "/* NOTAS REFERENCIADAS */ " &
        '                         "SELECT  " &
        '                         "	  TbDOC_CAB.VNF_TIPO_DOCUMENTO, " &
        '                         "	  TbDOC_ITEM_CTE.NFEID, " &
        '                         "	  TbDOC_CAB_NFE.NF_IDE_NNF, " &
        '                         "	  isnull(convert(varchar(255), TbDOC_CAB_NFE.NF_IDE_DHEMI, 103), '---') as 'NF_IDE_DHEMI', " &
        '                         "	  /*dbo.sp_limitar_string(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) as 'NF_EMIT_XNOME',*/ " &
        '                         "    CASE WHEN LEN(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---')) <= 40 " &
        '                         "         THEN isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---') " &
        '                         "         ELSE LEFT(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) + '...' " &
        '                         "    END AS 'NF_EMIT_XNOME', " &
        '                         "	  TbNFE.SITUACAO " &
        '                         "FROM " &
        '                         "	  TbDOC_ITEM_CTE " &
        '                         "	  left join TbNFE on TbDOC_ITEM_CTE.nfeid = TbNFE.nfeid " &
        '                         "	  left join TbDOC_CAB on TbNFE.nfeid = TbDOC_CAB.nfeid " &
        '                         "	  left join TbDOC_CAB_NFE on TbNFE.nfeid = TbDOC_CAB_NFE.nfeid " &
        '                         "	  left join TbDOC_CAB_NFE_REF on TbNFE.nfeid = TbDOC_CAB_NFE_REF.nfeid " &
        '                         "WHERE " &
        '                         "	  TbDOC_CAB_NFE_REF.NF_NFREF_REFNFE = '" & nfeid & "' " &
        '                         "	  OR TbDOC_CAB_NFE_REF.NF_NFREF_REFCTE = '" & nfeid & "'  "

        Dim strQuery As String = "/* CTE UTILIZADO PARA TRANSPORTAR A NOTA FISCAL */ " &
                                 "SELECT " &
                                 "	  TbDOC_CAB.VNF_TIPO_DOCUMENTO, " &
                                 "	  TbDOC_ITEM_CTE.NFEID, " &
                                 "	  TbDOC_CAB_NFE.NF_IDE_NNF, " &
                                 "	  isnull(convert(varchar(255), TbDOC_CAB_NFE.NF_IDE_DHEMI, 103), '---') as 'NF_IDE_DHEMI', " &
                                 "	  /*dbo.sp_limitar_string(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) as 'NF_EMIT_XNOME',*/ " &
                                 "    CASE WHEN LEN(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---')) <= 40 " &
                                 "         THEN isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---') " &
                                 "         ELSE LEFT(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) + '...' " &
                                 "    END AS 'NF_EMIT_XNOME', " &
                                 "	  TbNFE.SITUACAO " &
                                 "FROM " &
                                 "	  TbDOC_ITEM_CTE " &
                                 "	  inner join TbNFE on TbDOC_ITEM_CTE.nfeid = TbNFE.nfeid " &
                                 "	  inner join TbDOC_CAB on TbNFE.nfeid = TbDOC_CAB.nfeid " &
                                 "	  inner join TbDOC_CAB_NFE on TbNFE.nfeid = TbDOC_CAB_NFE.nfeid  " &
                                 "WHERE " &
                                 "	  TbDOC_ITEM_CTE.CT_INFNFE_CHAVE = '" & nfeid & "' " &
                                 "UNION ALL  " &
                                 "/* NOTAS FISCAIS TRANSPORTADAS PELO CTE */ " &
                                 "SELECT " &
                                 "	  isnull(TbDOC_CAB.VNF_TIPO_DOCUMENTO, 'NFE'), " &
                                 "	  TbDOC_ITEM_CTE.ct_infnfe_chave, " &
                                 "	  isnull(TbDOC_CAB_NFE.NF_IDE_NNF, '---'),  " &
                                 "	  isnull(convert(varchar(255), TbDOC_CAB_NFE.NF_IDE_DHEMI, 103), '---') as 'NF_IDE_DHEMI', " &
                                 "	  /*dbo.sp_limitar_string(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) as 'NF_EMIT_XNOME',*/ " &
                                 "    CASE WHEN LEN(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---')) <= 40 " &
                                 "         THEN isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---') " &
                                 "         ELSE LEFT(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) + '...' " &
                                 "    END AS 'NF_EMIT_XNOME', " &
                                 "	  isnull(TbNFE.SITUACAO, 'NÃO ENCONTRADO') as 'SITUACAO' " &
                                 "FROM " &
                                 "	  TbDOC_ITEM_CTE " &
                                 "	  inner join TbNFE on TbDOC_ITEM_CTE.ct_infnfe_chave = TbNFE.nfeid " &
                                 "	  inner join TbDOC_CAB on TbNFE.nfeid = TbDOC_CAB.nfeid " &
                                 "	  inner join TbDOC_CAB_NFE on TbNFE.nfeid = TbDOC_CAB_NFE.nfeid " &
                                 "WHERE " &
                                 "	  TbDOC_ITEM_CTE.nfeid = '" & nfeid & "' " &
                                 "UNION ALL " &
                                 "/* NOTAS REFERENCIADAS */ " &
                                 "SELECT  " &
                                 "	  TbDOC_CAB.VNF_TIPO_DOCUMENTO, " &
                                 "	  TbDOC_ITEM_CTE.NFEID, " &
                                 "	  TbDOC_CAB_NFE.NF_IDE_NNF, " &
                                 "	  isnull(convert(varchar(255), TbDOC_CAB_NFE.NF_IDE_DHEMI, 103), '---') as 'NF_IDE_DHEMI', " &
                                 "	  /*dbo.sp_limitar_string(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) as 'NF_EMIT_XNOME',*/ " &
                                 "    CASE WHEN LEN(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---')) <= 40 " &
                                 "         THEN isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---') " &
                                 "         ELSE LEFT(isnull(TbDOC_CAB_NFE.NF_EMIT_XNOME, '---'), 40) + '...' " &
                                 "    END AS 'NF_EMIT_XNOME', " &
                                 "	  TbNFE.SITUACAO " &
                                 "FROM " &
                                 "	  TbDOC_ITEM_CTE " &
                                 "	  inner join TbNFE on TbDOC_ITEM_CTE.nfeid = TbNFE.nfeid " &
                                 "	  inner join TbDOC_CAB on TbNFE.nfeid = TbDOC_CAB.nfeid " &
                                 "	  inner join TbDOC_CAB_NFE on TbNFE.nfeid = TbDOC_CAB_NFE.nfeid " &
                                 "	  inner join TbDOC_CAB_NFE_REF on TbNFE.nfeid = TbDOC_CAB_NFE_REF.nfeid " &
                                 "WHERE " &
                                 "	  TbDOC_CAB_NFE_REF.NF_NFREF_REFNFE = '" & nfeid & "' " &
                                 "	  OR TbDOC_CAB_NFE_REF.NF_NFREF_REFCTE = '" & nfeid & "'  "


        Return modSQL.Fill(strQuery)
        'Marcio Spinosa - 23/01/2020 - Ajuste no desempenho da query que a procura esta pesada (inserção do inner no lugar do left) - Fim
    End Function

    Public Function GetXml(ByVal NfeID As String) As String
        Dim objVerificar As New modVerificar()
        Return modSQL.ExecuteScalar("SELECT vnf_conteudo_xml FROM tbdoc_cab WHERE nfeid = '" & NfeID & "'").ToString()
    End Function

    Public Function GetDivergencias(ByVal NfeID As String) As DataTable
        Dim cmdText As String = "SELECT * FROM vwDivergencia where NFEID = '" & NfeID & "' order by ITENFE, CAMPO, SITUACAO, DATLOG desc"
        Return modSQL.Fill(cmdText)
    End Function

    Public Function GetMensagens(ByVal NfeID As String) As DataTable
        Dim cmdText As String = "select DATENV, EMAIL, SITUACAO, MENENV from TbMEN where NFEID = '" & NfeID & "' order by DATENV desc"
        Return modSQL.Fill(cmdText)
    End Function

    Public Function GetItens(ByVal NfeID As String) As modNFItem()
        Dim objVerificar As New modVerificar()
        Return GetByID(NfeID, False).ITENS_NF.ToArray()
    End Function

    Public Function SetPurchasedOrderInfo(ByVal NfeID As String, ByVal ItemNotaFiscal As String, ByVal Pedido As String, ByVal ItemPedido As String, ByVal Usuario As String) As String
        '--> SE JÁ FOI REALIZADA A INTEGRAÇÃO (MIGO/MIRO) DESTA NOTA FISCAL, ESTA ALTERAÇÃO NÃO PODE SER REALIZADA
        If (GetStatusIntegracao(NfeID) = "CONCLUÍDO") Then
            Return "Não é permitido fazer a associação desta NF pois o processo de integração foi concluído."
        End If

        Dim cmdText As String = "SELECT COUNT(*) FROM TbJUN where NFEID = '" & NfeID & "' and ITENFE = " & ItemNotaFiscal
        If String.IsNullOrEmpty(ItemPedido) Then
            ItemPedido = "0"
        End If

        If Convert.ToInt32(modSQL.ExecuteScalar(cmdText)) > 0 Then
            cmdText = "update TbJUN set PEDCOM = '" & Pedido & "', ITEPED = " & ItemPedido & ", ITEVAL = 'N', CODUSU = '" & Usuario & "', CODCOM = '', MOTIVO = '', NCMPED = '' WHERE NFEID = '" & NfeID & "' and ITENFE = " & ItemNotaFiscal
            modSQL.ExecuteNonQuery(cmdText)
        Else
            modSQL.CommandText = "select VALOR from TbPAR where PARAMETRO = 'PASTA_TEMPORARIA_XML_DO_SERVICO'"
            Dim pFileName As String = modSQL.ExecuteScalar(modSQL.CommandText) & "\XML.XML"

            Dim objVerificar As New modVerificar()
            Dim objNF As New modNF()
            objNF = GetByID(NfeID, False)

            Dim objItemNF As modNFItem
            objItemNF = objNF.ITENS_NF.Where(Function(x) x.NF_PROD_ITEM = ItemNotaFiscal).FirstOrDefault()

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
            sqlparams.Add(modSQL.AddSqlParameter("PEDCOM", SqlDbType.VarChar, Pedido))
            sqlparams.Add(modSQL.AddSqlParameter("ITEPED", SqlDbType.Decimal, ItemPedido))
            sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, objNF.VNF_CHAVE_ACESSO))
            sqlparams.Add(modSQL.AddSqlParameter("ITENFE", SqlDbType.Decimal, objItemNF.NF_PROD_ITEM))
            sqlparams.Add(modSQL.AddSqlParameter("ITEVAL", SqlDbType.VarChar, "N"))
            sqlparams.Add(modSQL.AddSqlParameter("CODUSU", SqlDbType.VarChar, Usuario))
            sqlparams.Add(modSQL.AddSqlParameter("CODCOM", SqlDbType.VarChar, objNF.SAP_DETAILS.PURCHASING_GROUP))
            sqlparams.Add(modSQL.AddSqlParameter("MOTIVO", SqlDbType.VarChar, String.Empty))
            sqlparams.Add(modSQL.AddSqlParameter("NCMNFE", SqlDbType.VarChar, objItemNF.NF_PROD_NCM))
            sqlparams.Add(modSQL.AddSqlParameter("NCMPED", SqlDbType.VarChar, objItemNF.SAP_ITEM_DETAILS.NCM_CODE))
            objItemNF.VNF_CODJUN = Convert.ToDecimal(modSQL.ExecuteScalarParams(strQuery, sqlparams))
        End If

        Return String.Empty
    End Function

    Public Function SetReferencedOrderInfo(ByVal NfeID As String, ByVal NFNumero As String, ByVal NFSerie As String, ByVal Usuario As String) As String

        '--> Verifica se a nota fiscal referenciada é mandatória
        Dim cfop As String = modSQL.ExecuteScalar("SELECT TOP 1 NF_PROD_CFOP FROM TbDOC_ITEM_NFE WHERE nfeid = '" + NfeID + "'")
        Dim objVerificar As New modVerificar()
        If Not (objVerificar.IsMandatoryNotaFiscalReferenciada(cfop)) Then
            Return ""
        End If

        '--> SE JÁ FOI REALIZADA A INTEGRAÇÃO (MIGO/MIRO) DESTA NOTA FISCAL, ESTA ALTERAÇÃO NÃO PODE SER REALIZADA
        If (GetStatusIntegracao(NfeID) = "CONCLUÍDO") Then
            Return "Não é permitido inserir referencia para esta NF pois o processo de integração foi concluído."
        End If

        '--> BUSCA PELA NOTA REFERENCIADA USANDO O NUMERO E SERIE INFORMADO, SENDO QUE O CNPJ DO EMISSOR DEVE SER O MESMO QUE A NOTA EM ASSOCIAÇÃO
        modSQL.CommandText = "SELECT refnf.NF_IDE_DHEMI                    " & _
                             "  FROM TbDOC_CAB_NFE refnf                   " & _
                             " INNER JOIN TbDOC_CAB_NFE nf                 " & _
                             "    ON nf.NF_EMIT_CNPJ = refnf.NF_EMIT_CNPJ  " & _
                             " WHERE nf.nfeid = '" & NfeID & "'            " & _
                             " AND RIGHT( '000000000' + CONVERT(VARCHAR(9), refnf.NF_IDE_NNF), 9) = '" & NFNumero.PadLeft(9, "0") & "' " & _
                             " AND RIGHT( '000' + CONVERT(VARCHAR(3), refnf.NF_IDE_SERIE), 3) = '" & NFSerie.PadLeft(3, "0") & "' "

        Dim dt As DataTable = modSQL.Fill(modSQL.CommandText)
        If dt.Rows.Count = 1 Then
            If Not (String.IsNullOrWhiteSpace(dt.Rows(0)("NF_IDE_DHEMI").ToString)) Then
                Dim REFDHEMI As Date = CDate(dt.Rows(0)("NF_IDE_DHEMI").ToString)
                modSQL.ExecuteNonQuery("UPDATE TbDOC_CAB_NFE SET NF_NFREF_REFNNF = '" & NFNumero & "', NF_NFREF_REFSerie = '" & NFSerie & "', NF_NFREF_REFDHEMI = '" & REFDHEMI.ToString("yyyy-MM-dd HH:mm:ss") & "'  WHERE NFEID = '" & NfeID & "'")
            Else
                Return "A nota referenciada não possui data de emissão."
            End If
        Else
            If dt.Rows.Count = 0 Then
                Return "A nota referenciada não existe no VNF."
            End If
            Return "Existem multiplas notas com esse número, serie e cnpj emissor."
        End If

        Return String.Empty
    End Function

    Public Function Validar(ByVal NfeID As String, ByVal FileName As String, ByVal Usuario As String, ByRef Mensagem As String) As modNF
        Dim objVerificar As New modVerificar()
        Dim objNF As New modNF()

        If (GetStatusIntegracao(NfeID) = "CONCLUÍDO") Then
            Mensagem = "Não é permitido fazer a associação desta NF pois o processo de integração foi concluído."
            Return GetByID(NfeID, False)
        End If

        objNF = objVerificar.Validar(NfeID, FileName, True, True, modVerificar.TipoProcessamento.Aplicacao, Usuario, Mensagem)
        BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.VerificarDocumento, "Fez uma nova associação da " & objNF.VNF_TIPO_DOCUMENTO & objNF.NF_IDE_NNF & ". O documento está com status " & objNF.VNF_STATUS.ToUpper(), NfeID)
        Return objNF
    End Function
    ''' <summary>
    ''' Cancela a nota na tabela TBNFE
    ''' </summary>
    ''' <param name="NfeID"></param>
    ''' <param name="Usuario"></param>
    ''' <param name="IgnoreEmail"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>Foi ajustado no sql o retorno dos dados do funcionário</example>
    Public Function Cancelar(ByVal NfeID As String, ByVal Usuario As String, ByVal IgnoreEmail As Boolean) As String
        If Not String.IsNullOrEmpty(Usuario) And modSQL.ExecuteScalar("SELECT acesitace FROM TbAcesso where acecodtel = 'ANUL' and acecodusu = '" & Usuario & "'") <> "LIBERADO" Then
            Return "Você não tem autorização para cancelar NF!" & vbNewLine & "Favor entrar em contato com o seu supervisor para anular esta divergência."
        End If
        'Marcio Spinosa - 28/05/2018 - CR00008351
        'Dim BODY As String = "DOC-e foi marcado como CANCELADO PELO FORNECEDOR pelo usuário " & Uteis.UserName() & " no dia " & DateTime.Now().ToString("dd/MM/yyyy") & " às " & DateTime.Now().ToString("HH:mm")
        Dim BODY As String = "DOC-e foi marcado como CANCELADO PELO FORNECEDOR pelo usuário " & getUserNameByLogon(Uteis.LogonName()) & " no dia " & DateTime.Now().ToString("dd/MM/yyyy") & " às " & DateTime.Now().ToString("HH:mm")
        'Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
        Dim cmdText As String = "insert into TbMEN values ('" & NfeID & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '', 'DOC-e CANCELADO', '" & BODY & "', '')" 'Marcio Spinosa - 02/01/2019
        modSQL.ExecuteNonQuery(cmdText)

        cmdText = "update TbLOG set SITUACAO = 'INATIVO', MOTIVO = 'NF CANCELADA POR FORNECEDOR' where SITUACAO = 'ATIVO' and NFEID = '" & NfeID & "'"
        modSQL.ExecuteNonQuery(cmdText)

        cmdText = "update TbNFE set NFECAN = 'S', SITUACAO = 'CANCELADA', USUCAN = '" & Uteis.LogonName() & "' where NFEID = '" & NfeID & "'"
        modSQL.ExecuteNonQuery(cmdText)

        BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.Cancelamento, "Cancelou a " & GetNumeroDocumento(NfeID), NfeID)

        'VERIFICAR SE A INTEGRAÇÃO JÁ FOI CONCLUIDA E CASO SIM ENVIAR EMAIL 
        If Not IgnoreEmail And GetStatusIntegracao(NfeID) = "CONCLUÍDO" Then
            Dim vObjModVerifica As New modVerificar
            vObjModVerifica.EnviarMsgCancelamento(NfeID)
        End If

        Return String.Empty

    End Function
    ''' <summary>
    ''' Desfaz o cancelamento da nota 
    ''' </summary>
    ''' <param name="NfeID"></param>
    ''' <remarks></remarks>
    ''' <example>Foi ajustado no sql o retorno dos dados do funcionário</example>
    Public Sub DesfazerCancelamento(ByVal NfeID As String)
        'Marcio Spinosa - 28/05/2018 - CR00008351
        '        Dim BODY As String = "Cancelamento do DOC-e DESFEITO pelo usuário " & Uteis.UserName() & " no dia " & DateTime.Now().ToString("dd/MM/yyyy") & " às " & DateTime.Now().ToString("HH:mm")
        Dim BODY As String = "Cancelamento do DOC-e DESFEITO pelo usuário " & getUserNameByLogon(Uteis.LogonName()) & " no dia " & DateTime.Now().ToString("dd/MM/yyyy") & " às " & DateTime.Now().ToString("HH:mm")
        'Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
        Dim cmdText As String = "insert into TbMEN values ('" & NfeID & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '', 'CANCELAMENTO DESFEITO DE DOC-e', '" & BODY & "', '')"
        modSQL.ExecuteNonQuery(cmdText)
        modSQL.ExecuteNonQuery("UPDATE TBNFE SET SITUACAO = 'PENDENTE', REPROCESSAR = 'S' WHERE NFEID = '" & NfeID & "'")

        BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.CancelamentoDesfeito, "Desfez o cancelamento da " & GetNumeroDocumento(NfeID), NfeID)
    End Sub
    ''' <summary>
    ''' Inseri o status de recusada na nota conforme validação
    ''' </summary>
    ''' <param name="NfeID"></param>
    ''' <param name="Justificativa"></param>
    ''' <remarks></remarks>
    ''' <example>Foi ajustado no sql o retorno dos dados do funcionário</example>
    Public Sub Recusar(ByVal NfeID As String, ByVal Justificativa As String)
        'Marcio Spinosa - 28/05/2018 - CR00008351
        'Dim BODY As String = "DOC-e foi marcado como RECUSADO pelo usuário " & Uteis.UserName() & " no dia " & DateTime.Now().ToString("dd/MM/yyyy") & " às " & DateTime.Now().ToString("HH:mm") & Environment.NewLine & " Justificativa: " & Justificativa
        Dim BODY As String = "DOC-e foi marcado como RECUSADO pelo usuário " & getUserNameByLogon(Uteis.LogonName()) & " no dia " & DateTime.Now().ToString("dd/MM/yyyy") & " às " & DateTime.Now().ToString("HH:mm") & Environment.NewLine & " Justificativa: " & Justificativa
        'Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
        Dim cmdText As String = "insert into TbMEN values ('" & NfeID & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '', 'DOC-e RECUSADO', '" & BODY & "','')" 'Marcio Spinosa - 21/02/2019 - CR00009165
        modSQL.ExecuteNonQuery(cmdText)

        cmdText = "update TbLOG set SITUACAO = 'INATIVO', MOTIVO = 'NF RECUSADA' where SITUACAO = 'ATIVO' and NFEID = '" & NfeID & "'"
        modSQL.ExecuteNonQuery(cmdText)

        cmdText = "UPDATE TBNFE SET SITUACAO = 'RECUSADA' WHERE NFEID = '" & NfeID & "'"
        modSQL.ExecuteNonQuery(cmdText)

        BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.Recusa, "Marcou a " & GetNumeroDocumento(NfeID) & " com recusa no verso", NfeID)
    End Sub
    ''' <summary>
    ''' Desfaz o status de recusada
    ''' </summary>
    ''' <param name="NfeID"></param>
    ''' <param name="Justificativa"></param>
    ''' <remarks></remarks>
    ''' <example>Foi ajustado no sql o retorno dos dados do funcionário</example>
    Public Sub DesfazerRecusa(ByVal NfeID As String, ByVal Justificativa As String)
        'Marcio Spinosa - 28/05/2018 - CR00008351
        'Dim BODY As String = "Recusa da Nota Fiscal DESFEITA pelo usuário " & Uteis.UserName() & " no dia " & DateTime.Now().ToString("dd/MM/yyyy") & " às " & DateTime.Now().ToString("HH:mm") & Environment.NewLine & " Justificativa: " & Justificativa
        Dim BODY As String = "Recusa da Nota Fiscal DESFEITA pelo usuário " & getUserNameByLogon(Uteis.LogonName()) & " no dia " & DateTime.Now().ToString("dd/MM/yyyy") & " às " & DateTime.Now().ToString("HH:mm") & Environment.NewLine & " Justificativa: " & Justificativa
        'Marcio Spinosa - 28/05/2018 - CR00008351 - Fim

        Dim cmdText As String = "insert into TbMEN values ('" & NfeID & "', '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', '', 'RECUSA DESFEITA DE DOC-e', '" & BODY & "','')"
        modSQL.ExecuteNonQuery(cmdText)
        modSQL.ExecuteNonQuery("UPDATE TBNFE SET SITUACAO = 'PENDENTE', REPROCESSAR = 'S' WHERE NFEID = '" & NfeID & "'")

        BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.RecusaDesfeita, "Desfez a marcação de recusa no verso da " & GetNumeroDocumento(NfeID), NfeID)
    End Sub

    Public Function ReenviarEmail(ByVal NfeID As String) As String
        Dim strSituacao As String = GetSituacaoDocumento(NfeID)
        Dim objVerificar As New modVerificar()

        If Not String.IsNullOrEmpty(strSituacao) Then

            If strSituacao = "ACEITA" Then
                objVerificar.EnviarMsgAceita(NfeID)
            ElseIf strSituacao = "REJEITADA" Then
                objVerificar.EnviarMsgRejeitada(NfeID)
            ElseIf strSituacao = "PENDENTE" Then
                objVerificar.EnviarMsgPendente(NfeID)
            End If

            BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.Email, "Enviou um e-mail da " & GetNumeroDocumento(NfeID) & " com status " & strSituacao.ToUpper() & " para o fornecedor ", NfeID)

            Return "Mensagem enviada com sucesso."

        Else
            Return "Não foi possível identificar o status da nota fiscal. A mensagem não foi enviada."
        End If

    End Function
    ''' <summary>
    ''' Envia o IP
    ''' </summary>
    ''' <param name="NfeID"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>Foi ajustado no sql o retorno dos dados do funcionário</example>
    Public Function EnviarIP(ByVal NfeID As String) As String
        Try
            'Ler o XML no Triangulus
            '--> PRIMEIRO TENTA BUSCAR NO VNF (NF JÁ FOI PROCESSADA)
            modSQL.CommandText = "SELECT VNF_CONTEUDO_XML FROM TbDOC_CAB WHERE NFEID = '" & NfeID & "'"
            Dim XML_STRING As String = modSQL.ExecuteScalar(modSQL.CommandText)

            '--> SE NÃO ENCONTRAR, BUSCA A NF NO TRIANGULUS
            If String.IsNullOrEmpty(XML_STRING) Then
                modSQL.CommandText = "select top 1 XML from vwNFF01_LISTA where CHAVE_ACESSO = '" & NfeID & "'"
                XML_STRING = modSQL.ExecuteScalar(modSQL.CommandText)
            End If

            'Ler em qual pasta deve-se gravar temporariamente o XML
            Dim cmdText As String = ""
            cmdText = "select VALOR from TbPAR where PARAMETRO = 'PASTA_TEMPORARIA_XML_DO_USUARIO'"

            Dim nomearq As String = "D:\Services\VNF\Temp\" & NfeID & "_" & Date.Now().ToString("ddMMyy.hhmmss") & ".XML"
            IO.File.WriteAllText(nomearq, XML_STRING)

            Dim wkMEMO As String = "Attachment file references NFEID: " & NfeID & Chr(13) & Chr(13) & "Metso - VNF"
            Dim IP_MAIL As String = modSQL.ExecuteScalar("select VALOR from tbPAR where PARAMETRO = 'IP_MAIL'")

            Dim NF As New List(Of String)
            NF.Add(nomearq)

            Dim FORN As String = modSQL.ExecuteScalar("select NF_EMIT_XNOME from TbDOC_CAB_NFE where NFEID = '" & NfeID & "'")
            Dim CNPJ_REMETENTE As String = modSQL.ExecuteScalar("select CNPJ_METSO from tbnfe where NFEID = '" & NfeID & "'")

            'Marcio Spinosa - 28/05/2018 - CR00008351
            'Uteis.SendMail("validador.notas.fiscais@metso.com", IP_MAIL, Nothing, Nothing, CNPJ_REMETENTE & "|" & FORN & " | " & NF.First(), wkMEMO, NF.ToArray())
            Dim strFrom As String = modSQL.ExecuteScalar("select valor from tbpar where parametro = 'EMAIL_VALIDADOR_NF'")
            Uteis.SendMail(strFrom, IP_MAIL, Nothing, Nothing, CNPJ_REMETENTE & "|" & FORN & " | " & NF.First(), wkMEMO, NF.ToArray())
            'modSQL.ExecuteNonQuery("INSERT INTO TBIPLOG VALUES ('" & NfeID & "', GETDATE(), '" & "User: " & Uteis.UserName() & " | Sent to: " & IP_MAIL & "', 'N', '', 'N', '')")
            modSQL.ExecuteNonQuery("INSERT INTO TBIPLOG VALUES ('" & NfeID & "', GETDATE(), '" & "User: " & getUserNameByLogon(Uteis.LogonName()) & " | Sent to: " & IP_MAIL & "', 'N', '', 'N', '')")
            'Marcio Spinosa - 28/05/2018 - CR00008351 - Fim

            Return String.Empty

        Catch ex As Exception
            Return "Ocorreu uma falha no envio de e-mail. O XML poderá ser enviado manualmente para o endereço metso_recebimento@signature.cl"
        End Try
    End Function

    Public Function RegistroManual(ByVal NfeID As String) As String
        Try
            If (GetStatusIntegracao(NfeID) = "CONCLUÍDO") Then
                Return "O processo de integração deste documento já está concluído"
            End If

            '--> INATIVA AS DIVERGÊNCIAS ATIVAS
            Dim strUpdateTbLog As String = "UPDATE " &
                                           "	TbLog " &
                                           "SET " &
                                           "	situacao = 'INATIVO', " &
                                           "	motivo = 'DOCUMENTO PROCESSADO MANUALMENTE', " &
                                           "	data_correcao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', " &
                                           "	acecodusu = '" & Uteis.LogonName() & "' " &
                                           "WHERE  " &
                                           "	situacao = 'ATIVO' and  " &
                                           "	nfeid = '" & NfeID & "' "
            modSQL.ExecuteNonQuery(strUpdateTbLog)

            '--> ATUALIZA A SITUACAÇÃO DO DOCUMENTO PARA ACEITA
            modSQL.ExecuteNonQuery("UPDATE TbNfe SET SITUACAO = 'ACEITA', SAP_STATUS_INTEGRACAO = 'CONCLUÍDO', SAP_DATE_INSERT = GETDATE()  WHERE nfeid = '" & NfeID & "'")

            '--> REGISTRA A INFORMAÇÃO NA TABELA DE INTEGRAÇÃO
            Dim strInsertTbIntegracao As String = "INSERT INTO TbIntegracao " &
                                                  "( " &
                                                  "	    NFEID, " &
                                                  "	    INT_MIGO_NF_ITEM_NUMBER, " &
                                                  "	    INT_MIGO_MAT_DOC_NUMBER, " &
                                                  "	    INT_MIRO_MAT_DOC_NUMBER " &
                                                  ") " &
                                                  "SELECT DISTINCT " &
                                                  "	    nfeid, " &
                                                  "	    isnull(ITENFE, 1),  " &
                                                  "	    'REGISTRADO MANUALMENTE', " &
                                                  "	    'REGISTRADO MANUALMENTE' " &
                                                  "FROM  " &
                                                  "	    tbjun " &
                                                  "WHERE " &
                                                  "	    nfeid = '" & NfeID & "' "
            modSQL.ExecuteNonQuery(strInsertTbIntegracao)

            '--> GRAVA LOG DO USUÁRIO QUE EXECUTOU A AÇÃO
            BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.RegistroManual, "A " & GetNumeroDocumento(NfeID) & " foi registrada manualmente.", NfeID)

            Return String.Empty
        Catch ex As Exception
            Return "Ocorreu uma falha no processamento. Os dados podem não estar atualizados no sistema"
        End Try
    End Function

    Public Function GetSituacaoDocumento(ByVal NfeId As String) As String
        Return modSQL.ExecuteScalar("SELECT situacao FROM TbNfe WHERE nfeid = '" + NfeId + "'")
    End Function

    Public Function GetStatusIntegracao(ByVal NfeId As String) As String
        'Comando SQL alterado para view padrão conforme orientação do Matheus
        '''''Dim strStatus As String = "SELECT " &
        '''''                          "	CASE " &
        '''''                          "		WHEN count(distinct int_migo_nf_item_number) = 0 THEN " &
        '''''                          "		'PENDENTE' " &
        '''''                          "		WHEN count(distinct int_migo_nf_item_number) < (select count(distinct nf_prod_item) from tbdoc_item_nfe where tbdoc_item_nfe.nfeid = TbIntegracao.nfeid) THEN " &
        '''''                          "		'INCOMPLETA' " &
        '''''                          "		ELSE " &
        '''''                          "		'CONCLUÍDO' " &
        '''''                          "	END " &
        '''''                          "FROM " &
        '''''                          "	TbIntegracao  " &
        '''''                          "WHERE  " &
        '''''                          "	nfeid = '" + NfeId + "' " &
        '''''                          "	and int_migo_mat_doc_number <> ''  " &
        '''''                          "	and int_miro_mat_doc_number <> '' " &
        '''''                          "GROUP BY " &
        '''''                          "	nfeid "
        Dim strStatus As String = "SELECT STATUS_INTEGRACAO " &
                                  "FROM " &
                                  "	VWSTATUSINTEGRACAO  " &
                                  "WHERE  " &
                                  "	nfeid = '" + NfeId + "' "
        Dim objRetorno As Object = modSQL.ExecuteScalar(strStatus)

        If (objRetorno Is Nothing) Then
            Return "PENDENTE"
        Else
            Return objRetorno.ToString()
        End If
    End Function

    Public Function GetNumeroDocumento(ByVal NfeId As String) As String
        Return modSQL.ExecuteScalar("SELECT VNF_TIPO_DOCUMENTO + ' ' + NF_IDE_NNF FROM TbDoc_Cab inner join TbDoc_Cab_Nfe on TbDoc_Cab.nfeid = TbDoc_Cab_Nfe.nfeid WHERE TbDoc_Cab.nfeid = '" & NfeId & "'")
    End Function

    Public Sub MaterialRecebido(ByVal NfeId As String)
        If (Convert.ToBoolean(modSQL.ExecuteScalar("SELECT isnull(VNF_MATERIAL_RECEBIDO, 0) FROM TbDOC_CAB WHERE NFEID = '" & NfeId & "'")) = False) Then
            modSQL.ExecuteNonQuery("UPDATE TbDOC_CAB SET VNF_MATERIAL_RECEBIDO = 1 WHERE NFEID = '" & NfeId & "'")
            BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.MaterialRecebido, "Sinalizou que os materiais da " & GetNumeroDocumento(NfeId) & " foram recebidos", NfeId)
        End If
    End Sub

    Public Sub EstornarMaterialRecebido(ByVal NfeId As String)
        If (Convert.ToBoolean(modSQL.ExecuteScalar("SELECT isnull(VNF_MATERIAL_RECEBIDO, 0) FROM TbDOC_CAB WHERE NFEID = '" & NfeId & "'")) = True) Then
            modSQL.ExecuteNonQuery("UPDATE TbDOC_CAB SET VNF_MATERIAL_RECEBIDO = 0 WHERE NFEID = '" & NfeId & "'")
            BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.MaterialRecebido, "Sinalizou que os materiais da " & GetNumeroDocumento(NfeId) & " foram estornados os recebimentos", NfeId)
        End If
    End Sub

    Public Function getMaterialRecebido(ByVal NfeId As String) As String
        Return modSQL.ExecuteScalar("SELECT VNF_MATERIAL_RECEBIDO FROM TbDOC_CAB WHERE NFEID = '" & NfeId & "'")
    End Function

    Public Function AlterarRelevancia(ByVal NfeId As String, ByVal ItemNf As Integer, ByVal ItemRelevante As String, ByRef Status As String, ByRef BloquearMudanca As Boolean) As String
        '---> 1. VERIFICA SE A INTEGRAÇÃO FOI CONCLUÍDA
        If (GetStatusIntegracao(NfeId) = "CONCLUÍDO") Then
            Status = String.Empty
            BloquearMudanca = True
            Return "Não é possível alterar a informação pois a integração com o SAP está concluída."
        End If

        '---> 2. VERIFICA SE O USUÁRIO TEM PERMISSÃO
        Dim objBLAcessos As New BLAcessos()
        If (objBLAcessos.ConsultaAcesso("AREL", Uteis.LogonName()) = False) Then
            Status = String.Empty
            BloquearMudanca = True
            Return "Você não possui permissão (AREL) para alterar essa informação."
        End If

        '---> 3. ALTERAR A RELEVÂNCIA DO ITEM
        Dim strMensagem As String = ""
        Dim objNF As modNF
        modSQL.ExecuteNonQuery("UPDATE " &
                               "	TbDOC_ITEM " &
                               "SET " &
                               "	VNF_ITEM_VALIDO = '" & ItemRelevante & "' " &
                               "FROM " &
                               "	TbDOC_ITEM " &
                               "	inner join tbjun " &
                               "	on TbDOC_ITEM.vnf_codjun = tbjun.codjun " &
                               "WHERE " &
                               "	TbDOC_ITEM.nfeid = '" & NfeId & "' " &
                               "	and ITENFE = " & ItemNf)

        If (modSQL.ExecuteScalar("SELECT situacao FROM tbnfe WHERE nfeid = '" & NfeId & "'").ToString() = "ACEITA" And ItemRelevante <> "X") Then
            objNF = Validar(NfeId, String.Empty, Uteis.LogonName(), strMensagem)
            Status = objNF.VNF_STATUS
        Else
            objNF = GetByID(NfeId, True)
            Status = objNF.VNF_STATUS
        End If

        Dim strRelevancia As String = IIf(ItemRelevante = "X", "irrelevante", "relevante")
        BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.RelevanciaAlterada, "Alterou o item " & ItemNf.ToString().PadLeft(3, "0") & " para " & strRelevancia, NfeId)
        BloquearMudanca = False
        Return strMensagem
    End Function

    Public Function SelecionarInboundDelivery(ByVal nfeid As String, ByVal itemNf As Integer, ByVal inboundDelivery As String, ByVal inboundDeliveryItem As Integer) As String
        Dim objItemNotaFiscal As New modNFItem()
        Dim strMensagem As String = ""
        Dim objNotaFiscal As modNF = GetByID(nfeid, strMensagem)

        If (Not String.IsNullOrEmpty(strMensagem)) Then
            Return strMensagem
        End If

        If (GetStatusIntegracao(nfeid) = "CONCLUÍDO") Then
            Return "Não é possível selecionar uma inbound delivery pois a integração com o SAP está concluída."
        End If

        objItemNotaFiscal = objNotaFiscal.ITENS_NF.Where(Function(x) x.NF_PROD_ITEM = itemNf).FirstOrDefault()

        Dim objVerificar As New modVerificar()
        Dim objRegras As modRegrasValidacao = objVerificar.GetRegrasValidacao("INBOUND_DELIVERY", objItemNotaFiscal.NF_PROD_CFOP, objItemNotaFiscal.SAP_ITEM_DETAILS.STORAGE_LOCATION, objItemNotaFiscal.SAP_ITEM_DETAILS.MATERIAL)

        If objRegras.IsExcecao Then
            Return "Esta categoria de nota fiscal não possui entrada de mercadorias. Não é permitido vincular uma inbound delivery com o item."
        End If

        'Dim decQtdNf As Decimal = IIf(objItemNotaFiscal.NF_PROD_QCOM > 0, objItemNotaFiscal.NF_PROD_QCOM, objItemNotaFiscal.NF_PROD_QTRIB)
        Dim decQtdNf As Decimal = objItemNotaFiscal.NF_PROD_QCOM
        Dim objInboundDelivery As modInboundDelivery = objItemNotaFiscal.VNF_INBOUND.Where(Function(x) x.SAP_INBOUND_DELIVERY_NUMBER = inboundDelivery And x.SAP_INBOUND_DELIVERY_ITEM_NUMBER = inboundDeliveryItem).FirstOrDefault()

        Dim objNotaFiscalSelecionada As modInboundDeliveryNFs = objInboundDelivery.NOTAS_FISCAIS.Where(Function(x) x.VNF_NFEID = nfeid And x.VNF_ITEM = itemNf).FirstOrDefault()
        If (Not objNotaFiscalSelecionada Is Nothing) Then
            objInboundDelivery.OPEN_QTY = objInboundDelivery.OPEN_QTY + decQtdNf
        End If

        If (decQtdNf > objInboundDelivery.OPEN_QTY) Then
            Return "A inbound selecionada não possui saldo suficiente"
        End If

        Dim strUpdateDocItem As String = "UPDATE  " &
                                         "	tbdoc_item " &
                                         "SET " &
                                         "	VNF_INBOUND_DELIVERY_NUMBER = '" & inboundDelivery & "', " &
                                         "	VNF_INBOUND_DELIVERY_ITEM_NUMBER = " & inboundDeliveryItem.ToString() & " " &
                                         "FROM	 " &
                                         "	tbdoc_item " &
                                         "	inner join tbjun " &
                                         "	on tbdoc_item.vnf_codjun = tbjun.codjun " &
                                         "WHERE " &
                                         "	tbdoc_item.nfeid = '" & nfeid & "' and " &
                                         "	itenfe = " & objItemNotaFiscal.NF_PROD_ITEM.ToString()

        modSQL.ExecuteNonQuery(strUpdateDocItem)

        BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.SelecionarInbound, "Selecionou a inbound " & inboundDelivery & "-" & inboundDeliveryItem & " para o item " & itemNf & " da " & GetNumeroDocumento(nfeid), nfeid)

        Return String.Empty

    End Function
    ''' <summary>
    ''' Envia o pedido para o SAP
    ''' </summary>
    ''' <param name="SapUser"></param>
    ''' <param name="SapPassword"></param>
    ''' <param name="CHAVE_ACESSO"></param>
    ''' <param name="pTipoMiro"></param>
    ''' <param name="pBlnImprimirSLIP"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>Marcio Spinosa - 28/05/2018 - CR00008351 - Foi ajustado no sql o retorno dos dados do funcionário</example>
    ''' <example>Marcio Spinosa - 07/08/2018 - CR00008351 - Ajustado o round na validação da migo com o xml para selecionar o miro do item correto</example>''' 
    Public Function EnviarSapPedido(ByVal SapUser As String, ByVal SapPassword As String, ByVal CHAVE_ACESSO As String, ByVal pTipoMiro As String, Optional ByVal pBlnImprimirSLIP As Boolean = False) As String
        Dim strNfeId As String = ""
        Dim vObjCFOPsFaturaConsignacao As Object
        Dim vObjCFOPsFaturaEntregaFutura As Object
        Dim vObjCFOPEscrituracao As String
        Dim vLisColectionPOProcessed As List(Of Long) = New List(Of Long)
        Dim vIntItensCount As Integer = 0
        Dim vAarrMigoPo As New List(Of SAP_RFC.GoodsMovementsPurchaseOrder)()
        Dim IsMigoRealizada As Boolean = False
        Dim IsOnlyMIRO As Boolean = False
        Dim vObjModVerificar As New modVerificar
        Dim vBlnIsSubContratacao As Boolean = False
        Dim vBlnIsNotaComplementar As Boolean = False

        Try
            '----------------------------------| REGRAS DE PROCESSAMENTO DE MIGO E MIRO |----------------------------------'
            '--> 1. AVISAR USUÁRIO QUANDO NÃO FOR FEITO NENHUM PROCESSO (MIGO/MIRO) E QUE ELE DEVERÁ REALIZAR MANUALMENTE
            '--> 2. AGRUPAR AS MIGOS PELO MESMO TIPO DE MOVIMENTO E CRIAR APENAS 1 REGISTRO NO SAP
            '--> 3. SE EXISTIR MAIS DE UM TIPO DE NF DENTRO DOS ITENS, NÃO REALIZAR MIGO e MIRO E AVISAR USUÁRIO
            '--> 4. SE EXISTIR MIRO MANUAL DENTRO DA NF, EXECUTAR A MIRO MANUAL INDEPENDENTE DOS OUTROS ITENS
            '--> 5. SE EXISTIR MIRO NÃO REALIZÁVEL DENTRO DA NF, NÃO REALIZAR A MIRO INDEPENDENTE DOS OUTROS ITENS

            If Boolean.Parse(Uteis.GetSettingsValue(Of String)("FixedSapSystemUser").ToString()) = True Then
                SapUser = Uteis.GetSettingsValue(Of String)("User").ToString()
                SapPassword = Uteis.GetSettingsValue(Of String)("Password").ToString()
            End If

            Dim strQuery As String = ""

            Dim IsMiroExist As Boolean = False
            Dim sqlparams As New List(Of SqlClient.SqlParameter)
            Dim datIntegracao As DateTime = DateTime.Now
            strQuery = "INSERT INTO TbIntegracao " &
                        "( " &
                        "	 NFEID " &
                        "	,INT_MIGO_MAT_DOC_NUMBER " &
                        "	,INT_MIGO_MAT_DOC_ITEM " &
                        "	,INT_MIGO_MAT_DOC_YEAR " &
                        "	,INT_MIGO_PO_NUMBER " &
                        "	,INT_MIGO_PO_ITEM " &
                        "	,INT_MIGO_NF_ITEM_NUMBER " &
                        "	,INT_MIGO_NF_QUANTITY " &
                        "	,INT_MIGO_NF_NET_ITEM_VALUE " &
                        ") " &
                        "VALUES " &
                        "( " &
                        "	 @NFEID " &
                        "	,@INT_MIGO_MAT_DOC_NUMBER " &
                        "	,@INT_MIGO_MAT_DOC_ITEM " &
                        "	,@INT_MIGO_MAT_DOC_YEAR " &
                        "	,@INT_MIGO_PO_NUMBER " &
                        "	,@INT_MIGO_PO_ITEM " &
                        "	,@INT_MIGO_NF_ITEM_NUMBER " &
                        "	,@INT_MIGO_NF_QUANTITY " &
                        "	,@INT_MIGO_NF_NET_ITEM_VALUE " &
                        ") "

            '---> VARIAVEIS DA MIGO
            Dim objMigoHeader As SAP_RFC.GoodsMovementsHeader
            Dim arrMigoReturn As New List(Of SAP_RFC.GoodsMovements)()
            Dim objSapRet As SAP_RFC.RfcReturn
            Dim vStrMaterialDocNumber As String = String.Empty

            Dim arrMigoPo As New List(Of SAP_RFC.GoodsMovementsPurchaseOrder)()
            Dim objMigoXmlHeader As SAP_RFC.GoodsMovementsXmlHeader
            Dim objMigoPo As SAP_RFC.GoodsMovementsPurchaseOrder
            Dim decValorNetNF As Decimal

            '---> VARIAVEIS DA MIRO
            Dim arrXmlItem As New List(Of SAP_RFC.InvoiceXmlItem)()
            Dim objXmlItem As SAP_RFC.InvoiceXmlItem

            Dim strMensagemValidacao As String = String.Empty
            'Marcio Spinosa - 28/05/2018 - CR00008351
            'Dim objNF As modNF = Validar(CHAVE_ACESSO, String.Empty, Uteis.UserName(), strMensagemValidacao)
            Dim objNF As modNF = Validar(CHAVE_ACESSO, String.Empty, getUserNameByLogon(Uteis.LogonName()), strMensagemValidacao)
            'Marcio Spinosa - 28/05/2018 - CR00008351 - Fim
            Dim vObjCollItens As New List(Of modNFItem)

            Dim vstrCFOPComp As String = modSQL.ExecuteScalar("SELECT VALOR FROM TBPAR WHERE PARAMETRO = 'CFOP_COMP'") 'Marcio Spinosa - 24/07/2018 - CR00008351

            '--> SE NO NOVO MATCHING A NF TEVE O STATUS ALTERADO OU TEVE ALGUMA FALHA, O PROCESSO DEVE SER INTERROMPIDO
            If (objNF.VNF_STATUS <> "ACEITA") Then
                Return "Foram encontradas divergências entre a nota fiscal e o pedido de compras. O processo foi interrompido"
            ElseIf (Not String.IsNullOrEmpty(strMensagemValidacao)) Then
                Return strMensagemValidacao
            End If

            '--> SE FOR UM CTE, APENAS O PRIMEIRO ITEM É RELEVANTE PARA POSTAGEM NO SAP
            If objNF.VNF_TIPO_DOCUMENTO = "CTE" Then
                Dim objItemCte As modNFItem = objNF.ITENS_NF.Where(Function(x) x.NF_PROD_ITEM = 1).FirstOrDefault()
                objNF.ITENS_NF.Clear()
                objNF.ITENS_NF.Add(objItemCte)
            End If

            Dim arrItensNfOrdernados As New List(Of modNFItem)()
            arrItensNfOrdernados = objNF.ITENS_NF.OrderBy(Function(x) x.MDP_TIPO_MOVIMENTO_MIGO).ToList()
            objNF.ITENS_NF = arrItensNfOrdernados

            Dim itemNF As modNFItem
            Dim strSerie As String = Uteis.GetSettingsValue(Of String)("SerieMigoMiro")
            If Not String.IsNullOrEmpty(strSerie) Then
                objNF.NF_IDE_SERIE = strSerie
            End If

            'Verifica se já não existe MIRO criada para esse nuúmero/serie de nota .
            If MIROExists(objNF.NF_IDE_NNF, objNF.NF_IDE_SERIE, objNF.NF_IDE_DHEMI, objNF.NF_EMIT_CNPJ, SapUser, SapPassword) Then
                Return "Já existe uma MIRO criada para esse documento. O processo foi interrompido"
                'Avisar que nota já foi postada e sair completamente do processo
            End If

            '----> SE HOUVER MAIS DE UM TIPO DE NOTA FISCAL ENTRE OS ITENS, DEVE CRIAR A MIRO MANUAL 
            Dim strTipoNF As String = ""
            If (pTipoMiro = "A") Then
                For i As Integer = 0 To objNF.ITENS_NF.Count - 1
                    If (String.IsNullOrEmpty(strTipoNF)) Then
                        strTipoNF = objNF.ITENS_NF(i).MDP_TIPO_NF
                    ElseIf (strTipoNF <> objNF.ITENS_NF(i).MDP_TIPO_NF) Then
                        pTipoMiro = "M"
                        Exit For
                    End If
                Next
            End If

            '----> DEFINE O TIPO DE MIRO QUE DEVERÁ SER REALIZADO PARA A NOTA FISCAL
            For i As Integer = 0 To objNF.ITENS_NF.Count - 1
                If (Not objNF.ITENS_NF(i).MDP_CRIAR_MIRO OrElse String.IsNullOrEmpty(objNF.ITENS_NF(i).MDP_TIPO_MIRO)) Then
                    pTipoMiro = ""
                ElseIf (Not String.IsNullOrEmpty(pTipoMiro) AndAlso objNF.ITENS_NF(i).MDP_TIPO_MIRO = "M") Then
                    pTipoMiro = "M"
                End If
            Next

            '--> SE ALGUM DOCUMENTO ESTIVER EM PROCESSAMENTO, O SISTEMA DEVE AGUARDAR A CONCLUSÃO
            Dim datRequisicao As DateTime = DateTime.Now
            strNfeId = objNF.VNF_CHAVE_ACESSO
            modSQL.ExecuteNonQuery("INSERT INTO TbIntegracaoPostagens (ipo_nfeid, ipo_usuario, ipo_data_inclusao) VALUES ('" & strNfeId & "', '" & Uteis.LogonName() & "', '" & datRequisicao.ToString("yyyy-MM-dd HH:mm:ss") & "')")
            Dim intIdPostagem As Integer = modSQL.ExecuteScalar("SELECT id_integracao_postagem FROM TbIntegracaoPostagens WHERE ipo_nfeid = '" & strNfeId & "'").ToString().ToInt()
            'Do
            '    Threading.Thread.Sleep(2000)
            'Loop While modSQL.ExecuteScalar("SELECT count(*) FROM TbIntegracaoPostagens WHERE ipo_data_conclusao is null and id_integracao_postagem < " & intIdPostagem).ToString().ToInt() > 0

            '--> VERIFICA SE É A NOTA FISCAL REFERENCIADA É REQUERIDA
            Dim objModVerificar = New modVerificar()
            Dim isMandatoryNotaFiscalReferenciada As Boolean = False
            If (objNF.ITENS_NF.Count > 0) Then
                isMandatoryNotaFiscalReferenciada = objModVerificar.IsMandatoryNotaFiscalReferenciada(objNF.ITENS_NF.FirstOrDefault().NF_PROD_CFOP)
            End If

            '----> INICIA O PROCESSAMENTO DE MIGO
            For i As Integer = 0 To objNF.ITENS_NF.Count - 1
                itemNF = objNF.ITENS_NF(i)

                '--> Se for um NFS,TLC e FAT o MDP_TIPO_MOVIMENTO_MIGO tem que ser 101- Consumo, Nao tem confirmacao de estoque.
                If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_NFS Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_TLC Or objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_FAT) Then
                    itemNF.MDP_TIPO_MOVIMENTO_MIGO = "101"
                    pTipoMiro = "M"
                    itemNF.MDP_CRIAR_MIRO = True
                End If

                '--> SE NÃO TEM A DEFINIÇÃO DO TIPO DE MIGO QUE DEVE SER CRIADA, SIGNIFICA QUE NÃO DEVE GERAR MIGO PARA O ITEM
                If (itemNF.VNF_ITEM_VALIDO = "X" OrElse (String.IsNullOrEmpty(itemNF.MDP_TIPO_MOVIMENTO_MIGO) And itemNF.MDP_CRIAR_MIRO = False)) Then
                    Continue For
                End If

                'Verifica se somente adiciona o componente da subcontratação
                'Marcio Spinosa - 23/07/2018 - CR00008351
                'If itemNF.VNF_IS_SUBCONTRATACAO And itemNF.NF_PROD_CFOP <> "5124" Then
                If itemNF.VNF_IS_SUBCONTRATACAO And vstrCFOPComp.Contains(itemNF.NF_PROD_CFOP) Then
                    'Marcio Spinosa - 23/07/2018 - CR00008351 - Fim
                    vBlnIsSubContratacao = True
                    objXmlItem = New SAP_RFC.InvoiceXmlItem()

                    objXmlItem.IS_DELIVERY_NOTE_COMPONENT = "X"
                    objXmlItem.MATERIAL_DOCUMENT_NUMBER = ""
                    objXmlItem.MATERIAL_DOCUMENT_ITEM = 0
                    objXmlItem.MATERIAL_DOCUMENT_YEAR = 0
                    objXmlItem.PURCHASING_DOCUMENT_NUMBER = itemNF.SAP_PO_NUMBER
                    objXmlItem.PURCHASING_DOCUMENT_ITEM_NUMBER = itemNF.SAP_ITEM_DETAILS.ITEM_NUMBER
                    objXmlItem.XML_ITEM = itemNF.NF_PROD_ITEM
                    'objXmlItem.QUANTITY_NF = IIf(itemNF.NF_PROD_QCOM > 0, itemNF.NF_PROD_QCOM, itemNF.NF_PROD_QTRIB)
                    objXmlItem.QUANTITY_NF = itemNF.NF_PROD_QCOM
                    objXmlItem.PLANT_WERKS = itemNF.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST.Select(Function(y) y.PLANT_WERKS).FirstOrDefault()
                    objXmlItem.MATERIAL_CODE_MATNR = itemNF.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST.Select(Function(y) y.MATERIAL_CODE_MATNR).FirstOrDefault()
                    objXmlItem.LOTE_BATCH = itemNF.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST.Where(Function(x) Not String.IsNullOrWhiteSpace(x.LOTE_BATCH)).Select(Function(y) y.LOTE_BATCH).FirstOrDefault()
                    objXmlItem.STORAGE_LOCATION_STGE_LOC = itemNF.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST.Where(Function(x) Not String.IsNullOrWhiteSpace(x.STORAGE_LOCATION_LGORT)).Select(Function(y) y.STORAGE_LOCATION_LGORT).FirstOrDefault()
                    objXmlItem.TAX_CODE_MWSKZ = itemNF.SAP_ITEM_DETAILS.TAX_CODE
                    objXmlItem.UNIT_MEASURE_MEINS = itemNF.SAP_ITEM_DETAILS.ITEM_COMPONENTS_LIST.Select(Function(y) y.UNIT_MEASURE_MEINS).FirstOrDefault()
                    objXmlItem.EXT_BASE_AMOUNT = itemNF.NF_PROD_VPROD
                Else
                    '--> CÁLCULO PARA DEFINIR O VALOR NET PARA ESCRITURAÇÃO DA NOTA FISCAL
                    decValorNetNF = itemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE

                    '---> PARA ITENS COM TAX CODE "F0", SOMAR O VALOR DE ICMS NO VALOR LÍQUIDO
                    If (itemNF.SAP_ITEM_DETAILS.TAX_CODE = "F0") Then
                        decValorNetNF += itemNF.NF_ICMS_VICMS
                    End If

                    '--> ADICIONA AS INFORMAÇÕES DO ITEM NA MIGO QUE DEVERÁ SER CRIADA
                    objMigoPo = New SAP_RFC.GoodsMovementsPurchaseOrder()
                    objMigoPo.PURCHASING_DOCUMENT_NUMBER = itemNF.SAP_PO_NUMBER
                    objMigoPo.PURCHASING_DOCUMENT_ITEM_NUMBER = itemNF.SAP_ITEM_DETAILS.ITEM_NUMBER
                    objMigoPo.NOTA_FISCAL_ITEM_NUMBER = itemNF.NF_PROD_ITEM
                    'objMigoPo.NOTA_FISCAL_QUANTITY = IIf(itemNF.NF_PROD_QCOM > 0, itemNF.NF_PROD_QCOM, itemNF.NF_PROD_QTRIB)
                    objMigoPo.NOTA_FISCAL_QUANTITY = itemNF.NF_PROD_QCOM


                    If (objNF.NF_IDE_FINNFE <> 2) Then
                        If Not String.IsNullOrEmpty(itemNF.MDP_TIPO_MOVIMENTO_MIGO) AndAlso itemNF.SAP_ITEM_DETAILS.CONFIRMATION_CONTROL_KEY = "0001" And (String.IsNullOrEmpty(itemNF.VNF_INBOUND_DELIVERY_NUMBER) Or itemNF.VNF_INBOUND_DELIVERY_ITEM_NUMBER = 0) Then
                            modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
                            Return "Não foi possível determinar uma inbound com saldo suficiente para o item " & itemNF.NF_PROD_ITEM.ToString() & " da nota fiscal. O processo foi interompido."
                        End If

                        If itemNF.SAP_ITEM_DETAILS.CONFIRMATION_CONTROL_KEY = "0001" And Not String.IsNullOrEmpty(itemNF.MDP_TIPO_MOVIMENTO_MIGO) Then
                            objMigoPo.INBOUND_DELIVERY_NUMBER = itemNF.VNF_INBOUND_DELIVERY_NUMBER
                            objMigoPo.INBOUND_DELIVERY_ITEM_NUMBER = itemNF.VNF_INBOUND_DELIVERY_ITEM_NUMBER
                        End If
                    End If


                    '--> SE O MODO PROCESSO ESTIVER MARCADO COMO "ENVIAR CÓDIGO DE IMPOSTO NA MIGO"
                    If (itemNF.MDP_ENVIAR_TAXCODE_MIGO) Then
                        '--> BUSCA O TAXCODE NA TABELA DE DE/PARA
                        Dim query As String = "SELECT TAX_CODE_REMESSA FROM TbTaxCode_MIGO WHERE TAX_CODE_FATURA = '" & itemNF.SAP_ITEM_DETAILS.TAX_CODE & "'"
                        Dim migoTaxCode As String = modSQL.ExecuteScalar(query).ToString()
                        objMigoPo.TAX_ON_SALES_PURCHASE_CODE_MWSKZ = migoTaxCode
                        objMigoPo.NETPR_XML_EXT_BASE_AMOUNT = itemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE
                    End If

                    arrMigoPo.Add(objMigoPo)

                    Dim objCofins As SAP_RFC.PurchaseOrderItemsTaxes
                    For Each objTax As SAP_RFC.PurchaseOrderItemsTaxes In itemNF.SAP_ITEM_DETAILS.ITEM_TAXES
                        If (objTax.TAX_NAME.ToUpper() = "COFINS") Then
                            objCofins = objTax
                            Exit For
                        End If
                    Next

                    Dim objPis As SAP_RFC.PurchaseOrderItemsTaxes
                    For Each objTax As SAP_RFC.PurchaseOrderItemsTaxes In itemNF.SAP_ITEM_DETAILS.ITEM_TAXES
                        If (objTax.TAX_NAME.ToUpper() = "PIS") Then
                            objPis = objTax
                            Exit For
                        End If
                    Next

                    objXmlItem = New SAP_RFC.InvoiceXmlItem()
                    objXmlItem.MATERIAL_DOCUMENT_NUMBER = ""
                    objXmlItem.MATERIAL_DOCUMENT_ITEM = 0
                    objXmlItem.MATERIAL_DOCUMENT_YEAR = 0
                    objXmlItem.PURCHASING_DOCUMENT_NUMBER = itemNF.SAP_PO_NUMBER
                    objXmlItem.PURCHASING_DOCUMENT_ITEM_NUMBER = itemNF.SAP_ITEM_DETAILS.ITEM_NUMBER
                    objXmlItem.XML_ITEM = itemNF.NF_PROD_ITEM
                    objXmlItem.FCI_NUMBER = itemNF.NF_PROD_FCI
                    objXmlItem.ITEM_CATEGORY = itemNF.SAP_ITEM_DETAILS.ITEM_CATEGORY
                    objXmlItem.ITEM_VALUE_WITH_TAXES_NF = itemNF.NF_PROD_VPROD
                    objXmlItem.INSURANCE_VALUE_WITH_TAXES = itemNF.NF_PROD_VSEG
                    objXmlItem.FREIGHT_VALUE_WITH_TAXES = itemNF.NF_PROD_VFRETE
                    objXmlItem.DISCOUNT_VALUE_WITH_TAXES = itemNF.NF_PROD_VDESC
                    objXmlItem.OTHER_VALUE_WITH_TAXES = itemNF.NF_PROD_VOUTRO
                    objXmlItem.NET_FREIGHT_VALUE_NF = itemNF.SAP_ITEM_DETAILS.NF_NET_FREIGHT_VALUE
                    objXmlItem.NET_FREIGHT_VALUE_PO = itemNF.SAP_ITEM_DETAILS.SAP_NET_FREIGHT_VALUE
                    objXmlItem.NET_INSURANCE_VALUE_NF = itemNF.SAP_ITEM_DETAILS.NF_NET_INSURANCE_VALUE
                    objXmlItem.NET_EXPENSES_VALUE_NF = itemNF.SAP_ITEM_DETAILS.NF_NET_OTHER_EXPENSES_VALUES
                    'objXmlItem.QUANTITY_NF = IIf(itemNF.NF_PROD_QCOM > 0, itemNF.NF_PROD_QCOM, itemNF.NF_PROD_QTRIB)
                    objXmlItem.QUANTITY_NF = itemNF.NF_PROD_QCOM

                    If vObjModVerificar.IsFaturaEntregaFutOUConsignacao(itemNF.NF_PROD_CFOP, "ID_MODO_PROCESSO_REMESSA_CONSIGNACAO", objNF.NF_IDE_FINNFE) Or vObjModVerificar.IsFaturaEntregaFutOUConsignacao(itemNF.NF_PROD_CFOP, "ID_MODO_PROCESSO_REMESSA_ENTREGA_FUTURA", objNF.NF_IDE_FINNFE) Then
                        objXmlItem.NET_ITEM_VALUE_NF = decValorNetNF
                        objXmlItem.NET_PRICE_NF = decValorNetNF
                    Else
                        objXmlItem.NET_ITEM_VALUE_NF = IIf(pTipoMiro = "A", decValorNetNF, itemNF.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE)
                        objXmlItem.NET_PRICE_NF = IIf(pTipoMiro = "A", decValorNetNF, itemNF.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE)
                    End If



                    If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_cte) Then
                        objXmlItem.CFOP = itemNF.NF_PROD_CFOP
                    End If

                    'Alteração para incluir o CFOP caso esse exista na tabela de de/para, isso fara com que a RFC substitua o valor original do SAP
                    'Somente para NF-e
                    If (objNF.VNF_TIPO_DOCUMENTO = modNF.tipo_doc_nfe) Then
                        vObjCFOPEscrituracao = GetCFOPEscrituracaoMIRO(itemNF.NF_PROD_CFOP, itemNF.SAP_ITEM_DETAILS.SAP_ITEM_CFOP.Replace("AA", ""))
                        If Not (String.IsNullOrWhiteSpace(vObjCFOPEscrituracao)) Then
                            objXmlItem.CFOP = vObjCFOPEscrituracao & "AA"
                        End If
                    End If

                    '---> QUANDO FOR "F0", NÃO ADICIONAR NENHUM IMPOSTO
                    If (itemNF.SAP_ITEM_DETAILS.TAX_CODE <> "F0") Then
                        objXmlItem.ICMS_TAX_VALUE = itemNF.NF_ICMS_VICMS
                        objXmlItem.ICMS_TAX_RATE = itemNF.NF_ICMS_PICMS
                        objXmlItem.ICMS_BASE_AMOUNT = itemNF.NF_ICMS_VBC
                        objXmlItem.PIS_BASE_AMOUNT_NF = itemNF.NF_PIS_VBC
                        objXmlItem.COFINS_BASE_AMOUNT_PO = objCofins.TAX_BASE_CALCULATION_AMOUNT
                        objXmlItem.COFINS_BASE_AMOUNT_NF = itemNF.NF_COFINS_VBC
                        objXmlItem.PIS_BASE_AMOUNT_PO = objPis.TAX_BASE_CALCULATION_AMOUNT
                        objXmlItem.ICMS_ST_TAX_VALUE = itemNF.NF_ICMS_VICMSST
                        objXmlItem.ICMS_ST_BASE_AMOUNT = itemNF.NF_ICMS_VBCST
                        objXmlItem.IPI_TAX_VALUE = itemNF.NF_IPI_VIPI
                        objXmlItem.IPI_BASE_AMOUNT = itemNF.NF_IPI_VBC


                        Dim strTaxSplitCode As String = modSQL.ExecuteScalar("SELECT VALOR FROM TbPAR WHERE PARAMETRO = 'TAX_SPLIT_CODE'").ToString()
                        Dim objIpiTaxSplit As New SAP_RFC.PurchaseOrderItemsTaxes
                        For Each objTax As SAP_RFC.PurchaseOrderItemsTaxes In itemNF.SAP_ITEM_DETAILS.ITEM_TAXES
                            If (objTax.TAX_NAME.ToUpper() = strTaxSplitCode) Then
                                objIpiTaxSplit = objTax
                                Exit For
                            End If
                        Next

                        Dim objIpi As New SAP_RFC.PurchaseOrderItemsTaxes()
                        objIpi = itemNF.SAP_ITEM_DETAILS.ITEM_TAXES.Where(Function(X) X.TAX_NAME = "IPI").FirstOrDefault()
                        Dim decValorIpiPo As Decimal = IIf(objIpiTaxSplit.TAX_NAME = String.Empty, objIpi.TAX_AMOUNT, objIpi.TAX_AMOUNT - objIpiTaxSplit.TAX_AMOUNT)
                        If (itemNF.SAP_ITEM_DETAILS.TAX_SPLIT = "X" And (itemNF.SAP_ITEM_DETAILS.TAX_CODE = "P3" Or itemNF.SAP_ITEM_DETAILS.TAX_CODE = "P6")) Then
                            objXmlItem.IPI_TAX_VALUE = ((itemNF.NF_PROD_VPROD / 2) * objIpi.TAX_PERCENTAGE) / 100
                            objXmlItem.IPI_BASE_AMOUNT = Math.Round(itemNF.NF_PROD_VPROD / 2, 2)
                        End If
                    End If

                    If vObjModVerificar.IsFaturaEntregaFutOUConsignacao(itemNF.NF_PROD_CFOP, "ID_MODO_PROCESSO_FATURA_CONSIGNACAO", objNF.NF_IDE_FINNFE) Then
                        For Each refNF In itemNF.SAP_ITEM_DETAILS.ITEM_MSEG.Where(Function(x) x.MOVIMENTO_BWART = "821").ToList
                            objXmlItem.NOTA_REFERENCIA_FATURA_XBLNR_MKPF = refNF.NF_REMESSA_XBLNR_MKPF
                            objXmlItem.MATERIAL_DOCUMENT_NUMBER = refNF.MIGO_NUMBER_MBLNR
                            objXmlItem.MATERIAL_DOCUMENT_YEAR = refNF.MIGO_YEAR_MJAHR
                            objXmlItem.MATERIAL_DOCUMENT_ITEM = refNF.MIGO_ITEM_SEQ_ZEILE
                        Next
                    End If

                    If (objNF.NF_IDE_FINNFE = 2) Then
                        For Each refNF In itemNF.SAP_ITEM_DETAILS.ITEM_MSEG.Where(Function(x) x.MOVIMENTO_BWART = "107").ToList
                            objXmlItem.NOTA_REFERENCIA_FATURA_XBLNR_MKPF = refNF.NF_REMESSA_XBLNR_MKPF
                            objXmlItem.MATERIAL_DOCUMENT_NUMBER = refNF.MIGO_NUMBER_MBLNR
                            objXmlItem.MATERIAL_DOCUMENT_YEAR = refNF.MIGO_YEAR_MJAHR
                            objXmlItem.MATERIAL_DOCUMENT_ITEM = refNF.MIGO_ITEM_SEQ_ZEILE
                        Next
                    End If
                End If

                arrXmlItem.Add(objXmlItem)

                '---> SE NÃO TIVER TIPO DE MOVIMENTO DE MIGO, ATUALIZAR A TABELA DE INTEGRAÇÃO COMO MIGO NÃO APLICÁVEL OU CONSIGNAÇÃO
                If String.IsNullOrEmpty(itemNF.MDP_TIPO_MOVIMENTO_MIGO) Then
                    '--->VERIFICA SE É UM CASO DE CONSIGNAÇÃO POR MEIO DO CFOP DO ITEM
                    Dim vStrMensagem As String = "NÃO APLICÁVEL"

                    vObjCFOPsFaturaEntregaFutura = modSQL.ExecuteScalar("EXEC SP_GET_CFOP_ENTREGA_FUT_CONSIG 'ID_MODO_PROCESSO_FATURA_ENTREGA_FUTURA'")
                    vObjCFOPsFaturaConsignacao = modSQL.ExecuteScalar("EXEC SP_GET_CFOP_ENTREGA_FUT_CONSIG 'ID_MODO_PROCESSO_FATURA_CONSIGNACAO'")

                    If vObjCFOPsFaturaEntregaFutura.ToString().Contains(itemNF.NF_PROD_CFOP) Or vObjCFOPsFaturaConsignacao.ToString().Contains(itemNF.NF_PROD_CFOP) Then
                        IsOnlyMIRO = True
                        vStrMensagem = "ENTREGA FUT_CONSIG."
                    End If

                    '--> Verifica se não é uma nota complementar, caso seja flega para criar somente a MIRO
                    If objNF.NF_IDE_FINNFE = "2" Then
                        IsOnlyMIRO = True
                        vBlnIsNotaComplementar = True
                        vStrMensagem = "NOTA COMPLEMENTAR"
                    End If

                    sqlparams = New List(Of SqlClient.SqlParameter)
                    sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, CHAVE_ACESSO))
                    sqlparams.Add(modSQL.AddSqlParameter("INT_ITEM_NOTA_FISCAL", SqlDbType.Int, itemNF.NF_PROD_ITEM))
                    sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_MAT_DOC_NUMBER", SqlDbType.VarChar, vStrMensagem))
                    sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_MAT_DOC_ITEM", SqlDbType.Int, 0))
                    sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_MAT_DOC_YEAR", SqlDbType.Int, 0))
                    sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_PO_NUMBER", SqlDbType.VarChar, itemNF.SAP_PO_NUMBER))
                    sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_PO_ITEM", SqlDbType.Int, itemNF.SAP_ITEM_DETAILS.ITEM_NUMBER))
                    sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_NF_ITEM_NUMBER", SqlDbType.Int, itemNF.NF_PROD_ITEM))
                    'sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_NF_QUANTITY", SqlDbType.Decimal, IIf(itemNF.NF_PROD_QCOM > 0, itemNF.NF_PROD_QCOM, itemNF.NF_PROD_QTRIB)))
                    sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_NF_QUANTITY", SqlDbType.Decimal, itemNF.NF_PROD_QCOM))
                    sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_NF_NET_ITEM_VALUE", SqlDbType.Decimal, IIf(pTipoMiro = "A", decValorNetNF, itemNF.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE)))
                    modSQL.ExecuteNonQueryParams(strQuery, sqlparams)
                ElseIf (i = objNF.ITENS_NF.Count - 1) OrElse (objNF.ITENS_NF(i).MDP_TIPO_MOVIMENTO_MIGO <> objNF.ITENS_NF(i + 1).MDP_TIPO_MOVIMENTO_MIGO) And IsOnlyMIRO = False Then
                    '---> SE É O ÚLTIMO ITEM DA NOTA FISCAL OU SE O PRÓXIMO ITEM TEM UM TIPO DE MOVIMENTO DE MIGO DIFERENTE, OU CASO CASO NÃO SEJA UM LANÇAMENTO DE 
                    '---> ENTREGA FUTURA/CONSIGNADO CRIAR A MIGO
                    objMigoHeader = New SAP_RFC.GoodsMovementsHeader()

                    '-->SETA O FLAG DE IDENTIFICAÇÃO DE SUBCONTRATAÇÃO 
                    If vBlnIsSubContratacao Then
                        objMigoHeader.IS_SUBCONTRACTING = "X"
                    End If


                    If Not String.IsNullOrWhiteSpace(objNF.NF_IDE_SERIE) Then
                        objMigoHeader.NOTA_FISCAL = objNF.NF_IDE_NNF & "-" & objNF.NF_IDE_SERIE
                    Else
                        objMigoHeader.NOTA_FISCAL = objNF.NF_IDE_NNF
                    End If

                    objMigoHeader.MOVE_TYPE = itemNF.MDP_TIPO_MOVIMENTO_MIGO
                    objMigoHeader.ACTION = "01"
                    If pBlnImprimirSLIP Then
                        objMigoHeader.PRINT_SLIP = "3"
                    Else
                        objMigoHeader.PRINT_SLIP = "0"
                    End If

                    objMigoHeader.SPECIAL_STOCK = ""
                    If (isMandatoryNotaFiscalReferenciada) Then
                        objMigoHeader.HEADER_TEXT = objNF.NF_NFREF_REFNNF & "-" & objNF.NF_NFREF_REFSerie
                        'Por alguma razão quando os campos exclusivos abaixo são preenchido dá erro na BAPI então não preenche-los
                        ' '' '' ''objMigoHeader.NOTA_FISCAL_REF = objNF.NF_NFREF_REFNNF
                        ' '' '' ''objMigoHeader.SERIE_REF = objNF.NF_NFREF_REFSerie
                        ' '' '' ''objMigoHeader.NF_CREDAT_REF = objNF.NF_NFREF_REFDHEMI
                    Else
                        objMigoHeader.HEADER_TEXT = "VNF-" & Uteis.LogonName()
                    End If

                    objMigoXmlHeader = New SAP_RFC.GoodsMovementsXmlHeader()
                    If Not String.IsNullOrWhiteSpace(objNF.NF_IDE_SERIE) Then
                        objMigoXmlHeader.NOTA_FISCAL = objNF.NF_IDE_NNF & "-" & objNF.NF_IDE_SERIE
                    Else
                        objMigoXmlHeader.NOTA_FISCAL = objNF.NF_IDE_NNF
                    End If

                    objMigoXmlHeader.DOCUMENT_DATE = objNF.NF_IDE_DHEMI
                    objMigoXmlHeader.RECEIVED_DATE = objNF.NF_IDE_DHEMI
                    objMigoXmlHeader.TOTAL_VALUE_WITH_TAXES = objNF.NF_ICMSTOT_VNF
                    objMigoXmlHeader.VENDOR_CNPJ = objNF.NF_EMIT_CNPJ
                    objMigoXmlHeader.NFE_ID = CHAVE_ACESSO
                    objMigoXmlHeader.XML_VERSION = objNF.NF_OUTROS_VERSAO
                    objMigoXmlHeader.PROTOCOL_NUMBER = ""
                    objMigoXmlHeader.VENDOR_NUMBER = objNF.SAP_DETAILS.VENDOR_CODE
                    objMigoXmlHeader.REMARK = ""
                    objMigoXmlHeader.FREIGHT_MODEL = objNF.NF_IDE_MODAL
                    objMigoXmlHeader.NF_TYPE = itemNF.MDP_TIPO_NF

                    arrMigoReturn = SAP_RFC.createGoodsMovements(SapUser, SapPassword, objMigoHeader, objMigoXmlHeader, arrXmlItem.Where(Function(x) String.IsNullOrEmpty(x.MATERIAL_DOCUMENT_NUMBER)).ToList(), arrMigoPo, objSapRet)
                    For Each objMessage As SAP_RFC.BapiMessage In objSapRet.BapiMessage
                        For Each itemNotaFiscal As SAP_RFC.InvoiceXmlItem In arrXmlItem.Where(Function(x) String.IsNullOrEmpty(x.MATERIAL_DOCUMENT_NUMBER)).ToList()
                            GravarMensagemIntegracao(CHAVE_ACESSO, itemNotaFiscal.XML_ITEM, datIntegracao, "MIGO", objMessage)
                        Next
                    Next

                    For Each objMigoReturn As SAP_RFC.GoodsMovements In arrMigoReturn
                        vStrMaterialDocNumber = objMigoReturn.MATERIAL_DOCUMENT_NUMBER & ", "

                        IsMigoRealizada = True
                        sqlparams = New List(Of SqlClient.SqlParameter)
                        sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, CHAVE_ACESSO))
                        sqlparams.Add(modSQL.AddSqlParameter("INT_ITEM_NOTA_FISCAL", SqlDbType.Int, itemNF.NF_PROD_ITEM))
                        sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_MAT_DOC_NUMBER", SqlDbType.VarChar, objMigoReturn.MATERIAL_DOCUMENT_NUMBER))
                        sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_MAT_DOC_ITEM", SqlDbType.Int, objMigoReturn.MATERIAL_DOCUMENT_ITEM))
                        sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_MAT_DOC_YEAR", SqlDbType.Int, objMigoReturn.MATERIAL_DOCUMENT_YEAR))
                        sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_PO_NUMBER", SqlDbType.VarChar, objMigoReturn.PURCHASING_DOCUMENT_NUMBER))
                        sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_PO_ITEM", SqlDbType.Int, objMigoReturn.PURCHASING_DOCUMENT_ITEM))
                        sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_NF_ITEM_NUMBER", SqlDbType.Int, objMigoReturn.NOTA_FISCAL_ITEM_NUMBER))
                        sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_NF_QUANTITY", SqlDbType.Decimal, objMigoReturn.NOTA_FISCAL_QUANTITY))
                        sqlparams.Add(modSQL.AddSqlParameter("INT_MIGO_NF_NET_ITEM_VALUE", SqlDbType.Decimal, objMigoReturn.NOTA_FISCAL_NET_ITEM_VALUE))
                        modSQL.ExecuteNonQueryParams(strQuery, sqlparams)
                        vStrMaterialDocNumber = objMigoReturn.MATERIAL_DOCUMENT_NUMBER & ", "

                        'Realiza relacionamento entre os itens retornados na MIGO com os do XML da nota para criar o array de chamada da MIRO
                        Dim tempXmlItem As SAP_RFC.InvoiceXmlItem
                        Dim tempItemNF As modNFItem

                        'Marcio Spinosa - 07/08/2018 - CR00008351
                        'tempItemNF = objNF.ITENS_NF.Where(Function(x) x.NF_PROD_XPED = objMigoReturn.PURCHASING_DOCUMENT_NUMBER And x.NF_PROD_NITEMPED = objMigoReturn.PURCHASING_DOCUMENT_ITEM And Math.Round(x.NF_PROD_QCOM, 3) = objMigoReturn.PO_QUANTITY_UNIT_ERFME And Not vObjCollItens.Contains(x)).FirstOrDefault()
                        tempItemNF = objNF.ITENS_NF.Where(Function(x) x.NF_PROD_XPED = objMigoReturn.PURCHASING_DOCUMENT_NUMBER And x.NF_PROD_NITEMPED = objMigoReturn.PURCHASING_DOCUMENT_ITEM And Math.Round(x.NF_PROD_QCOM, 3) = objMigoReturn.PO_QUANTITY_UNIT_ERFME And x.VNF_ITEM_VALIDO = "S" And Not vObjCollItens.Contains(x)).FirstOrDefault()
                        'Marcio Spinosa - 07/08/2018 - CR00008351 - Fim

                        If Not IsNothing(tempItemNF) Then
                            vObjCollItens.Add(tempItemNF)

                            ' '' ''decValorNetNF = ((tempItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE - tempItemNF.SAP_ITEM_DETAILS.NF_NET_FREIGHT_VALUE - tempItemNF.SAP_ITEM_DETAILS.NF_NET_INSURANCE_VALUE - tempItemNF.SAP_ITEM_DETAILS.NF_NET_OTHER_EXPENSES_VALUES) + tempItemNF.SAP_ITEM_DETAILS.NF_NET_DISCOUNT_VALUE)
                            decValorNetNF = tempItemNF.SAP_ITEM_DETAILS.NF_NET_ITEM_VALUE

                            '---> PARA ITENS COM TAX CODE "F0", SOMAR O VALOR DE ICMS NO VALOR LÍQUIDO
                            If (tempItemNF.SAP_ITEM_DETAILS.TAX_CODE = "F0") Then
                                decValorNetNF += tempItemNF.NF_ICMS_VICMS
                            End If

                            'Marcio Spinosa - 07/08/2018 - CR00008351
                            'tempXmlItem = arrXmlItem.Where(Function(x) x.PURCHASING_DOCUMENT_NUMBER = objMigoReturn.PURCHASING_DOCUMENT_NUMBER And x.PURCHASING_DOCUMENT_ITEM_NUMBER = objMigoReturn.PURCHASING_DOCUMENT_ITEM And tempItemNF.NF_PROD_QCOM = objMigoReturn.PO_QUANTITY_UNIT_ERFME And x.MATERIAL_DOCUMENT_NUMBER = "").FirstOrDefault()
                            tempXmlItem = arrXmlItem.Where(Function(x) x.PURCHASING_DOCUMENT_NUMBER = objMigoReturn.PURCHASING_DOCUMENT_NUMBER And x.PURCHASING_DOCUMENT_ITEM_NUMBER = objMigoReturn.PURCHASING_DOCUMENT_ITEM And Math.Round(tempItemNF.NF_PROD_QCOM, 3) = objMigoReturn.PO_QUANTITY_UNIT_ERFME And tempItemNF.VNF_ITEM_VALIDO = "S" And x.MATERIAL_DOCUMENT_NUMBER = "").FirstOrDefault()
                            'Marcio Spinosa - 07/08/2018 - CR00008351 - Fim
                            tempXmlItem.MATERIAL_DOCUMENT_NUMBER = objMigoReturn.MATERIAL_DOCUMENT_NUMBER
                            tempXmlItem.MATERIAL_DOCUMENT_ITEM = objMigoReturn.MATERIAL_DOCUMENT_ITEM
                            tempXmlItem.MATERIAL_DOCUMENT_YEAR = objMigoReturn.MATERIAL_DOCUMENT_YEAR
                            tempXmlItem.NOTA_REFERENCIA_FATURA_XBLNR_MKPF = objMigoXmlHeader.NOTA_FISCAL
                            tempXmlItem.NET_ITEM_VALUE_NF = IIf(pTipoMiro = "A", decValorNetNF, tempItemNF.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE)
                            tempXmlItem.NET_PRICE_NF = IIf(pTipoMiro = "A", decValorNetNF, tempItemNF.SAP_ITEM_DETAILS.SAP_NET_ITEM_VALUE)
                            tempXmlItem.ITEM_VALUE_WITH_TAXES_NF = tempXmlItem.ITEM_VALUE_WITH_TAXES_NF + tempItemNF.NF_PROD_VFRETE + tempItemNF.NF_PROD_VSEG + tempItemNF.NF_PROD_VOUTRO - tempItemNF.NF_PROD_VDESC
                            tempXmlItem.NET_FREIGHT_VALUE_NF = 0
                            tempXmlItem.NET_FREIGHT_VALUE_PO = 0
                            tempXmlItem.FREIGHT_VALUE_WITH_TAXES = 0
                            tempXmlItem.NET_INSURANCE_VALUE_NF = 0
                            tempXmlItem.INSURANCE_VALUE_WITH_TAXES = 0
                            tempXmlItem.NET_EXPENSES_VALUE_NF = 0
                            tempXmlItem.OTHER_VALUE_WITH_TAXES = 0
                            tempXmlItem.DISCOUNT_VALUE_WITH_TAXES = 0

                            'Marcio Spinosa - 07/08/2018 - CR00008351
                            'arrXmlItem(arrXmlItem.FindIndex(Function(x) x.PURCHASING_DOCUMENT_NUMBER = objMigoReturn.PURCHASING_DOCUMENT_NUMBER And x.PURCHASING_DOCUMENT_ITEM_NUMBER = objMigoReturn.PURCHASING_DOCUMENT_ITEM And tempItemNF.NF_PROD_QCOM = objMigoReturn.PO_QUANTITY_UNIT_ERFME And x.MATERIAL_DOCUMENT_NUMBER = "")) = tempXmlItem
                            arrXmlItem(arrXmlItem.FindIndex(Function(x) x.PURCHASING_DOCUMENT_NUMBER = objMigoReturn.PURCHASING_DOCUMENT_NUMBER And x.PURCHASING_DOCUMENT_ITEM_NUMBER = objMigoReturn.PURCHASING_DOCUMENT_ITEM And Math.Round(tempItemNF.NF_PROD_QCOM, 3) = objMigoReturn.PO_QUANTITY_UNIT_ERFME And tempItemNF.VNF_ITEM_VALIDO = "S" And x.MATERIAL_DOCUMENT_NUMBER = "")) = tempXmlItem
                            'Marcio Spinosa - 07/08/2018 - CR00008351 - Fim
                        End If
                    Next

                    '--> SE DEVERIA CRIAR A MIGO E HOUVE ALGUMA FALHA, NÃO EXECUTA A MIRO
                    If (Not String.IsNullOrEmpty(itemNF.MDP_TIPO_MOVIMENTO_MIGO) AndAlso (arrMigoReturn.Count = 0 OrElse String.IsNullOrEmpty(arrMigoReturn.FirstOrDefault().MATERIAL_DOCUMENT_NUMBER))) Then
                        ''Atualiza status de integração conforme regra de modo processo versus retorno da MIGO
                        modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_STATUS_INTEGRACAO = 'PENDENTE' WHERE NFEID = '" & strNfeId & "'")
                        pTipoMiro = ""
                        If (objSapRet.BapiMessage.Count > 0) Then
                            modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
                            Return "Ocorreu uma falha na MIGO: " & objSapRet.BapiMessage.FirstOrDefault().MESSAGE
                        Else
                            modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
                            Return "Ocorreu uma falha na MIGO: " & objSapRet.Exception.Message
                        End If
                    Else
                        Dim strComplementoRegistroMigo As String = ""
                        For Each oMigoItem As SAP_RFC.GoodsMovementsPurchaseOrder In arrMigoPo
                            If itemNF.SAP_ITEM_DETAILS.CONFIRMATION_CONTROL_KEY <> "0001" Then
                                strComplementoRegistroMigo = " sem referência a inbound delivery"
                            Else
                                strComplementoRegistroMigo = " com referência à inbound " & oMigoItem.INBOUND_DELIVERY_NUMBER & "-" & oMigoItem.INBOUND_DELIVERY_ITEM_NUMBER.ToString()
                            End If
                            BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.MigoRefInboundDelivery, "Foi criada a MIGO " & vStrMaterialDocNumber.Remove(vStrMaterialDocNumber.Length - 2, 2) & " para o item " & oMigoItem.NOTA_FISCAL_ITEM_NUMBER & " da " & objNF.VNF_TIPO_DOCUMENTO & " " & objNF.NF_IDE_NNF & strComplementoRegistroMigo, CHAVE_ACESSO)
                        Next

                        arrMigoPo = New List(Of SAP_RFC.GoodsMovementsPurchaseOrder)()
                        IsMigoRealizada = True
                    End If
                End If
            Next

            If (Not IsMigoRealizada AndAlso String.IsNullOrEmpty(pTipoMiro)) Then
                Return "Nenhum processo foi realizado pelo sistema. Execute a rotina diretamente no SAP."
            ElseIf (objNF.ITENS_NF.Where(Function(x) x.MDP_CRIAR_MIRO = True).Count = 0) Then
                modSQL.ExecuteNonQuery("UPDATE TbIntegracao SET INT_MIRO_MAT_DOC_NUMBER = 'NÃO APLICÁVEL', INT_MIRO_MAT_DOC_YEAR = 0 WHERE nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'")
                ''Atualiza status de integração conforme regra de modo processo versus retorno da MIGO
                modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_STATUS_INTEGRACAO = 'CONCLUÍDO' WHERE NFEID = '" & strNfeId & "'")
            ElseIf (objNF.ITENS_NF.Where(Function(x) x.MDP_AGUARDAR_LIBERACAO_MIGO = True).Count > 0) Then
                modSQL.ExecuteNonQuery("UPDATE TbIntegracao SET INT_MIRO_MAT_DOC_NUMBER = 'AGUARDANDO LIBERAÇÃO DE ESTOQUE', INT_MIRO_MAT_DOC_YEAR = 0 WHERE nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'")
            Else
                '---> CALCULAR O VALOR TOTAL A SER REGISTRADO DA NOTA FISCAL E REMOVE OS ITENS DO XML QUE SÃO COMPONENTES
                Dim decValorTotalNF As Decimal = objNF.NF_ICMSTOT_VNF
                If vBlnIsSubContratacao Then
                    '-->Verifica se não é um CFOP de gerar somente MIGO, caso seja sai da função
                    If objNF.ITENS_NF.Where(Function(x) x.NF_PROD_CFOP = "5903" Or x.NF_PROD_CFOP = "6903").ToList().Count() > 0 Then
                        Return String.Empty
                    End If

                    For Each itemIrrelevante As modNFItem In objNF.ITENS_NF
                        'Marcio Spinosa - 24/04/2018 - CR00008351
                        'If (itemIrrelevante.VNF_IS_SUBCONTRATACAO And itemIrrelevante.NF_PROD_CFOP <> "5124") Then
                        If (itemIrrelevante.VNF_IS_SUBCONTRATACAO And vstrCFOPComp.Contains(itemIrrelevante.NF_PROD_CFOP)) Then
                            'Marcio Spinosa - 24/04/2018 - CR00008351 - Fim
                            decValorTotalNF -= ((itemIrrelevante.NF_PROD_VPROD + itemIrrelevante.NF_IPI_VIPI + itemIrrelevante.NF_PROD_VFRETE + itemIrrelevante.NF_PROD_VSEG + itemIrrelevante.NF_PROD_VOUTRO) - itemIrrelevante.NF_PROD_VDESC)
                            arrXmlItem.Remove(arrXmlItem.Where(Function(x) x.XML_ITEM = itemIrrelevante.NF_PROD_ITEM).FirstOrDefault())
                        End If
                    Next
                Else
                    If (arrXmlItem.Count < objNF.ITENS_NF.Count) Then
                        For Each itemIrrelevante As modNFItem In objNF.ITENS_NF
                            If (itemIrrelevante.VNF_ITEM_VALIDO = "X") Then
                                decValorTotalNF -= ((itemIrrelevante.NF_PROD_VPROD + itemIrrelevante.NF_IPI_VIPI + itemIrrelevante.NF_PROD_VFRETE + itemIrrelevante.NF_PROD_VSEG + itemIrrelevante.NF_PROD_VOUTRO) - itemIrrelevante.NF_PROD_VDESC)
                            End If
                        Next
                    End If
                End If
                '---> CRIAR MIRO
                Dim objHeader As SAP_RFC.InvoiceHeader = New SAP_RFC.InvoiceHeader()
                objHeader.INVOICE = IIf(pTipoMiro = "A", "X", "")
                If Not String.IsNullOrWhiteSpace(objNF.NF_IDE_SERIE) Then
                    objHeader.NOTA_FISCAL = objNF.NF_IDE_NNF & "-" & objNF.NF_IDE_SERIE
                Else
                    objHeader.NOTA_FISCAL = objNF.NF_IDE_NNF
                End If

                If vBlnIsNotaComplementar Then
                    objHeader.I_IS_SUBSEQUENT_DEBIT = "X"
                End If


                objHeader.DOCUMENT_DATE = objNF.NF_IDE_DHEMI
                objHeader.RECEIVED_DATE = objNF.NF_IDE_DHEMI
                objHeader.TOTAL_VALUE_WITH_TAXES = decValorTotalNF
                objHeader.VENDOR_CNPJ = objNF.NF_EMIT_CNPJ
                objHeader.NFE_ID = CHAVE_ACESSO
                objHeader.XML_VERSION = objNF.NF_OUTROS_VERSAO
                objHeader.PROTOCOL_NUMBER = ""
                objHeader.VENDOR_NUMBER = objNF.SAP_DETAILS.VENDOR_CODE
                objHeader.REMARK = ""
                objHeader.FREIGHT_MODEL = objNF.NF_IDE_MODAL
                objHeader.NF_TYPE = objNF.ITENS_NF.FirstOrDefault().MDP_TIPO_NF

                Dim objMiroMessage As List(Of SAP_RFC.BapiMessage) = New List(Of SAP_RFC.BapiMessage)()
                Dim objSapRetMiro As SAP_RFC.RfcReturn = New SAP_RFC.RfcReturn()
                Dim strDocumentNumber As String = String.Empty
                Dim intDocumentYear As Integer = 0
                datIntegracao = DateTime.Now

                objMiroMessage = SAP_RFC.createInvoice(SapUser, SapPassword, objHeader, arrXmlItem, strDocumentNumber, intDocumentYear, objSapRetMiro)
                
                modSQL.ExecuteNonQuery("UPDATE TbIntegracao SET INT_MIRO_MAT_DOC_NUMBER = '" & strDocumentNumber & "', INT_MIRO_MAT_DOC_YEAR = " & intDocumentYear.ToString() & " WHERE nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'")
                For Each objMessage As SAP_RFC.BapiMessage In objMiroMessage
                    For Each itemNotaFiscal As SAP_RFC.InvoiceXmlItem In arrXmlItem
                        GravarMensagemIntegracao(CHAVE_ACESSO, itemNotaFiscal.XML_ITEM, datIntegracao, "MIRO", objMessage)
                    Next
                Next

                If (String.IsNullOrEmpty(strDocumentNumber)) Then
                    ''Atualiza status de integração conforme regra de modo processo versus retorno da MIRO
                    If IsMigoRealizada Then
                        modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_STATUS_INTEGRACAO = 'INCOMPLETA' WHERE NFEID = '" & strNfeId & "'")
                    Else
                        modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_STATUS_INTEGRACAO = 'PENDENTE' WHERE NFEID = '" & strNfeId & "'")
                    End If

                    If (objMiroMessage.Count > 0) Then
                        modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
                        Return "Ocorreu uma falha na MIRO:" & objMiroMessage.FirstOrDefault().MESSAGE
                    Else
                        modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
                        Return "Ocorreu uma falha na MIRO:" & objSapRetMiro.Exception.Message
                    End If
                Else
                    ''Atualiza status de integração conforme regra de modo processo versus retorno da MIRO
                    modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_STATUS_INTEGRACAO = 'CONCLUÍDO' WHERE NFEID = '" & strNfeId & "'")
                    BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.Miro, "Criou a MIRO " & strDocumentNumber & " para a " & objNF.VNF_TIPO_DOCUMENTO & objNF.NF_IDE_NNF, CHAVE_ACESSO)
                End If
            End If

            Return String.Empty
        Catch ex As Exception
            If IsMigoRealizada Then
                modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_STATUS_INTEGRACAO = 'INCOMPLETA' WHERE NFEID = '" & strNfeId & "'")
            Else
                modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_STATUS_INTEGRACAO = 'PENDENTE' WHERE NFEID = '" & strNfeId & "'")
            End If
            Return ex.Message
        Finally
            modSQL.ExecuteNonQuery("UPDATE TbIntegracaoPostagens SET ipo_data_conclusao = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE ipo_nfeid = '" & strNfeId & "' and ipo_data_conclusao is null")
        End Try
    End Function

    Public Function MIROExists(ByVal NF_Numero As String, ByVal NF_Serie As String, ByVal NF_Data_Emissao As Date, ByVal CNPJ_EMITENTE As String) As Boolean
        Return MIROExists(NF_Numero, NF_Serie, NF_Data_Emissao, CNPJ_EMITENTE, "", "")
    End Function

    Public Function MIROExists(ByVal NF_Numero As String, ByVal NF_Serie As String, ByVal NF_Data_Emissao As Date, ByVal CNPJ_EMITENTE As String, _
                               ByVal SapUser As String, ByVal SapPassword As String) As Boolean

        'Verifica se já não existe MIRO criada para esse nuúmero/serie de nota
        Dim vObjInput As SAP_RFC.SubsequentDebitImport = New SubsequentDebitImport()
        Dim vObjConsultaNF As SAP_RFC.SubsequentDebitNf

        vObjInput.arrNotaFiscal = New List(Of SAP_RFC.SubsequentDebitNf)()
        vObjInput.arrAllocation = New List(Of SAP_RFC.SubsequentDebitAllocation)()

        If String.IsNullOrWhiteSpace(NF_Serie) Then
            vObjConsultaNF.NF_REFERENCE = NF_Numero
        Else
            vObjConsultaNF.NF_REFERENCE = NF_Numero & "-" & NF_Serie
        End If

        vObjConsultaNF.CNPJ_VENDOR = CNPJ_EMITENTE
        vObjConsultaNF.NF_DATE = NF_Data_Emissao
        vObjInput.arrNotaFiscal.Add(vObjConsultaNF)

        Dim objReturn As SAP_RFC.SubsequentDebitReturn
        objReturn = SAP_RFC.createSubsequentDebit(SapUser, SapPassword, vObjInput)

        Return objReturn.NotaFiscal(0).STATUS = "S"
    End Function


    Public Function EnviarSapDebitoPosterior(ByVal SapUser As String, ByVal SapPassword As String, ByVal PostDocument As Boolean, ByVal Cte As modNF) As SAP_RFC.SubsequentDebitReturn

        If Boolean.Parse(Uteis.GetSettingsValue(Of String)("FixedSapSystemUser").ToString()) = True Then
            SapUser = Uteis.GetSettingsValue(Of String)("User").ToString()
            SapPassword = Uteis.GetSettingsValue(Of String)("Password").ToString()
        End If


        Dim decAliquotaIcms As Decimal = 0
        Dim decAliquotaPis As Decimal = 0
        Dim decAliquotaCofins As Decimal = 0
        Dim strNumeroNf As String
        Dim strSerieNf As String

        If Not Cte.ITENS_NF.FirstOrDefault().SAP_ITEM_DETAILS.ITEM_TAXES Is Nothing Then
            decAliquotaIcms = Cte.ITENS_NF.FirstOrDefault().SAP_ITEM_DETAILS.ITEM_TAXES.Where(Function(x) x.TAX_NAME = "ICMS").FirstOrDefault().TAX_PERCENTAGE / 100
            decAliquotaPis = Cte.ITENS_NF.FirstOrDefault().SAP_ITEM_DETAILS.ITEM_TAXES.Where(Function(x) x.TAX_NAME = "PIS").FirstOrDefault().TAX_PERCENTAGE / 100
            decAliquotaCofins = Cte.ITENS_NF.FirstOrDefault().SAP_ITEM_DETAILS.ITEM_TAXES.Where(Function(x) x.TAX_NAME = "COFINS").FirstOrDefault().TAX_PERCENTAGE / 100
        End If

        Dim objInput As SAP_RFC.SubsequentDebitImport
        If (Cte.NF_IDE_NNF.Length > 6) Then
            objInput.CTE = Cte.NF_IDE_NNF.Substring(Cte.NF_IDE_NNF.Length - 6).TrimStart("0")
        Else
            objInput.CTE = Cte.NF_IDE_NNF
        End If
        objInput.DOC_DATE = Cte.NF_IDE_DHEMI
        objInput.NF_TYPE = Cte.ITENS_NF.FirstOrDefault().MDP_TIPO_NF
        objInput.POST = ""
        objInput.CNPJ_VENDOR = Cte.NF_EMIT_CNPJ
        objInput.VENDOR_CODE = GetVendorCodeFretes(Cte.VNF_CHAVE_ACESSO, Cte.NF_EMIT_CNPJ)
        objInput.CFOP = GetCfopEntrada(Cte)
        objInput.TAX_CODE = "" '--> EM BRANCO
        objInput.PAYMENT_TERMS = "Y15N"
        objInput.NUM_DAYS = 0D '--> EM BRANCO
        objInput.VALUE = Cte.ITENS_NF.FirstOrDefault().NF_PROD_VPROD
        objInput.ICMS = 0 'Cte.ITENS_NF.FirstOrDefault().NF_ICMS_VICMS
        objInput.PIS = 0 'Math.Round(Cte.ITENS_NF.FirstOrDefault().NF_PROD_VPROD * decAliquotaPis, 2)
        objInput.COFINS = 0 'Math.Round(Cte.ITENS_NF.FirstOrDefault().NF_PROD_VPROD * decAliquotaCofins, 2)
        If (Not String.IsNullOrEmpty(Uteis.GetSettingsValue(Of String)("SerieMigoCte"))) Then
            objInput.SERIE = Uteis.GetSettingsValue(Of String)("SerieMigoCte")
        Else
            objInput.SERIE = Cte.NF_IDE_SERIE
        End If

        objInput.arrNotaFiscal = New List(Of SAP_RFC.SubsequentDebitNf)()
        objInput.arrAllocation = New List(Of SAP_RFC.SubsequentDebitAllocation)()

        Dim objNotaFiscal As modNF
        Dim objNf As SAP_RFC.SubsequentDebitNf
        For Each itemCte As modNFItem In Cte.ITENS_NF
            objNf = New SAP_RFC.SubsequentDebitNf()
            objNotaFiscal = New modNF()
            objNotaFiscal = GetByID(itemCte.CT_INFNFE_CHAVE, False)

            '--> SE A NOTA NÃO DEVE SER RATEADA, NÃO DEVE SER ADICIONADA NA LISTA DE ITENS DO SAP
            If (Not objNotaFiscal.VNF_RATEAR Or objNotaFiscal.VNF_STATUS = "NÃO ENCONTRADO") Then
                Continue For
            End If

            If (objNotaFiscal.NF_IDE_NNF.Length > 6) Then
                strNumeroNf = objNotaFiscal.NF_IDE_NNF.Substring(objNotaFiscal.NF_IDE_NNF.Length - 6).TrimStart("0")
            Else
                strNumeroNf = objNotaFiscal.NF_IDE_NNF
            End If

            If (Not String.IsNullOrEmpty(Uteis.GetSettingsValue(Of String)("SerieMigoMiro"))) Then
                strSerieNf = Uteis.GetSettingsValue(Of String)("SerieMigoMiro")
            Else
                strSerieNf = objNotaFiscal.NF_IDE_SERIE
            End If

            objNf.NF_REFERENCE = strNumeroNf & "-" & strSerieNf
            objNf.CNPJ_VENDOR = objNotaFiscal.NF_EMIT_CNPJ
            objNf.NF_DATE = objNotaFiscal.NF_IDE_DHEMI.ToString("yyyy-MM-dd").ToDateTime()
            objInput.arrNotaFiscal.Add(objNf)
        Next

        Dim objReturn As SAP_RFC.SubsequentDebitReturn
        If (Not objInput.arrNotaFiscal Is Nothing AndAlso objInput.arrNotaFiscal.Count > 0) Then
            objReturn = SAP_RFC.createSubsequentDebit(SapUser, SapPassword, objInput)
        End If

        If (Not objReturn.Allocation Is Nothing) Then
            Dim decValorReducaoBase As Decimal = 0
            Dim decValorBaseReduzida As Decimal = 0

            '--> SE TIVER ITEM DE USO/CONSUMO/ATIVO, REDETERMINA ICMS, PIS e COFINS PARA RETENÇÃO DE IMPOSTOS
            If (objReturn.Allocation.Where(Function(x) x.USAGE = "2" Or x.USAGE = "3").Count > 0) Then
                For Each itemUsoConsumo As SAP_RFC.SubsequentDebitAllocation In objReturn.Allocation.Where(Function(x) x.USAGE = "2" Or x.USAGE = "3")
                    decValorReducaoBase += itemUsoConsumo.CTE_ALLOCATION_GROSS
                Next
            End If

            decValorBaseReduzida = objInput.VALUE - decValorReducaoBase

            'Condicao aplicada pelo Pedro.h.araujo redondamento.
            'If decValorReducaoBase = 0 Then
            '    objInput.ICMS = valorIcmsNoXmldoCTE
            'EElse
            ' objInput.ICMS = Math.Round(decValorBaseReduzida * decAliquotaIcms, 2)
            'End If

            objInput.ICMS = Math.Round(decValorBaseReduzida * decAliquotaIcms, 2)
            objInput.PIS = Math.Round(decValorBaseReduzida * decAliquotaPis, 2)
            objInput.COFINS = Math.Round(decValorBaseReduzida * decAliquotaCofins, 2)
            objReturn = SAP_RFC.createSubsequentDebit(SapUser, SapPassword, objInput)

            '--> COMPLEMENTA COM A INFORMAÇÃO DE TAX CODE
            Dim arrAllocationReturn As New List(Of SAP_RFC.SubsequentDebitAllocation)()
            For Each itemNfAllocation As SAP_RFC.SubsequentDebitAllocation In objReturn.Allocation
                itemNfAllocation.TAX_CODE = GetTaxCode(itemNfAllocation.USAGE, objInput.ICMS)
                arrAllocationReturn.Add(itemNfAllocation)
            Next
            objReturn.Allocation = arrAllocationReturn
            objInput.arrAllocation = arrAllocationReturn

        End If

        If (PostDocument And objReturn.SapReturn.Success) Then

            '--> POSTA O DOCUMENTO NO SAP
            objInput.POST = "X"
            objReturn = SAP_RFC.createSubsequentDebit(SapUser, SapPassword, objInput)

            '--> GRAVA AS MENSAGENS RETORNADAS PELO SAP
            For Each objMessage As SAP_RFC.BapiMessage In objReturn.SapReturn.BapiMessage
                GravarMensagemIntegracao(Cte.VNF_CHAVE_ACESSO, 1, DateTime.Now(), "MIRO", objMessage)
            Next

            '--> ATUALIZA O STATUS DO DOCUMENTO NA TABELA DE INTEGRAÇÃO
            If (Not String.IsNullOrEmpty(objReturn.DOCUMENT_NUMBER)) Then
                Dim strInsertTbIntegracao As String = "INSERT INTO TbIntegracao( " &
                                                  " 	 NFEID " &
                                                  " 	,INT_MIGO_MAT_DOC_NUMBER " &
                                                  " 	,INT_MIRO_MAT_DOC_NUMBER " &
                                                  " 	,INT_MIRO_MAT_DOC_YEAR " &
                                                  ") VALUES ( " &
                                                  " 	 '" & Cte.VNF_CHAVE_ACESSO & "' " &
                                                  " 	,'NÃO APLICÁVEL' " &
                                                  " 	,'" & objReturn.DOCUMENT_NUMBER & "' " &
                                                  " 	," & objReturn.FISCAL_YEAR & " " &
                                                  ") "
                modSQL.ExecuteNonQuery(strInsertTbIntegracao)
                modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_STATUS_INTEGRACAO = 'CONCLUÍDO' WHERE NFEID = '" & Cte.VNF_CHAVE_ACESSO & "'")
                BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.Miro, "Criou a MIRO " & objReturn.DOCUMENT_NUMBER & " para a " & GetNumeroDocumento(Cte.VNF_CHAVE_ACESSO), Cte.VNF_CHAVE_ACESSO)
            End If
        End If

        Return objReturn
    End Function

    Public Function AjustarNumeroNf(ByVal numeroNf As String) As String
        If (numeroNf.Length > 6) Then
            Return numeroNf.Substring(numeroNf.Length - 6).ToInt().ToString()
        Else
            Return numeroNf
        End If
    End Function

    Public Function AlterarRateio(ByVal NfeID As String, ByVal Ratear As Boolean) As String
        Try
            modSQL.ExecuteNonQuery("UPDATE TbDOC_CAB SET vnf_ratear = " & Ratear.ToInt() & " WHERE nfeid = '" & NfeID & "'")
            Return String.Empty
        Catch ex As Exception
            Return "Ocorreu uma falha na alteração. O processo não foi concluído."
        End Try
    End Function

    Public Function GetVendorCode(ByVal CNPJ As String) As String
        If (Not CNPJ Is Nothing) Then
            Dim objBLVendors As New BLVendors()
            Dim arrVendors As New List(Of modVendors)()
            arrVendors = objBLVendors.GetByCNPJ(CNPJ)
            If (arrVendors.Count > 0) Then
                Return arrVendors.FirstOrDefault().VendorCode
            End If
        End If
        Return String.Empty
    End Function

    Public Function GetVendorCodeFretes(ByVal Nfeid As String, ByVal CnpjTransportadora As String) As String
        Dim objReturn = modSQL.ExecuteScalar("SELECT CodigoParceiroNegocio FROM vwRelatorioSAP WHERE ChaveAcessoCTE = '" & Nfeid & "'", modSQL.connectionStringFretes)

        If (objReturn Is Nothing) Then
            Return GetVendorCode(CnpjTransportadora)
        Else
            Return objReturn.ToString()
        End If
    End Function

    Public Function GetTaxCode(ByVal usage As String, icms As Decimal) As String
        Return modSQL.ExecuteScalar("SELECT tdp_tax_code FROM TbTaxCodeDebitoPosterior WHERE tdp_material_usage = '" & usage & "' and tdp_icms = " & IIf(icms = 0, 0, 1) & " ").ToString()
    End Function

    Public Function GetTipoFrete() As DataTable
        Dim query = "SELECT DISTINCT " &
                    "        CASE [ac].[cac_tipo_pagamento] " &
                    "          WHEN 1 THEN 'Débito Posterior' " &
                    "          WHEN 2 THEN 'Frete Pedido' " &
                    "          WHEN 3 THEN 'Frete Misto' " &
                    "          WHEN 4 THEN 'Importação' " &
                    "          WHEN 5 THEN 'Exportação' " &
                    "          ELSE 'Não Definido' " &
                    "        END AS 'TipoFrete' " &
                    "FROM [cac_analise_cte] ac "
        Return modSQL.Fill(query, modSQL.connectionStringFretes)
    End Function

    Public Function GetStatusIntegracao() As DataTable
        'Dim query = "SELECT DISTINCT " & _
        '            "CASE " & _
        '            "		WHEN VNF_TIPO_DOCUMENTO = 'CTE' and count(distinct INT_MIRO_MAT_DOC_NUMBER) = 1 THEN " & _
        '            "        'CONCLUÍDO' " & _
        '            "		WHEN count(distinct int_migo_nf_item_number) = 0 THEN " & _
        '            "        'PENDENTE' " & _
        '            "		WHEN count(distinct int_migo_nf_item_number) < (select count(distinct nf_prod_item) from tbdoc_item_nfe where tbdoc_item_nfe.nfeid = TbIntegracao.nfeid) THEN " & _
        '            "        'INCOMPLETA' " & _
        '            "		ELSE " & _
        '            "        'CONCLUÍDO' " & _
        '            "	END as 'STATUS_INTEGRACAO' " & _
        '            "        FROM TbDOC_CAB " & _
        '            " LEFT JOIN TbIntegracao ON TbDOC_CAB.NFEID = TbIntegracao.NFEID " & _
        '            "        GROUP BY " & _
        '            "	TbDOC_CAB.NFEID, " & _
        '            "	TbIntegracao.NFEID, " & _
        '            "	TbDOC_CAB.VNF_TIPO_DOCUMENTO "
        Dim query = "  Select DISTINCT " &
                    "         SAP_STATUS_INTEGRACAO as 'STATUS_INTEGRACAO' " &
                    "  FROM TbNFE " &
                    " WHERE SAP_STATUS_INTEGRACAO IS NOT NULL "

        Return modSQL.Fill(query)
    End Function

    Public Function GetCfopEntrada(ByVal pCte As modNF) As String
        Dim strUfTransportadora As String = pCte.NF_EMIT_UF
        Dim strUfTomador As String = pCte.CT_TOMA_UF
        Dim strUfOrigem As String = pCte.CT_IDE_UFINI
        Dim strCfopEntrada As String = ""

        If (strUfTransportadora = strUfTomador) Then
            strCfopEntrada = "1"
        Else
            strCfopEntrada = "2"
        End If

        If (strUfOrigem = strUfTransportadora) Then
            strCfopEntrada += "352"
        Else
            strCfopEntrada += "932"
        End If

        Return strCfopEntrada + "AA"
    End Function
    Public Function GetCFOPEscrituracaoMIRO(pStrCFOPXML As String, pStrCFOPSAP As String) As String
        Dim vObjCFOPEscrituracao As Object

        vObjCFOPEscrituracao = modSQL.ExecuteScalar("EXEC SP_GET_CFOP_AJSUTE_MIRO_ESCRITURAR '" & pStrCFOPXML & "', '" & pStrCFOPSAP & "'")
        If vObjCFOPEscrituracao = Nothing Then
            Return ""
        Else
            Return vObjCFOPEscrituracao.ToString()
        End If

    End Function

    Private Sub GravarMensagemIntegracao(ByVal CHAVE_ACESSO As String, ByVal pItemNotaFiscal As Int32, ByVal datIntegracao As DateTime, ByVal pCategory As String, ByVal objMessage As SAP_RFC.BapiMessage)
        Dim strQuery As String
        strQuery = "INSERT INTO TbIntegracaoMensagens " &
                           "( " &
                           "	 NFEID " &
                           "	,SAP_ITEM_NOTA_FISCAL " &
                           "	,SAP_CATEGORY " &
                           "	,SAP_DATE_INSERT " &
                           "	,SAP_TYPE " &
                           "	,SAP_ID " &
                           "	,SAP_NUMBER " &
                           "	,SAP_MESSAGE " &
                           "	,SAP_LOG_NO " &
                           "	,SAP_LOG_MSG_NO " &
                           "	,SAP_MESSAGE_V1 " &
                           "	,SAP_MESSAGE_V2 " &
                           "	,SAP_MESSAGE_V3 " &
                           "	,SAP_MESSAGE_V4 " &
                           "	,SAP_PARAMETER " &
                           "	,SAP_ROW " &
                           "	,SAP_FIELD " &
                           "	,SAP_SYSTEM " &
                           ") " &
                           "VALUES " &
                           "( " &
                           "	 @NFEID " &
                           "	,@SAP_ITEM_NOTA_FISCAL " &
                           "	,@SAP_CATEGORY " &
                           "	,@SAP_DATE_INSERT " &
                           "	,@SAP_TYPE " &
                           "	,@SAP_ID " &
                           "	,@SAP_NUMBER " &
                           "	,@SAP_MESSAGE " &
                           "	,@SAP_LOG_NO " &
                           "	,@SAP_LOG_MSG_NO " &
                           "	,@SAP_MESSAGE_V1 " &
                           "	,@SAP_MESSAGE_V2 " &
                           "	,@SAP_MESSAGE_V3 " &
                           "	,@SAP_MESSAGE_V4 " &
                           "	,@SAP_PARAMETER " &
                           "	,@SAP_ROW " &
                           "	,@SAP_FIELD " &
                           "	,@SAP_SYSTEM " &
                           ") "

        Dim sqlparams As New List(Of SqlClient.SqlParameter)
        sqlparams = New List(Of SqlClient.SqlParameter)
        sqlparams.Add(modSQL.AddSqlParameter("NFEID", SqlDbType.VarChar, CHAVE_ACESSO))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_ITEM_NOTA_FISCAL", SqlDbType.Int, pItemNotaFiscal))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_CATEGORY", SqlDbType.VarChar, pCategory))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_DATE_INSERT", SqlDbType.DateTime, datIntegracao))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_TYPE", SqlDbType.VarChar, objMessage.TYPE))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_ID", SqlDbType.VarChar, objMessage.ID))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_NUMBER", SqlDbType.Decimal, objMessage.NUMBER))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_MESSAGE", SqlDbType.VarChar, objMessage.MESSAGE))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_LOG_NO", SqlDbType.VarChar, objMessage.LOG_NO))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_LOG_MSG_NO", SqlDbType.Decimal, objMessage.LOG_MSG_NO))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_MESSAGE_V1", SqlDbType.VarChar, objMessage.MESSAGE_V1))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_MESSAGE_V2", SqlDbType.VarChar, objMessage.MESSAGE_V2))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_MESSAGE_V3", SqlDbType.VarChar, objMessage.MESSAGE_V3))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_MESSAGE_V4", SqlDbType.VarChar, objMessage.MESSAGE_V4))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_PARAMETER", SqlDbType.VarChar, objMessage.PARAMETER))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_ROW", SqlDbType.Int, objMessage.ROW))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_FIELD", SqlDbType.VarChar, objMessage.FIELD))
        sqlparams.Add(modSQL.AddSqlParameter("SAP_SYSTEM", SqlDbType.VarChar, objMessage.SYSTEM))
        modSQL.ExecuteNonQueryParams(strQuery, sqlparams)

        AtualizarDataIntegracao(CHAVE_ACESSO, datIntegracao)
    End Sub

    Public Sub AtualizarDataIntegracao(ByVal NFEID As String, ByVal DataIntegracao As Date)
        Try
            Dim dtText = DataIntegracao.ToString("yyyy-MM-dd HH:mm:ss")
            modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_DATE_INSERT = '" & DataIntegracao.ToString("yyyy-MM-dd HH:mm:ss") &
                                   "' WHERE NFEID = '" & NFEID &
                                   "' AND ISNULL(SAP_DATE_INSERT,  CAST('" & dtText & "' as Datetime)) <= CAST('" & dtText & "' as Datetime)")
        Catch
        End Try
    End Sub

    Public Function PodeModificar(ByVal NfeId As String) As Boolean
        Dim Situacao_Ok As Boolean
        Dim IntegracaoConcluida As Boolean

        Return PodeModificar(NfeId, Situacao_Ok, IntegracaoConcluida)
    End Function

    Public Function PodeModificar(ByVal NfeId As String, ByRef Situacao_Ok As Boolean, ByRef IntegracaoConcluida As Boolean) As Boolean

        Dim vBlnPodeModificar As Boolean = True
        Situacao_Ok = True

        If Not String.IsNullOrWhiteSpace(NfeId) Then
            Dim dtResult As DataTable = modSQL.Fill("SELECT [NFEID] ,[SITUACAO] ,[STATUS_INTEGRACAO] ,[PODE_MODIFICAR] FROM vwPodeModificarDocumento WHERE  nfeid = '" + NfeId + "' ")

            If (dtResult.Rows.Count > 0) Then
                'Marcio Spinosa - 24/04/2019 - CR00009165
                'Dim situacao As String = dtResult.Rows(0)("SITUACAO")
                Dim situacao As String = If(Not IsDBNull(dtResult.Rows(0)("SITUACAO")), dtResult.Rows(0)("SITUACAO"), "")
                'Marcio Spinosa - 24/04/2019 - CR00009165 - Fim
                Dim statusIntegracao As String = dtResult.Rows(0)("STATUS_INTEGRACAO")
                Dim isPodeModificar As Int16 = Convert.ToInt16(dtResult.Rows(0)("PODE_MODIFICAR"))

                If (situacao = "RECUSADA" Or situacao = "CANCELADA") Then
                    Situacao_Ok = False
                End If

                If statusIntegracao = "CONCLUÍDO" Then
                    IntegracaoConcluida = True
                Else
                    IntegracaoConcluida = False
                End If

                If (isPodeModificar = 0) Then
                    vBlnPodeModificar = False
                End If
            End If
        End If

        Return vBlnPodeModificar
    End Function

    Public Function ImprimirSLIP(ByVal pStrDocumentModel As String) As Boolean

        Dim objRetorno As Object


        Try
            Select Case pStrDocumentModel
                Case modNF.tipo_doc_nfe
                    objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'IMPRIMIR_SLIP_NFE' ")
                Case modNF.tipo_doc_cte
                    objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'IMPRIMIR_SLIP_CTE' ")
                Case modNF.tipo_nfse
                    objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'IMPRIMIR_SLIP_NFSE' ")
                Case modNF.tipo_doc_talonario
                    objRetorno = modSQL.ExecuteScalar("EXEC SP_GET_PARAMETRO 'IMPRIMIR_SLIP_TALONARIO' ")
                Case Else
                    Return False
            End Select

            If objRetorno.ToString() = "SIM" Then
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            Throw ex
        End Try



    End Function

    Public Sub UpdateStatusIntegracaoForJ1B1N(ByVal pStrNFEID As String, ByVal pStrStatusIntegracao As String)

        Try
            If pStrStatusIntegracao = "CONCLUÍDO" Then
                modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_STATUS_INTEGRACAO = '" & pStrStatusIntegracao & "', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', SAP_DATE_INSERT = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "' WHERE NFEID = '" & pStrNFEID & "'")
            Else
                modSQL.ExecuteNonQuery("UPDATE TBNFE SET SAP_STATUS_INTEGRACAO = '" & pStrStatusIntegracao & "' WHERE NFEID = '" & pStrNFEID & "'")
            End If



        Catch ex As Exception

        End Try



    End Sub
    'Marcio Spinosa - 28/05/2018 - CR00008351
    ''' <summary>
    ''' Método para retornar o Nome do usuário cadastrado no VNF
    ''' </summary>
    ''' <param name="pStrlogon"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>Marcio Spinosa - 28/05/2018 - CR00008351 - Foi ajustado no sql o retorno dos dados do funcionário</example>
    Public Function getUserNameByLogon(ByVal pStrlogon As String)
        Dim pstrUserName As String
        pstrUserName = modSQL.ExecuteScalar("SELECT usunomusu FROM TbUsuario where usucodusu = '" & Uteis.LogonName() & "'")
        Return pstrUserName
    End Function
    'Marcio Spinosa - 28/05/2018 - CR00008351

End Class

