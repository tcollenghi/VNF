Public Class BLTriangulus

    Public Function getFilial(ByVal pConnection As String) As DataTable
        Dim dtFilial As New DataTable
        Dim selectFilial As String = "SELECT DISTINCT ID_FILIAL, CNPJ, UF FROM NFE03_FILIAL"
        dtFilial = modSQL.Fill(selectFilial, pConnection)
        Return dtFilial
    End Function


    Public Function verificaRetornoNF(ByVal pConnection As String, ByVal objNFSaida As NFSaidaTriangulus, ByVal pstrFiliais As String) As String
        Dim dtResult As New DataTable
        Dim dtrow As DataRow
        Dim str24Horas As String = modSQL.ExecuteScalar("select VALOR from tbpar where parametro = 'CANCELAMENTO_24_HORAS'")
        Dim pstrSQL As String = ""
        If Not (String.IsNullOrEmpty(objNFSaida.Nfeid)) Then
            pstrSQL = "select top 1 max(id_lista), id_filial, cfg_numero_nf, cfg_serie_nf, stat, dt_emissao, chave_acesso 
                                 from nfp01_lista 
                                where chave_acesso = '{0}' and id_filial = {1} and dt_emissao = '{2}' 
                                group by id_filial, cfg_numero_nf, cfg_serie_nf, stat, dt_emissao, chave_acesso order by 1 desc"

            pstrSQL = String.Format(pstrSQL,
                                objNFSaida.Nfeid.ToString(),
                                objNFSaida.IDFilial.ToString(),
                                objNFSaida.DTEmissao.ToString("yyyy-MM-dd"))

        ElseIf Not (String.IsNullOrEmpty(objNFSaida.NFNumero)) Then
            pstrSQL = "select top 1 max(id_lista), id_filial, cfg_numero_nf, cfg_serie_nf, stat, dt_emissao, chave_acesso 
                                 from nfp01_lista 
                                where cfg_numero_nf = {0} and cfg_serie_nf = {1} and id_filial = {2} and dt_emissao = '{3}' 
                                group by id_filial, cfg_numero_nf, cfg_serie_nf, stat, dt_emissao, chave_acesso order by 1 desc"

            pstrSQL = String.Format(pstrSQL,
                                objNFSaida.NFNumero.ToString(),
                                objNFSaida.Serie.ToString(),
                                objNFSaida.IDFilial.ToString(),
                                objNFSaida.DTEmissao.ToString("yyyy-MM-dd"))
        End If

        dtResult = modSQL.Fill(pstrSQL, pConnection)


        'se o resultado for maior que 0, existe a nota emitida
        If Not (str24Horas.Contains(objNFSaida.IDFilial.ToString())) Then
            If (dtResult.Rows.Count > 0) Then
                dtrow = dtResult.Select().FirstOrDefault()
                If (objNFSaida.DTEmissao = Convert.ToDateTime(dtrow(5).ToString())) Then
                    objNFSaida.NFNumero = dtrow(2).ToString()
                    objNFSaida.Serie = dtrow(3).ToString()
                    objNFSaida.IDFilial = dtrow(1).ToString()
                    objNFSaida.DTEmissao = Convert.ToDateTime(dtrow(5).ToString())
                    If (Convert.ToDateTime(dtrow(5).ToString()) < DateTime.Now.AddDays(-7) And Not (objNFSaida.Nfeid Is Nothing)) Then
                        ' verifica se a nota está com status 100
                        If (dtrow(4).ToString() = "100") Then
                            If (Convert.ToDateTime(dtrow(5).ToString()) > DateTime.Now.AddDays(-20)) Then
                                If Not (pstrFiliais.Contains(objNFSaida.IDFilial.ToString())) Then
                                    Return "Cancelar"
                                ElseIf (pstrFiliais.Contains(objNFSaida.IDFilial.ToString())) Then
                                    Return "não cancelar"
                                Else
                                    Return "Inutilizar"
                                End If
                            Else
                                Return "data_excedida"
                            End If
                        End If
                    ElseIf (objNFSaida.Nfeid Is Nothing) Then
                        Return "Inutilizar"
                    Else
                        Return "data"
                    End If
                Else
                    Return "data"
                End If
                'Marcio Spinosa - 14/06/2019 - SR00284682 
            Else
                Return "Inutilizar"
                'Marcio Spinosa - 14/06/2019 - SR00284682 - Fim
            End If
        Else
            Return "24_horas"
        End If

    End Function

    Public Function CancelarChave(ByVal pConnection As String, ByVal objNFSaida As NFSaidaTriangulus, ByVal pstrUser As String) As Integer
        Dim dtFilial, dtResult As New DataTable
        Dim dtobj, dtrow As DataRow

        Try

            Dim pstrSQL = "select top 1 max(id_lista), id_filial, cfg_numero_nf, cfg_serie_nf, stat, dt_emissao, chave_acesso 
                                 from nfp01_lista 
                                where chave_acesso = '{0}' and id_filial = {1} and dt_emissao = '{2}' 
                                group by id_filial, cfg_numero_nf, cfg_serie_nf, stat, dt_emissao, chave_acesso order by 1 desc"

            pstrSQL = String.Format(pstrSQL,
                                    objNFSaida.Nfeid.ToString(),
                                    objNFSaida.IDFilial.ToString(),
                                    objNFSaida.DTEmissao.ToString("yyyy-MM-dd"))

            dtResult = modSQL.Fill(pstrSQL, pConnection)
            dtobj = dtResult.Select().FirstOrDefault()
            objNFSaida.NFNumero = dtobj(2).ToString()
            objNFSaida.Serie = dtobj(3).ToString()
            objNFSaida.IDFilial = dtobj(1).ToString()
            objNFSaida.DTEmissao = Convert.ToDateTime(dtobj(5).ToString())

            pstrSQL = " insert into nfp01_lista 
                                            (id_filial, uf_dest, cfg_numero_nf, cfg_serie_nf, dt_lista,
                                             dt_emissao, tipo, flag, flag_erro, stat,
                                             numero_nfe, serie_nfe,  cnpj_emitente,  cnpj_destinatario, tp_ambiente,
                                             mod, versao_sefaz, justif)
                                            values 
                                            ( {0}, {1}, '{2}', '{3}', '{4}', 
                                            '{4}', 2, 0,  0,  null, '{2}',
                                            '{3}', '{5}', 0, {6},  55, '{7}',
                                            'Cancelamento/inutilizacao')"



            dtFilial = getFilial(pConnection)

            dtrow = dtFilial.Select("ID_FILIAL = " + objNFSaida.IDFilial.ToString()).FirstOrDefault()
            pstrSQL = String.Format(pstrSQL, objNFSaida.IDFilial.ToString(),
                                            dtrow(2).ToString(),
                                            objNFSaida.NFNumero.ToString(),
                                            objNFSaida.Serie,
                                            objNFSaida.DTEmissao.ToString("yyyy-MM-dd"),
                                             dtrow(1).ToString(),
                                            objNFSaida.Ambiente,
                                            objNFSaida.Versao)


            modSQL.ExecuteScalar(pstrSQL, pConnection)
            RegistrarLog(pstrUser, "Cancelamento Neles - Triangulus", "Foi Efetuado o cancelamento da chave: " + objNFSaida.Nfeid)
            Return "0"
        Catch
            Return "1"
        End Try

    End Function

    Public Function CancelarNF(ByVal pConnection As String, ByVal objNFSaida As NFSaidaTriangulus, ByVal pstrUser As String) As Integer
        Dim dtFilial As New DataTable
        Dim dtrow As DataRow

        Try
            Dim pstrSQL As String = " insert into nfp01_lista 
                                            (id_filial, uf_dest, cfg_numero_nf, cfg_serie_nf, dt_lista,
                                             dt_emissao, tipo, flag, flag_erro, stat,
                                             numero_nfe, serie_nfe,  cnpj_emitente,  cnpj_destinatario, tp_ambiente,
                                             mod, versao_sefaz, justif)
                                            values 
                                            ( {0}, {1}, '{2}', '{3}', '{4}', 
                                            '{4}', 2, 0,  0,  null, '{2}',
                                            '{3}', '{5}', 0, {6},  55, '{7}',
                                            'Cancelamento/inutilizacao')"



            dtFilial = getFilial(pConnection)

            dtrow = dtFilial.Select("ID_FILIAL = " + objNFSaida.IDFilial.ToString()).FirstOrDefault()
            pstrSQL = String.Format(pstrSQL, objNFSaida.IDFilial.ToString(),
                                        dtrow(2).ToString(),
                                        objNFSaida.NFNumero.ToString(),
                                        objNFSaida.Serie,
                                        objNFSaida.DTEmissao.ToString("yyyy-MM-dd"),
                                         dtrow(1).ToString(),
                                        objNFSaida.Ambiente,
                                        objNFSaida.Versao)


            modSQL.ExecuteScalar(pstrSQL, pConnection)
            RegistrarLog(pstrUser, "Cancelamento/Inutilização Neles - Triangulus", "Foi Efetuado o cancelamento/Inutilização da chave: " + objNFSaida.Nfeid + ", numero: " + objNFSaida.NFNumero.ToString() + ", serie: " + objNFSaida.Serie)
            Return "0"
        Catch
            Return "1"
        End Try

    End Function

    Public Function AlterarStatusNF(ByVal pConnection As String, ByVal objNFSaida As NFSaidaTriangulus, ByVal pstrUser As String) As Integer
        Dim dtFilial As New DataTable
        Dim dtrow As DataRow
        Try
            Dim pstrSQL As String = "  UPDATE NFF01_LISTA SET FLAG = 4
                                        WHERE CHAVE_ACESSO = '{0}';

                                        UPDATE NFF02_DOC SET FLAG = '1111100100',  STAT = {1}                         
                                        WHERE CHAVE_ACESSO = '{0}'"

            pstrSQL = String.Format(pstrSQL,
                                    objNFSaida.Nfeid,
                                    objNFSaida.Status)

            Dim result As String = modSQL.ExecuteScalar(pstrSQL, pConnection)
            RegistrarLog(pstrUser, "Alterar Status NF - Triangulus", "Foi alterado o status da chave: " + objNFSaida.Nfeid + " para o status:" + objNFSaida.Status + "  de acesso no triangulus")
            Return result
        Catch ex As Exception
            Return "1"
        End Try
    End Function


    Public Function InserirNotaReferenciada(ByVal pConnection As String, ByVal objNF As NFSaidaTriangulus, ByVal pstrUser As String) As Integer
        Try
            Dim pstrSQL As String = " UPDATE VW51_NFE_REF SET NFREF_REFNFE = '{0}'
            WHERE (((VW51_NFE_REF.CFG_UN)={1}) AND 
                                              ((VW51_NFE_REF.CFG_NUMERO_NF)={2}) AND 
                                              ((VW51_NFE_REF.CFG_SERIE_NF)='{3}') and
                                              ((VW51_NFE_REF.REF_NITEM)=1))"

            pstrSQL = String.Format(pstrSQL,
                                    objNF.Nfeid,
                                    objNF.IDFilial,
                                    objNF.NFNumero.ToString(),
                                    objNF.Serie)

            Dim result As String = modSQL.ExecuteScalar(pstrSQL, pConnection)
            RegistrarLog(pstrUser, "Inserir Referência NF - Triangulus", "Foi inserido a chave: " + objNF.Nfeid + " como referencia para a nota: " + objNF.NFNumero.ToString() + ", serie: " + objNF.Serie)
            Return result
        Catch ex As Exception
            Return "1"
        End Try
    End Function


    Public Sub RegistrarLog(ByVal pstrUser As String, ByVal pstrTitle As String, ByVal pstrlogDescription As String)
        Try
            Dim sqlCommand = "insert into tblogapplication values ('{0}', '{1}', {2}, '{3}', '{4}', null, null, null)"

            sqlCommand = String.Format(sqlCommand,
                                        "Application",
                                        pstrUser,
                                        "getDate()",
                                        pstrTitle,
                                        pstrlogDescription)

            modSQL.ExecuteScalar(sqlCommand)

        Catch ex As Exception

        End Try
    End Sub

    Public Function InserirICMSBaseReduzida(ByVal pConnection As String, ByVal objNF As NFSaidaTriangulus, ByVal pstrUser As String) As Integer
        Try
            Dim pstrSQL As String = "  UPDATE VW65_DET_ICM SET VW65_DET_ICM.ICMS70_PREDBCST = 100,
                                                               VW65_DET_ICM.ICMS70_VBCST = 100, 
                                                               VW65_DET_ICM.ICMS70_VICMSST = 100
                                       WHERE VW65_DET_ICM.CFG_UN  = {0}          AND 
                                             VW65_DET_ICM.CFG_NUMERO_NF = {1} AND
                                             VW65_DET_ICM.CFG_SERIE_NF = '{2}'   AND 
                                             VW65_DET_ICM.DET_NITEM = {3}          AND
                                             VW65_DET_ICM.ICMS70_CST ='70' "

            pstrSQL = String.Format(pstrSQL,
                                    objNF.IDFilial,
                                    objNF.NFNumero.ToString(),
                                    objNF.Serie,
                                    objNF.Item.ToString())

            Dim result As String = modSQL.ExecuteScalar(pstrSQL, pConnection)

            RegistrarLog(pstrUser, "ICMS ST Base Reduzida - Triangulus", "Foi atualizada a ''Detalhe ICMS - VW65'' da nota: " + objNF.NFNumero.ToString() + " serie " + objNF.Serie + " item " + objNF.Item.ToString())


            pstrSQL = "UPDATE VW50_NFE SET VW50_NFE.ICMSTOT_VBCST = 100, 
                                           VW50_NFE.ICMSTOT_VST = 100, 
                                           VW50_NFE.ICMSTOT_VNF = 100, 
                                           VW50_NFE.FAT_VORIG = 100, 
                                           VW50_NFE.FAT_VLIQ = 100
                       WHERE VW50_NFE.CFG_UN = {0} AND 
                             VW50_NFE.CFG_NUMERO_NF = {1} AND
                             VW50_NFE.CFG_SERIE_NF ='{2}' "

            pstrSQL = String.Format(pstrSQL,
                                    objNF.IDFilial,
                                    objNF.NFNumero.ToString(),
                                    objNF.Serie)

            result = modSQL.ExecuteScalar(pstrSQL, pConnection)

            RegistrarLog(pstrUser, "ICMS ST Base Reduzida - Triangulus", "Foi atualizada a ''Total NF - VW50'' da nota: " + objNF.NFNumero.ToString() + " serie " + objNF.Serie + " item " + objNF.Item.ToString())


            pstrSQL = "UPDATE VW60_DET SET VW60_DET.NFCI = Null
                       WHERE VW60_DET.CFG_UN = {0}           AND
                             VW60_DET.CFG_NUMERO_NF = {1} AND
                             VW60_DET.CFG_SERIE_NF  = '{2}'  AND
                             VW60_DET.DET_NITEM = {3} "

            pstrSQL = String.Format(pstrSQL,
                                    objNF.IDFilial,
                                    objNF.NFNumero.ToString(),
                                    objNF.Serie,
                                    objNF.Item.ToString())

            result = modSQL.ExecuteScalar(pstrSQL, pConnection)
            RegistrarLog(pstrUser, "ICMS ST Base Reduzida - Triangulus", "Foi atualizada a ''Limpa FCI - VW60'' da nota: " + objNF.NFNumero.ToString() + " serie " + objNF.Serie + " item " + objNF.Item.ToString())

            Return result
        Catch ex As Exception
            Return "1"
        End Try
    End Function

End Class
