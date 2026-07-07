<!-- eslint-disable vue/html-self-closing -->
<template>
    <div>
        <validation-observer ref="rules">
            <!-- Filters -->
            <b-card no-body>
                <b-card-body>
                    <b-form @submit.prevent="search">
                        <!-- advance search input -->
                        <b-row>
                            <!-- FromDate -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        this.$t('Events.SearchForm.FromDate')
                                    "
                                    label-for="h-searchForm-dateFrom"
                                    label-cols-md="3"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        :rules="`fromDate:` + searchForm.dateTo"
                                        name="NotificationEventTemplateName"
                                    >
                                        <!-- <b-form-datepicker
                                            id="h-searchForm-dateFrom"
                                            v-model="searchForm.dateFrom"
                                            :date-format-options="{
                                                day: 'numeric',
                                                month: 'long',
                                                year: 'numeric',
                                            }"
                                            reset-button
                                            type="datetime"
                                            :locale="currentLocale"
                                            @input="search"
                                        >
                                        </b-form-datepicker> -->
                                        <date-picker
                                            id="h-searchForm-dateFrom"
                                            v-model="searchForm.dateFrom"
                                            type="datetime"
                                            :locale="currentLocale"
                                            format="DD-MM-YYYY HH:mm:ss"
                                            value-type="YYYY-MM-DD HH:mm:ss"
                                            style="width: 100%"
                                            @change="search"
                                        ></date-picker>
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>
                            <!-- Area -->
                            <b-col md="4">
                                <b-form-group
                                    :label="this.$t('Events.SearchForm.Area')"
                                    label-for="h-searchForm-area"
                                    label-cols-md="3"
                                >
                                    <tree-select
                                        v-model="searchForm.areaId"
                                        :options="listArea"
                                        label="text"
                                        :reduce="(area) => area.id"
                                        :multiple="true"
                                        placeholder=""
                                        value-consists-of="ALL"
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        @input="changeArea()"
                                    />
                                </b-form-group>
                            </b-col>
                            <!-- Device -->
                            <b-col md="4">
                                <b-form-group
                                    :label="this.$t('Events.SearchForm.Device')"
                                    label-for="h-searchForm-device"
                                    label-cols-md="3"
                                >
                                    <tree-select
                                        v-model="searchForm.deviceId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(device) => device.id"
                                        :options="lstDeviceByAreaId"
                                        :multiple="true"
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        value-consists-of="ALL"
                                        @input="search"
                                    >
                                    </tree-select>
                                </b-form-group>
                            </b-col>
                            <!-- ToDate -->
                            <b-col md="4">
                                <b-form-group
                                    :label="this.$t('Events.SearchForm.ToDate')"
                                    label-for="h-searchForm-dateTo"
                                    label-cols-md="3"
                                >
                                    <!-- <b-form-datepicker
                                        id="h-searchForm-dateTo"
                                        v-model="searchForm.dateTo"
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        reset-button
                                        type="datetime"
                                        :locale="currentLocale"
                                        @input="search"
                                    >
                                    </b-form-datepicker> -->
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
                            <!-- EventWarningLevel -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        this.$t('Events.SearchForm.Warning')
                                    "
                                    label-for="h-searchForm-area"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        v-model="searchForm.warningLevelId"
                                        :options="
                                            rechangeOptions(listWarningLevels)
                                        "
                                        label="text"
                                        :reduce="(item) => item.id"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </b-form>
                </b-card-body>
            </b-card>
        </validation-observer>
        <b-card title="" body-class="pt-1">
            <div class="text-left mb-1 d-flex" style="gap: 8px">
                <b-button
                    v-waves
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="$t('Conveyor.Excel.Header')"
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
                ref="eventTable"
                :columns="columns"
                data-url="/event/conveyor-events"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="conveyorEventTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Image -->
                    <span
                        v-if="props.column.field == 'image'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        <img
                            :src="`${baseURL}${props.row.image}`"
                            loading="lazy"
                            alt="Image"
                            width="100"
                            style="object-fit: cover; border-radius: 0.3em"
                            class="cursor-pointer"
                            @click="showModalEvent(props.row.eventId)"
                        />
                    </span>
                    <span
                        v-else-if="props.column.field == 'warningLevelId'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{
                            getStaticName(
                                props.row.warningLevelId,
                                listWarningLevels
                            )
                        }}
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <div class="d-flex justify-content-center">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/event/conveyorEvent/Detail/${props.row.eventId}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                        </div>
                    </span>
                </template>
            </BasicTable>
        </b-card>

        <b-modal
            v-model="modalImgEventShow"
            :title="$t('Events.SearchForm.EventPhotos')"
            size="lg"
        >
            <EventCarousel
                :list-event-files="listEventFiles"
                :base-u-r-l="baseURL"
                @hidden="clearEventFiles"
            />
            <!-- <b-carousel id="carousel-example-generic" indicators controls>
                <b-carousel-slide
                    v-for="item in listEventFiles"
                    :key="item.id"
                    :img-src="`${baseURL}${item.filePath}`"
                />
            </b-carousel> -->
            <template #modal-footer>
                <p></p>
            </template>
        </b-modal>
    </div>
</template>

<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import { $themeConfig } from '@themeConfig'
import moment from 'moment'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import TreeHelper from '@/utils/treeHelper'
import Tree from '@/views/extensions/tree/Tree.vue'

import { reactive } from '@vue/composition-api'
const isDevEnv = process.env.NODE_ENV == 'development'
import '@/assets/scss/_custom-tree-select.scss'

export default {
    mixins: [authorizationMixin],
    components: { Tree },
    setup() {
        // App Name
        const { apiURL } = $themeConfig.app
        return {
            apiURL,
        }
    },
    data() {
        return {
            modalImgEventShow: false,
            srcImgEvent: '',
            searchForm: {
                eventTypeId: 601,
                dateFrom: null,
                dateTo: null,
                areaId: null,
                deviceId: null,
            },
            // define options
            options: [],
            headerExcelDetail: [],
            export_fields_vi: {
                'Khu vực': 'areaName',
                'Thiết bị': 'deviceName',
                'Thời gian': 'accessTimeStr',
                'Loại giám sát': 'eventTypeName',
            },
            columns: [
                {
                    label: 'fireEvent.common.date',
                    field: 'accessTime',
                    formatFn(value) {
                        return moment.utc(value).format('DD/MM/YYYY')
                    },
                },
                {
                    label: 'fireEvent.common.time',
                    field: 'accessTime',
                    formatFn(value) {
                        return moment.utc(value).format('HH:mm:ss')
                    },
                },
                {
                    label: 'Events.Table.Area',
                    field: 'areaName',
                },
                {
                    label: 'Events.Table.Device',
                    field: 'deviceName',
                },
                {
                    label: 'Events.Table.Warning',
                    field: 'warningLevelId',
                },
                {
                    label: 'Events.Table.Image',
                    field: 'image',
                },
                {
                    label: 'Warning.List.Table.Operation',
                    field: 'action',
                },
            ],
            listArea: [],
            listDevice: [],
            lstDeviceByAreaId: [],
            listEventFiles: [],
            listWarningLevels: [],
        }
    },
    watch: {
        'searchForm.areaId': function (newVal) {
            // Gọi searchByAreaId khi giá trị thay đổi, bao gồm cả khi xóa lựa chọn
            this.searchByAreaId(newVal)
        },
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        baseURL() {
            return process.env.VUE_APP_BASE_URL
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        //this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
        this.searchForm.compId = accessToken.companyId
        this.loadArea()
        this.loadDevice()
        this.loadWarningLevels()
        const searchForm = getStorage('conveyorEventSearchForm')
        if (searchForm) {
            this.searchForm = { ...searchForm }
            this.search()
        } else {
            this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
        }
    },
    methods: {
        clearEventFiles() {
            this.listEventFiles = [] // clear khi modal đóng
        },
        getStaticName(id, lst) {
            return this.$t(lst.find((x) => x.id == id)?.label)
        },
        rechangeOptions(dataList) {
            return dataList?.map((item) => ({
                ...item,
                text: this.$t(item.label),
            }))
        },
        changeArea() {
            this.searchForm.deviceId = null
            if (
                this.searchForm.areaId != null &&
                this.searchForm.areaId.length > 0
            ) {
                this.lstDeviceByAreaId = this.listDevice.filter((x) =>
                    this.searchForm.areaId.includes(x.areaId)
                )
            } else {
                this.lstDeviceByAreaId = this.listDevice
            }
            this.search()
        },
        refresh() {
            clearStorage('conveyorEventSearchForm')
            this.searchForm = {
                eventTypeId: 601,
                dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                dateTo: null,
                areaId: null,
                deviceId: null,
            }
            this.$nextTick(() => {
                this.$refs.eventTable.refresh()
            })
        },
        search() {
            setStorage('conveyorEventSearchForm', this.searchForm, 120)
            this.$refs.eventTable.refresh()
        },
        searchByAreaId(value) {
            debugger
            var vm = this
            if (!value) {
                // Khi không chọn gì (chọn x)
                this.$services.get(`/lookup/devices`).then((response) => {
                    this.listDevice = TreeHelper.removeEmptyChildren(
                        response.data.data
                    )
                })
            } else {
                // Khi có giá trị (lựa chọn một khu vực)
                this.$services
                    .get(`/lookup/devices/` + value)
                    .then((response) => {
                        let devices = TreeHelper.removeEmptyChildren(
                            response.data.data
                        )
                        this.listDevice = devices.map((item) => {
                            const { text, ...rest } = item
                            return {
                                ...rest,
                                label: text,
                            }
                        })
                    })
            }
            setStorage('conveyorEventSearchForm', this.searchForm, 120)
            this.$refs.eventTable.refresh()
        },
        loadArea() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.listArea = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
        loadWarningLevels() {
            this.$services
                .get('/lookup/event-warning-levels/601')
                .then((response) => {
                    this.listWarningLevels = (response.data.data || []).map(
                        (item) => ({
                            id: parseInt(item.id, 10),
                            label: item.text,
                        })
                    )
                })
        },
        loadDevice() {
            this.$services.get('/lookup/devices').then((response) => {
                let devices = TreeHelper.removeEmptyChildren(response.data.data)
                this.listDevice = devices
                    .filter((x) => x.eventTypeId == 601)
                    .map((item) => {
                        const { text, ...rest } = item
                        return {
                            ...rest,
                            label: text,
                        }
                    })
                this.lstDeviceByAreaId = this.listDevice
            })
        },
        loadEventFiles(eventId) {
            this.$services
                .get(`/event/eventFilesById/${eventId}`)
                .then((response) => {
                    this.listEventFiles = response.data
                })
        },
        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.modalImgEventShow = true
        },
        async exportData() {
            debugger
            var vm = this
            var allAreaName = vm.$t('Export.All')
            let areaName = []
            if (
                vm.searchForm.areaId != null &&
                vm.searchForm.areaId.length > 0
            ) {
                vm.listArea.forEach((area) => {
                    if (vm.searchForm.areaId.includes(area.id)) {
                        areaName.push(area.label)
                    }
                })
                allAreaName = areaName.join(',')
            }

            var allDeviceName = vm.$t('Export.All')
            let deviceName = []
            if (
                vm.searchForm.deviceId != null &&
                vm.searchForm.deviceId.length > 0
            ) {
                vm.listDevice.forEach((devi) => {
                    if (vm.searchForm.deviceId.includes(devi.id)) {
                        deviceName.push(devi.label)
                    }
                })
                allDeviceName = deviceName.join(',')
            }

            let statusName = vm.$t('Export.All')

            if (vm.searchForm.warningLevelId != null) {
                const selected = vm.listWarningLevels.find(
                    (item) => item.id === vm.searchForm.warningLevelId
                )
                statusName = selected
                    ? vm.$t(selected.label)
                    : vm.$t('Export.All')
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

            vm.headerExcelDetail = [
                vm.$t('Events.Header.ExcelConveyor'),
                `${vm.$t('Events.SearchForm.FromDate')}: ${dateFrom}    ${vm.$t('Events.SearchForm.ToDate')} :${dateTo}`,
                `${vm.$t('Events.SearchForm.Area')}: ${allAreaName}`,
                `${vm.$t('Events.SearchForm.Device')}: ${allDeviceName}`,
                `${vm.$t('Events.Table.Warning')}: ${statusName}`,
            ]
            vm.export_fields_vi = {
                [vm.$t('fireEvent.common.date')]: 'accessDateStr',
                [vm.$t('fireEvent.common.time')]: 'accessTimeStr',
                [vm.$t('Events.Table.Area')]: 'areaName',
                [vm.$t('Events.Table.Device')]: 'deviceName',
                [vm.$t('Events.Table.Warning')]: 'warningLevelId',
            }
            var pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'accessTime',
                sortDesc: true,
            }
            //var formData = $.extend({}, pagination, vm.searchForm);
            var formData =
                new URLSearchParams(pagination).toString() +
                '&' +
                new URLSearchParams(vm.searchForm).toString()
            var response = await this.$services.get(
                vm.$refs.eventTable.dataUrl + '?' + formData
            )
            for (const item of response.data.data.data) {
                item.accessTimeStr = this.getTime(item.accessTime)
                item.accessDateStr = this.getDate(item.accessTime)
                item.warningLevelId = this.getStaticName(
                    item.warningLevelId,
                    this.listWarningLevels
                )
            }
            return response.data.data.data
        },
        getDate(date) {
            return date ? this.$moment.utc(date).format('DD/MM/YYYY') : null
        },
        getTime(date) {
            return date ? this.$moment.utc(date).format('HH:mm:ss') : null
        },
    },
}
</script>

<style lang="scss">
@import '@/assets/scss/_custom-tree-select.scss';

.center-icon {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100%;
    /* Đảm bảo bao phủ toàn bộ chiều cao của ô */
}

/* Đảm bảo video có thể căn giữa đúng */
</style>
