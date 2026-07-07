<!-- eslint-disable -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row>
                        <!-- Start: input Img -->
                        <b-col md="3" style="text-align: center">
                            <b-img
                                v-if="newStudent.avatarBase64"
                                :src="newStudent.avatarBase64"
                                class="mb-1 avatar-img"
                                alt="Avatar"
                            />
                            <div 
                                v-else
                                class="mb-1 avatar-placeholder w-100 h-100 d-flex align-items-center justify-content-center"
                            >
                                <span class="text-muted">Chưa có ảnh</span>
                            </div>
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
                                    :placeholder="
                                        $t(
                                            'study.student.common.form.label.avatarPath'
                                        )
                                    "
                                    drop-placeholder="Kéo file vào đây"
                                    @change="handleFileUpload"
                                    @drop.prevent="handleFileUpload"
                                />
                                <b-form-input
                                    v-model="newStudent.avatarBase64"
                                    type="text"
                                    style="display: none"
                                    :state="errors.length ? false : null"
                                >
                                </b-form-input>
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>
                            </validation-provider>
                        </b-col>
                        <!-- End: input Img -->
                        <!-- Start: input Data -->
                        <b-col md="9">
                            <b-row>
                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'study.student.common.form.label.code'
                                            )
                                        "
                                        label-for="h-code"
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="
                                                $t(
                                                    'study.student.common.form.label.code'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-code"
                                                trim
                                                v-model="newStudent.code"
                                                :placeholder="
                                                    $t(
                                                        'study.student.common.form.placeholder.code'
                                                    )
                                                "
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'study.student.common.form.label.citizenId'
                                            )
                                        "
                                        label-for="h-citizenId"
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            :rules="{
                                                required,
                                                regex: /^[0-9]{12}$/,
                                            }"
                                            :name="
                                                $t(
                                                    'study.student.common.form.label.citizenId'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-citizenId"
                                                v-model="
                                                    newStudent.identityCard
                                                "
                                                trim
                                                :placeholder="
                                                    $t(
                                                        'study.student.common.form.placeholder.citizenId'
                                                    )
                                                "
                                                :state="
                                                    errors.length > 0
                                                        ? false
                                                        : null
                                                "
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'study.student.common.form.label.fullname'
                                            )
                                        "
                                        label-for="h-fullname"
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="
                                                $t(
                                                    'study.student.common.form.label.fullname'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-fullname"
                                                trim
                                                v-model="newStudent.fullname"
                                                :placeholder="
                                                    $t(
                                                        'study.student.common.form.placeholder.fullname'
                                                    )
                                                "
                                                :state="
                                                    errors.length ? false : null
                                                "
                                            />
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
                                            futureDate,
                                        }"
                                    >
                                        <b-form-group
                                            :label="
                                                $t(
                                                    'categories.employees.common.form.label.birthday'
                                                )
                                            "
                                            label-for="h-birthday"
                                            label-cols-md="4"
                                            :class="formGroupClass"
                                        >
                                            <b-form-datepicker
                                                id="h-birthday"
                                                v-model="newStudent.birthday"
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.birthday'
                                                    )
                                                "
                                                :date-format-options="{
                                                    day: 'numeric',
                                                    month: 'long',
                                                    year: 'numeric',
                                                }"
                                                :locale="currentLocale"
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </b-form-group>
                                    </validation-provider>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'study.student.common.form.label.gender'
                                            )
                                        "
                                        label-for="h-gender"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <v-select
                                            id="h-gender"
                                            v-model="newStudent.gender"
                                            :options="options.gender"
                                            :reduce="(option) => option.value"
                                            :placeholder="
                                                $t(
                                                    'study.student.common.form.placeholder.gender'
                                                )
                                            "
                                        />
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <validation-provider
                                        v-slot="{ errors }"
                                        name="Số điện thoại"
                                        rules="phone"
                                    >
                                        <b-form-group
                                            :label="
                                                $t(
                                                    'study.student.common.form.label.phoneNumber'
                                                )
                                            "
                                            label-for="h-phoneNumber"
                                            label-cols-md="4"
                                            :class="formGroupClass"
                                        >
                                            <b-form-input
                                                id="h-phoneNumber"
                                                v-model="newStudent.phone"
                                                trim
                                                :placeholder="
                                                    $t(
                                                        'study.student.common.form.placeholder.phoneNumber'
                                                    )
                                                "
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
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
                                            :label="
                                                $t(
                                                    'categories.employees.common.form.label.email'
                                                )
                                            "
                                            label-for="h-email"
                                            label-cols-md="4"
                                            :class="formGroupClass"
                                        >
                                            <b-form-input
                                                id="h-email"
                                                v-model="newStudent.email"
                                                trim
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.email'
                                                    )
                                                "
                                            />
                                            <small class="text-danger">{{
                                                errors[0]
                                            }}</small>
                                        </b-form-group>
                                    </validation-provider>
                                </b-col>
                                <b-col md="6">
                                    <b-form-group
                                        :label="$t('Lớp học')"
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="$t('Lớp học')"
                                        >
                                            <v-select
                                                v-model="newStudent.ClassdMulId"
                                                :options="options.classes"
                                                :reduce="
                                                    (item) => parseInt(item.id)
                                                "
                                                :multiple="true"
                                                label="text"
                                                :placeholder="$t('Lớp học')"
                                            />
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
                                                v-model="newStudent.status"
                                                :options="lstStatus"
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
                        <b-button
                            v-if="authorize(['ManageSTDUsers'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            @click="navigateToList()"
                            type="reset"
                            variant="outline-secondary"
                            title="Cancel"
                            class="btn-120 mb-50"
                        >
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

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            options: {
                classes: [],
                groups: [],
                departmentTree: [],
                areaTree: null,
                gender: [
                    { value: 0, label: 'Nữ' },
                    { value: 1, label: 'Nam' },
                ],
            },
            newStudent: {
                code: null,
                fullname: null,
                birthday: null,
                gender: null,
                phoneNumber: null,
                email: null,
                avatarBase64: null,
                citizenId: null,
                status: 1,
            },
            lstStatus: [
                { value: 1, text: 'Hoạt động' },
                { value: 2, text: 'Ngưng hoạt động' },
            ],
        }
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        currentLocale() {
            return this.$i18n ? this.$i18n.locale : 'vi' // Nếu $i18n null, mặc định 'vi'
        },
    },
    async created() {
        await this.loadOptions()
    },
    methods: {
        handleFileUpload(event) {
            let file

            // Kiểm tra nếu là sự kiện kéo thả
            if (event.dataTransfer && event.dataTransfer.files.length > 0) {
                file = event.dataTransfer.files[0]
            } else if (event.target && event.target.files.length > 0) {
                file = event.target.files[0]
            }

            if (!file) {
                this.newStudent.avatarBase64 = ''
                return
            }

            const reader = new FileReader()
            reader.onload = (e) => {
                const base64String = e.target.result
                if (!base64String.startsWith('data:')) {
                    const mimeType = file.type || 'image/png'
                    this.newStudent.avatarBase64 = `data:${mimeType};base64,${base64String}`
                } else {
                    this.newStudent.avatarBase64 = base64String
                }
            }

            reader.onerror = () => {
                this.errors.push('Không thể đọc file, vui lòng thử lại.')
            }

            reader.readAsDataURL(file)
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
            this.$services
                .get('/lookup/departments-tree?type=1')
                .then((response) => {
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
                        const res = await this.$services.post(
                            '/stdUser',
                            this.newStudent
                        )
                        this.showSuccessToast(res.data)
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
                    title: this.$t('Success.Create'),
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
            this.$router.push({ path: '/study/student/list' })
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
