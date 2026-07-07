import ExcelJS from 'exceljs'

class ExcelExporter {
    constructor({
        title,
        query,
        data,
        columns,
        fileName = 'ExportedData.xlsx',
    }) {
        this.title = title
        this.query = query
        this.data = data
        this.columns = columns
        this.fileName = fileName
        this.workbook = new ExcelJS.Workbook()
        this.worksheet = this.workbook.addWorksheet('Sheet1')
    }

    addTitle() {
        this.worksheet.mergeCells(1, 1, 1, this.columns.length)
        const titleCell = this.worksheet.getCell(1, 1)
        titleCell.value = this.title
        titleCell.font = { bold: true, size: 16 }
        titleCell.alignment = { horizontal: 'center', vertical: 'middle' }
    }

    addQuery() {
        let rowIndex = 2
        this.query.forEach((item) => {
            const row = this.worksheet.getRow(rowIndex)
            row.values = [item.header, item.value]
            const headerCell = row.getCell(1)
            headerCell.font = { bold: true }
            headerCell.alignment = { horizontal: 'left', vertical: 'middle' }
            const valueCell = row.getCell(2)
            valueCell.alignment = { horizontal: 'left', vertical: 'middle' }
            this.worksheet.mergeCells(
                rowIndex,
                2,
                rowIndex,
                // this.columns.length > 3 ? this.columns.length : 3
                3
            )
            rowIndex += 1
        })

        this.worksheet.columns = [
            { width: Math.max(...this.query.map((q) => q.header.length), 10) },
            { width: Math.max(...this.query.map((q) => q.value.length), 20) },
        ]
    }

    addData() {
        const startRow = this.worksheet.lastRow
            ? this.worksheet.lastRow.number + 1
            : 1
        this.worksheet.getRow(startRow).values = this.columns.map(
            (col) => col.header
        )
        this.worksheet.getRow(startRow).font = { bold: true }
        this.worksheet.getRow(startRow).alignment = {
            horizontal: 'center',
            vertical: 'middle',
        }
        this.worksheet.columns = this.columns.map((col) => ({
            key: col.key,
            width: col.width || 20,
        }))
        this.data.forEach((row) => {
            this.worksheet.addRow(row)
        })
    }

    groupRows(startRow, column) {
        const lastRow = this.worksheet.lastRow.number
        if (this.worksheet.getCell(startRow, column).isMerged) {
            this.worksheet.getCell(startRow, column).font = { bold: true }
            this.groupRows(startRow + 1, column)
        } else {
            for (
                let rowNumber = startRow;
                rowNumber <= lastRow;
                rowNumber += 1
            ) {
                const cell = this.worksheet.getCell(rowNumber, column)
                cell.font = { bold: true }
                cell.alignment = {
                    horizontal: 'center',
                    vertical: 'middle',
                }
                if (
                    cell.value !==
                    this.worksheet.getCell(rowNumber + 1, column).value
                ) {
                    this.worksheet.mergeCells(
                        startRow,
                        column,
                        rowNumber,
                        column
                    )
                    this.groupRows(rowNumber + 1, column)
                    break
                }
            }
        }
    }

    async export() {
        this.addTitle()
        this.addQuery()
        this.addData()

        const buffer = await this.workbook.xlsx.writeBuffer()
        const blob = new Blob([buffer], {
            type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        })
        const link = document.createElement('a')
        link.href = URL.createObjectURL(blob)
        link.download = this.fileName
        link.click()
    }
}

export default ExcelExporter
