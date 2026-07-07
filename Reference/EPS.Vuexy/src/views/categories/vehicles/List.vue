<template>
    <b-container fluid>
        <b-card>
            <b-form @submit.prevent="search">
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
                        :date-format-options="searchData.dateFormatOptions"
                        :value-consists-of="searchData.valueConsistsOf"
                        :clearable="searchData.clearable"
                        @handle-binding="handleBinding"
                    />
                </b-row>
                <b-button
                    type="submit"
                    class="btn-hover-linear-primary"
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
        </b-card>
        <b-card>
            <div>
                <b-button
                    v-if="authorize(['ManageVehicle'])"
                    variant="primary"
                    :to="{ path: '/categories/vehicles/create' }"
                    class="mb-2 btn-hover-linear-primary"
                >
                    {{ this.$t('common.button.create') }}
                </b-button>
                <!-- <b-button
                    v-if="authorize(['ManageVehicle'])"
                    variant="primary"
                    :to="{ path: '/categories/vehicles/import' }"
                    style="margin-bottom: 15px; margin-left: 10px"
                >
                    {{ $t('Button.ImportExcel') }}
                </b-button> -->
            </div>
            <BasicTable
                ref="vehicleTable"
                :columns="columns"
                data-url="/vehicles"
                :search-form="searchForm"
                storage-name="vehiclesTable"
            >
                <template slot="table-row" slot-scope="props">
                    <span v-if="props.column.field === 'type'">
                        {{ getVehicleTypeName(props.row.type) }}
                    </span>
                    <span v-else-if="props.column.field === 'departmentType'">
                        {{ getDepartmentType(props.row.departmentType) }}
                    </span>
                    <span v-else-if="props.column.field === 'departmentId'">
                        {{ getDepartmentName(props.row.departmentId) }}
                    </span>
                    <span v-else-if="props.column.field === 'groupId'">
                        {{ getGroupName(props.row.groupId) }}
                    </span>
                    <span v-else-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize(['ViewVehicle'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-secondary"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/categories/vehicles/detail/${props.row.id}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageVehicle'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon btn-hover-linear-danger"
                                :title="$t('Button.Delete')"
                                @click="doDelete(props.row.id)"
                            >
                                <Icon
                                    icon="fluent:delete-20-regular"
                                    class="xs-icon"
                                />
                            </b-button>
                        </div>
                    </span>
                    <span v-else>{{ props.row[props.column.field] }}</span>
                </template>
            </BasicTable>
        </b-card>
    </b-container>
</template>

<script>
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import TreeHelper from '@/utils/treeHelper'

export default {
    mixins: [authorizationMixin],
    data() {
        return {
            searchDatas: [
                {
                    searchType: 'b-form-input',
                    filterName: 'filterLicensePlate',
                    label: 'categories.vehicles.common.licensePlate',
                    placeholder: 'common.input.placeholder',
                    autoSearch: true,
                    multiple: false,
                },
                {
                    searchType: 'b-form-input',
                    filterName: 'filterEmployeeName',
                    label: 'categories.vehicles.common.employeeName',
                    placeholder: 'common.input.placeholder',
                    autoSearch: true,
                    multiple: false,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterDepartmentTypes',
                    label: 'categories.vehicles.common.departmentType',
                    placeholder: 'common.select.placeholder',
                    options: [],
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterTypeIds',
                    label: 'categories.vehicles.common.type',
                    placeholder: 'common.select.placeholder',
                    options: [],
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterDepartmentIds',
                    label: 'categories.vehicles.common.department/contractor',
                    placeholder: 'common.select.placeholder',
                    options: [],
                    autoSearch: true,
                    // valueConsistsOf: 'ALL',
                    multiple: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterGroupIds',
                    label: 'categories.vehicles.common.group',
                    placeholder: 'common.select.placeholder',
                    options: [],
                    autoSearch: true,
                    multiple: true,
                },
            ],
            searchForm: {
                filterLicensePlate: '',
                filterGroupIds: '',
                filterTypeIds: null,
                filterDepartmentIds: null,
            },
            options: {
                departmentType: [
                    {
                        text: this.$t(
                            'common.options.departmentVehicleType.departmentVehicle'
                        ),
                        id: 1,
                    },
                    {
                        text: this.$t(
                            'common.options.departmentVehicleType.contractorVehicle'
                        ),
                        id: 2,
                    },
                ],
                vehicleType: [
                    {
                        id: 1,
                        text: this.$t('common.options.vehicleType.motorbike'),
                    },
                    {
                        id: 2,
                        text: this.$t('common.options.vehicleType.car'),
                    },
                ],
                departmentTree: [],
                departments: [],
                contractorTree: [],
                groups: [],
            },
            columns: [
                {
                    label: 'categories.vehicles.common.licensePlate',
                    field: 'licensePlate',
                },
                {
                    label: 'categories.vehicles.common.type',
                    field: 'type',
                },
                {
                    label: 'categories.vehicles.common.employeeName',
                    field: 'employeeName',
                },
                {
                    label: 'categories.vehicles.common.departmentType',
                    field: 'departmentType',
                },
                {
                    label: 'categories.vehicles.common.department/contractor',
                    field: 'departmentId',
                },
                {
                    label: 'categories.vehicles.common.group',
                    field: 'groupId',
                },

                {
                    label: 'Device.List.Table.Operation',
                    field: 'action',
                },
            ],
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    async created() {
        await this.loadOptions()
        this.searchDatas[2].options = this.options.departmentType
        this.searchDatas[3].options = this.options.vehicleType
        this.searchDatas[4].options = this.options.departments
        this.searchDatas[5].options = this.options.groups
    },
    methods: {
        handleBinding(event) {
            debugger
            if (event.filterName === 'filterDepartmentTypes') {
                this.searchForm.filterDepartmentIds = null
                if (event.value) {
                    this.searchDatas[4].options =
                        this.options.departments.filter(
                            (x) => x.type === event.value
                        )
                } else {
                    this.searchDatas[4].options = this.options.departments
                }
            }
            this.searchForm[event.filterName] = event.value
            if (event.autoSearch) this.search()
        },
        search() {
            this.$refs.vehicleTable.refresh()
        },
        doDelete(id) {
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
                        .delete(`/vehicles/${id}`)
                        .then((response) => {
                            console.log(response)
                            this.$refs.vehicleTable.refresh()
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
        async loadOptions() {
            // this.loadDepartmentTree()
            await this.loadDepartments()
            // this.loadContractorTree()
            await this.loadGroup()
        },
        async loadDepartmentTree() {
            try {
                const response = await this.$services.get(
                    '/lookup/departments-tree'
                )
                this.options.departmentTree = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.LU_DEPARTMENTS_TREE_500`)}`,
                    },
                })
            }
        },
        async loadContractorTree() {
            try {
                const response = await this.$services.get(
                    '/lookup/departments-tree?type=2'
                )
                this.options.contractorTree = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.LU_CONTRACTORS_TREE_500`)}`,
                    },
                })
            }
        },
        async loadDepartments() {
            try {
                const response = await this.$services.get('/lookup/departments')
                this.options.departments = response.data.data
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.LU_DEPARTMENTS_500`)}`,
                    },
                })
            }
        },
        async loadGroup() {
            try {
                const response = await this.$services.get('/lookup/groups')
                this.options.groups = response.data.data
            } catch (error) {
                console.log('error', error)
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.LU_GROUPS_500`)}`,
                    },
                })
            }
        },
        getVehicleTypeName(id) {
            const vehicleType = this.options.vehicleType.find(
                (item) => item.id.toString() === id.toString()
            )
            return vehicleType ? vehicleType.text : ''
        },
        getDepartmentType(id) {
            const departmentType = this.options.departmentType.find(
                (item) => item.id.toString() === id.toString()
            )
            return departmentType ? departmentType.text : ''
        },
        getDepartmentName(id) {
            const department = this.options.departments.find(
                (item) => item.value.toString() === id.toString()
            )
            return department ? department.text : ''
        },
        getGroupName(id) {
            debugger
            if(id == null)
            return
            const group = this.options.groups.find(
                (item) => item.id.toString() === id.toString()
            )
            return group ? group.text : ''
        },
    },
}
</script>

<style lang="scss"></style>
