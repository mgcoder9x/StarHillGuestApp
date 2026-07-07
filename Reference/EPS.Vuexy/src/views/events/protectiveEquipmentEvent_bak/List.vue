<template>
    <div>
        <b-card no-body>
            <b-card-body>
                <!-- Bọc form trong ValidationObserver để điều khiển validate -->
                <validation-observer ref="rules">
                    <b-form @submit.prevent="search">
                        <b-row>
                            <!-- FromDate -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'Events.ProtectiveEquipmentEvent.Table.DateFrom'
                                        )
                                    "
                                    label-for="h-searchForm-dateFrom"
                                    label-cols-md="3"
                                >
                                    <!-- Rule fromDate truyền ToDate để so sánh -->
                                    <validation-provider
                                        #default="{ errors }"
                                        :rules="
                                            `fromDate:` +
                                            (searchForm.dateTo || '')
                                        "
                                        name="FromDate"
                                    >
                                        <date-picker
                                            id="h-searchForm-dateFrom"
                                            v-model="searchForm.dateFrom"
                                            type="datetime"
                                            :locale="currentLocale"
                                            format="DD-MM-YYYY HH:mm:ss"
                                            value-type="YYYY-MM-DD HH:mm:ss"
                                            style="width: 100%"
                                            @change="search"
                                        />
                                        <!-- Hiển thị lỗi ngay dưới ô Từ ngày -->
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <!-- Area -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'Events.ProtectiveEquipmentEvent.Table.AreaName'
                                        )
                                    "
                                    label-for="h-searchForm-area"
                                    label-cols-md="3"
                                >
                                    <Treeselect
                                        v-model="searchForm.areaId"
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
                                        @input="changeArea()"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Device -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'Events.ProtectiveEquipmentEvent.Table.DeviceName'
                                        )
                                    "
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
                                        :reduce="(lstDevice) => lstDevice.id"
                                        :options="lstDeviceByAreaId"
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        value-consists-of="ALL"
                                        multiple
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- ToDate -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'Events.ProtectiveEquipmentEvent.Table.DateTo'
                                        )
                                    "
                                    label-for="h-searchForm-dateTo"
                                    label-cols-md="3"
                                >
                                    <date-picker
                                        id="h-searchForm-dateTo"
                                        v-model="searchForm.dateTo"
                                        type="datetime"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY HH:mm:ss"
                                        value-type="YYYY-MM-DD HH:mm:ss"
                                        style="width: 100%"
                                        @change="search"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Status -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'Events.ProtectiveEquipmentEvent.Table.Classify'
                                        )
                                    "
                                    label-for="h-searchForm-warning"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        v-model="searchForm.warningLevelId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(i) => i.id"
                                        :options="protectiveEquipmentOptions"
                                        @input="search"
                                    />
                                    <!-- <v-select
                                        v-model="searchForm.warningLevelId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(i) => i.id"
                                        :options="
                                            rechangeAndTranslateOptions(
                                                listProtectiveEquipment
                                            )
                                        "
                                        @input="search"
                                    /> -->
                                </b-form-group>
                            </b-col>

                            <!-- Safe -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'Events.ProtectiveEquipmentEvent.Table.Status'
                                        )
                                    "
                                    label-for="h-searchForm-safe"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        v-model="searchForm.isSafe"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(i) => i.id"
                                        :options="
                                            rechangeAndTranslateOptions(
                                                listSafe
                                            )
                                        "
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </b-form>
                </validation-observer>
            </b-card-body>
        </b-card>

        <b-card>
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

            <BasicTable
                ref="protectiveEquipmentEventRef"
                :columns="table.columns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="protectiveEquipmentEventtableConfig"
            >
                <template #table-row="{ column, row }">
                    <span v-if="column.field === 'image'">
                        <div class="d-flex justify-content-center">
                            <img
                                v-if="row.image"
                                :src="`${baseUrl}${row.image}`"
                                width="100"
                                style="cursor: pointer"
                                @click="showModalEvent(row.eventId)"
                            />
                        </div>
                    </span>

                    <!-- Column: Action -->
                    <span v-else-if="column.field === 'action'">
                        <div class="text-nowrap text-center">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/event/protectiveEquipmentEvent/detail/${row.eventId}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                        </div>
                    </span>
                </template>
            </BasicTable>

            <!-- Modal ảnh -->
            <b-modal
                v-model="isModalOpen"
                :title="$t('Events.SearchForm.EventPhotos')"
                size="lg"
                @hidden="clearEventFiles"
            >
                <EventCarousel
                    :list-event-files="listEventFiles"
                    :base-u-r-l="baseUrl"
                />
                <template #modal-footer><p /></template>
            </b-modal>
        </b-card>
    </div>
</template>

<script>
import EventType from '@/utils/eventType'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import Treeselect from '@riophae/vue-treeselect'
import TreeHelper from '@/utils/treeHelper'
import moment from 'moment'
import { ValidationObserver, ValidationProvider } from 'vee-validate'
import {
    ProtectiveEquipment,
    listSafe,
    listProtectiveEquipment,
} from './warningLevelData'

// ✅ Rule: FromDate phải <= ToDate (hiển thị lỗi dưới ô Từ ngày)
// extend('fromDate', {
//     params: ['to'],
//     message: 'reports.Validate.Date',
//     validate(value, { to }) {
//         if (!value || !to) return true
//         const fmt = 'YYYY-MM-DD HH:mm:ss'
//         const f = moment(value, fmt, true)
//         const t = moment(to, fmt, true)
//         if (!f.isValid() || !t.isValid()) return true
//         return f.isSameOrBefore(t)
//     },
// })

export default {
    name: 'ProtectiveEquipmentEventList',
    components: {
        Treeselect,
        ValidationObserver,
        ValidationProvider,
    },
    data() {
        return {
            ProtectiveEquipmentMap: {
                200: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Enough',
                202: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.FaceMask',
                203: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Gloves',
                201: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Helmet',
                204: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.HelmetGloves',
                205: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.HelmetMask',
                206: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.MaskGloves',
                207: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.MissingEverything',
                14: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.RawMaterialSize',
                13: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.ConveyorBeltMisalignment',
                12: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.ConveyorBeltTear',
                10: 'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.ConveyorBeltOverflow',
                // ... các mapping khác
            },
            listSafe,
            listProtectiveEquipment,
            searchForm: {
                eventTypeId: EventType.ProtectiveEquipmentEvent,
                warningLevelId: null,
                dateFrom: null,
                dateTo: null,
                areaId: [],
                deviceId: [],
                isSafe: null,
            },
            table: {
                dataUrl: '/event/protective-equipment-events',
                columns: [
                    {
                        label: 'Events.ProtectiveEquipmentEvent.Table.AccessDate',
                        field: 'accessTime',
                        formatFn(value) {
                            return moment.utc(value).format('DD/MM/YYYY')
                        },
                    },
                    {
                        label: 'Events.ProtectiveEquipmentEvent.Table.Time',
                        field: 'accessTime',
                        formatFn(value) {
                            return moment.utc(value).format('HH:mm:ss')
                        },
                    },
                    {
                        label: 'Events.ProtectiveEquipmentEvent.Table.AreaName',
                        field: 'areaName',
                        type: 'text',
                    },
                    {
                        label: 'Events.ProtectiveEquipmentEvent.Table.DeviceName',
                        field: 'deviceName',
                        type: 'text',
                    },
                    {
                        label: 'Events.ProtectiveEquipmentEvent.Table.Classify',
                        field: 'warningLevelId',
                        type: 'text',
                        formatFn: (value) =>
                            this.$t(
                                this.ProtectiveEquipmentMap[value] ||
                                    'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Unknown'
                            ),
                    },
                    {
                        label: 'Events.ProtectiveEquipmentEvent.Table.Status',
                        field: 'warningLevelId',
                        type: 'text',
                        formatFn: (value) =>
                            value === ProtectiveEquipment.Safe
                                ? this.$t(
                                      'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Safe'
                                  )
                                : this.$t(
                                      'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Unsafe'
                                  ),
                    },
                    {
                        label: 'Events.ProtectiveEquipmentEvent.Table.Image',
                        field: 'image',
                    },
                    {
                        label: 'Events.ProtectiveEquipmentEvent.Table.Action',
                        field: 'action',
                    },
                ],
            },
            isModalOpen: false,
            lstDevice: [],
            lstDeviceByAreaId: [],
            listAreas: [],
            listEventFiles: [],
            // Excel
            headerExcelDetail: [],
            export_fields_vi: {
                Ngày: 'accessDateStr',
                Giờ: 'accessTimeStr',
                'Khu vực': 'areaName',
                'Thiết bị': 'deviceName',
                'Phân loại': 'status',
                'Trạng thái': 'isSafe',
            },
        }
    },
    computed: {
        baseUrl() {
            return process.env.VUE_APP_BASE_URL
        },
        currentLocale() {
            return this.$i18n.locale
        },
        selectedAreaTexts() {
            const ids = Array.isArray(this.searchForm.areaId)
                ? this.searchForm.areaId
                : []
            const flatten = (areas) =>
                (areas || []).flatMap((a) => [
                    a,
                    ...(a.children ? flatten(a.children) : []),
                ])
            const flatList = flatten(this.listAreas)
            return flatList
                .filter((a) => ids.includes(a.id))
                .map((a) => a.label)
        },
        protectiveEquipmentOptions() {
            return [
                {
                    id: 201,
                    text: this.$t(
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.201'
                    ),
                },
                {
                    id: 202,
                    text: this.$t(
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.202'
                    ),
                },
                {
                    id: 203,
                    text: this.$t(
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.203'
                    ),
                },
                {
                    id: 204,
                    text: this.$t(
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.204'
                    ),
                },
                {
                    id: 205,
                    text: this.$t(
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.205'
                    ),
                },
                {
                    id: 206,
                    text: this.$t(
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.206'
                    ),
                },
                {
                    id: 207,
                    text: this.$t(
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.207'
                    ),
                },
                {
                    id: 208,
                    text: this.$t(
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.208'
                    ),
                },
                {
                    id: 209,
                    text: this.$t(
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.209'
                    ),
                },
                {
                    id: 210,
                    text: this.$t(
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.210'
                    ),
                },
            ]
        },
    },
    async created() {
        const cached = getStorage('protectiveEquipmentEvent')
        if (cached) this.searchForm = { ...cached }
        else this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')

        await this.loadAreasTree()
        this.loadDevicesTree()
        this.search()
    },
    methods: {
        clearEventFiles() {
            this.listEventFiles = []
        },
        loadEventFiles(eventId) {
            this.$services
                .get(`/event/eventFilesById/${eventId}`)
                .then((res) => {
                    this.listEventFiles = res.data
                })
        },
        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.isModalOpen = true
        },
        changeArea() {
            this.searchForm.deviceId = null
            if (this.searchForm.areaId && this.searchForm.areaId.length > 0) {
                this.lstDeviceByAreaId = this.lstDevice.filter((x) =>
                    this.searchForm.areaId.includes(x.areaId)
                )
            } else {
                this.lstDeviceByAreaId = this.lstDevice
            }
            this.search()
        },
        refresh() {
            clearStorage('protectiveEquipmentEvent')
            this.searchForm = {
                eventTypeId: EventType.ProtectiveEquipmentEvent,
                warningLevelId: null,
                dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                dateTo: null,
                areaId: [],
                deviceId: [],
                isSafe: false,
            }
            this.$nextTick(() =>
                this.$refs.protectiveEquipmentEventRef.refresh()
            )
        },
        async search() {
            // ✅ Validate trước khi gọi API – nếu sai sẽ hiện lỗi dưới ô Từ ngày
            const ok = await this.$refs.rules.validate()
            if (!ok) return

            setStorage('protectiveEquipmentEvent', this.searchForm, 120)
            this.$refs.protectiveEquipmentEventRef.refresh()
        },
        rechangeAndTranslateOptions(dataList) {
            return dataList?.map((item) => ({
                ...item,
                text: this.$t(item.text),
            }))
        },
        async loadAreasTree() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.listAreas = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
        loadDevicesTree() {
            this.$services.get('/lookup/devices').then((response) => {
                const devices = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
                this.lstDevice = devices
                    .filter((x) => x.eventTypeId == 205)
                    .map(({ text, ...rest }) => ({ ...rest, label: text }))
                this.lstDeviceByAreaId = this.lstDevice
            })
        },
        async exportData() {
            const vm = this

            let allDeviceName = vm.$t('Export.All')
            if (vm.searchForm.deviceId && vm.searchForm.deviceId.length > 0) {
                const names = []
                vm.lstDevice.forEach((d) => {
                    if (vm.searchForm.deviceId.includes(d.id))
                        names.push(d.label)
                })
                allDeviceName = names.join(',')
            }

            let areaName = vm.$t('Export.All')
            if (this.selectedAreaTexts.length > 0) {
                areaName = this.selectedAreaTexts.join(', ')
            }

            let statusName = vm.$t('Export.All')
            if (vm.searchForm.warningLevelId != null) {
                const selected = vm.listProtectiveEquipment.find(
                    (i) => i.id === vm.searchForm.warningLevelId
                )
                statusName = selected
                    ? vm.$t(selected.text)
                    : vm.$t('Export.All')
            }

            let hasSafeName
            if (this.searchForm.isSafe != null) {
                hasSafeName =
                    this.searchForm.isSafe === ProtectiveEquipment.Safe
                        ? this.$t(
                              'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Safe'
                          )
                        : this.$t(
                              'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Unsafe'
                          )
            } else {
                hasSafeName = this.$t('Export.All')
            }

            let dateFrom = moment(this.searchForm.dateFrom).format(
                'DD/MM/YYYY HH:mm'
            )
            let dateTo = moment(this.searchForm.dateTo).format(
                'DD/MM/YYYY HH:mm'
            )
            if (dateFrom === 'Invalid date') {
                dateFrom = moment('2020-01-01 00:00:00').format(
                    'DD/MM/YYYY HH:mm'
                )
            }
            if (dateTo === 'Invalid date') {
                dateTo = moment().format('DD/MM/YYYY HH:mm')
            }

            vm.headerExcelDetail = [
                vm.$t('Events.Header.ExcelProtective'),
                `${vm.$t('fireEvent.common.dateFrom')}: ${dateFrom}    ${vm.$t('fireEvent.common.dateTo')}: ${dateTo}`,
                `${vm.$t('Events.ProtectiveEquipmentEvent.Table.AreaName')}: ${areaName}`,
                `${vm.$t('Events.ProtectiveEquipmentEvent.Table.DeviceName')}: ${allDeviceName}`,
                `${vm.$t('Events.ProtectiveEquipmentEvent.Table.Classify')}: ${statusName}`,
                // `${vm.$t('Table.Status')}: ${hasSafeName}`,
            ]
            vm.export_fields_vi = {
                [vm.$t('Events.ProtectiveEquipmentEvent.Table.AccessDate')]:
                    'accessDateStr',
                [vm.$t('Events.ProtectiveEquipmentEvent.Table.Time')]:
                    'accessTimeStr',
                [vm.$t('Events.ProtectiveEquipmentEvent.Table.AreaName')]:
                    'areaName',
                [vm.$t('Events.ProtectiveEquipmentEvent.Table.DeviceName')]:
                    'deviceName',
                [vm.$t('Events.ProtectiveEquipmentEvent.Table.Status')]:
                    'isSafe',
                [vm.$t('Events.ProtectiveEquipmentEvent.Table.Classify')]:
                    'status',
            }

            const pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'accessTime',
                sortDesc: true,
            }
            const formData = `${new URLSearchParams(
                pagination
            ).toString()}&${new URLSearchParams(vm.searchForm).toString()}`

            const response = await this.$services.get(
                `${vm.$refs.protectiveEquipmentEventRef.dataUrl}?${formData}`
            )

            for (const item of response.data.data.data) {
                item.accessTimeStr = this.getTime(item.accessTime)
                item.accessDateStr = this.getDate(item.accessTime)
                item.status = this.$t(
                    this.ProtectiveEquipmentMap[item.warningLevelId] ||
                        'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Unknown'
                )
                item.isSafe =
                    item.warningLevelId === ProtectiveEquipment.Safe
                        ? this.$t(
                              'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Safe'
                          )
                        : this.$t(
                              'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Unsafe'
                          )
            }
            return response.data.data.data
        },
        getDate(date) {
            return date ? this.$moment(date).format('DD/MM/YYYY') : null
        },
        getTime(date) {
            return date ? this.$moment(date).format('HH:mm:ss') : null
        },
    },
}
</script>

<style></style>
