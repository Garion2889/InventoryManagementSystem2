Imports iTextSharp.text
Imports iTextSharp.text.pdf
Imports System.IO

Module exportpdf
    Dim dashboardform As New Dashboard()
    Public Sub prdExportToPDF()
        Dim table As DataTable = dashboardform.productGetData()

        If table.Rows.Count = 0 Then
            MessageBox.Show("No data available to export.")
            Return
        End If

        Dim pdfDoc As New Document(PageSize.A4, 10, 10, 10, 10)

        Try
            Dim filepath As String = Path.Combine(Application.StartupPath, "Products\products.pdf")
            Dim pdfWriter As PdfWriter = PdfWriter.GetInstance(pdfDoc, New FileStream(filepath, FileMode.Create))
            pdfDoc.Open()

            Dim titleFont As New Font(Font.FontFamily.HELVETICA, 16, Font.BOLD)
            pdfDoc.Add(New Paragraph("Database Export", titleFont))
            pdfDoc.Add(New Paragraph(" "))

            Dim pdfTable As New PdfPTable(table.Columns.Count)
            pdfTable.WidthPercentage = 100

            For Each column As DataColumn In table.Columns
                Dim cell As New PdfPCell(New Phrase(column.ColumnName))
                cell.BackgroundColor = BaseColor.LIGHT_GRAY
                pdfTable.AddCell(cell)
            Next
            For Each row As DataRow In table.Rows
                For Each cellData As Object In row.ItemArray
                    pdfTable.AddCell(cellData.ToString())
                Next
            Next

            pdfDoc.Add(pdfTable)
            pdfDoc.Close()

            MessageBox.Show("PDF exported successfully to: " & filepath)
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message)
        Finally
            If pdfDoc.IsOpen Then
                pdfDoc.Close()
            End If
        End Try
    End Sub
    Public Sub supplierExportToPDF()
        Dim table As DataTable = dashboardform.supplierGetData()

        If table.Rows.Count = 0 Then
            MessageBox.Show("No data available to export.")
            Return
        End If

        Dim pdfDoc As New Document(PageSize.A4, 10, 10, 10, 10)

        Try
            Dim filepath As String = Path.Combine(Application.StartupPath, "Suppliers\suppliers.pdf")
            Dim pdfWriter As PdfWriter = PdfWriter.GetInstance(pdfDoc, New FileStream(filepath, FileMode.Create))
            pdfDoc.Open()

            Dim titleFont As New Font(Font.FontFamily.HELVETICA, 16, Font.BOLD)
            pdfDoc.Add(New Paragraph("Database Export", titleFont))
            pdfDoc.Add(New Paragraph(" "))

            Dim pdfTable As New PdfPTable(table.Columns.Count)
            pdfTable.WidthPercentage = 100

            For Each column As DataColumn In table.Columns
                Dim cell As New PdfPCell(New Phrase(column.ColumnName))
                cell.BackgroundColor = BaseColor.LIGHT_GRAY
                pdfTable.AddCell(cell)
            Next

            For Each row As DataRow In table.Rows
                For Each cellData As Object In row.ItemArray
                    pdfTable.AddCell(cellData.ToString())
                Next
            Next

            pdfDoc.Add(pdfTable)
            pdfDoc.Close()

            MessageBox.Show("PDF exported successfully to: " & filepath)
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message)
        Finally
            If pdfDoc.IsOpen Then
                pdfDoc.Close()
            End If
        End Try
    End Sub
End Module
