Imports System.IO
Imports MySql.Data.MySqlClient

Public Class Login
    Private loginAttempts As Integer = 0
    Private Const maxLoginAttempts As Integer = 5
    Private time As Integer = 30
    Private numLocked = 0
    Dim logFilePath As String = Path.Combine(Application.StartupPath, "log.txt")
    ' On login or initialization


    Dim connectionString As String = "Server=localhost;Database=isleshopdb;uid=root;Pwd=;"

    Private Sub btnLogin_Click(sender As Object, e As EventArgs) Handles btnLogin.Click
        Dim username As String = txtUsername.Text
        Dim password As String = txtPassword.Text
        Dim admin As String = txtUsername.Text
        Dim adminpass As String = txtPassword.Text

        If ValidateUserLogin(username, password) Then
            AuditTrail.WriteLog(username & " has logged in")
            loginAttempts = 0
            Dim dashboard As New Dashboard("", username)
            dashboard.Show()
            Me.Hide()
        ElseIf AdminLogin(admin, adminpass) Then
            ' MessageBox.Show("Welcome Administrator Successful!")
            AuditTrail.WriteLog(admin & " has logged in as an admin.")
            loginAttempts = 0
            Dim dashboard As New Dashboard("", admin)
            dashboard.Show()
            dashboard.btnAudit.Visible = True
            Me.Hide()
        Else
            loginAttempts += 1
            errorlogin.Visible = True
            AuditTrail.WriteLog(username & " | Login failed. Attempts: " & loginAttempts)

            If loginAttempts >= maxLoginAttempts Then
                LockForm()
                Task.Delay(time * 1000).ContinueWith(Sub(t) UnlockForm())
                AuditTrail.WriteLog("Form locked for " & time & " seconds.")
            End If
        End If
    End Sub

    Private Function ValidateUserLogin(username As String, password As String) As Boolean
        Dim query As String = "SELECT COUNT(*) FROM useraccounts WHERE username = @username AND password = @password;"

        Using connection As New MySqlConnection(connectionString)
            Using cmd As New MySqlCommand(query, connection)
                cmd.Parameters.AddWithValue("@username", username)
                cmd.Parameters.AddWithValue("@password", password)
                Try
                    connection.Open()
                    Dim result As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                    If result > 0 Then
                        Return True
                    Else
                        Return False
                    End If
                Catch ex As MySqlException
                    MessageBox.Show("Error connecting to database: " & ex.Message)
                    Return False
                End Try
            End Using
        End Using
    End Function
    Private Function AdminLogin(admin As String, adminpass As String) As Boolean
        Dim query As String = "Select count(*) from adminacc where admin = @admin and adminpass = @adminpass;"

        Using connection As New MySqlConnection(connectionString)
            Using cmd As New MySqlCommand(query, connection)
                cmd.Parameters.AddWithValue("@admin", admin)
                cmd.Parameters.AddWithValue("@adminpass", adminpass)
                Try
                    connection.Open()
                    Dim result As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                    If result > 0 Then
                        Return True
                    Else
                        Return False
                    End If
                Catch ex As MySqlException
                    MessageBox.Show("Error connecting to database: " & ex.Message)
                    AuditTrail.WriteLog("Error connecting to database: " & ex.Message)
                    Return False
                End Try
            End Using
        End Using
    End Function

    Private Sub LockForm()
        If numLocked = 5 Then
            MsgBox("This form will now close. Please contact the admin for assistance.")
            AuditTrail.WriteLog("Closing the application due to excessive log in attempts.")
            Application.Exit()
        End If
        txtUsername.Enabled = False
        txtPassword.Enabled = False
        btnLogin.Enabled = False
        MsgBox("Too many login attempts. Please try again after " & time & " seconds.")
    End Sub

    Private Sub UnlockForm()
        txtUsername.Enabled = True
        txtPassword.Enabled = True
        btnLogin.Enabled = True
        txtUsername.Clear()
        txtPassword.Clear()
        errorlogin.Visible = False
        loginAttempts = 0
        time += 30
        numLocked += 1
        AuditTrail.WriteLog("Form Unlocked.")
    End Sub

    Private Sub PictureBox2_Click(sender As Object, e As EventArgs) Handles PictureBox2.Click
        Application.Exit()
    End Sub

    Private Sub Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Panel1.Paint

    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click

    End Sub

    Private Sub Login_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' This should be called only once, typically when the application starts
        AuditTrail.InitializeLogFile("myapp_log.txt", Path.Combine(Application.StartupPath, "Logs"))
        AuditTrail.WriteLog("Application started.")

    End Sub
End Class
