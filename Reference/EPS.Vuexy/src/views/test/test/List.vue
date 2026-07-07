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
                    v-if="authorize(['ManageSTDTest'])"
                    variant="primary"
                    :to="{ path: '/test/test/Create' }"
                    style="margin-bottom: 15px"
                >
                    {{ this.$t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="classesTable"
                :columns="columns"
                data-url="/stdTest"
                :search-form="searchForm"
                storageName="classesTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Action -->
                    <span v-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize(['ViewSTDTest'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/test/test/Detail/${props.row.id}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageSTDTest'])"
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
                    <span v-else-if="props.column.field === 'isMock'">
                        <div style="text-align: center;">
                            <b-form-checkbox v-model="props.row.isMock" disabled>
                            </b-form-checkbox>
                        </div>
                    </span>
                    <span v-else-if="props.column.field === 'isFixed'">
                        <div style="text-align: center;">
                            <b-form-checkbox v-model="props.row.isFixed" disabled>
                            </b-form-checkbox>
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
                    filterName: 'titleDescription',
                    label: 'Tiêu đề/ mô tả',
                    placeholder:'Mã/tên lớp học',
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'classesId',
                    label: 'Lớp học',
                    placeholder:'Lớp học',
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
                    filterName: 'configId',
                    label: 'Ma trận',
                    placeholder:'Ma trận',
                    options: [],
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
                    label: this.$t('Tiêu đề'),
                    field: 'title',
                },
                {
                    label: this.$t('Mô tả'),
                    field: 'description',
                },
                {
                    label: this.$t('Lớp học'),
                    field: 'className',
                },
                {
                    label: this.$t('Ma trận'),
                    field: 'configName',
                },
                {
                    label: this.$t('Ôn tập'),
                    field: 'isMock',
                },
                {
                    label: this.$t('Đề cố định'),
                    field: 'isFixed',
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
                    label: this.$t('Thời gian làm bài'),
                    field: 'duration',
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
        this.loadClasses()
        this.loadConfig()
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
                        .delete('/stdTest/' + item)
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
        loadClasses() {
            this.$services.get('/lookup/classes').then((response) => {
                this.searchDatas[1].options = response.data.data
            })
        },
        loadConfig() {
            this.$services.get('/lookup/config').then((response) => {
                this.searchDatas[4].options = response.data.data
            })
        },
    },
}
</script>

<style lang="scss"></style>
