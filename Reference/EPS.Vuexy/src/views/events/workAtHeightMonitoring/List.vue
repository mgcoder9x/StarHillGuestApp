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
                                        v-model="searchForm.eventTypeId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(i) => parseInt(i.id)"
                                        :options="eventTypeOptions"
                                        @input="
                                            updateSelectedWarningLevels()
                                            search()
                                        "
                                    />
                                </b-form-group>
                            </b-col>
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'Events.ProtectiveEquipmentEvent.Table.Equipment'
                                        )
                                    "
                                    label-for="h-searchForm-warning"
                                    label-cols-md="3"
                                >
                                    <tree-select
                                        v-model="searchForm.warningLevelId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        :reduce="(i) => i.id"
                                        :options="options.selectedWarningLevels"
                                        multiple
                                        :close-on-select="false"
                                        :clear-on-select="false"
                                        :max-height="300"
                                        placeholder=""
                                        @input="search"
                                    />
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
                        :name="$t('Events.Header.ExcelWorkAtHeight')"
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
                <b-button
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                    @click="createManyIssue()"
                >
                    <Icon icon="famicons:enter-outline" class="sm-icon" />
                    <span class="ml-25">{{
                        $t('Button.CreateMoreIssue')
                    }}</span>
                </b-button>
            </div>

            <BasicTable
                ref="protectiveEquipmentEventRef"
                :columns="table.columns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                :show-check-box="true"
                storage-name="workAtHeightMonitoringTableConfig"
            >
                <template #table-row="{ column, row }">
                    <span v-if="column.field === 'image'">
                        <div class="d-flex justify-content-center">
                            <img
                                v-if="row.image"
                                :src="`${baseUrl}${row.image}`"
                                style="
                                    width: 60px;
                                    height: 80px;
                                    object-fit: cover;
                                    border-radius: 0.3em;
                                    cursor: pointer;
                                "
                                @click="showModalEvent(row.eventId)"
                            />
                        </div>
                    </span>

                    <!-- Column: Action -->
                    <span v-else-if="column.field === 'eventTypeId'">
                        {{ formatEventType(row.eventTypeId) }}
                    </span>
                    <span v-else-if="column.field === 'warningLevelId'">
                        {{ formatWarningLevel(row.warningLevelId) }}
                    </span>
                    <span v-else-if="column.field === 'status'">
                        {{ formatSafetyType(row.warningLevelId) }}
                    </span>
                    <span v-else-if="column.field === 'action'">
                        <div class="text-nowrap text-center">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/event/workAtHeightMonitoring/detail/${row.eventId}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="!isSafe(row.warningLevelId)"
                                v-b-tooltip.hover
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                variant="label-secondary"
                                :title="$t('Button.CreateIssue')"
                                @click="createIssue(row)"
                            >
                                <Icon icon="mdi:alert" class="xs-icon" />
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
            <b-modal
                v-model="modalCreateProblem"
                :title="$t('Xử lý vi phạm an toàn lao động')"
                dialog-class="modal-70"
                hide-footer
            >
                <!-- <create-problem
                    :new-data="dataCreateProblem"
                    @success="modalCreateProblem = false"
                >
                </create-problem> -->
                <create-issue
                    :create-data="dataCreateProblem"
                    @success="modalCreateProblem = false"
                >
                </create-issue>
            </b-modal>
        </b-card>
    </div>
</template>

<script>
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import Treeselect from '@riophae/vue-treeselect'
import TreeHelper from '@/utils/treeHelper'
import moment from 'moment'
import { ValidationObserver, ValidationProvider } from 'vee-validate'
import { listSafe, listProtectiveEquipment } from './warningLevelData'

const { mapTextToLabel } = TreeHelper
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
            modalCreateProblem: false,
            dataCreateProblem: {
                contractorId: null,
                employeeId: null,
                gender: null,
                birthday: null,
                birthdayStr: null,
                eventTypeId: null,
                warningLevelId: null,
                accessTime: null,
                areaId: null,
                image: null,
            },
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
            SafetyTypes: {
                EnoughEquipment: 2058,
                EnoughSafetyHarness: 2091,
                EnoughFence: 2122,
            },
            listSafe,
            listProtectiveEquipment,
            searchForm: {
                eventTypeId: null,
                EventTypeIds: '209,212',
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
                        field: 'eventTypeId',
                    },
                    {
                        label: 'Events.ProtectiveEquipmentEvent.Table.Equipment',
                        field: 'warningLevelId',
                    },
                    {
                        label: 'Events.ProtectiveEquipmentEvent.Table.Status',
                        field: 'status',
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
            options: {
                eventTypes: [],
                selectedWarningLevels: [],
                warningLevels: [],
                devices: [],
                selectedDevices: [],
            },
        }
    },
    computed: {
        baseUrl() {
            return process.env.VUE_APP_BASE_URL
            // return "https://novaland.atin.vn/Service/"
        },
        currentLocale() {
            return this.$i18n.locale
        },
        eventTypeOptions() {
            const { locale } = this.$i18n
            if (locale === 'vi') {
                return this.options.eventTypes
            }
            return this.options.eventTypes.map((item) => ({
                ...item,
                text: item.englishName,
            }))
        },
        warningLevelOptions() {
            return this.options.warningLevels.map((opt) => ({
                ...opt,
                text: this.$t(
                    `Events.ProtectiveEquipmentEvent.WarningLevels.${opt.id}`
                ),
            }))
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
    },
    watch: {
        currentLocale() {
            this.updateSelectedWarningLevels()
        },
    },
    async created() {
        const cached = getStorage('workAtHeightMonitoring')
        if (cached) this.searchForm = { ...cached }
        else this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')

        await this.loadAreasTree()
        this.loadDevicesTree()
        this.featEventTypes()
        this.featWarningLevels()
        this.search()
    },
    methods: {
        createIssue(data) {
            const issueData = { ...data }
            issueData.description = `Cảnh báo ${issueData.accessTimeStr} ở khu vực ${issueData.areaName}`

            debugger
            issueData.createdAt = moment
                .utc(issueData.accessTime)
                .format('YYYY-MM-DD HH:mm:ss')
            issueData.personIds = []
            issueData.eventIds = issueData.eventId
            this.dataCreateProblem = JSON.parse(JSON.stringify(issueData))
            this.modalCreateProblem = true
        },
        createManyIssue() {
            const selected = this.$refs.protectiveEquipmentEventRef.selectedRows
            console.log(selected)
            if (!selected || selected.length === 0) {
                this.$bvToast?.toast(this.$t('Error.Select.Again1'), {
                    title: this.$t('Error.Noti'),
                    variant: 'warning',
                    solid: true,
                    autoHideDelay: 3000,
                })
                return
            }

            const hasUnsafe = selected.some((x) =>
                this.isSafe(x.warningLevelId)
            )
            if (hasUnsafe) {
                this.$bvToast?.toast(this.$t('Error.Select.Again2'), {
                    title: this.$t('Error.Noti'),
                    variant: 'danger',
                    solid: true,
                })
                return
            }

            const data = { ...selected[0] }
            data.description = `Cảnh báo ${data.accessTimeStr} ở khu vực ${data.areaName}`
            data.createdAt = moment
                .utc(data.accessTime)
                .format('YYYY-MM-DD HH:mm:ss')
            data.personIds = []
            data.eventIds = selected.map((x) => x.eventId).join(',')

            this.dataCreateProblem = JSON.parse(JSON.stringify(data))
            this.modalCreateProblem = true
        },
        formatEventType(eventTypeId) {
            const existingType = this.eventTypeOptions.find(
                (i) => parseInt(i.id, 10) === eventTypeId
            )
            return existingType
                ? this.$t(existingType.text)
                : this.$t(
                      'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Unknown'
                  )
        },
        formatWarningLevel(warningLevelId) {
            if (!warningLevelId) {
                return this.$t(
                    `Events.ProtectiveEquipmentEvent.WarningLevels.Unknown`
                )
            }
            const key = `Events.ProtectiveEquipmentEvent.WarningLevels.${warningLevelId}`
            if (this.$te(key)) {
                return this.$t(key)
            }
            return this.$t(
                `Events.ProtectiveEquipmentEvent.WarningLevels.Unknown`
            )
        },
        isSafe(warningLevelId) {
            const safety = [
                this.SafetyTypes.EnoughEquipment,
                this.SafetyTypes.EnoughSafetyHarness,
                this.SafetyTypes.EnoughFence,
            ]
            return safety.includes(warningLevelId)
        },
        formatSafetyType(warningLevelId) {
            const safety = [
                this.SafetyTypes.EnoughEquipment,
                this.SafetyTypes.EnoughSafetyHarness,
                this.SafetyTypes.EnoughFence,
            ]
            return safety.includes(warningLevelId)
                ? this.$t(
                      'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Safe'
                  )
                : this.$t(
                      'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Unsafe'
                  )
        },
        featEventTypes() {
            this.$services
                .get(`/lookup/eventType?equipmentOnly=true`)
                .then((res) => {
                    this.options.eventTypes = res.data.data.filter(
                        (item) => item.id !== '205'
                    )
                })
        },
        featWarningLevels() {
            this.$services
                .get(`/lookup/warning-levels?equipmentOnly=true`)
                .then((res) => {
                    this.options.warningLevels = res.data.data.filter(
                        (item) => item.parentId !== 205
                    )
                    this.updateSelectedWarningLevels()
                })
        },
        updateSelectedWarningLevels() {
            if (!this.searchForm.eventTypeId) {
                this.options.selectedWarningLevels = mapTextToLabel(
                    this.warningLevelOptions
                )
                this.searchForm.warningLevelId = null
            } else {
                this.options.selectedWarningLevels = mapTextToLabel(
                    this.warningLevelOptions.filter(
                        (item) => item.parentId === this.searchForm.eventTypeId
                    )
                )
                // Reset warningLevelId nếu giá trị hiện tại không có trong danh sách mới
                const isValidSelection =
                    this.options.selectedWarningLevels.some(
                        (item) => item.id === this.searchForm.warningLevelId
                    )
                if (!isValidSelection) {
                    this.searchForm.warningLevelId = null
                }
            }
        },
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
            clearStorage('workAtHeightMonitoring')
            this.searchForm = {
                eventTypeId: null,
                EventTypeIds: '209,212',
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

            // Update eventTypeIds logic before searching
            if (
                this.searchForm.eventTypeId !== null &&
                this.searchForm.eventTypeId !== undefined
            ) {
                this.searchForm.EventTypeIds = null
            } else {
                this.searchForm.EventTypeIds = '209,212'
            }

            setStorage('workAtHeightMonitoring', this.searchForm, 120)

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
                    .filter((x) => x.eventTypeId !== 205)
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

            if (vm.searchForm.eventTypeId != null) {
                statusName = this.formatEventType(vm.searchForm.eventTypeId)
            }

            // let hasSafeName
            // if (this.searchForm.isSafe != null) {
            //     hasSafeName =
            //         this.searchForm.isSafe === ProtectiveEquipment.Safe
            //             ? this.$t(
            //                   'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Safe'
            //               )
            //             : this.$t(
            //                   'Events.ProtectiveEquipmentEvent.ProtectiveEquipmentType.Unsafe'
            //               )
            // } else {
            //     hasSafeName = this.$t('Export.All')
            // }

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
                vm.$t('Events.Header.ExcelWorkAtHeight'),
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
                [vm.$t('Events.ProtectiveEquipmentEvent.Table.Equipment')]:
                    'status',
                [vm.$t('Events.ProtectiveEquipmentEvent.Table.Status')]:
                    'classify',
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

            const exportData = response.data.data.data.map((item) => ({
                ...item,
                accessTimeStr: this.getTime(item.accessTime),
                accessDateStr: this.getDate(item.accessTime),
                status: this.formatWarningLevel(item.warningLevelId),
                classify: this.formatSafetyType(item.warningLevelId),
            }))
            console.log('exportData', exportData)
            return exportData
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

<style>
.modal-70 {
    max-width: 70vw;
}
</style>
