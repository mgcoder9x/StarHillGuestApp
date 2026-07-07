<template>
    <div>
        <!-- Filters -->
        <b-card no-body>
            <b-card-body>
                <b-form @submit.prevent="search">
                    <!-- advance search input -->
                    <b-row>
                        <FormSearch v-for="searchData in searchDatas" :key="searchData.filterName"
                            :search-type="searchData.searchType" :filter-name="searchData.filterName"
                            :label="searchData.label" :placeholder="searchData.placeholder" :md="searchData.md"
                            :multiple="searchData.multiple" :label-cols-md="searchData.labelColsMd"
                            :options="searchData.options" :auto-search="searchData.autoSearch"
                            :model-value="searchData.modelValue" @handle-binding="handleBinding" />
                    </b-row>
                    <b-button type="submit" style="
                            position: absolute;
                            width: 1px;
                            height: 1px;
                            overflow: hidden;
                            clip: rect(0, 0, 0, 0);
                        ">{{ this.$t('common.button.search') }}</b-button>
                </b-form>
            </b-card-body>
        </b-card>

        <b-card title="">
            <div>
                <b-button
                    v-if="authorize([authorizeName.manage])"
                    variant="primary"
                    @click="modalCreateIssue = true"
                    style="margin-bottom: 15px"
                >
                    {{ this.$t('Button.Create') }}
                </b-button>
            </div>
            <!-- table -->
            <BasicTable ref="issueTable" :columns="table.columns" :data-url="table.dataUrl" :search-form="searchForm"
                storageName="issuesTable">
                <template v-slot:table-row="{ column, row }">
                    <!-- format Column: DueAt, ClosedAt, CreatedAt, UpdatedAt -->
                    <span v-if="column.field === 'dueAt'">
                        {{ row.dueAt ? formatDateTime(row.dueAt) : '' }}
                    </span>
                    <span v-else-if="column.field === 'closedAt'">
                        {{ row.closedAt ? formatDateTime(row.closedAt) : '' }}
                    </span>
                    <span v-else-if="column.field === 'createdAt'">
                        {{ row.createdAt ? formatDateTime(row.createdAt) : '' }}
                    </span>
                    <span v-else-if="column.field === 'updatedAt'">
                        {{ row.updatedAt ? formatDateTime(row.updatedAt) : '' }}
                    </span>
                    <span v-else-if="column.field == 'projectId'">
                        {{ getStaticName(row.projectId, searchDatas[0].options) }}
                    </span>
                    <span v-else-if="column.field == 'issueTypeId'" class="center-label"
                        :style="{ backgroundColor: getStaticColor(row.issueTypeId, searchDatas[1].options)}">
                        {{ getStaticName(row.issueTypeId, searchDatas[1].options) }}
                    </span>
                    <span v-else-if="column.field == 'priorityId'"  class="center-label"
                        :style="{ backgroundColor: getStaticColor(row.priorityId, searchDatas[2].options)}">
                        {{ getStaticName(row.priorityId, searchDatas[2].options) }}
                    </span>
                    <span v-else-if="column.field == 'statusId'" class="center-label"
                        :style="{ backgroundColor: getStaticColor(row.statusId, searchDatas[3].options)}">
                        {{ getStaticName(row.statusId, searchDatas[3].options) }}
                    </span>
                    <span v-else-if="column.field == 'userId'">
                        {{ getStaticName(row.userId, listUser) }}
                    </span>
                    <!-- Column: Action -->
                    <span v-else-if="column.field === 'action'">
                        <div class="text-nowrap">
                            <!-- <b-button v-if="authorize([authorizeName.view])" v-b-tooltip.hover v-waves
                                variant="label-secondary" class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')" @click="showDetailIssue(row)"
                                :to="{
                                    path: routerLink.detail + row.id,
                                }">
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button> -->
                            <b-button v-if="authorize([authorizeName.view])" v-b-tooltip.hover v-waves
                                variant="label-secondary" class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                :title="$t('Button.Detail')"
                                :to="{
                                    path: routerLink.detail + row.id,
                                }">
                                <Icon icon="mdi:eye-outline" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-b-tooltip.hover
                                v-waves
                                variant="label-secondary"
                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon"
                                @click="onReport(row)"
                            >
                                <Icon icon="carbon:report" class="xs-icon" />
                            </b-button>
                            <b-button
                                v-if="authorize(['RemoveIssue'])"
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
                            </b-button>
                        </div>
                    </span>
                </template>
            </BasicTable>
        </b-card>
        <b-modal
            v-model="modalCreateIssue"
                :title="$t('Issue.Header')"
                dialog-class="modal-70"
                hide-footer
            >
            <create-issue @success="handleCreateSuccess">
            </create-issue>
        </b-modal>
        <b-modal
            v-model="showReportModal"
            size="xl"
            :title="$t('Problem.Report.Title')"
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
        <b-sidebar id="sidebar-add-new-event" v-model="isSidebarActive" sidebar-class="sidebar-xxl"
            :visible="isSidebarActive" bg-variant="white" backdrop no-header right style="overflow-y: hidden !important;">

            <!-- Header cố định -->
            <div class="sidebar-header">
                <h3>{{ dataIssue.code }}</h3>
                <b-col style="margin-bottom: 15px;">
                    <div class="input-action">
                        <b-input type="text" v-model="dataIssue.title" placeholder="Nhập tiêu đề" />
                        <div class="actions">
                            <button class="btn-save" >✔</button>
                        </div>
                    </div>

                </b-col>
                <b-col>
                    <b-form-group
                        :label="this.$t('Issue.Label.Deadline')"
                        label-for="h-searchForm-dateTo"
                        label-cols-md="4"
                    >
                        <date-picker
                            id="h-searchForm-dateTo"
                            v-model="searchForm.dateTo"
                            type="datetime"
                            :locale="currentLocale"
                            format="DD-MM-YYYY HH:mm:ss"
                            value-type="YYYY-MM-DD HH:mm:ss"
                            style="width: 100%"
                        ></date-picker>
                    </b-form-group>
                </b-col>
            </div>
        </b-sidebar>

    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import moment from 'moment'
export default {
    components: {
        ToastificationContent,
    },
    mixins: [authorizationMixin],
    data() {
        return {
            searchDatas: [
                {
                    searchType: 'v-select',
                    filterName: 'filterProject',
                    label: 'Issue.Label.Project',
                    placeholder: 'Issue.Label.Project',
                    multiple: true,
                    options: [],
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterIssueTypeId',
                    label: 'Issue.Label.Type',
                    placeholder: 'Issue.Label.Type',
                    multiple: true,
                    options: [],
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterPriority',
                    label: 'Issue.Label.Priority',
                    placeholder: 'Issue.Label.Priority',
                    multiple: true,
                    options: [],
                    autoSearch: true,
                },
                {
                    searchType: 'v-select',
                    filterName: 'filterStatusId',
                    label: 'Issue.Label.Status',
                    placeholder: 'Issue.Label.Status',
                    multiple: true,
                    options: [],
                    autoSearch: true,
                },
                {
                    filterName: 'filterCode',
                    label: 'Issue.Label.Code',
                    placeholder: 'Issue.Label.Code',
                },
            ],
            searchForm: {
                filterCode: null,
                filterProject: null,
                filterIssueTypeId: null,
                filterStatusId: null,
                filterPriority: null,
            },
            table: {
                dataUrl: '/issue',
                columns: [
                    {
                        label: 'Issue.Label.CreatedAt',
                        field: 'createdAt',
                    },
                    {
                        label: 'Issue.Label.Project',
                        field: 'projectId',
                    },
                    {
                        label: 'Issue.Label.Code',
                        field: 'code',
                    },
                    {
                        label: 'Issue.Label.Type',
                        field: 'issueTypeId',
                    },
                    {
                        label: 'Issue.Label.Title',
                        field: 'title',
                    },
                    {
                        label: 'Issue.Label.Status',
                        field: 'statusId',
                    },
                    {
                        label: 'Issue.Label.Priority',
                        field: 'priorityId',
                    },
                    {
                        label: 'Issue.Label.User',
                        field: 'userId',
                    },
                    {
                        label: 'Issue.Label.Action',
                        field: 'action',
                    },
                ],
            },
            authorizeName: {
                manage: 'ManageIssue',
                view: 'ViewIssue',
            },
            routerLink: {
                create: '/issues/create/',
                detail: '/issues/detail/',
                delete: '/issue/',
            },
            listUser: [],
            listContractor: [],
            isSidebarActive: false,
            dataIssue: [],
            modalCreateIssue: false,
            showReportModal: false,
            reportViewer: {
                serviceUrl: `${process.env.VUE_APP_BASE_URL}/api/reports`,
                reportSource: 'ReportProblemDetail.trdp',
                parameters: {
                    pid:null,
                    lang: this.$i18n.locale,
                },
                scaleMode: 'FIT_PAGE_WIDTH',
                viewMode: 'INTERACTIVE',
                scale: 1.0,
                show: false,
            },
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        baseURL() {
            return process.env.VUE_APP_BASE_URL
            //return "https://novaland.atin.vn/Service"
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.searchForm.compId = accessToken.companyId
        this.lookupData()
    },
    methods: {
        onReport(row) {
            debugger
            this.reportViewer.parameters.pid = row.id
            this.reportViewer.parameters.lang = this.$i18n.locale
            this.reportViewer.parameters.title = row.title
            this.reportViewer.parameters.contractor = this.getStaticName(row.contractorId, this.listContractor)
            this.reportViewer.parameters.code = row.code
            this.reportViewer.parameters.accessTime = this.formatDateTime(row.createdAt)
            this.reportViewer.parameters.description = row.description
            this.reportViewer.parameters.project = this.getStaticName(row.projectId, this.searchDatas[0].options)
            this.reportViewer.parameters.issueType = this.getStaticName(row.issueTypeId, this.searchDatas[1].options)
            this.reportViewer.parameters.totalFine = Number(row.totalFine).toLocaleString('vi-VN')
            this.reportViewer.parameters.baseURL = this.baseURL
            this.reportViewer.parameters.dateNow = moment().format('DD/MM/YYYY')
            this.reportViewer.parameters.datetimeNow = moment().format('DD/MM/YYYY HH:mm:ss')

            this.showReportModal = true

            // Refresh report after modal opens
            this.$nextTick(() => {
                if (this.$refs.reportViewer) {
                    this.$refs.reportViewer.refresh()
                }
            })
        },
        showDetailIssue(data){
            console.log(data)
            this.dataIssue = data
            this.isSidebarActive = true
        },
        getStaticName(id, lst) {
            return this.$t(lst.find((x) => x.id == id)?.text)
        },
        getStaticColor(id, lst) {
            const item =  lst.find((x) => x.id== id)
            return item ? item.color : "#000"
        },
        handleBinding(event) {
            this.searchForm[event.filterName] = event.value
            if (event.autoSearch) this.search()
        },
        search() {
            this.$refs.issueTable.refresh()
        },
        doDelete(item) {
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
                            this.$refs.issueTable.refresh()
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
        lookupData() {
            // this.$services.get('/lookup/company-tree').then((response) => {
            //     this.searchDatas[0].options = response.data
            // })
            this.$services.get('/lookup/ims_project').then((response) => {
                this.searchDatas[0].options = response.data.data
            })
            this.$services.get('/lookup/ims_issusType').then((response) => {
                this.searchDatas[1].options = response.data.data
            })
            this.$services.get('/lookup/ims_priority').then((response) => {
                this.searchDatas[2].options = response.data.data
            })
            this.$services.get('/lookup/ims_status').then((response) => {
                this.searchDatas[3].options = response.data.data
            })
            this.$services.get('/lookup/user').then((response) => {
                this.listUser = response.data.data
            })
            this.$services.get('/lookup/departments?type=2').then((response) => {
                this.listContractor = response.data.data
            })
        },
        formatDateTime(dateString) {
            const date = new Date(dateString)
            const day = String(date.getDate()).padStart(2, '0')
            const month = String(date.getMonth() + 1).padStart(2, '0')
            const year = date.getFullYear()
            const hours = String(date.getHours()).padStart(2, '0')
            const minutes = String(date.getMinutes()).padStart(2, '0')
            const seconds = String(date.getSeconds()).padStart(2, '0')
            return `${day}/${month}/${year} ${hours}:${minutes}:${seconds}`
        },
        handleCreateSuccess() {
            this.modalCreateIssue = false 
            this.$refs.issueTable.refresh()       
        }
    },
}
</script>

<style lang="scss">
.sidebar-xxl {
    width: 30vw !important;
    /* tùy ý: 600px, 800px... */
    max-width: 100%;
}

.sidebar-header {
    position: sticky;
    top: 0;
    /* để che nội dung khi scroll */
    z-index: 10;
    padding: 1rem;
    border-bottom: 1px solid #ddd;
    height: 20vh;
}

.sidebar-body {
    max-height: calc(80vh);
    /* trừ chiều cao header */
    overflow-y: auto;
    padding: 1rem;
}

.input-action {
  display: flex;
  align-items: center;
  gap: 4px;
}

.input-action input {
  flex: 1;
  padding: 6px 8px;
}

.input-action .actions button {
  border: 1px solid #ccc;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 4px;
}

.btn-save {
  color: green;
}

.btn-cancel {
  color: red;
}

.center-label {
  padding: 2px 6px;       
  border-radius: 4px;     
  font-weight: 500;
  display: inline-block;
  min-width: 60px;        
  text-align: center;
}
.modal-70 {
    max-width: 70vw;
}
</style>