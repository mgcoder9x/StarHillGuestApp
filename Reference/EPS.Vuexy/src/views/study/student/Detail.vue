<!-- eslint-disable -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row>
                        <!-- Start: input Img -->
                        <b-col md="3" style="text-align: center;">
                            <b-img v-if="viewAvatar" :src="viewAvatar" class="mb-1 avatar-img" alt="Avatar" />
                            <div 
                                v-else
                                class="mb-1 avatar-placeholder w-100 h-100 d-flex align-items-center justify-content-center"
                            >
                                <span class="text-muted">Chưa có ảnh</span>
                            </div>
                            <b-form-file placeholder="" drop-placeholder="Kéo file vào đây" @change="handleFileUpload"
                                :disabled="!isEdit" />
                        </b-col>
                        <!-- End: input Img -->
                        <!-- Start: input Data -->
                        <b-col md="9">
                            <b-row>
                                <b-col md="6">
                                    <b-form-group :label="$t(
                                        'study.student.common.form.label.code'
                                    )
                                        " label-for="h-code" label-cols-md="4" label-class="required"
                                        :class="formGroupClass">
                                        <validation-provider #default="{ errors }" rules="required"
                                            :name="$t('study.student.common.form.label.code')">
                                            <b-form-input id="h-code" v-model="updateStudent.code" :disabled="!isEdit"
                                                v-if="isEdit"
                                                :placeholder="$t('study.student.common.form.placeholder.code')" />
                                            <label v-if="!isEdit" class="col-form-label ">{{ updateStudent.code
                                                }}</label>
                                            <small class="text-danger">{{
                                                errors[0]
                                                }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group :label="$t(
                                        'study.student.common.form.label.citizenId'
                                    )
                                        " label-for="h-citizenId" label-cols-md="4" label-class="required"
                                        :class="formGroupClass">
                                        <validation-provider #default="{ errors }" :rules="{
                                            required, regex: /^[0-9]{12}$/
                                        }" :name="$t(
                                            'study.student.common.form.label.citizenId'
                                        )
                                            ">
                                            <b-form-input id="h-citizenId" v-model="updateStudent.identityCard"
                                                :disabled="!isEdit" :placeholder="$t(
                                                    'study.student.common.form.placeholder.citizenId'
                                                )
                                                    " :state="errors.length > 0
                                                        ? false
                                                        : null
                                                        " v-if="isEdit"/>
                                            <label v-if="!isEdit" class="col-form-label ">{{ updateStudent.identityCard
                                                }}</label>
                                            <small class="text-danger">{{
                                                errors[0]
                                                }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group :label="$t(
                                        'study.student.common.form.label.fullname'
                                    )
                                        " label-for="h-fullname" label-cols-md="4" label-class="required"
                                        :class="formGroupClass">
                                        <validation-provider #default="{ errors }" rules="required" :name="$t(
                                            'study.student.common.form.label.fullname'
                                        )
                                            ">
                                            <b-form-input id="h-fullname" :disabled="!isEdit"
                                                v-model="updateStudent.fullName" :placeholder="$t(
                                                    'study.student.common.form.placeholder.fullname'
                                                )
                                                    " :state="errors.length ? false : null
                                                        " v-if="isEdit"/>
                                            <label v-if="!isEdit" class="col-form-label ">{{ updateStudent.fullName
                                                }}</label>
                                            <small class="text-danger">{{
                                                errors[0]
                                                }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>
                                <b-col md="6">
                                        <validation-provider
                                    v-slot="{ errors }"
                                    name="Ngày sinh"
                                    :rules="{
                                        futureDate
                                    }"
                                >
                                    <b-form-group
                                        :label="$t('categories.employees.common.form.label.birthday')"
                                        label-for="h-birthday"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <b-form-datepicker
                                            id="h-birthday"
                                            v-model="updateStudent.birthday"
                                            :placeholder="$t('categories.employees.common.form.placeholder.birthday')"
                                            :date-format-options="{
                                                day: 'numeric',
                                                month: 'long',
                                                year: 'numeric',
                                            }"
                                                :locale="currentLocale"
                                                :disabled="!isEdit"

                                        />
                                        <small class="text-danger">{{ errors[0] }}</small>
                                    </b-form-group>
                                </validation-provider>
                                </b-col>
                    

                                <b-col md="6">
                                    <b-form-group :label="$t(
                                        'study.student.common.form.label.gender'
                                    )
                                        " label-for="h-gender" label-cols-md="4" :class="formGroupClass">
                                        <v-select id="h-gender" v-model="updateStudent.gender" :disabled="!isEdit"
                                            :options="options.gender" :reduce="(option) => option.value" :placeholder="$t(
                                                'study.student.common.form.placeholder.gender'
                                            )
                                                " />
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                <validation-provider
                                    v-slot="{ errors }"
                                    name="Số điện thoại"
                                    rules="phone"
                                >
                                    <b-form-group
                                        :label="$t('categories.employees.common.form.label.phoneNumber')"
                                        label-for="h-phoneNumber"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <b-form-input
                                            id="h-phoneNumber"
                                            v-model="updateStudent.phone"
                                            trim
                                            :placeholder="$t('categories.employees.common.form.placeholder.phoneNumber')"
                                            :disabled="!isEdit"
                                        />
                                        <small class="text-danger">{{ errors[0] }}</small>
                                                                                 

                                    </b-form-group>
                                </validation-provider>
                            </b-col>
                                <b-col md="6">
                                <validation-provider
                                    v-slot="{ errors }"
                                    name="Email"
                                    rules="email"
                                >
                                    <b-form-group
                                        :label="$t('categories.employees.common.form.label.email')"
                                        label-for="h-email"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <b-form-input
                                            id="h-email"
                                            v-model="updateStudent.email"
                                            trim
                                            :placeholder="$t('categories.employees.common.form.placeholder.email')"
                                                                                        :disabled="!isEdit"

                                        />
                                        <small class="text-danger">{{ errors[0] }}</small>

                                    </b-form-group>
                                </validation-provider>
                            </b-col>

                                <b-col md="6">
                                    <b-form-group :label="$t(
                                        'Lớp học'
                                    )
                                        " label-cols-md="4" label-class="required" :class="formGroupClass">
                                        <validation-provider #default="{ errors }" rules="required" :name="$t(
                                            'Lớp học'
                                        )
                                            ">
                                            <v-select v-model="updateStudent.classdMulId" :options="options.classes"
                                                :reduce="(item) => parseInt(item.id)" :multiple="true"
                                                :disabled="!isEdit" label="text" :placeholder="$t(
                                                    'Lớp học'
                                                )
                                                    " />
                                            <small class="text-danger">{{
                                                errors[0]
                                                }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>
                                
                            <b-col md="6">
                                    <b-form-group
                                        :label="this.$t('Trạng thái')"
                                        label-cols-md="4"
                                        label-class="required"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            name="ClassStatus"
                                        >
                                            <b-form-select
                                                v-model="updateStudent.status"
                                                :options="options.status"
                                                :disabled="!isEdit"
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>
                            </b-row>
                        </b-col>
                        <!-- End: input Data -->
                    </b-row>
                    <!-- Button Action -->
                    <div class="text-center">
                        <b-button v-if="!isEdit" type="button" variant="primary" class="mx-50 mb-50 btn-120"
                            @click.prevent="isEdit = true">
                            {{ $t('common.button.edit') }}
                        </b-button>
                        <b-button v-if="authorize(['ManageSTDUsers']) && isEdit" type="submit" variant="primary"
                            title="Save" class="mx-50 mb-50 btn-120">
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button @click="navigateToList()" type="reset" variant="outline-secondary" title="Cancel"
                            class="btn-120 mb-50">
                            {{ $t('common.button.cancel') }}
                        </b-button>
                    </div>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>
<script>
/*eslint-disable*/

import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import TreeHelper from '@/utils/treeHelper'
import getBaseUrl from '@/utils/get-baseUrl'

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            options: {

                classes: [],
                compTree: [],
                groups: [],
                departmentTree: [],
                areaTree: null,
                gender: [
                    { value: 0, label: 'Nữ' },
                    { value: 1, label: 'Nam' },
                ],
                status: [
                    { value: 1, text: 'Đang hoạt động' },
                    { value: 2, text: 'Ngừng hoạt động' },
                ],
            },
            updateStudent: {
                compId: null,
                depId: null,
                groupId: null,
                code: null,
                fullName: null,
                jobDuties: null,
                position: null,
                birthday: null,
                gender: null,
                phoneNumber: null,
                email: null,
                avatarBase64: null,
                citizenId: null,
            },
            viewAvatar: null,
            isEdit: false,
        }
    },
    computed: {
        studentId() {
            return this.$route.params.studentId
        },
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        imgUrl() {
            const { VUE_APP_BASE_URL: baseURL } = process.env
            return `${getBaseUrl()}/STDUserss`
        },
         currentLocale() {
            return this.$i18n ? this.$i18n.locale : "vi"; // Nếu $i18n null, mặc định 'vi'
        },
    },
    
    async created() {
        await this.loadOptions()
        await this.getData()
    },
    methods: {
        async getData() {
            debugger
            try {
                var vm = this
                await this.$services.get(`/stdUser/${this.studentId}`).then((response) => {
                    vm.updateStudent = response.data.data
                    vm.viewAvatar = this.imgUrl + '/' + this.updateStudent.avartarPath
                })
            } catch (error) {
                console.log(error)
            }
        },
        handleFileUpload(event) {
            const file = event.target.files[0]
            this.updateStudent.avatarBase64 = event.target.files[0]
            if (file) {
                const reader = new FileReader()
                reader.onload = (e) => {
                    const base64String = e.target.result
                    if (!base64String.startsWith('data:')) {
                        const mimeType = file.type || 'image/png'
                        this.updateStudent.avatarBase64 = `data:${mimeType};base64,${base64String}`
                    } else {
                        this.updateStudent.avatarBase64 = base64String
                    }
                    this.viewAvatar = this.updateStudent.avatarBase64
                }
                reader.onerror = () => {
                    this.errors.push('Không thể đọc file, vui lòng thử lại.')
                }
                reader.readAsDataURL(file)
            } else {
                this.updateStudent.avatarBase64 = ''
            }
        },
        async loadOptions() {
            this.$services.get('/lookup/classes').then((response) => {
                this.options.classes = response.data.data
            })
            this.$services.get('/lookup/company-tree').then((response) => {
                this.options.compTree = TreeHelper.removeEmptyChildren(
                    response.data
                )
            })
            this.$services.get('/lookup/groups').then((response) => {
                this.options.groups = response.data.data
            })
            this.$services.get('/lookup/departments-tree?type=1').then((response) => {
                this.options.departmentTree = response.data.data
            })
            const response = await this.$services.get('/lookup/areas-tree')
            this.options.areaTree = TreeHelper.removeEmptyChildren(
                response.data.data
            )
        },
        async onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                if (success) {
                    try {
                        const res = await this.$services.put(
                            '/stdUser/' + this.studentId,
                            this.updateStudent
                        )
                        this.showSuccessToast(res.data)
                        this.isEdit = false
                        this.navigateToList()
                    } catch (error) {
                        this.showErrorToast(error)
                    }
                }
            })
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        `Success.Create`
                    ),
                    icon: 'CheckIcon',
                    variant: 'success',
                },
            })
        },
        showErrorToast(error) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: 'Error',
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(error.message),
                },
            })
        },
        navigateToList() {
            if (this.isEdit) {
                this.isEdit = false
                this.getData()
            } else {
                this.$router.push({ path: '/study/student/list' })
            }
        },
    },
}
</script>

<style lang="scss" scoped>
.avatar-img {
    max-width: 100%;
    object-fit: contain;
}
.avatar-placeholder {
    border: 2px dashed #ccc;
    min-height: 150px;
}
</style>
