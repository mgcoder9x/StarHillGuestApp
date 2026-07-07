<template>
    <b-container fluid class="p-0">
        <b-card body-class="px-1 py-0 pt-1">
            <validation-observer ref="rules">
                <b-form>
                    <b-row>
                        <!-- dateFrom -->
                        <b-col md="4">
                            <b-form-group
                                :label="$t('fireEvent.common.dateFrom')"
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

                        <!-- fireType -->
                        <b-col md="4">
                            <b-form-group
                                :label="$t('fireEvent.common.fireType')"
                                label-cols-md="3"
                            >
                                <tree-select
                                    v-model="searchForm.fireType"
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    :reduce="(lstFireType) => lstFireType.id"
                                    :options="
                                        rechangeAndTranslateOptions(lstFireType)
                                    "
                                    placeholder=""
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                        <!-- level -->
                        <b-col md="4">
                            <b-form-group
                                :label="$t('fireEvent.common.level')"
                                label-cols-md="3"
                            >
                                <tree-select
                                    v-model="searchForm.fireLevel"
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    :reduce="(level) => level.id"
                                    :options="
                                        rechangeAndTranslateOptions(level)
                                    "
                                    multiple
                                    placeholder=""
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <!-- dateTo -->
                        <b-col md="4">
                            <b-form-group
                                :label="$t('fireEvent.common.dateTo')"
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
                                ></date-picker>
                            </b-form-group>
                        </b-col>
                        <!-- area -->
                        <b-col md="4">
                            <b-form-group
                                :label="$t('fireEvent.common.area')"
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
                                ></Treeselect>
                            </b-form-group>
                        </b-col>
                        <!-- device -->
                        <b-col md="4">
                            <b-form-group
                                :label="$t('fireEvent.common.device')"
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
                                    :options="listDeviceByAreaId"
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
                        :name="$t('fireEvent.common.header')"
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
                ref="fireEventsTable"
                :columns="columns"
                data-url="/fireEvents"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="fireEventTableConfig"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Device -->
                    <span v-if="props.column.field == 'stt'">
                        {{ props.row.stt }}
                    </span>
                    <span v-if="props.column.field == 'date'">
                        {{ getDate(props.row.accessTime) }}
                    </span>
                    <span v-if="props.column.field == 'time'">
                        {{ getTime(props.row.accessTime) }}
                    </span>
                    <span v-if="props.column.field == 'area'">
                        {{ getStaticName(props.row.areaId, lstArea) }}
                    </span>
                    <span v-if="props.column.field == 'device'">
                        {{ getStaticName(props.row.deviceId, lstDevice) }}
                    </span>
                    <span v-if="props.column.field == 'fireType'">
                        {{ getDynamicName(props.row.fireType, lstFireType) }}
                    </span>
                    <span v-if="props.column.field == 'level'">
                        <div
                            class="v-chip v-chip--size-large w-100 text-center"
                            :class="
                                props.row.level == 1
                                    ? 'bg-linear-success'
                                    : props.row.level == 2
                                      ? 'bg-linear-yellow'
                                      : props.row.level == 3
                                        ? 'bg-linear-warning'
                                        : 'bg-linear-danger'
                            "
                        >
                            <span
                                class="v-chip__content text-primary fw-black text-white"
                            >
                                {{ getDynamicName(props.row.level, level) }}
                            </span>
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
                            alt="Image"
                            loading="lazy"
                            style="
                                width: 60px;
                                height: 80px;
                                object-fit: cover;
                                border-radius: 0.3em;
                            "
                            :src="`${baseURL}${props.row.image}`"
                            @click="showModalEvent(props.row.eventId)"
                        />
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <div class="text-nowrap text-center">
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
                        </div>
                    </span>
                </template>
            </BasicTable>
        </b-card>
        <b-modal
            v-model="modalImgEventShow"
            :title="$t('fireEvent.common.media')"
            modal-class="modal-80"
            hide-footer
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
const { mapTextToLabel } = TreeHelper
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import { ValidationObserver, ValidationProvider } from 'vee-validate'

const isDevEnv = process.env.NODE_ENV === 'development'

export default {
    name: 'FireEventList',
    components: { Treeselect, ValidationObserver, ValidationProvider },
    setup() {
        const { apiURL } = $themeConfig.app
        const lstArea = ref([])
        const lstDevice = ref([])
        const listEventFiles = ref([])
        onMounted(async () => {
            lstArea.value = await lookupService.getAreas()
            //lstDevice.value = await lookupService.getDevices()
        })
        return {
            apiURL,
            lstArea,
            lstDevice,
            listEventFiles,
        }
    },
    data() {
        const lstFireType = [
            {
                id: 1,
                text: 'fireEvent.fireType.smoke',
            },
            {
                id: 2,
                text: 'fireEvent.fireType.fire',
            },
        ]
        const level = [
            {
                id: 1,
                text: 'fireEvent.level.low',
            },
            {
                id: 2,
                text: 'fireEvent.level.medium',
            },
            {
                id: 3,
                text: 'fireEvent.level.high',
            },
            {
                id: 4,
                text: 'fireEvent.level.critical',
            },
        ]
        return {
            lstFireType,
            level,
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
                Ngày: 'accessDateStr',
                Giờ: 'accessTimeStr',
                'Khu vực': 'areaName',
                'Thiết bị': 'deviceName',
                'Loại sự cố': 'fireTypeName',
                'Mức độ sự cố': 'levelName',
            },
            columns: [
                {
                    label: 'fireEvent.common.date',
                    field: 'date',
                },
                {
                    label: 'fireEvent.common.time',
                    field: 'time',
                },
                {
                    label: 'fireEvent.common.area',
                    field: 'area',
                },
                {
                    label: 'fireEvent.common.device',
                    field: 'device',
                },
                {
                    label: 'fireEvent.common.fireType',
                    field: 'fireType',
                },
                {
                    label: 'fireEvent.common.level',
                    field: 'level',
                },
                {
                    label: 'fireEvent.common.media',
                    field: 'image',
                },
                {
                    label: 'fireEvent.common.action',
                    field: 'action',
                },
            ],
            //baseURL: isDevEnv ? 'http://localhost:1938' : this.apiURL,
            modalImgEventShow: false,
            listAreas: [],
            listDeviceByAreaId: [],
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        baseURL() {
            return process.env.VUE_APP_BASE_URL
        },
    },
    async created() {
        const searchForm = getStorage('fireEventSearchForm')
        if (searchForm) {
            this.searchForm = { ...searchForm }
        } else {
            this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
            this.$nextTick(() => {
                this.$refs.rules.validate()
            })
        }
        await this.loadAreasTree()
        this.loadDevicesTree()
        this.search()
    },
    watch: {},
    methods: {
        goToDetail(eventId) {
            this.$router.push({
                path: `/event/fireEvent/Detail/${eventId}`,
                query: { from: 'Screen1' }
            });
        },
        
        clearEventFiles() {
            this.listEventFiles = [] // clear khi modal đóng
        },
        changeArea() {
            this.searchForm.deviceId = null
            if (
                this.searchForm.areaId != null &&
                this.searchForm.areaId.length > 0
            ) {
                this.listDeviceByAreaId = this.lstDevice.filter((x) =>
                    this.searchForm.areaId.includes(x.areaId)
                )
            } else {
                this.listDeviceByAreaId = this.lstDevice
            }
            this.search()
        },
        refresh() {
            clearStorage('fireEventSearchForm')
            this.searchForm = {
                dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                dateTo: null,
                areaId: null,
                deviceId: null,
                fireType: null,
                fireLevel: null,
            }
            this.$nextTick(() => {
                this.$refs.rules.validate()
                this.$refs.fireEventsTable.refresh()
            })
        },
        async search() {
            const ok = await this.$refs.rules.validate()
            if (!ok) return
            setStorage('fireEventSearchForm', this.searchForm, 120)
            this.$refs.fireEventsTable.refresh()
        },
        async exportData() {
            debugger
            let vm = this
            var allAreaName = vm.$t('Export.All')
            let areaName = []
            if (vm.searchForm.areaId != null && vm.searchForm.areaId != [] && vm.searchForm.areaId.length > 0) {
                vm.lstArea.forEach((area) => {
                    if (
                        vm.searchForm.areaId
                            .map(String)
                            .includes(area.id.toString())
                    ) {
                        areaName.push(area.text)
                    }
                })
                allAreaName = areaName.join(',')
            }

            var allDeviceName = vm.$t('Export.All')
            let deviceName = []
            if (vm.searchForm.deviceId != null && vm.searchForm.deviceId != [] && vm.searchForm.deviceId.length > 0) {
                vm.lstDevice.forEach((devi) => {
                    if (vm.searchForm.deviceId.includes(devi.id)) {
                        deviceName.push(devi.label)
                    }
                })
                allDeviceName = deviceName.join(',')
            }

            let fireTypeName
            if (vm.searchForm.fireType != null && vm.searchForm.fireType.length > 0) {
                vm.lstFireType.forEach((lft) => {
                    if (lft.id == vm.searchForm.fireType) {
                        fireTypeName = this.$t(lft.text)
                    }
                })
            } else {
                fireTypeName = vm.$t('Export.All')
            }

            let fireLevelName
            if (this.searchForm.fireLevel != null && this.searchForm.fireLevel.length > 0) {
                fireLevelName = this.searchForm.fireLevel
                    .map((level) => {
                        return this.getDynamicName(level, this.level)
                    })
                    .join(', ')
            } else {
                fireLevelName = this.$t('Export.All')
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
                vm.$t('fireEvent.common.header'),
                `${vm.$t('fireEvent.common.dateFrom')}: ${dateFrom}    ${vm.$t(
                    'fireEvent.common.dateTo'
                )}: ${dateTo}`,
                `${vm.$t('fireEvent.common.device')}: ${allDeviceName}`,
                `${vm.$t('fireEvent.common.area')}: ${allAreaName}`,
                `${vm.$t('fireEvent.common.fireType')}: ${fireTypeName}`,
                `${vm.$t('fireEvent.common.level')}: ${fireLevelName}`,
            ]
            vm.export_fields_vi = {
                [vm.$t('fireEvent.common.date')]: 'accessDateStr',
                [vm.$t('fireEvent.common.time')]: 'accessTimeStr',
                [vm.$t('fireEvent.common.area')]: 'areaName',
                [vm.$t('fireEvent.common.device')]: 'deviceName',
                [vm.$t('fireEvent.common.fireType')]: 'fireTypeName',
                [vm.$t('fireEvent.common.level')]: 'levelName',
            }
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
                vm.$refs.fireEventsTable.dataUrl + '?' + formData
            )
            for (const item of response.data.data.data) {
                item.accessTimeStr = this.getTime(item.accessTime)
                item.accessDateStr = this.getDate(item.accessTime)
                item.levelName = this.getDynamicName(item.level, this.level)
                item.fireTypeName = this.getDynamicName(
                    item.fireType,
                    this.lstFireType
                )
            }
            return response.data.data.data
        },
        getStaticName(id, lst) {
            // eslint-disable-next-line eqeqeq
            return lst.find((x) => x.id == id)?.text
        },
        getDynamicName(id, lst) {
            const item = lst.find(
                // eslint-disable-next-line eqeqeq
                (x) => x.id == id
            )
            return this.$t(item?.text)
        },
        getDate(date) {
            return date ? this.$moment(date).format('DD/MM/YYYY') : null
        },
        getTime(date) {
            return date ? this.$moment(date).format('HH:mm:ss') : null
        },
        rechangeOptions(dataList) {
            return dataList?.map((item) => ({
                ...item,
                text: item.text,
            }))
        },
        rechangeAndTranslateOptions(dataList) {
            const translated = dataList?.map((item) => ({
                ...item,
                text: this.$t(item.text),
            }))
            return mapTextToLabel(translated)
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
        loadDevicesTree() {
            this.$services.get('/lookup/devices').then((response) => {
                let devices = TreeHelper.removeEmptyChildren(response.data.data)
                this.lstDevice = devices
                    .filter((x) => x.eventTypeId == 500)
                    .map((item) => {
                        const { text, ...rest } = item
                        return {
                            ...rest,
                            text: text,
                            label: text,
                        }
                    })
                this.listDeviceByAreaId = this.lstDevice
            })
        },
    },
}
</script>
<style lang="scss">
@import '@/assets/scss/_custom-tree-select.scss';

.v-chip {
    align-items: center;
    display: flex;
    justify-content: center;
    max-width: 100%;
    min-width: 0;
    overflow: hidden;
    position: relative;
    text-decoration: none;
    white-space: nowrap;
    vertical-align: middle;
    border-color: rgba(var(--v-border-color), var(--v-border-opacity));
    border-style: solid;
    border-width: 0;
    border-radius: 0.375rem;
    height: calc(var(--v-chip-height) + 0px);
    &.v-chip--size-default {
        --v-chip-size: 13px;
        --v-chip-height: 32px;
        font-size: 13px;
        padding: 0 12px;
    }
    &.v-chip--size-large {
        --v-chip-size: 13.125px;
        --v-chip-height: 38px;
        font-size: 13.125px;
        padding: 0 14px;
    }
    &.v-chip--size-x-large {
        --v-chip-size: 13.25px;
        --v-chip-height: 44px;
        font-size: 13.25px;
        padding: 0 17px;
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
