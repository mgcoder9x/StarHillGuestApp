<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row cols="1" align-h="center">
                        <b-col md="8">
                            <b-form-group
                                :label="this.$t('Mã lớp học')"
                                label-for="h-camera-code"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    name="ClassCode"
                                >
                                    <b-form-input
                                        id="h-camera-code"
                                        v-model="classes.code"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                :label="this.$t('Tên lớp học')"
                                label-for="h-camera-name"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="ClassName"
                                >
                                    <b-form-input
                                        id="h-camera-name"
                                        v-model="classes.name"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                :label="this.$t('Khóa học')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="ClassCourse"
                                >
                                    <b-form-select
                                        v-model="classes.courseId"
                                        :options="lstCourse"
                                        @change="changeCourseId($event)"
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                :label="this.$t('Bài giảng')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="ClassLesson"
                                >
                                    <v-select
                                        v-model="classes.listLessonId"
                                        :options="lstLesson"
                                        :reduce="(item) => parseInt(item.id)"
                                        :multiple="true"
                                        :disabled="!editing"
                                        label="text"
                                        :placeholder="$t('Bài giảng')"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                label="Ngày bắt đầu hiệu lực"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="`required|validateCourseDates:${course.startTime}`"
                                    :name="
                                        this.$t(
                                            'categories.groupAccesses.common.form.label.effectiveFrom'
                                        )
                                    "
                                >
                                    <b-form-datepicker
                                        id="createForm-effectiveFrom"
                                        v-model="classes.startDate"
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
                        <b-col md="8">
                            <b-form-group
                                label="Ngày hết hiệu lực"
                                label-class="required"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="`required|validateEndDate:${classes.startDate},${course.endTime}`"
                                    :name="
                                        this.$t(
                                            'categories.groupAccesses.common.form.label.effectiveTo'
                                        )
                                    "
                                >
                                    <b-form-datepicker
                                        id="createForm-effectiveTo"
                                        v-model="classes.endDate"
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
                        <b-col md="8">
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
                                        v-model="classes.status"
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
                    <b-row>
                        <b-col>
                            <div class="text-center">
                                <b-button
                                    v-if="
                                        editing &&
                                        authorize(['ManageSTDClasses'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="validationForm"
                                >
                                    {{ this.$t('Button.Save') }}
                                </b-button>
                                <b-button
                                    v-if="
                                        !editing &&
                                        authorize(['ManageSTDClasses'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="edit"
                                    >{{ this.$t('Button.Edit') }}</b-button
                                >
                                <b-button
                                    v-if="!editing"
                                    :to="{ path: '/study/classes/list' }"
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary"
                                >
                                    {{ this.$t('Button.Back') }}
                                </b-button>
                                <b-button
                                    v-if="editing"
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary"
                                    @click="cancel"
                                    >{{ this.$t('Button.Cancel') }}</b-button
                                >
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
import TreeHelper from '@/utils/treeHelper'
import getBaseUrl from '@/utils/get-baseUrl'

export default {
    mixins: [authorizationMixin],
    components: { LatLngPicker, LatLngPickerImage },
    data() {
        return {
            classes: {
                code: null,
                name: null,
                courseId: null,
                compId: null,
                listLessonId: null,
                startDate: null,
                endDate: null,
                status: null,
            },
            course: {
                startTime: null,
                endTime: null,
            },
            lstCourse: [],
            lstLesson: [],
            lstStatus: [
                { value: 1, text: 'Hoạt động' },
                { value: 2, text: 'Ngưng hoạt động' },
            ],
            editing: false,
            isShowMapImg: false,
        }
    },
    computed: {
        classId() {
            return this.$route.params.classId
        },
        currentLocale() {
            return this.$i18n.locale
        },
    },
    async created() {
        const accessToken = this.$services.getUserData()
        this.loadCourse()
        this.classes.compId = accessToken.companyId
        await this.loadClassesDetail()
    },
    methods: {
        async changeCourseId(event) {
            this.classes.listLessonId = null
            await this.$services
                .get(`/lookup/lessonByCourseId/${event}`)
                .then((response) => {
                    this.lstLesson = response.data.data
                })
            await this.$services
                .get(`/stdCourses/${event}`)
                .then((response) => {
                    debugger
                    this.course = response.data.data
                })
        },
        loadClassesDetail() {
            this.$services
                .get(`/stdClasses/${this.classId}`)
                .then((response) => {
                    this.classes = response.data.data
                    this.$services
                        .get(
                            `/lookup/lessonByCourseId/${this.classes.courseId}`
                        )
                        .then((response) => {
                            this.lstLesson = response.data.data
                        })
                    this.$services
                        .get(`/stdCourses/${this.classes.courseId}`)
                        .then((response) => {
                            this.course = response.data.data
                        })
                })
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .put(`/stdClasses/${this.classId}`, this.classes)
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
                            this.$router.push({ name: 'study-classes-list' })
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
            })
        },
        //lookup data
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
            this.loadClassesDetail()
        },
    },
}
</script>

<style lang="scss"></style>
