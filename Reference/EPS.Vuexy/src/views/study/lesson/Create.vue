<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row cols="1" align-h="center">
                        <b-col md="8">
                            <b-form-group
                                :label="this.$t('Tiêu đề')"
                                label-for="h-camera-code"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="LessonTitle"
                                >
                                    <b-form-input
                                        id="h-camera-code"
                                        v-model="lesson.title"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                :label="this.$t('Mô tả')"
                                label-for="h-camera-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="LessonDescription"
                                >
                                    <b-form-input
                                        id="h-camera-name"
                                        v-model="lesson.description"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                :label="this.$t('Cấp độ')"
                                label-for="h-camera-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="LessonLevel"
                                >
                                    <b-form-input
                                        id="h-camera-name"
                                        type="number"
                                        v-model="lesson.level"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :min="0"
                                        @keydown="preventMinusKey"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                :label="this.$t('Số phút yêu cầu')"
                                label-for="h-camera-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="LessonRequireMinute"
                                >
                                    <b-form-input
                                        id="h-camera-name"
                                        type="number"
                                        v-model="lesson.requireMinute"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :min="0"
                                        @keydown="preventMinusKey"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                :label="
                                    this.$t('Phần trăm cần để hoàn thành(%)')
                                "
                                label-for="h-camera-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="LessonPercent"
                                >
                                    <b-form-input
                                        id="h-camera-name"
                                        type="number"
                                        v-model="lesson.percent"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :min="0"
                                        @keydown="preventMinusKey"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                :label="this.$t('Nội dung')"
                                label-for="h-camera-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.employees.import.importRequire'
                                        )
                                    "
                                >
                                    <b-form-file
                                        id="file-input"
                                        ref="file-input"
                                        placeholder="File bài giảng"
                                        @change="onChange($event)"
                                    ></b-form-file>
                                    <b-form-input
                                        v-model="lesson.file"
                                        type="text"
                                        style="display: none"
                                        :state="errors.length ? false : null"
                                    >
                                    </b-form-input>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <!-- <b-col md="8">
                            <b-form-group
                                :label="
                                    this.$t('Thời gian của bài học(phút)')
                                "
                                label-for="h-camera-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="LessonTimeStudy"
                                >
                                    <b-form-input
                                        id="h-camera-name"
                                        type="number"
                                        v-model="lesson.timeStudy"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :min="0"
                                        @keydown="preventMinusKey"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col> -->
                        <b-col md="8">
                            <b-form-group
                                :label="this.$t('Thời gian nhận diện(s)')"
                                label-for="h-camera-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="LessonConfigTime"
                                >
                                    <b-form-input
                                        id="h-camera-name"
                                        type="number"
                                        v-model="lesson.configTime"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :min="0"
                                        @keydown="preventMinusKey"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <!-- <b-progress :value="uploadProgress" max="100" animated show-progress /> -->
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="authorize(['ManageSTDLesson'])"
                                type="submit"
                                variant="primary"
                                class="mr-1"
                                @click.prevent="validationForm"
                            >
                                <Icon
                                    icon="material-symbols:save-outline"
                                    class="sm-icon"
                                />
                                <span class="ml-50">
                                    {{ $t('common.button.save') }}
                                </span>
                            </b-button>
                            <b-button
                                :to="{ path: '/study/lesson/list' }"
                                type="reset"
                                variant="outline-secondary"
                            >
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
import LatLngPicker from '@/components/LatLngPicker'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import { v4 as uuidv4 } from 'uuid'

export default {
    mixins: [authorizationMixin],
    components: { LatLngPicker },
    data() {
        return {
            lesson: {
                title: null,
                description: null,
                contentUrl: null,
                compId: null,
                level: null,
                fileData: null,
                fileName: null,
                endFile: null,
                file: null,
                timeStudy: null,
                percent: null,
                configTime: null,
            },
            chunkSize: 5 * 1024 * 1024, // 5MB mỗi chunk
        }
    },
    computed: {},
    created() {
        const accessToken = this.$services.getUserData()
        this.lesson.compId = accessToken.companyId
    },
    methods: {
        preventMinusKey(event) {
            if (event.key === '-' || event.key === 'e' || event.key === '.') {
                event.preventDefault()
            }
        },
        async validationForm() {
            this.$refs.rules.validate().then(async (success) => {
                if (success) {
                    if (!this.lesson.file) {
                        this.$toast({
                            component: ToastificationContent,
                            position: 'top-right',
                            props: {
                                title: this.$t('Error.Error'),
                                icon: 'AlertTriangleIcon',
                                variant: 'danger',
                                text: this.$t('Vui lòng chọn file!'),
                            },
                        })
                        return
                    }

                    this.isUploading = true
                    this.uploadProgress = 0

                    try {
                        await this.uploadInChunks(this.lesson.file)
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
            })
        },
        onChange(evt) {
            const file = evt.target.files[0]
            if (!file) return
            this.lesson.file = file
            // Kiểm tra nếu là video thì tính thời lượng
            if (file.type.startsWith('video/')) {
                const video = document.createElement('video')
                video.preload = 'metadata'

                video.onloadedmetadata = () => {
                    window.URL.revokeObjectURL(video.src) // Giải phóng bộ nhớ
                    this.lesson.timeStudy = video.duration // Lưu thời lượng vào lesson
                }

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

                await this.$services.post('/stdLesson/upload-chunk', formData, {
                    headers: { 'Content-Type': 'multipart/form-data' },
                })
            }

            // Sau khi upload xong, gọi API merge file
            var a = await this.$services.post('/stdLesson/merge-file', {
                fileName,
                totalChunks,
            })
            // Xong lưu thông tin
            this.lesson.contentUrl = a.data.filename
            //this.lesson.percent = 80 //Phải học xong 80% mới được học bài tiếp
            this.$services.post('/stdLesson', this.lesson)
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
                        path: '/study/lesson/list',
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
    },
}
</script>

<style lang="scss"></style>
