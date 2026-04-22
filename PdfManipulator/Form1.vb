Imports Atalasoft.Imaging.Codec.Pdf
Imports Atalasoft.Imaging.Codec
Imports System.IO
Imports Atalasoft.PdfDoc
Imports Atalasoft.Imaging.WinControls

Public Class Form1

#Region "FIELDS"
    Private _currentFile As String = Nothing
    Private _instructionForm As Form
#End Region

    Shared Sub New()
        ' allows us to get ImageInfo for user-selected PDFs
        ' Default resolution is 96 DPI, but we'll explicitly call it to demonstrate how to define
        ' We'll also set (annotation) RenderSettings to none so we get nice clean thumbnails
        Dim pdfDec As New PdfDecoder()
        pdfDec.Resolution = 96
        pdfDec.RenderSettings = New RenderSettings() With { _
         .AnnotationSettings = AnnotationRenderSettings.None _
        }
        RegisteredDecoders.Decoders.Add(pdfDec)
    End Sub

    Public Sub New()
        Dim splashForm As New Splash()
        splashForm.Show()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub


#Region "Instructions"
    Private Sub Form1_Load(ByVal sender As Object, ByVal e As System.EventArgs)
        System.Threading.Thread.Sleep(500)

        Dim show As Boolean = True
        Dim settings As String = Application.UserAppDataPath + "\PdfManipulatorSettings.txt"
        If System.IO.File.Exists(settings) Then
            Using reader As New System.IO.FileStream(settings, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)
                show = (reader.ReadByte() = 1)
            End Using
        End If

        If show Then
            ShowInstructions()
        End If
    End Sub

    Private Sub ShowInstructions()
        If _instructionForm Is Nothing Then
            _instructionForm = New Instructions()
            AddHandler _instructionForm.Disposed, AddressOf _instructionForm_Disposed
        End If
        _instructionForm.Show()
    End Sub

    Private Sub _instructionForm_Disposed(ByVal sender As Object, ByVal e As EventArgs)
        _instructionForm = Nothing
    End Sub
#End Region


#Region "Misc Interface Items"
    Private Sub LinkLabel1_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        System.Diagnostics.Process.Start("http://www.atalasoft.com")
    End Sub
#End Region


#Region "File Menu"
    Private Sub AddFileToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AddFileToolStripMenuItem.Click
        Dim fileOpener As New OpenFileDialog()
        fileOpener.Filter = "PDF Files (*.pdf)|*.pdf"

        If fileOpener.ShowDialog() = DialogResult.OK Then
            TryAddingFile(fileOpener.FileName)
        End If
    End Sub

    Private Sub SaveAsToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SaveAsToolStripMenuItem.Click
        Dim fileSaver As New SaveFileDialog()
        fileSaver.Filter = "PDF File (*.pdf)|*.pdf"

        If fileSaver.ShowDialog() = DialogResult.OK Then
            TrySavingFile(fileSaver.FileName)
        End If

    End Sub

    Private Sub QuitToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles QuitToolStripMenuItem.Click
        Application.Exit()
    End Sub
#End Region


#Region "Help Menu"
    Private Sub InstructionsToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles InstructionsToolStripMenuItem.Click
        ShowInstructions()
    End Sub

    Private Sub AboutToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AboutToolStripMenuItem.Click
        Dim aboutBox As AtalaDemos.AboutBox.About = New AtalaDemos.AboutBox.About("About Atalasoft DotImage PDF Manipulator Demo", "DotImage PDF Manipulator Demo")
        aboutBox.Description = "Demonstrates how to manipulate pages in a PDF, and resave without having to recompose the entire PDF. Includes the ability to reoder pages, remove pages, merge multiple PDF files, and save the result. " & vbCrLf & vbCrLf & "This Windows Forms application demonstrates the use of features in our PdfDoc class that do NOT require the purchase of DotPdf."
        aboutBox.ShowDialog()
    End Sub
#End Region


#Region "NonMenu Event Handlers"
    Private Sub ThumbnailView1_KeyDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles ThumbnailView1.KeyDown
        If e.KeyCode = Keys.Delete OrElse e.KeyCode = Keys.Back Then
            DeleteItems(ThumbnailView1.SelectedItems)
        End If
    End Sub
#End Region


#Region "Main working methods"

    Private Sub TryAddingFile(ByVal fileName As String)
        If CanOpenThisFileAsPdf(fileName) Then
            If AlreadyAddedFile(fileName) Then
                MessageBox.Show("This file is already in the collection.")
            Else
                _currentFile = fileName
                OnFileAdded(fileName)
            End If
        Else
            MessageBox.Show("Unable to open the file - it might not be a PDF or it might be damaged.")
        End If
    End Sub

    Private Function AlreadyAddedFile(ByVal fileName As String) As Boolean
        For Each o As Object In fileList.Items
            Dim pair As FileFramePair = DirectCast(o, FileFramePair)
            If pair.FileName = fileName Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Function CanOpenThisFileAsPdf(ByVal fileName As String) As Boolean
        Using pdfFileStream As New FileStream(fileName, FileMode.Open)
            Dim pdfdecoder As New PdfDecoder()
            Try
                pdfdecoder.GetImageInfo(pdfFileStream)
            Catch
                Return False
            End Try
            Return True
        End Using
    End Function

    Private Sub OnFileAdded(ByVal fileName As String)
        Dim info As ImageInfo = RegisteredDecoders.GetImageInfo(fileName)
        Dim offset As Integer = ThumbnailView1.Items.Count
        For i As Integer = 0 To info.FrameCount - 1
            AddEachPageAsThumbnail(fileName, i, offset)
        Next
        fileList.Items.Add(New FileFramePair(fileName, 0))
    End Sub

    Private Sub AddEachPageAsThumbnail(ByVal fileName As String, ByVal page As Integer, ByVal offset As Integer)
        Dim pair As New FileFramePair(fileName, page)
        ThumbnailView1.Items.Add(fileName, page, pair.ToString() + vbLf & "Page" & page)
        ThumbnailView1.Items(offset + page).Tag = pair
    End Sub


    Private Sub TrySavingFile(ByVal fileName As String)
        If AlreadyAddedFile(fileName) Then
            MessageBox.Show("Can't save over one of the source files.")
        Else
            SaveTo(fileName)
        End If
    End Sub

    Private Sub SaveTo(ByVal outFileName As String)
        If Not HasSomeThumbnailsLoaded() Then
            Return
        End If

        Dim originals As PdfDocument() = MakePdfDocuments()
        Dim final As New PdfDocument()

        Try
            ProcessThumbnails(ThumbnailView1.Items, originals, final, outFileName)
        Finally
            CloseAllLoadedPdfDocuments(originals)
        End Try
    End Sub

    Private Sub ProcessThumbnails(ByVal thumbnailCollection As ThumbnailCollection, ByVal originals As PdfDocument(), ByVal final As PdfDocument, ByVal outFileName As String)
        For Each thumb As Thumbnail In ThumbnailView1.Items
            Dim fileFrame As FileFramePair = DirectCast(thumb.Tag, FileFramePair)
            Dim original As PdfDocument = GetPdfDocument(fileFrame.FileName, originals)
            If original Is Nothing Then
                MessageBox.Show("Unable to find the original source file " + fileFrame.ToString() + " - this should never happen - skipping file.")
                Continue For
            End If
            final.Pages.Add(original.Pages(fileFrame.Frame))
        Next
        Using outStream As New FileStream(outFileName, FileMode.Create)
            final.Save(outStream)
        End Using
    End Sub

    Private Sub CloseAllLoadedPdfDocuments(ByVal originals As PdfDocument())
        For Each doc As PdfDocument In originals
            doc.Pages(0).Stream.Close()
        Next
    End Sub

    Private Function HasSomeThumbnailsLoaded() As Boolean
        If ThumbnailView1.Items.Count = 0 Then
            MessageBox.Show("Cannot create a PDF file from zero thumbnails!")
            Return False
        End If
        Return True
    End Function

    Private Function GetPdfDocument(ByVal fileName As String, ByVal originals As PdfDocument()) As PdfDocument
        For i As Integer = 0 To fileList.Items.Count - 1
            If DirectCast(fileList.Items(i), FileFramePair).FileName = fileName Then
                Return originals(i)
            End If
        Next
        Return Nothing
    End Function

    Private Function MakePdfDocuments() As PdfDocument()
        Dim pdfArray As PdfDocument() = New PdfDocument(fileList.Items.Count - 1) {}
        For i As Integer = 0 To pdfArray.Length - 1
            Dim pair As FileFramePair = DirectCast(fileList.Items(i), FileFramePair)
            pdfArray(i) = New PdfDocument(pair.FileName)
        Next
        Return pdfArray
    End Function

    Private Sub DeleteItems(ByVal collection As SelectedThumbnailCollection)
        If collection Is Nothing OrElse collection.Count = 0 Then
            Return
        End If

        Dim thumbnails As Thumbnail() = New Thumbnail(collection.Count - 1) {}
        collection.CopyTo(thumbnails)
        ThumbnailView1.Items.Remove(thumbnails)
    End Sub
#End Region

End Class
