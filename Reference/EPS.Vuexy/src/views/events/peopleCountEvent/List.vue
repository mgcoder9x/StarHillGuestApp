<!-- eslint-disable vue/html-self-closing -->
<template>
    <div>
        <validation-observer ref="rules">
            <!-- Filters -->
            <b-card no-body>
                <b-card-body>
                    <b-form @submit.prevent="search">
                        <!-- ROW 1 -->
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
                                <Treeselect
                                    v-model="searchForm.areaId"
                                    :dir="$store.state.appConfig.isRTL ? 'rtl' : 'ltr'"
                                    label="text"
                                    :reduce="(area) => area.id"
                                    :options="listArea"
                                    :multiple="true"
                                    placeholder=""
                                    :limit="3"
                                    :limit-text="(count) => `+${count}`"
                                    class="treeselect-nowrap"
                                    
                                    :flat="true" 
                                    valueConsistsOf="BRANCH_PRIORITY"
                                    
                                    @input="search"
                                ></Treeselect>
                            </b-form-group>
                        </b-col>

                            <!-- Device -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('Events.Table.Equipment')"
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
                                        :options="listDevice"
                                        :multiple="true"
                                        placeholder=""
                                        :limit="3"
                                        :limit-text="(count) => `+${count}`"
                                        class="treeselect-nowrap"
                                        value-consistsOf="ALL"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <!-- ROW 2 -->
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

                            <!-- Direction (Ra / Vào) -->
                            <!-- <b-col md="4">
                                <b-form-group
                                    :label="$t('Events.SearchForm.Direction')"
                                    label-for="h-searchForm-direction"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        id="h-searchForm-direction"
                                        v-model="searchForm.direction"
                                        :dir="
                                            $store.state.appConfig.isRTL
                                                ? 'rtl'
                                                : 'ltr'
                                        "
                                        label="text"
                                        :reduce="(opt) => opt.id"
                                        :options="directionOptions"
                                        :clearable="true"
                                        :searchable="false"
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col> -->
                        </b-row>

                        <!-- hidden submit -->
                        <b-button
                            type="submit"
                            style="
                                position: absolute;
                                width: 1px;
                                height: 1px;
                                overflow: hidden;
                                clip: rect(0, 0, 0, 0);
                            "
                        >
                            Search
                        </b-button>
                    </b-form>
                </b-card-body>
            </b-card>
        </validation-observer>

        <b-card title="">
            <div class="text-left mb-1 d-flex" style="gap: 8px">
                <b-button
                    v-waves
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="$t('Events.Header.PeopleCount')"
                        :fetch="exportData"
                        type="xlsx"
                        :fields="currentExportFields"
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
                ref="peopleCountTable"
                :columns="columns"
                data-url="/peopleCountEvent"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="peopleCountEventTable"
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
                            width="120"
                            height="80"
                            style="object-fit: cover; border-radius: 0.3em"
                            class="cursor-pointer"
                            @click="showModalEvent(props.row.eventId)"
                        />
                    </span>

                    <!-- Column: directionStr -->
                    <span v-else-if="props.column.field === 'directionStr'">
                        {{ translateDirectionStr(props.row.directionStr) }}
                    </span>
                </template>
            </BasicTable>
        </b-card>
    </div>
</template>

<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import { $themeConfig } from '@themeConfig'
const isDevEnv = process.env.NODE_ENV == 'development'
import moment from 'moment'
import Treeselect from '@riophae/vue-treeselect'
import TreeHelper from '@/utils/treeHelper'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import { ValidationObserver, ValidationProvider } from 'vee-validate'

const directionMap = {
  'Vào': 'in',
  'Ra': 'out'
};

export default {
    mixins: [authorizationMixin],
    components: { Treeselect,TreeHelper,ValidationObserver, 
        ValidationProvider  },
    setup() {
        const { apiURL } = $themeConfig.app
        return { apiURL }
    },
    data() {
        return {
            modalImgEventShow: false,
            srcImgEvent: '',
            baseURL: isDevEnv ? 'http://localhost:1938' : this.apiURL,

            searchForm: {
                dateFrom: null,
                dateTo: null,
                areaId: null,
                deviceId: null,
                // NEW: filter hướng
                direction: null, // 1 = In (Vào), 2 = Out (Ra) — điều chỉnh cho đúng API nếu khác
            },            
            columns: [
                {
                    label: 'fireEvent.common.time',
                    field: 'accessTime',
                    formatFn(value) {
                        return moment.utc(value).format('DD/MM/YYYY HH:mm:ss')
                    },
                },

                { label: 'Events.Table.Area', field: 'areaName' },
                { label: 'Events.Table.Device', field: 'deviceName' },
                { label: 'Events.Table.PeopleCount', field: 'numberOfPeople' },
                // { label: 'Events.Table.Direction', field: 'directionStr' },
            ],

            headerExcelDetail: [],
            export_fields_vi: {
                'Thời gian': 'accessTimeStr',
                'Khu vực': 'areaName',
                'Thiết bị': 'deviceName',
                'Số người': 'numberOfPeople',
                // Hướng: 'directionStr',
            },
            export_fields_en: {
                Time: 'accessTimeStr',
                Area: 'areaName',
                Device: 'deviceName',
                'People Count': 'numberOfPeople',
                // Direction: 'directionStr',
            },

            listArea: [],
            listDevice: [],

            // NEW: 2 lựa chọn Ra/Vào
            directionOptions: [
                {
                    id: 1,
                    text:
                        this.$t('direction.in') ||
                        this.$t('direction.In') ||
                        'Vào',
                },
                {
                    id: 2,
                    text:
                        this.$t('direction.out') ||
                        this.$t('direction.Out') ||
                        'Ra',
                },
            ],
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        currentExportFields() {
            return this.currentLocale === 'en'
                ? this.export_fields_en
                : this.export_fields_vi
        },
    },
    created() {
    const accessToken = this.$services.getUserData()
    this.searchForm.compId = accessToken.companyId
    this.loadArea()
    this.loadDevice()
    
    const saved = getStorage('peopleCountEventTable')
    if (saved) {
        this.searchForm = { ...saved }
    } else {
        this.searchForm.dateFrom = moment().format('YYYY-MM-DD 00:00:00')
        // ✅ Validate ngay khi load để check nếu có dateTo từ cache
        this.$nextTick(() => {
            this.$refs.rules.validate()
        })
    }
},
    watch: {
        // cập nhật label Ra/Vào khi đổi ngôn ngữ
        '$i18n.locale'() {
            this.directionOptions = [
                {
                    id: 1,
                    text:
                        this.$t('direction.in') ||
                        this.$t('direction.In') ||
                        'Vào',
                },
                {
                    id: 2,
                    text:
                        this.$t('direction.out') ||
                        this.$t('direction.Out') ||
                        'Ra',
                },
            ]
        },
        'searchForm.areaId': {
            handler() {
                this.search()
            },
            deep: true,
        },
        'searchForm.deviceId': {
            handler() {
                this.search()
            },
            deep: true,
        },
    },
    methods: {
        translateDirectionStr(directionStr) {
            const key = directionMap[directionStr];
            return key ? this.$t('Events.Direction.' + key) : directionStr;
        },
        async exportData() {
            var vm = this
            debugger
            let areaName
            if (
                vm.searchForm.areaId != null &&
                vm.searchForm.areaId.length > 0
            ) {
                const areaNames = []
                const findAreaNames = (nodes) => {
                    if (!nodes || nodes.length === 0) return
                    nodes.forEach((node) => {
                        if (vm.searchForm.areaId.includes(node.id)) {
                            areaNames.push(node.text || node.label)
                        }
                        if (node.children && node.children.length > 0) {
                            findAreaNames(node.children)
                        }
                    })
                }
                findAreaNames(vm.listArea)
                areaName = areaNames.join(', ')
            } else {
                areaName = vm.$t('Export.All')
            }

            let deviceName
            if (
                vm.searchForm.deviceId != null &&
                vm.searchForm.deviceId.length > 0
            ) {
                const deviceNames = []
                vm.listDevice.forEach((dv) => {
                    if (vm.searchForm.deviceId.includes(dv.id)) {
                        deviceNames.push(dv.text || dv.label)
                    }
                })
                deviceName = deviceNames.join(', ')
            } else {
                deviceName = vm.$t('Export.All')
            }

            let dateFrom = moment(this.searchForm.dateFrom).format(
                'DD/MM/YYYY HH:mm'
            )
            let dateTo = moment(this.searchForm.dateTo).format(
                'DD/MM/YYYY HH:mm'
            )
            if (dateFrom == 'Invalid date')
                dateFrom = moment('2020-01-01 00:00:00').format(
                    'DD/MM/YYYY HH:mm'
                )
            if (dateTo == 'Invalid date')
                dateTo = moment().format('DD/MM/YYYY HH:mm')

            vm.headerExcelDetail = [
                vm.$t('Events.Header.PeopleCount'),
                `${vm.$t('Events.SearchForm.FromDate')}: ${dateFrom}        ${vm.$t('Events.SearchForm.ToDate')}: ${dateTo}`,
                `${vm.$t('Events.SearchForm.Device')}: ${deviceName}`,
                `${vm.$t('Events.SearchForm.Area')}: ${areaName}`,
            ]

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
                vm.$refs.peopleCountTable.dataUrl + '?' + formData
            )
            return response.data.data.data
        },
        async search() {
            const ok = await this.$refs.rules.validate()
            if (!ok) return

            setStorage('peopleCountEventTable1', this.searchForm, 120)
            this.$refs.peopleCountTable.refresh()
        },
        refresh() {
            clearStorage('peopleCountEventTable')
            this.searchForm = {
                dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                dateTo: null,
                areaId: null,
                deviceId: null,
                direction: null,
            }
            this.$nextTick(() => {
                this.$refs.rules.validate() // ✅ Validate sau khi reset
                this.$refs.peopleCountTable.refresh()
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
            // this.$services.get('/lookup/devices').then((response) => {
            //     const devices = TreeHelper.removeEmptyChildren(
            //         response.data.data
            //     )
            //     this.listDevice = devices
            //         .filter((x) => x.eventTypeId == 204)
            //         .map(({ text, ...rest }) => ({ ...rest, label: text }))
            // })
            this.$services.get('/lookup/devices').then((response) => {
                let devices = TreeHelper.removeEmptyChildren(response.data.data)
                this.listDevice = devices
                    .filter((x) => x.eventTypeId == 700)
                    .map((item) => {
                        const { text, ...rest } = item
                        return {
                            ...rest,
                            text: text,
                            label: text,
                        }
                    })
                // this.listDeviceByAreaId = this.lstDevice
            })
        },
        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.modalImgEventShow = true
        },
        
        
    },
}
</script>

<style lang="scss">
@import '@/assets/scss/_custom-tree-select.scss';
</style>
