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
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('VehicleEvent.Field.FromDate')"
                                    label-for="h-searchForm-dateFrom"
                                    :label-cols-md="form.labelColsMd"
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
                                        />
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <!-- LicensePlate -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="
                                        $t('VehicleEvent.Field.LicensePlate')
                                    "
                                    :label-cols-md="form.labelColsMd"
                                    label-for="h-searchForm-licensePlate"
                                >
                                    <b-form-input
                                        id="h-searchForm-licensePlate"
                                        v-model="searchForm.licensePlate"
                                        type="text"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Cột 3 gom: VehicleType + Direction (nhỏ) -->
                            <b-col :md="form.colMd">
                                <b-row class="compact-row">
                                    <b-col cols="6">
                                        <b-form-group
                                            :label="
                                                $t(
                                                    'VehicleEvent.Field.Direction'
                                                )
                                            "
                                            :label-cols-md="6"
                                            class="mb-0 compact-group"
                                        >
                                            <!-- HƯỚNG -->
                                            <v-select
                                                class="vs-one-line"
                                                v-model="searchForm.direction"
                                                :dir="
                                                    $store.state.appConfig.isRTL
                                                        ? 'rtl'
                                                        : 'ltr'
                                                "
                                                label="text"
                                                :reduce="(d) => d.id"
                                                :options="
                                                    rechangeDerectionOptions(
                                                        directions
                                                    )
                                                "
                                                :clearable="true"
                                                :searchable="false"
                                                @input="search"
                                            />
                                        </b-form-group>
                                    </b-col>
                                    <b-col cols="6">
                                        <b-form-group
                                            :label="
                                                $t(
                                                    'VehicleEvent.Field.VehicleType'
                                                )
                                            "
                                            :label-cols-md="4"
                                            class="mb-0 compact-group"
                                        >
                                            <v-select
                                                v-model="searchForm.vehicleType"
                                                :dir="
                                                    $store.state.appConfig.isRTL
                                                        ? 'rtl'
                                                        : 'ltr'
                                                "
                                                label="text"
                                                :reduce="
                                                    (vehicleType) =>
                                                        vehicleType.id
                                                "
                                                :options="
                                                    rechangeOptions(
                                                        listVehicleType
                                                    )
                                                "
                                                :clearable="true"
                                                @input="search"
                                            />
                                        </b-form-group>
                                    </b-col>
                                </b-row>
                            </b-col>
                        </b-row>
                        <!-- ROW 2: Đến ngày | Loại truy cập (multi) | Màu xe -->
                        <b-row>
                            <!-- ToDate -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('VehicleEvent.Field.ToDate')"
                                    label-for="h-searchForm-dateTo"
                                    :label-cols-md="form.labelColsMd"
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

                            <!-- AccessType -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('VehicleEvent.Field.AccessType')"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <v-select
                                        v-model="searchForm.accessTypeIds"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(item) => item.id"
                                        :options="listAccessType"
                                        :multiple="true"
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        valueConsistsOf="ALL"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Color (Màu xe) -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('VehicleEvent.Field.Color')"
                                    label-for="h-searchForm-color"
                                    :label-cols-md="3"
                                >
                                    <v-select
                                        v-model="searchForm.color"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(color) => color.id"
                                        :options="rechangeOptions(listColor)"
                                        :clearable="true"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <!-- ROW 3: Khu vực | Thiết bị | Hãng xe -->
                        <b-row>
                            <!-- Area -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('VehicleEvent.Field.Area')"
                                    label-for="h-searchForm-area"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <tree-select
                                        v-model="searchForm.areaIds"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(area) => area.id"
                                        :multiple="true"
                                        :options="areaTree"
                                        :value-consists-of="'ALL'"
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        valueConsistsOf="ALL"
                                        @input="changeArea"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Device -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('Events.SearchForm.Device')"
                                    label-for="h-searchForm-device"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <tree-select
                                        v-model="searchForm.deviceIds"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(device) => device.id"
                                        :options="listDeviceByAreaId"
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

                            <!-- Manufacturer -->
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="
                                        $t('VehicleEvent.Field.Manufacturer')
                                    "
                                    label-for="h-searchForm-manufacturer"
                                    :label-cols-md="3"
                                >
                                    <v-select
                                        v-model="searchForm.manufacturers"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="
                                            (manufacturer) => manufacturer.id
                                        "
                                        :options="
                                            rechangeOptions(listManufacturer)
                                        "
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
                        :name="$t('VehicleEvent.Field.Header')"
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
                ref="vehicleEventTable"
                :columns="columns"
                data-url="/vehicleEvent"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="vehicelEventTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Color -->
                    <span
                        v-if="props.column.field == 'colorStr'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getColorName(props.row.color) }}
                    </span>
                    <!-- Column: VehicleType -->
                    <span
                        v-else-if="props.column.field == 'vehicleType'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getVehicleTypeName(props.row.vehicleType) }}
                    </span>
                    <!-- Column: deptTypeStr -->
                    <span
                        v-else-if="props.column.field == 'deptTypeStr'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getDeptTypeName(props.row.deptType) }}
                    </span>
                    <!-- Column: deptTypeStr -->
                    <span
                        v-else-if="props.column.field == 'departmentId'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getDeptName(props.row.departmentId) }}
                    </span>
                    <!-- Column: CarType -->
                    <!-- <span
                        v-else-if="props.column.field == 'carType'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ $t(props.row.carTypeStr) }}
                    </span> -->
                    <!-- Column: Area -->
                    <span
                        v-else-if="props.column.field == 'areaId'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ props.row.areaName }}
                    </span>
                    <!-- Column: Device -->
                    <span
                        v-else-if="props.column.field == 'deviceId'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ props.row.deviceName }}
                    </span>
                    <!-- Column: Direction -->
                    <span
                        v-else-if="props.column.field == 'direction'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getDirectionName(props.row.direction) }}
                    </span>
                    <span
                        v-else-if="props.column.field == 'violation'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getAccessTypeName(props.row.violation) }}
                    </span>
                    <span v-else-if="props.column.field == 'eventTypeId'">
                        {{ getEventTypeName(props.row.eventTypeId) }}
                    </span>
                    <!-- Column: Manufacturer -->
                    <!-- <span
                        v-else-if="props.column.field == 'manufacturer'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ getManufacturerName(props.row.manufacturer) }}
                    </span> -->
                    <!-- Column: IsBlackList -->
                    <!-- <span
                        v-else-if="props.column.field == 'isBlackList'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ $t(props.row.isBlackListStr) }}
                    </span> -->
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
                        <div class="center-icon text-nowrap">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                @click="goToDetail(props.row.eventId)"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="
                                    props.row.licensePlates != null &&
                                    props.row.licensePlates != 'EMPTY'
                                "
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="
                                    $t('route.breadcrumb.dashboardItinerary')
                                "
                                :to="{
                                    path: `/event/vehicleEvent/itinerary/2/${props.row.licensePlates}/${searchForm.dateFrom}/${searchForm.dateTo}`,
                                }"
                            >
                                <Icon icon="mdi:map" class="xs-icon" />
                            </b-button>
                        </div>
                    </span>
                </template>
            </BasicTable>
        </b-card>
        <!-- Image Slider -->
        <b-modal
            v-model="modalImgEventShow"
            :title="$t('Events.SearchForm.EventPhotos')"
            hide-footer
            modal-class="modal-80"
            @hidden="clearEventFiles"
        >
            <EventCarousel
                :list-event-files="listEventFiles"
                :base-u-r-l="baseURL"
            />
            <template #modal-footer>
                <p></p>
            </template>
        </b-modal>
    </div>
</template>
<script>
/* eslint-disable */
import { $themeConfig } from '@themeConfig'
import {
    listBlackList,
    listNumberOfSeat,
    listVehicleType,
    listColor,
    listBackgroundColor,
    listCarType,
} from '@/data'
import { directions, accessTypes } from '@/constants'
import { lookupService } from '@/services'
import { onMounted, ref } from '@vue/composition-api'
import moment from 'moment'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import { ValidationObserver, ValidationProvider } from 'vee-validate'
import { FILETYPE, FILETYPE_NAME } from '@/constants/fileType'
import { nextTick } from 'vue'
const isDevEnv = process.env.NODE_ENV == 'development'
export default {
    components: { ValidationObserver, ValidationProvider },
    setup() {
        // App Name
        const listManufacturer = ref([])
        const areaTree = ref([])
        const listArea = ref([])
        const listDevice = ref([])
        const listEventTypes = ref([])
        const listDeviceByAreaId = ref([])

        onMounted(async () => {
            listManufacturer.value = await lookupService.getManufacturer()
            areaTree.value = await lookupService.getAreasTree()
            listArea.value = await lookupService.getAreas()
            listDevice.value = await lookupService.getDevices()
            if (listDevice.value.length > 0) {
                listDevice.value = listDevice.value
                    .filter(
                        (x) =>
                            parseInt(x.eventTypeId) >= 300 &&
                            parseInt(x.eventTypeId) < 400
                    )
                    .map((item) => ({
                        ...item,
                        label: item.text,
                    }))
                listDeviceByAreaId.value = listDevice.value
            }
            listEventTypes.value = await lookupService.getEventTypes()
            if (listEventTypes.value.length > 0) {
                listEventTypes.value = listEventTypes.value.filter(
                    (x) => parseInt(x.id) >= 300 && parseInt(x.id) < 400
                )
            }
        })

        return {
            listBlackList,
            listNumberOfSeat,
            listVehicleType,
            listCarType,
            listBackgroundColor,
            listColor,
            listManufacturer,
            areaTree,
            listDevice,
            directions,
            accessTypes,
            listArea,
            listEventTypes,
            listDeviceByAreaId,
        }
    },
    data() {
        return {
            FILETYPE,
            FILETYPE_NAME,
            form: {
                colMd: 4,
                labelColsMd: 4,
            },
            modalImgEventShow: false,
            searchForm: {
                licensePlate: null,
                dateFrom: null,
                dateTo: null,
                bgColor: null,
                carType: null,
                vehicleType: null,
                manufacturers: null,
                color: null,
                numberOfSeat: null,
                isBlackList: null,
                deviceIds: null,
                areaIds: null,
                eventTypeIds: null,
            },
            headerExcelDetail: [],
            export_fields_vi: {
                Ngày: 'accessDateStr',
                Giờ: 'accessTimeStr',
                'Biển số xe': 'licensePlates',
                'Chủ sở hữu': 'fullname',
                'Loại phương tiện': 'vehicleTypeStr',
                'Phân loại': 'deptType',
                'Phòng ban/Nhà thầu': 'departmentId',
                'Khu vực': 'areaName',
                'Thiết bị': 'deviceName',
                Hướng: 'directionStr',
                'Loại truy cập': 'violationStr',
                'Số ghế ngồi': 'totalNumberOfSeats',
                'Hành vi': 'eventTypeStr',
            },
            columns: [
                {
                    label: 'VehicleEvent.Field.DateColumn',
                    field: 'accessDateStr',
                },
                {
                    label: 'VehicleEvent.Field.Time',
                    field: 'accessTimeStr',
                },
                {
                    label: 'VehicleEvent.Field.LicensePlate',
                    field: 'licensePlates',
                },
                {
                    label: 'VehicleEvent.Field.Owner',
                    field: 'fullname',
                },
                {
                    label: 'VehicleEvent.Field.VehicleType',
                    field: 'vehicleType',
                },
                {
                    label: 'VehicleEvent.Field.Classify',
                    field: 'deptTypeStr',
                },
                {
                    label: 'VehicleEvent.Field.DeptOrContractor',
                    field: 'departmentId',
                },
                {
                    label: 'VehicleEvent.Field.Area',
                    field: 'areaId',
                },
                {
                    label: 'VehicleEvent.Field.Device',
                    field: 'deviceId',
                },
                {
                    label: 'VehicleEvent.Field.Direction',
                    field: 'direction',
                },
                {
                    label: 'VehicleEvent.Field.AccessType',
                    field: 'violation',
                },
                {
                    label: 'VehicleEvent.Field.EventType',
                    field: 'eventTypeId',
                },
                {
                    label: 'VehicleEvent.Field.Manufacturer',
                    field: 'manufacturerName',
                },
                {
                    label: 'VehicleEvent.Field.Color',
                    field: 'colorStr',
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
            listEventFiles: [],
            listDeptType: [
                {
                    text: this.$t(
                        'common.options.departmentVehicleType.departmentVehicle'
                    ),
                    id: 1,
                },
                {
                    text: this.$t(
                        'common.options.departmentVehicleType.contractorVehicle'
                    ),
                    id: 2,
                },
            ],
            listDept: [],
            listAccessType: [
                { id: 0, text: this.$t('accessType.allow') },
                { id: 1, text: this.$t('accessType.deny') },
                { id: 2, text: this.$t('accessType.noPermission') },
                { id: 3, text: this.$t('accessType.expired') },
                { id: 4, text: this.$t('accessType.wrongArea') },
                { id: 5, text: this.$t('accessType.wrongTime') },
            ],
            // Defined options select
        }
    },
    async created() {
        const accessToken = this.$services.getUserData()
        debugger
        this.searchForm.compId = accessToken.companyId
        this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
        await this.loadDepartments()
        this.searchForm.compId = accessToken.companyId

        const searchForm1 = getStorage('vehicleEventSearchForm')
        if (searchForm1) {
            this.searchForm = { ...searchForm1 }
        } else {
            this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
            this.$nextTick(() => {
                this.$refs.rules.validate()
            })
        }
        this.$nextTick(() => {
            this.search()
        })
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        baseURL() {
            return process.env.VUE_APP_BASE_URL
            //return "Https://demo.atin.vn/Service"
        },
    },
    methods: {
        goToDetail(eventId) {
            this.$router.push({
                path: `/event/vehicleEvent/Detail/${eventId}`,
                query: { from: 'Screen1' }
            });
        },
        clearEventFiles() {
            this.listEventFiles = [] // clear khi modal đóng
        },
        changeArea() {
            this.searchForm.deviceIds = null
            if (
                this.searchForm.areaIds != null &&
                this.searchForm.areaIds.length > 0
            ) {
                this.listDeviceByAreaId = this.listDevice.filter((x) =>
                    this.searchForm.areaIds.includes(x.areaId)
                )
            } else {
                this.listDeviceByAreaId = this.listDevice
            }
            this.search()
        },
        refresh() {
            clearStorage('vehicleEventSearchForm')
            this.searchForm = {
                licensePlate: null,
                dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                dateTo: null,
                carType: null,
                vehicleType: null,
                deviceId: null,
                areaIds: null,
                eventTypeIds: null,
            }
            this.$nextTick(() => {
                this.$refs.rules.validate()
                this.$refs.vehicleEventTable.refresh()
            })
        },
        async loadDepartments() {
            try {
                const response = await this.$services.get('/lookup/departments')
                this.listDept = response.data.data
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.LU_DEPARTMENTS_500`)}`,
                    },
                })
            }
        },
        //search method
        async search() {
            const ok = await this.$refs.rules.validate()
            if (!ok) return
            setStorage('vehicleEventSearchForm', this.searchForm, 120)
            this.$refs.vehicleEventTable.refresh()
        },
        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.modalImgEventShow = true
        },
        rechangeOptions(dataList) {
            return dataList.map((item) => ({
                ...item,
                text: this.$t(item.text),
            }))
        },
        rechangeDerectionOptions(dataList) {
            return dataList.map((item) => ({
                ...item,
                text: this.$t(`direction.${item.text}`),
            }))
        },
        getColorName(colorId) {
            let colorName = ''
            this.listColor.forEach((color) => {
                if (color.id == colorId) {
                    colorName = this.$t(color.text)
                }
            })
            return colorName
        },
        getDeptTypeName(deptTypeId) {
            let deptTypeName = ''
            this.listDeptType.forEach((dept) => {
                if (dept.id == deptTypeId) {
                    deptTypeName = this.$t(dept.text)
                }
            })
            return deptTypeName
        },
        getDeptName(deptId) {
            let deptName = ''
            this.listDept.forEach((dept) => {
                if (dept.id == deptId) {
                    deptName = this.$t(dept.text)
                }
            })
            return deptName
        },
        getManufacturerName(manufacturerId) {
            let manufacturerName = ''
            this.listManufacturer.forEach((manufacturer) => {
                if (manufacturer.id == manufacturerId) {
                    manufacturerName = this.$t(manufacturer.text)
                }
            })
            return manufacturerName
        },
        getVehicleTypeName(vehicleTypeId) {
            debugger
            let vehicleTypeName = ''
            this.listVehicleType.forEach((vehicleType) => {
                if (vehicleType.id == vehicleTypeId) {
                    vehicleTypeName = this.$t(vehicleType.text)
                }
            })
            return vehicleTypeName
        },
        getDirectionName(directionId) {
            const direction = this.directions.find(
                (x) => x.value == directionId
            )
            return direction ? this.$t(`direction.${direction.text}`) : ''
        },
        getAccessTypeName(accessTypeId) {
            const accessType = this.accessTypes.find(
                (x) => x.value == accessTypeId
            )
            return accessType ? this.$t(`accessType.${accessType.text}`) : ''
        },
        getEventTypeName(eventTypeId) {
            const eventType = this.listEventTypes.find(
                (x) => x.id == eventTypeId
            )
            return eventType ? this.$t(eventType.text.split('_')[1]) : ''
        },
        async exportData() {
            var vm = this
            let areaName
            if (
                vm.searchForm.areaIds != null &&
                vm.searchForm.areaIds.length > 0
            ) {
                const listAreaName = vm.searchForm.areaIds.map((areaId) => {
                    const area = vm.listArea.find((x) => x.id == areaId)
                    return area ? area.text : ''
                })
                areaName = listAreaName.join(', ')
            } else {
                areaName = vm.$t('Export.All')
            }

            let vehicleTypeName
            if (vm.searchForm.vehicleType != null) {
                vm.listVehicleType.forEach((vehicleType) => {
                    if (vehicleType.id == vm.searchForm.vehicleType) {
                        vehicleTypeName = vm.$t(vehicleType.text)
                    }
                })
            } else {
                vehicleTypeName = vm.$t('Export.All')
            }

            let deviceName
            debugger
            if (
                vm.searchForm.deviceIds != null &&
                vm.searchForm.deviceIds.length > 0
            ) {
                vm.listDevice.forEach((dv) => {
                    if ((dv.Id = vm.searchForm.deviceIds)) {
                        deviceName = dv.text
                    }
                })
            } else {
                deviceName = vm.$t('Export.All')
            }

            let eventTypeName
            if (vm.searchForm.eventTypeIds != null) {
                const listEventTypeName = vm.searchForm.eventTypeIds.map(
                    (eventTypeId) => {
                        const eventType = vm.listEventTypes.find(
                            (x) => x.id == eventTypeId
                        )
                        return eventType
                            ? vm.$t(eventType.text.split('_')[1])
                            : ''
                    }
                )
                eventTypeName = listEventTypeName.join(', ')
            } else {
                eventTypeName = vm.$t('Export.All')
            }

            var allAccessTypeName = vm.$t('Export.All')
            let accessTypeName = []
            if (
                vm.searchForm.accessTypeIds != null &&
                vm.searchForm.accessTypeIds != [] &&
                vm.searchForm.accessTypeIds.length > 0
            ) {
                vm.listAccessType.forEach((at) => {
                    if (vm.searchForm.accessTypeIds.includes(at.id)) {
                        accessTypeName.push(at.text)
                    }
                })
                allAccessTypeName = accessTypeName.join(',')
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
            if (vm.searchForm.licensePlate == null) {
                vm.searchForm.licensePlate = ''
            }

            let manufacturersName = []
            if (
                vm.searchForm.manufacturers != null &&
                vm.searchForm.manufacturers.length > 0
            ) {
                vm.searchForm.manufacturers.forEach((manufacturersId) => {
                    vm.listManufacturer.forEach((manufacturers) => {
                        if (manufacturers.id == manufacturersId) {
                            manufacturersName.push(manufacturers.text)
                        }
                    })
                })
            } else {
                manufacturersName.push(vm.$t('Export.All'))
            }
            if (manufacturersName.length == 0) {
                manufacturersName.push(vm.$t('Export.All'))
            }
            let manufacturersStr = manufacturersName.join(', ')

            vm.headerExcelDetail = [
                vm.$t('VehicleEvent.Field.Header'),
                `${vm.$t('VehicleEvent.Field.FromDate')}: ${dateFrom}     ${vm.$t('VehicleEvent.Field.ToDate')}: ${dateTo}`,
                `${vm.$t('VehicleEvent.Field.VehicleType')}: ${vehicleTypeName}`,
                `${vm.$t('VehicleEvent.Field.AccessType')} : ${allAccessTypeName}`,
                `${vm.$t('VehicleEvent.Field.Device')}: ${deviceName}`,
                `${vm.$t('VehicleEvent.Field.Area')}: ${areaName}`,
                `${
                    vm.searchForm.licensePlate
                        ? vm.$t('VehicleEvent.Field.LicensePlate') +
                          ' : ' +
                          vm.searchForm.licensePlate
                        : ''
                }`,
            ]
            vm.export_fields_vi = {
                [vm.$t('VehicleEvent.Field.Date')]: 'accessDateStr',
                [vm.$t('VehicleEvent.Field.Time')]: 'accessTimeStr',
                [vm.$t('VehicleEvent.Field.LicensePlate')]: 'licensePlates',
                [vm.$t('VehicleEvent.Field.Owner')]: 'fullname',
                [vm.$t('VehicleEvent.Field.VehicleType')]: 'vehicleTypeStr',
                [vm.$t('VehicleEvent.Field.Classify')]: 'deptType',
                [vm.$t('VehicleEvent.Field.DeptOrContractor')]: 'departmentId',
                [vm.$t('VehicleEvent.Field.Area')]: 'areaName',
                [vm.$t('VehicleEvent.Field.Device')]: 'deviceName',
                [vm.$t('VehicleEvent.Field.AccessType')]: 'violationStr',
                [vm.$t('VehicleEvent.Field.EventType')]: 'eventTypeStr',
                [vm.$t('VehicleEvent.Field.Direction')]: 'directionStr',
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
                vm.$refs.vehicleEventTable.dataUrl + '?' + formData
            )
            for (let i = 0; i < response.data.data.data.length; i++) {
                response.data.data.data[i].vehicleTypeStr =
                    vm.getVehicleTypeName(
                        response.data.data.data[i].vehicleType
                    )

                response.data.data.data[i].typeName = vm.$t(
                    response.data.data.data[i].typeName
                )
                response.data.data.data[i].areaName = vm.$t(
                    response.data.data.data[i].areaName
                )
                response.data.data.data[i].deptType = vm.getDeptTypeName(
                    response.data.data.data[i].deptType
                )
                response.data.data.data[i].departmentId = vm.getDeptName(
                    response.data.data.data[i].departmentId
                )

                response.data.data.data[i].violationStr = vm.getAccessTypeName(
                    response.data.data.data[i].violation
                )
                response.data.data.data[i].eventTypeStr = vm.getEventTypeName(
                    response.data.data.data[i].eventTypeId
                )

                response.data.data.data[i].directionStr = vm.getDirectionName(
                    response.data.data.data[i].direction
                )
            }
            return response.data.data.data
        },
        //Load data lookup
        loadEventFiles(eventId) {
            this.$services
                .get(`/event/eventFilesById/${eventId}`)
                .then(async (response) => {
                    const files = response.data
                    const result = await Promise.all(
                        files.map((item) => {
                            const fullSrc = this.baseURL + item.filePath
                            if (
                                item.fileType === FILETYPE.LICENSE_PLATE_IMAGE
                            ) {
                                return new Promise((resolve) => {
                                    const img = new Image()
                                    img.src = fullSrc
                                    img.onload = () => {
                                        resolve({
                                            ...item,
                                            width: img.naturalWidth,
                                            height: img.naturalHeight,
                                        })
                                    }
                                    img.onerror = () => {
                                        // fallback nếu load ảnh lỗi
                                        resolve({
                                            ...item,
                                            width: 0,
                                            height: 0,
                                        })
                                    }
                                })
                            } else {
                                return Promise.resolve(item)
                            }
                        })
                    )
                    this.listEventFiles = result
                })
        },
    },
}
</script>

<style lang="scss">
@import '@/assets/scss/_custom-tree-select.scss';

.compact-row {
    margin-left: -6px !important;
    margin-right: -6px !important;
    > .col-6 {
        padding-left: 6px !important;
        padding-right: 6px !important;
    }
}
.compact-group {
    .v-select,
    .vs__dropdown-toggle {
        min-height: 36px;
    }
    .form-group {
        margin-bottom: 0.25rem;
    }
}
/* Chốt 1 dòng tuyệt đối cho vue-select */
.vs-one-line {
    .vs__dropdown-toggle {
        display: flex !important;
        align-items: center !important;
        flex-wrap: nowrap !important; /* KHÔNG cho wrap */
        min-height: 36px;
        height: 36px;
    }

    /* Vùng text được chọn */
    .vs__selected-options {
        display: flex !important;
        align-items: center !important;
        flex: 1 1 auto !important;
        min-width: 0 !important;
        flex-wrap: nowrap !important; /* KHÔNG cho wrap */
        overflow: hidden !important;
    }

    .vs__selected {
        max-width: 100% !important;
        white-space: nowrap !important;
        overflow: hidden !important;
        text-overflow: ellipsis !important;
        margin-right: 4px !important;
        line-height: 34px; /* can giữa theo chiều dọc */
    }

    /* Ẩn hoàn toàn search input để khỏi chiếm chỗ */
    .vs__search {
        flex: 0 0 0 !important;
        width: 0 !important;
        min-width: 0 !important;
        padding: 0 !important;
        margin: 0 !important;
        border: 0 !important;
    }

    /* (x) và caret giữ nguyên 1 cột bên phải, không co giãn */
    .vs__actions {
        flex: 0 0 auto !important;
        align-self: center !important;
    }
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
