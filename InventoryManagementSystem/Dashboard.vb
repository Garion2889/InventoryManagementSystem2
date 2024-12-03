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

    Public Sub New(admin As String, username As String)
        InitializeComponent()
        activeacc = $"{admin} / {username}"
    End Sub
    'Audit Trail
    Private logDirectoryPath As String = "Logs"
    ' Execute query with parameters
    Private Sub ExecuteQuery(query As String, ParamArray parameters() As MySqlParameter)
        Using conn As New MySqlConnection(connectionString)
            Using cmd As New MySqlCommand(query, conn)
                cmd.Parameters.AddRange(parameters)
                conn.Open()
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    ' UI Navigation
    Private Sub NavigateTo(panel As Panel)
        ' Hide all panels
        dashboardpnl.Visible = False
        pnlProducts.Visible = False
        supplierspnl.Visible = False
        Reportspnl.Visible = False
        Adjntranspanl.Visible = False
        Auditpnl.Visible = False

        ' Show the specified panel
        panel.Visible = True
    End Sub

    ' Navigation Button Click Events
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

    ' Exit Application
    Private Sub PictureBox3_Click(sender As Object, e As EventArgs) Handles PictureBox3.Click
        AuditTrail.WriteLog(activeacc & "exits the application.")
        Application.Exit()
    End Sub

    ' Form Load
    Private Sub Dashboard_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadProducts()
        LoadSuppliers()
        LoadAdjustments()
        LoadLogFiles()
        LoadStockAlertData()
        stocklevel.Series.Clear()

        ' Add a new series for the bar chart
        Dim series As New Series("Stock Data")
        series.ChartType = SeriesChartType.Bar ' Set the chart type to Bar

        ' Customize the Bar Chart
        series.IsValueShownAsLabel = True ' Show values on the chart
        series.LabelFormat = "#.##" ' Format the values

        ' Add the series to the existing chart
        stocklevel.Series.Add(series)

        ' Configure the chart area for better visibility
        With stocklevel.ChartAreas(0)
            .AxisX.Title = "Products" ' X-axis title
            .AxisY.Title = "Stock Level" ' Y-axis title
            .AxisX.Interval = 1 ' Ensure all categories are displayed
            .AxisX.MajorGrid.Enabled = False ' Disable gridlines on X-axis
            .AxisY.MajorGrid.LineColor = Color.LightGray ' Light gray gridlines on Y-axis
        End With

        ' Retrieve data from MySQL and populate the chart
        currentstock(series)
        reorderchart.Series.Clear()

        ' Add a series for current stock (Bar Chart)
        Dim stockSeries As New Series("Current Stock")
        stockSeries.ChartType = SeriesChartType.Column ' Use Column chart type
        stockSeries.IsValueShownAsLabel = True
        stockSeries.LabelFormat = "#.##"

        Dim reorderSeries As New Series("Reorder Level")
        reorderSeries.ChartType = SeriesChartType.Line ' Keep Line chart type
        reorderSeries.BorderWidth = 2
        reorderSeries.Color = Color.Red
        reorderSeries.IsValueShownAsLabel = False

        ' Add both series to the chart
        reorderchart.Series.Add(stockSeries)
        reorderchart.Series.Add(reorderSeries)

        ' Configure the chart area
        With reorderchart.ChartAreas(0)
            .AxisX.Title = "Items"
            .AxisY.Title = "Quantity"
            .AxisX.Interval = 1
            .AxisX.MajorGrid.Enabled = False
            .AxisY.MajorGrid.LineColor = Color.LightGray
        End With

        ' Retrieve data from MySQL and populate the chart
        reorder(stockSeries, reorderSeries)

        restockstatus.Series.Clear()

        ' Add a new series for the pie chart
        Dim restockSeries As New Series("Restock Status")
        restockSeries.ChartType = SeriesChartType.Pie ' Set the chart type to Pie
        restockSeries.IsValueShownAsLabel = True ' Show values on the chart
        restockSeries.LabelFormat = "#%" ' Format as percentage

        ' Add the series to the chart
        restockstatus.Series.Add(restockSeries)

        ' Retrieve data and populate the chart
        restocked(restockSeries)

        strecevied.Series.Clear()

        ' Configure the chart area for better visibility
        With strecevied.ChartAreas(0)
            .AxisX.Title = "Products"
            .AxisY.Title = "Stock Received"
            .AxisX.Interval = 1
            .AxisX.MajorGrid.Enabled = False
            .AxisY.MajorGrid.LineColor = Color.LightGray
        End With

        ' Retrieve data from MySQL and populate the chart
        stockreceived()
        expirychart.Series.Clear()

        ' Configure the chart area for a timeline chart
        With expirychart.ChartAreas(0)
            .AxisX.Title = "Products"
            .AxisY.Title = "Expiration Date"
            .AxisX.Interval = 1
            .AxisX.MajorGrid.Enabled = False
            .AxisY.MajorGrid.LineColor = Color.LightGray
            .AxisY.LabelStyle.Format = "yyyy-MM-dd" ' Format Y-axis as a date
        End With

        ' Retrieve data from MySQL and populate the chart
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

        ' Configure Chart Area
        productsupplier.ChartAreas.Clear()
        Dim chartArea As New ChartArea("MainArea")
        productsupplier.ChartAreas.Add(chartArea)
        prosup()

        sek()

        ' Configure Chart Area

    End Sub
    Private Sub utp()
        tus.Series.Clear()

        ' Add a Chart Area if not already present
        tus.ChartAreas.Clear()
        Dim chartArea As New ChartArea("ChartArea1") ' Ensure the name matches
        tus.ChartAreas.Add(chartArea)

        ' Add a series for the bar chart
        Dim series As New Series("Total Units Supplied")
        series.ChartType = SeriesChartType.Bar ' Set the chart type to Bar
        series.ChartArea = "ChartArea1" ' Assign to the correct ChartArea
        series.IsValueShownAsLabel = True ' Display values on the chart
        series.Font = New Font("Arial", 10, FontStyle.Bold) ' Optional: Style for labels

        ' Add the series to the chart
        tus.Series.Add(series)

        ' Retrieve data from MySQL and populate the chart
        umf(series)
    End Sub
    Private Sub umf(series As Series)
        Dim query As String = "SELECT ProductName, SUM(Stock) AS TotalUnitsSupplied " &
                              "FROM products " &
                              "GROUP BY ProductName " &
                              "ORDER BY TotalUnitsSupplied DESC"

        ' Connect to the MySQL database
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            ' Execute the query and read the data
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    ' Get product name and total units supplied
                    Dim productName As String = reader("ProductName").ToString()
                    Dim totalUnits As Double = Convert.ToDouble(reader("TotalUnitsSupplied"))

                    ' Add data points to the series
                    series.Points.AddXY(productName, totalUnits)
                End While
            End Using
        End Using

    End Sub
    Private Sub sek()
        totalordersplaced.Series.Clear()

        ' Add a Chart Area if not already present
        totalordersplaced.ChartAreas.Clear()
        Dim chartArea As New ChartArea("ChartArea1") ' Ensure the name matches
        totalordersplaced.ChartAreas.Add(chartArea)

        ' Add a series for the line chart
        Dim series As New Series("Total Orders Placed")
        series.ChartType = SeriesChartType.Line ' Set the chart type to Line
        series.ChartArea = "ChartArea1" ' Assign to the correct ChartArea
        series.BorderWidth = 2 ' Line thickness
        series.IsValueShownAsLabel = True ' Display values on the chart

        ' Add the series to the chart
        totalordersplaced.Series.Add(series)

        ' Retrieve data from MySQL and populate the chart
        totalplacedorder(series)
    End Sub
    Private Sub totalplacedorder(series As Series)
        Dim query As String = "SELECT SupplierName, SUM(TotalOrdersPlaced) AS Orders " &
                              "FROM suppliers_info " &
                              "GROUP BY SupplierName " &
                              "ORDER BY Orders DESC"

        ' Connect to the MySQL database
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            ' Execute the query and read the data
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    ' Get supplier name and order count
                    Dim supplierName As String = reader("SupplierName").ToString()
                    Dim totalOrders As Double = Convert.ToDouble(reader("Orders"))

                    ' Add data points to the series
                    series.Points.AddXY(supplierName, totalOrders)
                End While
            End Using
        End Using
    End Sub
    Private Sub prosup()
        ' Define your MySQL connection string


        ' Define the SQL query
        Dim query As String = "SELECT Supplier, COUNT(Product) AS ProductCount FROM suppliers GROUP BY Supplier"

        ' Connect to the MySQL database
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            ' Execute the query and read the data
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    ' Get the supplier name and product count
                    Dim supplierName As String = reader("Supplier").ToString()
                    Dim productCount As Double = Convert.ToDouble(reader("ProductCount"))

                    ' Add a series for each supplier
                    Dim series As New Series(supplierName)
                    series.ChartType = SeriesChartType.StackedBar ' Set to Stacked Bar Chart
                    series.Points.AddXY(supplierName, productCount)

                    ' Add the series to the chart
                    productsupplier.Series.Add(series)
                End While
            End Using
        End Using
    End Sub

    Private Sub Wastage()
        Dim query As String = "SELECT SUM(Quantity) AS Wastage FROM stock_overview;"

        If Not waste.Series.IsUniqueName("Wastage") Then
            waste.Series.Remove(waste.Series("Wastage"))
        End If
        waste.Series.Add("Wastage")
        waste.Series("Wastage").ChartType = DataVisualization.Charting.SeriesChartType.Doughnut
        waste.Series("Wastage").IsValueShownAsLabel = True

        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()
            Using reader As MySqlDataReader = command.ExecuteReader()
                waste.Series("Wastage").Points.Clear() ' Clear any existing points
                If reader.Read() Then
                    Dim totalWastage As Integer = Convert.ToInt32(reader("Wastage"))
                    waste.Series("Wastage").Points.AddXY("Total Wastage", totalWastage)
                End If
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

                    ' Add data to the scatter plot
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

                    ' Add data to the chart
                    Chart2.Series("Stock Value").Points.AddXY(productName, totalValue)
                End While
            End Using
        End Using
    End Sub

    Private Sub expiry()
        ' Define your MySQL connection string
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

        ' Connect to the MySQL database
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            ' Create a new series for the timeline chart
            Dim series As New Series("Upcoming Expirations")
            series.ChartType = SeriesChartType.Point
            series.IsValueShownAsLabel = True
            expirychart.Series.Add(series)

            ' Execute the query and read the data
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim productName As String = reader("product_name").ToString()
                    Dim expiryDate As Date = Convert.ToDateTime(reader("expiry_date"))

                    ' Add data points to the series
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
        ' Define the SQL query
        Dim query As String = "
            SELECT 
                p.Product AS product_name, 
                s.Supplier AS supplier, 
                so.StockReceived AS stockreceived
            FROM 
                stock_overview so
            JOIN 
                products p ON so.ProductID = p.ProductID
            JOIN 
                suppliers s ON p.Supplier = s.Supplier
            WHERE 
                so.StockReceived > 0;"

        ' Connect to the MySQL database
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            ' Execute the query and read the data
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim productName As String = reader("productname").ToString()
                    Dim supplierName As String = reader("supplier").ToString()
                    Dim stockReceived As Double = Convert.ToDouble(reader("stockreceived"))

                    ' Check if a series already exists for the supplier
                    Dim series As Series = strecevied.Series.FindByName(supplierName)
                    If series Is Nothing Then
                        ' Add a new series for the supplier
                        series = New Series(supplierName)
                        series.ChartType = SeriesChartType.StackedBar
                        series.IsValueShownAsLabel = True
                        strecevied.Series.Add(series)
                    End If

                    ' Add data points to the series
                    series.Points.AddXY(productName, stockReceived)
                End While
            End Using
        End Using
    End Sub
    Private Sub restocked(series As Series)

        ' Define the SQL query to retrieve stock data
        Dim query As String = "SELECT productname, quantity, reorderlevel FROM stock_overview"

        ' Initialize counters for each category
        Dim lowCount As Integer = 0
        Dim adequateCount As Integer = 0
        Dim overstockCount As Integer = 0

        ' Connect to the MySQL database
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            ' Execute the query and read data
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    ' Read product data
                    Dim stock As Integer = Convert.ToInt32(reader("quantity"))
                    Dim reorderLevel As Integer = Convert.ToInt32(reader("reorderlevel"))

                    ' Categorize the product
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

        ' Add data points to the pie chart
        series.Points.AddXY("Low", lowCount)
        series.Points.AddXY("Adequate", adequateCount)
        series.Points.AddXY("Overstock", overstockCount)

        ' Customize colors for each category
        series.Points(0).Color = Color.Red ' Low
        series.Points(1).Color = Color.Orange ' Adequate
        series.Points(2).Color = Color.Green ' Overstock
    End Sub
    Private Sub reorder(stockSeries As Series, reorderSeries As Series)

        ' Define the SQL query
        Dim query As String = "SELECT productname, quantity, reorderlevel FROM stock_overview"

        ' Connect to the MySQL database
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            ' Execute the query and read the data
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    ' Get the item name, stock, and reorder level
                    Dim itemName As String = reader("productname").ToString()
                    Dim stock As Double = Convert.ToDouble(reader("quantity"))
                    Dim reorderLevel As Double = Convert.ToDouble(reader("reorderlevel"))

                    ' Add data points to the stock series
                    Dim stockPointIndex As Integer = stockSeries.Points.AddXY(itemName, stock)

                    ' Apply color coding to stock bars
                    If stock < reorderLevel Then
                        stockSeries.Points(stockPointIndex).Color = Color.Orange ' Low stock
                    Else
                        stockSeries.Points(stockPointIndex).Color = Color.Green ' Sufficient stock
                    End If

                    ' Add data points to the reorder level series
                    reorderSeries.Points.AddXY(itemName, reorderLevel)
                End While
            End Using
        End Using
    End Sub
    Private Sub currentstock(series As Series)
        ' Define your MySQL connection string

        ' Define the SQL query
        Dim query As String = "SELECT product, quantity FROM products"

        ' Connect to the MySQL database
        Using connection As New MySqlConnection(connectionString)
            Dim command As New MySqlCommand(query, connection)
            connection.Open()

            ' Execute the query and read the data
            Using reader As MySqlDataReader = command.ExecuteReader()
                While reader.Read()
                    ' Get the item name and stock values
                    Dim itemName As String = reader("product").ToString()
                    Dim stock As Double = Convert.ToDouble(reader("quantity"))

                    ' Add data points to the series
                    Dim pointIndex As Integer = series.Points.AddXY(itemName, stock)

                    ' Apply color coding
                    If stock = 0 Then
                        series.Points(pointIndex).Color = Color.Red ' Out of stock
                        AddToStockAlert(itemName, "Out of Stock", Color.Red)
                    ElseIf stock < 30 Then
                        series.Points(pointIndex).Color = Color.Orange ' Low stock
                        AddToStockAlert(itemName, "Low Stock", Color.Orange)
                    Else
                        series.Points(pointIndex).Color = Color.Green ' Sufficient stock
                    End If
                End While
            End Using
        End Using
    End Sub

    Private Sub AddToStockAlert(itemName As String, status As String, color As Color)
        ' Add the stock alert to a ListBox or Panel
        Dim label As New Label With {
            .Text = $"{itemName}: {status}",
            .ForeColor = color,
            .AutoSize = True
        }
    End Sub

    ' Load Data from Database
    Private Sub LoadData(tableName As String, dataGridView As DataGridView)
        Try
            Dim query As String = $"SELECT * FROM {tableName}"
            Dim adapter As New MySqlDataAdapter(query, connection)
            Dim table As New DataTable()
            adapter.Fill(table)
            dataGridView.DataSource = table
            For Each row As DataGridViewRow In dataGridView.Rows
                If row.IsNewRow Then Continue For

                ' Store the old values in the dictionary
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

    ' Search Functionality for Products
    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        SearchTable("products", TextBox1.Text, DataGridView2)
    End Sub

    ' Search Functionality for Suppliers
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

    ' Add/Edit/Delete for Products

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

                ' Create a log string for this row
                Dim logMessage As String = String.Empty
                logMessage &= $"/ {activeacc} edited row with {primaryKey} = {row.Cells(primaryKey).Value}: "

                ' Loop through all cells except for the primary key column
                Dim hasChanges As Boolean = False
                For Each cell As DataGridViewCell In row.Cells
                    If cell.OwningColumn.Name <> primaryKey Then
                        ' Only log if the value has changed
                        Dim oldValue As Object = oldValues(cell.OwningColumn.Name & row.Index.ToString())
                        If oldValue IsNot Nothing AndAlso Not oldValue.Equals(cell.Value) Then
                            ' Add the field and its change to the log
                            logMessage &= $"{cell.OwningColumn.Name}: '{oldValue}' → '{cell.Value}', "
                            hasChanges = True
                        End If
                    End If
                Next

                ' If there are changes, log the message
                If hasChanges Then
                    ' Trim the trailing comma and space
                    If logMessage.EndsWith(", ") Then
                        logMessage = logMessage.Substring(0, logMessage.Length - 2)
                    End If
                    AuditTrail.WriteLog(logMessage)

                    ' Proceed to save the changes to the database
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


    ' Add and Delete Rows
    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles addprdpanelbtn.Click
        addproductpanel.Visible = True
    End Sub

    ' Delete selected row from the grid
    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        DeleteSelectedRow(DataGridView2, "products", "ProductID")
    End Sub

    ' Delete rows from the database and DataGridView
    Private Sub DeleteSelectedRow(dataGridView As DataGridView, tableName As String, primaryKey As String)
        Try
            If dataGridView.SelectedRows.Count > 0 Then
                ' Confirm deletion with the user
                Dim result As DialogResult = MessageBox.Show("Are you sure you want to delete the selected rows?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                If result = DialogResult.No Then Exit Sub

                ' Loop through each selected row
                For Each row As DataGridViewRow In dataGridView.SelectedRows
                    If Not row.IsNewRow Then
                        ' Ensure the primary key value is not null or empty
                        Dim primaryKeyValue As Object = row.Cells(primaryKey).Value
                        If primaryKeyValue IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(primaryKeyValue.ToString()) Then
                            ' Log the deletion
                            Dim logMessage As String = $"Row deleted: {primaryKey} = {primaryKeyValue.ToString()}"
                            AuditTrail.WriteLog(logMessage) ' This will log the deletion

                            ' Execute the DELETE query
                            Dim query As String = $"DELETE FROM {tableName} WHERE {primaryKey} = @ID"
                            ExecuteQuery(query, New MySqlParameter("@ID", primaryKeyValue))

                            ' Remove row from the DataGridView
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
            ' Open database connection
            connection.Open()

            ' Prepare the insert query
            Dim insertQuery As String = "INSERT INTO products (Product, Category, Quantity, Brand, CostPrice, SellingPrice, Supplier, StockStatus, ExpiryDate) " &
                                    "VALUES (@Product, @Category, @Quantity, @Brand, @CostPrice, @SellingPrice, @Supplier, @StockStatus, @ExpiryDate)"

            ' Execute the insert query
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

            ' Reload the products list after adding
            LoadProducts()

            ' Log the details of the added product
            Dim logMessage As String = $"Product added: {txtProd.Text}, Category: {txtCategory.Text}, Quantity: {txtQuantity.Text}, " &
                                   $"Brand: {txtBrand.Text}, Supplier: {txtSup.Text}, CostPrice: {txtCostPrice.Text}, " &
                                   $"SellingPrice: {txtSellingPrice.Text}, StockStatus: {txtStockStatus.Text}, ExpiryDate: {dtpExpiryDate.Value.ToShortDateString()}"
            AuditTrail.WriteLog(logMessage) ' Log the added product details

            ' Success message after saving
            MessageBox.Show("Product details saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            ' Display error message if something goes wrong
            MessageBox.Show("Error saving product: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub


    ' Delete supplier
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

    ' Add a new supplier
    Private Sub btnNewProduct_Click(sender As Object, e As EventArgs) Handles supplieraddbtn.Click
        Try
            ' Open database connection
            Using connection As New MySqlConnection("Server=localhost;Database=isleshopdb;Uid=root;Pwd=;")
                connection.Open()

                ' Prepare the insert query
                Dim insertQuery As String = "INSERT INTO suppliers (Supplier, Product, ContactPerson, PhoneNumber, EmailAddress) " &
                                        "VALUES (@Supplier, @Product, @ContactPerson, @PhoneNumber, @Email)"

                ' Execute the insert query
                Using insertCommand As New MySqlCommand(insertQuery, connection)
                    insertCommand.Parameters.AddWithValue("@Supplier", txtSupplier.Text)
                    insertCommand.Parameters.AddWithValue("@Product", txtProduct.Text)
                    insertCommand.Parameters.AddWithValue("@ContactPerson", txtContact.Text)
                    insertCommand.Parameters.AddWithValue("@PhoneNumber", txtPhoneNumber.Text)
                    insertCommand.Parameters.AddWithValue("@Email", txtEmail.Text)
                    insertCommand.ExecuteNonQuery()
                End Using
            End Using

            ' Reload the suppliers list after adding
            LoadSuppliers()

            ' Log the details of the added supplier
            Dim logMessage As String = $"Supplier added: {txtSupplier.Text}, Product: {txtProduct.Text}, " &
                                   $"Contact Person: {txtContact.Text}, Phone Number: {txtPhoneNumber.Text}, Email: {txtEmail.Text}"
            AuditTrail.WriteLog(logMessage) ' Log the added supplier details

            ' Success message after saving
            MessageBox.Show("Supplier details saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            ' Display error message if something goes wrong
            MessageBox.Show("Error saving supplier: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Clear product input fields
    Private Sub ClearProductInputFields()
        txtProd.Clear()
        txtCategory.Clear()
        txtQuantity.Clear()
        txtBrand.Clear()
        txtCostPrice.Clear()
        txtSupplier.Clear()
        txtSellingPrice.Clear()
        txtStockStatus.Clear()
        dtpExpiryDate.Value = DateTime.Now ' Reset to today
    End Sub

    ' Clear supplier input fields
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
    Private Sub LoadStockAlertData()
        Try
            Dim threshold As Integer = 5 ' Define low stock threshold
            Dim query As String = "SELECT Product, Quantity FROM products WHERE Quantity <= @Threshold ORDER BY Quantity ASC"
            Dim adapter As New MySqlDataAdapter(query, connection)
            adapter.SelectCommand.Parameters.AddWithValue("@Threshold", threshold)

            Dim table As New DataTable()
            adapter.Fill(table)

            UpdateStockAlertChart(table) ' Pass data to the chart update function
        Catch ex As Exception
            MessageBox.Show($"Error loading stock alert data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub UpdateStockAlertChart(data As DataTable)
        ' Clear existing series
        stocklevel.Series.Clear()

        ' Create a new series
        Dim series As New Series("Stock Alert") With {
        .ChartType = SeriesChartType.Column
    }

        ' Loop through the data and add points to the series
        For Each row As DataRow In data.Rows
            Dim productName As String = row("Product").ToString()
            Dim quantity As Integer = Convert.ToInt32(row("Quantity"))

            ' Add a data point
            Dim point As New DataPoint()
            point.AxisLabel = productName
            point.YValues = New Double() {quantity}

            ' Apply color coding
            If quantity = 0 Then
                point.Color = Color.Red ' Out of stock
            ElseIf quantity <= 5 Then
                point.Color = Color.Yellow ' Low stock
            Else
                point.Color = Color.Green ' Adequate stock
            End If

            series.Points.Add(point)
        Next

        ' Add the series to the chart
        stocklevel.Series.Add(series)

        ' Configure chart area
        With stocklevel.ChartAreas(0)
            .AxisX.Title = "Products"
            .AxisY.Title = "Quantity"
            .RecalculateAxesScale()
        End With

        ' Refresh chart
        stocklevel.Invalidate()
    End Sub

End Class