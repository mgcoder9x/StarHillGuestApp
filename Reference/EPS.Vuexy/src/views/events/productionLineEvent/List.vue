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
                                    :label="$t('Events.SearchForm.FromDate')"
                                    label-for="h-searchForm-dateFrom"
                                    label-cols-md="3"
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
                                    ></date-picker>
                                </b-form-group>
                            </b-col>
                            <!-- Production Line (Dây chuyền) -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'ProductionLineEvent.SearchForm.ProductionLine'
                                        )
                                    "
                                    label-for="h-searchForm-productionLine"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        v-model="searchForm.productionLineId"
                                        :options="listProductionLines"
                                        label="text"
                                        :reduce="(item) => item.id"
                                        :placeholder="
                                            $t(
                                                'ProductionLineEvent.SearchForm.ProductionLinePlaceholder'
                                            )
                                        "
                                        @input="handleProductionLineChange"
                                    />
                                </b-form-group>
                            </b-col>
                            <!-- Step (Công đoạn/bước) -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'ProductionLineEvent.SearchForm.Step'
                                        )
                                    "
                                    label-for="h-searchForm-step"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        v-model="searchForm.stepId"
                                        :options="filteredListWorkFlow"
                                        label="text"
                                        :reduce="(item) => item.id"
                                        :placeholder="
                                            $t(
                                                'ProductionLineEvent.SearchForm.StepPlaceholder'
                                            )
                                        "
                                        :disabled="!searchForm.productionLineId"
                                        multiple
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
                                    ></date-picker>
                                </b-form-group>
                            </b-col>
                            <!-- Warning Level (Phân loại) -->
                            <b-col md="4">
                                <b-form-group
                                    :label="
                                        $t(
                                            'ProductionLineEvent.SearchForm.WarningLevel'
                                        )
                                    "
                                    label-for="h-searchForm-warningLevel"
                                    label-cols-md="3"
                                >
                                    <v-select
                                        v-model="searchForm.warningLevelId"
                                        :options="listWarningLevels"
                                        label="text"
                                        :reduce="(item) => item.id"
                                        :multiple="true"
                                        placeholder=""
                                        @input="search"
                                    />
                                </b-form-group>
                            </b-col>
                            <!-- Device (Thiết bị) -->
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('Events.SearchForm.Device')"
                                    label-for="h-searchForm-device"
                                    label-cols-md="3"
                                >
                                    <tree-select
                                        v-model="searchForm.deviceId"
                                        :options="listDevice"
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
                            <b-col md="4">
                                <b-form-group
                                    :label="$t('ProductionLineEvent.SearchForm.Status')"
                                    label-for="h-searchForm-status"
                                    label-cols-md="3"
                                >
                                       <v-select
                                        v-model="searchForm.status"
                                        :options="listStatus"
                                        :multiple="true"
                                        placeholder=""
                                        @input="search"
                                        label="text"
                                        :reduce="(item) => item.id"
                                    >
                                    </v-select>
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
                        :name="$t('ProductionLineEvent.Excel.Header')"
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
                data-url="/production-line-event"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="productionLineEventTable"
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
                    <!-- Column: WarningLevel -->
                    <span
                        v-else-if="props.column.field == 'warningLevelName'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ props.row.warningLevelName }}
                    </span>
                    <span
                        v-else-if="props.column.field == 'status'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{ props.row.status === EStatus.ACTIVE
                            ? $t('ProductionLineEvent.Status.Completed')
                            : $t('ProductionLineEvent.Status.Violation') }}
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
                                    path: `/event/productionLineEvent/detail/${props.row.eventId}`,
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
            <template #modal-footer>
                <p></p>
            </template>
        </b-modal>
    </div>
</template>

<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import moment from 'moment'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'
import TreeHelper from '@/utils/treeHelper'
import '@/assets/scss/_custom-tree-select.scss'
export default {
    mixins: [authorizationMixin],
    data() {
        return {
            EStatus: { ACTIVE: 1, INACTIVE: 0 },
            modalImgEventShow: false,
            srcImgEvent: '',
            searchForm: {
                dateFrom: null,
                dateTo: null,
                productionLineId: null,
                stepId: [],
                warningLevelId: null,
                deviceId: null,
                status: null,
            },
            headerExcelDetail: [],
            export_fields_vi: {},
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
                    label: 'ProductionLineEvent.Table.ProductionLine',
                    field: 'productionLineName',
                },
                {
                    label: 'ProductionLineEvent.Table.Step',
                    field: 'stepName',
                },
                {
                    label: 'ProductionLineEvent.Table.Person',
                    field: 'personNo',
                },
                {
                    label: 'ProductionLineEvent.Table.EventType',
                    field: 'eventTypeName',
                },
                {
                    label: 'ProductionLineEvent.Table.WarningLevel',
                    field: 'warningLevelName',
                },
                {
                    label: 'ProductionLineEvent.Table.Status',
                    field: 'status',
                },
                
                {
                    label: 'Events.Table.Device',
                    field: 'deviceName',
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
            listWarningLevels: [],
            listDevice: [],
            listEventFiles: [],
            listProductionLines: [],
            listWorkFlowItems: [],
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        listStatus() {
            return [
                { id: this.EStatus.ACTIVE, text: this.$t('ProductionLineEvent.Status.Completed') },
                { id: this.EStatus.INACTIVE, text: this.$t('ProductionLineEvent.Status.Violation') },
            ]
        },
        baseURL() {
            // return "https://smartfactory.atin.vn/Service"
            return process.env.VUE_APP_BASE_URL
        },
        filteredListWorkFlow(){
            if(this.searchForm.productionLineId){
                return this.listWorkFlowItems
                    .filter(item => item.workFlowId == this.searchForm.productionLineId)
                    .sort((a, b) =>
                        String(a.text || '')
                            .localeCompare(String(b.text || ''))
                    )
            }
            return []
        }
    },
    watch: {},
    async created() {
        const accessToken = this.$services.getUserData()
        const defaultSearchForm = this.buildDefaultSearchForm(
            accessToken.companyId
        )

        await Promise.all([
            this.loadWarningLevels(),
            this.loadDevice(),
            this.loadProductionLines(),
            this.loadSteps(),
        ])

        const hasRouteQuery = this.hasRouteSearchQuery()
        if (hasRouteQuery) {
            this.searchForm = {
                ...defaultSearchForm,
                dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
            }
            this.applyRouteSearchQuery()
        } else {
            const searchForm = getStorage('productionLineEventSearchForm')
            if (searchForm) {
                this.searchForm = {
                    ...defaultSearchForm,
                    ...searchForm,
                }
            } else {
                this.searchForm = {
                    ...defaultSearchForm,
                    dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                }
            }
        }

        this.$nextTick(() => {
            if (this.$refs.eventTable) {
                this.$refs.eventTable.refresh()
            }
        })
    },
    methods: {
        buildDefaultSearchForm(compId) {
            return {
                dateFrom: null,
                dateTo: null,
                productionLineId: null,
                stepId: [],
                warningLevelId: null,
                deviceId: null,
                status: null,
                compId,
            }
        },
        handleProductionLineChange() {
            this.searchForm.stepId = []
            this.search()
        },
        parseMultiNumberQuery(value) {
            if (value === undefined || value === null || value === '') {
                return []
            }

            const raw = Array.isArray(value) ? value : [value]
            return raw
                .flatMap((item) => String(item).split(','))
                .map((item) => Number(item))
                .filter((item) => !Number.isNaN(item))
        },
        hasRouteSearchQuery() {
            const { productionLineId, stepId, status } = this.$route.query || {}
            return Boolean(
                productionLineId ||
                    stepId ||
                    status
            )
        },
        applyRouteSearchQuery() {
            const {
                productionLineId,
                stepId,
                status,
            } = this.$route.query || {}

            if (productionLineId) {
                const parsedProductionLineId = Number(productionLineId)
                if (!Number.isNaN(parsedProductionLineId)) {
                    this.searchForm.productionLineId = parsedProductionLineId
                }
            }

            if (stepId) {
                this.searchForm.stepId = this.parseMultiNumberQuery(stepId)
            }

            if (status !== undefined && status !== null && status !== '') {
                const parsedStatus = Number(status)
                if (!Number.isNaN(parsedStatus)) {
                    this.searchForm.status = [parsedStatus]
                }
            }
        },
        clearEventFiles() {
            this.listEventFiles = []
        },
        refresh() {
            clearStorage('productionLineEventSearchForm')
            const accessToken = this.$services.getUserData()
            this.searchForm = {
                ...this.buildDefaultSearchForm(accessToken.companyId),
                dateFrom: moment().format('YYYY-MM-DD 00:00:00'),
            }
            this.$nextTick(() => {
                this.$refs.eventTable.refresh()
            })
        },
        search() {
            setStorage('productionLineEventSearchForm', this.searchForm, 120)
            if (this.$refs.eventTable) {
                this.$refs.eventTable.refresh()
            }
        },
        loadWarningLevels() {
            return this.$services
                .get('/lookup/workflow-event-warning-levels')
                .then((response) => {
                    let levels = response.data.data || []
                    this.listWarningLevels = levels.map((item) => ({
                        id: Number(item.id),
                        text: item.text || item.name,
                    }))
                })
        },
        loadDevice() {
            return this.$services.get('/lookup/devices').then((response) => {
                let devices = TreeHelper.removeEmptyChildren(response.data.data)
                this.listDevice = devices.map((item) => {
                    const { text, ...rest } = item
                    return {
                        ...rest,
                        label: text,
                    }
                })
            })
        },
        loadProductionLines() {
            return this.$services.get('/lookup/workflows').then((response) => {
                this.listProductionLines = (response.data.data || []).map((item) => ({
                    ...item,
                    id: Number(item.id ?? item.Id),
                    text: item.text || item.Text || item.name || item.Name || '',
                }))
            })
        },
        loadSteps() {
            return this.$services.get('/lookup/workflowItem').then((response) => {
                this.listWorkFlowItems = (response.data.data || []).map((item) => ({
                    ...item,
                    id: Number(item.id ?? item.Id),
                    text: item.text || item.Text || item.name || item.Name || '',
                    workFlowId: Number(item.workFlowId ?? item.WorkFlowId),
                }))
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
            var vm = this
            const toCsv = (value) => {
                if (Array.isArray(value)) return value.join(',')
                return value
            }

            const exportSearchForm = {
                ...vm.searchForm,
                productionLineId: vm.searchForm.productionLineId ?? null,
                stepId: toCsv(vm.searchForm.stepId),
                warningLevelId: toCsv(vm.searchForm.warningLevelId),
                deviceId: toCsv(vm.searchForm.deviceId),
                status: toCsv(vm.searchForm.status),
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
                vm.$t('ProductionLineEvent.Excel.Header'),
                `${vm.$t('Events.SearchForm.FromDate')}: ${dateFrom}    ${vm.$t('Events.SearchForm.ToDate')} :${dateTo}`,
            ]
            vm.export_fields_vi = {
                [vm.$t('fireEvent.common.date')]: 'accessDateStr',
                [vm.$t('fireEvent.common.time')]: 'accessTimeStr',
                [vm.$t('ProductionLineEvent.Table.ProductionLine')]: 'productionLineName',
                [vm.$t('ProductionLineEvent.Table.Step')]: 'stepName',
                [vm.$t('ProductionLineEvent.Table.Person')]: 'personNo',
                [vm.$t('ProductionLineEvent.Table.EventType')]: 'eventTypeName',
                [vm.$t('ProductionLineEvent.Table.WarningLevel')]: 'warningLevelName',
                [vm.$t('Events.Table.Device')]: 'deviceName',
            }
            var pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'accessTime',
                sortDesc: true,
            }
            var formData =
                new URLSearchParams(pagination).toString() +
                '&' +
                new URLSearchParams(exportSearchForm).toString()
            var response = await this.$services.get(
                vm.$refs.eventTable.dataUrl + '?' + formData
            )
            for (const item of response.data.data.data) {
                item.accessTimeStr = this.getTime(item.accessTime)
                item.accessDateStr = this.getDate(item.accessTime)
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
}
</style>
