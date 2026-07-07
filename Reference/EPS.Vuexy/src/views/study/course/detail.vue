<template>
    <validation-observer ref="observer">
        <b-card>
            <b-card-body>
                <b-form @submit.prevent="onSubmit">
                    <b-row cols="1" align-h="center">
                        <b-col cols="8">
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
                                                :disabled="!editing"

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
                                    <validation-provider
                                        rules="required"
                                        v-slot="{ errors }"
                                        name="Mã khóa học"
                                    >
                                        <b-form-group
                                            :label="$t('Mã khóa học')"
                                            label-class="required"
                                            label-cols-md="4"
                                        >
                                            <b-form-input
                                                v-model="course.code"
                                                trim
                                                :disabled="!editing"
                                            ></b-form-input>
                                            <small class="validate-massage">
                                                {{ errors[0] }}
                                            </small>
                                        </b-form-group>
                                    </validation-provider>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <validation-provider
                                        rules="required"
                                        v-slot="{ errors }"
                                        name="Tên khóa học"
                                    >
                                        <b-form-group
                                            :label="$t('Tên khóa học')"
                                            label-class="required"
                                            label-cols-md="4"
                                        >
                                            <b-form-input
                                                v-model="course.name"
                                                trim
                                                :disabled="!editing"
                                            ></b-form-input>
                                            <small class="validate-massage">
                                                {{ errors[0] }}
                                            </small>
                                        </b-form-group>
                                    </validation-provider>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <b-form-group
                                        :label="
                                            this.$t(
                                                'categories.groupAccesses.common.form.label.effectiveFrom'
                                            )
                                        "
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            :rules="`required|fromDate:${course.endTime}`"
                                            :name="
                                                this.$t(
                                                    'categories.groupAccesses.common.form.label.effectiveFrom'
                                                )
                                            "
                                        >
                                            <b-form-datepicker
                                                id="createForm-effectiveFrom"
                                                v-model="course.startTime"
                                                :date-format-options="{
                                                    day: 'numeric',
                                                    month: 'long',
                                                    year: 'numeric',
                                                }"
                                                reset-button
                                                type="datetime"
                                                :locale="currentLocale"
                                                :disabled="!editing"
                                            >
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
                                    <b-form-group
                                        :label="
                                            this.$t(
                                                'categories.groupAccesses.common.form.label.effectiveTo'
                                            )
                                        "
                                        label-class="required"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="
                                                this.$t(
                                                    'categories.groupAccesses.common.form.label.effectiveTo'
                                                )
                                            "
                                        >
                                            <b-form-datepicker
                                                id="createForm-effectiveTo"
                                                v-model="course.endTime"
                                                :date-format-options="{
                                                    day: 'numeric',
                                                    month: 'long',
                                                    year: 'numeric',
                                                }"
                                                reset-button
                                                type="datetime"
                                                :locale="currentLocale"
                                                :disabled="!editing"
                                            >
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
                                    <b-form-group
                                        :label="$t('Bài giảng')"
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="$t('Bài giảng')"
                                        >
                                            <v-select
                                                v-model="course.lessonId"
                                                :options="options.lesson"
                                                :reduce="
                                                    (item) => parseInt(item.id)
                                                "
                                                :multiple="true"
                                                label="text"
                                                :disabled="!editing"
                                                :placeholder="$t('Bài giảng')"
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <b-form-group
                                        :label="$t('Mô tả')"
                                        label-cols-md="4"
                                    >
                                        <b-form-textarea
                                            v-model="course.description"
                                            trim
                                            rows="3"
                                            max-rows="5"
                                            :disabled="!editing"
                                        ></b-form-textarea>
                                    </b-form-group>
                                </b-col>
                            </b-row>
                            <b-row>
                                <b-col>
                                    <b-form-group
                                        :label="this.$t('Trạng thái')"
                                        label-for="h-camera-area"
                                        label-cols-md="4"
                                        label-class="required"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            name="ClassStatus"
                                        >
                                            <b-form-select
                                                v-model="course.status"
                                                :options="lstStatus"
                                                :disabled="!editing"
                                            />
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
                            <Transition mode="out-in">
                                <button
                                    v-if="!editing"
                                    key="first"
                                    class="btn mx-50 mt-2 mb-50 btn-120 btn-primary"
                                    @click.prevent="editing = true"
                                >
                                    {{
                                        $t(
                                            'categories.waterWarning.Label.button.update'
                                        )
                                    }}
                                </button>
                                <button
                                    v-else
                                    key="second"
                                    class="btn mx-50 mt-2 mb-50 btn-120 btn-primary"
                                    @click.prevent="onSubmit()"
                                >
                                    {{
                                        $t(
                                            'categories.waterWarning.Label.button.save'
                                        )
                                    }}
                                </button>
                            </Transition>
                            <Transition mode="out-in">
                                <button
                                    v-if="!editing"
                                    key="first"
                                    class="btn btn-120 mb-50 btn-outline-secondary mt-2"
                                    @click.prevent="back()"
                                >
                                    {{
                                        $t(
                                            'categories.waterWarning.Label.button.back'
                                        )
                                    }}
                                </button>
                                <button
                                    v-else
                                    key="first"
                                    class="btn btn-120 mb-50 btn-outline-secondary mt-2"
                                    @click.prevent="cancel()"
                                >
                                    {{
                                        $t(
                                            'categories.waterWarning.Label.button.cancel'
                                        )
                                    }}
                                </button>
                            </Transition>
                        </b-col>
                    </b-row>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>
<script>
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    data() {
        return {
            editing: false,
            edited: false,
            options: {
                lesson: [],
            },
            course: {
                title: null,
                description: null,
                code: null,
                name: null,
                startTime: null,
                endTime: null,
                status: null,
            },
            lstStatus: [
                { value: 1, text: 'Hoạt động' },
                { value: 2, text: 'Ngưng hoạt động' },
            ],
        }
    },
    computed: {
        courseId() {
            debugger
            return this.$route.params.courseId
        },
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        currentLocale() {
            return this.$i18n.locale
        },
    },
    async created() {
        await this.loadOptions()
        await this.loadData()
    },
    methods: {
        async loadOptions() {
            this.loadLesson()
        },
        loadLesson() {
            this.$services.get('/lookup/lesson').then((response) => {
                this.options.lesson = response.data.data
            })
        },
        async onSubmit() {
            // validate
            const validate = await this.$refs.observer.validate()
            if (validate === false) {
                return
            }

            this.$services
                .put(`/stdCourses/${this.courseId}`, this.course)
                .then((res) => {
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
                    this.$router.push({ name: 'study-course-list' })
                })
                .catch((err) => {
                    // this.notification(false, err.response.data.message)
                    this.$toast({
                        component: ToastificationContent,
                        position: 'top-right',
                        props: {
                            title: this.$t('Error.Error'),
                            icon: 'AlertTriangleIcon',
                            variant: 'danger',
                            text: `${this.$t(err.message)}`,
                        },
                    })
                })
        },
        cancel() {
            this.editing = false
            this.loadData()
        },
        back() {
            this.$router.push({ name: 'study-course-list' })
        },
        loadData() {
            debugger
            this.$services.get(`/stdCourses/${this.courseId}`).then((res) => {
                this.course = res.data.data
            })
        },
        notification(isSuccess, message) {
            if (isSuccess) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'bottom-right',
                    props: {
                        title: this.$t(
                            'categories.waterWarning.Label.notification.success'
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
                        title: this.$t(
                            'categories.waterWarning.Label.notification.error'
                        ),
                        icon: 'CheckIcon',
                        variant: 'danger',
                        text: message,
                    },
                })
            }
        },
    },
}
</script>
<style>
.validate-massage {
    color: red;
}
</style>
