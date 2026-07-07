<!-- src/views/events/virtualFenceEvent/List.vue -->
<template>
    <div>
        <b-card no-body>
            <b-card-body>
                <!-- Search Form -->
                <validation-observer ref="rules">
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
                                            class="custom-date-picker"
                                            @change="search"
                                        />
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
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
                                        @input="changeArea"
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

                            <!-- ĐỐI TƯỢNG (Person/Vehicle) -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'VirtualFenceEvent.SearchForm.ObjectType'
                                        )
                                    "
                                    label-for="h-searchForm-objectType"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        id="h-searchForm-objectType"
                                        v-model="searchForm.objectType"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(o) => o.id"
                                        :options="objectTypeOptions"
                                        placeholder=""
                                        @input="onObjectTypeChange"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- PHÂN LOẠI (Registered/Unregistered) -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'VirtualFenceEvent.SearchForm.Category'
                                        )
                                    "
                                    label-for="h-searchForm-category"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        id="h-searchForm-category"
                                        v-model="searchForm.personType"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(item) => item.id"
                                        :options="listCategory"
                                        placeholder=""
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>

                            <!-- Persons (tên) -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'VirtualFenceEvent.SearchForm.Person'
                                        )
                                    "
                                    label-for="h-searchForm-person"
                                    label-cols-md="3"
                                >
                                    <b-form-input
                                        id="h-searchForm-person"
                                        v-model.trim="searchForm.personName"
                                        @keyup.enter="search"
                                        @input="onPersonInput"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </b-form>
                </validation-observer>

                <!-- Face toggle -->
                <b-row class="align-items-center mt-1">
                    <b-col md="4">
                        <b-form-group
                            :label="
                                $t(
                                    'categories.employees.common.form.label.findFace'
                                )
                            "
                            label-for="h-face-toggle"
                            label-cols-md="4"
                            class="mb-0 d-flex align-items-center"
                        >
                            <b-form-checkbox
                                id="h-face-toggle"
                                v-model="showFaceSearch"
                                switch
                                size="sm"
                                class="mb-0"
                                style="
                                    display: flex;
                                    align-items: center;
                                    justify-content: left;
                                    padding-bottom: 5px;
                                    margin-top: 0;
                                "
                            />
                        </b-form-group>
                    </b-col>
                </b-row>

                <!-- Face search advanced -->
                <b-collapse v-model="showFaceSearch">
                    <b-row no-gutters class="align-items-stretch">
                        <!-- ẢNH -->
                        <b-col md="4" class="pr-md-50 mb-75">
                            <div class="face-dropzone">
                                <div class="dropzone-inner">
                                    <b-img
                                        v-if="faceImagePreview"
                                        :src="faceImagePreview"
                                        class="preview-img shadow-sm"
                                        alt="Ảnh khuôn mặt"
                                        fluid
                                    />
                                    <div v-else class="preview-placeholder">
                                        <Icon
                                            icon="mdi:account-circle"
                                            class="avatar-xxl"
                                        />
                                        <small class="text-muted mt-25">{{
                                            $t('')
                                        }}</small>
                                    </div>
                                </div>
                                <b-form-file
                                    v-model="faceFile"
                                    accept="image/*"
                                    size="sm"
                                    browse-text="Browse"
                                    drop-placeholder="Kéo file vào đây"
                                    class="mt-50"
                                    @change="handleFileUpload"
                                />
                            </div>
                        </b-col>

                        <!-- SLIDER -->
                        <b-col md="4" class="pl-md-50" style="padding: 25px">
                            <div class="face-slider w-100">
                                <div
                                    class="d-flex align-items-center justify-content-between mb-25"
                                >
                                    <span class="slider-label mb-0">{{
                                        $t(
                                            'categories.employees.common.form.label.pointIdentify'
                                        )
                                    }}</span>
                                    <span class="slider-value"
                                        >{{ identifyPercent }}%</span
                                    >
                                </div>
                                <input
                                    v-model.number="identifyScore"
                                    type="range"
                                    min="0"
                                    max="1"
                                    step="0.01"
                                    class="range"
                                />
                                <div
                                    class="d-flex justify-content-between mt-25 text-muted xsmall"
                                >
                                    <span>0%</span><span>50%</span
                                    ><span>100%</span>
                                </div>
                                <small class="text-muted d-block mt-25">
                                    {{
                                        $t(
                                            'categories.employees.common.form.label.filterResult'
                                        )
                                    }}
                                    ≥ {{ identifyPercent }}%.
                                </small>
                            </div>
                        </b-col>
                    </b-row>
                </b-collapse>
            </b-card-body>
        </b-card>

        <!-- Table + actions -->
        <b-card title="">
            <div class="text-left mb-1 d-flex" style="gap: 8px">
                <b-button
                    v-waves
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="$t('VirtualFenceEvent.Header.Excel')"
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
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                    variant="primary"
                    @click="refresh"
                >
                    <Icon icon="famicons:enter-outline" class="sm-icon" />
                    <span class="ml-25">{{ $t('Button.Refresh') }}</span>
                </b-button>
            </div>

            <BasicTable
                ref="eventTable"
                :columns="columns"
                data-url="/virtualFenceEvents"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="virtualFenceEventTable"
            >
                <template #table-row="props">
                    <!-- Loại đối tượng -->
                    <span v-if="props.column.field === 'objectType'">
                        <div class="center-icon text-nowrap">
                            {{ toObjectTypeText(props.row.troubleTypeStr) }}
                        </div>
                    </span>

                    <!-- Phân loại -->
                    <span v-else-if="props.column.field === 'category'">
                        <div class="center-icon text-nowrap">
                            {{
                                props.row.personType === 1
                                    ? $t(
                                          'VirtualFenceEvent.PersonType.Registered'
                                      )
                                    : $t(
                                          'VirtualFenceEvent.PersonType.Unregistered'
                                      )
                            }}
                        </div>
                    </span>

                    <!-- Mã/Biển số -->
                    <span v-else-if="props.column.field === 'codeOrPlate'">
                        <div class="center-icon text-nowrap">
                            <template
                                v-if="isVehicle(props.row.troubleTypeStr)"
                            >
                                {{ props.row.licensePlate || '—' }}
                            </template>
                            <template v-else>
                                <span v-if="props.row.personType === 1">
                                    {{
                                        props.row.employeeCode ||
                                        props.row.personCode ||
                                        props.row.userCode ||
                                        props.row.userName ||
                                        '—'
                                    }}
                                </span>
                                <span v-else>—</span>
                            </template>
                        </div>
                    </span>

                    <!-- Device -->
                    <span v-else-if="props.column.field === 'deviceId'">
                        <div class="center-icon">
                            {{ props.row.deviceName }}
                        </div>
                    </span>

                    <!-- Area -->
                    <span v-else-if="props.column.field === 'areaId'">
                        <div class="center-icon">
                            {{ getStaticName(props.row.areaId, lstAllArea) }}
                        </div>
                    </span>

                    <!-- TroubleType -->
                    <span v-else-if="props.column.field === 'troubleType'">
                        <div class="center-icon">
                            {{ $t(props.row.troubleTypeStr) }}
                        </div>
                    </span>

                    <!-- UserName -->
                    <span v-else-if="props.column.field === 'userName'">
                        <div class="center-icon text-nowrap">
                            {{
                                props.row.userName ||
                                $t('VirtualFenceEvent.Unknown') ||
                                'Unknown'
                            }}
                        </div>
                    </span>

                    <!-- PersonType (nếu còn dùng ở excel) -->
                    <span v-else-if="props.column.field === 'personType'">
                        <div class="center-icon text-nowrap">
                            {{
                                props.row.personType === 1
                                    ? $t(
                                          'VirtualFenceEvent.PersonType.Registered'
                                      )
                                    : $t(
                                          'VirtualFenceEvent.PersonType.Unregistered'
                                      )
                            }}
                        </div>
                    </span>

                    <!-- Image -->
                    <span v-else-if="props.column.field === 'image'">
                        <div class="center-icon">
                            <img
                                :src="`${baseURL}${props.row.filePath}`"
                                loading="lazy"
                                width="100"
                                alt="Image"
                                class="cursor-pointer"
                                @click="showModalEvent(props.row.eventId)"
                            />
                        </div>
                    </span>

                    <!-- Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <div
                            class="center-icon text-nowrap"
                            style="display: flex; justify-content: center"
                        >
                            <b-button
                                v-if="
                                    !authorize ||
                                    authorize(['ViewVirtualFenceEvent'])
                                "
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                @click="goToDetail(props.row.eventId)"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                        </div>
                    </span>

                    <!-- Mặc định -->
                    <span v-else>
                        {{ props.formattedRow[props.column.field] }}
                    </span>
                </template>
            </BasicTable>
        </b-card>

        <!-- Image Slider -->
        <!-- Image Slider -->
        <b-modal
            v-model="modalImgEventShow"
            :title="$t('WaterEvents.Label.Image')"
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
import moment from 'moment'
import { extend, ValidationObserver, ValidationProvider } from 'vee-validate'
import { lookupService } from '@/services'
import { reactive, ref, onMounted } from '@vue/composition-api'
import treeHelper from '@/utils/treeHelper'
import { TROUBLE_TYPES } from '../virtualFenceEvent/data'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

const FACE_PERSON_API = '/employees/face-person-search'

// (Rule fromDate có thể bật lại nếu cần)

export default {
    components: { ValidationObserver, ValidationProvider },
    setup() {
        const { VUE_APP_BASE_URL: baseURL } = process.env

        const searchForm = reactive({
            licensePlate: '',
            troubleType: null, // (giữ cho API)
            objectType: null, // (UI) 1=Person, 2=Vehicle
            dateFrom: null,
            dateTo: null,
            areaId: [],
            deviceId: [],
            personType: null, // 0/1
            personName: null,
            personId: '',
            filterCompId: null,
        })

        const modalImgEventShow = ref(false)
        const listEventFiles = ref([])
        const lstAllArea = ref([])

        const listTroubleType = reactive(
            treeHelper.removeEmptyChildren(TROUBLE_TYPES)
        )

        onMounted(async () => {
            lstAllArea.value = await lookupService.getAreas()
        })

        return {
            baseURL,
            searchForm,
            modalImgEventShow,
            listEventFiles,
            listTroubleType,
            lstAllArea,
        }
    },

    data() {
        return {
            headerExcelDetail: [],
            export_fields_vi: {},
            columns: [
                {
                    label: 'VirtualFenceEvent.Field.Date',
                    field: 'accessDateStr',
                },
                {
                    label: 'VirtualFenceEvent.Field.Time',
                    field: 'accessTimeStr',
                },
                { label: 'VirtualFenceEvent.Field.Area', field: 'areaId' },
                { label: 'VirtualFenceEvent.Field.Device', field: 'deviceId' },

                // MỚI
                {
                    label: 'VirtualFenceEvent.Field.ObjectType',
                    field: 'objectType',
                },
                {
                    label: 'VirtualFenceEvent.Field.Category',
                    field: 'category',
                },
                {
                    label: 'VirtualFenceEvent.Field.CodeOrPlate',
                    field: 'codeOrPlate',
                },

                // Cũ (giữ lại nếu cần đối chiếu)
                {
                    label: 'VirtualFenceEvent.Field.TroubleType',
                    field: 'troubleType',
                },
                {
                    label: 'VirtualFenceEvent.Field.UserName',
                    field: 'userName',
                },
                { label: 'VirtualFenceEvent.Field.Image', field: 'image' },
                { label: 'VirtualFenceEvent.Field.Operation', field: 'action' },
            ],

            listArea: [],
            listDevice: [],
            listDeviceByAreaId: [],
            listPerson: [],
            showFaceSearch: false,
            personDebTimer: null,
            faceFile: null,
            faceImagePreview: '',
            identifyScore: 0.85,
            debounceTimer: null,
        }
    },

    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        objectTypeOptions() {
            return [
                { id: 1, text: this.$t('VirtualFenceEvent.ObjectType.Person') },
                {
                    id: 2,
                    text: this.$t('VirtualFenceEvent.ObjectType.Vehicle'),
                },
            ]
        },
        listCategory() {
            return [
                {
                    id: 1,
                    text: this.$t('VirtualFenceEvent.PersonType.Registered'),
                },
                {
                    id: 0,
                    text: this.$t('VirtualFenceEvent.PersonType.Unregistered'),
                },
            ]
        },
        identifyPercent() {
            return Math.round((this.identifyScore || 0) * 100)
        },

        // // ✅ MỚI: bỏ 1 video cuối nếu bị thừa
        // filteredEventFiles() {
        //     const files = this.listEventFiles || []
        //     if (!files.length) return files

        //     const isVideo = (f) =>
        //         f && f.filePath && /\.(mp4|avi|mov|mkv|webm)$/i.test(f.filePath)

        //     // Tìm tất cả index là video
        //     const videoIndexes = files
        //         .map((f, i) => (isVideo(f) ? i : -1))
        //         .filter((i) => i !== -1)

        //     // Nếu có từ 2 video trở lên -> bỏ video cuối
        //     if (videoIndexes.length > 1) {
        //         const lastVideoIndex = videoIndexes[videoIndexes.length - 1]
        //         return files.filter((_, idx) => idx !== lastVideoIndex)
        //     }

        //     // Không có hoặc chỉ 1 video -> giữ nguyên
        //     return files
        // },
    },

    watch: {
        identifyScore() {
            if (!this.showFaceSearch || !this.faceImagePreview) return
            this.searchForm.personId = ''
            clearTimeout(this.debounceTimer)
            this.debounceTimer = setTimeout(() => this.search(), 250)
            this.$nextTick(() => {
                const el = document.querySelector('input[type="range"].range')
                if (el)
                    el.style.setProperty(
                        '--value',
                        `${this.identifyScore * 100}%`
                    )
            })
        },
        showFaceSearch(val) {
            if (!val) {
                this.faceFile = null
                this.faceImagePreview = ''
                this.searchForm.personId = ''
                this.identifyScore = 0.85
                this.$nextTick(() => this.search())
            }
        },
    },

    created() {
        this.loadArea()
        this.loadDevice()
        this.loadPerson()

        const saved = getStorage('virtualFenceEventSearchForm')
        if (saved) {
            Object.assign(this.searchForm, saved)
        } else {
            this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
            this.$nextTick(() => {
                this.$refs.rules.validate()
            })
        }
    },

    mounted() {
        this.updateRangeBackground()
    },
    updated() {
        this.updateRangeBackground()
    },

    methods: {
        goToDetail(eventId) {
            this.$router.push({
                path: `/event/virtualFenceEvent/Detail/${eventId}`,
                query: { from: 'Screen1' },
            })
        },
        // helpers cho cột mới
        isVehicle(troubleTypeStr) {
            return String(troubleTypeStr || '')
                .toUpperCase()
                .includes('VEHICLE')
        },
        toObjectTypeText(troubleTypeStr) {
            return this.isVehicle(troubleTypeStr)
                ? this.$t('VirtualFenceEvent.ObjectType.Vehicle')
                : this.$t('VirtualFenceEvent.ObjectType.Person')
        },

        onObjectTypeChange(val) {
            // Đồng bộ về troubleType cho API: 1 = PERSON, 2 = VEHICLE
            this.searchForm.troubleType = val === 2 ? 2 : val === 1 ? 1 : null
            this.search()
        },

        onPersonInput() {
            clearTimeout(this.personDebTimer)
            this.personDebTimer = setTimeout(() => {
                this.search()
            }, 300)
        },
        loadPerson() {
            this.$services.get('/lookup/employees').then((response) => {
                const items = treeHelper.removeEmptyChildren(response.data.data)
                this.listPerson = items.map(({ text, ...rest }) => ({
                    ...rest,
                    label: text,
                }))
            })
        },
        clearEventFiles() {
            this.listEventFiles = []
        },
        getStaticName(id, lst) {
            const item = (lst || []).find((x) => String(x.id) === String(id))
            return item ? item.text : id
        },
        loadArea() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.listArea = treeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
        loadDevice() {
            this.$services.get('/lookup/devices').then((response) => {
                const devices = treeHelper.removeEmptyChildren(
                    response.data.data
                )
                // VirtualFence eventTypeId = 400
                this.listDevice = devices
                    .filter((x) => x.eventTypeId == 400)
                    .map(({ text, ...rest }) => ({ ...rest, label: text }))
                this.listDeviceByAreaId = this.listDevice
            })
        },

        refresh() {
            clearStorage('virtualFenceEventSearchForm')
            this.searchForm = Object.assign(this.searchForm, {
                licensePlate: '',
                troubleType: null,
                objectType: null,
                dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                dateTo: null,
                areaId: [],
                deviceId: [],
                personType: null,
                personName: null,
                personId: '',
            })
            this.listDeviceByAreaId = this.listDevice
            this.$nextTick(() => {
                this.$refs.rules.validate()
                this.$refs.eventTable.refresh()
            })
        },

        async exportData() {
            const vm = this
            // dates
            let dateFrom = moment(vm.searchForm.dateFrom).format(
                'DD/MM/YYYY HH:mm'
            )
            let dateTo = moment(vm.searchForm.dateTo).format('DD/MM/YYYY HH:mm')
            if (dateFrom === 'Invalid date')
                dateFrom = moment('2020-01-01 00:00:00').format(
                    'DD/MM/YYYY HH:mm'
                )
            if (dateTo === 'Invalid date')
                dateTo = moment().format('DD/MM/YYYY HH:mm')

            // area names
            let allAreaName = vm.$t('Export.All')
            if (vm.searchForm.areaId && vm.searchForm.areaId.length > 0) {
                const areaNames = []
                vm.lstAllArea.forEach((a) => {
                    if (vm.searchForm.areaId.map(String).includes(String(a.id)))
                        areaNames.push(a.text)
                })
                allAreaName = areaNames.join(',')
            }

            // device names
            let allDeviceName = vm.$t('Export.All')
            if (vm.searchForm.deviceId && vm.searchForm.deviceId.length > 0) {
                const names = []
                vm.listDevice.forEach((d) => {
                    if (vm.searchForm.deviceId.includes(d.id))
                        names.push(d.label)
                })
                allDeviceName = names.join(',')
            }

            // object type
            let allObjectType = vm.$t('Export.All')
            if (vm.searchForm.objectType != null) {
                allObjectType =
                    vm.searchForm.objectType === 2
                        ? vm.$t('VirtualFenceEvent.ObjectType.Vehicle')
                        : vm.$t('VirtualFenceEvent.ObjectType.Person')
            }

            // category
            let allCategory = vm.$t('Export.All')
            if (vm.searchForm.personType != null) {
                allCategory =
                    vm.searchForm.personType == 1
                        ? vm.$t('VirtualFenceEvent.PersonType.Registered')
                        : vm.$t('VirtualFenceEvent.PersonType.Unregistered')
            }

            // personName
            let allPersonName = vm.$t('Export.All')
            if (vm.searchForm.personName)
                allPersonName = vm.searchForm.personName

            vm.headerExcelDetail = [
                vm.$t('VirtualFenceEvent.Header.Excel'),
                `${vm.$t('Events.SearchForm.FromDate')}: ${dateFrom}    ${vm.$t('Events.SearchForm.ToDate')}: ${dateTo}`,
                `${vm.$t('Events.SearchForm.Device')}: ${allDeviceName}`,
                `${vm.$t('Events.SearchForm.Area')}: ${allAreaName}`,
                `${vm.$t('VirtualFenceEvent.SearchForm.ObjectType')}: ${allObjectType}`,
                `${vm.$t('VirtualFenceEvent.SearchForm.Category')}: ${allCategory}`,
                `${vm.$t('VirtualFenceEvent.SearchForm.Person')}: ${allPersonName}`,
            ]

            vm.export_fields_vi = {
                [vm.$t('VirtualFenceEvent.Field.Date')]: 'accessDateStr',
                [vm.$t('VirtualFenceEvent.Field.Time')]: 'accessTimeStr',
                [vm.$t('VirtualFenceEvent.Field.Area')]: 'areaId',
                [vm.$t('VirtualFenceEvent.Field.Device')]: 'deviceName',
                // excel thêm 3 cột mới
                [vm.$t('VirtualFenceEvent.Field.ObjectType')]: 'objectTypeStr',
                [vm.$t('VirtualFenceEvent.Field.Category')]: 'categoryStr',
                [vm.$t('VirtualFenceEvent.Field.CodeOrPlate')]:
                    'codeOrPlateStr',
                // cũ
                [vm.$t('VirtualFenceEvent.Field.UserName')]: 'userName',
            }

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
                `${vm.$refs.eventTable.dataUrl}?${formData}`
            )
            const rows = response?.data?.data?.data || []

            rows.forEach((r) => {
                r.areaId = vm.getStaticName(r.areaId, vm.lstAllArea)
                r.userName =
                    r.userName ||
                    vm.$t('VirtualFenceEvent.Unknown') ||
                    'Unknown'
                const isVeh = vm.isVehicle(r.troubleTypeStr)
                r.objectTypeStr = isVeh
                    ? vm.$t('VirtualFenceEvent.ObjectType.Vehicle')
                    : vm.$t('VirtualFenceEvent.ObjectType.Person')
                r.categoryStr =
                    r.personType == 1
                        ? vm.$t('VirtualFenceEvent.PersonType.Registered')
                        : vm.$t('VirtualFenceEvent.PersonType.Unregistered')
                r.codeOrPlateStr = isVeh
                    ? r.licensePlate || '—'
                    : r.personType == 1
                      ? r.employeeCode ||
                        r.personCode ||
                        r.userCode ||
                        r.userName ||
                        '—'
                      : '—'
            })
            return rows
        },

        rechangeOptions(dataList) {
            return dataList?.map((item) => ({
                ...item,
                label: this.$t(item.label),
            }))
        },

        changeArea() {
            this.searchForm.deviceId = []
            if (this.searchForm.areaId && this.searchForm.areaId.length > 0) {
                this.listDeviceByAreaId = this.listDevice.filter((x) =>
                    this.searchForm.areaId.includes(x.areaId)
                )
            } else {
                this.listDeviceByAreaId = this.listDevice
            }
            this.search()
        },

        async search() {
            if (this.showFaceSearch && this.faceImagePreview)
                await this.ensureFaceEmployeeIds()
            const ok = this.$refs.rules.validate()
            if (!ok) return
            setStorage('virtualFenceEventSearchForm', this.searchForm, 120)
            this.$refs.eventTable && this.$refs.eventTable.refresh()
        },

        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.modalImgEventShow = true
        },

        loadEventFiles(eventId) {
            this.$services
                .get(`/event/eventFilesById/${eventId}`)
                .then((res) => {
                    this.listEventFiles = res.data
                })
        },

        handleFileUpload(e) {
            const file = e?.target?.files?.[0] || this.faceFile
            this.faceImagePreview = ''
            if (file) {
                const reader = new FileReader()
                reader.onload = (evt) => {
                    this.faceImagePreview = String(evt.target.result)
                    this.searchForm.personId = ''
                    this.search()
                }
                reader.onerror = () => {
                    this.$bvToast.toast(
                        'Không thể đọc file, vui lòng thử lại.',
                        {
                            title: this.$t('Error.Error'),
                            variant: 'danger',
                            solid: true,
                        }
                    )
                }
                reader.readAsDataURL(file)
            }
        },

        async ensureFaceEmployeeIds() {
            if (!this.showFaceSearch || !this.faceImagePreview) {
                this.searchForm.personId = ''
                return true
            }
            if (this.searchForm.personId) return true
            try {
                const comIds = this.searchForm.filterCompId
                    ? [Number(this.searchForm.filterCompId)]
                    : []
                const res = await this.$services.post(FACE_PERSON_API, {
                    faceImageBase64: this.faceImagePreview,
                    identifyScore: this.identifyScore ?? 0.8,
                    quality: 0.2,
                    numResult: 500,
                    comIds,
                })
                const personIds = res?.data?.data?.personIds || []
                const MAX = 5000
                this.searchForm.personId =
                    personIds.length === 0
                        ? '00000000-0000-0000-0000-000000000000'
                        : personIds.slice(0, MAX).join(',')

                const top = res?.data?.data?.matches?.[0]
                if (top) {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: 'Face match',
                            text: `Top: ${top.fullname || top.personCode} (${Math.round((top.score || 0) * 100)}%)`,
                            icon: 'UserIcon',
                            variant: 'info',
                        },
                    })
                }
                return true
            } catch (e) {
                console.error('Face search error:', e)
                this.$bvToast.toast('Tìm kiếm khuôn mặt lỗi!', {
                    title: this.$t('Error.Error') || 'Lỗi',
                    variant: 'danger',
                    solid: true,
                })
                this.searchForm.personId =
                    '00000000-0000-0000-0000-000000000000'
                return true
            }
        },

        updateRangeBackground() {
            this.$nextTick(() => {
                const el = document.querySelector('input[type="range"].range')
                if (el)
                    el.style.setProperty(
                        '--value',
                        `${this.identifyScore * 100}%`
                    )
            })
        },
        nomalizeDate(data) {
            return (data || '').replace(/,/g, ', ')
        },
    },

    beforeDestroy() {
        clearTimeout(this.personDebTimer)
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

/* Face UI */
.face-dropzone {
    padding: 0.5rem;
    border: 1px dashed rgba(24, 144, 255, 0.28);
    border-radius: 10px;
    background: linear-gradient(180deg, #f9fbff 0%, #fff 100%);
}
.dropzone-inner {
    min-height: 200px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #fff;
    border: 1px dashed rgba(24, 144, 255, 0.22);
    border-radius: 8px;
}
.preview-img {
    max-height: 192px;
    object-fit: contain;
    object-position: center;
    border-radius: 8px;
}
.preview-placeholder {
    display: flex;
    flex-direction: column;
    align-items: center;
    color: #98a2b3;
}
.avatar-xxl {
    width: 84px;
    height: 84px;
    opacity: 0.45;
}
.face-slider {
    padding: 0.75rem;
    border: 1px dashed rgba(24, 144, 255, 0.2);
    border-radius: 10px;
    background: #fbfdff;
    min-height: 200px;
    display: flex;
    flex-direction: column;
    justify-content: center;
}
.slider-label {
    font-weight: 600;
    color: #44556b;
}
.slider-value {
    font-weight: 700;
    color: #1677ff;
}
.range {
    -webkit-appearance: none;
    appearance: none;
    width: 100%;
    height: 4px;
    border-radius: 999px;
    background: #e2e8f0;
    outline: none;
    position: relative;
    margin: 8px 0;
}
.range::-webkit-slider-thumb {
    -webkit-appearance: none;
    appearance: none;
    width: 16px;
    height: 16px;
    border-radius: 50%;
    background: #fff;
    border: 3px solid #1677ff;
    box-shadow: 0 2px 6px rgba(22, 119, 255, 0.25);
    cursor: pointer;
    margin-top: -6px;
}
.range::-moz-range-thumb {
    width: 16px;
    height: 16px;
    border-radius: 50%;
    background: #fff;
    border: 3px solid #1677ff;
    box-shadow: 0 2px 6px rgba(22, 119, 255, 0.25);
    cursor: pointer;
    margin-top: -6px;
}
.range::-moz-range-track {
    height: 4px;
    border-radius: 999px;
    background: #e2e8f0;
}
.range::-webkit-slider-runnable-track {
    background: linear-gradient(
        to right,
        #1677ff 0%,
        #1677ff var(--value, 0%),
        #e2e8f0 var(--value, 0%)
    );
    height: 4px;
    border-radius: 999px;
    position: relative;
}
.range::-moz-range-progress {
    background-color: #1677ff;
    height: 4px;
    border-radius: 999px 0 0 999px;
}
.xsmall {
    font-size: 11px;
}
.mb-25 {
    margin-bottom: 0.25rem !important;
}
.pr-md-50 {
    padding-right: 0.5rem !important;
}
.pl-md-50 {
    @media (min-width: 768px) {
        padding-left: 0.5rem !important;
    }
}

/* Face toggle alignment fix */
#h-face-toggle.custom-control {
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
    padding-top: 0 !important;
    margin-top: 0 !important;
}
#h-face-toggle.custom-control .custom-control-input {
    margin-top: 0 !important;
}
#h-face-toggle.custom-control .custom-control-label {
    display: flex !important;
    align-items: center !important;
    padding-top: 0 !important;
    margin-bottom: 0 !important;
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
