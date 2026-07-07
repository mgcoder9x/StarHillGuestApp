<!-- eslint-disable -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row>
                        <!-- Start: input Img -->
                        <b-col md="3">
                            <b-img
                                v-if="viewAvatar"
                                :src="viewAvatar"
                                class="mb-1 avatar-img"
                                alt="Avatar"
                            />
                            <b-form-file
                                :placeholder="
                                    $t(
                                        'categories.employees.common.form.label.avartarPath'
                                    )
                                "
                                drop-placeholder="Kéo file vào đây"
                                @change="handleFileUpload"
                                :disabled="!isEdit"
                                accept=".jpg, .jpeg, .png"
                            />
                        </b-col>
                        <!-- End: input Img -->
                        <!-- Start: input Data -->
                        <b-col md="9">
                            <b-row>
                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.compName'
                                            )
                                        "
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.compName'
                                                )
                                            "
                                        >
                                            <tree-select
                                                v-model="updateEmployee.compId"
                                                :options="options.compTree"
                                                label="text"
                                                :reduce="(option) => option.id"
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.compName'
                                                    )
                                                "
                                                :disabled="!isEdit"
                                                @input="changeCompany($event)"
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
                                                'categories.employees.common.form.label.depName'
                                            )
                                        "
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.depName'
                                                )
                                            "
                                        >
                                            <tree-select
                                                v-model="updateEmployee.depId"
                                                :options="
                                                    options.departmentTree
                                                "
                                                label="text"
                                                :reduce="(option) => option.id"
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.depName'
                                                    )
                                                "
                                                :disabled="!isEdit"
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
                                                'categories.employees.common.form.label.groupName'
                                            )
                                        "
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.groupName'
                                                )
                                            "
                                        >
                                            <v-select
                                                v-model="updateEmployee.listGroupId"
                                                :options="options.groupByCompId"
                                                :multiple="true"
                                                :reduce="
                                                    (item) => parseInt(item.id)
                                                "
                                                label="text"
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.groupName'
                                                    )
                                                "
                                                :disabled="!isEdit"
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
                                                'categories.employees.common.form.label.code'
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
                                                    'categories.employees.common.form.label.code'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-code"
                                                v-model.trim="
                                                    updateEmployee.code
                                                "
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.code'
                                                    )
                                                "
                                                :disabled="!isEdit"
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
                                                'categories.employees.common.form.label.citizenId'
                                            )
                                        "
                                        label-for="h-citizenId"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            :rules="{
                                                regex: /^[0-9]{9,12}$/,
                                            }"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.citizenId'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-citizenId"
                                                v-model.trim="
                                                    updateEmployee.citizenId
                                                "
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.citizenId'
                                                    )
                                                "
                                                :state="
                                                    errors.length > 0
                                                        ? false
                                                        : null
                                                "
                                                :disabled="!isEdit"
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
                                                'categories.employees.common.form.label.fullname'
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
                                                    'categories.employees.common.form.label.fullname'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-fullname"
                                                v-model.trim="
                                                    updateEmployee.fullname
                                                "
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.fullname'
                                                    )
                                                "
                                                :state="
                                                    errors.length ? false : null
                                                "
                                                :disabled="!isEdit"
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
                                                'categories.employees.common.form.label.jobDuties'
                                            )
                                        "
                                        label-for="h-jobDuties"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <b-form-input
                                            id="h-jobDuties"
                                            v-model.trim="
                                                updateEmployee.jobDuties
                                            "
                                            :placeholder="
                                                $t(
                                                    'categories.employees.common.form.placeholder.jobDuties'
                                                )
                                            "
                                            :disabled="!isEdit"
                                        />
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.position'
                                            )
                                        "
                                        label-for="h-position"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <b-form-input
                                            id="h-position"
                                            v-model.trim="
                                                updateEmployee.position
                                            "
                                            :placeholder="
                                                $t(
                                                    'categories.employees.common.form.placeholder.position'
                                                )
                                            "
                                            :disabled="!isEdit"
                                        />
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
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
                                            v-model="updateEmployee.birthday"
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
                                            :locale="this.$i18n.locale"
                                            :disabled="!isEdit"
                                        />
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.gender'
                                            )
                                        "
                                        label-for="h-gender"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <v-select
                                            id="h-gender"
                                            v-model="updateEmployee.gender"
                                            :options="
                                                rechangeOptions(options.gender)
                                            "
                                            :reduce="(option) => option.value"
                                            :placeholder="
                                                $t(
                                                    'categories.employees.common.form.placeholder.gender'
                                                )
                                            "
                                            :disabled="!isEdit"
                                        />
                                    </b-form-group>
                                </b-col>

                                <b-col md="6">
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.employees.common.form.label.phoneNumber'
                                            )
                                        "
                                        label-for="h-phoneNumber"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            :rules="{
                                                regex: /^\d{10,11}$/,
                                            }"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.phoneNumber'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-phoneNumber"
                                                v-model.trim="
                                                    updateEmployee.phoneNumber
                                                "
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.phoneNumber'
                                                    )
                                                "
                                                :disabled="!isEdit"
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
                                                'categories.employees.common.form.label.email'
                                            )
                                        "
                                        label-for="h-email"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            :rules="{
                                                regex: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
                                            }"
                                            :name="
                                                $t(
                                                    'categories.employees.common.form.label.email'
                                                )
                                            "
                                        >
                                            <b-form-input
                                                id="h-email"
                                                v-model.trim="
                                                    updateEmployee.email
                                                "
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.email'
                                                    )
                                                "
                                                :disabled="!isEdit"
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
                                                'categories.employees.common.form.label.status'
                                            )
                                        "
                                        label-for="h-status"
                                        label-cols-md="4"
                                        :class="formGroupClass"
                                        label-class="required"
                                    >
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            name="Status"
                                        >
                                            <v-select
                                                id="h-status"
                                                v-model="updateEmployee.status"
                                                :options="
                                                    rechangeOptions(
                                                        options.status
                                                    )
                                                "
                                                :reduce="
                                                    (option) => option.value
                                                "
                                                :placeholder="
                                                    $t(
                                                        'categories.employees.common.form.placeholder.status'
                                                    )
                                                "
                                                :clearable="false"
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
                        <b-button
                            v-if="!isEdit"
                            type="button"
                            variant="primary"
                            class="mx-50 mb-50 btn-120"
                            @click.prevent="isEdit = true"
                        >
                            {{ $t('common.button.edit') }}
                        </b-button>
                        <b-button
                            v-if="authorize(['ManageEmployee']) && isEdit"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            v-if="!isEdit"
                            :to="{ path: '/categories/employees/list' }"
                            type="button"
                            class="mx-50 mb-50 btn-120"
                            variant="outline-secondary"
                        >
                            {{ this.$t('Button.Back') }}
                        </b-button>
                        <b-button
                            @click="navigateToList()"
                            v-if="isEdit"
                            type="button"
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
import getBaseUrl from '@/utils/get-baseUrl'

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            isFirstLoad: true,
            options: {
                compTree: [],
                groups: [],
                groupByCompId: [],
                departmentTree: [],
                areaTree: null,
                gender: [
                    { value: 0, label: 'categories.employees.gender.female' },
                    { value: 1, label: 'categories.employees.gender.male' },
                ],
                status: [
                    { value: 1, label: 'StatusList.Active' },
                    { value: 2, label: 'StatusList.Inactive' },
                ],
            },
            updateEmployee: {
                compId: null,
                depId: null,
                groupId: null,
                code: null,
                fullname: null,
                jobDuties: null,
                position: null,
                birthday: null,
                gender: null,
                phoneNumber: null,
                email: null,
                avatarBase64: null,
                citizenId: null,
                // listGroupId: null,
            },
            viewAvatar: null,
            isEdit: false,
            // filterGroupByCom:[]
        }
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        imgUrl() {
            const { VUE_APP_BASE_URL: baseURL } = process.env
            return `${getBaseUrl()}/Employees`
        },
    },
    async created() {
        await this.loadOptions()
        await this.getData()
    },
    methods: {
        changeCompany(event) {
            if (this.isFirstLoad) {
                this.isFirstLoad = false
                return
            }
            this.updateEmployee.listGroupId = null
            if (event) {
                this.options.groupByCompId = this.options.groups.filter(
                    (x) => x.compId == event
                )
            } else {
                this.options.groupByCompId = this.options.groups
            }
        },
        
        rechangeOptions(dataList) {
            return dataList?.map((item) => ({
                ...item,
                label: this.$t(item.label),
            }))
        },
        async getData() {
            try {
                const res = await this.$services.get(
                    `/employees/${this.$route.params.id}`
                )
                this.updateEmployee = res.data.data
                if (this.updateEmployee.listGroupId.length > 0) {
                    this.updateEmployee.listGroupId = [
                        ...new Set(this.updateEmployee.listGroupId),
                    ]
                } else {
                    this.updateEmployee.listGroupId = [
                        ...new Set([this.updateEmployee.groupId]),
                    ]
                }
                this.viewAvatar =
                    this.imgUrl + '/' + this.updateEmployee.avartarPath
                if (this.updateEmployee.compId) {
                    this.options.groupByCompId = this.options.groups.filter(
                        (x) => x.compId == this.updateEmployee.compId
                )
                } else {
                    this.options.groupByCompId = this.options.groups
                }
            } catch (error) {
                console.log('error')
            }
        },
        handleFileUpload(event) {
            const file = event.target.files[0]
            this.updateEmployee.avatarBase64 = event.target.files[0]
            if (file) {
                const reader = new FileReader()
                reader.onload = (e) => {
                    const base64String = e.target.result
                    if (!base64String.startsWith('data:')) {
                        const mimeType = file.type || 'image/png'
                        this.updateEmployee.avatarBase64 = `data:${mimeType};base64,${base64String}`
                    } else {
                        this.updateEmployee.avatarBase64 = base64String
                    }
                    this.viewAvatar = this.updateEmployee.avatarBase64
                }
                reader.onerror = () => {
                    this.errors.push('Không thể đọc file, vui lòng thử lại.')
                }
                reader.readAsDataURL(file)
            } else {
                this.updateEmployee.avatarBase64 = ''
            }
        },
        async loadOptions() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.options.compTree = TreeHelper.removeEmptyChildren(
                    response.data
                )
            })
            this.$services.get('/lookup/groupsNotCompId').then((response) => {
                this.options.groups = response.data.data
                this.options.groupByCompId = this.options.groups
            })
            this.$services
                .get('/lookup/departments-tree?type1')
                .then((response) => {
                    this.options.departmentTree = response.data.data
                })
            const response = await this.$services.get('/lookup/areas-tree')
            this.options.areaTree = TreeHelper.removeEmptyChildren(
                response.data.data
            )
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        async onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                if (success) {
                    try {
                        this.updateEmployee.code = this.trimField(
                            this.updateEmployee.code
                        )
                        this.updateEmployee.fullname = this.trimField(
                            this.updateEmployee.fullname
                        )
                        this.updateEmployee.jobDuties = this.trimField(
                            this.updateEmployee.jobDuties
                        )
                        this.updateEmployee.position = this.trimField(
                            this.updateEmployee.position
                        )
                        this.updateEmployee.phoneNumber = this.trimField(
                            this.updateEmployee.phoneNumber
                        )
                        this.updateEmployee.email = this.trimField(
                            this.updateEmployee.email
                        )

                        const res = await this.$services.put(
                            '/employees/' + this.$route.params.id,
                            this.updateEmployee
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
                        `categories.employees.error.${data.errorCode}`
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
                    title: this.$t('Error.Error'),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(`${error.message}`),
                },
            })
        },
        navigateToList() {
            this.isEdit = false
            this.getData()
        },
    },
}
</script>

<style lang="scss" scoped>
.avatar-img {
    max-width: 100%;
    object-fit: contain;
}
</style>
