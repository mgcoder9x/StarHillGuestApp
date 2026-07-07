<template>
    <div>
        <ValidationObserver ref="observer">
            <b-card>
                <b-card-body>
                    <b-form>
                        <b-row>
                            <b-col md="6">
                                <b-form-group
                                    :label="$t('WaterEvents.Label.Areas')"
                                    label-cols-md="4"
                                >
                                    <tree-select
                                        v-model="SearchForm.AreaId"
                                        :options="listArea"
                                        label="text"
                                        :reduce="(area) => area.id"
                                    />
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <ValidationProvider
                                    v-slot="{ errors }"
                                    :name="$t('WaterEvents.Label.FromDate')"
                                    :rules="`fromDate:` + SearchForm.ToDate"
                                >
                                    <b-form-group
                                        label-cols-md="4"
                                        :label="
                                            $t('WaterEvents.Label.FromDate')
                                        "
                                    >
                                        <b-form-datepicker
                                            v-model="SearchForm.FromDate"
                                            :date-format-options="{
                                                day: 'numeric',
                                                month: 'long',
                                                year: 'numeric',
                                            }"
                                            type="datetime"
                                            :locale="currentLocale"
                                            :placeholder="
                                                $t(
                                                    'WaterEvents.Placeholder.Datetime'
                                                )
                                            "
                                        />
                                        <small class="validate-message">{{
                                            errors[0]
                                        }}</small>
                                    </b-form-group>
                                </ValidationProvider>
                            </b-col>
                            <b-col md="6">
                                <b-form-group
                                    label-cols-md="4"
                                    :label="$t('WaterEvents.Label.ToDate')"
                                >
                                    <b-form-datepicker
                                        v-model="SearchForm.ToDate"
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        type="datetime"
                                        :locale="currentLocale"
                                        :placeholder="
                                            $t(
                                                'WaterEvents.Placeholder.Datetime'
                                            )
                                        "
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </b-form>
                    <div class="text-center">
                        <b-button variant="primary" @click="exportData">
                            <feather-icon icon="DownloadCloudIcon" />
                            {{ $t('Button.ExportExcel') }}
                        </b-button>
                    </div>
                </b-card-body>
            </b-card>
        </ValidationObserver>
    </div>
</template>
<script>
import { lookupService } from '@/services'
// import ExcelExport from '@/components/ExcelExport.vue'
import useJwt from '@/auth/jwt/useJwt'
import ExcelExporter from '@/utils/export-excel'

class CustomExcelExporter extends ExcelExporter {
    addTitle() {
        this.worksheet.mergeCells(1, 1, 1, this.columns.length - 1)
        const titleCell = this.worksheet.getCell(1, 1)
        titleCell.value = this.title
        titleCell.font = { bold: true, size: 16 }
        titleCell.alignment = { horizontal: 'center', vertical: 'middle' }
    }

    addData() {
        const startRow = this.worksheet.lastRow
            ? this.worksheet.lastRow.number + 1
            : 1
        this.worksheet.getRow(startRow).values = this.columns
            .slice(1)
            .map((col) => col.header)
        this.worksheet.getRow(startRow).font = { bold: true }
        this.worksheet.getRow(startRow).alignment = {
            horizontal: 'center',
            vertical: 'middle',
        }
        this.worksheet.columns = this.columns.slice(1).map((col) => ({
            key: col.key,
            width: col.width || 20,
        }))
        let currentDate = null
        this.data.forEach((row) => {
            if (row.date !== currentDate) {
                const [year, month, day] = row.date.split('-')
                this.worksheet.addRow([`Ngày ${day}/${month}/${year}`])
                this.worksheet.mergeCells(
                    this.worksheet.lastRow.number,
                    1,
                    this.worksheet.lastRow.number,
                    this.columns.length - 1
                )
                currentDate = row.date
            }
            const newRow = { ...row }
            delete newRow.date
            this.worksheet.addRow(newRow)
        })
        this.groupRows(startRow + 1, 1)
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

                    this.calTotal(startRow, rowNumber, 5)
                    this.calTotal(startRow, rowNumber, 6)

                    this.groupRows(rowNumber + 1, column)
                    break
                }
            }
        }
    }

    calTotal(startRow, endRow, target) {
        const total = this.worksheet.getCell(startRow, target)
        this.worksheet.mergeCells(startRow, target, endRow, target)
        total.value = {
            formula: `SUM(INDIRECT(ADDRESS(${startRow}, COLUMN()-2) & ":" & ADDRESS(${endRow}, COLUMN()-2)))`, // Dùng dấu phẩy
        }
        total.alignment = {
            horizontal: 'center',
            vertical: 'middle',
        }
    }
}
export default {
    data() {
        return {
            excelData: {
                title: 'THỐNG KÊ SẢN LƯỢNG',
                query: [
                    {
                        header: 'Khu vực',
                        key: 'area',
                        value: '',
                    },
                    {
                        header: 'Thời gian từ',
                        key: 'from',
                        value: '',
                    },
                    {
                        header: 'Đến',
                        key: 'to',
                        value: '',
                    },
                ],
                columns: [
                    {
                        header: 'Thời gian',
                        key: 'date',
                    },
                    {
                        header: 'Khu vực',
                        key: 'cascade',
                        width: 25,
                    },
                    {
                        header: 'Lớp',
                        key: 'class',
                        width: 25,
                    },
                    {
                        header: 'Đạt',
                        key: 'success',
                        width: 15,
                    },
                    {
                        header: 'Lỗi',
                        key: 'failed',
                        width: 15,
                    },
                    {
                        header: 'Đạt',
                        key: 'totalSuccess',
                        width: 15,
                    },
                    {
                        header: 'Lỗi',
                        key: 'totalFailed',
                        width: 15,
                    },
                ],
                data: [],
            },
            listArea: [],
            SearchForm: {
                AreaId: null,
                FromDate: null,
                ToDate: null,
            },
        }
    },

    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    async created() {
        this.listArea = await lookupService.getAreasTree()
    },
    methods: {
        async exportData() {
            this.excelData.data = []
            const { data } = await useJwt.post(
                '/fireWorkEvents/fireworkEvents',
                {
                    dateFrom: this.SearchForm.FromDate,
                    dateTo: this.SearchForm.ToDate,
                    casCadeFwId: this.SearchForm.AreaId,
                }
            )
            data.data.forEach((item) => {
                this.excelData.data.push({
                    date: item.date,
                    cascade: item.cascadeName,
                    class: item.className,
                    success: item.success,
                    failed: item.failed,
                })
            })
            function getLabelById(id, tree) {
                const stack = [...tree]

                while (stack.length) {
                    const node = stack.pop()
                    if (node.id === id) {
                        return node.name
                    }
                    if (node.children && node.children.length) {
                        stack.push(...node.children)
                    }
                }

                return null // Trả về `null` nếu không tìm thấy
            }

            this.excelData.query = [
                {
                    header: 'Khu vực',
                    key: 'area',
                    value:
                        getLabelById(this.SearchForm.AreaId, this.listArea) ||
                        'Tất cả',
                },
                {
                    header: 'Thời gian từ',
                    key: 'from',
                    value: this.formatDateToDMY(this.SearchForm.FromDate),
                },
                {
                    header: 'Đến',
                    key: 'to',
                    value: this.formatDateToDMY(this.SearchForm.ToDate),
                },
            ]
            const exporter = new CustomExcelExporter(this.excelData)
            await exporter.export()
        },
        formatDate(accessTime) {
            if (!accessTime) return ''
            const dateTime = new Date(accessTime)
            return dateTime.toISOString().slice(0, 10)
        },
        formatTime(accessTime) {
            if (!accessTime) return ''
            const dateTime = new Date(accessTime)
            return dateTime.toTimeString().slice(0, 8)
        },
        formatDateToDMY(dateString) {
            const [year, month, day] = dateString.split('-')
            return `${day}/${month}/${year}`
        },
    },
}
</script>
<style scoped>
.validate-message {
    color: red;
}
</style>
