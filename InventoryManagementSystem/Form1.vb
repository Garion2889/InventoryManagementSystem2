Imports System.Windows.Forms.DataVisualization.Charting
Imports MySql.Data.MySqlClient
Imports Mysqlx

Public Class Form1
    Private Const connectionString As String = "Server=localhost;Database=asdsad;Uid=root;Pwd=;Convert Zero DateTime=True;"
    Private Shared ReadOnly connection As New MySqlConnection(connectionString)

    Private Sub LoadStockAlertData()
        Try
            ' Define low stock threshold and limit
            Dim threshold As Integer = 5
            Dim query As String = "SELECT Product, Quantity FROM products WHERE Quantity <= @Threshold ORDER BY Quantity ASC LIMIT 100"
            Dim adapter As New MySqlDataAdapter(query, connection)
            adapter.SelectCommand.Parameters.AddWithValue("@Threshold", threshold)

            Dim table As New DataTable()
            adapter.Fill(table)

            ' Update the chart with the fetched data
            Chart1.SuspendLayout()
            UpdateStockAlertChart(table)
            Chart1.ResumeLayout()
        Catch ex As Exception
            MessageBox.Show($"Error loading stock alert data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try


    End Sub
    Private Sub UpdateStockAlertChart(data As DataTable)
        ' Clear existing chart data
        Chart1.Series.Clear()

        ' Create a new series
        Dim series As New Series("Stock Alert")
        series.ChartType = SeriesChartType.Bar

        ' Loop through the data and add points to the series
        For Each row As DataRow In data.Rows
            Dim productName As String = row("Product").ToString()
            Dim quantity As Integer = Convert.ToInt32(row("Quantity"))

            ' Add a data point
            Dim point As DataPoint = New DataPoint()
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

            ' Add point to series
            series.Points.Add(point)
        Next

        ' Add the series to the chart
        Chart1.Series.Add(series)

        ' Update chart area
        Chart1.ChartAreas(0).AxisX.Title = "Products"
        Chart1.ChartAreas(0).AxisY.Title = "Quantity"
        Chart1.ChartAreas(0).RecalculateAxesScale()
    End Sub
End Class