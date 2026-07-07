<template>
    <validation-observer ref="rules">
        <b-card-body>
            <div class="p-6 shadow-md rounded-lg max-w-3xl mx-auto">
                <!-- <h2 class="text-xl font-semibold mb-4">Create Issue</h2> -->
                <b-row>
                    <b-col md="12">
                        <b-form-group :label="this.$t('Issue.Label.Title')" label-cols-md="1" label-class="required">
                            <validation-provider #default="{ errors }" rules="required" :name="$t('Issue.Label.Title')">
                                <b-form-input v-model="createData.title" />
                                <small class="text-danger">{{ errors[0] }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                </b-row>
                <b-row>
                    <b-col md="6">
                        <b-form-group :label="this.$t('Issue.Label.Project')" label-cols-md="2" label-class="required">
                            <validation-provider #default="{ errors }" rules="required"
                                :name="$t('Issue.Label.Project')">
                                <v-select v-model="createData.projectId" label="text" :reduce="(area) => area.id"
                                    :options="listIssueProject">
                                </v-select>
                                <small class="text-danger">{{ errors[0] }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group :label="this.$t('Issue.Label.Type')" label-cols-md="2" label-class="required">
                            <validation-provider #default="{ errors }" rules="required" :name="$t('Issue.Label.Type')">
                                <v-select v-model="createData.issueTypeId" label="text" :reduce="(area) => area.id"
                                    :options="listIssueType">
                                </v-select>
                                <small class="text-danger">{{ errors[0] }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-row>
                    <b-col md="6">
                        <b-form-group :label="this.$t('Issue.Label.Priority')" label-cols-md="2" label-class="required">
                            <validation-provider #default="{ errors }" rules="required"
                                :name="$t('Issue.Label.Priority')">
                                <v-select v-model="createData.priorityId" label="text" :reduce="(area) => area.id"
                                    :options="listPriority">
                                </v-select>
                                <small class="text-danger">{{ errors[0] }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group :label="this.$t('Issue.Label.CreatedAt')" label-cols-md="2" label-class="required">
                            <validation-provider #default="{ errors }" rules="required" :name="$t('Issue.Label.CreatedAt')">
                                <date-picker v-model="createData.createdAt" type="datetime" :locale="currentLocale"
                                    format="DD-MM-YYYY HH:mm:ss" value-type="YYYY-MM-DD HH:mm:ss" style="width: 100%" />
                                <small class="text-danger">{{ errors[0] }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-row>
                    <b-col md="12">
                        <b-form-group :label="this.$t('Issue.Label.Description')" label-cols-md="1">
                            <quill-editor v-model="createData.description" :options="editorOptions"
                                class="h-40 border rounded quill-custom" ref="editor" />
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-row>
                    <b-col md="12">
                        <b-form-group :label="this.$t('Issue.Label.Attachment')" label-cols-md="1">
                            <b-form-file id="file-input" ref="fileInput" multiple v-model="fileUpload"></b-form-file>

                            <!-- Hiển thị danh sách file đã upload -->
                            <div v-for="(file, index) in listFileSuccess" :key="index"
                                class="d-flex align-items-center mt-2">

                                <!-- Nếu là ảnh của event -->
                                <div v-if="file.isAuto" class="mr-2">
                                    <video v-if="file.fileType == 2" :src="getUrlIsAuto(file.filePath)" width="64"
                                        height="48" class="rounded border" muted></video>
                                    <img v-else :src="getUrlIsAuto(file.filePath)" alt="preview" width="64" height="48"
                                        class="rounded border" />
                                </div>

                                <!-- Ảnh -->
                                <div v-else-if="file.mimeType.startsWith('image/')" class="mr-2">
                                    <img :src="getUrl(file.filePath)" alt="preview" width="64" height="48"
                                        class="rounded border" />
                                </div>

                                <!-- Video (thumbnail nhỏ, không controls) -->
                                <div v-else-if="file.mimeType.startsWith('video/')" class="mr-2">
                                    <video :src="getUrl(file.filePath)" width="64" height="48" class="rounded border"
                                        muted></video>
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
                                    <small class="text-muted" v-if="file.isAuto != true">{{ formatSize(file.size)
                                    }}</small>
                                </div>

                                <!-- Xóa -->
                                <b-button size="sm" variant="link" @click="removeFile(index)">
                                    <Icon icon="mdi:close" class="xs-icon" />
                                </b-button>
                            </div>
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-row>
                    <b-col md="6">
                        <b-form-group :label="this.$t('Issue.Label.User')" label-cols-md="2" label-class="required">
                            <validation-provider #default="{ errors }" rules="required" :name="$t('Issue.Label.User')">
                                <v-select v-model="createData.userId" label="text" :reduce="(area) => area.id"
                                    :options="listUser">
                                </v-select>
                                <small class="text-danger">{{ errors[0] }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group :label="this.$t('Issue.Label.InspectionDepartment')" label-cols-md="2"
                            label-class="required">
                            <validation-provider #default="{ errors }" rules="required"
                                :name="$t('Issue.Label.InspectionDepartment')">
                                <v-select v-model="createData.inspectId" label="text" :reduce="(area) => area.id"
                                    :options="listUser">
                                </v-select>
                                <small class="text-danger">{{ errors[0] }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-row>
                    <b-col md="6">
                        <b-form-group :label="this.$t('Issue.Label.Contractor')" label-cols-md="2"
                            label-class="required">
                            <validation-provider #default="{ errors }" rules="required"
                                :name="$t('Issue.Label.Contractor')">
                                <v-select v-model="createData.contractorId" label="text" :reduce="(area) => area.id"
                                    :options="listContractor" @input="changeContractorId($event)">
                                </v-select>
                                <small class="text-danger">{{ errors[0] }}</small>
                            </validation-provider>
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group :label="this.$t('Issue.Label.PersonViolation')" label-cols-md="2">
                            <v-select v-model="createData.personIds" label="text" :reduce="(emp) => emp.value"
                                :options="listEmployee" multiple>
                            </v-select>
                        </b-form-group>
                    </b-col>
                </b-row>
                <b-row>
                    <b-col md="6">
                        <b-form-group :label="$t('Issue.Label.TotalFine')" label-cols-md="2">
                            <b-form-input type="number" v-model.number="createData.totalFine" step="any" />
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-col class="text-center">
                    <b-button @click="createIssue" variant="success">
                        {{ $t('Button.Create') }}
                    </b-button>
                </b-col>
            </div>
        </b-card-body>
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
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import 'quill/dist/quill.core.css'
// eslint-disable-next-line
import 'quill/dist/quill.snow.css'
// eslint-disable-next-line
import 'quill/dist/quill.bubble.css'
import moment from 'moment'
import { quillEditor } from 'vue-quill-editor'
import Quill from 'quill';

// Lấy ra icons của quill
const icons = Quill.import('ui/icons');

// Gán thêm icon mới cho 'attachment'
icons['attachment'] = '<i class="fas fa-paperclip"></i>';
// hoặc SVG thuần tuý: '<svg viewBox="0 0 18 18">...</svg>'

export default {
    components: {
        quillEditor,
    },
    props: {
        createData: {
            type: Object,
            required: true
        }
    },
    data() {
        return {
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
            form: {
                project: "2025",
                issueType: "bug",
                summary: "",
                description: "",
                priority: "medium",
                labels: "",
                environment: "",
                attachment: [],
                assignee: "auto",
                epicLink: "",
                sprint: ""
            },
            listFileSuccess: [],
            fileUpload: null,
            listIssueType: [],
            listUser: [],
            listIssueProject: [],
            listPriority: [],
            listContractor: [],
            listEmployee: [],
            listMapType: [],
            isPreviewModalVisible: false,
            previewType: null, // "image" | "video"
            previewUrl: null,
        }
    },
    async created() {
        var vm = this
        await this.loadIssueProject()
        await this.loadIssueType()
        this.loadUser()
        this.loadPriority()
        this.loadContractor()
        await this.loadMapTypeIssueEvent()
        if (!vm.createData) {
            vm.createData = {
                userId: null,
                issueTypeId: null,
                projectId: null,
                priorityId: null,
                title: null,
                description: null,
                issueType: null,
                listFile: [],
                inspectId: null,
                contractorId: null,
                personIds: [],
                totalFine: null

            }
        }
        if (vm.createData.title == null) {
            this.$set(vm.createData, 'title', 'Biên bản phạt vi phạm an toàn lao động, vệ sinh công trường')
        }
        if (vm.createData.warningLevelId) {
            var map = vm.listMapType.find(x => x.warningLevelId == vm.createData.warningLevelId)
            if (map) {
                this.$set(vm.createData, 'projectId', map.projectId.toString())
                this.$set(vm.createData, 'issueTypeId', map.issueTypeId.toString())
            }
        }
        if (vm.createData.eventId) {
            vm.getData()
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    watch: {
        fileUpload(newFiles) {
            if (newFiles && newFiles.length > 0) {
                this.uploadFiles(newFiles);
            }
        }
    },
    methods: {
        async loadMapTypeIssueEvent() {
            await this.$services.get('/issue/getMapTypeIssueEvent').then((response) => {
                this.listMapType = response.data.data
            })
        },
        loadContractor() {
            return this.$services.get('/lookup/departments?type=2').then((response) => {
                this.listContractor = response.data.data
            })
        },
        changeContractorId(event) {
            this.createData.personIds = []
            if (event) {
                this.$services.get(`/lookup/departments/${event}/employees`).then((response) => {
                    this.listEmployee = response.data.data
                })
            }
            else {
                this.listEmployee = []
            }
        },
        openPreview(file) {
            const url = file.isAuto ? this.getUrlIsAuto(file.filePath) : this.getUrl(file.filePath)

            // Check xem có phải image
            const isImage = (file.isAuto && file.fileType !== 2) || file.mimeType?.startsWith('image/')

            // Check xem có phải video
            const isVideo = (file.isAuto && file.fileType == 2) || file.mimeType?.startsWith('video/')

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

        async getData() {
            const response = await this.$services.get(
                `/event/eventFilesById/${this.createData.eventIds}`
            )
            const newData = response.data.map(x => ({
                ...x,
                id: null,
                isAuto: true
            }))
            this.listFileSuccess.push(...newData)
        },
        getUrl(path) {
            return `${process.env.VUE_APP_BASE_URL}/IMSFile/${path}`
            //return `http://192.168.1.85:42001/Service/IMSFile/${path}`
        },
        getUrlIsAuto(path) {
            return `${process.env.VUE_APP_BASE_URL}${path}`
            //return `http://192.168.1.85:42001/Service/${path}`
        },
        formatSize(size) {
            if (size < 1024) return size + " B";
            if (size < 1024 * 1024) return (size / 1024).toFixed(1) + " KB";
            return (size / (1024 * 1024)).toFixed(1) + " MB";
        },
        removeFile(index) {
            this.listFileSuccess.splice(index, 1);
        },
        async uploadFiles(files) {
            try {
                let formData = new FormData();
                for (let f of files) {
                    formData.append("files", f);
                }
                this.$services.post('/file/issue', formData).then((response) => {
                    this.listFileSuccess.push(...response.data)
                })
            } catch (err) {
                console.error("Upload failed:", err);
            }
        },
        selectImage() {
            const input = document.createElement("input");
            input.setAttribute("type", "file");
            input.setAttribute("accept", "image/*");
            input.click();

            input.onchange = () => {
                const file = input.files[0];

                if (file) {
                    // === 1. Thêm file vào b-form-file ===
                    this.createData.files = [...this.createData.files, file];

                    // === 2. Hiển thị trong quill-editor ===
                    const reader = new FileReader();
                    reader.onload = e => {
                        const quill = this.$refs.editor.quill;
                        const range = quill.getSelection();
                        quill.insertEmbed(range.index, "image", e.target.result);
                    };
                    reader.readAsDataURL(file);
                }
            };
        },
        loadIssueType() {
            return this.$services.get('/lookup/ims_issusType').then((response) => {
                this.listIssueType = response.data.data
            })
        },
        loadPriority() {
            return this.$services.get('/lookup/ims_priority').then((response) => {
                this.listPriority = response.data.data
            })
        },
        loadIssueProject() {
            return this.$services.get('/lookup/ims_project').then((response) => {
                this.listIssueProject = response.data.data
            })
        },
        loadUser() {
            return this.$services.get('/lookup/user').then((response) => {
                this.listUser = response.data.data
            })
        },
        createIssue(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                if (success) {
                    console.log("Created issue:", this.createData);
                    this.createData.listFile = this.listFileSuccess
                    this.$services.post('/issue', this.createData)
                        .then((response) => {
                            this.$emit("success")
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t(`Success.Create`),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${this.$t(error.response.data.message)}`,
                                },
                            })
                        })
                }
            })

        }
    }
}
</script>
<style>
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
