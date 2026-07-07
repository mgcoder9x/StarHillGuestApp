<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form>
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
                            :model-value="searchData.modelValue"
                            @handle-binding="handleBinding"
                        />
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>

        <b-card title="">
            <div>
                <b-button
                    v-if="authorize(['ManageSTDClasses'])"
                    variant="primary"
                    :to="{ path: '/study/classes/Create' }"
                    style="margin-bottom: 15px"
                >
                    {{ this.$t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="classesTable"
                :columns="columns"
                data-url="/stdClasses"
                :search-form="searchForm"
                storageName="classesTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Action -->
                    <span v-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize(['ViewSTDClasses'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/study/classes/Detail/${props.row.id}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageSTDClasses'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
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
                </template>
            </BasicTable>
        </b-card>
    </div>
</template>

<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import TreeHelper from '@/utils/treeHelper'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    mixins: [authorizationMixin],
    components: { ToastificationContent },
    data() {
        return {
            searchDatas: [
               {
                    filterName: 'codeName',
                    label: 'Mã/tên lớp học',
                    placeholder:'Mã/tên lớp học',
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'courseId',
                    label: 'Khóa học',
                    placeholder:'Khóa học',
                    options: [],
                    autoSearch: true,
                },
                {
                    searchType: 'b-form-datepicker',
                    filterName: 'startDate',
                    label: 'categories.groupAccesses.list.searchForm.label.effectiveFrom',
                    placeholder: 'common.form.placeholder.selectValue',
                    autoSearch: true,
                    dateFormatOptions: {
                        day: 'numeric',
                        month: 'long',
                        year: 'numeric',
                    },
                },
                {
                    searchType: 'b-form-datepicker',
                    filterName: 'endDate',
                    label: 'categories.groupAccesses.list.searchForm.label.effectiveTo',
                    placeholder: 'common.form.placeholder.selectValue',
                    autoSearch: true,
                    dateFormatOptions: {
                        day: 'numeric',
                        month: 'long',
                        year: 'numeric',
                    },
                },
                {
                    searchType: 'v-select',
                    filterName: 'status',
                    label: 'categories.employees.list.searchForm.label.status',
                    placeholder:
                        'categories.employees.list.searchForm.placeholder.status',
                    options: [
                        { id: 1, text: 'Hoạt động' },
                        { id: 2, text: 'Ngừng hoạt động' },
                    ],
                    autoSearch: true,
                },
            ],
            searchForm: {
                codeName: '',
                compId: '',
                courseId: null,
                startDate: null,
                endDate: null,
                status: null
            },
            // define options
            options: [],
            columns: [
                {
                    label: this.$t('Mã lớp'),
                    field: 'code',
                },
                {
                    label: this.$t('Tên lớp'),
                    field: 'name',
                },
                {
                    label: this.$t('Khóa học'),
                    field: 'courseName',
                },
                {
                    label: this.$t('Ngày bắt đầu hiệu lực'),
                    field: 'startDateStr',
                },
                {
                    label: this.$t('Ngày hết hiệu lức'),
                    field: 'endDateStr',
                },
                {
                    label: this.$t('Trạng thái'),
                    field: 'statusStr',
                },
                {
                    label: this.$t('Device.List.Table.Operation'),
                    field: 'action',
                },
            ],
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        this.loadCourse()
    },
    methods: {
        handleBinding(event) {
            this.searchForm[event.filterName] = event.value
            if (event.autoSearch) this.search()
        },
        search() {
            this.$refs.classesTable.refresh()
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
                        .delete('/stdClasses/' + item)
                        .then((response) => {
                            this.$refs.classesTable.refresh()
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
        showSuccessToast() {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: 'Success',
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        
        //lookup data
        loadCourse() {
            this.$services.get('/lookup/courses').then((response) => {
                this.searchDatas[1].options = response.data.data
            })
        },
    },
}
</script>

<style lang="scss"></style>
