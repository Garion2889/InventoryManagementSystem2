Imports System.IO

Public Class AuditTrail
    Private Shared logFilePath As String
    Private Shared logFileName As String
    Private Shared logDirectory As String
    Private Shared lockObject As New Object()

    Public Shared Sub InitializeLogFile(ByVal logFileName As String, ByVal logDirectory As String)
        If String.IsNullOrEmpty(logDirectory) Or String.IsNullOrEmpty(logFileName) Then
            Throw New ArgumentException("Log directory or file name cannot be empty")
        End If

        If Not Directory.Exists(logDirectory) Then
            Directory.CreateDirectory(logDirectory)
        End If

        Dim timestamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")
        AuditTrail.logFileName = Path.GetFileNameWithoutExtension(logFileName) & "_" & timestamp & Path.GetExtension(logFileName)
        AuditTrail.logDirectory = logDirectory
        logFilePath = Path.Combine(logDirectory, AuditTrail.logFileName)

        Try
            Using sw As StreamWriter = File.CreateText(logFilePath)
                sw.WriteLine("Log History - Created on " & DateTime.Now.ToString())
            End Using
        Catch ex As Exception
            MessageBox.Show("Error creating log file: " & ex.Message)
        End Try
    End Sub

    Public Shared Sub WriteLog(ByVal logMessage As String)
        SyncLock lockObject
            Try
                If String.IsNullOrEmpty(logFilePath) Then
                    Throw New InvalidOperationException("Log file is not initialized.")
                End If

                Using sw As StreamWriter = File.AppendText(logFilePath)
                    sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & " - " & logMessage)
                End Using
            Catch ex As Exception
                MessageBox.Show("Error writing to log file: " & ex.Message)
            End Try
        End SyncLock
    End Sub
End Class
