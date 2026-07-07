<!-- eslint-disable -->
<template>
    <validation-observer ref="observer">
        <b-card>
            <b-card-body>
                <b-form @submit.prevent="onSubmit">
                    <b-row>
                        <b-col md="3">
                            <b-img v-if="course.path" :src="course.path" class="mb-1 avatar-img" alt="Avatar" />
                            <b-form-file :placeholder="
                                    $t(
                                        'study.student.common.form.label.avatarPath'
                                    )
                                " drop-placeholder="Kéo file vào đây" @change="handleFileUpload" />
                        </b-col>
                        <b-col cols="9">
                            <!-- <b-row>
                                <b-col>
                                    <validation-provider
                                        rules="required"
                                        v-slot="{ errors }"
                                        name="Tiêu đề"
                                    >
                                        <b-form-group
                                            :label="
                                                $t(
                                                    'Tiêu đề'
                                                )
                                            "
                                            label-class="required"
                                            label-cols-md="4"
                                        >
                                            <b-form-input
                                                v-model="course.title"
                                                 trim

                                            ></b-form-input>
                                            <small class="validate-massage">
                                                {{ errors[0] }}
                                            </small>
                                        </b-form-group>
                                    </validation-provider>
                                </b-col>
                            </b-row> -->
                            <b-row>
                                <b-col>
                                    <validation-provider rules="required" v-slot="{ errors }" name="Mã khóa học">
                                        <b-form-group :label="
                                                $t(
                                                    'Mã khóa học'
                                                )
                                            " label-class="required" label-cols-md="4">
                                            <b-form-input v-model="course.code" trim></b-form-input>
                                            <small class="validate-massage">
                                                {{ errors[0] }}
                                            </small>
                                        </b-form-group>
                                    </validation-provider>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <validation-provider rules="required" v-slot="{ errors }" name="Tên khóa học">
                                        <b-form-group :label="
                                                $t(
                                                    'Tên khóa học'
                                                )
                                            " label-class="required" label-cols-md="4">
                                            <b-form-input v-model="course.name" trim></b-form-input>
                                            <small class="validate-massage">
                                                {{ errors[0] }}
                                            </small>
                                        </b-form-group>
                                    </validation-provider>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <b-form-group :label="
                                    this.$t(
                                        'categories.groupAccesses.common.form.label.effectiveFrom'
                                    )
                                " label-cols-md="4" label-class="required" :class="formGroupClass">
                                        <validation-provider #default="{ errors }" :rules="
                                        `required|fromDate:${course.endTime}` 

                                    " :name="
                                        this.$t(
                                            'categories.groupAccesses.common.form.label.effectiveFrom'
                                        )
                                    ">
                                            <b-form-datepicker id="createForm-effectiveFrom" v-model="course.startTime"
                                                :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }" reset-button type="datetime" :locale="currentLocale">
                                            </b-form-datepicker>
                                            <small class="text-danger">{{
                                                errors[0]
                                                }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <b-form-group :label="
                                    this.$t(
                                        'categories.groupAccesses.common.form.label.effectiveTo'
                                    )
                                " label-class="required" label-cols-md="4" :class="formGroupClass">
                                        <validation-provider #default="{ errors }" rules="required" :name="
                                        this.$t(
                                            'categories.groupAccesses.common.form.label.effectiveTo'
                                        )
                                    ">
                                            <b-form-datepicker id="createForm-effectiveTo" v-model="course.endTime"
                                                :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }" reset-button type="datetime" :locale="currentLocale">
                                            </b-form-datepicker>
                                            <small class="text-danger">{{
                                                errors[0]
                                                }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <b-form-group :label="
                                    $t(
                                        'Bài giảng'
                                    )
                                " label-cols-md="4" label-class="required" :class="formGroupClass">
                                        <validation-provider #default="{ errors }" rules="required" :name="
                                        $t(
                                            'Bài giảng'
                                        )
                                    ">
                                            <v-select v-model="course.lessonId" :options="options.lesson"
                                                :reduce="(item) => parseInt(item.id)" :multiple="true" label="text"
                                                :placeholder="
                                            $t(
                                                'Bài giảng'
                                            )
                                        " />
                                            <small class="text-danger">{{
                                                errors[0]
                                                }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <b-form-group :label="
                                            $t(
                                                'Mô tả'
                                            )
                                        " label-cols-md="4">
                                        <b-form-textarea trim v-model="course.description" rows="3"
                                            max-rows="5"></b-form-textarea>
                                    </b-form-group>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <b-form-group :label="this.$t('Trạng thái')" label-cols-md="4"
                                        label-class="required">
                                        <validation-provider #default="{ errors }" rules="required" name="ClassStatus">
                                            <b-form-select v-model="course.status" :options="lstStatus" />
                                            <small class="text-danger">{{
                                                errors[0]
                                                }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>
                            </b-row>
                        </b-col>
                    </b-row>
                    <b-row class="justify-conten-center">
                        <b-col cols="12" class="text-center">
                            <button class="btn mx-50 mt-2 mb-50 btn-120 btn-primary"
                                v-if="authorize(['ManageSTDCourse'])" click="submit">
                                {{ $t('common.button.save') }}

                            </button>
                            <button class="btn btn-120 mb-50 btn-outline-secondary mt-2" @click="back()">
                                {{ $t('common.button.cancel') }}
                            </button>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>
<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

/*eslint-disable*/
export default {
    mixins: [authorizationMixin],
    data() {
        return {
            options: {
                lesson: [],
            },
            course: {
               // title: null,
                description: null,
                code: null,
                name: null,
                startTime: null,
                endTime: null,
                path: null,
                status: 1
            },
            lstStatus: [
                { value: 1, text: "Hoạt động"},
                { value: 2, text: "Ngưng hoạt động"},
            ]
        }
    },
       computed: {
         formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        currentLocale() {
            return this.$i18n.locale
        },
    },
    async created() {
        await this.loadOptions()
    },
    methods: {
        handleFileUpload(event) {
            debugger
            const file = event.target.files[0]
            this.course.path = event.target.files[0]
            if (file) {
                const reader = new FileReader()
                reader.onload = (e) => {
                    const base64String = e.target.result
                    if (!base64String.startsWith('data:')) {
                        const mimeType = file.type || 'image/png'
                        this.course.path = `data:${mimeType};base64,${base64String}`
                    } else {
                        this.course.path = base64String
                    }
                }
                reader.onerror = () => {
                    this.errors.push('Không thể đọc file, vui lòng thử lại.')
                }
                reader.readAsDataURL(file)
            } else {
                this.course.path = ''
            }
        },
        async loadOptions() {
            this.loadLesson()
           
        },
          loadLesson() {
            this.$services.get('/lookup/lesson').then((response) => {
                this.options.lesson = response.data.data
            })
        },
        async onSubmit() {
            //validate
            let validate = await this.$refs.observer.validate()
            if (validate === false) {
                return
            }

            this.$services
                .post('/stdCourses', this.course)
                .then((response) => {
                    this.notification(true)
                    this.$router.push({ name: 'study-course-list' })
                })
                .catch((error) => {
                    this.notification(false, error.message)
                })
        },

        notification(isSuccess, message) {
            if (isSuccess) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'bottom-right',
                    props: {
                        title: this.$t(
                            'Success.Create'
                        ),
                        icon: 'CheckIcon',
                        variant: 'success',
                    },
                })
            } else {
                this.$toast({
                    component: ToastificationContent,
                    position: 'bottom-right',
                    props: {
                        title: 'ERROR',
                        icon: 'CheckIcon',
                        variant: 'danger',
                        text: this.$t(message),
                    },
                })
            }
        },

        back() {
            this.$router.push({ name: 'study-course-list' })
        },
    },
}
</script>
<style>
.validate-massage {
    color: red;
}
.avatar-img {
    max-width: 100%;
    object-fit: contain;
}
</style>
