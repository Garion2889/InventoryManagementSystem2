Imports System.Data.SqlClient
Imports System.IO
Imports System.Windows.Forms.DataVisualization.Charting
Imports MySql.Data.MySqlClient

Public Class Dashboard
    ' Database connection string
    Private Const connectionString As String = "Server=localhost;Database=isleshopdb;Uid=root;Pwd=;Convert Zero DateTime=True;"
    Private connection As New MySqlConnection(connectionString)

    ' Tracks editing state
    Private isEditingProducts As Boolean = False
    Private isEditingSuppliers As Boolean = False
    Private oldValues As New Dictionary(Of String, Object)

    'Active Acc
    Private activeacc As String

    Private WithEvents fadeTimer As Timer
    Private fadeIn As Boolean = True
    Private fadeOut As Boolean = False
    Private currentAlpha As Integer = 0 ' Used to store current alpha value for transparency

    Private lblMessage As Label

    Public Sub initialize()
        If toastPanel Is Nothing Then
            MessageBox.Show("Toast Panel not initialized.")
            Return
        End If
        If lblMessage Is Nothing Then
            lblMessage = New Label()
            lblMessage.Text = ""
            lblMessage.ForeColor = Color.FromArgb(226, 230, 235)
            lblMessage.Font = New Font("Arial", 10, FontStyle.Bold)
            lblMessage.TextAlign = ContentAlignment.MiddleCenter
            lblMessage.Dock = DockStyle.Fill
            toastPanel.Controls.Add(lblMessage)
        End If
        fadeTimer = New Timer()
        fadeTimer.Interval = 50
    End Sub
    Public Sub ShowToast(message As String)
        initialize()
        If lblMessage Is Nothing Then
            MessageBox.Show("Label not initialized in the toast panel.")
            Return
        End If
        lblMessage.Text = message
        currentAlpha = 0
        toastPanel.BackColor = Color.FromArgb(currentAlpha, 33, 41, 52)
        toastPanel.Visible = True
        fadeIn = True
        fadeOut = False
        fadeTimer.Start()
    End Sub
    Private Sub fadeTimer_Tick(sender As Object, e As EventArgs) Handles fadeTimer.Tick
        If fadeIn Then
            If currentAlpha < 255 Then
                currentAlpha += 5
                toastPanel.BackColor = Color.FromArgb(currentAlpha, 33, 41, 52)
            Else
                fadeIn = False
                Task.Delay(3000).ContinueWith(Sub() BeginFadeOut())
            End If
        ElseIf fadeOut Then
            If currentAlpha > 0 Then
                currentAlpha -= 5
                toastPanel.BackColor = Color.FromArgb(currentAlpha, 33, 41, 52)
            Else
                fadeTimer.Stop()
                toastPanel.Visible = False
            End If
        End If
    End Sub

    Private Sub BeginFadeOut()
        fadeOut = True
    End Sub

    Public Sub New(admin As String, username As String)
        InitializeComponent()
        activeacc = $"{admin} / {username}"
    End Sub

    Public Sub New()
    End Sub

    Private logDirectoryPath As String = "Logs"

    Private Sub ExecuteQuery(query As String, ParamArray parameters() As MySqlParameter)
        Using conn As New MySqlConnection(connectionString)
            Using cmd As New MySqlCommand(query, conn)
                cmd.Parameters.AddRange(parameters)
                conn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub


    Private Sub NavigateTo(panel As Panel)

        dashboardpnl.Visible = False
        pnlProducts.Visible = False
        supplierspnl.Visible = False
        Reportspnl.Visible = False
        Adjntranspanl.Visible = False
        Auditpnl.Visible = False
        panel.Visible = True
    End Sub

    Private Sub btnDashboard_Click(sender As Object, e As EventArgs) Handles btnDashboard.Click
        NavigateTo(dashboardpnl)
        If dashboardpnl.Visible = False Then
            AuditTrail.WriteLog(activeacc & "navigates to Dashboard.")
        End If
    End Sub

    Private Sub btnProducts_Click(sender As Object, e As EventArgs) Handles btnProducts.Click
        NavigateTo(pnlProducts)
        If pnlProducts.Visible = False Then
            AuditTrail.WriteLog(activeacc & "navigates to Products.")
        End If
    End Sub

    Private Sub btnSuppliers_Click(sender As Object, e As EventArgs) Handles btnSuppliers.Click
        NavigateTo(supplierspnl)
        If supplierspnl.Visible = False Then
            AuditTrail.WriteLog(activeacc & "navigates to Suppliers.")
        End If
    End Sub

    Private Sub btnReports_Click(sender As Object, e As EventArgs) Handles btnReports.Click
        NavigateTo(Reportspnl)
        If Reportspnl.Visible = False Then
            AuditTrail.WriteLog(activeacc & "navigates to Reports.")
        End If
    End Sub

    Private Sub btnAdjnTrans_Click(sender As Object, e As EventArgs) Handles btnAdjnTrans.Click
        NavigateTo(Adjntranspanl)
        If Adjntranspanl.Visible = False Then
            AuditTrail.WriteLog(activeacc & "navigates to dashboard.")
        End If
    End Sub

    Private Sub btnAudit_Click(sender As Object, e As EventArgs) Handles btnAudit.Click
        NavigateTo(Auditpnl)
        If btnAudit.Visible = False Then
            AuditTrail.WriteLog(activeacc & "navigates to dashboard.")
        End If
    End Sub

    Private Sub PictureBox3_Click(sender As Object, e As EventArgs) Handles PictureBox3.Click
        AuditTrail.WriteLog(activeacc & "exits the application.")
        Application.Exit()
    End Sub

    Private Sub Dashboard_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadProducts()
        LoadSuppliers()
        LoadAdjustments()
        LoadLogFiles()

        Try
            Using conn As New MySqlConnection(connectionString)
                conn.Open()

                Dim expiringQuery As String = "SELECT COUNT(*) FROM products WHERE ExpiryDate <= DATE_ADD(CURDATE(), INTERVAL 7 DAY);"
                Dim expiringCmd As New MySqlCommand(expiringQuery, conn)
                Dim expiringCount As Integer = Convert.ToInt32(expiringCmd.ExecuteScalar())

                Dim lowStockQuery As String = "SELECT COUNT(*) FROM products where stockstatus = 'Low Stock';"
                Dim lowStockCmd As New MySqlCommand(lowStockQuery, conn)
                Dim lowStockCount As Integer = Convert.ToInt32(lowStockCmd.ExecuteScalar())

                If expiringCount > 0 OrElse lowStockCount > 0 Then
                    Dim message As String = ""
                    message = "Notification" & Environment.NewLine
                    If expiringCount > 0 Then
                        message &= $"Expiring items: {expiringCount}" & Environment.NewLine
                    End If
                    If lowStockCount > 0 Then
                        message &= $"Low-stock items: {lowStockCount}"
                    End If
                    ShowToast(message)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try


        stocklevel.Series.Clear()

        Dim series As New Series("Stock Data")
        series.ChartType = SeriesChartType.Bar

        series.IsValueShownAsLabel = True
        series.LabelFormat = "#.##"

        stocklevel.Series.Add(series)

        With stocklevel.ChartAreas(0)
            .AxisX.Title = "Products"
            .AxisY.Title = "Stock Level"
            .AxisX.Interval = 1
            .AxisX.MajorGrid.Enabled = False
            .AxisY.MajorGrid.LineColor = Color.LightGray
        End With

        currentstock(series)
        reorderchart.Series.Clear()


        Dim stockSeries As New Series("Current Stock")
        stockSeries.ChartType = SeriesChartType.Column
        stockSeries.IsValueShownAsLabel = True
        stockSeries.LabelFormat = "#.##"

        Dim reorderSeries As New Series("Reorder Level")
        reorderSeries.ChartType = SeriesChartType.Line
        reorderSeries.BorderWidth = 2
        reorderSeries.Color = Color.Red
        reorderSeries.IsValueShownAsLabel = False

        reorderchart.Series.Add(stockSeries)
        reorderchart.Series.Add(reorderSeries)

        With reorderchart.ChartAreas(0)
            .AxisX.Title = "Items"
            .AxisY.Title = "Quantity"
            .AxisX.Interval = 1
            .AxisX.MajorGrid.Enabled = False
            .AxisY.MajorGrid.LineColor = Color.LightGray
        End With

        reorder(stockSeries, reorderSeries)

        restockstatus.Series.Clear()

        Dim restockSeries As New Series("Restock Status")
        restockSeries.ChartType = SeriesChartType.Pie
        restockSeries.IsValueShownAsLabel = True

        restockstatus.Series.Add(restockSeries)

        restocked(restockSeries)

        strecevied.Series.Clear()

        With strecevied.ChartAreas(0)
            .AxisX.Title = "Products"
            .AxisY.Title = "Stock Received"
            .AxisX.Interval = 1
            .AxisX.MajorGrid.Enabled = False
            .AxisY.MajorGrid.LineColor = Color.LightGray
        End With

        stockreceived()
        expirychart.Series.Clear()

        With expirychart.ChartAreas(0)
            .AxisX.Title = "Products"
            .AxisY.Title = "Expiration Date"
            .AxisX.Interval = 1
            .AxisX.MajorGrid.Enabled = False
            .AxisY.MajorGrid.LineColor = Color.LightGray
            .AxisY.LabelStyle.Format = "yyyy-MM-dd"
        End With

        expiry()
        totalvalue()

        With salesperfomance
            .Series.Clear()
            .Series.Add("Sales Performance")
            .Series("Sales Performance").ChartType = DataVisualization.Charting.SeriesChartType.Point
            .ChartAreas(0).AxisX.Title = "Product Name"
            .ChartAreas(0).AxisY.Title = "Sales Volume"
            .ChartAreas(0).AxisX.Interval = 1
            .ChartAreas(0).AxisX.IsLabelAutoFit = True
            .Series("Sales Performance").MarkerSize = 10
            .Series("Sales Performance").MarkerStyle = DataVisualization.Charting.MarkerStyle.Circle
        End With
        SalesPerformance()
        productsupplier.Series.Clear()

        productsupplier.ChartAreas.Clear()
        Dim chartArea As New ChartArea("MainArea")
        productsupplier.ChartAreas.Add(chartArea)
        prosup()
        sek()
        TUS()
        OFR()
        connectAlt()
        connectTotalSpending()
        connectPaymentTerms()
        connectOutstandingPayments()
        loadsupplierrating()
        loadIssuesReportedBarChart()
        loadPreferredSupplierStatusBarChart()
    End Sub
    Private Sub TUS()
        TUSCHART.Series.Clear()

        TUSCHART.ChartAreas.Clear()
        Dim chartArea As New ChartArea("ChartArea1")
        TUSCHART.ChartAreas.Add(chartArea)

        Dim series As New Series("Total Units Supplied")
        series.ChartType = SeriesChartType.Column
        series.ChartArea = "ChartArea1"
        series.IsValueShownAsLabel = True
        series.Font = New Font("Arial", 10, FontStyle.Bold)

        TUSCHART.Series.Add(series)

        tusload(series)
    End Sub
    Private Sub tusload(series As Series)
        Dim query As String = "SELECT suppliername,TotalUnitsSupplied
                              FROM suppliers_info;"
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim supplierName As String = reader("SupplierName").ToString()
                    Dim totalUnits As Double = Convert.ToDouble(reader("TotalUnitsSupplied"))
                    series.Points.AddXY(supplierName, totalUnits)
                End While
            End Using
        End Using
    End Sub
    Private Sub SetFulfillmentRateGaugeChart(fulfillmentRate As Double)
        OFRchart.Series.Clear()
        OFRchart.Legends.Clear()

        Dim series As New Series("Fulfillment Rate")
        series.ChartType = SeriesChartType.Doughnut
        series.Points.Add(fulfillmentRate)
        series.Points.Add(100 - fulfillmentRate)

        OFRchart.Series.Add(series)

        OFRchart.Palette = ChartColorPalette.BrightPastel
        OFRchart.ChartAreas(0).Area3DStyle.Enable3D = True

        series("PieLabelStyle") = "Outside"
        series("Exploded") = "true"
        series.BorderWidth = 0

        OFRchart.Titles.Clear()
        OFRchart.Titles.Add("Order Fulfillment Rate")

        For Each point As DataPoint In series.Points
            point.IsValueShownAsLabel = True
            point.Label = $"{point.YValues(0):0}%"
        Next
    End Sub
    Dim query As String = "SELECT OrderFulfillmentRate FROM suppliers_info;"

    Private Sub connectAlt()
        Dim query As String = "SELECT SupplierName, AverageLeadTime FROM suppliers_info;"

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            Using reader As MySqlDataReader = command.ExecuteReader()
                altChart.Series.Clear()

                Dim series As New Series("Average Lead Time")
                series.ChartType = SeriesChartType.Bar
                series.BorderWidth = 3
                series.Color = Color.Blue

                While reader.Read()
                    Dim Supplier As String = reader("SupplierName").ToString()
                    Dim averageLeadTime As Double = If(IsDBNull(reader("AverageLeadTime")), 0, Convert.ToDouble(reader("AverageLeadTime")))

                    series.Points.AddXY(Supplier, averageLeadTime)
                End While

                altChart.Series.Add(series)

                altChart.Palette = ChartColorPalette.BrightPastel
                altChart.ChartAreas(0).Area3DStyle.Enable3D = False

                altChart.Titles.Clear()
                altChart.Titles.Add("Average Lead Time per Supplier")

                altChart.ChartAreas(0).AxisX.Title = "Supplier"
                altChart.ChartAreas(0).AxisY.Title = "Average Lead Time (Days)"

                altChart.ChartAreas(0).AxisY.LabelStyle.Format = "0.00"

                altChart.ChartAreas(0).AxisX.LabelStyle.Angle = 0
                altChart.ChartAreas(0).AxisX.Interval = 1
            End Using
        End Using
    End Sub
    Private Sub connectPaymentTerms()
        Dim query As String = "SELECT SupplierName, PaymentTerms, COUNT(*) AS TermCount FROM suppliers_info GROUP BY SupplierName, PaymentTerms;"

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            Using reader As MySqlDataReader = command.ExecuteReader()
                paymentTermsChart.Series.Clear()
                paymentTermsChart.ChartAreas.Clear()
                Dim chartArea As New ChartArea("ChartArea1")
                paymentTermsChart.ChartAreas.Add(chartArea)
                Dim uniqueTerms As New HashSet(Of String)
                Dim data As New List(Of Tuple(Of String, String, Integer))
                While reader.Read()
                    Dim supplier As String = reader("SupplierName").ToString()
                    Dim paymentTerm As String = reader("PaymentTerms").ToString()
                    Dim termCount As Integer = Convert.ToInt32(reader("TermCount"))
                    uniqueTerms.Add(paymentTerm)
                    data.Add(Tuple.Create(supplier, paymentTerm, termCount))
                End While
                For Each term As String In uniqueTerms
                    Dim series As New Series(term)
                    series.ChartType = SeriesChartType.Bar
                    series.IsValueShownAsLabel = True
                    For Each entry In data
                        If entry.Item2 = term Then
                            series.Points.AddXY(entry.Item1, entry.Item3)
                        End If
                    Next
                    paymentTermsChart.Series.Add(series)
                Next
                paymentTermsChart.Titles.Clear()
                paymentTermsChart.Titles.Add("Payment Terms by Supplier")
                paymentTermsChart.Palette = ChartColorPalette.BrightPastel

                chartArea.AxisX.Title = "Supplier Name"
                chartArea.AxisY.Title = "Frequency of Payment Terms"
                chartArea.AxisY.LabelStyle.Format = "0"

                chartArea.InnerPlotPosition = New ElementPosition(5, 5, 90, 85)
                chartArea.AxisX.Interval = 1
                chartArea.AxisX.LabelStyle.Font = New Font("Arial", 8)

            End Using
        End Using
    End Sub
    Private Sub connectOutstandingPayments()
        Dim query As String = "SELECT SupplierName, OutstandingPayments FROM suppliers_info;"

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            Using reader As MySqlDataReader = command.ExecuteReader()
                outstandingPaymentsChart.Series.Clear()
                outstandingPaymentsChart.ChartAreas.Clear()

                Dim chartArea As New ChartArea("ChartArea1")
                outstandingPaymentsChart.ChartAreas.Add(chartArea)

                Dim series As New Series("Outstanding Payments")
                series.ChartType = SeriesChartType.Bar
                series.IsValueShownAsLabel = True
                series.Color = Color.Orange

                While reader.Read()
                    Dim supplierName As String = reader("SupplierName").ToString()
                    Dim outstandingPayments As Double = Convert.ToDouble(reader("OutstandingPayments"))

                    Dim shortSupplierName As String = If(supplierName.Length > 10, supplierName.Substring(0, 10) & "...", supplierName)

                    series.Points.AddXY(shortSupplierName, outstandingPayments)
                End While

                outstandingPaymentsChart.Series.Add(series)

                outstandingPaymentsChart.Titles.Clear()
                outstandingPaymentsChart.Titles.Add("Outstanding Payments by Supplier")
                outstandingPaymentsChart.Palette = ChartColorPalette.BrightPastel

                chartArea.AxisX.Title = "Supplier Name"
                chartArea.AxisY.Title = "Outstanding Payments (in $)"
                chartArea.AxisY.LabelStyle.Format = "C"

                chartArea.AxisX.LabelStyle.Angle = -45
                chartArea.AxisX.LabelStyle.Font = New Font("Arial", 8)

                chartArea.InnerPlotPosition = New ElementPosition(5, 5, 90, 85)
                chartArea.AxisX.Interval = 1
            End Using
        End Using
    End Sub

    Private Sub connectTotalSpending()
        Dim query As String = "SELECT SupplierName, TotalSpending FROM suppliers_info;"

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            Using reader As MySqlDataReader = command.ExecuteReader()
                spendingChart.Series.Clear()

                Dim series As New Series("Total Spending")
                series.ChartType = SeriesChartType.Bar
                series.BorderWidth = 3
                series.Color = Color.Blue
                While reader.Read()
                    Dim Supplier As String = reader("SupplierName").ToString()

                    Dim TotalSpending As Double = If(IsDBNull(reader("TotalSpending")), 0, Convert.ToDouble(reader("TotalSpending")))

                    series.Points.AddXY(Supplier, TotalSpending)
                End While

                spendingChart.Series.Add(series)

                spendingChart.Palette = ChartColorPalette.BrightPastel
                spendingChart.ChartAreas(0).Area3DStyle.Enable3D = False

                spendingChart.Titles.Clear()
                spendingChart.Titles.Add("Average Lead Time per Supplier")

                spendingChart.ChartAreas(0).AxisX.Title = "Supplier"
                spendingChart.ChartAreas(0).AxisY.Title = "Total Spending"

                spendingChart.ChartAreas(0).AxisY.LabelStyle.Format = "0.00"

                spendingChart.ChartAreas(0).AxisX.LabelStyle.Angle = 0
                spendingChart.ChartAreas(0).AxisX.Interval = 1
            End Using
        End Using
    End Sub

    Private Sub OFR()
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()


            Using reader As MySqlDataReader = command.ExecuteReader()
                If reader.Read() Then

                    Dim fulfillmentRate As Double = Convert.ToDouble(reader("OrderFulfillmentRate"))


                    SetFulfillmentRateGaugeChart(fulfillmentRate)
                End If
            End Using
        End Using
    End Sub
    Private Sub utp()
        TUSCHART.Series.Clear()

        TUSCHART.ChartAreas.Clear()
        Dim chartArea As New ChartArea("ChartArea1")
        TUSCHART.ChartAreas.Add(chartArea)


        Dim series As New Series("Total Units Supplied")
        series.ChartType = SeriesChartType.Bar
        series.ChartArea = "ChartArea1"
        series.IsValueShownAsLabel = True
        series.Font = New Font("Arial", 10, FontStyle.Bold)

        TUSCHART.Series.Add(series)
        umf(series)
    End Sub

    Private Sub loadsupplierrating()
        Dim query As String = "SELECT SupplierName, SupplierRating FROM suppliers_info;"
        suprate.Series.Clear()

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            Using reader As MySqlDataReader = command.ExecuteReader()
                Dim Series1 As New Series("SupplierRating")
                Series1.ChartType = SeriesChartType.Bar

                While reader.Read()
                    Dim supplierName As String = reader("SupplierName").ToString()
                    Dim supplyrate As Integer = Convert.ToInt32(reader("SupplierRating"))


                    Series1.Points.AddXY(supplierName, supplyrate)
                End While
                suprate.Series.Add(Series1)

                suprate.Titles.Clear()
                suprate.Titles.Add("Supplier Ratings")


                suprate.ChartAreas(0).AxisX.Title = "Supplier"
                suprate.ChartAreas(0).AxisY.Title = "Rating (1-5)"


                suprate.ChartAreas(0).AxisY.Minimum = 0
                suprate.ChartAreas(0).AxisY.Maximum = 5
                suprate.ChartAreas(0).AxisY.Interval = 1



                suprate.ChartAreas(0).AxisX.Interval = 1
                suprate.ChartAreas(0).AxisX.LabelStyle.IsEndLabelVisible = True
                suprate.ChartAreas(0).AxisX.LabelStyle.Font = New Font("Arial", 8)

                suprate.Width = 600 '
            End Using
        End Using
    End Sub

    Private Sub loadPreferredSupplierStatusBarChart()
        Dim query As String = "SELECT SupplierName, PreferredSupplierStatus FROM suppliers_info;"
        PSSchart.Series.Clear()

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            Using reader As MySqlDataReader = command.ExecuteReader()
                Dim series As New Series("Preferred Supplier Status")
                series.ChartType = SeriesChartType.Bar
                series.BorderWidth = 3

                While reader.Read()
                    Dim supplierName As String = reader("SupplierName").ToString()
                    Dim preferredStatus As Integer = Convert.ToInt32(reader("PreferredSupplierStatus"))
                    If preferredStatus = 1 Then
                        series.Points.AddXY(supplierName, 1)
                    ElseIf preferredStatus = 0 Then
                        series.Points.AddXY(supplierName, 0)
                    End If
                End While

                PSSchart.Series.Add(series)

                PSSchart.Titles.Clear()
                PSSchart.Titles.Add("Supplier Preferred Status")

                PSSchart.ChartAreas(0).AxisX.Title = "Supplier"
                PSSchart.ChartAreas(0).AxisY.Title = "Preferred Status"

                PSSchart.ChartAreas(0).AxisY.Minimum = 0
                PSSchart.ChartAreas(0).AxisY.Maximum = 1
                PSSchart.ChartAreas(0).AxisY.Interval = 1
                PSSchart.Width = 600

                PSSchart.ChartAreas(0).AxisX.Interval = 1
                PSSchart.ChartAreas(0).AxisX.LabelStyle.IsEndLabelVisible = True
                PSSchart.ChartAreas(0).AxisX.LabelStyle.Font = New Font("Arial", 8)

                series.Color = Color.Blue
            End Using
        End Using
    End Sub

    Private Sub loadIssuesReportedBarChart()
        Dim query As String = "SELECT SupplierName, IssuesReported FROM suppliers_info;"
        IssuesChart.Series.Clear()
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Using reader As MySqlDataReader = command.ExecuteReader()
                Dim series As New Series("Issues Reported")
                series.ChartType = SeriesChartType.Bar
                series.BorderWidth = 3
                While reader.Read()
                    Dim supplierName As String = reader("SupplierName").ToString()
                    Dim issuesCount As Integer = Convert.ToInt32(reader("IssuesReported"))

                    series.Points.AddXY(supplierName, issuesCount)
                End While

                IssuesChart.Series.Add(series)

                IssuesChart.Titles.Clear()
                IssuesChart.Titles.Add("Issues Reported by Suppliers")

                IssuesChart.ChartAreas(0).AxisX.Title = "Supplier"
                IssuesChart.ChartAreas(0).AxisY.Title = "Number of Issues"

                IssuesChart.ChartAreas(0).AxisY.Minimum = 0
                IssuesChart.ChartAreas(0).AxisY.Interval = 1

                IssuesChart.Width = 600

                IssuesChart.ChartAreas(0).AxisX.Interval = 1
                IssuesChart.ChartAreas(0).AxisX.LabelStyle.IsEndLabelVisible = True
                IssuesChart.ChartAreas(0).AxisX.LabelStyle.Font = New Font("Arial", 8)

                series.Color = Color.Red
            End Using
        End Using
    End Sub


    Private Sub umf(series As Series)
        Dim query As String = "SELECT ProductName, SUM(Stock) AS TotalUnitsSupplied " &
                              "FROM products " &
                              "GROUP BY ProductName " &
                              "ORDER BY TotalUnitsSupplied DESC"

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim productName As String = reader("ProductName").ToString()
                    Dim totalUnits As Double = Convert.ToDouble(reader("TotalUnitsSupplied"))
                    series.Points.AddXY(productName, totalUnits)
                End While
            End Using
        End Using

    End Sub

    Private Sub sek()
        totalordersplaced.Series.Clear()
        totalordersplaced.ChartAreas.Clear()
        Dim chartArea As New ChartArea("ChartArea1")
        totalordersplaced.ChartAreas.Add(chartArea)
        Dim series As New Series("Total Orders Placed")
        series.ChartType = SeriesChartType.Line
        series.ChartArea = "ChartArea1"
        series.BorderWidth = 2
        series.IsValueShownAsLabel = True
        totalordersplaced.Series.Add(series)

        totalplacedorder(series)
    End Sub
    Private Sub totalplacedorder(series As Series)
        Dim query As String = "SELECT SupplierName, SUM(TotalOrdersPlaced) AS Orders " &
                              "FROM suppliers_info " &
                              "GROUP BY SupplierName " &
                              "ORDER BY Orders DESC"
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()

                    Dim supplierName As String = reader("SupplierName").ToString()
                    Dim totalOrders As Double = Convert.ToDouble(reader("Orders"))

                    series.Points.AddXY(supplierName, totalOrders)
                End While
            End Using
        End Using
    End Sub
    Private Sub prosup()
        Dim query As String = "SELECT Supplier, COUNT(Product) AS ProductCount FROM suppliers GROUP BY Supplier"
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim supplierName As String = reader("Supplier").ToString()
                    Dim productCount As Double = Convert.ToDouble(reader("ProductCount"))
                    Dim series As New Series(supplierName)
                    series.ChartType = SeriesChartType.StackedBar
                    series.Points.AddXY(supplierName, productCount)
                    productsupplier.Series.Add(series)
                End While
            End Using
        End Using
    End Sub

    Private Sub SalesPerformance()
        Dim query As String = "
        SELECT 
            ProductName , 
            Category, totalvalue, 
            SalesPerformance
        FROM 
            stock_overview"

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim productName As String = reader("productname").ToString()
                    Dim totalSalesVolume As Integer = Convert.ToInt32(reader("totalvalue"))
                    Dim demandCategory As String = reader("SalesPerformance").ToString()
                    salesperfomance.Series("Sales Performance").Points.AddXY(productName, totalSalesVolume)
                    salesperfomance.Series("Sales Performance").Points.Last().Label = demandCategory
                    salesperfomance.Series("Sales Performance").Points.Last().MarkerColor = GetMarkerColor(demandCategory)
                End While
            End Using
        End Using
    End Sub

    Private Function GetMarkerColor(demandCategory As String) As Drawing.Color
        Select Case demandCategory
            Case "High Demand"
                Return Drawing.Color.Green
            Case "Adequate"
                Return Drawing.Color.Orange
            Case "Low Demand"
                Return Drawing.Color.Red
            Case Else
                Return Drawing.Color.Gray
        End Select
    End Function

    Private Sub totalvalue()

        Dim query As String = "
        SELECT 
            Productname , 
           category, 
            totalvalue
        FROM 
            stock_overview
       ;"

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim productName As String = reader("productname").ToString()
                    Dim category As String = reader("category").ToString()
                    Dim totalValue As Decimal = Convert.ToDecimal(reader("totalvalue"))
                    Chart2.Series("Stock Value").Points.AddXY(productName, totalValue)
                End While
            End Using
        End Using
    End Sub

    Private Sub expiry()
        Dim query As String = "
            SELECT 
                p.Product AS product_name, 
                p.Category AS category_name, 
                pe.ExpiryDate AS expiry_date
            FROM 
                products p
            JOIN 
                products pe ON p.ProductID = pe.ProductID
            WHERE 
                pe.ExpiryDate >= CURDATE()
            ORDER BY 
                pe.ExpiryDate ASC;"
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Dim series As New Series("Upcoming Expirations")
            series.ChartType = SeriesChartType.Point
            series.IsValueShownAsLabel = True
            expirychart.Series.Add(series)
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim productName As String = reader("product_name").ToString()
                    Dim expiryDate As Date = Convert.ToDateTime(reader("expiry_date"))
                    series.Points.AddXY(productName, expiryDate)
                End While
            End Using
        End Using
        With Chart2
            .Series.Clear()
            .Series.Add("Stock Value")
            .Series("Stock Value").ChartType = DataVisualization.Charting.SeriesChartType.Bar
            .ChartAreas(0).AxisX.Title = "Product Name"
            .ChartAreas(0).AxisY.Title = "Total Value"
            .ChartAreas(0).AxisX.Interval = 1
            .ChartAreas(0).AxisX.IsLabelAutoFit = False
        End With
    End Sub

    Private Sub stockreceived()
        Dim query As String = "
   SELECT p.Product, s.Supplier, SUM(so.StockReceived) AS TotalStockReceived
FROM stock_overview so
JOIN products p ON so.ProductID = p.ProductID
JOIN suppliers s ON p.Supplier = s.Supplier
WHERE so.StockReceived > 0
GROUP BY p.Product, s.Supplier
ORDER BY s.Supplier, p.Product;"

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim productName As String = reader("Product").ToString()
                    Dim supplierName As String = reader("Supplier").ToString()
                    Dim stockReceived As Double = Convert.ToDouble(reader("TotalStockReceived"))
                    Dim series As Series = strecevied.Series.FindByName(supplierName)
                    If series Is Nothing Then
                        series = New Series(supplierName)
                        series.ChartType = SeriesChartType.StackedBar
                        series.IsValueShownAsLabel = True
                        strecevied.Series.Add(series)
                    End If
                    series.Points.AddXY(productName, stockReceived)
                End While
            End Using
        End Using
    End Sub
    Private Sub restocked(series As Series)
        Dim query As String = "SELECT productname, quantity, reorderlevel FROM stock_overview"
        Dim lowCount As Integer = 0
        Dim adequateCount As Integer = 0
        Dim overstockCount As Integer = 0
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim stock As Integer = Convert.ToInt32(reader("quantity"))
                    Dim reorderLevel As Integer = Convert.ToInt32(reader("reorderlevel"))
                    If stock < reorderLevel Then
                        lowCount += 1
                    ElseIf stock <= reorderLevel * 2 Then
                        adequateCount += 1
                    Else
                        overstockCount += 1
                    End If
                End While
            End Using
        End Using
        series.Points.AddXY("Low", lowCount)
        series.Points.AddXY("Adequate", adequateCount)
        series.Points.AddXY("Overstock", overstockCount)
        series.Points(0).Color = Color.Red
        series.Points(1).Color = Color.Orange
        series.Points(2).Color = Color.Green
    End Sub
    Private Sub reorder(stockSeries As Series, reorderSeries As Series)
        Dim query As String = "SELECT productname, quantity, reorderlevel FROM stock_overview"
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim itemName As String = reader("productname").ToString()
                    Dim stock As Double = Convert.ToDouble(reader("quantity"))
                    Dim reorderLevel As Double = Convert.ToDouble(reader("reorderlevel"))
                    Dim stockPointIndex As Integer = stockSeries.Points.AddXY(itemName, stock)
                    If stock < reorderLevel Then
                        stockSeries.Points(stockPointIndex).Color = Color.Orange
                    Else
                        stockSeries.Points(stockPointIndex).Color = Color.Green
                    End If
                    reorderSeries.Points.AddXY(itemName, reorderLevel)
                End While
            End Using
        End Using
    End Sub
    Private Sub currentstock(series As Series)
        Dim query As String = "SELECT product, quantity FROM products"
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim itemName As String = reader("product").ToString()
                    Dim stock As Double = Convert.ToDouble(reader("quantity"))
                    Dim pointIndex As Integer = series.Points.AddXY(itemName, stock)
                    If stock = 0 Then
                        series.Points(pointIndex).Color = Color.Red
                        AddToStockAlert(itemName, "Out of Stock", Color.Red)
                    ElseIf stock < 30 Then
                        series.Points(pointIndex).Color = Color.Orange
                        AddToStockAlert(itemName, "Low Stock", Color.Orange)
                    Else
                        series.Points(pointIndex).Color = Color.Green
                    End If
                End While
            End Using
        End Using
    End Sub

    Private Sub AddToStockAlert(itemName As String, status As String, color As Color)
        Dim label As New Label With {
            .Text = $"{itemName}: {status}",
            .ForeColor = color,
            .AutoSize = True
        }
    End Sub

    Private Sub LoadData(tableName As String, dataGridView As DataGridView)
        Try
            Dim query As String = $"SELECT * FROM {tableName}"
            Dim adapter As New MySqlDataAdapter(query, connection)
            Dim table As New DataTable()
            adapter.Fill(table)
            dataGridView.DataSource = table
            For Each row As DataGridViewRow In dataGridView.Rows
                If row.IsNewRow Then Continue For
                For Each cell As DataGridViewCell In row.Cells
                    oldValues(cell.OwningColumn.Name & row.Index.ToString()) = If(cell.Value, DBNull.Value)
                Next
            Next

        Catch ex As Exception
            MessageBox.Show($"Error loading {tableName}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadProducts()
        LoadData("products", DataGridView2)
    End Sub

    Private Sub LoadSuppliers()
        LoadData("suppliers", DataGridView3)
    End Sub

    Private Sub LoadAdjustments()
        LoadData("adjustments", DataGridView1)
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        SearchTable("products", TextBox1.Text, DataGridView2)
    End Sub
    Private Sub TextBox3_TextChanged(sender As Object, e As EventArgs) Handles TextBox3.TextChanged
        SearchTable("suppliers", TextBox3.Text, DataGridView3)
    End Sub

    Private Sub SearchTable(tableName As String, searchValue As String, dataGridView As DataGridView)
        Try
            Dim query As String = $"SELECT * FROM {tableName} WHERE CONCAT_WS('', {GetColumnsForSearch(tableName)}) LIKE @SearchValue"
            Dim adapter As New MySqlDataAdapter(query, connection)
            adapter.SelectCommand.Parameters.AddWithValue("@SearchValue", $"%{searchValue}%")
            Dim table As New DataTable()
            adapter.Fill(table)
            dataGridView.DataSource = table
        Catch ex As Exception
            MessageBox.Show($"Error searching {tableName}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function GetColumnsForSearch(tableName As String) As String
        Select Case tableName
            Case "products"
                Return "ProductID, Category, Brand, Product, Quantity, CostPrice, SellingPrice, ExpiryDate, Supplier, StockStatus"
            Case "suppliers"
                Return "Supplier, Product, ContactPerson, PhoneNumber, EmailAddress, ID"
            Case Else
                Return "*"
        End Select
    End Function
    Private Sub btnEdit_Click(sender As Object, e As EventArgs) Handles btnEdit.Click
        ToggleEditing(DataGridView2, btnEdit, "products", "ProductID", isEditingProducts)
        AuditTrail.WriteLog(activeacc & " toggles product edit.")
    End Sub

    Private Sub btnEditSupplier_Click(sender As Object, e As EventArgs) Handles btnEditSupplier.Click
        ToggleEditing(DataGridView3, btnEditSupplier, "suppliers", "ID", isEditingSuppliers)
        AuditTrail.WriteLog(activeacc & " toggles supplier edit.")
    End Sub

    Private Sub ToggleEditing(dataGridView As DataGridView, button As Button, tableName As String, primaryKey As String, ByRef isEditing As Boolean)
        If isEditing Then
            dataGridView.ReadOnly = True
            button.Text = $"Edit {tableName}"
            SaveDataGridChanges(dataGridView, tableName, primaryKey)
            LoadData(tableName, dataGridView)
            isEditing = False
        Else
            dataGridView.ReadOnly = False
            button.Text = "Done"
            isEditing = True
        End If
    End Sub

    Private Sub SaveDataGridChanges(dataGridView As DataGridView, tableName As String, primaryKey As String)
        Try
            For Each row As DataGridViewRow In dataGridView.Rows
                If row.IsNewRow Then Continue For
                Dim logMessage As String = String.Empty
                logMessage &= $"/ {activeacc} edited row with {primaryKey} = {row.Cells(primaryKey).Value}: "
                Dim hasChanges As Boolean = False
                For Each cell As DataGridViewCell In row.Cells
                    If cell.OwningColumn.Name <> primaryKey Then
                        Dim oldValue As Object = oldValues(cell.OwningColumn.Name & row.Index.ToString())
                        If oldValue IsNot Nothing AndAlso Not oldValue.Equals(cell.Value) Then
                            logMessage &= $"{cell.OwningColumn.Name}: '{oldValue}' → '{cell.Value}', "
                            hasChanges = True
                        End If
                    End If
                Next
                If hasChanges Then
                    If logMessage.EndsWith(", ") Then
                        logMessage = logMessage.Substring(0, logMessage.Length - 2)
                    End If
                    AuditTrail.WriteLog(logMessage)
                    Dim updateQuery As String = $"UPDATE {tableName} SET " &
                    String.Join(", ", row.Cells.Cast(Of DataGridViewCell) _
                    .Where(Function(c) c.OwningColumn.Name <> primaryKey) _
                    .Select(Function(c) $"{c.OwningColumn.Name} = @{c.OwningColumn.Name}")) &
                    $" WHERE {primaryKey} = @{primaryKey}"

                    Using command As New MySqlCommand(updateQuery, connection)
                        For Each cell As DataGridViewCell In row.Cells
                            command.Parameters.AddWithValue($"@{cell.OwningColumn.Name}", If(cell.Value, DBNull.Value))
                        Next
                        If connection.State = ConnectionState.Closed Then connection.Open()
                        command.ExecuteNonQuery()
                        connection.Close()
                    End Using
                End If
            Next
            MessageBox.Show("Changes saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles addprdpanelbtn.Click
        addproductpanel.Visible = True
    End Sub
    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        DeleteSelectedRow(DataGridView2, "products", "ProductID")
    End Sub
    Private Sub DeleteSelectedRow(dataGridView As DataGridView, tableName As String, primaryKey As String)
        Try
            If dataGridView.SelectedRows.Count > 0 Then
                Dim result As DialogResult = MessageBox.Show("Are you sure you want to delete the selected rows?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                If result = DialogResult.No Then Exit Sub
                For Each row As DataGridViewRow In dataGridView.SelectedRows
                    If Not row.IsNewRow Then
                        Dim primaryKeyValue As Object = row.Cells(primaryKey).Value
                        If primaryKeyValue IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(primaryKeyValue.ToString()) Then
                            Dim logMessage As String = $"Row deleted: {primaryKey} = {primaryKeyValue.ToString()}"
                            AuditTrail.WriteLog(logMessage)
                            Dim query As String = $"DELETE FROM {tableName} WHERE {primaryKey} = @ID"
                            ExecuteQuery(query, New MySqlParameter("@ID", primaryKeyValue))
                            dataGridView.Rows.Remove(row)
                        Else
                            MessageBox.Show("Error: Missing primary key value for the selected row.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        End If
                    End If
                Next

                MessageBox.Show("Selected rows deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                MessageBox.Show("No rows selected for deletion.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error deleting rows: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnProduct_Click(sender As Object, e As EventArgs) Handles addproductbtn.Click
        Try
            connection.Open()
            Dim insertQuery As String = "INSERT INTO products (Product, Category, Quantity, Brand, CostPrice, SellingPrice, Supplier, StockStatus, ExpiryDate) " &
                                    "VALUES (@Product, @Category, @Quantity, @Brand, @CostPrice, @SellingPrice, @Supplier, @StockStatus, @ExpiryDate)"
            Using insertCommand As New MySqlCommand(insertQuery, connection)
                insertCommand.Parameters.AddWithValue("@Product", txtProd.Text)
                insertCommand.Parameters.AddWithValue("@Category", txtCategory.Text)
                insertCommand.Parameters.AddWithValue("@Quantity", txtQuantity.Text)
                insertCommand.Parameters.AddWithValue("@Supplier", txtSup.Text)
                insertCommand.Parameters.AddWithValue("@Brand", txtBrand.Text)
                insertCommand.Parameters.AddWithValue("@CostPrice", txtCostPrice.Text)
                insertCommand.Parameters.AddWithValue("@SellingPrice", txtSellingPrice.Text)
                insertCommand.Parameters.AddWithValue("@StockStatus", txtStockStatus.Text)
                insertCommand.Parameters.AddWithValue("@ExpiryDate", dtpExpiryDate.Value)
                insertCommand.ExecuteNonQuery()
            End Using
            LoadProducts()
            Dim logMessage As String = $"Product added: {txtProd.Text}, Category: {txtCategory.Text}, Quantity: {txtQuantity.Text}, " &
                                   $"Brand: {txtBrand.Text}, Supplier: {txtSup.Text}, CostPrice: {txtCostPrice.Text}, " &
                                   $"SellingPrice: {txtSellingPrice.Text}, StockStatus: {txtStockStatus.Text}, ExpiryDate: {dtpExpiryDate.Value.ToShortDateString()}"
            AuditTrail.WriteLog(logMessage)
            MessageBox.Show("Product details saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            MessageBox.Show("Error saving product: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub btnDeleteSupplier_Click(sender As Object, e As EventArgs) Handles btnDeleteSupplier.Click
        DeleteSelectedRow(DataGridView3, "suppliers", "ID")
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles canceladdbtn.Click
        addproductpanel.Visible = False
    End Sub

    Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles suppliercancelbtn.Click
        supplieraddpanel.Visible = False
    End Sub

    Private Sub btnNewSupplier_Click(sender As Object, e As EventArgs) Handles btnNewSupplier.Click
        supplieraddpanel.Visible = True
    End Sub
    Private Sub btnNewProduct_Click(sender As Object, e As EventArgs) Handles supplieraddbtn.Click
        Try
            Using connection As New MySqlConnection("Server=localhost;Database=isleshopdb;Uid=root;Pwd=;")
                connection.Open()
                Dim insertQuery As String = "INSERT INTO suppliers (Supplier, Product, ContactPerson, PhoneNumber, EmailAddress) " &
                                        "VALUES (@Supplier, @Product, @ContactPerson, @PhoneNumber, @Email)"

                Using insertCommand As New MySqlCommand(insertQuery, connection)
                    insertCommand.Parameters.AddWithValue("@Supplier", txtSupplier.Text)
                    insertCommand.Parameters.AddWithValue("@Product", txtProduct.Text)
                    insertCommand.Parameters.AddWithValue("@ContactPerson", txtContact.Text)
                    insertCommand.Parameters.AddWithValue("@PhoneNumber", txtPhoneNumber.Text)
                    insertCommand.Parameters.AddWithValue("@Email", txtEmail.Text)
                    insertCommand.ExecuteNonQuery()
                End Using
            End Using
            LoadSuppliers()
            Dim logMessage As String = $"Supplier added: {txtSupplier.Text}, Product: {txtProduct.Text}, " &
                                   $"Contact Person: {txtContact.Text}, Phone Number: {txtPhoneNumber.Text}, Email: {txtEmail.Text}"
            AuditTrail.WriteLog(logMessage)
            MessageBox.Show("Supplier details saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            MessageBox.Show("Error saving supplier: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub ClearProductInputFields()
        txtProd.Clear()
        txtCategory.Clear()
        txtQuantity.Clear()
        txtBrand.Clear()
        txtCostPrice.Clear()
        txtSupplier.Clear()
        txtSellingPrice.Clear()
        txtStockStatus.Clear()
        dtpExpiryDate.Value = DateTime.Now
    End Sub
    Private Sub ClearSupplierInputFields()
        txtSupplier.Clear()
        txtProduct.Clear()
        txtContact.Clear()
        txtPhoneNumber.Clear()
        txtEmail.Clear()
    End Sub

    Private Sub RichTextBox1_TextChanged(sender As Object, e As EventArgs) Handles textBoxLogContent.TextChanged

    End Sub

    Private Sub LoadLogFiles()
        listBoxLogs.Items.Clear()
        Dim logFiles As String() = Directory.GetFiles(logDirectoryPath, "*.txt")
        For Each logFile As String In logFiles
            listBoxLogs.Items.Add(Path.GetFileName(logFile))
        Next
    End Sub
    Private Sub listBoxLogs_SelectedIndexChanged(sender As Object, e As EventArgs) Handles listBoxLogs.SelectedIndexChanged
        If listBoxLogs.SelectedItem IsNot Nothing Then
            Dim selectedLogFile As String = Path.Combine(logDirectoryPath, listBoxLogs.SelectedItem.ToString())
            DisplayLogFileContent(selectedLogFile)
        End If
    End Sub
    Private Sub DisplayLogFileContent(filePath As String)
        Try
            Dim logContent As String = File.ReadAllText(filePath)
            textBoxLogContent.Text = logContent
        Catch ex As Exception
            MessageBox.Show("Error reading log file: " & ex.Message)
        End Try
    End Sub
    Private Sub btnLoadLogs_Click(sender As Object, e As EventArgs) Handles btnLoadLogs.Click
        LoadLogFiles()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles exportprdbtn.Click
        prdExportToPDF()
    End Sub
    Public Function productGetData() As DataTable
        Dim query As String = "SELECT * FROM products"
        Dim conn As New MySqlConnection(connectionString)
        Dim adapter As New MySqlDataAdapter(query, conn)
        Dim table As New DataTable()
        Try
            conn.Open()
            adapter.Fill(table)
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message)
        Finally
            conn.Close()
        End Try

        Return table
    End Function

    Public Function supplierGetData() As DataTable
        Dim query As String = "SELECT * FROM suppliers"
        Dim conn As New MySqlConnection(connectionString)
        Dim adapter As New MySqlDataAdapter(query, conn)
        Dim table As New DataTable()
        Try
            conn.Open()
            adapter.Fill(table)
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message)
        Finally
            conn.Close()
        End Try

        Return table
    End Function

    Private Sub supplierexportbtn_Click(sender As Object, e As EventArgs) Handles supplierexportbtn.Click
        supplierExportToPDF()
    End Sub
End Class