<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row cols="1" align-h="center">
                        <b-col md="8">
                            <b-form-group :label="this.$t('Nội dung câu hỏi')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="Nội dung câu hỏi">
                                    <b-form-input v-model="question.content" :state="
                                            errors.length > 0 ? false : null
                                        " :disabled="!editing" />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <b-col md="8">
                            <b-form-group :label="this.$t('Khóa học')" label-for="h-camera-area" label-cols-md="4"
                                label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="Khóa học">
                                    <b-form-select v-model="question.courseId" :options="lstCourse"
                                        :disabled="!editing" />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group :label="this.$t('Loại câu hỏi')" label-for="h-camera-area" label-cols-md="4"
                                label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="Loại câu hỏi">
                                    <b-form-select v-model="question.questionType" :options="lstQuestionType"
                                        :disabled="!editing" />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group :label="this.$t('Cấp độ')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="Cấp độ">
                                    <b-form-input type="number" v-model="question.level" :state="
                                            errors.length > 0 ? false : null
                                        " :min="0" @keydown="preventMinusKey" :disabled="!editing" />
                                    <small class="text-danger">{{
                                        errors[0]
                                        }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group :label="this.$t('Mô tả')" label-for="h-camera-name" label-cols-md="4">
                                <b-form-file id="file-input" ref="file-input" placeholder="File bài giảng"
                                    :disabled="!editing" @change="onChange" v-if="editing" />
                                <b-form-input v-model="question.fileName" type="text" @click="navigateToPage()" v-if="!editing"
                                    readonly />
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group label-cols-md="4">
                                <b-form-checkbox v-model="question.knockoutQuestion" :disabled="!editing">
                                    Câu hỏi điểm liệt
                                </b-form-checkbox>
                            </b-form-group>
                        </b-col>
                        <b-col md="8" class="fieldset">
                            <div style="display: flex; justify-content: space-between">
                                <span style="font-weight: bold">{{
                                    this.$t("Câu trả lời")
                                    }}</span>
                            </div>
                            <div style="padding-top: 10px">
                                <b-table striped :items="question.lstAnswer" :fields="fieldsAnswer">
                                    <template v-slot:cell(isCorrect)="row">
                                        <b-form-checkbox v-model="row.item.isCorrect" :disabled="!editing" />
                                    </template>
                                    <template v-slot:cell(content)="row">
                                        <validation-provider #default="{ errors }" rules="required" name="Cấp độ">
                                            <b-form-input v-model="row.item.content"
                                                :state="errors.length > 0 ? false : null" :disabled="!editing" />
                                            <small class="text-danger">{{errors[0]}}</small>
                                        </validation-provider>
                                    </template>
                                    <template v-slot:cell(action)="row">
                                        <div class="text-nowrap">
                                            <b-button color="dark" @click="addAnswer()" :disabled="!editing"
                                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon">+</b-button>
                                            <b-button v-if="question.lstAnswer.length > 1"
                                                @click.stop="removeAnswer(row.index)" :disabled="!editing"
                                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon">
                                                <Icon icon="fluent:delete-20-regular" class="xs-icon" />
                                            </b-button>
                                        </div>
                                    </template>
                                </b-table>
                            </div>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col>
                            <div class="text-center">
                                <b-button v-if="
                                        editing &&
                                        authorize(['ManageSTDQuestion'])
                                    " type="button" variant="primary" class="mx-50 mb-50 btn-120"
                                    @click="validationForm">
                                    {{ this.$t('Button.Save') }}
                                </b-button>
                                <b-button v-if="
                                        !editing &&
                                        authorize(['ManageSTDQuestion'])
                                    " type="button" variant="primary" class="mx-50 mb-50 btn-120" @click="edit">{{
                                    this.$t('Button.Edit') }}</b-button>
                                <b-button v-if="!editing" :to="{ path: '/test/question/list' }" type="button"
                                    class="mx-50 mb-50 btn-120" variant="outline-secondary">
                                    {{ this.$t('Button.Back') }}
                                </b-button>
                                <b-button v-if="editing" type="button" class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary" @click="cancel">{{ this.$t('Button.Cancel')
                                    }}</b-button>
                            </div>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import LatLngPicker from '@/components/LatLngPicker'
import LatLngPickerImage from '@/components/LatLngPickerImage'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'

export default {
    mixins: [authorizationMixin],
    components: { LatLngPicker, LatLngPickerImage },
    data() {
        return {
            question: {
                content: null,
                courseId: null,
                questionType: null,
                level: null,
                knockoutQuestion: false,
                file: null,
                lstAnswer: [
                    {
                        content: null,
                        isCorrect: false,
                    },
                ]
            },
            lstQuestionType: [
                { value: 1, text: 'Thông dụng' },
                { value: 2, text: 'Vận dụng' },
                { value: 3, text: 'Hiểu biết' },
            ],
            fieldsAnswer: [
                {
                    key: "isCorrect",
                    label: this.$t("Đáp án đúng"),
                },
                {
                    key: "content",
                    label: this.$t("Nội dung đáp án"),
                },
                {
                    key: "action",
                    label: this.$t("Thao tác"),
                },
            ],
            lstCourse: [],
            editing: false,
            chunkSize: 5 * 1024 * 1024, // 5MB mỗi chunk
        }
    },
    computed: {
        questionId() {
            return this.$route.params.questionId
        },
        currentLocale() {
            return this.$i18n.locale
        },
    },
    async created() {
        this.loadCourse()
        await this.loadQuestionDetail()
    },
    methods: {
        navigateToPage(){
            window.open(this.question.url, "_blank");
        },
        onChange(evt) {
            const file = evt.target.files[0]
            if (!file) return
            this.question.file = file
            // Kiểm tra nếu là video thì tính thời lượng
            if (file.type.startsWith('video/')) {
                const video = document.createElement('video')
                video.preload = 'metadata'

                video.src = URL.createObjectURL(file)
            }
        },
        async uploadInChunks(file) {
            const totalChunks = Math.ceil(file.size / this.chunkSize)
            const fileName = file.name

            for (let i = 0; i < totalChunks; i++) {
                const start = i * this.chunkSize
                const end = Math.min(start + this.chunkSize, file.size)
                const chunk = file.slice(start, end)

                const formData = new FormData()
                formData.append('file', chunk)
                formData.append('chunkIndex', i)
                formData.append('totalChunks', totalChunks)
                formData.append('fileName', fileName)

                await this.$services.post('/stdQuestion/upload-chunk', formData, {
                    headers: { 'Content-Type': 'multipart/form-data' },
                })
            }

            // Sau khi upload xong, gọi API merge file
            var a = await this.$services.post('/stdQuestion/merge-file', {
                fileName,
                totalChunks,
            })
            // Xong lưu thông tin
            this.question.mediaUrl = a.data.filename
            this.$services.put(`/stdQuestion/${this.questionId}`, this.question)
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
                    this.cancel()
                })
                .catch((error) => {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t('Error.Error'),
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                            text: `${this.$t(error.message)}`,
                        },
                    })
                })
        },
        addAnswer() {
            let item = {
                content: null,
                isCorrect: false,
            };
            var cloneItem = JSON.parse(JSON.stringify(item));
            this.question.lstAnswer.push(cloneItem);
        },
        removeAnswer(index) {
            this.question.lstAnswer.splice(index, 1);
        },
        preventMinusKey(event) {
            if (event.key === '-' || event.key === 'e' || event.key === '.') {
                event.preventDefault()
            }
        },
        loadQuestionDetail() {
            this.$services
                .get(`/stdQuestion/${this.questionId}`)
                .then((response) => {
                    this.question = response.data.data
                    this.question.fileName = response.data.data.mediaUrl
                })
        },
        async validationForm() {
            var vm = this
            this.$refs.rules.validate().then(async (success) => {
                debugger
                if (success) {
                    if (vm.question.file != null) {
                        try {
                            await this.uploadInChunks(this.question.file)
                        } catch (error) {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: error,
                                },
                            })
                        } finally {
                            this.isUploading = false
                        }
                    }
                    else{
                        this.$services.put(`/stdQuestion/${this.questionId}`, this.question)
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
                                this.cancel()
                                this.$router.push({ name: 'test-question-list' })
                            })
                            .catch((error) => {
                                this.$toast({
                                    component: ToastificationContent,
                                    position: 'top-right',
                                    props: {
                                        title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${this.$t(error.message)}`,
                                },
                            })
                        })
                    }
                    
                }
            })
        },
        loadCourse() {
            this.$services.get('/lookup/courses').then((response) => {
                this.lstCourse = response.data.data
            })
        },
        edit() {
            this.editing = true
        },
        cancel() {
            this.editing = false
            this.loadQuestionDetail()
        },
    },
}
</script>

<style lang="scss"></style>
