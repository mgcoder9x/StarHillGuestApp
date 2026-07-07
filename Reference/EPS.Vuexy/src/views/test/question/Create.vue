<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row cols="1" align-h="center">
                        <b-col md="8">
                            <b-form-group :label="this.$t('Nội dung câu hỏi')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="Nội dung câu hỏi">
                                    <b-form-input v-model="question.content" :state="errors.length > 0 ? false : null
                                        " />
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
                                    <b-form-select v-model="question.courseId" :options="lstCourse" />
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
                                    <b-form-select v-model="question.questionType" :options="lstQuestionType" />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group :label="this.$t('Cấp độ')" label-cols-md="4" label-class="required">
                                <validation-provider #default="{ errors }" rules="required" name="Cấp độ">
                                    <b-form-input type="number" v-model="question.level" :state="errors.length > 0 ? false : null
                                        " :min="0" @keydown="preventMinusKey" />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group :label="this.$t('Mô tả')" label-for="h-camera-name" label-cols-md="4">
                                <b-form-file id="file-input" ref="file-input" placeholder="File bài giảng"
                                    @change="onChange($event)"></b-form-file>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group label-cols-md="4">
                                <b-form-checkbox v-model="question.knockoutQuestion">
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
                                        <b-form-checkbox v-model="row.item.isCorrect" />
                                    </template>
                                    <template v-slot:cell(content)="row">
                                        <validation-provider #default="{ errors }" rules="required" name="Cấp độ">
                                            <b-form-input v-model="row.item.content"
                                                :state="errors.length > 0 ? false : null" />
                                            <small class="text-danger">{{ errors[0] }}</small>
                                        </validation-provider>
                                    </template>
                                    <template v-slot:cell(action)="row">
                                        <div class="text-nowrap">
                                            <b-button color="dark" @click="addAnswer()"
                                                class="mr-0 mr-sm-50 mb-50 mb-sm-0 btn-icon">+</b-button>
                                            <b-button v-if="question.lstAnswer.length > 1"
                                                @click.stop="removeAnswer(row.index)"
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
                        <b-col class="text-center">
                            <b-button v-if="authorize(['ManageSTDQuestion'])" type="submit" variant="primary"
                                class="mr-1" @click.prevent="save">
                                <Icon icon="material-symbols:save-outline" class="sm-icon" />
                                <span class="ml-50">
                                    {{ $t('common.button.save') }}
                                </span>
                            </b-button>
                            <b-button :to="{ path: '/test/question/List' }" type="reset" variant="outline-secondary">
                                <Icon icon="mdi:cancel" class="sm-icon" />
                                <span class="ml-50">
                                    {{ $t('common.button.cancel') }}
                                </span>
                            </b-button>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>

<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'

export default {
    mixins: [authorizationMixin],
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
            lstQuestionType: [
                { value: 1, text: 'Thông dụng' },
                { value: 2, text: 'Vận dụng' },
                { value: 3, text: 'Hiểu biết' },
            ],
            chunkSize: 5 * 1024 * 1024, // 5MB mỗi chunk
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    created() {
        this.loadCourse()
    },
    methods: {
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
            this.$services.post('/stdQuestion', this.question)
                .then((response) => {
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t('Success.Create'),
                            icon: 'CheckIcon',
                            variant: 'success',
                        },
                    })
                    this.$router.push({
                        path: '/test/question/list',
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
        async save() {
            this.$refs.rules.validate().then(async (success) => {
                if (success) {
                    if (this.question.file != null) {
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
                    else {
                        this.$services
                            .post('/stdQuestion', this.question)
                            .then((response) => {
                                this.$toast({
                                    component: ToastificationContent,
                                    position: 'top-right',
                                    props: {
                                        title: this.$t('Success.Create'),
                                        icon: 'CheckIcon',
                                        variant: 'success',
                                    },
                                })
                                this.$router.push({
                                    path: '/test/question/List',
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
    },
}
</script>

<style lang="scss"></style>
