<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <!-- advance search input -->
                    <b-row>
                        <form-search
                            v-for="searchData in searchDatas"
                            :key="searchData.filterName"
                            :search-type="searchData.searchType"
                            :filter-name="searchData.filterName"
                            :label="searchData.label"
                            :placeholder="searchData.placeholder"
                            :format="'DD-MM-YYYY HH:mm:ss'"
                            :md="searchData.md"
                            :label-cols-md="searchData.labelColsMd"
                            :options="searchData.options"
                            :auto-search="searchData.autoSearch"
                            :model-value="searchData.modelValue"
                            @handle-binding="handleBinding"
                        />
                    </b-row>
                    <b-button
                        type="submit"
                        style="
                            position: absolute;
                            width: 1px;
                            height: 1px;
                            overflow: hidden;
                            clip: rect(0, 0, 0, 0);
                        "
                        >{{ this.$t('common.button.search') }}
                    </b-button>
                </b-form>
            </b-card-body>
        </b-card>

        <b-card title="">
            <div class="text-left mb-1 d-flex" style="gap: 8px">
                <b-button
                    v-if="authorize([authorizeName.manage])"
                    variant="primary"
                    :to="{ path: routerLink.create }"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <!-- <Icon icon="famicons:enter-outline" class="sm-icon" /> -->
                    {{ this.$t('Button.Create') }}
                </b-button>
                <b-button
                    v-waves
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                >
                    <downloadExcel
                        :header="headerExcelDetail"
                        :name="$t('categories.guess.Header.Excel')"
                        :fetch="exportData"
                        type="xlsx"
                        :fields="export_fields_vi"
                    >
                        <div class="d-flex align-items-center">
                            <!-- <Icon
                                icon="famicons:enter-outline"
                                class="sm-icon"
                            /> -->
                            <span class="ml-25">{{
                                $t('Button.ExportExcel')
                            }}</span>
                        </div>
                    </downloadExcel>
                </b-button>
                <b-button
                    variant="primary"
                    class="mb-1 btn-hover-linear-primary border-0 d-flex align-items-center"
                    @click="onReport"
                >
                    <!-- <Icon
                                icon="famicons:enter-outline"
                                class="sm-icon"
                            /> -->
                    <span class="ml-25">{{ $t('IN') }}</span>
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="groupTable"
                :columns="table.columns"
                :data-url="table.dataUrl"
                :sort-by="'creationTime'"
                :search-form="searchForm"
                storage-name="guessTable"
            >
                <template v-slot:table-row="{ column, row }">
                    <!-- Column: Action -->
                    <span v-if="column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize([authorizeName.view])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: routerLink.detail + row.id,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <!-- <b-button
                                v-if="authorize([authorizeName.manage])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Delete')"
                                @click="doDelete(row.id)"
                            >
                                <Icon
                                    icon="fluent:delete-20-regular"
                                    class="xs-icon"
                                />
                            </b-button> -->
                            <!-- v-if="authorize([authorizeName.manage]) &&
                                    !row.endTime" -->
                            <b-button
                                v-if="authorize([authorizeName.manage]) && !row.endTime"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Checkout')"
                                @click="onCheckout(row.id)"
                            >
                                <Icon
                                    icon="akar-icons:arrow-forward-thick-fill"
                                    class="xs-icon"
                                />
                            </b-button>
                        </div>
                    </span>
                    <span v-else-if="column.field === 'avartarPath'">
                        <div class="text-nowrap">
                            <b-img
                                v-if="row.avartarPath"
                                :src="`${imgUrl}/${row.avartarPath}`"
                                height="100"
                                width="100"
                                class="avatar-crop mr-1"
                                @click="
                                    openModal(`${imgUrl}/${row.avartarPath}`)
                                "
                            ></b-img>
                        </div>
                    </span>
                    <span v-else-if="column.field === 'startTime'">
                        <div class="text-left">
                            {{ formatDateTime(row.startTime) }}
                        </div>
                    </span>
                    <span v-else-if="column.field === 'endTime'">
                        <div class="text-left">
                            {{ formatDateTime(row.endTime) }}
                        </div>
                    </span>
                </template>
            </BasicTable>
            <!-- Modal -->
            <b-modal
                v-model="isModalOpen"
                :title="$t('common.form.label.guestImage')"
                :ok-title="$t('common.button.close')"
                hide-header-close
                ok-only
                size="lg"
                centered
            >
                <div class="d-flex justify-content-center">
                    <img
                        :src="modalImageUrl"
                        alt="Full Image"
                        loading="lazy"
                        style="
                            height: 60vh;
                            object-fit: contain;
                            object-position: center;
                        "
                    />
                </div>
            </b-modal>
        </b-card>
        <b-modal
            v-model="showReportModal"
            size="xl"
            :title="$t('Employees.Report.Title')"
        >
            <telerik-report-viewer
                ref="reportViewer"
                :report-source="reportViewer.reportSource"
                :parameters="reportViewer.parameters"
                class="reportViewer"
            />
            <template #modal-footer>
                <b-button variant="primary" @click="showReportModal = false">
                    {{ $t('common.button.close') }}
                </b-button>
            </template>
        </b-modal>
    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import getBaseUrl from '@/utils/get-baseUrl'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import moment from 'moment'

export default {
    // components: { ToastificationContent },
    mixins: [authorizationMixin],
    data() {
        return {
            searchDatas: [
                {
                    filterName: 'filterNameCode',
                    label: 'categories.guess.list.searchForm.label.nameCode',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.nameCode',
                },
                {
                    searchType: 'tree-select',
                    filterName: 'filterDepId',
                    label: 'categories.employees.list.searchForm.label.contractor',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.contractor',
                    options: [],
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterGender',
                    label: 'categories.employees.list.searchForm.label.gender',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.gender',
                    options: [
                        {
                            id: 0,
                            text: 'categories.employees.gender.female',
                        },
                        {
                            id: 1,
                            text: 'categories.employees.gender.male',
                        },
                    ],
                    autoSearch: true,
                },
                {
                    filterName: 'filterCitizenId',
                    label: 'categories.employees.list.searchForm.label.citizenId',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.citizenId',
                    autoSearch: true,
                },
                {
                    searchType: 'date-picker',
                    filterName: 'startTime',
                    label: 'categories.employees.list.table.label.startTime',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.checkIn',
                    autoSearch: true,
                    format: 'DD-MM-YYYY HH:mm:ss',
                    modelValue: null,
                },
                {
                    searchType: 'date-picker',
                    filterName: 'endTime',
                    label: 'categories.employees.list.table.label.endTime',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.checkOut',
                    autoSearch: true,
                },
                // {
                //     searchType: 'v-select',
                //     filterName: 'filterStatus',
                //     modelValue: 1,
                //     label: 'categories.employees.list.searchForm.label.status',
                //     placeholder:
                //         'categories.employees.list.searchForm.placeholder.status',
                //     options: [
                //         { id: 1, text: 'Đang hoạt động' },
                //         { id: 2, text: 'Ngừng hoạt động' },
                //     ],
                //     autoSearch: true,
                // },
            ],
            searchForm: {
                filterCompId: null,
                filterNameCode: null,
                filterDepId: [],
                filterGender: null,
                filterCitizenId: null,
                // filterStatus: null,
                PersonTypeStr: '2,3',
            },
            headerExcelDetail: [],
            export_fields_vi: {
                [this.$t('categories.employees.list.table.label.compName')]:
                    'compName',
                [this.$t('categories.employees.list.table.label.fullname')]:
                    'fullname',
                [this.$t('categories.employees.list.table.label.citizenId')]:
                    'citizenId',
                [this.$t('categories.employees.list.table.label.cardNumber')]:
                    'cardId',
                [this.$t('categories.employees.list.table.label.contractor')]:
                    'depName',
                [this.$t('categories.employees.list.table.label.gender')]:
                    'genderFormatted',
                [this.$t('categories.employees.list.table.label.receiver')]:
                    'receiver',
                [this.$t('categories.employees.list.table.label.startTime')]:
                    'startTimeFormatted',
                [this.$t('categories.employees.list.table.label.endTime')]:
                    'endTimeFormatted',
            },
            showReportModal: false,
            reportViewer: {
                serviceUrl: `${process.env.VUE_APP_BASE_URL}/api/reports`,
                reportSource: 'ReportGuessElectronic.trdp',
                parameters: {
                    startDate: null,
                    endDate: null,
                    genderText: null,
                    compId: null,
                    depId: null,
                    nameCode: null,
                    gender: null,
                    citizenId: null,
                    lang: this.$i18n.locale,
                    citizenIdText: null,
                    nameCodeText: null,
                    depName: null,
                    imgBaseUrl: null,
                },
                scaleMode: 'FIT_PAGE_WIDTH',
                viewMode: 'INTERACTIVE',
                scale: 1.0,
                show: false,
            },
            table: {
                dataUrl: '/employees',
                columns: [
                    {
                        label: 'categories.employees.list.table.label.fullname',
                        field: 'fullname',
                    },
                    {
                        label: 'categories.employees.list.table.label.citizenId',
                        field: 'citizenId',
                    },
                    {
                        label: 'categories.employees.list.table.label.cardNumber',
                        field: 'cardId',
                    },
                    {
                        label: 'categories.employees.list.table.label.gender',
                        field: 'gender',
                        formatFn: (value) => {
                            switch (value) {
                                case 0:
                                    return this.$t(
                                        'categories.employees.gender.female'
                                    )
                                case 1:
                                    return this.$t(
                                        'categories.employees.gender.male'
                                    )
                                default:
                                    return ''
                            }
                        },
                    },
                    {
                        label: 'categories.employees.list.table.label.contractor',
                        field: 'depName',
                    },
                    {
                        label: 'categories.employees.list.table.label.compName',
                        field: 'compGuest',
                    },
                    // {
                    //    label:
                    //        'categories.employees.list.table.label.status'
                    //    ,
                    //    field: 'status',
                    //    formatFn: (value) => {
                    //        switch (value) {
                    //            case 1:
                    //                return 'Đang hoạt động'
                    //            case 2:
                    //                return 'Ngừng hoạt động'
                    //            default:
                    //                return ''
                    //        }
                    //    },
                    // },
                    {
                        label: 'categories.employees.list.table.label.receiver',
                        field: 'receiver',
                    },
                    {
                        label: 'categories.employees.list.table.label.startTime',
                        field: 'startTime',
                        type: 'date',
                        dateInputFormat: "yyyy-MM-dd'T'HH:mm:ss",
                        dateOutputFormat: 'dd/MM/yyyy HH:mm',
                        thClass: 'text-left',
                    },
                    {
                        label: 'categories.employees.list.table.label.endTime',
                        field: 'endTime',
                        type: 'date',
                        dateInputFormat: "yyyy-MM-dd'T'HH:mm:ss",
                        dateOutputFormat: 'dd/MM/yyyy HH:mm',
                        thClass: 'text-left',
                    },
                    {
                        label: 'categories.employees.list.table.label.image',
                        field: 'avartarPath',
                    },
                    {
                        label: 'categories.employees.list.table.label.action',
                        field: 'action',
                    },
                ],
            },
            authorizeName: {
                manage: 'ManageEmployee',
                view: 'ViewEmployee',
            },
            routerLink: {
                create: '/categories/guess/create/',
                detail: '/categories/guess/detail/',
                delete: '/employees/',
                checkout: '/employees/checkout/',
            },
            isModalOpen: false,
            modalImageUrl: null,
        }
    },
    computed: {
        imgUrl() {
            // const { VUE_APP_BASE_URL: baseURL } = process.env
            return `${getBaseUrl()}/employees`
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.filterCompId = accessToken.companyId
        this.lookupData()
        // Gán giá trị mặc định cho startTime = hôm nay
        this.$nextTick(() => {
            const startTimeItem = this.searchDatas.find(
                (item) => item.filterName === 'startTime'
            )
            if (startTimeItem) {
                startTimeItem.modelValue = moment().toDate()
                this.search()
            }
        })
    },
    methods: {
        formatDateTime(dateString) {
            if (!dateString) return ''
            return moment(dateString).format('DD/MM/YYYY HH:mm')
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
            this.$refs.groupTable.refresh()
        },
        doDelete(item) {
            // confirm text
            this.$swal({
                title: this.$t('Message.MessageDelete1'),
                text: this.$t('Message.MessageDelete2'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: this.$t('Message.Agree'),
                cancelButtonText: this.$t('Message.Exit'),
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-outline-danger ml-1',
                },
                buttonsStyling: false,
            }).then((result) => {
                if (result.value) {
                    this.$services
                        .delete(this.routerLink.delete + item)
                        .then(() => {
                            this.$refs.groupTable.refresh()
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Delete'),
                                    text: '',
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                        })
                }
            })
        },
        onCheckout(id) {
            // confirm text
            this.$swal({
                title: this.$t('Message.CheckoutMessage1'),
                text: this.$t('Message.CheckoutMessage2'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: this.$t('Message.Agree'),
                cancelButtonText: this.$t('Message.Exit'),
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-outline-danger ml-1',
                },
                buttonsStyling: false,
            }).then((result) => {
                if (result.value) {
                    this.$services
                        .patch(this.routerLink.checkout + id)
                        .then(() => {
                            this.$refs.groupTable.refresh()
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Checkout'),
                                    text: '',
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                        })
                }
            })
        },
        onReport() {
            this.reportViewer.parameters.imgBaseUrl = this.imgUrl
            // Set parameters from current searchForm
            this.reportViewer.parameters.compId = this.searchForm.filterCompId
            // this.reportViewer.parameters.startDate = moment().format('YYYY-MM-DD 00:00:00') // Default start date
            // this.reportViewer.parameters.endDate = moment().format('YYYY-MM-DD 23:59:59') // Default end date
            this.reportViewer.parameters.startDate = this.searchForm.startTime
                ? moment(this.searchForm.startTime).format('YYYY-MM-DD HH:mm:ss')
                : null
            this.reportViewer.parameters.endDate = this.searchForm.endTime
                ? moment(this.searchForm.endTime).format('YYYY-MM-DD HH:mm:ss')
                : null
            this.reportViewer.parameters.depId =
                this.searchForm.filterDepId || null
            // this.reportViewer.parameters.code = this.searchForm.filterNameCode || null
            this.reportViewer.parameters.gender =
                this.searchForm.filterGender ?? null

            this.reportViewer.parameters.citizenId =
                this.searchForm.filterCitizenId || null
            this.reportViewer.parameters.lang = this.$i18n.locale

            // Set text params for report
            let depNameValue = this.$t(
                'Report.vehicleViolationReport.SearchForm.All'
            ) // Default
            const { filterDepId } = this.searchForm // Single value (e.g., 39)

            if (filterDepId !== null && filterDepId !== undefined) {
                const filterDepIdNum = Number(filterDepId) // Convert sang number để so sánh an toàn
                const matchingDep = this.searchDatas[1].options.find((dep) => {
                    const depIdNum = Number(dep.id)
                    return depIdNum === filterDepIdNum
                })
                if (matchingDep) {
                    depNameValue = matchingDep.label || matchingDep.text || '' // Ưu tiên 'label' từ log của bạn
                } else {
                    console.log('No matching dep found')
                }
            } else {
                console.log('No filterDepId: using default')
            }
            this.reportViewer.parameters.depName = depNameValue

            const g = this.searchForm.filterGender
            const genderText =
                g === null || g === undefined
                    ? this.$t('Report.vehicleViolationReport.SearchForm.All')
                    : g === 0
                      ? 'Nữ'
                      : g === 1
                        ? 'Nam'
                        : ''
            this.reportViewer.parameters.genderText = genderText

            this.reportViewer.parameters.nameCodeText =
                this.searchForm.filterNameCode ||
                this.$t('Report.vehicleViolationReport.SearchForm.All')
            this.reportViewer.parameters.citizenIdText =
                this.searchForm.filterCitizenId ||
                this.$t('Report.vehicleViolationReport.SearchForm.All')

            this.showReportModal = true

            // Refresh report after modal opens
            this.$nextTick(() => {
                if (this.$refs.reportViewer) {
                    this.$refs.reportViewer.refresh()
                }
            })
        },
        async exportData() {
            const vm = this

            // Name/Code
            let allNameCode = vm.$t('Export.All')
            if (vm.searchForm.filterNameCode) {
                allNameCode = vm.searchForm.filterNameCode
            }

            // Department/Contractor
            let allDepName = vm.$t('Export.All')
            if (
                vm.searchForm.filterDepId &&
                vm.searchForm.filterDepId.length > 0
            ) {
                const names = []
                vm.searchDatas[1].options.forEach((dep) => {
                    if (vm.searchForm.filterDepId.includes(dep.id))
                        names.push(dep.text)
                })
                allDepName = names.join(',')
            }

            // Gender
            let allGenderName = vm.$t('Export.All')
            if (vm.searchForm.filterGender !== null && vm.searchForm.filterGender !== undefined) {
                const genderOption = vm.searchDatas[2].options.find(g => g.id === vm.searchForm.filterGender)
                if (genderOption) allGenderName = genderOption.text
            }

            // CitizenId
            let allCitizenId = vm.$t('Export.All')
            if (vm.searchForm.filterCitizenId) {
                allCitizenId = vm.searchForm.filterCitizenId
            }

            // Dates
            const start = vm.searchForm.startTime
                ? vm
                      .$moment(vm.searchForm.startTime)
                      .format('DD/MM/YYYY HH:mm:ss')
                : vm.$t('Export.All')

            const end = vm.searchForm.endTime
                ? vm
                      .$moment(vm.searchForm.endTime)
                      .format('DD/MM/YYYY HH:mm:ss')
                : vm.$t('Export.All')

            vm.headerExcelDetail = [
                vm.$t('Employees.Excel.Header'),
                `${vm.$t('categories.employees.list.searchForm.label.startTime')}: ${start}      ${vm.$t('categories.employees.list.searchForm.label.endTime')}: ${end}`,
                `${vm.$t('categories.guess.list.searchForm.label.nameCode')}: ${allNameCode}`,
                `${vm.$t('categories.employees.list.searchForm.label.contractor')}: ${allDepName}`,
                `${vm.$t('categories.employees.list.searchForm.label.gender')}: ${allGenderName}`,
                `${vm.$t('categories.employees.list.searchForm.label.citizenId')}: ${allCitizenId}`,
            ]

            vm.export_fields_vi = {
                [vm.$t('categories.employees.list.table.label.compName')]:
                    'compName',
                [vm.$t('categories.employees.list.table.label.fullname')]:
                    'fullname',
                [vm.$t('categories.employees.list.table.label.citizenId')]:
                    'citizenId',
                [vm.$t('categories.employees.list.table.label.cardNumber')]:
                    'cardId',
                [vm.$t('categories.employees.list.table.label.contractor')]:
                    'depName',
                [vm.$t('categories.employees.list.table.label.gender')]:
                    'genderFormatted',
                [vm.$t('categories.employees.list.table.label.receiver')]:
                    'receiver',
                [vm.$t('categories.employees.list.table.label.startTime')]:
                    'startTimeFormatted',
                [vm.$t('categories.employees.list.table.label.endTime')]:
                    'endTimeFormatted',
            }
            // big page fetch
            const pagination = {
                page: 1,
                itemsPerPage: 99999,
                sortBy: 'creationTime',
                sortDesc: true,
            }
            const formData = `${new URLSearchParams(
                pagination
            ).toString()}&${new URLSearchParams(vm.searchForm).toString()}`

            const response = await this.$services.get(
                `${vm.table.dataUrl}?${formData}`
            )
            const transformedData = response.data.data.data.map((row) => ({
                ...row,
                genderFormatted:
                    row.gender === 0 ? 'Nữ' : row.gender === 1 ? 'Nam' : '',
                startTimeFormatted: row.startTime
                    ? moment(row.startTime).format('DD/MM/YYYY HH:mm:ss')
                    : '',
                endTimeFormatted: row.endTime
                    ? moment(row.endTime).format('DD/MM/YYYY HH:mm:ss')
                    : '',
            }))
            return transformedData
            // return response.data.data.data
        },
        // lookup data
        lookupData() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.searchDatas[0].options = response.data
            })
            this.$services
                .get('/lookup/departments-tree?type=2')
                .then((response) => {
                    this.searchDatas[1].options = response.data.data
                })
        },
    },
}
</script>

<style lang="scss" scoped>
.avatar-crop {
    object-fit: cover;
    object-position: center;
    width: 4rem;
    height: 5rem;
    cursor: pointer;
}
</style>
