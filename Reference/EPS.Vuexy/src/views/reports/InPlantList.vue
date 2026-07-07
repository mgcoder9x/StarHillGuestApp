<!-- src/views/vehicleSessions/InPlantList.vue -->
<!-- eslint-disable vue/html-self-closing -->
<template>
    <div>
        <validation-observer ref="rules">
            <!-- FILTERS -->
            <b-card no-body>
                <b-card-body>
                    <b-form @submit.prevent="searchImmediate">
                        <!-- submit ẩn để Enter trong input kích hoạt tìm kiếm ngay -->
                        <button
                            type="submit"
                            style="display: none"
                            aria-hidden="true"
                        ></button>

                        <!-- Row 1: From | VehicleType (single) | Areas (multi) -->
                        <b-row>
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('VehicleInPlant.Field.FromDate')"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <validation-provider
                                        #default="{ errors }"
                                        :rules="`fromDate:` + searchForm.to"
                                        name="fromDate"
                                    >
                                        <date-picker
                                            v-model="searchForm.from"
                                            type="datetime"
                                            :locale="currentLocale"
                                            format="DD-MM-YYYY HH:mm:ss"
                                            value-type="YYYY-MM-DD HH:mm:ss"
                                            style="width: 100%"
                                            @change="searchDebounced"
                                        />
                                        <small class="text-danger">{{
                                            errors[0]
                                        }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>

                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="
                                        $t('VehicleInPlant.Field.VehicleType')
                                    "
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <v-select
                                        v-model="searchForm.vehicleTypeUi"
                                        :options="vehicleTypeOptions"
                                        :reduce="(i) => i.id"
                                        label="text"
                                        :multiple="false"
                                        :clearable="true"
                                        @input="onVehicleTypeChange"
                                        @clear="onVehicleTypeClear"
                                    />
                                </b-form-group>
                            </b-col>

                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('VehicleInPlant.Field.Areas')"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <v-select
                                        v-model="searchForm.areaIds"
                                        :options="areaOptionsI18n"
                                        :reduce="(i) => i.id"
                                        label="text"
                                        :multiple="true"
                                        :clearable="true"
                                        @input="searchDebounced"
                                        @clear="searchDebounced"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>

                        <!-- Row 2: To | Plate | Owner -->
                        <b-row>
                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('VehicleInPlant.Field.ToDate')"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <date-picker
                                        v-model="searchForm.to"
                                        type="datetime"
                                        :locale="currentLocale"
                                        format="DD-MM-YYYY HH:mm:ss"
                                        value-type="YYYY-MM-DD HH:mm:ss"
                                        style="width: 100%"
                                        @change="searchDebounced"
                                    />
                                </b-form-group>
                            </b-col>

                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('VehicleInPlant.Field.Plate')"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <b-form-input
                                        v-model="searchForm.plate"
                                        @input="searchDebounced"
                                    />
                                </b-form-group>
                            </b-col>

                            <b-col :md="form.colMd">
                                <b-form-group
                                    :label="$t('VehicleInPlant.Field.Owner')"
                                    :label-cols-md="form.labelColsMd"
                                >
                                    <b-form-input
                                        v-model="searchForm.ownerName"
                                        @input="searchDebounced"
                                    />
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </b-form>
                </b-card-body>
            </b-card>
        </validation-observer>

        <!-- TABLE -->
        <b-card>
            <!-- TOOLBAR -->
            <div
                class="table-toolbar d-flex justify-content-end"
                style="gap: 8px"
            >
                <b-button
                    v-waves
                    variant="primary"
                    class="btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <downloadExcel
                        :header="excelHeader"
                        :name="$t('VehicleInPlant.Export.FileName')"
                        :fetch="exportData"
                        type="xlsx"
                        :fields="exportFields"
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
                    class="btn-hover-linear-primary border-0 d-flex align-items-center"
                    @click="refresh"
                >
                    <Icon icon="famicons:enter-outline" class="sm-icon" />
                    <span class="ml-25">{{ $t('Button.Refresh') }}</span>
                </b-button>
            </div>

            <BasicTable
                ref="table"
                :key="tableKey"
                :columns="columns"
                data-url="/vehicleEvent/sessions/inside"
                :search-form="cleanedSearchForm"
                :sort-by="'inTime'"
                storage-name="vehicleInPlantTable"
            >
                <template #table-row="props">
                    <span v-if="props.column.field === 'inDate'">
                        {{ toDate(props.row.inTime) }}
                    </span>
                    <span v-else-if="props.column.field === 'inTimeOnly'">
                        {{ toTime(props.row.inTime) }}
                    </span>
                    <span v-else-if="props.column.field === 'vehicleType'">
                        {{ getVehicleTypeNameFromApi(props.row.vehicleType) }}
                    </span>
                    <span v-else>
                        {{ props.formattedRow[props.column.field] }}
                    </span>
                </template>
            </BasicTable>
        </b-card>
    </div>
</template>

<script>
/* eslint-disable */
import moment from 'moment'
import { listVehicleType } from '@/data'
import { lookupService } from '@/services'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'

/**
 * ====== MAP ENUM ======
 * Nếu backend đã chuẩn 1=Car, 2=Motorbike → giữ identity map.
 */
const MAP_UI_TO_API = { 1: 1, 2: 2 }
const MAP_API_TO_UI = { 1: 1, 2: 2 }

export default {
    name: 'InPlantList',
    data() {
        return {
            storageKey: 'vehicleInPlantSearch',
            form: { colMd: 4, labelColsMd: 3 },
            listVehicleType, // [{ id, text }]
            areaOptionsRaw: [], // [{ id, name, labelKey? }]
            tableKey: 0,
            searchForm: {
                from: null,
                to: null,
                vehicleTypeUi: null, // UI value (1=Car,2=Motorbike theo UI)
                areaIds: [], // MULTI chọn khu vực theo ID
                plate: null,
                ownerName: null,
            },
            columns: [
                { label: 'VehicleInPlant.Col.Date', field: 'inDate' },
                { label: 'VehicleInPlant.Col.TimeIn', field: 'inTimeOnly' },
                { label: 'VehicleInPlant.Col.Plate', field: 'licensePlates' },
                {
                    label: 'VehicleInPlant.Col.VehicleType',
                    field: 'vehicleType',
                },
                { label: 'VehicleInPlant.Col.LaneIn', field: 'inLaneName' },
                { label: 'VehicleInPlant.Col.Owner', field: 'ownerName' },
                { label: 'VehicleInPlant.Col.Stay', field: 'stayMinutes' },
            ],
            excelHeader: [],
            exportFields: {},
            // ---- State cho tìm kiếm an toàn
            _searchDebounceTimer: null,
            _searchBusy: false,
        }
    },

    computed: {
        currentLocale() {
            return this.$i18n?.locale || 'vi'
        },
        vehicleTypeOptions() {
            return this.listVehicleType.map((x) => ({
                id: Number(x.id),
                text: this.$t(x.text),
            }))
        },
        // Options khu vực: hiển thị text, giữ id để gửi lên AreaIds
        areaOptionsI18n() {
            return (this.areaOptionsRaw || []).map((a) => ({
                id: a.id,
                name: a.name,
                text: a.labelKey ? this.$t(a.labelKey) : a.name,
            }))
        },
        /**
         * Payload gửi lên API (PascalCase) theo VehicleInPlantGridPagingDto:
         * From, To, VehicleType, AreaIds(int[]), Plate, OwnerName
         */
        cleanedSearchForm() {
            const o = {}
            const sf = this.searchForm

            if (sf.from) o.From = sf.from
            if (sf.to) o.To = sf.to

            if (
                sf.vehicleTypeUi !== null &&
                sf.vehicleTypeUi !== undefined &&
                sf.vehicleTypeUi !== ''
            ) {
                const uiVal = Number(sf.vehicleTypeUi)
                const apiVal = MAP_UI_TO_API[uiVal] ?? uiVal
                o.VehicleType = apiVal
            }

            if (Array.isArray(sf.areaIds) && sf.areaIds.length) {
                o.AreaIds = sf.areaIds.slice()
            }

            if (sf.plate) o.Plate = sf.plate
            if (sf.ownerName) o.OwnerName = sf.ownerName

            if (sf.compId) o.CompId = sf.compId

            return o
        },
    },

    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        // 1) Khôi phục filter từ cache (phiên bản mới: dùng areaIds)
        //  const cached = getStorage(this.storageKey)
        //  if (cached) {
        //      const migrated = { ...cached }

        //      // migrate legacy vehicleTypes -> vehicleTypeUi nếu có
        //      if (
        //          Array.isArray(migrated.vehicleTypes) &&
        //          !migrated.vehicleTypeUi
        //      ) {
        //          migrated.vehicleTypeUi = migrated.vehicleTypes.length
        //              ? Number(migrated.vehicleTypes[0])
        //              : null
        //          delete migrated.vehicleTypes
        //      }
        //      if (
        //          migrated.vehicleType !== undefined &&
        //          migrated.vehicleType !== null &&
        //          migrated.vehicleType !== ''
        //      ) {
        //          const apiVal = Number(migrated.vehicleType)
        //          migrated.vehicleTypeUi = MAP_API_TO_UI[apiVal] ?? apiVal
        //          delete migrated.vehicleType
        //      }

        //      this.searchForm = { ...this.searchForm, ...migrated }
        // } else {
        // 2) Không có cache → set mặc định NGAY (trước mọi await)
        this.searchForm.from = moment().format('YYYY-MM-DD 00:00:00')
        this.searchForm.to = moment().format('YYYY-MM-DD 23:59:59')
        // }

        // 3) Load danh sách khu vực ASYNC, KHÔNG chặn filter
        lookupService.getAreas().then((areas) => {
            this.areaOptionsRaw = (areas || []).map((a) => ({
                id: a.id,
                name: a.name ?? a.text ?? String(a.id ?? ''),
                labelKey: a.labelKey,
            }))
        })

        // 4) Gọi search sau khi table render
        this.$nextTick(() => this.searchImmediate())
    },

    watch: {
        '$i18n.locale'() {
            this.tableKey++
            this.$nextTick(() => this.searchImmediate())
        },
    },

    methods: {
        toDate(v) {
            return v ? moment(v).format('DD/MM/YYYY') : ''
        },
        toTime(v) {
            return v ? moment(v).format('HH:mm') : ''
        },

        getVehicleTypeNameFromApi(apiValue) {
            const uiVal = MAP_API_TO_UI[Number(apiValue)] ?? Number(apiValue)
            const x = this.listVehicleType.find((v) => Number(v.id) === uiVal)
            return x ? this.$t(x.text) : ''
        },

        onVehicleTypeChange(val) {
            this.searchForm.vehicleTypeUi =
                val === null || val === undefined || val === ''
                    ? null
                    : Number(val)
            this.searchDebounced()
        },
        onVehicleTypeClear() {
            this.searchForm.vehicleTypeUi = null
            this.searchDebounced()
        },

        // ====== TÌM KIẾM AN TOÀN (debounce + lock) ======
        async searchDebounced() {
            const ok = await this.$refs.rules.validate()
            if (!ok) return
            if (this._searchDebounceTimer)
                clearTimeout(this._searchDebounceTimer)
            this._searchDebounceTimer = setTimeout(() => {
                this.searchCore()
            }, 200)
        },
        searchImmediate() {
            if (this._searchDebounceTimer) {
                clearTimeout(this._searchDebounceTimer)
                this._searchDebounceTimer = null
            }
            this.searchCore(true)
        },
        searchCore(immediate = false) {
            if (this._searchBusy) return
            this._searchBusy = true
            try {
                setStorage(this.storageKey, this.searchForm, 120)
                if (immediate) {
                    this.$refs.table && this.$refs.table.refresh()
                } else {
                    this.$nextTick(
                        () => this.$refs.table && this.$refs.table.refresh()
                    )
                }
            } finally {
                setTimeout(() => {
                    this._searchBusy = false
                }, 50)
            }
        },

        // ====== REFRESH ======
        refresh() {
            //clearStorage(this.storageKey)

            // reset filter về mặc định full ngày hôm nay
            const accessToken = this.$services.getUserData()
            this.searchForm.compId = accessToken.companyId
            this.searchForm = {
                from: moment().format('YYYY-MM-DD 00:00:00'),
                to: moment().format('YYYY-MM-DD 23:59:59'),
                vehicleTypeUi: null,
                areaIds: [],
                plate: null,
                ownerName: null,
                compId: this.searchForm.compId,
            }

            // đợi Vue cập nhật xong rồi gọi forceSearch (1 request, from/to mới)
            this.$nextTick(() => {
                this.forceSearch()
            })
        },
        // Gọi API ngay với searchForm hiện tại, bỏ qua debounce & lock
        forceSearch() {
            // huỷ mọi debounce đang chờ
            if (this._searchDebounceTimer) {
                clearTimeout(this._searchDebounceTimer)
                this._searchDebounceTimer = null
            }

            // mở khoá để lần này luôn được refresh
            this._searchBusy = false

            // lưu cache mới
            setStorage(this.storageKey, this.searchForm, 120)

            // refresh bảng ngay lập tức
            this.$refs.table && this.$refs.table.refresh()
        },
        // ====== EXPORT ======
        buildQuery(paramsObj) {
            const p = new URLSearchParams()
            for (const [k, v] of Object.entries(paramsObj)) {
                if (v == null || v === '') continue
                if (Array.isArray(v)) {
                    if (!v.length) continue
                    v.forEach((item) => {
                        if (item != null && item !== '') p.append(k, item)
                    })
                } else {
                    p.append(k, v)
                }
            }
            return p.toString()
        },

        async exportData() {
            const from =
                this.toDate(this.searchForm.from) +
                ' ' +
                moment(this.searchForm.from).format('HH:mm')
            const to =
                this.toDate(this.searchForm.to) +
                ' ' +
                moment(this.searchForm.to).format('HH:mm')

            const uiVal = this.searchForm.vehicleTypeUi
            const vt =
                uiVal !== null && uiVal !== undefined && uiVal !== ''
                    ? this.listVehicleType.find(
                          (v) => Number(v.id) === Number(uiVal)
                      )
                        ? this.$t(
                              this.listVehicleType.find(
                                  (v) => Number(v.id) === Number(uiVal)
                              ).text
                          )
                        : this.$t('Export.All')
                    : this.$t('Export.All')

            const idToText = new Map(
                this.areaOptionsI18n.map((o) => [o.id, o.text])
            )
            const areasTxt =
                (this.searchForm.areaIds || [])
                    .map((id) => idToText.get(id) || id)
                    .join(', ') || this.$t('Export.All')
            const plate = this.searchForm.plate || this.$t('Export.All')
            const owner = this.searchForm.ownerName || this.$t('Export.All')

            this.excelHeader = [
                this.$t('VehicleInPlant.Export.Header'),
                `${this.$t('VehicleInPlant.Field.FromDate')}: ${from}     ${this.$t('VehicleInPlant.Field.ToDate')}: ${to}`,
                `${this.$t('VehicleInPlant.Field.VehicleType')}: ${vt}`,
                `${this.$t('VehicleInPlant.Field.Areas')}: ${areasTxt}`,
                `${this.$t('VehicleInPlant.Field.Plate')}: ${plate}     ${this.$t('VehicleInPlant.Field.Owner')}: ${owner}`,
            ]

            this.exportFields = {
                [this.$t('VehicleInPlant.Col.Date')]: 'inDate',
                [this.$t('VehicleInPlant.Col.TimeIn')]: 'inTimeOnly',
                [this.$t('VehicleInPlant.Col.Plate')]: 'licensePlates',
                [this.$t('VehicleInPlant.Col.VehicleType')]: 'vehicleTypeStr',
                [this.$t('VehicleInPlant.Col.LaneIn')]: 'inGateName',
                [this.$t('VehicleInPlant.Col.Owner')]: 'ownerName',
                [this.$t('VehicleInPlant.Col.Stay')]: 'stayMinutes',
            }

            const pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'inTime',
                sortDesc: true,
            }

            const formData = this.buildQuery({
                ...pagination,
                ...this.cleanedSearchForm,
            })
            const resp = await this.$services.get(
                '/vehicleEvent/sessions/inside?' + formData
            )

            return (resp.data?.data?.data || []).map((r) => ({
                ...r,
                inDate: this.toDate(r.inTime),
                inTimeOnly: this.toTime(r.inTime),
                vehicleTypeStr: this.getVehicleTypeNameFromApi(r.vehicleType),
            }))
        },
    },
}
</script>

<style lang="scss">
/* ===== Kill-switch: ngăn toolbar bị clone vào thead khi header sticky/clone ===== */
.table thead .table-toolbar,
.b-table thead .table-toolbar,
thead .table-toolbar {
    display: none !important;
}

/* Toolbar thật: nổi trên bảng, không bị sticky header che hoặc lôi theo */
.table-toolbar {
    position: relative;
    z-index: 3;
}

/* Đảm bảo vùng bảng nằm dưới toolbar trong stacking context */
.card .table-responsive,
.card .table {
    position: relative;
    z-index: 1;
}
</style>
