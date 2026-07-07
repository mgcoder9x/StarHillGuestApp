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
                                <!-- <b-form-datepicker
                                reset-button
                                v-model="reportViewer.parameters.startDate"
                                @input="onParameterChange"
                            /> -->
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
                                        format="DD-MM-YYYY"
                                        value-type="YYYY-MM-DD"
                                        style="width: 100%"
                                        reset-button
                                        @input="onParameterChange"
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
                                <!-- <b-form-datepicker
                                v-model="reportViewer.parameters.endDate"
                                reset-button
                                @input="onParameterChange"
                            /> -->
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="EndDate"
                                >
                                    <date-picker
                                        id="h-searchForm-dateFrom"
                                        v-model="
                                            reportViewer.parameters.endDate
                                        "
                                        type="datetime"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY"
                                        value-type="YYYY-MM-DD"
                                        style="width: 100%"
                                        reset-button
                                        @input="onParameterChange"
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
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    this.$t(
                                        'VehicleEvent.Field.Direction'
                                    )
                                "
                                label-for="color"
                                :label-cols="3"
                                :horizontal="true"
                                label-align-md="left"
                            >
                                <v-select
                                    v-model="reportViewer.parameters.direction"
                                    label="text"
                                    :reduce="(item) => item.id"
                                    :options="listDirection"
                                    placeholder=""
                                ></v-select>
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
            areasSelected: [],
            areasId: null,
            colorId: null,
            reportViewer: {
                serviceUrl: `${process.env.VUE_APP_BASE_URL}/api/reports`,
                reportSource: 'ReportGoodsStatisticsByPort.trdp',
                parameters: {
                    startDate: null,
                    endDate: null,
                    compId: null,
                    areaIds: null,
                    direction: null,
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
        listDepType() {
            return [
                {
                    id: 1,
                    text: this.$t('VehicleEvent.Common.DeptType.Car'),
                },
                {
                    id: 2,
                    text: this.$t('VehicleEvent.Common.DeptType.Contractor'),
                },
            ]
        },
        listDirection(){
            return[
                { id: 1, text: this.$t('CarEvent.Common.WarningType.In') },
                { id: 2, text: this.$t('CarEvent.Common.WarningType.Out') },
            ]
        },

        listVehicleType() {
            return [
                {
                    id: 1,
                    text: this.$t('VehicleEvent.Common.VehicleType.Moto'),
                },
                { id: 2, text: this.$t('VehicleEvent.Common.VehicleType.Car') },
            ]
        },
    },
    watch: {
        '$i18n.locale'(newLang) {
            this.reportViewer.parameters.lang = newLang
            this.onReport() // hoặc gọi refresh trực tiếp nếu đã có report
        },
    },
    async created() {
        const accessToken = this.$services.getUserData()
        this.reportViewer.parameters.compId = accessToken.companyId
        this.reportViewer.parameters.startDate = moment().format(
            'YYYY-MM-DD'
        )
        await this.loadAreas()
    },
    methods: {
        async loadAreas() {
            const res = await this.$services.get('/lookup/areas-tree')
            this.areas = TreeHelper.removeEmptyChildren(res.data.data)
        },
        onReport() {
            debugger
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    debugger
                    this.reportViewer.parameters.lang = this.$i18n.locale
                    this.reportViewer.parameters.areaIds = this.areasId
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

                    // Cảnh báo (warning level)
                    this.reportViewer.parameters.warningLevels = this.warningLevels
                        ? this.warningLevels.join(',')
                        : ''

                    // ⚙️ Thêm tham số direction (1 = vào, 2 = ra)
                    let d = Number(this.reportViewer.parameters.direction);
                    this.reportViewer.parameters.direction = (d === 1 || d === 2) ? d : null;

                    // Refresh the Telerik report viewer
                    this.$refs.reportViewer.refresh()
                }
            })
        },
        onParameterChange() {
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
