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
                                label-for="startDate"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="`required|fromDate:${reportViewer.parameters.end_date}`"
                                    name="StartDate"
                                >
                                    <date-picker
                                        id="h-searchForm-dateFrom"
                                        v-model="
                                            reportViewer.parameters.start_date
                                        "
                                        type="datetime"
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
                                label-for="endDate"
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
                                            reportViewer.parameters.end_date
                                        "
                                        type="datetime"
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
            areasId: null,
            devices: [],
            reportViewer: {
                serviceUrl: `${process.env.VUE_APP_BASE_URL}/api/reports`,
                reportSource: 'InOutStaticReport.trdp', // File báo cáo mới
                parameters: {
                    start_date: null,
                    end_date: null,
                    comp_id: null,
                    area_id: null,
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
        '$i18n.locale'(newLang) {
            this.reportViewer.parameters.lang = newLang
            this.onReport()
        },
    },
    async created() {
        const accessToken = this.$services.getUserData()
        this.reportViewer.parameters.comp_id = accessToken.companyId
        // this.reportViewer.parameters.start_date = moment().format('YYYY-MM-DD')
        await this.loadAreas()
    },
    methods: {
        async loadAreas() {
            const res = await this.$services.get('/lookup/areas-tree')
            this.areas = TreeHelper.removeEmptyChildren(res.data.data)
        },
        
        onReport() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.reportViewer.parameters.lang = this.$i18n.locale
                    this.reportViewer.parameters.area_id = this.areasId
                        ? this.areasId.join(',')
                        : null
                    if (this.areasId != null) {
                        const areaSelect = this.areas.filter((x) =>
                            this.areasId.includes(x.id)
                        )
                        this.reportViewer.parameters.areaName =
                            areaSelect.length > 0
                                ? areaSelect.map((x) => x.label).join(',')
                                : ''
                    } else {
                        this.reportViewer.parameters.areaName = this.$t(
                            'Report.vehicleViolationReport.SearchForm.All'
                        )
                    }
                    // this.reportViewer.parameters.licenseplates =
                    //     this.reportViewer.parameters.licenseplates || null
                    // this.reportViewer.parameters.deviceId =
                    //     this.reportViewer.parameters.deviceId || null
                    // this.reportViewer.parameters.licensePlatesText = this
                    //     .reportViewer.parameters.licensePlates
                    //     ? this.reportViewer.parameters.licensePlates
                    //     : this.$t(
                    //           'Report.vehicleViolationReport.SearchForm.All'
                    //       )
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

<style scoped>
.reportViewer {
    position: relative;
    width: 100%;
    height: 70vh;
}
</style>
