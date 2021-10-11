Imports System.Data.SqlClient
Imports System.Xml
Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Net
Imports System.Globalization
Imports System.Configuration
Imports MetsoFramework.SAP

Public Class modVerificarCTE

    '[2016-10-26 Demian] Comentado pois o método não é utilizado
    'Public Shared Function VerificarStatusCTe(ByVal objNF As modNF, ByVal strModoContingencia As String) As String
    '    Dim strStatus As String = String.Empty

    '    '---> BUSCA O STATUS DO CT-E NO SISTEMA DE FRETES
    '    modSQLFretes.CommandText = "select top 1 cte_status from vwCTE_INFO where cte_chave_acesso = '" & objNF.VNF_CHAVE_ACESSO & "'"
    '    Dim objStatusCte As Object = modSQLFretes.ExecuteScalar(modSQLFretes.CommandText)

    '    If objStatusCte Is Nothing Then
    '        strStatus = "PENDENTE"
    '        modSQL.CommandText = "select count(*) from TbNFE where nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'"
    '        If modSQL.ExecuteScalar(modSQL.CommandText) = 0 Then
    '            modSQL.CommandText = "insert into TbNFE values ('" & objNF.VNF_CHAVE_ACESSO & "', 'S', 'N', 0, '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', 'S', '', 'N', 'PENDENTE', 'N', '" & objNF.NF_DEST_CNPJ.Trim & "', '" & strModoContingencia & "')"
    '        Else
    '            modSQL.CommandText = "update TbNFE set SITUACAO = 'PENDENTE', NFEVAL = 'S', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', NFEREL = 'S', REPROCESSAR = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
    '        End If

    '    ElseIf objStatusCte.ToString().ToUpper() = "ACEITA" Then
    '        strStatus = "ACEITA"
    '        modSQL.CommandText = "select count(*) from TbNFE where nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'"
    '        If modSQL.ExecuteScalar(modSQL.CommandText) = 0 Then
    '            modSQL.CommandText = "insert into TbNFE values ('" & objNF.VNF_CHAVE_ACESSO & "', 'S', 'N', 0, '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', 'S', '', 'N', 'ACEITA', 'N', '" & objNF.NF_DEST_CNPJ.Trim & "', '" & strModoContingencia & "')"
    '        Else
    '            modSQL.CommandText = "update TbNFE set SITUACAO = 'ACEITA', NFEVAL = 'S', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', NFEREL = 'S', REPROCESSAR = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
    '        End If

    '    ElseIf objStatusCte.ToString().ToUpper() = "REJEITADA" Then
    '        strStatus = "REJEITADA"
    '        modSQL.CommandText = "select count(*) from TbNFE where nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'"
    '        If modSQL.ExecuteScalar(modSQL.CommandText) = 0 Then
    '            modSQL.CommandText = "insert into TbNFE values ('" & objNF.VNF_CHAVE_ACESSO & "', 'S', 'N', 0, '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', 'S', '', 'N', 'REJEITADA', 'N', '" & objNF.NF_DEST_CNPJ.Trim & "', '" & strModoContingencia & "')"
    '        Else
    '            modSQL.CommandText = "update TbNFE set SITUACAO = 'REJEITADA', NFEVAL = 'S', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', NFEREL = 'S', REPROCESSAR = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
    '        End If

    '    ElseIf objStatusCte.ToString().ToUpper() = "CANCELADA MANUALMENTE" Then
    '        strStatus = "CANCELADA"
    '        modSQL.CommandText = "select count(*) from TbNFE where nfeid = '" & objNF.VNF_CHAVE_ACESSO & "'"
    '        If modSQL.ExecuteScalar(modSQL.CommandText) = 0 Then
    '            modSQL.CommandText = "insert into TbNFE values ('" & objNF.VNF_CHAVE_ACESSO & "', 'S', 'N', 0, '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', 'S', '', 'N', 'REJEITADA', 'N', '" & objNF.NF_DEST_CNPJ.Trim & "', '" & strModoContingencia & "')"
    '        Else
    '            modSQL.CommandText = "update TbNFE set SITUACAO = 'CANCELADA', NFEVAL = 'S', DATVAL = '" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "', NFEREL = 'S', REPROCESSAR = 'N' where NFEID = '" & objNF.VNF_CHAVE_ACESSO & "'"
    '        End If
    '    End If

    '    modSQL.ExecuteNonQuery(modSQL.CommandText)
    '    Return strStatus
    'End Function

End Class
