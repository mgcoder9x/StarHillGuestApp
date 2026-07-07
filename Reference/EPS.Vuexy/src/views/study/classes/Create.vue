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
                                    v-slot="{ errors }"
                                    :rules="`required|validateCourseDates:${course.startTime}`"
                                    :name="'Ngày bắt đầu'"
                                >
                                    <b-form-datepicker
                                        v-model="classes.startDate"
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        reset-button
                                        type="datetime"
                                        :locale="currentLocale"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>

                        <b-col md="8">
                            <b-form-group
                                label="Ngày hết hiệu lực"
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    v-slot="{ errors }"
                                    :rules="`required|validateEndDate:${classes.startDate},${course.endTime}`"
                                    :name="'Ngày hết hiệu lực'"
                                >
                                    <b-form-datepicker
                                        v-model="classes.endDate"
                                        :date-format-options="{
                                            day: 'numeric',
                                            month: 'long',
                                            year: 'numeric',
                                        }"
                                        reset-button
                                        type="datetime"
                                        :locale="currentLocale"
                                    />
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
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="authorize(['ManageSTDClasses'])"
                                type="submit"
                                variant="primary"
                                class="mr-1"
                                @click.prevent="save"
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
                                :to="{ path: '/study/classes/List' }"
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
import TreeHelper from '@/utils/treeHelper'

export default {
    mixins: [authorizationMixin],
    components: { LatLngPicker },
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
                status: 1,
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
        }
    },
    computed: {
        currentLocale() {
            return this.$i18n.locale
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.loadCourse()
        this.loadLesson()
        this.classes.compId = accessToken.companyId
    },
    methods: {
        beforeCourseStartTime(value) {
            if (
                this.classes.startDate &&
                this.course.startTime &&
                new Date(this.classes.startDate) <
                    new Date(this.course.startTime)
            ) {
                return 'Vui lòng chọn thời gian lớp học sau thời gian bắt đầu của khóa học.'
            }
            return true
        },
        afterCourseEndTime(value) {
            if (
                this.classes.endDate &&
                this.course.endTime &&
                new Date(this.classes.endDate) > new Date(this.course.endTime)
            ) {
                return 'Vui lòng chọn thời gian lớp học trước thời gian kết thúc của khóa học.'
            }
            return true
        },
        validateCourseTimes() {
            if (!this.course.startTime || !this.course.endTime) {
                return 'Vui lòng chọn khóa học với thời gian bắt đầu và kết thúc.'
            }
            return true
        },
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
        save() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .post('/stdClasses', this.classes)
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
                                path: '/study/classes/List',
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
            })
        },
        loadCourse() {
            this.$services.get('/lookup/courses').then((response) => {
                this.lstCourse = response.data.data
            })
        },
        loadLesson() {
            this.$services.get('/lookup/lesson').then((response) => {
                this.lstLesson = response.data.data
            })
        },
    },
}
</script>

<style lang="scss"></style>
