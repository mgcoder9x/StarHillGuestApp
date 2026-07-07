<template>
    <button :class="buttonClass" @click="handleExport">
        {{ buttonLabel }}
    </button>
</template>

<script>
import ExcelJS from 'exceljs'

export default {
    name: 'ExcelExport',
    props: {
        title: {
            type: String,
            required: true,
        },
        query: {
            type: Array,
            required: true,
        },
        data: {
            type: Array,
            required: true, // Dữ liệu cần xuất (mảng các object)
        },
        columns: {
            type: Array,
            required: true, // Định nghĩa các cột (header)
        },
        fileName: {
            type: String,
            default: 'ExportedData.xlsx', // Tên file mặc định
        },
        buttonLabel: {
            type: String,
            default: 'Xuất Excel', // Nhãn nút mặc định
        },
        buttonClass: {
            type: String,
            default: 'btn-export', // Lớp CSS tùy chỉnh cho nút
        },
    },
    methods: {
        async handleExport() {
            const workbook = new ExcelJS.Workbook()
            const worksheet = workbook.addWorksheet('Sheet1')

            // Gộp ô cho tiêu đề báo cáo
            const addTitle = () => {
                worksheet.mergeCells(1, 1, 1, this.columns.length)
                const titleCell = worksheet.getCell(1, 1)
                titleCell.value = this.title // Đặt tiêu đề báo cáo
                titleCell.font = { bold: true, size: 16 } // Font chữ in đậm, kích thước lớn
                titleCell.alignment = {
                    horizontal: 'center',
                    vertical: 'middle',
                }
            }

            const addQuery = () => {
                let rowIndex = 2
                this.query.forEach((item) => {
                    const row = worksheet.getRow(rowIndex)
                    row.values = [item.header, item.value]
                    const headerCell = row.getCell(1)
                    headerCell.font = { bold: true }
                    headerCell.alignment = {
                        horizontal: 'left',
                        vertical: 'middle',
                    }
                    const valueCell = row.getCell(2)
                    valueCell.alignment = {
                        horizontal: 'left',
                        vertical: 'middle',
                    }
                    worksheet.mergeCells(
                        rowIndex,
                        2,
                        rowIndex,
                        this.columns.length > 3 ? this.columns.length : 3
                    )
                    rowIndex += 1
                })

                // Tự động điều chỉnh kích thước cột theo nội dung
                worksheet.columns = [
                    {
                        width: Math.max(
                            ...this.query.map((q) => q.header.length),
                            10
                        ),
                    },
                    {
                        width: Math.max(
                            ...this.query.map((q) => q.value.length),
                            20
                        ),
                    },
                ]
            }

            const addData = () => {
                // Xác định dòng trống đầu tiên
                const startRow = worksheet.lastRow
                    ? worksheet.lastRow.number + 1
                    : 1
                // Thêm tiêu đề cột
                worksheet.getRow(startRow).values = this.columns.map(
                    (col) => col.header
                )
                worksheet.getRow(startRow).font = { bold: true }
                worksheet.getRow(startRow).alignment = {
                    horizontal: 'center',
                    vertical: 'middle',
                }
                worksheet.columns = this.columns.map((col) => ({
                    key: col.key,
                    width: col.width || 20,
                }))
                // Thêm dữ liệu
                this.data.forEach((row) => {
                    worksheet.addRow(row)
                })
            }

            // Gọi hàm
            addTitle()
            addQuery()
            addData()

            // Lưu file Excel
            const buffer = await workbook.xlsx.writeBuffer()
            const blob = new Blob([buffer], {
                type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
            })
            const link = document.createElement('a')
            link.href = URL.createObjectURL(blob)
            link.download = this.fileName
            link.click()
        },
    },
}
</script>

<style scoped>
/* CSS tùy chỉnh cho nút */
.btn-export {
    background-color: #4caf50;
    color: white;
    border: none;
    padding: 10px 20px;
    font-size: 16px;
    cursor: pointer;
    border-radius: 4px;
}

.btn-export:hover {
    background-color: #45a049;
}
</style>
