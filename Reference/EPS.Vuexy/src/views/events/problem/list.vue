<template>
    <b-container fluid class="p-0">
        <b-card body-class="px-1 py-0 pt-1">
            <validation-observer ref="rules">
                <b-form>
                    <b-row>
                        <b-col md="4">
                            <b-form-group
                                label="Mã/tên"
                                label-for="h-searchForm-code-name"
                                label-cols-md="3"
                            >
                                <b-form-input
                                    id="h-searchForm-text"
                                    v-model="searchForm.filterText"
                                    type="text"
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="4">
                            <b-form-group
                                :label="$t('Nhà thầu')"
                                label-for="h-searchForm-device"
                                label-cols-md="3"
                            >
                                <tree-select
                                    v-model="searchForm.filterContractor"
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    label="text"
                                    :reduce="(dept) => dept.id"
                                    :multiple="true"
                                    :options="listContractor"
                                    :value-consists-of="'ALL'"
                                    placeholder=""
                                    :limit="3"
                                    :limit-text="(count) => `+${count}`"
                                    class="treeselect-nowrap"
                                    value-consists-of="ALL"
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                        <!-- area -->
                        <b-col md="4">
                            <b-form-group label="Khu vực" label-cols-md="3">
                                <Treeselect
                                    v-model="searchForm.filterArea"
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    label="text"
                                    :reduce="(lstArea) => lstArea.id"
                                    :options="listAreas"
                                    multiple
                                    placeholder=""
                                    :limit="3"
                                    :limit-text="(count) => `+${count}`"
                                    class="treeselect-nowrap"
                                    value-consists-of="ALL"
                                    @input="search"
                                ></Treeselect>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <!-- dateFrom -->
                        <b-col md="4">
                            <b-form-group
                                label="Thời gian từ"
                                label-cols-md="3"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="`fromDate:` + searchForm.dateTo"
                                    name="NotificationEventTemplateName"
                                >
                                    <date-picker
                                        id="h-searchForm-dateFrom"
                                        v-model="searchForm.dateFrom"
                                        type="datetime"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY HH:mm:ss"
                                        value-type="YYYY-MM-DD HH:mm:ss"
                                        style="width: 100%"
                                        class="custom-date-picker"
                                        @change="search"
                                    ></date-picker>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <!-- dateTo -->
                        <b-col md="4">
                            <b-form-group label="Đến" label-cols-md="3">
                                <date-picker
                                    id="h-searchForm-dateTo"
                                    v-model="searchForm.dateTo"
                                    type="datetime"
                                    :locale="currentLocale"
                                    format="DD-MM-YYYY HH:mm:ss"
                                    value-type="YYYY-MM-DD HH:mm:ss"
                                    style="width: 100%"
                                    @change="search"
                                ></date-picker>
                            </b-form-group>
                        </b-col>
                        <b-col md="4">
                            <b-form-group
                                label="Phân loại"
                                label-for="h-searchForm-warning"
                                label-cols-md="3"
                            >
                                <v-select
                                    v-model="searchForm.filterWarningLevel"
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    label="text"
                                    :reduce="(i) => i.id"
                                    :options="listWarningLevelId"
                                    multiple
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                </b-form>
            </validation-observer>
        </b-card>
        <b-card title="" body-class="p-1">
            <div class="text-left mb-1 d-flex" style="gap: 8px">
                <b-button
                    v-waves
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="$t('ProtectiveEquipmentEvent.Excel.Header')"
                        :fetch="exportData"
                        type="xlsx"
                        :fields="export_fields_vi"
                    >
                        <div class="d-flex align-items-center">
                            <Icon
                                icon="famicons:enter-outline"
                                class="sm-icon"
                            />
                            <span class="ml-25">{{
                                $t('Button.ExportExcel')
                            }}</span>
                        </div>
                    </downloadExcel>
                </b-button>

                <b-button
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                    @click="refresh()"
                >
                    <Icon icon="famicons:enter-outline" class="sm-icon" />
                    <span class="ml-25">{{ $t('Button.Refresh') }}</span>
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="problemTable"
                :columns="columns"
                data-url="/problem"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="problemTableConfig"
            >
                <template #table-row="props">
                    <!-- <span v-if="props.column.field == 'stt'">
                        {{ props.row.stt }}
                    </span> -->
                    <span v-if="props.column.field == 'accessTime'">
                        {{ getDate(props.row.accessTime) }}
                    </span>
                    <span v-else-if="props.column.field == 'eventTypeId'">
                        {{
                            getDynamicName(props.row.eventTypeId, listEventType)
                        }}
                    </span>
                    <span v-else-if="props.column.field == 'area'">
                        {{ getDynamicName(props.row.areaId, lstArea) }}
                    </span>
                    <!-- Warning -->
                    <span
                        v-else-if="props.column.field === 'warningLevelId'"
                        :style="{ display: 'flex', color: props.row.color }"
                    >
                        {{
                            formatWarningLevel(props.row.warningLevelId) ||
                            $t('ProtectiveEquipmentType.Unknown')
                        }}
                    </span>

                    <!-- Image -->
                    <span
                        v-else-if="props.column.field === 'image'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        <img
                            :src="`${baseURL}${props.row.image}`"
                            alt="Image"
                            width="100"
                            style="object-fit: cover; border-radius: 0.3em"
                            class="cursor-pointer"
                            loading="lazy"
                            @click="showModalEvent(props.row.eventId)"
                        />
                    </span>

                    <!-- Actions -->
                    <span v-else-if="props.column.field === 'action'">
                        <div class="center-icon text-nowrap">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/reports/problem/detail/${props.row.problemId}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                @click="onReport(props.row.problemId)"
                            >
                                <Icon icon="carbon:report" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Delete')"
                                @click="doDelete(props.row.problemId)"
                            >
                                <Icon
                                    icon="fluent:delete-20-regular"
                                    class="xs-icon"
                                />
                            </b-button>
                        </div>
                    </span>
                </template>
            </BasicTable>
        </b-card>
        <b-modal
            v-model="showReportModal"
            size="xl"
            :title="$t('Problem.Report.Title')"
        >
            <telerik-report-viewer
                ref="reportViewer"
                :report-source="reportViewer.reportSource"
                :parameters="reportViewer.parameters"
                class="reportViewer"
            />
            <template #modal-footer>
                <b-button variant="primary" @click="showReportModal = false">
                    {{ $t('common.button.close') }}
                </b-button>
            </template>
        </b-modal>
        <!-- <b-modal
            v-model="modalImgEventShow"
            :title="$t('fireEvent.common.media')"
            size="lg"
        >
            <b-carousel id="carousel-example-generic" indicators controls>
                <b-carousel-slide
                    v-for="item in listEventFiles"
                    :key="item.id"
                    :img-src="`${baseURL}${item.filePath}`"
                />
            </b-carousel>
            <template #modal-footer>
                <p></p>
            </template>
        </b-modal> -->
        <b-modal
            v-model="modalImgEventShow"
            :title="$t('Events.SearchForm.EventPhotos')"
            size="lg"
            @hidden="clearEventFiles"
        >
            <EventCarousel
                :list-event-files="listEventFiles"
                :base-u-r-l="baseURL"
            />
            
            <template #modal-footer><p /></template>
        </b-modal>
    </b-container>
</template>

<script>
/* eslint-disable */
import { $themeConfig } from '@themeConfig'
import { lookupService } from '@/services'
import { onMounted, ref } from '@vue/composition-api'
import moment from 'moment'
import Treeselect from '@riophae/vue-treeselect'
import TreeHelper from '@/utils/treeHelper'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

const isDevEnv = process.env.NODE_ENV === 'development'

export default {
    name: 'FireEventList',
    components: {
        Treeselect,
    },
    setup() {
        const { apiURL } = $themeConfig.app
        const lstArea = ref([])
        const lstContractor = ref([])
        onMounted(async () => {
            lstArea.value = await lookupService.getAreas()
            lstContractor.value = await lookupService.getContractor()
        })
        return {
            apiURL,
            lstArea,
            lstContractor
        }
    },
    data() {
        return {
            searchForm: {
                dateFrom: null,
                dateTo: null,
                areaId: null,
                deviceId: null,
                fireType: null,
                fireLevel: null,
            },
            headerExcelDetail: [],
            export_fields_vi: {
                'Thời gian': 'accessTime',
                'Mã/CCCD': 'personCode',
                'Họ tên': 'personName',
                'Nhà thầu': 'contractorName',
                'Lỗi vi phạm': 'eventTypeId',
                'Phân loại': 'warningLevelId',
                'Khu vực': 'areaName',
                'Người xử lý': 'handleName',
            },
            columns: [
                {
                    label: 'Thời gian',
                    field: 'accessTime',
                },
                {
                    label: 'Mã/CCCD',
                    field: 'personCode',
                },
                {
                    label: 'Họ tên',
                    field: 'personName',
                },
                {
                    label: 'Nhà thầu',
                    field: 'contractorName',
                },
                {
                    label: 'Lỗi vi phạm',
                    field: 'eventTypeId',
                },
                {
                    label: 'Phân loại',
                    field: 'warningLevelId',
                },
                {
                    label: 'Khu vực',
                    field: 'area',
                },
                {
                    label: 'Người xử lý',
                    field: 'handleName',
                },
                {
                    label: 'Hình ảnh',
                    field: 'image',
                },
                {
                    label: 'Thao tác',
                    field: 'action',
                },
            ],
            showReportModal: false,
            reportViewer: {
                serviceUrl: `${process.env.VUE_APP_BASE_URL}/api/reports`,
                reportSource: 'ReportProblemDetail.trdp',
                parameters: {
                    pid:null,
                    lang: this.$i18n.locale,
                },
                scaleMode: 'FIT_PAGE_WIDTH',
                viewMode: 'INTERACTIVE',
                scale: 1.0,
                show: false,
            },
            modalImgEventShow: false,
            listContractor: [],
            listAreas: [],
            listEventType: [],
            listWarningLevelId: [],
            listEventFiles: [],
        }
    },
    computed: {
        baseURL() {
            return process.env.VUE_APP_BASE_URL
            //return 'http://192.168.1.254:42007/Service'
        },
        currentLocale() {
            return this.$i18n.locale
        },
        warningLevelOptions() {
            return this.options.warningLevels.map((opt) => ({
                ...opt,
                text: this.$t(
                    `Events.ProtectiveEquipmentEvent.WarningLevels.${opt.id}`
                ),
            }))
        },
    },
    async created() {
        const searchForm = getStorage('problemSearchForm')
        if (searchForm) {
            this.searchForm = { ...searchForm }
        } else {
            this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
        }
        await this.loadAreasTree()
        this.featEventTypes()
        this.featWarningLevels()
        this.loadContractor()
        this.search()
    },
    watch: {
    },
    methods: {
        clearEventFiles() {
            this.listEventFiles = []
        },
        refresh() {
            clearStorage('problemSearchForm')
            this.searchForm = {
                eventTypeId: null,
                warningLevelId: null,
                dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                dateTo: null,
                areaId: [],
                deviceId: [],
                isSafe: false,
            }
            this.$nextTick(() =>
                this.$refs.problemTable.refresh()
            )
        },
        featWarningLevels() {
            this.$services.get(`/lookup/warning-levels`).then((res) => {
                this.listWarningLevelId = res.data.data
            })
        },
        onReport(id) {
            this.reportViewer.parameters.pid = id
            this.reportViewer.parameters.lang = this.$i18n.locale
            this.showReportModal = true

            // Refresh report after modal opens
            this.$nextTick(() => {
                if (this.$refs.reportViewer) {
                    this.$refs.reportViewer.refresh()
                }
            })
        },
        loadContractor() {
            this.$services.get('/lookup/departments-tree?type=2').then((response) => {
                const cont = TreeHelper.removeEmptyChildren(response.data.data)
                this.listContractor = cont.map((item) => {
                    const { text, ...rest } = item
                    return { label: text, ...rest }
                })
            })
        },
        formatWarningLevel(warningLevelId) {
            return this.$t(
                `Events.ProtectiveEquipmentEvent.WarningLevels.${warningLevelId}`
            )
        },
        featEventTypes() {
            this.$services.get(`/lookup/eventType?equipmentOnly=true`).then((res) => {
                this.listEventType = res.data.data
            })
        },
        search() {
            setStorage('problemSearchForm', this.searchForm, 120)
            this.$refs.problemTable.refresh()
        },
        async exportData() {
            debugger
            let vm = this
            let areaName
            if (this.searchForm.filterArea != null) {
                areaName = this.searchForm.filterArea
                    .map((area) => {
                        return this.getDynamicName(area, this.lstArea)
                    })
                    .join(', ')
            } else {
                areaName = this.$t('Export.All')
            }

            let contractorName
            if (this.searchForm.filterContractor != null) {
                contractorName = this.searchForm.filterContractor
                    .map((con) => {
                        return this.getDynamicName(con, this.lstContractor)
                    })
                    .join(', ')
            } else {
                contractorName = this.$t('Export.All')
            }

            let warningName
            if (this.searchForm.filterWarningLevel != null) {
                warningName = this.searchForm.filterWarningLevel
                    .map((war) => {
                        return this.getDynamicName(war, this.listWarningLevelId)
                    })
                    .join(', ')
            } else {
                warningName = this.$t('Export.All')
            }


            let dateFrom = moment(this.searchForm.dateFrom).format(
                'DD/MM/YYYY HH:mm'
            )
            let dateTo = moment(this.searchForm.dateTo).format(
                'DD/MM/YYYY HH:mm'
            )
            if (dateFrom == 'Invalid date') {
                dateFrom = moment('2020-01-01 00:00:00').format(
                    'DD/MM/YYYY HH:mm'
                )
            }
            if (dateTo == 'Invalid date') {
                dateTo = moment().format('DD/MM/YYYY HH:mm')
            }

            let text;
            if (this.searchForm.filterText != null && this.searchForm.filterText != '') {
                text = this.searchForm.filterText
            }
            else {
                text = this.$t('Export.All')
            }

            vm.headerExcelDetail = [
                vm.$t('Quản lý lỗi an toàn'),
                `${vm.$t('Thời gian từ')}: ${dateFrom}    ${vm.$t('đến')}: ${dateTo}`,
                `${vm.$t('Nhà thầu')}: ${contractorName}`,
                `${vm.$t('Khu vực')}: ${areaName}`,
                `${vm.$t('Phân loại')}: ${warningName}`,
                `${vm.$t('Mã/tên')}: ${text}`,
            ]
            const pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'accessTime',
                sortDesc: true,
            }
            let formData =
                new URLSearchParams(pagination).toString() +
                '&' +
                new URLSearchParams(vm.searchForm).toString()
            let response = await this.$services.get(
                vm.$refs.problemTable.dataUrl + '?' + formData
            )
            for (const item of response.data.data.data) {
                item.accessTime = this.getDate(item.accessTime)
                item.eventTypeId = this.getDynamicName(item.eventTypeId, this.listEventType)
                item.warningLevelId = this.formatWarningLevel(item.warningLevelId)
            }
            return response.data.data.data
        },
        getDynamicName(id, lst) {
            return lst.find((x) => x.id == id)?.text
        },
        doDelete(item) {
            // confirm text
            this.$swal({
                title: this.$t('Message.MessageDelete1'),
                text: this.$t('Message.MessageDelete2'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: this.$t('Message.Agree'),
                cancelButtonText: this.$t('Message.Exit'),
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-outline-danger ml-1',
                },
                buttonsStyling: false,
            }).then((result) => {
                if (result.value) {
                    this.$services
                        .delete('/problem/' + item)
                        .then((response) => {
                            this.$refs.problemTable.refresh()
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Delete'),
                                    text: '',
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: 'Error',
                                    text: '',
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: this.$t(
                                        `categories.areas.error.${error.response.data.errorCode}`
                                    ),
                                },
                            })
                        })
                }
            })
        },
        getDate(date) {
            return date ? this.$moment(date).format('DD/MM/YYYY HH:mm') : null
        },
        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.modalImgEventShow = true
        },
        loadEventFiles(eventId) {
            this.$services
                .get(`/event/eventFilesById/${eventId}`)
                .then((response) => {
                    this.listEventFiles = response.data
                })
        },
        async loadAreasTree() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.listAreas = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
    },
}
</script>
<style lang="scss">
@import '@/assets/scss/_custom-tree-select.scss';
</style>
