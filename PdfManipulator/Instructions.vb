Public Class Instructions

#Region "FIELDS"
    Private _loading As Boolean
#End Region


#Region "Other Event Handlers"
    Private Sub Instructions_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Dim asm As System.Reflection.Assembly = System.Reflection.Assembly.GetExecutingAssembly()
        Dim stream As System.IO.Stream = asm.GetManifestResourceStream("PdfManipulator.Instructions.rtf")
        If stream IsNot Nothing Then
            Me.rtfInstructions.LoadFile(stream, RichTextBoxStreamType.RichText)
            stream.Close()
        End If

        _loading = True
        Dim settings As String = Application.UserAppDataPath + "\PdfManipulatorSettings.txt"
        If System.IO.File.Exists(settings) Then
            Using reader As New System.IO.FileStream(settings, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)
                Me.chkShowAtStartup.Checked = (reader.ReadByte() = 1)
            End Using
        End If
        _loading = False
    End Sub
#End Region


#Region "User Action Handlers"
    Private Sub btnClose_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub

    Private Sub chkShowAtStartup_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkShowAtStartup.CheckedChanged
        If _loading Then
            Return
        End If
        Dim settings As String = Application.UserAppDataPath + "\PdfManipulatorSettings.txt"
        Using writer As New System.IO.FileStream(settings, System.IO.FileMode.Create, System.IO.FileAccess.Write)
            writer.WriteByte(CByte(If(Me.chkShowAtStartup.Checked, 1, 0)))
        End Using
    End Sub
#End Region


End Class