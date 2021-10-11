Imports System.IO
Imports MetsoFramework.Utils

Public Class BLNotificacoes


    Public Function GetDivergencias() As DataTable
        Return New DataTable()
    End Function

    Public Function GetHistoricoUsuarios() As DataTable
        Return New DataTable()
    End Function

    Public Function GetHistoricoSistema() As DataTable
        Return New DataTable()
    End Function

    Public Function GetNotificacoes() As Dictionary(Of String, Object)
        Dim objBLDivergencias As New BLDivergencias()
        Dim objBLNotaFiscal As New BLNotaFiscal()
        Dim objBLPortaria As New BLPortaria()

        Dim objRetorno As New Dictionary(Of String, Object)
        objRetorno.Add("MateriaisBloqueado", objBLPortaria.GetQtdItensBloqueadosEmProcessamento())
        objRetorno.Add("PedidosPrioritarios", objBLPortaria.GetQtdItensPrioritariosEmProcessamento())
        objRetorno.Add("DivergenciasAtivas", objBLDivergencias.GetQtdDivergenciasAtivasUsuario(Uteis.LogonName()))
        objRetorno.Add("DocumentosPendentes", objBLNotaFiscal.GetQtdNotasPendentes())
        objRetorno.Add("ProcessamentoFiscal", 0)

        Return objRetorno
    End Function

End Class
