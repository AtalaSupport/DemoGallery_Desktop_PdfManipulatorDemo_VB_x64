Imports System.IO

Public Structure FileFramePair
    Private _fileName As String
    Private _frame As Integer

    Public Sub New(ByVal fileName As String, ByVal frame As Integer)
        _fileName = fileName
        _frame = frame
    End Sub

    Public Property FileName() As String
        Get
            Return _fileName
        End Get
        Set(ByVal value As String)
            _fileName = value
        End Set
    End Property
    Public Property Frame() As Integer
        Get
            Return _frame
        End Get
        Set(ByVal value As Integer)
            _frame = value
        End Set
    End Property
    Public Overrides Function ToString() As String
        Return Path.GetFileName(_fileName)
    End Function
End Structure