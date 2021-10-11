'Autor: Marcio Spinosa - 24/05/2018 - CR00008351 
'Data: 24/05/2018
'OBS: Ajuste feito para o VNF não utilize o AD para trazer dados do usuário
' tendo os mesmos em base
Imports System.IO
Imports MetsoFramework.Utils

Public Class BLLog

    Public Enum LogType
        Application
        Service
    End Enum

    Public Enum LogTitle
        VerificarDocumento
        AnularDivergencia
        CorrigirDivergencia
        Cancelamento
        CancelamentoDesfeito
        Recusa
        RecusaDesfeita
        Email
        PortariaAceita
        PortariaRejeitada
        PortariaPendente
        PortariaEntrada
        PortariaSaida
        CaminhaoChegada
        CaminhaoEntrada
        CaminhaoSaida
        AssociacaoPortal
        ModoContingenciaOn
        ModoContingenciaOff
        Migo
        MigoRefInboundDelivery
        Miro
        MaterialRecebido
        RelevanciaAlterada
        EnvioXml
        SelecionarInbound
        RegistroManual
        InserirTalonario
        CriarOcorrencia
        EncaminharOcorrencia
        ResponderOcorrencia
        FinalizarOcorrencia
        EstornoVNF
        ErroEstornoVNF
        GeradaNotaManualJ1B1N
    End Enum


    Public Shared Sub Insert(ByVal Type As LogType, ByVal Usuario As String, ByVal Title As LogTitle, ByVal Description As String, Optional ByVal NfeId As String = "", Optional ByVal Link As String = "")
        Dim strQuery As String = "INSERT INTO TbLOGApplication( " & _
                                 "	 log_type " & _
                                 "	,log_user " & _
                                 "	,log_date " & _
                                 "	,log_title " & _
                                 "	,log_description " & _
                                 "	,log_nfeid " & _
                                 "	,log_icon " & _
                                 "	,log_link " & _
                                 ") VALUES ( " & _
                                 "	 '" & Type.ToString() & "' " & _
                                 "	,'" & Usuario & "' " & _
                                 "	,'" & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & "'" & _
                                 "	,'" & GetLogTitle(Title) & "' " & _
                                 "	,'" & Description & "' " & _
                                 "	,'" & NfeId & "' " & _
                                 "	,'" & GetLogIcon(Title) & "' " & _
                                 "	,'" & Link & "' ) "

        modSQL.ExecuteNonQuery(strQuery)
    End Sub

    Private Shared Function GetLogTitle(ByVal Title As LogTitle) As String
        Select Case Title
            Case LogTitle.CaminhaoChegada
                Return "<a href=""""><strong class=""cursor-default"">Chegada de caminhão</strong></a>"
            Case LogTitle.CaminhaoEntrada
                Return "<a href=""""><strong class=""cursor-default"">Entrada de caminhão</strong></a>"
            Case LogTitle.CaminhaoSaida
                Return "<a href=""""><strong class=""cursor-default txt-color-greenDark"">Saída de caminhão</strong></a>"
            Case LogTitle.AssociacaoPortal
                Return "<a href=""""><strong class=""cursor-default"">Associação pelo portal</strong></a>"
            Case LogTitle.ModoContingenciaOn
                Return "<a href=""""><strong class=""cursor-default txt-color-red"">Modo contingência</strong></a>"
            Case LogTitle.ModoContingenciaOff
                Return "<a href=""""><strong class=""cursor-default txt-color-greenDark"">Modo contingência</strong></a>"
            Case Else
                Return Title.ToString()
        End Select
    End Function

    Private Shared Function GetLogIcon(ByVal Title As LogTitle) As String
        Select Case Title
            Case LogTitle.AnularDivergencia
                Return "<i class=""fa fa-smile-o txt-color-orange""></i>"
            Case LogTitle.Cancelamento
                Return "<i class=""fa fa-thumbs-o-down txt-color-red""></i>"
            Case LogTitle.CancelamentoDesfeito
                Return "<i class=""fa fa-check-circle txt-color-greenDark""></i>"
            Case LogTitle.CorrigirDivergencia
                Return "<i class=""fa fa-thumbs-o-up txt-color-greenDark""></i>"
            Case LogTitle.Email
                Return "<i class=""fa fa-send txt-color-blue""></i>"
            Case LogTitle.PortariaAceita
                Return "<i class=""fa fa-check-circle txt-color-greenDark""></i>"
            Case LogTitle.PortariaPendente
                Return "<i class=""fa fa-retweet txt-color-red""></i>"
            Case LogTitle.PortariaRejeitada
                Return "<i class=""fa fa-exclamation-circle txt-color-red""></i>"
            Case LogTitle.Recusa
                Return "<i class=""fa fa-ban txt-color-red""></i>"
            Case LogTitle.RecusaDesfeita
                Return "<i class=""fa fa-check-circle txt-color-greenDark""></i>"
            Case LogTitle.CaminhaoChegada
                Return "<div class=""smart-timeline-icon""><i class=""fa fa-truck""></i></div>"
            Case LogTitle.CaminhaoEntrada
                Return "<div class=""smart-timeline-icon""><i class=""fa fa-truck""></i></div>"
            Case LogTitle.CaminhaoSaida
                Return "<div class=""smart-timeline-icon bg-color-greenDark""><i class=""fa fa-truck""></i></div>"
            Case LogTitle.AssociacaoPortal
                Return "<div class=""smart-timeline-icon""><i class=""fa fa-pencil""></i></div>"
            Case LogTitle.ModoContingenciaOn
                Return "<div class=""smart-timeline-icon bg-color-red""><i class=""fa fa-bullhorn""></i></div>"
            Case LogTitle.ModoContingenciaOff
                Return "<div class=""smart-timeline-icon bg-color-greenDark""><i class=""fa fa-bullhorn""></i></div>"
            Case LogTitle.Migo
                Return "<i class=""fa fa-thumbs-o-up txt-color-greenDark""></i>"
            Case LogTitle.Miro
                Return "<i class=""fa fa-thumbs-o-up txt-color-greenDark""></i>"
            Case LogTitle.EnvioXml
                Return "<i class=""fa fa-upload txt-color-blue""></i>"
            Case LogTitle.SelecionarInbound
                Return "<i class=""fa fa-calendar txt-color-blue""></i>"
            Case LogTitle.RegistroManual
                Return "<i class=""fa fa-pencil txt-color-blue""></i>"
            Case LogTitle.CriarOcorrencia
                Return "<i class=""fa fa-exclamation-triangle txt-color-red""></i>"
            Case LogTitle.EncaminharOcorrencia
                Return "<i class=""fa fa-mail-forward txt-color-blue""></i>"
            Case Else
                Return ""
        End Select
    End Function

    'Marcio Spinosa - 24/05/2018 - CR00008351 
    ''' <summary>
    ''' Retorna as 20 últimas comparações 
    ''' </summary>
    ''' <param name="Type"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    ''' <example>Foi ajustado no sql o retorno dos dados do funcionário</example>
    Public Function getLog(ByVal Type As LogType) As DataTable
        Dim strQuery As String = "SELECT TOP 20 " & _
                                 "	 log_type " & _
                                 "	,log_user " & _
                                 "	,usunomusu " & _
                                 "	,log_date " & _
                                 "	,log_title " & _
                                 "	,log_description " & _
                                 "	,log_link " & _
                                 "	,log_nfeid " & _
                                 "	,log_icon " & _
                                 "FROM  " & _
                                 "	TbLOGApplication WITH(NOLOCK) " & _
                                 " inner join tbusuario on log_user = usucodusu	" & _
                                 "WHERE " & _
                                 "	log_type = '" & Type.ToString() & "' " & _
                                 "ORDER BY " & _
                                 "	log_date desc "
        'Marcio Spinosa - 24/05/2018 - CR00008351 - Fim
        Return modSQL.Fill(strQuery)
    End Function

End Class
