Imports MetsoFramework.Utils

Public MustInherit Class modSAP

    'RMM 600
    'Public RfcAppServerIP As String = "139.74.219.8"

    'PMM 600
    Public Shared RfcAppServerIP As String = Uteis.GetSettingsValue(Of String)("MessageServerHost").ToString()
    Public Shared RfcSysNumber As String = Uteis.GetSettingsValue(Of String)("SystemNumber").ToString()
    Public Shared RfcClientNumber As String = Uteis.GetSettingsValue(Of String)("Client").ToString()
    Public Shared RfcUser As String = Uteis.GetSettingsValue(Of String)("User").ToString()
    Public Shared RfcPassword As String = Uteis.GetSettingsValue(Of String)("Password").ToString()

    Public Shared Function RfcConStr() As String

        If (String.IsNullOrEmpty(RfcAppServerIP)) Then
            RfcAppServerIP = Uteis.GetSettingsValue(Of String)("AppServerHost").ToString()
        End If

        Return " ASHOST=" & RfcAppServerIP & _
               " SYSNR=" & RfcSysNumber & _
               " CLIENT=" & RfcClientNumber & _
               " USER=" & RfcUser & _
               " PASSWD=" & RfcPassword
    End Function

End Class
