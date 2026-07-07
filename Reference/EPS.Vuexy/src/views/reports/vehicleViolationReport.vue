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
                                <!-- Validate: start_date <= end_date -->
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="
                                        `required|fromDate:` +
                                        (reportViewer.parameters.endDate || '')
                                    "
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
                                    />
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
                                        'Report.vehicleViolationReport.SearchForm.Area'
                                    )
                                "
                                label-for="areasId"
                                label-cols-md="3"
                            >
                                <Treeselect
                                    id="areasId"
                                    :multiple="true"
                                    placeholder=""
                                    :options="areas"
                                    :reduce="(item) => item.id"
                                    v-model="areasId"
                                    :value-consists-of="'ALL'"
                                    @input="onUpdateAreas"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <b-row>
                        <b-col md="6">
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
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
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
import { ValidationObserver, ValidationProvider, extend } from 'vee-validate'

// Rule: start_date <= end_date
// extend('fromDate', {
//     params: ['to'],
//     message: 'Thời gian từ ngày phải nhỏ hơn thời gian đến ngày',
//     validate(value, { to }) {
//         if (!value || !to) return true
//         const f = moment(value, 'YYYY-MM-DD', true)
//         const t = moment(to, 'YYYY-MM-DD', true)
//         if (!f.isValid() || !t.isValid()) return true
//         return f.isSameOrBefore(t)
//     },
// })

export default {
    components: {
        Treeselect,
        ValidationObserver,
        ValidationProvider,
    },
    data() {
        return {
            areas: [],
            areasSelected: [],
            areasId: [], // mảng rỗng mặc định
            reportViewer: {
                serviceUrl: `${process.env.VUE_APP_BASE_URL}/api/reports`,
                reportSource: 'vehicleViolationReport.trdp',
                parameters: {
                    startDate: null,
                    endDate: null,
                    compId: null,
                    areasId: null,
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
        this.reportViewer.parameters.compId = 5 // hoặc accessToken.companyId
        this.reportViewer.parameters.startDate = moment().format('YYYY-MM-DD')
        await this.loadAreas()
    },
    methods: {
        async loadAreas() {
            const res = await this.$services.get('/lookup/areas-tree')
            this.areas = TreeHelper.removeEmptyChildren(res.data.data)
        },
        async onReport() {
            const ok = await this.$refs.rules.validate()
            if (!ok) return

            // đồng bộ tham số
            this.reportViewer.parameters.lang = this.$i18n.locale
            this.reportViewer.parameters.areasId =
                Array.isArray(this.areasId) && this.areasId.length
                    ? this.areasId.join(',')
                    : null

            this.$refs.reportViewer.refresh()
        },
        async onParameterChange() {
            await this.$refs.rules.validate()
            this.$refs.reportViewer.hideReport()
        },
        onUpdateAreas() {
            // có thể xử lý thêm nếu cần; hiện tại chỉ để đồng bộ khi export
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
