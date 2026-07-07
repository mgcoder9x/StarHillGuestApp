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
                            :model-value="searchData.modelValue"
                            @handle-binding="handleBinding"
                        />
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>

        <b-card title="">
            <!-- table -->
            <BasicTable
                ref="classesTable"
                :columns="columns"
                data-url="/stdUserTest"
                :search-form="searchForm"
                storageName="classesTable"
            >
                <template slot="table-row" slot-scope="props">
                    <!-- Column: Action -->
                    <span v-if="props.column.field === 'action'">
                        <div class="text-nowrap">
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                @click="showModal(props.row.id)"
                            >
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
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
        <b-modal ref="viewModal" size="lg" hide-footer title="Danh sách câu hỏi trong bài kiểm tra">
            <div v-if="previewQuestions.length > 0">
                <b-list-group>
                    <b-list-group-item
                        v-for="(question, index) in previewQuestions"
                        :key="index"
                        class="position-relative"
                        >
                        <div class="d-flex justify-content-between align-items-start">
                            <h5 class="mb-2">
                            {{ index + 1 }}. {{ question.Content }}
                            </h5>
                            <span :class="getStatusClass(question)" class="badge">
                            {{ getStatusText(question) }}
                            </span>
                        </div>

                        <ul class="pl-3">
                            <li
                            v-for="(answer, idx) in question.ListAnswer"
                            :key="idx"
                            :style="getAnswerStyle(answer.Id, question)"
                            >
                            {{ answer.Content }}
                            <span v-if="answer.Id === question.AnswerSuccess" class="ml-1">✔</span>
                            </li>
                        </ul>
                    </b-list-group-item>
                </b-list-group>
            </div>
            <div v-else class="text-center text-muted">
                Không có câu hỏi nào được chọn từ ma trận.
            </div>
        </b-modal>
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
                    filterName: 'userName',
                    label: 'Học viên',
                    placeholder:'Học viên',
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'testId',
                    label: 'Bài kiểm tra',
                    placeholder:'Bài kiểm tra',
                    options: [],
                    autoSearch: true,
                },
                {
                    searchType: 'b-form-datepicker',
                    filterName: 'startDate',
                    label: 'Ngày bắt đầu làm',
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
                    label: 'Đến',
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
                    filterName: 'passed',
                    label: 'Trạng thái',
                    placeholder:'Trạng thái',
                    options: [
                        { id: true, text: 'Đạt' },
                        { id: false, text: 'Chưa đạt' },
                    ],
                    autoSearch: true,
                },
            ],
            searchForm: {
                userName: '',
                testId: null,
                startDate: null,
                endDate: null,
                passed: null
            },
            // define options
            options: [],
            columns: [
                {
                    label: "Học viên",
                    field: 'userName',
                },
                {
                    label: "Bài kiểm tra",
                    field: 'testName',
                },
                {
                    label: "Thời gian bắt đầu làm",
                    field: 'startTimeStr',
                },
                {
                    label: "Đến",
                    field: 'endTimeStr',
                },
                {
                    label: "Số câu đúng",
                    field: 'answesSuccess',
                },
                {
                    label: "Tổng số câu",
                    field: 'answerTotal',
                },
                {
                    label: "Trạng thái",
                    field: 'passed',
                    formatFn: (value) => {
                        if(value){
                            return 'Đạt'
                        }
                        else{
                            return 'Chưa đạt'
                        }
                    },
                },
                {
                    label: this.$t('Device.List.Table.Operation'),
                    field: 'action',
                },
            ],
            previewQuestions: [],
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        this.loadClasses()
    },
    methods: {
        getAnswerStyle(answerId, question) {
            const selected = question.SelectedAnswerId;
            const correct = question.AnswerSuccess;

            if (answerId === correct) {
            return { color: 'green', fontWeight: 'bold' };
            } else if (selected != null && answerId === selected && selected !== correct) {
            return { color: 'red', fontWeight: 'bold' };
            }
            return {};
        },
        getStatusText(question) {
            if (question.SelectedAnswerId == null) return "Chưa chọn";
            return question.SelectedAnswerId === question.AnswerSuccess ? "Đúng" : "Sai";
        },
        getStatusClass(question) {
            if (question.SelectedAnswerId == null) return "badge-warning";
            return question.SelectedAnswerId === question.AnswerSuccess
            ? "badge-success"
            : "badge-danger";
        },
        showModal(id) {
            this.generateQuesttion(id)
            this.$refs.viewModal.show()
        },
        async generateQuesttion(id) {
            this.previewQuestions = [];
            await this.$services
                .get(`/stdUserTest/${id}`)
                .then((res) => {
                    debugger
                    this.previewQuestions = JSON.parse(res.data.data.testJson)
                })
                .catch(() => {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: 'Không thể lấy danh sách câu hỏi',
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                        },
                    })
                })
        },
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
            this.$services.get('/lookup/test').then((response) => {
                this.searchDatas[1].options = response.data.data
            })
        },
    },
}
</script>

<style lang="scss"></style>
