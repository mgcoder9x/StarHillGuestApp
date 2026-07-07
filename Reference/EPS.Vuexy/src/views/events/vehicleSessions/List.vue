<!-- src/views/vehicleSessions/List.vue -->
<!-- eslint-disable vue/html-self-closing -->
<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <!-- ROW 1: From | To | Lane | VehicleType -->
                    <b-row>
                        <!-- FromDate -->
                        <b-col :md="form.colMd">
                            <b-form-group
                                :label="$t('VehicleSessions.Field.FromDate')"
                                label-for="vs-from"
                                :label-cols-md="form.labelColsMd"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="`fromDate:` + (searchForm.to || '')"
                                    name="fromDate"
                                >
                                    <date-picker
                                        id="vs-from"
                                        v-model="searchForm.from"
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
                        <b-col :md="form.colMd">
                            <b-form-group
                                :label="
                                    $t('VehicleSessions.Field.LicensePlate')
                                "
                                label-for="vs-plate"
                                :label-cols-md="form.labelColsMd"
                            >
                                <b-form-input
                                    id="vs-plate"
                                    v-model="searchForm.plate"
                                    type="text"
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col :md="form.colMd">
                            <b-form-group
                                :label="$t('VehicleSessions.Field.VehicleType')"
                                :label-cols-md="form.labelColsMd"
                            >
                                <v-select
                                    v-model="searchForm.vehicleType"
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    label="text"
                                    :reduce="(x) => x.id"
                                    :options="rechangeOptions(listVehicleType)"
                                    :clearable="true"
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                        <!-- ToDate -->

                        <!-- Lane (Vào / Ra) -->
                        <!-- <b-col :md="form.colMd">
                            <b-form-group
                                :label="$t('VehicleSessions.Field.Lane')"
                                :label-cols-md="form.labelColsMd"
                            >
                                <v-select
                                    v-model="searchForm.laneType"
                                    :dir="
                                        $store.state.appConfig.isRTL
                                            ? 'rtl'
                                            : 'ltr'
                                    "
                                    :options="laneOptions"
                                    label="text"
                                    :reduce="(x) => x.id"
                                    :clearable="true"
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col> -->
                    </b-row>

                    <!-- ROW 2: Plate | Owner -->
                    <b-row>
                        <!-- Plate -->
                        <b-col :md="form.colMd">
                            <b-form-group
                                :label="$t('VehicleSessions.Field.ToDate')"
                                label-for="vs-to"
                                :label-cols-md="form.labelColsMd"
                            >
                                <date-picker
                                    id="vs-to"
                                    v-model="searchForm.to"
                                    type="datetime"
                                    :locale="currentLocale"
                                    format="DD-MM-YYYY HH:mm:ss"
                                    value-type="YYYY-MM-DD HH:mm:ss"
                                    style="width: 100%"
                                    @change="search"
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Owner -->
                        <b-col :md="form.colMd">
                            <b-form-group
                                :label="$t('VehicleSessions.Field.Owner')"
                                label-for="vs-owner"
                                :label-cols-md="form.labelColsMd"
                            >
                                <b-form-input
                                    id="vs-owner"
                                    v-model="searchForm.ownerName"
                                    type="text"
                                    @input="search"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>

        <!-- Table -->
        <b-card>
            <!-- Toolbar -->
            <div class="d-flex justify-content-end" style="gap: 8px">
                <b-button
                    v-waves
                    variant="primary"
                    class="btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <downloadExcel
                        :header="excelHeader"
                        :name="$t('VehicleSessions.Export.FileName')"
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
                :columns="columns"
                data-url="/vehicleEvent/sessions"
                :search-form="searchForm"
                :sort-by="'inTime'"
                storage-name="vehicleSessionsTable"
            >
                <template #table-row="props">
                    <span v-if="props.column.field === 'inTime'">{{
                        formatDt(props.row.inTime)
                    }}</span>
                    <span v-else-if="props.column.field === 'outTime'">{{
                        formatDt(props.row.outTime)
                    }}</span>
                    <span v-else-if="props.column.field === 'stayMinutes'">{{
                        props.row.stayMinutes || 0
                    }}</span>
                    <span v-else-if="props.column.field === 'vehicleType'">{{
                        getVehicleTypeName(props.row.vehicleType)
                    }}</span>
                    <span v-else-if="props.column.field === 'status'">
                        <b-badge :variant="statusVariant(props.row.status)">{{
                            getStatusName(props.row.status)
                        }}</b-badge>
                    </span>
                    <span v-else-if="props.column.field === 'action'">
                        <div class="center-icon text-nowrap">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/event/vehicleSessionsEvent/detail/${props.row.id}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                        </div>
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
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'

export default {
    name: 'VehicleSessionsList',
    data() {
        return {
            storageKey: 'vehicleSessionsSearchForm',
            listVehicleType,
            // 4 cột / hàng
            form: { colMd: 4, labelColsMd: 4 },

            // Chỉ 2 lựa chọn tĩnh
            laneOptions: [
                { id: 'in', text: this.$t('VehicleSessions.Field.In') },
                { id: 'out', text: this.$t('VehicleSessions.Field.Out') },
            ],

            searchForm: {
                from: null,
                to: null,
                laneType: null, // 'in' | 'out'
                vehicleType: null,
                plate: null,
                ownerName: null,
                compId: null,
                // giữ cho table (không dùng để filter theo làn nữa)
                status: null,
            },

            columns: [
                { label: 'VehicleSessions.Field.InTime', field: 'inTime' },
                { label: 'VehicleSessions.Field.OutTime', field: 'outTime' },
                { label: 'VehicleSessions.Field.Stay', field: 'stayMinutes' },
                {
                    label: 'VehicleSessions.Field.LicensePlate',
                    field: 'licensePlates',
                },
                {
                    label: 'VehicleSessions.Field.VehicleType',
                    field: 'vehicleType',
                },
                { label: 'VehicleSessions.Field.Owner', field: 'ownerName' },
                // { label: 'VehicleSessions.Field.InGate', field: 'inGateName' },
                { label: 'VehicleSessions.Field.InGate', field: 'areaName' },
                // {
                //     label: 'VehicleSessions.Field.OutGate',
                //     field: 'outGateName',
                // },
                {
                    label: 'VehicleSessions.Field.OutGate',
                    field: 'areaName',
                },
                // { label: 'VehicleSessions.Field.Status1', field: 'status' },
                { label: 'VehicleSessions.Field.Operation', field: 'action' },
            ],

            excelHeader: [],
            exportFields: {},
        }
    },

    created() {
        debugger
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        const cached = getStorage(this.storageKey)
        if (cached) {
            this.searchForm = { ...cached }
        } else {
            this.searchForm.from = moment().format('YYYY-MM-DD 00:00:00')
        }
        this.$nextTick(() => this.search())
    },

    computed: {
        currentLocale() {
            return this.$i18n?.locale || 'vi'
        },
    },

    methods: {
        rechangeOptions(list) {
            return list.map((it) => ({ ...it, text: this.$t(it.text) }))
        },
        formatDt(v) {
            return v ? moment(v).format('DD/MM/YYYY HH:mm:ss') : ''
        },
        getVehicleTypeName(id) {
            const x = this.listVehicleType.find((v) => v.id == id)
            return x ? this.$t(x.text) : ''
        },
        getStatusName(s) {
            const k =
                s === 1
                    ? 'Matched'
                    : s === 2
                      ? 'InOnly'
                      : s === 3
                        ? 'OutOnly'
                        : 'Unknown'
            return this.$t(`VehicleSessions.Status.${k}`)
        },
        statusVariant(s) {
            return s === 1
                ? 'success'
                : s === 2
                  ? 'warning'
                  : s === 3
                    ? 'secondary'
                    : 'light'
        },

        search() {
            setStorage(this.storageKey, this.searchForm, 120)
            this.$refs.table.refresh()
        },

        refresh() {
            clearStorage(this.storageKey)
            this.searchForm = {
                from: moment().format('YYYY-MM-DD 00:00:00'),
                to: null,
                laneType: null,
                vehicleType: null,
                plate: null,
                ownerName: null,
                status: null,
                compId: this.searchForm.compId,
            }
            this.$nextTick(() => this.$refs.table.refresh())
        },

        async exportData() {
            // Header: Title + From/To + VehicleType + Lane
            const from = this.searchForm.from
                ? moment(this.searchForm.from).format('DD/MM/YYYY HH:mm')
                : this.$t('Export.All')
            const to = this.searchForm.to
                ? moment(this.searchForm.to).format('DD/MM/YYYY HH:mm')
                : this.$t('Export.All')
            const vt = this.searchForm.vehicleType
                ? this.getVehicleTypeName(this.searchForm.vehicleType)
                : this.$t('Export.All')
            const laneStr = this.searchForm.laneType
                ? this.searchForm.laneType === 'in'
                    ? this.$t('Vào')
                    : this.$t('Ra')
                : this.$t('Export.All')

            this.excelHeader = [
                this.$t('VehicleSessions.Export.Title'),
                `${this.$t('VehicleSessions.Field.FromDate')}: ${from}     ${this.$t('VehicleSessions.Field.ToDate')}: ${to}`,
                `${this.$t('VehicleSessions.Field.VehicleType')}: ${vt}     ${this.$t('VehicleSessions.Field.Lane')}: ${laneStr}`,
            ]

            this.exportFields = {
                [this.$t('VehicleSessions.Field.InTime')]: 'inTime',
                [this.$t('VehicleSessions.Field.OutTime')]: 'outTime',
                [this.$t('VehicleSessions.Field.Stay')]: 'stayMinutes',
                [this.$t('VehicleSessions.Field.LicensePlate')]:
                    'licensePlates',
                [this.$t('VehicleSessions.Field.VehicleType')]:
                    'vehicleTypeStr',
                [this.$t('VehicleSessions.Field.Owner')]: 'ownerName',
                [this.$t('VehicleSessions.Field.InGate')]: 'inGateName',
                [this.$t('VehicleSessions.Field.OutGate')]: 'outGateName',
                [this.$t('VehicleSessions.Field.Status')]: 'statusStr',
            }

            const pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'inTime',
                sortDesc: false,
            }
            const formData =
                new URLSearchParams(pagination).toString() +
                '&' +
                new URLSearchParams(this.searchForm).toString()
            const resp = await this.$services.get(
                '/vehicleEvent/sessions?' + formData
            )

            return (resp.data?.data?.data || []).map((r) => ({
                ...r,
                inTime: this.formatDt(r.inTime),
                outTime: this.formatDt(r.outTime),
                vehicleTypeStr: this.getVehicleTypeName(r.vehicleType),
                statusStr: this.getStatusName(r.status),
            }))
        },
    },
}
</script>

<style lang="scss">
/* 4 cột mỗi hàng: From | To | Lane | VehicleType */
</style>
