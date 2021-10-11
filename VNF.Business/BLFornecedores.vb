Imports MetsoFramework.Core

Public Class BLFornecedores

    Public Function GetByFilter(ByVal Cnpj As String, ByVal RazaoSocial As String, ByVal CodigoSap As String) As DataTable
        Dim strQuery As String = "SELECT codfor, razfor, cnpj, homologado, email_nfe, regime_especial FROM vwFORNECEDOR "
        Dim strFilter As String = ""

        If (Not String.IsNullOrEmpty(Cnpj)) Then
            strFilter &= " and cnpj = '" & Cnpj & "'"
        End If

        If (Not String.IsNullOrEmpty(RazaoSocial)) Then
            strFilter &= " and razfor like '%" & RazaoSocial & "%'"
        End If

        If (Not String.IsNullOrEmpty(CodigoSap)) Then
            strFilter &= " and codfor = '" & CodigoSap & "'"
        End If

        If (Not String.IsNullOrEmpty(strFilter)) Then
            strFilter = "WHERE " & strFilter.Substring(4)
        End If

        Return modSQL.Fill(strQuery & strFilter)
    End Function

    Public Sub UpdateEmail(ByVal Id As String, ByVal Email As String)
        Try
            Dim strQuery = String.Format("UPDATE TBFOR SET EMAIL_NFE = '{0}' WHERE CODFOR = '{1}'", Email, Id)
            modSQL.ExecuteNonQuery(strQuery)
        Catch ex As Exception

        End Try
    End Sub

    Public Function GetNotasFiscais(ByVal Cnpj As String) As DataTable
        Dim strQuery As String = "SELECT DISTINCT " &
                                 "	  TBNFE.NFEID,  " &
                                 "	  NF_IDE_NNF,  " &
                                 "	  NF_IDE_SERIE,  " &
                                 "	  NF_IDE_DHEMI,  " &
                                 "	  DATVAL, " &
                                 "	  SITUACAO = CASE WHEN ISNULL(SITUACAO, '') = '' THEN 'PENDENTE' ELSE SITUACAO END, " &
                                 "	  NF_EMIT_CNPJ,  " &
                                 "	  NF_EMIT_XNOME,  " &
                                 "	  NFEREL,  " &
                                 "	  NFEVAL,  " &
                                 "	  CODCOM,  " &
                                 "	  NF_DEST_CNPJ " &
                                 "FROM  " &
                                 "	  TBNFE " &
                                 "	  LEFT JOIN TbDOC_CAB ON (TBNFE.NFEID = TbDOC_CAB.NFEID) " &
                                 "	  LEFT JOIN TbDOC_CAB_NFE ON (TBNFE.NFEID = TbDOC_CAB_NFE.NFEID) " &
                                 "	  LEFT JOIN TBJUN ON (TBNFE.NFEID = TBJUN.NFEID)  " &
                                 "WHERE " &
                                 "	  NF_EMIT_CNPJ = '" & Cnpj & "'  " &
                                 "ORDER BY " &
                                 "	  NF_IDE_DHEMI desc "

        Return modSQL.Fill(strQuery)
    End Function

End Class
