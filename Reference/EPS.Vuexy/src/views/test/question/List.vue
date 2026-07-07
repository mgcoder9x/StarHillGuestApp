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
                    v-if="authorize(['ManageSTDQuestion'])"
                    variant="primary"
                    :to="{ path: '/test/question/Create' }"
                    style="margin-bottom: 15px"
                >
                    {{ this.$t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable
                ref="classesTable"
                :columns="columns"
                data-url="/stdQuestion"
                :search-form="searchForm"
                storageName="classesTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Action -->
                    <span v-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-if="authorize(['ViewSTDQuestion'])"
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: `/test/question/Detail/${props.row.id}`,
                                }"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['ManageSTDQuestion'])"
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
                    <span v-else-if="props.column.field === 'knockoutQuestion'">
                        <div style="text-align: center;">
                            <b-form-checkbox v-model="props.row.knockoutQuestion" disabled>
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
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    mixins: [authorizationMixin],
    components: { ToastificationContent },
    data() {
        return {
            searchDatas: [
                {
                    filterName: 'content',
                    label: 'Nội dung',
                    placeholder:'Nội dung',
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
                    searchType: 'v-select',
                    filterName: 'questionType',
                    label: 'Loại câu hỏi',
                    placeholder:'Loại câu hỏi',
                    options: [
                        { id: 1, text: 'Thông dụng' },
                        { id: 2, text: 'Vận dụng' },
                        { id: 3, text: 'Hiểu biết' },
                    ],
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'type',
                    label: 'Số lượng đáp án đúng',
                    placeholder:'Số lượng đáp án đúng',
                    options: [
                        { id: 1, text: 'Một đáp án đúng' },
                        { id: 2, text: 'Nhiều đáp án đúng' },
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
                    label: this.$t('Nội dung'),
                    field: 'content',
                },
                {
                    label: this.$t('Khóa học'),
                    field: 'courseId',
                    formatFn: (value) => {
                        return this.searchDatas[1].options.find(x => x.id == value).text
                    },
                },
                {
                    label: this.$t('Loại câu hỏi'),
                    field: 'questionType',
                    formatFn: (value) => {
                        switch (value) {
                            case 1:
                                return 'Thông dụng'
                            case 2:
                                return 'Vận dụng'
                            case 3:
                                return 'Hiểu biết'
                            default:
                                return ''
                        }
                    },
                },
                {
                    label: this.$t('Số lượng đáp án đúng'),
                    field: 'type',
                    formatFn: (value) => {
                        switch (value) {
                            case 1:
                                return 'Một đáp án đúng'
                            case 2:
                                return 'Nhiều đáp án đúng'
                            default:
                                return ''
                        }
                    },
                },
                {
                    label: this.$t('Cấp độ'),
                    field: 'level',
                },
                {
                    label: this.$t('Câu hỏi điểm liệt'),
                    field: 'knockoutQuestion',
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
                        .delete('/stdQuestion/' + item)
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
        loadCourse() {
            this.$services.get('/lookup/courses').then((response) => {
                this.searchDatas[1].options = response.data.data
            })
        },
    },
}
</script>

<style lang="scss"></style>
