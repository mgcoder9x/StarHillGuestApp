<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <b-col md="12">
                            <b-form-group :label="this.$t('Issue.Label.Title')" label-for="h-camera-code"
                                label-cols-md="2" label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="DeviceCode">
                                    <b-form-input id="h-camera-code" v-model="issue.title" :state="errors.length > 0 ? false : null
                                        " :disabled="!editing" />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="6">
                            <b-form-group :label="this.$t('Issue.Label.Project')" label-cols-md="4"
                                label-class="required">
                                <validation-provider #default="{ errors }" rules="required"
                                    :name="$t('Issue.Label.Project')">
                                    <v-select v-model="issue.projectId" label="text" :reduce="(issue) => issue.id"
                                        :options="listIssueProject" :disabled="!editing">
                                    </v-select>
                                    <small class="text-danger">{{ errors[0] }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group :label="this.$t('Issue.Label.Type')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required"
                                    :name="$t('Issue.Label.Type')">
                                    <v-select v-model="issue.issueTypeId" label="text" :reduce="(area) => area.id"
                                        :options="listIssueType" :disabled="!editing">
                                    </v-select>
                                    <small class="text-danger">{{ errors[0] }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="6">
                            <b-form-group :label="this.$t('Issue.Label.Priority')" label-cols-md="4"
                                label-class="required">
                                <validation-provider #default="{ errors }" rules="required"
                                    :name="$t('Issue.Label.Priority')">
                                    <v-select v-model="issue.priorityId" label="text" :reduce="(area) => area.id"
                                        :options="listPriority" :disabled="!editing">
                                    </v-select>
                                    <small class="text-danger">{{ errors[0] }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group :label="this.$t('Ngày phát hiện')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required" :name="$t('Ngày phát hiện')">
                                    <date-picker v-model="issue.createdAt" type="datetime" :locale="currentLocale" :disabled="!editing"
                                        format="DD-MM-YYYY HH:mm:ss" value-type="YYYY-MM-DD HH:mm:ss" style="width: 100%" />
                                    <small class="text-danger">{{ errors[0] }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="6">
                            <b-form-group :label="this.$t('Issue.Label.User')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required"
                                    :name="$t('Issue.Label.User')">
                                    <v-select v-model="issue.userId" label="text" :reduce="(area) => area.id"
                                        :options="listUser" :disabled="!editing">
                                    </v-select>
                                    <small class="text-danger">{{ errors[0] }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group :label="this.$t('Bộ phận thanh tra')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required" :name="$t('Bộ phận thanh tra')">
                                    <v-select v-model="issue.inspectId" label="text" :reduce="(area) => area.id"
                                        :options="listUser" :disabled="!editing">
                                    </v-select>
                                    <small class="text-danger">{{ errors[0] }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <!-- <b-col md="6">
                            <b-form-group :label="this.$t('Nhà thầu')" label-cols-md="4">
                                <v-select v-model="issue.contractorId" label="text" :reduce="(item) => item.id"
                                    :options="listContractor" @input="changeContractorId($event)" :disabled="!editing">
                                </v-select>
                            </b-form-group>
                        </b-col> -->
                        <b-col md="6">
                            <b-form-group :label="this.$t('Nhà thầu')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required"
                                    :name="$t('Issue.Label.Project')">
                                    <v-select v-model="issue.contractorId" label="text" :reduce="(area) => area.id"
                                        :options="listContractor" @input="changeContractorId($event)" :disabled="!editing">
                                    </v-select>
                                    <small class="text-danger">{{ errors[0] }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group :label="this.$t('Nhân sự vi phạm')" label-cols-md="4">
                                <v-select v-model="issue.personIds" label="text" :reduce="(item) => item.id"
                                    :options="listEmployee" multiple :disabled="!editing">
                                </v-select>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="6">
                            <b-form-group :label="this.$t('Issue.Label.Code')" label-for="h-camera-code"
                                label-cols-md="4">
                                <validation-provider #default="{ errors }" rules="required"
                                    :name="$t('Issue.Label.User')">
                                    <b-form-input id="h-camera-code" v-model="issue.code" :state="errors.length > 0 ? false : null
                                        " :disabled="true" />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group :label="this.$t('Issue.Label.Status')" label-cols-md="4"
                                label-class="required">
                                <validation-provider #default="{ errors }" rules="required"
                                    :name="$t('Issue.Label.Status')">
                                    <v-select v-model="issue.statusId" label="text" :reduce="(area) => area.id"
                                        :options="listStatus" :disabled="!editing">
                                    </v-select>
                                    <small class="text-danger">{{ errors[0] }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    
                    <b-row>
                        <b-col md="6">
                            <b-form-group :label="$t('Issue.Label.TotalFine')" label-cols-md="4">
                                <b-form-input type="number" v-model.number="issue.totalFine" step="any" :disabled="!editing"/>
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <b-row>
                        <b-col>
                            <b-form-group :label="this.$t('Issue.Label.Description')" label-cols-md="2">
                                <quill-editor v-model="issue.description" :options="editorOptions"
                                    class="h-40 border rounded quill-custom" ref="editor" :disabled="!editing" />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col>
                            <b-form-group :label="this.$t('Issue.Label.Attachment')" label-cols-md="2">
                                <b-form-file id="file-input" ref="fileInput" multiple v-model="fileUpload"
                                    :disabled="!editing"></b-form-file>
                                <!-- Hiển thị danh sách file đã upload -->
                                <div v-for="(file, index) in issue.listFile" :key="index"
                                    class="d-flex align-items-center mt-2">

                                    <!-- Nếu là ảnh của event -->
                                    <div v-if="file.isAuto" class="mr-2">
                                        <video v-if="file.mimeType.startsWith('video/')"
                                            :src="getUrlIsAuto(file.filePath)" width="64" height="48"
                                            class="rounded border" muted></video>
                                        <img v-else :src="getUrlIsAuto(file.filePath)" alt="preview" width="64"
                                            height="48" class="rounded border" />
                                    </div>

                                    <!-- Ảnh -->
                                    <div v-else-if="file.mimeType.startsWith('image/')" class="mr-2">
                                        <img :src="getUrl(file.filePath)" alt="preview" width="64" height="48"
                                            class="rounded border" />
                                    </div>

                                    <!-- Video (thumbnail nhỏ, không controls) -->
                                    <div v-else-if="file.mimeType.startsWith('video/')" class="mr-2">
                                        <video :src="getUrl(file.filePath)" width="64" height="48"
                                            class="rounded border" muted></video>
                                    </div>

                                    <!-- File khác -->
                                    <div v-else class="mr-2">
                                        <i class="far fa-file-alt fa-2x"></i>
                                    </div>

                                    <!-- Tên + dung lượng -->
                                    <div class="flex-grow-1">
                                        <!-- Click fileName mới mở modal hoặc tải file -->
                                        <div class="file-name" @click="openPreview(file)"
                                            style="cursor: pointer; font-weight: 500;">
                                            {{ file.fileName }}
                                        </div>
                                    </div>

                                    <!-- Xóa -->
                                    <b-button size="sm" variant="link" @click="removeFile(index)" :disabled="!editing">
                                        <Icon icon="mdi:close" class="xs-icon" />
                                    </b-button>
                                </div>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <!-- <b-row>
                        <b-col>
                            <b-form-group :label="$t('Events.Table.Image')" label-cols-md="12">
                                <media-swiper :uuid="issue.eventId" />
                            </b-form-group>
                        </b-col>
                    </b-row> -->

                    <b-row>
                        <b-col>
                            <div class="text-center">
                                <Transition mode="out-in">
                                    <b-button v-if="
                                        editing && authorize(['ManageIssue'])
                                    " v-waves type="button" variant="primary"
                                        class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                        @click="validationForm">
                                        <Icon icon="material-symbols:save-outline" class="sm-icon" />
                                        <span class="ml-50">
                                            {{ $t('common.button.save') }}
                                        </span>
                                    </b-button>
                                    <b-button v-if="
                                        !editing &&
                                        authorize(['ManageIssue'])
                                    " v-waves type="button" variant="primary"
                                        class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                        @click="startEdit">
                                        <Icon icon="line-md:edit-twotone" class="sm-icon" />
                                        <span class="ml-25">
                                            {{ $t('common.button.edit') }}
                                        </span>
                                    </b-button>
                                </Transition>
                                <b-button v-if="!editing" v-waves :to="{ path: '/issues/list' }" type="button"
                                    variant="outline-secondary"
                                    class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0">
                                    <Icon icon="line-md:arrow-small-left" class="sm-icon" />
                                    <span class="ml-25">
                                        {{ $t('common.button.back') }}
                                    </span>
                                </b-button>
                                <b-button v-if="editing" v-waves type="button" class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary btn-hover-linear-secondary border-0" @click="stopEdit">
                                    <Icon icon="mdi:cancel" class="sm-icon" />
                                    <span class="ml-50">
                                        {{ $t('common.button.cancel') }}
                                    </span>
                                </b-button>
                            </div>
                        </b-col>
                    </b-row>
                </b-form>
                <!-- Tabs nằm dưới cùng -->
                <b-tabs card lazy class="mt-2">
                    <!-- Tab Comment -->
                    <b-tab :title="$t('Issue.Label.Comment')">
                        <div class="p-2">
                            <h5>{{ $t('Issue.Label.Comment') }}</h5>
                            <div v-for="(c, index) in issue.listComment" :key="index" class="mb-3 border-bottom pb-2">
                                <!-- Nếu đang sửa -->
                                <div v-if="c.isEditing">
                                    <b-form-textarea v-model="c.editContent" rows="2" />
                                    <div class="mt-1">
                                        <b-button size="sm" variant="success" class="mr-1" @click="saveComment(index)">
                                            {{ $t('common.button.save') }}
                                        </b-button>
                                        <b-button size="sm" variant="secondary" @click="cancelEdit(index)">
                                            {{ $t('common.button.cancel') }}
                                        </b-button>
                                    </div>
                                </div>
                                <!-- Nếu đang hiển thị -->
                                <div v-else>
                                    <strong>{{ c.authorName }}</strong>
                                    <small class="text-muted">({{ c.createdAtStr }})</small>
                                    <div>{{ c.body }}</div>
                                    <div class="mt-1" v-if="c.authorId == userId">
                                        <b-button size="sm" variant="link" @click="editComment(index)">
                                            <Icon icon="line-md:edit-twotone" /> {{ $t('common.button.edit') }}
                                        </b-button>
                                        <b-button size="sm" variant="link" class="text-danger"
                                            @click="deleteComment(index)">
                                            <Icon icon="mdi:delete" /> {{ $t('common.button.delete') }}
                                        </b-button>
                                    </div>
                                </div>
                            </div>

                            <!-- Thêm comment -->
                            <b-form-group :label="$t('Issue.Label.NewComment')">
                                <b-form-textarea v-model="newComment" rows="3" />
                            </b-form-group>
                            <b-button size="sm" variant="primary" @click="addComment">
                                {{ $t('Button.AddComment') }}
                            </b-button>
                        </div>
                    </b-tab>

                    <!-- Tab Lịch sử thao tác -->
                    <b-tab :title="$t('Issue.Label.History')">
                        <div class="p-2">
                            <ul class="list-unstyled">
                                <li v-for="(h, index) in issue.listLogs" :key="index" class="mb-3">
                                    <!-- Dòng actor và thời gian -->
                                    <div>
                                        <strong class="text-primary">{{ h.actorName }}</strong>
                                        <small class="text-muted"> - {{ h.createdAtStr }}</small>
                                    </div>

                                    <!-- Bảng chi tiết Field, Original, New -->
                                    <table class="table table-sm mt-1">
                                        <thead>
                                            <tr>
                                                <th style="width: 15vw;">{{ $t('Issue.Label.Action') }}</th>
                                                <th style="width: 30vw;">{{ $t('Issue.Label.OldValue') }}</th>
                                                <th>{{ $t('Issue.Label.NewValue') }}</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td>{{ $t(h.action) }}</td>
                                                <td>{{ h.sourceValue }}</td>
                                                <td>{{ h.destinationValue }}</td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </li>
                            </ul>
                        </div>
                    </b-tab>
                </b-tabs>
            </b-card-body>
        </b-card>

        <!-- Modal preview -->
        <b-modal id="preview-modal" v-model="isPreviewModalVisible" size="lg" hide-footer centered>
            <div class="text-center">
                <template v-if="previewType === 'image'">
                    <img :src="previewUrl" alt="preview" style="max-width: 100%; max-height: 80vh;" />
                </template>
                <template v-else-if="previewType === 'video'">
                    <video :src="previewUrl" controls autoplay muted style="max-width: 100%; max-height: 80vh;"></video>
                </template>
            </div>
        </b-modal>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import { $themeConfig } from '@themeConfig'
import moment from 'moment'
import { quillEditor } from 'vue-quill-editor'

const isDevEnv = process.env.NODE_ENV == 'development'
export default {
    mixins: [authorizationMixin],
    components: {
        quillEditor
    },
    setup() {
        // App Name
        const { apiURL } = $themeConfig.app
        return {
            apiURL,
        }
    },
    data() {
        return {
            newComment: '',
            history: [
                { time: '27/08/2025 13:00', action: 'Tạo issue' },
                { time: '27/08/2025 14:00', action: 'Thay đổi trạng thái sang In Progress' }
            ],
            editorOptions: {
                placeholder: 'Type description here...',
                theme: 'snow',
                modules: {
                    toolbar: {
                        container: [
                            ['bold', 'italic', 'underline', 'strike'],
                            [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                            [{ 'header': [1, 2, 3, false] }],
                            ['clean']
                        ],
                    }
                }
            },
            issue: {
                projectId: null,
                issueTypeId: null,
                priorityId: null,
                userId: null,
                description: null,
                inspectId: null,
                contractorId: null,
                totalFine: null,
                personIds: []
            },
            listIssueProject: [],
            listIssueType: [],
            listUser: [],
            listContractor: [],
            listEmployee: [],
            listPriority: [],
            listStatus: [],
            editing: false,
            fileUpload: null,
            isPreviewModalVisible: false,
            previewType: null, // "image" | "video"
            previewUrl: null,
            userId: null,
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
        issueId() {
            return this.$route.params.issueId
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.userId = accessToken.userId
        this.loadIssueType()
        this.loadIssueProject()
        this.loadUser()
        this.loadContractor()
        this.loadPriority()
        this.loadStatus()
        this.loadIssueDetail()
    },
    watch: {
        fileUpload(newFiles) {
            if (newFiles && newFiles.length > 0) {
                this.uploadFiles(newFiles);
            }
        }
    },
    methods: {
        openPreview(file) {
            const url = file.isAuto ? this.getUrlIsAuto(file.filePath) : this.getUrl(file.filePath)

            // Check xem có phải image
            const isImage = file.mimeType?.startsWith('image/')

            // Check xem có phải video
            const isVideo = file.mimeType?.startsWith('video/')

            if (isImage || isVideo) {
                this.previewType = isImage ? 'image' : 'video'
                this.previewUrl = url
                this.isPreviewModalVisible = true
            } else {
                // Luôn tải về file (kể cả .txt)
                fetch(url)
                    .then(res => res.blob())
                    .then(blob => {
                        const link = document.createElement('a')
                        link.href = window.URL.createObjectURL(blob)
                        link.download = file.fileName || 'download'
                        document.body.appendChild(link)
                        link.click()
                        link.remove()
                        window.URL.revokeObjectURL(link.href)
                    })
            }
        },
        async confirmDelete() {
            return await this.$swal.fire({
                title: this.$t('common.confirmation.delete.title'),
                text: this.$t('common.confirmation.delete.message'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: this.$t('Message.Agree'),
                cancelButtonText: this.$t('Message.Exit'),
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-outline-danger ml-1',
                },
                buttonsStyling: false,
            })
        },
        editComment(index) {
            this.issue.listComment[index].isEditing = true
            this.issue.listComment[index].editContent = this.issue.listComment[index].body
        },
        saveComment(index) {
            debugger
            this.issue.listComment[index].body = this.issue.listComment[index].editContent

            this.$services.put(`/issue/editComment/${this.issue.listComment[index].id}`, this.issue.listComment[index]).then((response) => {
                this.issue.listComment[index].isEditing = false
                this.issue.listComment[index].editContent = ''
            })
        },
        cancelEdit(index) {
            this.issue.listComment[index].isEditing = false
            this.issue.listComment[index].editContent = ''
        },
        async deleteComment(index) {
            const { isConfirmed } = await this.confirmDelete()
            if (isConfirmed) {
                await this.$services.delete(`/issue/deleteComment/${this.issue.listComment[index].id}`).then((rs) => {
                    this.issue.listComment.splice(index, 1)
                })
            }
        },
        addComment() {
            var newComment = {
                body: this.newComment
            }
            this.$services.post(`/issue/addComment/${this.issueId}`, newComment).then((response) => {
                this.newComment = ''
                this.loadIssueDetail()
            })
        },
        showImage(url) {
            this.selectedImage = url
            this.isImageModalVisible = true
        },
        removeFile(index) {
            this.issue.listFile.splice(index, 1);
        },
        async uploadFiles(files) {
            try {
                let formData = new FormData();
                for (let f of files) {
                    formData.append("files", f);
                }
                this.$services.post('/file/issue', formData).then((response) => {
                    debugger
                    this.issue.listFile.push(...response.data)
                })
            } catch (err) {
                console.error("Upload failed:", err);
            }
        },
        getUrl(path) {
            return `${process.env.VUE_APP_BASE_URL}/IMSFile/${path}`
            //return `http://192.168.1.85:42001/Service/IMSFile/${path}`
        },
        getUrlIsAuto(path) {
            return `${process.env.VUE_APP_BASE_URL}${path}`
            //return `http://192.168.1.85:42001/Service/${path}`
        },
        loadStatus() {
            this.$services.get('/lookup/ims_status').then((response) => {
                this.listStatus = response.data.data
            })
        },
        loadUser() {
            this.$services.get('/lookup/user').then((response) => {
                this.listUser = response.data.data
            })
        },
        loadIssueType() {
            this.$services.get('/lookup/ims_issusType').then((response) => {
                this.listIssueType = response.data.data
            })
        },
        loadIssueProject() {
            this.$services.get('/lookup/ims_project').then((response) => {
                this.listIssueProject = response.data.data
            })
        },
        loadPriority() {
            this.$services.get('/lookup/ims_priority').then((response) => {
                this.listPriority = response.data.data
            })
        },
        loadContractor() {
            this.$services.get('/lookup/departments?type=2').then((response) => {
                this.listContractor = response.data.data
            })
        },
        changeContractorId() {
            this.issue.personIds = []
            if (this.issue.contractorId) {
                this.loadEmployee()
            }
            else {
                this.listEmployee = []
            }
        },
        loadEmployee() {
            if (!this.issue.contractorId) return
            this.$services.get(`/lookup/departments/${this.issue.contractorId}/employees`).then((response) => {
                    this.listEmployee = response.data.data
                })
        },
        loadIssueDetail() {
            this.$services.get(`/issue/${this.issueId}`).then((response) => {
                this.issue = response.data.data
                this.issue.projectId = this.issue.projectId.toString()
                this.issue.issueTypeId = this.issue.issueTypeId.toString()
                this.issue.priorityId = this.issue.priorityId.toString()
                this.issue.userId = this.issue.userId.toString()
                this.issue.statusId = this.issue.statusId.toString()
                this.issue.contractorId = this.issue.contractorId.toString()
                this.issue.inspectId = this.issue.inspectId.toString()
                this.issue.createdAt = moment(this.issue.createdAt).format('YYYY-MM-DD HH:mm:ss')
                if (this.issue.contractorId) {
                    this.loadEmployee()
                }

                this.issue.dueAt = this.issue.dueAt ? moment.utc(this.issue.dueAt).format('YYYY-MM-DD HH:mm:ss') : null
                this.issue.closedAt = this.issue.closedAt ? moment.utc(this.issue.closedAt).format('YYYY-MM-DD HH:mm:ss') : null

                this.issue.listComment = this.issue.listComment.map(c => ({
                    ...c,
                    isEditing: false,
                    editContent: ''
                }))
            })
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .put(`/issue/${this.issueId}`, this.issue)
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Update'),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.stopEdit()
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${this.$t(
                                        error.message
                                    )}`,
                                },
                            })
                        })
                }
            })
        },

        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.loadIssueDetail()
        },
    },
}
</script>

<style lang="scss">
.quill-custom .ql-toolbar {
    overflow: visible !important;
}

.quill-custom .ql-picker {
    overflow: visible !important;
    position: relative;
}

.quill-custom .ql-picker-options {
    position: absolute !important;
    z-index: 9999 !important;
}
</style>
