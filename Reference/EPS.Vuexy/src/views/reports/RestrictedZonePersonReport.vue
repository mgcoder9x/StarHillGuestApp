<!-- RestrictedZonePersonReport.telerik.vue -->
<template>
    <validation-observer ref="rules">
        <b-container fluid class="px-0">
            <b-card body-class="px-1 py-1">
                <b-form>
                    <!-- Hàng 1: Từ ngày - Đến ngày -->
                    <b-row>
                        <b-col>
                            <b-form-group
                                :label="$t('Report.vehicleViolationReport.SearchForm.startDate')"
                                label-cols-md="3"
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

                        <b-col>
                            <b-form-group
                                :label="$t('Report.vehicleViolationReport.SearchForm.endDate')"
                                label-cols-md="3"
                            >
                                <date-picker
                                    id="h-searchForm-dateTo"
                                    v-model="reportViewer.parameters.endDate"
                                    type="datetime"
                                    :locale="currentLocale"
                                    format="DD-MM-YYYY HH:mm:ss"
                                    value-type="YYYY-MM-DD HH:mm:ss"
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
                                :label="$t('Report.vehicleViolationReport.SearchForm.Area')"
                                label-cols-md="3"
                                label-for="areasId"
                            >
                                <Treeselect
                                    :multiple="true"
                                    placeholder=""
                                    :options="areas"
                                    :reduce="(item) => item.id"
                                    v-model="areasId"
                                    :value-consists-of="'ALL'"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                @click="onReport"
                                variant="primary"
                                class="mt-2"
                            >
                                {{
                                    $t('Report.vehicleViolationReport.button.exportReport') || 'Tra cứu'
                                }}
                            </b-button>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card>

            <!-- Viewer -->
            <telerik-report-viewer
                ref="reportViewer"
                :service-url="reportViewer.serviceUrl"
                :report-source="reportViewer.reportSource"
                :parameters="reportViewer.parameters"
                :view-mode="reportViewer.viewMode"
                :scale-mode="reportViewer.scaleMode"
                :scale="reportViewer.scale"
            />
        </b-container>
    </validation-observer>
</template>

<script>
import TreeHelper from '@/utils/treeHelper'
import Treeselect from '@riophae/vue-treeselect'
import moment from 'moment'

export default {
    name: 'RestrictedZonePersonReport',
    components: { Treeselect },
    data() {
        const lang = this.$i18n?.locale || 'vi'
        return {
            areas: [],
            areasNotTree: [],
            areasId: [],
            reportViewer: {
                serviceUrl: `${process.env.VUE_APP_BASE_URL}/api/reports`,
                reportSource: 'ReportPeopleWarning.trdp',
                parameters: {
                    startDate: moment()
                        .subtract(1, 'day')
                        .format('YYYY-MM-DD 00:00:00'),
                    endDate: moment().format('YYYY-MM-DD 23:59:59'),
                    compId: null,
                    areasId: null, // '1,2,3'
                    dedupSeconds: 30,
                    areaName: '',
                    lang: lang,
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
    },
    async created() {
        const accessToken = this.$services.getUserData()
        this.reportViewer.parameters.compId = accessToken.companyId
        await this.loadAreas()
        await this.loadAreasNotTree()
    },
    watch: {
        '$i18n.locale'(newLang) {
            this.reportViewer.parameters.lang = newLang
            this.onParameterChange()
        },
    },
    methods: {
        async loadAreas() {
            const res = await this.$services.get('/lookup/areas-tree')
            this.areas = TreeHelper.removeEmptyChildren(res.data.data)
        },
        async loadAreasNotTree() {
            const res = await this.$services.get('/lookup/areas')
            this.areasNotTree = res.data.data
        },
        onParameterChange() {
            if (this.$refs.reportViewer?.hideReport)
                this.$refs.reportViewer.hideReport()
        },
        onReport() {
            this.$refs.rules.validate().then((ok) => {
                if (!ok) return
                // map areaIds
                this.reportViewer.parameters.areasId = this.areasId?.length
                    ? this.areasId.join(',')
                    : null

                // areaName cho header
                if (this.areasId?.length) {
                    const picked = this.areasNotTree.filter((x) =>
                        this.areasId.map(String).includes(String(x.id))
                    )
                    this.reportViewer.parameters.areaName = picked.length
                        ? picked.map((x) => x.text).join(',')
                        : ''
                } else {
                    this.reportViewer.parameters.areaName =
                        this.$t('Export.All') || 'Tất cả'
                }

                this.$refs.reportViewer.refresh()
            })
        },
        // Export XLSX qua viewer
        // onExportExcel() {
        //     if (this.$refs.reportViewer?.exportReport) {
        //         this.$refs.reportViewer.exportReport('XLSX')
        //     } else {
        //         // fallback: reload rồi export (tùy wrapper viewer của bạn)
        //         this.$refs.reportViewer.refresh(() =>
        //             this.$refs.reportViewer.exportReport('XLSX')
        //         )
        //     }
        // },
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
