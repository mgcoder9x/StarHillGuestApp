<!-- VehicleInOutReport.telerik.vue -->
<template>
    <validation-observer ref="rules">
        <b-container fluid class="px-0">
            <b-card body-class="px-1 py-1">
                <b-form>
                    <!-- Hàng 1: Từ ngày - Đến ngày -->
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('VehicleInPlant.Field.FromDate')"
                                label-cols-md="2"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="`required|fromDate:${reportViewer.parameters.endDate}`"
                                    name="StartDate"
                                >
                                    <date-picker
                                        id="h-searchForm-dateFrom"
                                        v-model="
                                            reportViewer.parameters.startDate
                                        "
                                        type="datetime"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY HH:mm:ss"
                                        value-type="YYYY-MM-DD HH:mm:ss"
                                        style="width: 100%"
                                        @input="onParameterChange"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <b-col md="6">
                            <b-form-group
                                :label="$t('VehicleInPlant.Field.ToDate')"
                                label-cols-md="2"
                            >
                                <date-picker
                                    id="h-searchForm-dateTo"
                                    v-model="reportViewer.parameters.endDate"
                                    type="datetime"
                                    :locale="currentLocale"
                                    format="DD-MM-YYYY HH:mm:ss"
                                    value-type="YYYY-MM-DD"
                                    style="width: 100%"
                                    @input="onParameterChange"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <!-- Hàng 2: Khu vực -->
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="areaLabel"
                                label-for="areasId"
                                label-cols-md="2"
                            >
                                <Treeselect
                                    v-model="areasId"
                                    :multiple="true"
                                    placeholder=""
                                    :options="areas"
                                    :reduce="(item) => item.id"
                                    :value-consists-of="'ALL'"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-row>
                                <b-col md="6">
                                    <b-form-group
                                        :label="$t('countVehicle.Search.Type')"
                                        label-cols-md="4"
                                    >
                                        <b-form-select
                                            v-model="
                                                reportViewer.parameters
                                                    .officerFilter
                                            "
                                            :options="officerFilterOptions"
                                            @change="onParameterChange"
                                        />
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="$t('countVehicle.Search.Group')"
                                        label-cols-md="3"
                                    >
                                        <b-form-select
                                            v-model="
                                                reportViewer.parameters
                                                    .identityGroup
                                            "
                                            :options="identityGroupOptions"
                                            @change="onParameterChange"
                                        />
                                    </b-form-group>
                                </b-col>
                            </b-row>
                        </b-col>
                    </b-row>

                    <!-- Hàng 3: Phân loại + Nhóm xe (CÙNG 1 HÀNG) -->
                    <!-- <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('countVehicle.Search.Group')"
                                label-cols-md="3"
                            >
                                <b-form-select
                                    v-model="
                                        reportViewer.parameters.identityGroup
                                    "
                                    :options="identityGroupOptions"
                                    @change="onParameterChange"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row> -->

                    <!-- Nút -->
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                variant="primary"
                                class="mt-2"
                                @click="onReport"
                            >
                                {{
                                    currentLocale === 'vi'
                                        ? 'Tra cứu'
                                        : $t(
                                              'Report.vehicleViolationReport.button.exportReport'
                                          ) || 'Search'
                                }}
                            </b-button>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card>

            <telerik-report-viewer
                ref="reportViewer"
                :report-source="reportViewer.reportSource"
                :parameters="reportViewer.parameters"
            />
        </b-container>
    </validation-observer>
</template>

<script>
import TreeHelper from '@/utils/treeHelper'
import Treeselect from '@riophae/vue-treeselect'
import moment from 'moment'

export default {
    name: 'VehicleInOutReportTelerik',
    components: { Treeselect },
    data() {
        // const lang = this.$i18n?.locale || 'vi'
        return {
            areas: [],
            areasNotTree: [],
            areasId: [],
            reportViewer: {
                serviceUrl: `${process.env.VUE_APP_BASE_URL}/api/reports`,
                reportSource: 'ReportVehicleCount.trdp',
                parameters: {
                    // mặc định: hôm qua 00:00:00
                    startDate: moment()
                        .subtract(1, 'day')
                        .startOf('day')
                        .format('YYYY-MM-DD HH:mm:ss'),
                    endDate: null,
                    compId: null,
                    p_area_ids: null,
                    lang: this.$i18n?.locale || 'vi',
                    officerFilter: null, // 'officer' | 'nonofficer' | null
                    identityGroup: null, // 'identified' | 'unknown' | null
                    dedupSeconds: 30, // vẫn gửi nếu backend dùng, nhưng không hiện trên UI
                    // areaName: '',
                    officerFilterText: null,
                    identityGroupText: null,
                },
                scaleMode: 'FIT_PAGE_WIDTH',
                viewMode: 'INTERACTIVE',
                scale: 1.0,
            },
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n?.locale || 'vi'
        },
        allText() {
            return this.currentLocale === 'vi' ? 'Tất cả' : 'All'
        },
        areaLabel() {
            // fallback nếu thiếu key i18n
            return (
                this.$t('Report.vehicleViolationReport.SearchForm.Area') ||
                (this.currentLocale === 'vi' ? 'Khu vực' : 'Area')
            )
        },
        // Tạo options theo ngôn ngữ => đổi ngay khi chuyển locale
        officerFilterOptions() {
            return [
                { value: null, text: this.allText },
                {
                    value: 'officer',
                    text:
                        this.currentLocale === 'vi'
                            ? 'Xe cán bộ chiến sĩ'
                            : 'Officer vehicles',
                },
                {
                    value: 'nonofficer',
                    text:
                        this.currentLocale === 'vi'
                            ? 'Không phải xe cán bộ'
                            : 'Non-officer vehicles',
                },
            ]
        },
        identityGroupOptions() {
            return [
                { value: null, text: this.allText },
                {
                    value: 'identified',
                    text:
                        this.currentLocale === 'vi'
                            ? 'Định danh (có biển số)'
                            : 'Identified (has plate)',
                },
                {
                    value: 'unknown',
                    text:
                        this.currentLocale === 'vi'
                            ? 'Không xác định (Unknown)'
                            : 'Unknown',
                },
            ]
        },
    },
    watch: {
        '$i18n.locale': function (newLang) {
            this.reportViewer.parameters.lang = newLang
            // thay đổi options/label sẽ phản chiếu qua computed;
            // ẩn report để user bấm Tra cứu sau khi chỉnh
            // this.onReport()
            this.onParameterChange()
        },
        // khi đổi khu vực thì cập nhật tên hiển thị trong header report
        areasId() {
            this.applyAreaName()
        },
    },
    async created() {
        const accessToken = this.$services.getUserData()
        this.reportViewer.parameters.compId = accessToken.companyId
        await this.loadAreas()
        await this.loadAreasNotTree()
    },
    methods: {
        async loadAreas() {
            const res = await this.$services.get('/lookup/areas-tree')
            console.log(res)
            this.areas = TreeHelper.removeEmptyChildren(res.data.data)
            console.log(this.areas)
        },
        async loadAreasNotTree() {
            const res = await this.$services.get('/lookup/areas')
            this.areasNotTree = res.data.data
        },
        onParameterChange() {
            if (this.$refs.reportViewer && this.$refs.reportViewer.hideReport) {
                this.$refs.reportViewer.hideReport()
            }
        },
        applyAreaName() {
            if (this.areasId && this.areasId.length) {
                const picked = this.areasNotTree.filter((x) =>
                    this.areasId.map(String).includes(String(x.id))
                )
                this.reportViewer.parameters.areaName = picked.length
                    ? picked.map((x) => x.text).join(',')
                    : ''
            } else {
                this.reportViewer.parameters.areaName = this.allText
            }
        },
        onReport() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    debugger
                    this.reportViewer.parameters.lang = this.$i18n.locale
                    this.reportViewer.parameters.p_area_ids = this.areasId
                        ? this.areasId.join(',')
                        : null

                    // Lấy areaName
                    if (this.areasId != null && this.areasId.length > 0) {
                        const areaSelect = this.areasNotTree.filter((x) =>
                            this.areasId.map(String).includes(x.id.toString())
                        )
                        this.reportViewer.parameters.areaName =
                            areaSelect.length > 0
                                ? areaSelect.map((x) => x.text).join(',')
                                : ''
                    } else {
                        this.reportViewer.parameters.areaName =
                            this.$t('Export.All')
                    }

                    // Lấy officerFilterText
                    const officerFilterOption = this.officerFilterOptions.find(
                        (opt) =>
                            opt.value ===
                            this.reportViewer.parameters.officerFilter
                    )
                    this.reportViewer.parameters.officerFilterText =
                        officerFilterOption
                            ? officerFilterOption.text
                            : this.$t('Export.All')

                    // Lấy identityGroupText
                    const identityGroupOption = this.identityGroupOptions.find(
                        (opt) =>
                            opt.value ===
                            this.reportViewer.parameters.identityGroup
                    )
                    this.reportViewer.parameters.identityGroupText =
                        identityGroupOption
                            ? identityGroupOption.text
                            : this.$t('Export.All')

                    this.$refs.reportViewer.refresh()
                }
            })
        },
    },
}
</script>

<style scoped>
.reportViewer {
    position: relative;
    width: 100%;
    height: 70vh;
}
</style>
