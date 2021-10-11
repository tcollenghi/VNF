Public Class BLCompradores

    Public Function GetAll() As DataTable
        Dim CmdText As String = "select * from TbCOM order by CODCOM"
        Return modSQL.Fill(CmdText)
    End Function

End Class
