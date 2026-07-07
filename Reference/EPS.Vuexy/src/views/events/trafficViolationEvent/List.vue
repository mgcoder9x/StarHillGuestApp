<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <!-- advance search input -->
                    <b-form @submit.prevent="search">
                        <!-- advance search input -->
                        <b-row>
                            <FormSearch
                                v-for="searchData in searchDatas"
                                :key="searchData.filterName"
                                :search-type="searchData.searchType"
                                :filter-name="searchData.filterName"
                                :label="searchData.label"
                                :placeholder="searchData.placeholder"
                                :md="searchData.md"
                                :label-cols-md="searchData.labelColsMd"
                                :options="searchData.options"
                                :auto-search="searchData.autoSearch"
                                :multiple="searchData.multiple"
                                :locale="currentLocale"
                                :date-format-options="
                                    searchData.dateFormatOptions
                                "
                                :value-consists-of="searchData.valueConsistsOf"
                                :model-value="searchData.modelValue"
                                @handle-binding="handleBinding"
                            />
                        </b-row>
                    </b-form>
                    <b-button
                        type="submit"
                        style="
                            position: absolute;
                            width: 1px;
                            height: 1px;
                            overflow: hidden;
                            clip: rect(0, 0, 0, 0);
                        "
                        >{{ this.$t('common.button.search') }}</b-button
                    >
                </b-form>
            </b-card-body>
        </b-card>

        <b-card title="">
            <div class="text-left mb-1 d-flex" style="gap: 8px">
                <b-button
                    variant="primary"
                    class="btn-hover-linear-primary border-0"
                >
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="$t('Events.Header.ExcelTrafficViolation')"
                        :fetch="exportData"
                        type="xlsx"
                        :fields="export_fields_vi"
                    >
                        <Icon icon="famicons:enter-outline" class="sm-icon" />
                        <span class="ml-25">{{
                            $t('Button.ExportExcel')
                        }}</span>
                    </downloadExcel>
                </b-button>

                <b-button
                    class="btn-hover-linear-primary border-0"
                    variant="primary"
                    @click="refresh()"
                >
                    <Icon icon="famicons:enter-outline" class="sm-icon" />
                    <span class="ml-25">
                        {{ $t('Button.Refresh') }}
                    </span>
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="vuegoodTable"
                :columns="table.columns"
                :data-url="table.dataUrl"
                :search-form="searchForm"
                :sort-by="'accessTime'"
            >
                <template v-slot:table-row="{ column, row }">
                    <span
                        v-if="column.field === 'image'"
                        style="
                            display: flex;
                            justify-content: center;
                            align-items: center;
                        "
                    >
                        <img
                            :src="`${imgUrl}${row.image}`"
                            alt="Image"
                            width="100"
                            style="object-fit: cover; border-radius: 0.3em"
                            class="cursor-pointer"
                            loading="lazy"
                            @click="openModal(`${imgUrl}${row.image}`)"
                        />
                    </span>
                    <span v-else-if="column.field === 'vehicleType'">
                        {{ getVehicleType(row.vehicleType) }}
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="column.field === 'action'">
                        <div class="center-icon text-nowrap">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary border-0"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/event/trafficViolationEvent/detail/${row.eventId}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                        </div>
                    </span>
                </template>
            </BasicTable>
            <!-- Modal -->
            <b-modal
                v-model="isModalOpen"
                :title="$t('Events.SearchForm.EventPhotos')"
                hide-footer
                size="lg"
            >
                <b-carousel id="carousel-example-generic" indicators controls>
                    <div class="d-flex justify-content-center">
                        <img
                            :src="modalImageUrl"
                            loading="lazy"
                            alt="Full Image"
                            style="
                                height: 60vh;
                                object-fit: contain;
                                object-position: center;
                            "
                        />
                    </div>
                </b-carousel>
            </b-modal>
        </b-card>
    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import moment from 'moment'
import TreeHelper from '@/utils/treeHelper'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'

export default {
    mixins: [authorizationMixin],
    data() {
        return {
            searchDatas: [
                {
                    searchType: 'date-picker',
                    filterName: 'filterDateFrom',
                    label: 'Events.SearchForm.FromDate',
                    // dateFormatOptions: 'DD-MM-YYYY HH:mm:ss',
                    locale: this.$i18n.locale,
                    style: 'width: 100%',
                    autoSearch: true,
                    md: 4,
                    modelValue: null,
                },
                {
                    searchType: 'date-picker',
                    filterName: 'filterDateTo',
                    label: 'Events.SearchForm.ToDate',
                    // dateFormatOptions: 'DD-MM-YYYY HH:mm:ss',
                    locale: this.$i18n.locale,
                    style: 'width: 100%',
                    autoSearch: true,
                    md: 4,
                    modelValue: null,
                },
                {
                    filterName: 'filterLicensePlate',
                    label: 'TrafficViolationEvent.Detail.LicensePlate',
                    md: 4,
                    autoSearch: true,
                    modelValue: null,
                },
                {
                    filterName: 'filterWarningLevel',
                    label: 'TrafficViolationEvent.Detail.WarningLevel',
                    md: 4,
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterVehicleType',
                    label: 'TrafficViolationEvent.Detail.VehicleType',
                    options: [
                        {
                            id: 1,
                            text: this.$t(
                                'common.options.vehicleType.motorbike'
                            ),
                        },
                        {
                            id: 2,
                            text: this.$t('common.options.vehicleType.car'),
                        },
                    ],
                    md: 4,
                    autoSearch: true,
                },
                {
                    searchType: 'tree-select',
                    filterName: 'filterAreaId',
                    label: 'TrafficViolationEvent.Detail.AreaName',
                    multiple: true,
                    options: [],
                    autoSearch: true,
                    md: 4,
                },
                {
                    searchType: 'tree-select',
                    filterName: 'filterDeviceId',
                    label: 'TrafficViolationEvent.Detail.Device',
                    multiple: true,
                    options: [],
                    autoSearch: true,
                    md: 4,
                },
            ],
            searchForm: {
                filterDateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                filterDateTo: null,
                filterLicensePlate: null,
                filterWarningLevel: null,
                filterVehicleType: null,
                filterAreaId: null,
                filterDeviceId: null,
            },
            form: {
                colMd: 4,
                labelColsMd: 4,
            },
            table: {
                dataUrl: '/trafficViolationEvents',
                columns: [
                    {
                        label: 'TrafficViolationEvent.Detail.Date',
                        field: 'accessTime',
                        formatFn(value) {
                            return moment.utc(value).format('DD/MM/YYYY')
                        },
                    },
                    {
                        label: 'TrafficViolationEvent.Detail.Time',
                        field: 'accessTime',
                        formatFn(value) {
                            return moment.utc(value).format('HH:mm:ss')
                        },
                    },
                    {
                        label: 'TrafficViolationEvent.Detail.LicensePlate',
                        field: 'licensePlate',
                    },
                    {
                        label: 'TrafficViolationEvent.Detail.WarningLevel',
                        field: 'warningLevelId',
                    },
                    {
                        label: 'TrafficViolationEvent.Detail.VehicleType',
                        field: 'vehicleType',
                    },
                    {
                        label: 'TrafficViolationEvent.Detail.AreaName',
                        field: 'areaName',
                    },
                    {
                        label: 'TrafficViolationEvent.Detail.Device',
                        field: 'deviceName',
                    },
                    {
                        label: 'TrafficViolationEvent.Detail.Image',
                        field: 'image',
                    },
                    {
                        label: 'TrafficViolationEvent.Detail.Operation',
                        field: 'action',
                    },
                ],
            },
            isModalOpen: false,
            modalImageUrl: null,
            headerExcelDetail: [],
            export_fields_vi: {
                'Thời gian': 'accessTime',
                Mã: 'userCode',
                'Người dùng': 'userName',
                'Phòng ban': 'depName',
                'Khu vực': 'areaName',
                'Thiết bị': 'deviceName',
                'Loại nhận diện': 'eventTypeId',
                'Trạng thái': 'status',
            },
            lstOptionIdentification: [
                {
                    id: 1,
                    text: this.$t(
                        'TrafficViolationEvent.Common.Identification.Yes'
                    ),
                },
                {
                    id: 2,
                    text: this.$t(
                        'TrafficViolationEvent.Common.Identification.No'
                    ),
                },
            ],
        }
    },
    computed: {
        imgUrl() {
            const { VUE_APP_BASE_URL: baseURL } = process.env
            return `${baseURL}`
        },
        currentLocale() {
            return this.$i18n.locale
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        this.lookupData()
        const searchForm = getStorage('trafficViolationEventSearchForm')
        if (searchForm) {
            this.searchForm = { ...searchForm }
        } else {
            this.searchForm.filterDateFrom = moment().format(
                'YYYY-MM-DD 00:00:00'
            )
        }
        this.refreshSearchFrom()
    },

    methods: {
        refreshSearchFrom() {
            /** Cập nhật giá trị component FormSearch theo searchForm */
            Object.keys(this.searchForm).forEach((key) => {
                const searchFrom = this.searchDatas.find(
                    (item) => item.filterName === key
                )
                searchFrom.modelValue = this.searchForm[key]
            })
        },
        refresh() {
            clearStorage('trafficViolationEventSearchForm')
            this.searchForm = {
                filterDateFrom: moment().format('YYYY-MM-DD 00:00:00'),
                filterDateTo: null,
                filterLicensePlate: null,
                filterWarningLevel: null,
                filterVehicleType: null,
                filterAreaId: null,
                filterDeviceId: null,
            }
            this.refreshSearchFrom()
            this.$nextTick(() => {
                this.$refs.vuegoodTable.refresh()
            })
        },
        getVehicleType(value) {
            const vehicleType = {
                1: 'common.options.vehicleType.motorbike',
                2: 'common.options.vehicleType.car',
            }
            return this.$t(vehicleType[value])
        },
        openModal(url) {
            this.modalImageUrl = url
            this.isModalOpen = true
        },
        handleBinding(event) {
            this.searchForm[event.filterName] = event.value
            if (event.autoSearch) this.search()
        },
        search() {
            setStorage('trafficViolationEventSearchForm', this.searchForm, 120)
            if (
                this.searchForm.filterDateFrom &&
                this.searchForm.filterDateTo
            ) {
                const fromDate = moment(this.searchForm.filterDateFrom)
                const toDate = moment(this.searchForm.filterDateTo)
                // Nếu từ ngày lớn hơn đến ngày, hiển thị thông báo lỗi
                if (fromDate.isAfter(toDate)) {
                    this.$bvToast.toast('Vui lòng nhập lại từ ngày!', {
                        title: `${this.$t('Error.Error')}`,
                        variant: 'danger',
                        solid: true,
                    })
                    return // Dừng lại nếu điều kiện không hợp lệ
                }
            }

            this.$refs.vuegoodTable.refresh()
        },
        // lookup data
        lookupData() {
            this.$services.get('/lookup/areas-tree').then((response) => {
                const areas = TreeHelper.removeEmptyChildren(response.data.data)
                const searchData = this.searchDatas.find(
                    (x) => x.filterName === 'filterAreaId'
                )
                searchData.options = areas.map((item) => {
                    const { text, ...rest } = item
                    return { label: text, ...rest }
                })
            })
            this.$services.get('/lookup/devices').then((response) => {
                // this.searchDatas[3].options = response.data.data
                const devices = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
                const searchData = this.searchDatas.find(
                    (x) => x.filterName === 'filterDeviceId'
                )
                searchData.options = devices
                    .filter((x) => x.eventTypeId === 200)
                    .map((item) => {
                        const { text, ...rest } = item
                        return { label: text, ...rest }
                    })
            })
        },
        async exportData() {
            // const vm = this
            // let allAreaName = vm.$t('Export.All')
            // if (
            //     vm.searchForm.filterAreaId != null &&
            //     vm.searchForm.filterAreaId != []
            // ) {
            //     vm.lstAllArea.forEach((area) => {
            //         if (
            //             vm.searchForm.filterAreaId
            //                 .map(String)
            //                 .includes(area.id.toString())
            //         ) {
            //             areaName.push(area.label)
            //         }
            //     })
            //     allAreaName = areaName.join(',')
            // }
            // let allDeviceName = vm.$t('Export.All')
            // const deviceName = []
            // if (
            //     vm.searchForm.filterDeviceId != null &&
            //     vm.searchForm.filterDeviceId !== []
            // ) {
            //     vm.searchDatas[1].options.forEach((devi) => {
            //         if (vm.searchForm.filterDeviceId.includes(devi.id)) {
            //             deviceName.push(devi.label)
            //         }
            //     })
            //     allDeviceName = deviceName.join(',')
            // }
            // var allStatusName = vm.$t('Export.All')
            // let statusName = []
            // if (
            //     vm.searchForm.filterStatus != null &&
            //     vm.searchForm.filterStatus != []
            // ) {
            //     vm.searchDatas[5].options.forEach((sta) => {
            //         if (vm.searchForm.filterStatus.includes(sta.id)) {
            //             statusName.push(sta.text)
            //         }
            //     })
            //     allStatusName = statusName.join(',')
            // }
            // var allIden = vm.$t('Export.All')
            // if (vm.searchForm.filterIdentifi != null) {
            //     vm.lstOptionIdentification.forEach((ide) => {
            //         if (ide.id == vm.searchForm.filterIdentifi) {
            //             allIden = ide.text
            //         }
            //     })
            // }
            // let nameCode
            // if (vm.searchForm.filterNameCode != null) {
            //     nameCode = vm.searchForm.filterNameCode
            // } else {
            //     nameCode = vm.$t('Export.All')
            // }
            // let dateFrom = moment(this.searchForm.filterDateFrom).format(
            //     'DD/MM/YYYY HH:mm'
            // )
            // let dateTo = moment(this.searchForm.filterDateTo).format(
            //     'DD/MM/YYYY HH:mm'
            // )
            // if (dateFrom === 'Invalid date') {
            //     dateFrom = moment('2020-01-01 00:00:00').format(
            //         'DD/MM/YYYY HH:mm'
            //     )
            // }
            // if (dateTo === 'Invalid date') {
            //     dateTo = moment().format('DD/MM/YYYY HH:mm')
            // }
            // vm.headerExcelDetail = [
            //     `${vm.$t('VehicleEvent.Common.Header.Excel')}`,
            //     `${vm.$t('WaterEvents.Label.FromDate')}: ${dateFrom}     ${vm.$t('WaterEvents.Label.ToDate')}: ${dateTo}`,
            //     `${vm.$t('WaterEvents.Label.Devices')}: ${allDeviceName}`,
            //     `${vm.$t('WaterEvents.Label.Areas')}: ${allAreaName}`,
            //     `${vm.$t('VehicleEvent.Field.NameOrCode')}: ${nameCode}`,
            //     `${vm.$t('VehicleEvent.Field.Status')}: ${allStatusName}`,
            //     `${vm.$t('VehicleEvent.Field.Identification')}: ${allIden}`,
            // ]
            // const pagination = {
            //     page: 1,
            //     itemsPerPage: 99999,
            //     sortBy: 'accessTime',
            //     sortDesc: true,
            // }
            // const formData = `${new URLSearchParams(pagination).toString()}&${new URLSearchParams(vm.searchForm).toString()}`
            // const response = await this.$services.get(
            //     `${vm.$refs.vuegoodTable.dataUrl}?${formData}`
            // )
            // for (let i = 0; i < response.data.data.data.length; i++) {
            //     response.data.data.data[i].accessTime = moment(
            //         response.data.data.data[i].accessTime
            //     ).format('DD/MM/YYYY HH:mm')
            // }
            // return response.data.data.data
        },
    },
}
</script>

<style lang="scss" scoped>
.avatar-crop {
    object-fit: cover;
    object-position: center;
    width: 8rem;
    height: 9rem;
    cursor: pointer;
}
</style>
