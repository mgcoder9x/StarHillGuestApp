<template>
    <b-container fluid class="p-0">
        <b-card>
            <b-form @submit.prevent="search">
                <b-row>
                    <b-col :md="formConfig.colMd">
                        <b-form-group
                            :label="$t('label.dateTime')"
                            label-for="searchForm-dateTime"
                            :label-cols-md="formConfig.labelColMd"
                        >
                            <date-picker
                                id="searchForm-dateTime"
                                v-model="multiSelect.dateTime"
                                type="datetime"
                                :locale="currentLocale"
                                format="DD-MM-YYYY HH:mm:ss"
                                value-type="YYYY-MM-DD HH:mm:ss"
                                range
                                class="custom-date-picker w-100"
                                :placeholder="
                                    $t('common.form.placeholder.selectValue')
                                "
                                @change="search"
                            ></date-picker>
                        </b-form-group>
                    </b-col>
                    <b-col :md="formConfig.colMd">
                        <b-form-group
                            :label="$t('label.area')"
                            label-for="searchForm-area"
                            :label-cols-md="formConfig.labelColMd"
                        >
                            <tree-select
                                id="searchForm-area"
                                v-model="multiSelect.areas"
                                :dir="isRTL ? 'rtl' : 'ltr'"
                                :options="options.areasTree"
                                label="text"
                                :reduce="(item) => item.id"
                                :multiple="true"
                                value-consists-of="ALL"
                                :limit="3"
                                :limit-text="(count) => `+${count}`"
                                class="treeselect-nowrap"
                                :placeholder="
                                    $t('common.form.placeholder.selectValue')
                                "
                                @input="search"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col :md="formConfig.colMd">
                        <b-form-group
                            :label="$t('label.device')"
                            :label-cols-md="formConfig.labelColMd"
                            label-for="searchForm-device"
                        >
                            <v-select
                                id="searchForm-device"
                                v-model="multiSelect.devices"
                                label="text"
                                :dir="isRTL ? 'rtl' : 'ltr'"
                                multiple
                                allow-clear
                                :reduce="(item) => item.id"
                                :options="options.filterDeviceByArea"
                                :placeholder="
                                    $t('common.form.placeholder.selectValue')
                                "
                                @input="search"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col :md="formConfig.colMd">
                        <b-form-group
                            :label="$t('label.nameOrCode')"
                            :label-cols-md="formConfig.labelColMd"
                            label-for="searchForm-name-or-code"
                        >
                            <b-input
                                id="searchForm-name-or-code"
                                v-model="searchForm.filterNameCode"
                                :placeholder="
                                    $t('common.form.placeholder.enterValue')
                                "
                                @input="search"
                            />
                        </b-form-group>
                    </b-col>
                    <b-col :md="formConfig.colMd">
                        <b-form-group
                            :label="$t('label.status')"
                            :label-cols-md="formConfig.labelColMd"
                            label-for="searchForm-register-status"
                        >
                            <v-select
                                id="searchForm-register-status"
                                v-model="searchForm.isRegistered"
                                label="text"
                                :dir="isRTL ? 'rtl' : 'ltr'"
                                allow-clear
                                :reduce="(item) => item.id"
                                :options="options.registerStatus"
                                :placeholder="
                                    $t('common.form.placeholder.selectValue')
                                "
                                @input="search"
                            />
                        </b-form-group>
                    </b-col>
                </b-row>
            </b-form>
        </b-card>
        <!-- Table -->
        <b-card>
            <div class="mb-3 d-flex">
                <downloadExcel
                    :header="headerExcelDetail"
                    :name="$t('events.restrictedZonesEvent.excel.header')"
                    :fetch="exportData"
                    type="xlsx"
                    :fields="export_fields"
                >
                    <b-button
                        variant="success"
                        rounded
                        class="btn-hover-linear-success border-0 mr-25"
                    >
                        <Icon
                            icon="file-icons:microsoft-excel"
                            class="sm-icon mr-50"
                        />
                        <span>
                            {{ $t('common.button.export') }}
                        </span>
                    </b-button>
                </downloadExcel>
                <b-button
                    variant="warning"
                    rounded
                    class="btn-hover-linear-warning border-0"
                    @click="refresh"
                >
                    <Icon
                        icon="mingcute:refresh-1-fill"
                        class="sm-icon mr-50"
                    />
                    <span> {{ $t('common.button.refresh') }}</span>
                </b-button>
            </div>
            <BasicTable
                ref="restrictedZonesEventTable"
                :columns="table.columns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                :sort-by="'accessTime'"
                storage-name="restrictedZonesEventTableConfig"
            >
                <template slot="table-row" slot-scope="props">
                    <span v-if="props.column.field == 'accessDate'">
                        {{ $moment(props.row.accessTime).format('DD/MM/YYYY') }}
                    </span>
                    <span v-else-if="props.column.field == 'accessTime'">
                        {{ $moment(props.row.accessTime).format('HH:mm:ss') }}
                    </span>
                    <span
                        v-else-if="props.column.field == 'isRegistered'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        {{
                            isStranger(props.row.personId)
                                ? $t('constants.registerStatus.unregisted')
                                : $t('constants.registerStatus.registed')
                        }}
                    </span>
                    <span
                        v-else-if="props.column.field == 'image'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        <img
                            :src="`${baseURL}${props.row.filePath}`"
                            loading="lazy"
                            style="
                                width: 60px;
                                height: 80px;
                                object-fit: cover;
                                border-radius: 0.3em;
                            "
                            alt="Event"
                            class="cursor-pointer"
                            @click="showModalEvent(props.row.eventId)"
                        />
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="props.column.field === 'action'">
                        <div
                            class="center-icon text-nowrap"
                            style="display: flex; justify-content: center"
                        >
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/event/restrictedZonesEvent/Detail/${props.row.eventId}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                        </div>
                    </span>
                </template>
            </BasicTable>
        </b-card>
        <!-- Image Slider -->
        <b-modal v-model="modalImgEventShow" title="Ảnh sự kiện" size="lg">
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
import debounce from 'lodash/debounce'
import { lookupService } from '@/services'
import {
    ref,
    reactive,
    computed,
    getCurrentInstance,
    nextTick,
    onMounted,
    watch,
} from '@vue/composition-api'
import { setStorage, getStorage } from '@/utils/cacheHelper'
import { REGISTER_STATUS, registerStatus } from '@/constants'
import { getText } from '@/utils/arrayHelper.cs'

export default {
    name: 'RestrictedZonesEventList',
    setup() {
        const vm = getCurrentInstance().proxy
        // computed
        const currentLocale = computed(() => vm.$i18n.locale)
        const baseURL = computed(() => process.env.VUE_APP_BASE_URL)
        // ===== data =====
        const storageKey = 'restrictedZonesEventCache'
        const searchCache = getStorage(storageKey)

        const searchFormBlank = {
            filterDateFrom: vm
                .$moment()
                .startOf('day')
                .format('YYYY-MM-DD HH:mm:ss'),
            filterDateTo: vm
                .$moment()
                .endOf('day')
                .format('YYYY-MM-DD HH:mm:ss'),
            filterAreaId: '',
            filterDeviceId: '',
            filterNameCode: '',
            isRegistered: null,
            is206: true,
        }
        const multiSelectBlank = {
            dateTime: [
                vm.$moment().startOf('day').format('YYYY-MM-DD HH:mm:ss'),
                vm.$moment().endOf('day').format('YYYY-MM-DD HH:mm:ss'),
            ],
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

        const options = reactive({
            devices: [],
            filterDeviceByArea: [],
            areas: [],
            areasTree: [],
            registerStatus: registerStatus.map((item) => ({
                id: item.id,
                text: vm.$t(`constants.${item.i18nKey}`),
            })),
        })
        const formConfig = reactive({
            colMd: 4,
            labelColMd: 4,
            labelAlign: 'left',
        })
        const table = reactive({
            dataUrl: '/faceEvents',
            columns: [
                {
                    label: 'events.restrictedZonesEvent.table.date',
                    field: 'accessDate',
                },
                {
                    label: 'events.restrictedZonesEvent.table.time',
                    field: 'accessTime',
                },
                {
                    label: 'events.restrictedZonesEvent.table.area',
                    field: 'areaName',
                },
                {
                    label: 'events.restrictedZonesEvent.table.device',
                    field: 'deviceName',
                },
                {
                    label: 'events.restrictedZonesEvent.table.isRegistered',
                    field: 'isRegistered',
                },
                {
                    label: 'events.restrictedZonesEvent.table.userName',
                    field: 'userName',
                },
                {
                    label: 'events.restrictedZonesEvent.table.userCode',
                    field: 'userCode',
                },
                {
                    label: 'events.restrictedZonesEvent.table.image',
                    field: 'image',
                },
                {
                    label: 'events.restrictedZonesEvent.table.action',
                    field: 'action',
                },
            ],
        })

        const restrictedZonesEventTable = ref(null)
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
            restrictedZonesEventTable.value?.refresh?.()
        }

        function isStranger(personId) {
            return personId == '00000000-0000-0000-0000-000000000000'
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
        onMounted(async () => {
            const [rDevices, rAreas, rAreasTree] = await Promise.allSettled([
                lookupService.getDevices(),
                lookupService.getAreas(),
                lookupService.getAreasTree(),
            ])

            if (rDevices.status === 'fulfilled') {
                options.devices = rDevices.value
                options.filterDeviceByArea = rDevices.value
            } else {
                console.error('getDevices failed:', rDevices.reason)
                options.devices = []
            }

            if (rAreas.status === 'fulfilled') {
                options.areas = rAreas.value
            } else {
                console.error('getAreas failed:', rAreas.reason)
                options.areas = []
            }

            if (rAreasTree.status === 'fulfilled') {
                options.areasTree = rAreasTree.value
            } else {
                console.error('getAreasTree failed:', rAreasTree.reason)
                options.areasTree = []
            }
        })

        // ======= watch =======
        watch(
            () => multiSelect.dateTime,
            (range) => {
                const [from, to] = range || []
                searchForm.filterDateFrom = from || ''
                searchForm.filterDateTo = to || ''
            },
            { deep: true, immediate: true }
        )

        watch(
            () => multiSelect.areas,
            (array) => {
                searchForm.filterAreaId =
                    multiSelect.areas.length > 0
                        ? multiSelect.areas.join(',')
                        : ''
                if (array.length == 0)
                    options.filterDeviceByArea = options.devices
                else
                    options.filterDeviceByArea = options.devices.filter(
                        (device) => {
                            return multiSelect.areas.includes(device.areaId)
                        }
                    )

                multiSelect.devices = multiSelect.devices.filter((device) => {
                    return options.filterDeviceByArea.includes(device)
                })
            },
            { deep: true }
        )
        watch(
            () => multiSelect.devices,
            () => {
                searchForm.filterDeviceId =
                    multiSelect.devices.length > 0
                        ? multiSelect.devices.join(',')
                        : ''
            },
            { deep: true }
        )
        const headerExcelDetail = reactive([])
        const export_fields = reactive([])
        async function exportData() {
            let allAreaName = vm.$t('Export.All')

            if (vm.multiSelect.areas && vm.multiSelect.areas.length > 0) {
                const areaName = vm.multiSelect.areas.map((area) => {
                    getText(area.Id, options.areas)
                })
                allAreaName = areaName.join(',')
            }

            var allDeviceName = vm.$t('Export.All')
            if (vm.multiSelect.devices && vm.multiSelect.devices.length > 0) {
                const deviceName = vm.multiSelect.devices.map((device) => {
                    getText(device.Id, options.devices)
                })
                alldeviceName = deviceName.join(',')
            }

            vm.headerExcelDetail = [
                `${vm.$t('events.restrictedZonesEvent.excel.header')}`,
                `${vm.$t('events.restrictedZonesEvent.excel.dateTimeFrom')}: ${vm.$moment(searchForm.filterDateFrom).format('DD/MM/YYYY HH:mm')}    ${vm.$t('events.restrictedZonesEvent.excel.dateTimeTo')}: ${vm.$moment(searchForm.filterDateTo).format('DD/MM/YYYY HH:mm')}`,
                `${vm.$t('events.restrictedZonesEvent.excel.device')}: ${allDeviceName}`,
                `${vm.$t('events.restrictedZonesEvent.excel.area')}: ${allAreaName}`,
            ]
            vm.export_fields = {
                [vm.$t('events.restrictedZonesEvent.table.date')]: 'accessDate',
                [vm.$t('events.restrictedZonesEvent.table.time')]: 'accessTime',
                [vm.$t('events.restrictedZonesEvent.table.area')]: 'areaName',
                [vm.$t('events.restrictedZonesEvent.table.device')]:
                    'deviceName',
                [vm.$t('events.restrictedZonesEvent.table.isRegistered')]:
                    'isRegistered',
                [vm.$t('events.restrictedZonesEvent.table.userName')]:
                    'userName',
                [vm.$t('events.restrictedZonesEvent.table.userCode')]:
                    'userCode',
            }

            const pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'accessTime',
                sortDesc: true,
            }

            const formData = `${new URLSearchParams(pagination).toString()}&${new URLSearchParams(vm.searchForm).toString()}`
            const response = await vm.$services.get(
                `${vm.$refs.restrictedZonesEventTable.dataUrl}?${formData}`
            )

            for (const item of response.data.data.data) {
                item.accessDate = vm
                    .$moment(item.accessDate)
                    .format('DD/MM/YYYY')
                item.accessTime = vm.$moment(item.accessTime).format('HH:mm:ss')
                item.isRegistered = vm.isStranger(item.personId)
                    ? vm.$t('constants.isRegistered.unregisted')
                    : vm.$t('constants.isRegistered.registered')
            }

            return response.data.data.data
        }
        async function showModalEvent(eventId) {
            vm.loadEventFiles(eventId)
            vm.modalImgEventShow = true
        }
        async function loadEventFiles(eventId) {
            const response = await vm.$services.get(
                `/event/eventFilesById/${eventId}`
            )
            vm.listEventFiles = response.data
        }
        // =========================
        const modalImgEventShow = ref(false)
        const listEventFiles = ref([])

        return {
            page,
            itemsPerPage,
            initialPage,
            initialItemsPerPage,
            isRTL,
            formConfig,
            table,
            options,
            search,
            refresh,
            getPagination,
            restrictedZonesEventTable,
            isStranger,
            searchForm,
            multiSelect,
            currentLocale,
            baseURL,
            modalImgEventShow,
            listEventFiles,
            export_fields,
            showModalEvent,
            loadEventFiles,
            exportData,
            headerExcelDetail,
        }
    },
}
</script>

<style lang="scss">
@import '@/assets/scss/_custom-tree-select.scss';
</style>
