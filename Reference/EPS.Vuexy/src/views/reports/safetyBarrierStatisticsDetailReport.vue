<template>
    <validation-observer ref="rules">
        <b-container fluid class="px-0">
            <b-card body-class="px-1 py-1">
                <b-form>
                    <b-row>
                        <b-col>
                            <b-form-group
                                :label="
                                    $t(
                                        'Report.vehicleViolationReport.SearchForm.startDate'
                                    )
                                "
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
                                        type="date"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY"
                                        value-type="YYYY-MM-DD"
                                        style="width: 100%"
                                        reset-button
                                        @change="onParameterChange"
                                    ></date-picker>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col>
                            <b-form-group
                                :label="
                                    $t(
                                        'Report.vehicleViolationReport.SearchForm.endDate'
                                    )
                                "
                                label-cols-md="3"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="EndDate"
                                >
                                    <date-picker
                                        id="h-searchForm-dateTo"
                                        v-model="
                                            reportViewer.parameters.endDate
                                        "
                                        type="date"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY"
                                        value-type="YYYY-MM-DD"
                                        style="width: 100%"
                                        reset-button
                                        @change="onParameterChange"
                                    ></date-picker>

                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'Report.vehicleViolationReport.SearchForm.Area'
                                    )
                                "
                                label-for="areasId"
                                label-cols-md="3"
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
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                variant="primary"
                                class="mt-2"
                                @click="onReport"
                            >
                                {{
                                    $t(
                                        'Report.vehicleViolationReport.button.exportReport'
                                    )
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
    components: {
        Treeselect,
    },
    data() {
        return {
            areas: [],
            areasSelected: [],
            areasId: [],
            reportViewer: {
                serviceUrl: `${process.env.VUE_APP_BASE_URL}/api/reports`,
                reportSource: 'SafetyBarrierStatisticsDetailReport.trdp',
                parameters: {
                    startDate: null,
                    endDate: null,
                    compId: null,
                    areaIds: null,
                    lang: this.$i18n.locale,
                },
                scaleMode: 'FIT_PAGE_WIDTH',
                viewMode: 'INTERACTIVE',
                scale: 1.0,
                show: false,
            },
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    watch: {
        '$i18n.locale': function (newLang) {
            this.reportViewer.parameters.lang = newLang
            this.onReport() // hoặc gọi refresh trực tiếp nếu đã có report
        },
    },
    async created() {
        const accessToken = this.$services.getUserData()
        this.reportViewer.parameters.compId = accessToken.companyId
        this.reportViewer.parameters.startDate = moment().format('YYYY-MM-DD')
        // this.reportViewer.parameters.startDate = moment().startOf('week').add(1, 'days').format('YYYY-MM-DD')
        // this.reportViewer.parameters.endDate = moment().format('YYYY-MM-DD')
        await this.loadAreas()
        this.loadAreasNotTree()
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
        onReport() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.reportViewer.parameters.lang = this.$i18n.locale
                    this.reportViewer.parameters.areaIds = this.areasId
                        ? this.areasId.join(',')
                        : null
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
                    this.$refs.reportViewer.refresh()
                }
            })
        },
        async onParameterChange() {
            await this.$refs.rules.validate()
            this.$refs.reportViewer.hideReport()
        },
    },
}
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
.reportViewer {
    position: relative;
    width: 100%;
    height: 70vh;
}
</style>
