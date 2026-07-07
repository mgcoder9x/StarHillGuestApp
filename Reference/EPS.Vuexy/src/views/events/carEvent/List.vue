<template>
    <b-container fluid class="p-0">
        <validation-observer ref="rules">
            <b-card>
                <b-form @submit.prevent="search">
                    <b-row>
                        <!-- FromDate -->
                        <b-col :md="formConfig.colMd">
                            <b-form-group
                                :label="this.$t('VehicleEvent.Field.FromDate')"
                                label-for="searchForm-dateFrom"
                                :label-cols-md="formConfig.labelColMd"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="`fromDate:` + searchForm.dateTo"
                                    name="NotificationEventTemplateName"
                                >
                                    <date-picker
                                        id="searchForm-dateFrom"
                                        v-model="searchForm.dateFrom"
                                        type="datetime"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY HH:mm:ss"
                                        value-type="YYYY-MM-DD HH:mm:ss"
                                        class="custom-date-picker w-100"
                                        :placeholder="
                                            $t(
                                                'common.form.placeholder.selectValue'
                                            )
                                        "
                                        @change="search"
                                    ></date-picker>
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <!-- LicensePlate -->
                        <b-col :md="formConfig.colMd">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.LicensePlate')"
                                :label-cols-md="formConfig.labelColMd"
                                label-for="searchForm-licensePlate"
                            >
                                <b-form-input
                                    id="searchForm-licensePlate"
                                    v-model="searchForm.licensePlate"
                                    type="text"
                                    :placeholder="
                                        $t('common.form.placeholder.enterValue')
                                    "
                                    @input="handleLicensePlateInput"
                                    @blur="handleLicensePlateBlur"
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Area -->
                        <b-col :md="formConfig.colMd">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Area')"
                                label-for="searchForm-area"
                                :label-cols-md="formConfig.labelColMd"
                            >
                                <tree-select
                                    id="searchForm-area"
                                    v-model="multiSelect.areas"
                                    :dir="isRTL ? 'rtl' : 'ltr'"
                                    :reduce="(area) => area.id"
                                    :multiple="true"
                                    :options="options.areasTree"
                                    :value-consists-of="'ALL'"
                                    :limit="3"
                                    :limit-text="(count) => `+${count}`"
                                    class="treeselect-nowrap"
                                    :placeholder="
                                        $t(
                                            'common.form.placeholder.selectValue'
                                        )
                                    "
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                        <!-- ToDate -->
                        <b-col :md="formConfig.colMd">
                            <b-form-group
                                :label="this.$t('Events.SearchForm.ToDate')"
                                label-for="searchForm-dateTo"
                                :label-cols-md="formConfig.labelColMd"
                            >
                                <date-picker
                                    id="searchForm-dateTo"
                                    v-model="searchForm.dateTo"
                                    type="datetime"
                                    :locale="currentLocale"
                                    format="DD-MM-YYYY HH:mm:ss"
                                    value-type="YYYY-MM-DD HH:mm:ss"
                                    class="custom-date-picker w-100"
                                    @change="search"
                                ></date-picker>
                            </b-form-group>
                        </b-col>
                        <!-- Merchandise -->
                        <b-col :md="formConfig.colMd">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Merchandise')"
                                :label-cols-md="formConfig.labelColMd"
                                label-for="searchForm-merchandise"
                            >
                                <v-select
                                    id="searchForm-merchandise"
                                    v-model="searchForm.warningLevelId"
                                    :options="
                                        rechangeOptions(listWarningLevels)
                                    "
                                    label="text"
                                    :clearable="true"
                                    :reduce="(item) => item.id"
                                    :dir="isRTL ? 'rtl' : 'ltr'"
                                    :placeholder="
                                        $t(
                                            'common.form.placeholder.selectValue'
                                        )
                                    "
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Status -->
                        <b-col :md="formConfig.colMd">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Status')"
                                :label-cols-md="formConfig.labelColMd"
                                label-for="searchForm-statusId"
                            >
                                <v-select
                                    id="searchForm-statusId"
                                    v-model="searchForm.status"
                                    :options="rechangeOptions(listWarningStatus)"
                                    label="text"
                                    :clearable="true"
                                    :reduce="(item) => item.id"
                                    :dir="isRTL ? 'rtl' : 'ltr'"
                                    :placeholder="
                                        $t(
                                            'common.form.placeholder.selectValue'
                                        )
                                    "
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Device -->
                        <b-col :md="formConfig.colMd">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Device')"
                                :label-cols-md="formConfig.labelColMd"
                                label-for="searchForm-device"
                            >
                                <tree-select
                                    id="searchForm-device"
                                    v-model="multiSelect.devices"
                                    :dir="isRTL ? 'rtl' : 'ltr'"
                                    multiple
                                    :clearable="true"
                                    :reduce="(device) => device.id"
                                    :multiple="true"
                                    :options="options.devices"
                                    :value-consists-of="'ALL'"
                                    :limit="3"
                                    :limit-text="(count) => `+${count}`"
                                    class="treeselect-nowrap"
                                    :placeholder="
                                        $t(
                                            'common.form.placeholder.selectValue'
                                        )
                                    "
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col :md="formConfig.colMd">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Direction')"
                                :label-cols-md="formConfig.labelColMd"
                                label-for="searchForm-direction"
                            >
                                <v-select
                                    id="searchForm-direction"
                                    v-model="searchForm.direction"
                                    :options="rechangeOptions(listDirection)"
                                    label="text"
                                    :reduce="(item) => item.id"
                                    :dir="isRTL ? 'rtl' : 'ltr'"
                                    :placeholder="
                                        $t(
                                            'common.form.placeholder.selectValue'
                                        )
                                    "
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card>
        </validation-observer>
        <!-- Table -->
        <b-card title="">
            <div class="text-left mb-1 d-flex" style="gap: 8px">
                <b-button
                    v-waves
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="$t('CarEvent.Common.Header.Excel')"
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
                ref="carEventTable"
                :columns="table.columns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="carEventTableConfig"
                :initial-page="initialPage"
                :initial-items-per-page="initialItemsPerPage"
                @pagination="getPagination"
            >
                <template slot="table-row" slot-scope="props">
                    <span
                        v-if="props.column.field == 'deviceId'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ $t(props.row.deviceName) }}
                    </span>

                    <!-- Column: Area -->
                    <span
                        v-else-if="props.column.field == 'areaid'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ $t(props.row.areaName) }}
                    </span>

                    <span v-else-if="props.column.field == 'eventTypeId'">
                        {{ getEventTypeName(props.row.eventTypeId) }}
                    </span>
                    <!-- Column: Merchandise -->
                    <span
                        v-else-if="props.column.field == 'warningLevelTitle'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getWarningLevelTitle(props.row.warningLevelId) }}
                    </span>
                    <span
                        v-else-if="props.column.field == 'direction'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getDirection(props.row.direction) }}
                    </span>
                    <!-- Column: Plan Cargo Code - Show multiple plans warning -->
                    <span
                        v-else-if="props.column.field == 'cargoCode'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        <b-badge
                            v-if="props.row.hasMultiplePlans"
                            variant="warning"
                            v-b-tooltip.hover
                            :title="getAllPlansTooltip(props.row)"
                        >
                            {{ $t('categories.tosSyncInfo.multiplePlans') }}
                        </b-badge>
                        <div v-else class="d-flex align-items-center">
                            <span>{{ props.row.cargoCode || '-' }}</span>
                            <b-badge
                                v-if="
                                    props.row.isPlanDirectionMismatch ||
                                    props.row.isPlanMismatch
                                "
                                variant="danger"
                                class="ml-1"
                                v-b-tooltip.hover
                                :title="props.row.mismatchReason"
                            >
                                <feather-icon
                                    icon="AlertTriangleIcon"
                                    size="14"
                                />
                            </b-badge>
                        </div>
                    </span>
                    <!-- Column: Status -->
                    <!-- <span
                        v-else-if="props.column.field == 'warningLevelLevel'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getStatusTitle(props.row.warningLevelId) }}
                    </span> -->
                    <span
                        v-else-if="props.column.field == 'statusName'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getStatusTitle(props.row.status,props.row.warningLevelId) }}
                    </span>
                    <!-- Column: licensePlateImage -->
                    <span
                        v-else-if="props.column.field == 'licensePlateImage'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        <img
                            :src="`${baseURL}${props.row.licensePlateImage}`"
                            loading="lazy"
                            alt="Image"
                            style="
                                width: 60px;
                                height: 80px;
                                object-fit: cover;
                                border-radius: 0.3em;
                            "
                            class="cursor-pointer"
                            @click="showModalEvent(props.row.eventId)"
                        />
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <div
                            class="text-nowrap"
                            style="display: flex; justify-content: center"
                        >
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/event/carEvent/Detail/${props.row.eventId}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                        </div>
                    </span>
                    <!-- Column: Image -->
                    <span
                        v-else-if="props.column.field == 'image'"
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
                            style="
                                width: 60px;
                                height: 80px;
                                object-fit: cover;
                                border-radius: 0.3em;
                            "
                            class="cursor-pointer"
                            @click="showModalEvent(props.row.eventId)"
                        />
                    </span>
                </template>
            </BasicTable>
        </b-card>
        <!-- Image Slider -->
        <b-modal
            v-model="modalImgEventShow"
            :title="$t('VehicleEvent.Field.Image')"
            size="lg"
        >
            <EventCarousel
                :list-event-files="listEventFiles"
                :base-u-r-l="baseURL"
            />
            <template #modal-footer>
                <p></p>
            </template>
        </b-modal>
    </b-container>
</template>
<script>
import {
    ref,
    reactive,
    computed,
    watch,
    getCurrentInstance,
    nextTick,
    onMounted,
} from '@vue/composition-api'
import debounce from 'lodash/debounce'
import { $themeConfig } from '@themeConfig'
import {
    listBlackList,
    listNumberOfSeat,
    listVehicleType,
    listColor,
    listBackgroundColor,
    listCarType,
} from '@/data'
import TreeHelper from '@/utils/treeHelper'
import { directions, accessTypes } from '@/constants'
import { lookupService } from '@/services'
import { setStorage, getStorage } from '@/utils/cacheHelper'
import EVENT_TYPE from '@/constants/eventType'
import useJwt from '@/auth/jwt/useJwt'
import { CarWarningType, CarStatus, Direction } from './warningLevelData'

export default {
    name: 'CarEventList',
    setup() {
        const vm = getCurrentInstance().proxy
        // computed
        const currentLocale = computed(() => vm.$i18n.locale)
        const baseURL = computed(() => process.env.VUE_APP_BASE_URL)
        // ===== data =====
        const storageKey = 'carEventsCache'
        const searchCache = getStorage(storageKey)

        const searchFormBlank = {
            licensePlate: null,
            warningLevelId: null,
            status: null,
            dateFrom: vm.$moment().format('YYYY-MM-DD 00:00:00'),
            dateTo: vm.$moment().format('YYYY-MM-DD 23:59:59'),
            vehicleType: null,
            manufacturers: null,
            direction: null,
            deviceId: null,
            areaId: null,
            eventTypeIds: null,
        }
        const multiSelectBlank = {
            devices: [],
            areas: [],
        }

        const searchForm = reactive(
            searchCache ? { ...searchCache.searchForm } : { ...searchFormBlank }
        )
        const multiSelect = reactive(
            searchCache
                ? { ...searchCache.multiSelect }
                : { ...multiSelectBlank }
        )

        const page = ref(searchCache ? searchCache?.pagination?.page : 1)
        const itemsPerPage = ref(
            searchCache ? searchCache?.pagination?.itemsPerPage : 10
        )
        const initialPage = page.value
        const initialItemsPerPage = itemsPerPage.value

        const isRTL = computed(() => vm.$store?.state?.appConfig?.isRTL)

        // Helper function để map text -> label đệ quy cho tree structure
        const mapTextToLabel = (nodes) => {
            if (!Array.isArray(nodes)) return []
            return nodes.map((node) => {
                const newNode = { ...node }
                // Map text sang label
                if (newNode.text) {
                    newNode.label = newNode.text
                } else if (
                    !newNode.label &&
                    (newNode.name || newNode.deviceName)
                ) {
                    newNode.label = newNode.name || newNode.deviceName
                } else if (!newNode.label && newNode.id) {
                    newNode.label = String(newNode.id)
                }
                // Xử lý đệ quy cho children
                if (
                    Array.isArray(newNode.children) &&
                    newNode.children.length > 0
                ) {
                    newNode.children = mapTextToLabel(newNode.children)
                }
                return newNode
            })
        }

        const options = reactive({
            devices: [],
            areasTree: [],
            // Dùng vm.$t để map label (vue-i18n v8)
            // cargoStatus: cargoStatus.map((item) => ({
            //     id: item.key,
            //     label: vm.$t(`constants.${item.value}`),
            // })),
        })
        const formConfig = reactive({
            colMd: 4,
            labelColMd: 4,
            labelAlign: 'left',
        })
        const table = reactive({
            dataUrl: '/carEvent',
            columns: [
                { label: 'VehicleEvent.Field.Date', field: 'accessDateStr' },
                { label: 'VehicleEvent.Field.Time', field: 'accessTimeStr' },
                {
                    label: 'VehicleEvent.Field.LicensePlate',
                    field: 'licensePlate',
                },
                {
                    label: 'VehicleEvent.Field.Merchandise',
                    field: 'warningLevelTitle',
                },
                {
                    label: 'Hàng kế hoạch',
                    field: 'cargoCode',
                },
                {
                    label: 'VehicleEvent.Field.Area',
                    field: 'areaid',
                },
                {
                    label: 'VehicleEvent.Field.DeviceGateType',
                    field: 'deviceId',
                },
                {
                    label: 'VehicleEvent.Field.Direction',
                    field: 'direction',
                },
                {
                    label: 'VehicleEvent.Field.Status',
                    field: 'statusName',
                },
                {
                    label: 'VehicleEvent.Field.Image',
                    field: 'image',
                },
                {
                    label: 'VehicleEvent.Field.Operation',
                    field: 'action',
                },
            ],
        })

        const carEventTable = ref(null)
        // ===== methods =====
        function cacheSearchCondition() {
            setStorage(
                storageKey,
                {
                    searchForm,
                    multiSelect,
                    pagination: {
                        page: page.value,
                        itemsPerPage: itemsPerPage.value,
                    },
                },
                3000
            )
        }

        function searchNow() {
            cacheSearchCondition()
            carEventTable.value?.refresh?.()
        }

        // Debounce KHÔNG lệ thuộc "this" → tránh lỗi mất ngữ cảnh
        const search = debounce(() => {
            searchNow()
        }, 1000)

        function handleLicensePlateInput(value) {
            // Allow typing but delay search
            search()
        }

        function handleLicensePlateBlur(event) {
            // Trim whitespace when user leaves the field
            const trimmedValue = event.target.value
                ? event.target.value.trim()
                : event.target.value
            searchForm.licensePlate = trimmedValue
            search()
        }

        async function refresh() {
            Object.assign(searchForm, searchFormBlank)
            Object.assign(multiSelect, multiSelectBlank)
            await nextTick()
            searchNow()
        }

        function getPagination(pagination) {
            page.value = pagination.page
            itemsPerPage.value = pagination.itemsPerPage
            cacheSearchCondition()
        }
        // ===== hooks =====

        watch(
            () => multiSelect.devices,
            (newValue) => {
                if (newValue.length > 0) {
                    searchForm.deviceId = newValue.join(',')
                } else {
                    searchForm.deviceId = null
                }
            },
            { deep: true, immediate: true }
        )

        watch(
            () => multiSelect.areas,
            (newValue) => {
                if (newValue.length > 0) {
                    searchForm.areaId = newValue.join(',')
                } else {
                    searchForm.areaId = null
                }
            },
            { deep: true, immediate: true }
        )
        // ===========================================
        const { apiURL } = $themeConfig.app
        const listManufacturer = ref([])
        const listArea = ref([])
        const listDevice = ref([])
        const listEventTypes = ref([])
        const targetIds = [303, 304, 305, 311, 312] // lấy id hàng hóa
        const listWarningLevels = reactive(
            // TreeHelper.removeEmptyChildren(CarWarningType)
            CarWarningType.filter((item) => targetIds.includes(item.id)).map(
                (item) => ({
                    id: item.id,
                    value: item.value ?? item.id,
                    label: item.label,
                })
            )
        )
        const listWarningStatus = reactive(
            TreeHelper.removeEmptyChildren(CarStatus)
        )
        const listDirection = reactive(
            TreeHelper.removeEmptyChildren(Direction)
        )
        onMounted(async () => {
            const [rAreasTree, rManufacturer, rAreas, rEventTypes] =
                await Promise.allSettled([
                    lookupService.getAreasTree(),
                    lookupService.getManufacturer(),
                    lookupService.getAreas(),
                    lookupService.getEventTypes(),
                ])
            // const [rDevices, rAreasTree, rManufacturer, rAreas, rEventTypes] =
            //     await Promise.allSettled([
            //         lookupService.getDevices(),
            //         lookupService.getAreasTree(),
            //         lookupService.getManufacturer(),
            //         lookupService.getAreas(),
            //         lookupService.getEventTypes(),
            //     ])
            const rDevices = await useJwt.get('/lookup/devices?eventTypeId=800')
            if (rDevices.status === 200) {
                // Map đệ quy text -> label cho tree structure
                const devicesData = rDevices.data.data || []
                options.devices = mapTextToLabel(devicesData)
                console.log('devices', options.devices)
                listDevice.value = options.devices
            } else {
                console.error('getDevices failed:', rDevices.data.message)
                options.devices = []
                listDevice.value = []
            }

            if (rAreasTree.status === 'fulfilled') {
                // Map đệ quy text -> label cho areas tree
                options.areasTree = mapTextToLabel(rAreasTree.value || [])
                console.log('options.areasTree', options.areasTree)
            } else {
                console.error('getAreasTree failed:', rAreasTree.reason)
                options.areasTree = []
            }

            if (rManufacturer.status === 'fulfilled') {
                listManufacturer.value = rManufacturer.value
            } else {
                console.error('getManufacturer failed:', rManufacturer.reason)
                listManufacturer.value = []
            }

            if (rAreas.status === 'fulfilled') {
                listArea.value = rAreas.value
            } else {
                console.error('getAreas failed:', rAreas.reason)
                listArea.value = []
            }

            if (rEventTypes.status === 'fulfilled') {
                listEventTypes.value = (rEventTypes.value || []).filter((x) => {
                    const id = Number(x?.id)
                    return id >= 800 && id < 900
                })
            } else {
                console.error('getEventTypes failed:', rEventTypes.reason)
                listEventTypes.value = []
            }
        })

        // Get all plans tooltip text
        function getAllPlansTooltip(row) {
            if (!row.cargoCodeMultiple) return ''
            try {
                const plans = JSON.parse(row.cargoCodeMultiple)
                return `${vm.$t(
                    'categories.tosSyncInfo.allPlans'
                )}: ${plans.join(', ')}`
            } catch {
                return row.cargoCodeMultiple
            }
        }

        return {
            searchForm,
            multiSelect,
            page,
            itemsPerPage,
            initialPage,
            initialItemsPerPage,
            options,
            table,
            isRTL,
            carEventTable,
            search,
            refresh,
            getPagination,
            handleLicensePlateInput,
            handleLicensePlateBlur,
            formConfig,
            currentLocale,
            baseURL,

            apiURL,
            listBlackList,
            listNumberOfSeat,
            listVehicleType,
            listCarType,
            listBackgroundColor,
            listColor,
            listManufacturer,
            listDevice,
            directions,
            accessTypes,
            listArea,
            listEventTypes,
            listWarningLevels,
            listWarningStatus,
            listDirection,
            getAllPlansTooltip,
        }
    },
    data() {
        return {
            // searchForm: {
            //     licensePlate: null,
            //     warningLevelId: null,
            //     status: null,
            //     dateFrom: null,
            //     dateTo: null,
            //     vehicleType: null,
            //     manufacturers: null,
            //     direction: null,
            //     deviceId: null,
            //     areaId: null,
            //     eventTypeIds: null,
            // },
            form: {
                colMd: 4,
                labelColsMd: 4,
            },
            modalImgEventShow: false,
            listDeviceByEventType: [],
            listEventFiles: [],
            headerExcelDetail: [],
            export_fields_vi: {
                Ngày: 'accessDateStr',
                Giờ: 'accessTimeStr',
                'Biển số xe': 'licensePlate',
                'Màu xe': 'colorStr',
                'Loại phương tiện': 'vehicleTypeStr',
                'Khu vực': 'areaName',
                'Loại truy cập': 'violationStr',
                'Số ghế ngồi': 'totalNumberOfSeats',
                'Hành vi': 'eventTypeStr',
                'Hàng hóa': 'warningLevelTitle',
                'Trạng thái': 'statusName',
                Hướng: 'directionStr',
            },
            // columns: [
            //     {
            //         label: 'VehicleEvent.Field.Date',
            //         field: 'accessDateStr',
            //     },
            //     {
            //         label: 'VehicleEvent.Field.Time',
            //         field: 'accessTimeStr',
            //     },
            //     {
            //         label: 'VehicleEvent.Field.LicensePlate',
            //         field: 'licensePlate',
            //     },
            //     {
            //         label: 'VehicleEvent.Field.Merchandise',
            //         field: 'warningLevelTitle',
            //     },
            //     {
            //         label: 'VehicleEvent.Field.Area',
            //         field: 'areaid',
            //     },
            //     {
            //         label: 'VehicleEvent.Field.DeviceGateType',
            //         field: 'deviceId',
            //     },
            //     {
            //         label: 'VehicleEvent.Field.Direction',
            //         field: 'direction',
            //     },
            //     {
            //         label: 'VehicleEvent.Field.Status',
            //         field: 'warningLevelLevel',
            //     },
            //     {
            //         label: 'VehicleEvent.Field.Image',
            //         field: 'image',
            //     },
            //     {
            //         label: 'VehicleEvent.Field.Operation',
            //         field: 'action',
            //     },
            // ],
        }
    },
    // created() {
    //     this.loadDevice()
    //     // const accessToken = this.$services.getUserData()
    //     // const userId = accessToken.userId
    //     // const cacheKey = `vehicleEventSearchForm_${userId}`
    //     // const searchForm = getStorage(cacheKey)

    //     // if (searchForm) {
    //     //     Object.keys(searchForm).forEach((item) => {
    //     //         this.searchForm[item] = searchForm[item]
    //     //     })
    //     //     // this.searchForm = { ...searchForm }
    //     //     this.search()
    //     // } else {
    //     //     this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
    //     // }
    // },
    // computed: {
    //     currentLocale() {
    //         return this.$i18n.locale
    //     },
    //     baseURL() {
    //         // return 'this.apiURL'
    //         return process.env.VUE_APP_BASE_URL
    //     },
    // },
    methods: {
        // search() {
        //     setStorage('vehicleEventSearchForm', this.searchForm, 120)
        //     this.$refs.vehicleEventTable.refresh()
        // },
        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.modalImgEventShow = true
        },
        rechangeOptions(dataList) {
            return dataList.map((item) => ({
                ...item,
                text: this.$t(item.label),
            }))
        },
        // loadDevice() {
        //     this.$services.get('/lookup/devices').then((response) => {
        //         let devices = TreeHelper.removeEmptyChildren(response.data.data)
        //         this.listDevice = devices.map((item) => {
        //             const { text, ...rest } = item
        //             return {
        //                 ...rest,
        //                 label: text,
        //             }
        //         })
        //         this.listDeviceByEventType = this.listDevice
        //     })
        // },
        getColorName(colorId) {
            let colorName = ''
            this.listColor.forEach((color) => {
                if (color.id === colorId) {
                    colorName = this.$t(color.text)
                }
            })
            return colorName
        },
        getVehicleTypeName(vehicleTypeId) {
            let vehicleTypeName = ''
            this.listVehicleType.forEach((vehicleType) => {
                if (vehicleType.id === vehicleTypeId) {
                    vehicleTypeName = this.$t(vehicleType.text)
                }
            })
            return vehicleTypeName
        },
        getDirection(directionId) {
            let vehicleTypeName = ''
            this.listDirection.forEach((vehicleType) => {
                if (vehicleType.id === directionId) {
                    vehicleTypeName = this.$t(vehicleType.label)
                }
            })
            return vehicleTypeName
        },
        getEventTypeName(eventTypeId) {
            const eventType = this.listEventTypes.find(
                (x) => x.id === eventTypeId
            )
            return eventType ? this.$t(eventType.text.split('_')[1]) : ''
        },
        getWarningLevelTitle(warningLevelId) {
            const warningLevel = this.listWarningLevels.find(
                (x) => x.id === warningLevelId
            )
            return warningLevel ? this.$t(warningLevel.label) : null
        },
        
        getStatusTitle(status,warningLevelId) {
            let statusName = this.listWarningStatus.find(
                    (x) => x.id === warningLevelId
                )
            if(statusName){
                return this.$t(statusName.label)
            }
            else{
                statusName = this.listWarningStatus.find(
                    (x) => x.id === status
                )
                return statusName ? this.$t(statusName.label) : null
            }
        },

        async exportData() {
            const vm = this

            const areaName = vm.searchForm.areaId
                ? vm.searchForm.areaId
                      .split(',')
                      .map((areaId) => {
                          const area = vm.listArea.find((x) => x.id === areaId)
                          return area ? area.text : ''
                      })
                      .join(', ')
                : vm.$t('Export.All')
            const deviceName = vm.searchForm.deviceId
                ? vm.searchForm.deviceId
                      .split(',')
                      .map((deviceId) => {
                          const device = vm.listDevice.find(
                              (x) => x.id === deviceId
                          )
                          return device ? device.text : ''
                      })
                      .join(', ')
                : vm.$t('Export.All')

            const allLicenseName = vm.searchForm.licensePlate
                ? vm.searchForm.licensePlate
                : vm.$t('Export.All')

            let directionName = vm.searchForm.direction
                ? vm.listDirection.find((x) => x.id === vm.searchForm.direction)
                      ?.label
                : 'Export.All'
            directionName = vm.$t(directionName)

            let dateFrom = this.$moment(this.searchForm.dateFrom).format(
                'DD/MM/YYYY HH:mm'
            )
            let dateTo = this.$moment(this.searchForm.dateTo).format(
                'DD/MM/YYYY HH:mm'
            )
            if (dateFrom === 'Invalid date') {
                dateFrom = this.$moment('2020-01-01 00:00:00').format(
                    'DD/MM/YYYY HH:mm'
                )
            }
            if (dateTo === 'Invalid date') {
                dateTo = this.$moment().format('DD/MM/YYYY HH:mm')
            }
            if (vm.searchForm.licensePlate === null) {
                vm.searchForm.licensePlate = ''
            }

            let warningLevelName = vm.searchForm.warningLevelId
                ? vm.listWarningLevels.find(
                      (x) => x.id === vm.searchForm.warningLevelId
                  )?.label
                : 'Export.All'
            warningLevelName = vm.$t(warningLevelName)

            // let statusName = vm.searchForm.status
            //     ? vm.listWarningStatus.find(
            //           (x) =>
            //               x.id === vm.searchForm.status ||
            //               (Array.isArray(x.value) &&
            //                   x.value.includes(vm.searchForm.warningLevelId))
            //       )?.label
            //     : 'Export.All'
            // statusName = vm.$t(statusName)
            let statusName = vm.$t('Export.All')

            if (vm.searchForm.warningLevelId) {
                statusName = vm.getStatusTitle(vm.searchForm.status,vm.searchForm.warningLevelId)
            }
            vm.headerExcelDetail = [
                vm.$t('CarEvent.Common.Header.Excel'),
                `${vm.$t(
                    'VehicleEvent.Field.FromDate'
                )}: ${dateFrom}      ${vm.$t(
                    'VehicleEvent.Field.ToDate'
                )}: ${dateTo}`,
                `${vm.$t('VehicleEvent.Field.Area')}: ${areaName}`,
                // `${vm.$t('VehicleEvent.Field.Merchandise')}: ${warningLevelName}`,
                `${vm.$t('VirtualFenceEvent.Field.LicensePlate')}: ${allLicenseName}`,
                `${vm.$t(
                    'VehicleEvent.Field.Merchandise'
                )}: ${warningLevelName}`,
                `${vm.$t('VehicleEvent.Field.Status')}: ${statusName}`,
                `${vm.$t('VehicleEvent.Field.DeviceGateType')}: ${deviceName}`,
                `${vm.$t('VehicleEvent.Field.Direction')}: ${directionName}`,
            ]

            // Define export fields to match table columns
            vm.export_fields_vi = {
                [vm.$t('VehicleEvent.Field.Date')]: 'accessDateStr',
                [vm.$t('VehicleEvent.Field.Time')]: 'accessTimeStr',
                [vm.$t('VehicleEvent.Field.LicensePlate')]: 'licensePlate',
                [vm.$t('VehicleEvent.Field.Merchandise')]: 'warningLevelTitle',
                [vm.$t('VehicleEvent.Field.Area')]: 'areaName',
                [vm.$t('VehicleEvent.Field.DeviceGateType')]: 'deviceName',
                [vm.$t('VehicleEvent.Field.Direction')]: 'directionStr',
                [vm.$t('VehicleEvent.Field.Status')]: 'statusTitle',
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
                `${vm.$refs.carEventTable.dataUrl}?${formData}`
            )

            for (const item of response.data.data.data) {
                item.accessDateStr = this.$moment(item.accessTime).format(
                    'DD/MM/YYYY'
                )
                item.accessTimeStr = this.$moment(item.accessTime).format(
                    'HH:mm:ss'
                )
                item.licensePlate = item.licensePlate || ''
                item.warningLevelTitle =
                    vm.getWarningLevelTitle(item.warningLevelId) || ''
                item.areaName = vm.$t(item.areaName) || ''
                item.deviceName = vm.$t(item.deviceName) || ''
                item.directionStr = vm.getDirection(item.direction) || ''
                item.statusTitle = vm.getStatusTitle(item.status, item.warningLevelId) || ''
            }

            return response.data.data.data
        },
        loadEventFiles(eventId) {
            this.$services
                .get(`/event/eventFilesById/${eventId}`)
                .then((response) => {
                    this.listEventFiles = response.data
                })
        },
        getDirectionName(directionId) {
            const direction = this.directions.find((x) => x.id === directionId)
            return direction ? this.$t(direction.text) : ''
        },
        getAccessTypeName(violationId) {
            const accessType = this.accessTypes.find(
                (x) => x.id === violationId
            )
            return accessType ? this.$t(accessType.text) : ''
        },
    },
}
</script>
<style lang="scss">
@import '@/assets/scss/_custom-tree-select.scss';
</style>
