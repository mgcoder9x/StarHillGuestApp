<!-- AllEvents.vue -->
<!-- eslint-disable vue/html-self-closing -->
<template>
    <div>
        <validation-observer ref="rules">
            <!-- Filters -->
            <b-card no-body>
                <b-card-body>
                    <b-form @submit.prevent="search">
                        <b-row>
                            <!-- FromDate -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('Events.SearchForm.FromDate')"
                                    label-for="h-searchForm-dateFrom"
                                    label-cols-md="3"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        :rules="`fromDate:` + searchForm.dateTo"
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
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <!-- EventType -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('Events.SearchForm.EventType')"
                                    label-for="h-searchForm-eventType"
                                    label-cols-md="3"
                                >
                                    <tree-select
                                        id="h-searchForm-eventType"
                                        :key="`eventType-${$i18n.locale}`"
                                        v-model="searchForm.eventTypeId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="label"
                                        :reduce="(eventType) => eventType.id"
                                        :options="eventTypeOptions"
                                        placeholder=""
                                        @input="changeEventType"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Device -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('Events.SearchForm.Device')"
                                    label-for="h-searchForm-device"
                                    label-cols-md="3"
                                >
                                    <tree-select
                                        id="h-searchForm-device"
                                        v-model="searchForm.deviceId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(device) => device.id"
                                        :options="listDeviceByEventType"
                                        :multiple="true"
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        value-consists-of="ALL"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <b-row>
                            <!-- ToDate -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('Events.SearchForm.ToDate')"
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

                            <!-- Area -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('Events.SearchForm.Area')"
                                    label-for="h-searchForm-area"
                                    label-cols-md="3"
                                >
                                    <tree-select
                                        id="h-searchForm-area"
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
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Classification / WarningLevel -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('Events.Table.Warning')"
                                    label-for="h-searchForm-warning"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        id="h-searchForm-warning"
                                        v-model="searchForm.warningLevelId"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        :options="listWarningOptions"
                                        label="label"
                                        :reduce="(o) => o.id"
                                        :multiple="true"
                                        :clearable="true"
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
                        :name="$t('Events.Header.Excel')"
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
                    @click="refresh"
                >
                    <Icon icon="famicons:enter-outline" class="sm-icon" />
                    <span class="ml-25">{{ $t('Button.Refresh') }}</span>
                </b-button>
            </div>

            <!-- table -->
            <BasicTable
                ref="eventTable"
                :columns="columns"
                data-url="/event"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="allEventTable"
            >
                <template #table-row="props">
                    <!-- Warning -->
                    <span
                        v-if="props.column.field === 'warningLevelId'"
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
                                    path: `/event/allEvents/Detail/${props.row.eventId}`,
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
            modal-class="modal-80"
            hide-footer
            @hidden="clearEventFiles"
        >
            <EventCarousel
                :list-event-files="listEventFiles"
                :base-u-r-l="baseURL"
            />
            <template #modal-footer><p /></template>
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
import '@/assets/scss/_custom-tree-select.scss'
import { ValidationObserver, ValidationProvider } from 'vee-validate'
import protectiveEquipmentTranslations from '@/libs/i18n/locales/vi/event-protectiveEquipmentEvent.json'
import protectiveEquipmentTranslationsEn from '@/libs/i18n/locales/en/event-protectiveEquipmentEvent.json'
import {
    ProtectiveEquipment,
    CarWarningType,
    ProtectiveEquipmentEvent,
} from '../protectiveEquipmentEvent/warningLevelData'

export default {
    mixins: [authorizationMixin],
    components: { Tree, ValidationObserver, ValidationProvider },
    setup() {
        const { apiURL } = $themeConfig.app
        return { apiURL }
    },
    data() {
        return {
            // constants / maps
            ProtectiveEquipment,
            CarWarningType,
            ProtectiveEquipmentEvent,
            protectiveEquipmentTranslations,
            protectiveEquipmentTranslationsEn,

            // ui state
            modalImgEventShow: false,
            srcImgEvent: '',

            // search model
            searchForm: {
                compId: null,
                eventTypeId: null,
                dateFrom: null,
                dateTo: null,
                areaId: [],
                deviceId: [],
                warningLevelId: [], // multiple ids
            },

            // table / export
            headerExcelDetail: [],
            export_fields_vi: {
                [this.$t('Events.Table.Time')]: 'accessTimeStr',
                [this.$t('Events.Table.Area')]: 'areaName',
                [this.$t('Events.Table.Device')]: 'deviceName',
                [this.$t('Events.Table.EventType')]: 'eventTypeName',
                [this.$t('Events.Table.Warning')]: 'warningName',
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
                    label: 'VehicleEvent.Field.Time',
                    field: 'accessTime',
                    formatFn(value) {
                        return moment.utc(value).format('HH:mm:ss')
                    },
                },
                { label: 'Events.Table.Area', field: 'areaName' },
                { label: 'Events.Table.Device', field: 'deviceName' },
                { label: 'Events.Table.EventType', field: 'eventTypeName' },
                { label: 'Events.Table.Warning', field: 'warningLevelId' },
                { label: 'Events.Table.Image', field: 'image' },
                { label: 'Warning.List.Table.Operation', field: 'action' },
            ],

            // lookups
            listArea: [],
            listDevice: [],
            listDeviceByEventType: [],
            listEventType: [],
            listEventFiles: [],
            listWarningOptions: [],
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        eventTypeOptions() {
            const mapLabel = (items) => {
                return items.map((item) => {
                    const newItem = {
                        ...item,
                        label:
                            this.$i18n.locale === 'vi'
                                ? item.text
                                : item.englishName || item.text,
                    }
                    if (newItem.children && newItem.children.length > 0) {
                        newItem.children = mapLabel(newItem.children)
                    }
                    return newItem
                })
            }
            return mapLabel(this.listEventType)
        },
        baseURL() {
            return process.env.VUE_APP_BASE_URL
        },
    },
    watch: {
        // Remap labels when language changes
        '$i18n.locale'() {
            this.mergeWarningTranslations()
            this.buildWarningOptions(this.searchForm.eventTypeId)
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken?.companyId || null
        this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')

        // i18n for warning keys
        this.mergeWarningTranslations()

        // lookups
        this.loadArea()
        this.loadDevice()
        this.loadEventType()

        // build select options
        this.buildWarningOptions()

        // restore search state
        const saved = getStorage('allEventSearchForm')
        if (saved) {
            this.searchForm = { ...saved }
            if (this.searchForm.eventTypeId) {
                this.buildWarningOptions(this.searchForm.eventTypeId)
            }
        } else {
            this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
            // ✅ Validate ngay khi load để check nếu có dateTo từ cache
            this.$nextTick(() => {
                this.$refs.rules.validate()
            })
        }
        this.search()
    },
    methods: {
        // ===== i18n helpers =====
        mergeWarningTranslations() {
            debugger
            if (
                this.protectiveEquipmentTranslations.ProtectiveEquipmentType ||
                this.protectiveEquipmentTranslationsEn.ProtectiveEquipmentType
            ) {
                if (this.currentLocale === 'vi') {
                    this.$i18n.mergeLocaleMessage('vi', {
                        ProtectiveEquipmentType:
                            this.protectiveEquipmentTranslations
                                .ProtectiveEquipmentType,
                    })
                } else {
                    this.$i18n.mergeLocaleMessage('en', {
                        ProtectiveEquipmentType:
                            this.protectiveEquipmentTranslationsEn
                                .ProtectiveEquipmentType,
                    })
                }
            }
        },
        buildWarningOptions(eventTypeId) {
            let url = '/lookup/event-warning-levels'
            if (eventTypeId) {
                url += `/${eventTypeId}`
            }
            this.$services.get(url).then((response) => {
                const data = response.data.data || []
                this.listWarningOptions = data.map((item) => ({
                    id: parseInt(item.id, 10),
                    label: item.text,
                }))
            })
        },

        // buildWarningOptions() {
        //   this.$services.get('/lookup/event-warning-levels').then((response) => {
        //     const devices = TreeHelper.removeEmptyChildren(response.data.data)
        //     this.listWarningOptions = devices.map(({ text, ...rest }) => ({
        //       ...rest,
        //       label: text,
        //     }))
        //   })
        // },
        // ===== UI actions =====
        changeEventType() {
            this.searchForm.deviceId = []
            this.searchForm.warningLevelId = []
            if (this.searchForm.eventTypeId != null) {
                this.listDeviceByEventType = this.listDevice.filter(
                    (x) => x.eventTypeId == this.searchForm.eventTypeId
                )
                this.buildWarningOptions(this.searchForm.eventTypeId)
            } else {
                this.listDeviceByEventType = this.listDevice
                this.buildWarningOptions()
            }
            this.search()
        },
        async search() {
            const ok = await this.$refs.rules.validate()
            if (!ok) return
            setStorage('allEventSearchForm', this.searchForm, 120)
            this.$refs.eventTable && this.$refs.eventTable.refresh()
        },
        refresh() {
            clearStorage('allEventSearchForm')
            this.searchForm = {
                compId: this.searchForm.compId,
                eventTypeId: null,
                dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                dateTo: null,
                areaId: [],
                deviceId: [],
                warningLevelId: [],
            }

            this.$nextTick(
                () =>
                    this.$refs.eventTable &&
                    this.$refs.eventTable.refresh() &&
                    this.$refs.rules.validate()
            )
        },

        // ===== lookups / data =====
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                // const eventType = TreeHelper.removeEmptyChildren(response.data.data)
                // this.listEventType = eventType.map(({ text, ...rest }) => ({
                //   ...rest,
                //   label: text,
                // }))
                let eventType = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
                // const allowed = ['300', '400', '500']

                // eventType = eventType.filter(
                //     (item) =>
                //         allowed.includes(item.id) ||
                //         allowed.includes(item.value)
                // )

                this.listEventType = eventType.map((item) => ({
                    ...item,
                    label: item.text,
                }))
            })
        },
        loadArea() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.listArea = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
        loadDevice() {
            this.$services.get('/lookup/devices').then((response) => {
                const devices = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
                this.listDevice = devices.map(({ text, ...rest }) => ({
                    ...rest,
                    label: text,
                }))
                this.listDeviceByEventType = this.listDevice
            })
        },

        // ===== images modal =====
        clearEventFiles() {
            this.listEventFiles = []
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

        // ===== export =====
        async exportData() {
            const vm = this

            // Area
            let allAreaName = vm.$t('Export.All')
            if (vm.searchForm.areaId && vm.searchForm.areaId.length > 0) {
                const names = []
                vm.listArea.forEach((area) => {
                    if (vm.searchForm.areaId.includes(area.id))
                        names.push(area.label)
                })
                allAreaName = names.join(',')
            }

            // Device
            let allDeviceName = vm.$t('Export.All')
            if (vm.searchForm.deviceId && vm.searchForm.deviceId.length > 0) {
                const names = []
                vm.listDevice.forEach((dv) => {
                    if (vm.searchForm.deviceId.includes(dv.id))
                        names.push(dv.label)
                })
                allDeviceName = names.join(',')
            }

            // EventType
            let eventTypeName = vm.$t('Export.All')
            if (vm.searchForm.eventTypeId != null) {
                const et = vm.listEventType.find(
                    (le) => le.id == vm.searchForm.eventTypeId
                )
                if (et)
                    eventTypeName =
                        vm.$i18n.locale === 'vi'
                            ? et.text
                            : et.englishName || et.text
            }

            // Warning level (Phân loại)
            let allWarningName = vm.$t('Export.All')
            if (
                vm.searchForm.warningLevelId &&
                vm.searchForm.warningLevelId.length > 0
            ) {
                const names = []
                vm.listWarningOptions.forEach((o) => {
                    if (vm.searchForm.warningLevelId.includes(o.id))
                        names.push(o.label)
                })
                allWarningName = names.join(',')
            }

            // Dates
            let dateFrom = moment(vm.searchForm.dateFrom).format(
                'DD/MM/YYYY HH:mm'
            )
            let dateTo = moment(vm.searchForm.dateTo).format('DD/MM/YYYY HH:mm')
            if (dateFrom === 'Invalid date') {
                dateFrom = moment('2020-01-01 00:00:00').format(
                    'DD/MM/YYYY HH:mm'
                )
            }
            if (dateTo === 'Invalid date') {
                dateTo = moment().format('DD/MM/YYYY HH:mm')
            }

            vm.headerExcelDetail = [
                vm.$t('Events.Header.Excel'),
                `${vm.$t('Events.SearchForm.FromDate')}: ${dateFrom}    ${vm.$t('Events.SearchForm.ToDate')}: ${dateTo}`,
                `${vm.$t('Events.SearchForm.Device')}: ${allDeviceName}`,
                `${vm.$t('Events.SearchForm.Area')}: ${allAreaName}`,
                `${vm.$t('Events.SearchForm.EventType')}: ${eventTypeName}`,
                `${vm.$t('Events.Table.Warning')}: ${allWarningName}`,
            ]
            vm.export_fields_vi = {
                [vm.$t('Events.Table.Time')]: 'accessTimeStr',
                [vm.$t('Events.Table.Area')]: 'areaName',
                [vm.$t('Events.Table.Device')]: 'deviceName',
                [vm.$t('Events.Table.EventType')]: 'eventTypeName',
                [vm.$t('Events.Table.Warning')]: 'warningName',
            }
            // big page fetch
            const pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'accessTime',
                sortDesc: true,
            }
            const formData =
                new URLSearchParams(pagination).toString() +
                '&' +
                new URLSearchParams(vm.searchForm).toString()

            const response = await this.$services.get(
                vm.$refs.eventTable.dataUrl + '?' + formData
            )
            return response.data.data.data
        },

        // ===== helpers =====
        formatWarningLevel(value) {
            if (!value) return this.$t('ProtectiveEquipmentType.Unknown')

            const numValue = parseInt(value, 10)
            const option = this.listWarningOptions.find((o) => o.id === numValue)
            if (option) return option.label

            if (
                !isNaN(numValue) &&
                (this.ProtectiveEquipment[numValue] ||
                    this.CarWarningType[numValue] ||
                    this.ProtectiveEquipmentEvent[numValue])
            ) {
                const key =
                    this.ProtectiveEquipment[numValue] ||
                    this.CarWarningType[numValue] ||
                    this.ProtectiveEquipmentEvent[numValue]
                return this.$t(key)
            }

            if (
                typeof value === 'string' &&
                value.startsWith('ProtectiveEquipmentType.')
            ) {
                return this.$t(value)
            }
            return this.$t('ProtectiveEquipmentType.Unknown')
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
}

.modal-80 .modal-dialog {
    max-width: 80vw !important;
    width: 80vw !important;
    height: 80vh !important;
    margin: 10vh auto;
}

.modal-80 .modal-content {
    height: 80vh !important;
    display: flex;
    flex-direction: column;
}

.modal-80 .modal-body {
    flex: 1;
    display: flex;
    overflow: hidden;
    padding: 1rem;
}

.modal-80 #carousel-example-generic {
    width: 100%;
    height: 100%;
}

.modal-80 .carousel-inner {
    height: 100%;
}

.modal-80 .carousel-item {
    height: 100%;
}
</style>
