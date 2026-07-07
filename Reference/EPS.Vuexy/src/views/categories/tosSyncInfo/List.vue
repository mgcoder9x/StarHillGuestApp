<template>
    <b-container fluid>
        <b-card>
            <b-form @submit.prevent="search">
                <b-row>
                    <b-col md="4">
                        <b-form-group
                            :label="this.$t('Events.SearchForm.FromDate')"
                            label-for="h-searchForm-dateFrom"
                            label-cols-md="3"
                        >
                            <validation-provider
                                #default="{ errors }"
                                :rules="`fromDate:` + searchForm.dateTimeTo"
                                name="NotificationEventTemplateName"
                            >
                                <date-picker
                                    id="h-searchForm-dateFrom"
                                    v-model="searchForm.dateTimeFrom"
                                    type="datetime"
                                    :locale="currentLocale"
                                    format="DD-MM-YYYY HH:mm:ss"
                                    value-type="YYYY-MM-DD HH:mm:ss"
                                    style="width: 100%"
                                    @change="search"
                                ></date-picker>
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <b-col md="4">
                        <b-form-group
                            :label="$t('categories.tosSyncInfo.planCode')"
                            label-for="planCode"
                            :label-cols="form.labelCols"
                            :label-align="form.labelAlign"
                        >
                            <b-form-input
                                id="planCode"
                                v-model="searchForm.planCode"
                                :placeholder="
                                    $t('common.form.placeholder.enterValue')
                                "
                                @input="search"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="4">
                        <b-form-group
                            :label="$t('categories.tosSyncInfo.vesselCode')"
                            label-for="vesselCode"
                            :label-cols="form.labelCols"
                            :label-align="form.labelAlign"
                        >
                            <b-form-input
                                id="vesselCode"
                                v-model="searchForm.vesselCode"
                                :placeholder="
                                    $t('common.form.placeholder.enterValue')
                                "
                                @input="search"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="4">
                        <b-form-group
                            :label="this.$t('Events.SearchForm.ToDate')"
                            label-for="h-searchForm-dateTo"
                            label-cols-md="3"
                        >
                            <date-picker
                                id="h-searchForm-dateTo"
                                v-model="searchForm.dateTimeTo"
                                type="datetime"
                                :locale="currentLocale"
                                format="DD-MM-YYYY HH:mm:ss"
                                value-type="YYYY-MM-DD HH:mm:ss"
                                style="width: 100%"
                                @change="search"
                            ></date-picker>
                        </b-form-group>
                    </b-col>
                    <b-col md="4">
                        <b-form-group
                            :label="$t('categories.tosSyncInfo.cargoCode')"
                            label-for="cargoCode"
                            :label-cols="form.labelCols"
                            :label-align="form.labelAlign"
                        >
                            <b-form-input
                                id="cargoCode"
                                v-model="searchForm.cargoCode"
                                :placeholder="
                                    $t('common.form.placeholder.enterValue')
                                "
                                @input="search"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="4">
                        <b-form-group
                            :label="
                                $t('categories.tosSyncInfo.vehicleLicensePlate')
                            "
                            label-for="vehicleLicensePlate"
                            :label-cols="form.labelCols"
                            :label-align="form.labelAlign"
                        >
                            <b-form-input
                                id="vehicleLicensePlate"
                                v-model="searchForm.licensePlate"
                                :placeholder="
                                    $t('common.form.placeholder.enterValue')
                                "
                                @input="search"
                            />
                        </b-form-group>
                    </b-col>
                    <!-- <b-col md="4">
                        <b-form-group
                            :label="$t('categories.tosSyncInfo.dateTime')"
                            label-for="dateTime"
                            :label-cols="form.labelCols"
                            :label-align="form.labelAlign"
                        >
                            <date-picker
                                id="dateTime"
                                v-model="multiSelect.dateTime"
                                type="datetime"
                                :clearable="false"
                                format="DD-MM-YYYY HH:mm:ss"
                                value-type="YYYY-MM-DD HH:mm:ss"
                                class="w-100"
                                :placeholder="
                                    $t('common.form.placeholder.enterValue')
                                "
                                range
                                @input="search"
                            />
                        </b-form-group>
                    </b-col> -->
                </b-row>
            </b-form>
        </b-card>
        <b-card>
            <div class="text-left mb-1 d-flex" style="gap: 8px">
                <b-button
                    v-waves
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="
                            $t('categories.tosSyncInfo.title') ||
                            'TOS_Sync_Info'
                        "
                        :fetch="exportData"
                        type="xlsx"
                        :fields="exportFieldsVi"
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
                    <Icon icon="clarity:refresh-line" class="sm-icon" />
                    <span class="ml-25">
                        {{ $t('Button.Refresh') }}
                    </span>
                </b-button>
            </div>
            <BasicTable
                ref="tosSyncInfoTable"
                :columns="table.columns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                :sort-by="'accessAt'"
                storage-name="TosSyncInfoColumnsCache"
                :initial-page="initialPage"
                :initial-items-per-page="initialItemsPerPage"
                @pagination="getPagination"
            >
                <template v-slot:table-row="{ column, row }">
                    <span v-if="column.field === 'eta'" class="text-nowrap">
                        {{
                            !row.eta
                                ? ''
                                : $moment(row.eta).format('DD-MM-YYYY HH:mm:ss')
                        }}
                    </span>
                    <span
                        v-else-if="column.field === 'accessAt'"
                        class="text-nowrap"
                    >
                        {{
                            !row.accessAt
                                ? ''
                                : $moment(row.accessAt).format(
                                      'DD-MM-YYYY HH:mm:ss'
                                  )
                        }}
                    </span>
                    <!-- Show multiple plans warning -->
                    <span v-else-if="column.field === 'planCode'">
                        <!-- <b-badge 
                            v-if="row.hasMultiplePlans" 
                            variant="warning"
                            v-b-tooltip.hover 
                            :title="getAllPlansTooltip(row)"
                        >
                            {{ $t('categories.tosSyncInfo.multiplePlans') }}
                            ({{ row.planCount }})
                        </b-badge>
                        <span v-else>{{ row.planCode || '-' }}</span> -->
                        <span>{{ row.planCode || '-' }}</span>
                    </span>
                    <!-- Show mismatch warning -->
                    <span v-else-if="column.field === 'cargoCode'">
                        <div class="d-flex align-items-center">
                            <span>{{ row.cargoCode || '-' }}</span>
                            <b-badge
                                v-if="row.isPlanDirectionMismatch"
                                variant="danger"
                                class="ml-1"
                                v-b-tooltip.hover
                                :title="row.mismatchReason"
                            >
                                <feather-icon
                                    icon="AlertTriangleIcon"
                                    size="14"
                                />
                            </b-badge>
                        </div>
                    </span>
                </template>
            </BasicTable>
        </b-card>
    </b-container>
</template>
<script>
import {
    defineComponent,
    ref,
    reactive,
    computed,
    watch,
    getCurrentInstance,
    nextTick,
} from '@vue/composition-api'
import debounce from 'lodash/debounce'
import { getStorage, setStorage } from '@/utils/cacheHelper'
import { getLabel } from '@/utils/arrayHelper.cs'

export default defineComponent({
    name: 'TosSyncInfoList',
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    setup() {
        // Lấy vm để dùng $t, $store trong setup (Vue 2 + composition-api)
        const vm = getCurrentInstance().proxy

        // ===== cache & state =====
        const storageKey = 'TosSyncInfoCache'
        const searchCache = getStorage(storageKey)

        const searchFormBlank = {
            planCode: '',
            vesselCode: '',
            cargoCode: '',
            licensePlate: '',
            dateTimeFrom: vm.$moment().format('YYYY-MM-DD 00:00:00'),
            dateTimeTo: vm.$moment().format('YYYY-MM-DD 23:59:59'),
        }
        const multiSelectBlank = {
            dateTime: [
                vm.$moment().format('YYYY-MM-DD 00:00:00'),
                vm.$moment().format('YYYY-MM-DD 23:59:59'),
            ],
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

        const form = reactive({
            labelCols: 3,
            labelAlign: 'left',
        })

        const table = reactive({
            dataUrl: '/tosSyncInfo',
            columns: [
                { label: 'categories.tosSyncInfo.eta', field: 'eta' },
                { label: 'categories.tosSyncInfo.accessAt', field: 'accessAt' },
                { label: 'categories.tosSyncInfo.planCode', field: 'planCode' },
                {
                    label: 'categories.tosSyncInfo.licensePlate',
                    field: 'licensePlate',
                },
                {
                    label: 'categories.tosSyncInfo.cargoCode',
                    field: 'cargoCode',
                },
                {
                    label: 'categories.tosSyncInfo.vesselCode',
                    field: 'vesselCode',
                },
            ],
        })
        const exportFieldsVi = ref({})
        const headerExcelDetail = ref([])

        const tosSyncInfoTable = ref(null)

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
            tosSyncInfoTable.value?.refresh?.()
        }

        // Debounce KHÔNG lệ thuộc "this" → tránh lỗi mất ngữ cảnh
        const search = debounce(() => {
            searchNow()
        }, 1000)

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

        // Cập nhật dateTimeFrom/To khi chọn range
        watch(
            () => multiSelect.dateTime,
            (range) => {
                const [from, to] = range || []
                searchForm.dateTimeFrom = from || ''
                searchForm.dateTimeTo = to || ''
            },
            { deep: true, immediate: true }
        )

        // Export Excel function
        async function exportData() {
            const dateFrom = vm
                .$moment(searchForm.dateTimeFrom)
                .format('DD/MM/YYYY HH:mm')
            const dateTo = vm
                .$moment(searchForm.dateTimeTo)
                .format('DD/MM/YYYY HH:mm')

            const planCodeValue = searchForm.planCode || vm.$t('Export.All')
            const vesselCodeValue = searchForm.vesselCode || vm.$t('Export.All')
            const cargoCodeValue = searchForm.cargoCode || vm.$t('Export.All')
            const licensePlateValue =
                searchForm.licensePlate || vm.$t('Export.All')

            // Set Excel header
            headerExcelDetail.value = [
                vm.$t('categories.tosSyncInfo.title') || 'TOS Sync Info Export',
                `${vm.$t(
                    'Events.SearchForm.FromDate'
                )}: ${dateFrom}      ${vm.$t(
                    'Events.SearchForm.ToDate'
                )}: ${dateTo}`,
                `${vm.$t('categories.tosSyncInfo.planCode')}: ${planCodeValue}`,
                `${vm.$t(
                    'categories.tosSyncInfo.vesselCode'
                )}: ${vesselCodeValue}`,
                `${vm.$t(
                    'categories.tosSyncInfo.cargoCode'
                )}: ${cargoCodeValue}`,
                `${vm.$t(
                    'categories.tosSyncInfo.vehicleLicensePlate'
                )}: ${licensePlateValue}`,
            ]

            // Define export fields to match table columns
            exportFieldsVi.value = {
                [vm.$t('categories.tosSyncInfo.eta')]: 'etaStr',
                [vm.$t('categories.tosSyncInfo.accessAt')]: 'accessAtStr',
                [vm.$t('categories.tosSyncInfo.planCode')]: 'planCode',
                [vm.$t('categories.tosSyncInfo.licensePlate')]: 'licensePlate',
                [vm.$t('categories.tosSyncInfo.cargoCode')]: 'cargoCode',
                [vm.$t('categories.tosSyncInfo.vesselCode')]: 'vesselCode',
            }

            const pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'accessAt',
                sortDesc: true,
            }

            const formData = `${new URLSearchParams(
                pagination
            ).toString()}&${new URLSearchParams(searchForm).toString()}`

            try {
                const response = await vm.$services.get(
                    `${table.dataUrl}?${formData}`
                )

                const data = response.data.data.data || response.data.data || []

                // Format data for export
                data.forEach((item) => {
                    // eslint-disable-next-line no-param-reassign
                    item.etaStr = item.eta
                        ? vm.$moment(item.eta).format('DD-MM-YYYY HH:mm:ss')
                        : ''
                    // eslint-disable-next-line no-param-reassign
                    item.accessAtStr = item.accessAt
                        ? vm
                              .$moment(item.accessAt)
                              .format('DD-MM-YYYY HH:mm:ss')
                        : ''
                    // eslint-disable-next-line no-param-reassign
                    item.planCode = item.planCode || ''
                    // eslint-disable-next-line no-param-reassign
                    item.licensePlate = item.licensePlate || ''
                    // eslint-disable-next-line no-param-reassign
                    item.cargoCode = item.cargoCode || ''
                    // eslint-disable-next-line no-param-reassign
                    item.vesselCode = item.vesselCode || ''
                })

                return data
            } catch (error) {
                console.error('Export data error:', error)
                return []
            }
        }

        // Get all plans tooltip text
        function getAllPlansTooltip(row) {
            if (!row.allPlanCodes) return ''
            try {
                const plans = JSON.parse(row.allPlanCodes)
                return `${vm.$t(
                    'categories.tosSyncInfo.allPlans'
                )}: ${plans.join(', ')}`
            } catch {
                return row.allPlanCodes
            }
        }

        return {
            searchForm,
            multiSelect,
            form,
            table,
            isRTL,
            initialPage,
            initialItemsPerPage,
            getLabel,
            tosSyncInfoTable,
            search,
            searchNow,
            refresh,
            getPagination,
            exportFieldsVi,
            headerExcelDetail,
            exportData,
            getAllPlansTooltip,
        }
    },
})
</script>
