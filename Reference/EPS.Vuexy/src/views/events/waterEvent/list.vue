<template>
    <div>
        <ValidationObserver ref="observer">
            <b-card>
                <b-card-body>
                    <b-form>
                        <b-row>
                            <!-- FromDate -->
                            <b-col md="4">
                                <ValidationProvider
                                    v-slot="{ errors }"
                                    :name="$t('WaterEvents.Label.FromDate')"
                                    :rules="`fromDate:` + SearchForm.ToDate"
                                >
                                    <b-form-group
                                        label-cols-md="4"
                                        :label="
                                            $t('WaterEvents.Label.FromDate')
                                        "
                                    >
                                        <date-picker
                                            id="h-searchForm-FromDate"
                                            v-model="SearchForm.FromDate"
                                            type="datetime"
                                            :locale="currentLocale"
                                            format="DD-MM-YYYY HH:mm:ss"
                                            value-type="YYYY-MM-DD HH:mm:ss"
                                            style="width: 100%"
                                            class="custom-date-picker"
                                            @change="search"
                                        ></date-picker>
                                        <small class="validate-message">{{
                                            errors[0]
                                        }}</small>
                                    </b-form-group>
                                </ValidationProvider>
                            </b-col>
                            <!-- Area -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('WaterEvents.Label.Areas')"
                                    label-cols-md="4"
                                >
                                    <Treeselect
                                        v-model="SearchForm.AreaId"
                                        :multiple="true"
                                        :options="listAreas"
                                        :reduce="(area) => area.id"
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        valueConsistsOf="ALL"
                                        @input="search"
                                    >
                                    </Treeselect>
                                </b-form-group>
                            </b-col>
                            <!-- Device -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('WaterEvents.Label.Devices')"
                                    label-cols-md="4"
                                >
                                    <tree-select
                                        v-model="SearchForm.DeviceId"
                                        :multiple="true"
                                        true
                                        :options="listDevices"
                                        :reduce="(item) => item.id"
                                        :get-option-label="
                                            (item) => $t(item.text)
                                        "
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        valueConsistsOf="ALL"
                                        @input="search"
                                    >
                                    </tree-select>
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <!-- ToDate -->
                            <b-col md="4">
                                <b-form-group
                                    label-cols-md="4"
                                    :label="$t('WaterEvents.Label.ToDate')"
                                >
                                    <date-picker
                                        id="h-searchForm-ToDate"
                                        v-model="SearchForm.ToDate"
                                        type="datetime"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY HH:mm:ss"
                                        value-type="YYYY-MM-DD HH:mm:ss"
                                        style="width: 100%"
                                        @change="search"
                                    ></date-picker>
                                </b-form-group>
                            </b-col>
                            <!-- WaterLevel -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('WaterEvents.Label.WaterLevel')"
                                    label-cols-md="4"
                                >
                                    <div style="width: 100%; display: flex">
                                        <b-form-select
                                            v-model="SearchForm.LevelType"
                                            style="width: 20%"
                                            :options="Comparison"
                                            name="LevelType"
                                            @input="search"
                                        />
                                        <div style="width: 80%">
                                            <ValidationProvider
                                                v-slot="{ errors }"
                                                :rules="{ decimal: 3 }"
                                                name="Giá trị lớn nhất"
                                            >
                                                <b-form-input
                                                    v-model="
                                                        SearchForm.WaterLevel
                                                    "
                                                    type="number"
                                                    style="
                                                        width: 100%;
                                                        display: flex;
                                                    "
                                                    @input="search"
                                                />
                                                <small class="validate-message">
                                                    {{ errors[0] }}
                                                </small>
                                            </ValidationProvider>
                                        </div>
                                    </div>
                                </b-form-group>
                            </b-col>
                            <!-- Warning -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('WaterEvents.Label.Warning')"
                                    label-cols-md="4"
                                >
                                    <v-select
                                        v-model="SearchForm.WarningId"
                                        :options="listWarningLevels"
                                        multiple
                                        :settings="{ allowClear: true }"
                                        :reduce="(item) => item.id"
                                        :get-option-label="
                                            (item) => $t(item.text)
                                        "
                                        @input="search"
                                    >
                                    </v-select>
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </b-form>
                </b-card-body>
            </b-card>
        </ValidationObserver>
        <!-- Table -->
        <b-card title="">
            <div
                style="text-align: end; margin-bottom: 10px; margin-top: -10px"
            >
                <b-button
                    variant="secondary"
                    class="btn-hover-linear-success border-0"
                >
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="$t('Danh sách sự kiện giám sát mực nước')"
                        :fetch="exportData"
                        type="xlsx"
                        :fields="export_fields_vi"
                    >
                        <Icon
                            icon="file-icons:microsoft-excel"
                            class="sm-icon"
                        />
                        <span class="ml-25">{{
                            $t('Button.ExportExcel')
                        }}</span>
                    </downloadExcel>
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="eventWaterTable"
                :columns="Columns"
                data-url="/waterEvents"
                :search-form="SearchForm"
                :sort-by="'accessTime'"
                storage-name="waterEventTable"
            >
                <!-- Row -->
                <template slot="table-row" slot-scope="props">
                    <span
                        v-if="props.column.field == 'stt'"
                        :style="CoulumnStyle"
                    >
                        {{ props.row.stt }}
                    </span>
                    <span
                        v-if="props.column.field == 'accessTimeString'"
                        :style="CoulumnStyle"
                    >
                        {{ props.row.accessTimeString }}
                    </span>
                    <span
                        v-if="props.column.field == 'accessTimeStr'"
                        :style="CoulumnStyle"
                    >
                        {{ formatTime($t(props.row.accessTime)) }}
                    </span>
                    <span
                        v-if="props.column.field == 'deviceId'"
                        :style="CoulumnStyle"
                    >
                        {{ $t(props.row.deviceName) }}
                    </span>
                    <span
                        v-else-if="props.column.field == 'areaId'"
                        :style="CoulumnStyle"
                    >
                        {{ $t(props.row.areaName) }}
                    </span>
                    <span
                        v-else-if="props.column.field == 'waterLevel'"
                        :style="CoulumnStyle"
                    >
                        {{ $t(props.row.waterLevel) }}
                    </span>
                    <span
                        v-else-if="props.column.field == 'warning'"
                        :style="CoulumnStyle"
                    >
                        {{ $t(props.row.warning) }}
                    </span>
                    <span
                        v-else-if="props.column.field == 'image'"
                        :style="CoulumnStyle"
                    >
                        <img
                            :src="`${baseURL}${props.row.image}`"
                            loading="lazy"
                            alt="Image"
                            width="120"
                            height="80"
                            style="
                                object-fit: cover;
                                border-radius: 0.3em;
                                max-width: 100%;
                            "
                            class="cursor-pointer"
                            @click="showModalEvent(props.row.eventId)"
                        />
                    </span>
                    <span v-else-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/event/waterEvent/detail/${props.row.id}`,
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
            title="Ảnh sự kiện"
            ok-title="Đóng"
            hide-header-close
            ok-only
            size="lg"
        >
            <b-carousel id="carousel-example-generic" indicators controls>
                <b-carousel-slide
                    v-for="item in listEventFiles"
                    :key="item.id"
                    :img-src="`${baseURL}${item.filePath}`"
                />
            </b-carousel>
        </b-modal>
    </div>
</template>
<script>
import { $themeConfig } from '@themeConfig'
import moment from 'moment'
import Treeselect from '@riophae/vue-treeselect'
import { lookupService } from '@/services'
import TreeHelper from '@/utils/treeHelper'

const isDevEnv = process.env.NODE_ENV == 'development'

export default {
    components: {
        Treeselect,
    },

    data() {
        return {
            listAreas: [],
            listDevices: [],
            listWarningLevels: [],
            listEventFiles: [],
            SearchForm: {
                LevelType: 1,
                DeviceId: null,
                AreaId: null,
                DeviceName: null,
                AreaName: null,
                WaterLevel: null,
                ToDate: null,
                FromDate: null,
                WarningId: null,
            },
            Comparison: [
                { value: '1', text: '=' },
                { value: '2', text: '>' },
                { value: '3', text: '<' },
                { value: '4', text: '>=' },
                { value: '5', text: '<=' },
            ],
            CoulumnStyle: {
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
            },
            modalImgEventShow: false,
            baseURL: isDevEnv
                ? $themeConfig.app.apiURLDev
                : $themeConfig.app.apiURL,
            headerExcelDetail: [],
            export_fields_vi: {
                Ngày: 'accessTimeString',
                'Thời gian': 'accessTime',
                'Thiết bị': 'deviceName',
                'Khu vực': 'areaName',
                'Mực nước(m)': 'waterLevel',
                'Cảnh báo': 'warning',
            },
        }
    },

    computed: {
        currentLocale() {
            return this.$i18n.locale
        },

        Columns() {
            return [
                {
                    label: this.$t('WaterEvents.Field.Date'),
                    field: 'accessTimeString',
                },
                {
                    label: this.$t('WaterEvents.Field.Time'),
                    field: 'accessTimeStr',
                },
                {
                    label: this.$t('WaterEvents.Field.Device'),
                    field: 'deviceId',
                },
                {
                    label: this.$t('WaterEvents.Field.Area'),
                    field: 'areaId',
                },
                {
                    label: this.$t('WaterEvents.Field.WaterLevel'),
                    field: 'waterLevel',
                },
                {
                    label: this.$t('WaterEvents.Field.Warning'),
                    field: 'warning',
                },
                {
                    label: this.$t('WaterEvents.Field.Image'),
                    field: 'image',
                },
                {
                    label: this.$t('WaterEvents.Field.Action'),
                    field: 'action',
                },
            ]
        },
    },

    async created() {
        await this.loadAreasTree()
        this.loadDevices()
        this.listWarningLevels = await lookupService.getWarningLevels()
        //this.SearchForm.FromDate = moment().format('YYYY-MM-DD 00:00:00')
    },

    watch: {
        // 'SearchForm.AreaId': function (newVal) {
        //     // Gọi searchByAreaId khi giá trị thay đổi, bao gồm cả khi xóa lựa chọn
        //     //this.searchByAreaId(newVal)
        // },
    },
    methods: {
        // searchByAreaId(value) {
        //     debugger
        //     var vm = this
        //     var valueStr
        //     if (value.length != 0) {
        //         valueStr = value.join(',')
        //         this.listDevices = this.listDevices.filter((x) =>
        //             valueStr.includes(x.areaId)
        //         )
        //     } else {
        //         // Khi có giá trị (lựa chọn một khu vực)
        //         return this.$services
        //             .get(`/lookup/devices`)
        //             .then((response) => {
        //                 this.listDevices = response.data.data
        //             })
        //     }
        //     setStorage('allEventSearchForm', this.searchForm, 120)
        //     this.$refs.eventWaterTable.refresh()
        // },
        async search() {
            const isValid = await this.$refs.observer.validate()
            if (isValid) {
                this.$refs.eventWaterTable.refresh()
            }
        },
        formatDate(accessTime) {
            if (!accessTime) return ''
            const dateTime = new Date(accessTime)
            return dateTime.toISOString().slice(0, 10)
        },
        formatTime(accessTime) {
            if (!accessTime) return ''
            const dateTime = new Date(accessTime)
            return dateTime.toTimeString().slice(0, 8)
        },
        loadEventFiles(eventId) {
            this.$services
                .get(`/event/eventFilesById/${eventId}`)
                .then((response) => {
                    this.listEventFiles = response.data
                })
                .catch((error) => {
                    console.log(error)
                })
        },
        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.modalImgEventShow = true
        },
        async exportData() {
            const vm = this
            let areaName
            if (
                vm.SearchForm.AreaId != null &&
                vm.SearchForm.AreaId.length > 0
            ) {
                const listAreaName = vm.SearchForm.AreaId.map((areaId) => {
                    const area = vm.listAreas.find((x) => x.id == areaId)
                    return area ? area.text : ''
                })
                areaName = listAreaName.join(', ')
            } else {
                areaName = vm.$t('Export.All')
            }

            let deviceName
            if (
                vm.SearchForm.DeviceId != null &&
                vm.SearchForm.DeviceId.length > 0
            ) {
                const listDeviceName = vm.SearchForm.DeviceId.map(
                    (deviceId) => {
                        const device = vm.listDevices.find(
                            (x) => x.id == deviceId
                        )
                        return device ? device.text : ''
                    }
                )
                deviceName = listDeviceName.join(', ')
            } else {
                deviceName = vm.$t('Export.All')
            }

            let warningName
            if (
                vm.SearchForm.WarningId != null &&
                vm.SearchForm.WarningId.length > 0
            ) {
                const listWarningName = vm.SearchForm.WarningId.map((war) => {
                    const warning = vm.listWarningLevels.find(
                        (x) => x.id == war
                    )
                    return warning ? warning.text : ''
                })
                warningName = listWarningName.join(', ')
            } else {
                warningName = vm.$t('Export.All')
            }

            let dateFrom = moment(this.SearchForm.FromDate).format(
                'DD/MM/YYYY HH:mm'
            )
            let dateTo = moment(this.SearchForm.ToDate).format(
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

            let waterLevelName = null
            if (
                vm.SearchForm.WaterLevel != null &&
                vm.SearchForm.WaterLevel != ''
            ) {
                vm.Comparison.forEach((wl) => {
                    if (wl.value == vm.SearchForm.LevelType) {
                        waterLevelName = wl.text + vm.SearchForm.WaterLevel
                    }
                })
            } else {
                waterLevelName = vm.$t('Export.All')
            }
            vm.headerExcelDetail = [
                'Danh sách sự kiện giám sát mực nước',
                `${vm.$t(
                    'WaterEvents.Label.FromDate'
                )}: ${dateFrom}     ${vm.$t(
                    'WaterEvents.Label.ToDate'
                )}: ${dateTo}`,
                `${vm.$t('WaterEvents.Label.Devices')}: ${deviceName}`,
                `${vm.$t('WaterEvents.Label.Areas')}: ${areaName}`,
                `${vm.$t('WaterEvents.Label.WaterLevel')}: ${waterLevelName}`,
                `${vm.$t('WaterEvents.Label.Warning')}: ${warningName}`,
            ]

            const pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'accessTime',
                sortDesc: true,
            }
            // var formData = $.extend({}, pagination, vm.searchForm);
            const formData = `${new URLSearchParams(
                pagination
            ).toString()}&${new URLSearchParams(vm.SearchForm).toString()}`
            const response = await this.$services.get(
                `${vm.$refs.eventWaterTable.dataUrl}?${formData}`
            )
            for (let i = 0; i < response.data.data.data.length; i++) {
                response.data.data.data[i].accessTime = vm.formatTime(
                    response.data.data.data[i].accessTime
                )
            }
            return response.data.data.data
        },
        async loadAreasTree() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                this.listAreas = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            })
        },
        async loadDevices() {
            this.$services.get('/lookup/devices').then((response) => {
                let devices = TreeHelper.removeEmptyChildren(response.data.data)
                this.listDevices = devices.map((item) => {
                    const { text, ...rest } = item
                    return {
                        ...rest,
                        label: text,
                    }
                })
            })
        },
    },
}
</script>
<style lang="scss">
@import '@/assets/scss/_custom-tree-select.scss';

.validate-message {
    color: red;
}
</style>
