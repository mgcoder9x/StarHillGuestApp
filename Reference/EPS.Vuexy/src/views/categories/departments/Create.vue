<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row cols="1" align-h="center">
                        <b-col md="8">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.departments.common.form.label.departmentCode'
                                    )
                                "
                                label-for="h-departments-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    name="DepartmentCode"
                                >
                                    <b-form-input
                                        id="h-department-code"
                                        v-model="newDepartment.code"
                                        :placeholder="
                                            $t(
                                                'categories.departments.common.form.placeholder.departmentCode'
                                            )
                                        "
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
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                name="DepartmentName"
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.departments.common.form.label.departmentName'
                                        )
                                    "
                                    label-for="h-department-name"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        id="h-full-name"
                                        v-model="newDepartment.name"
                                        :placeholder="
                                            $t(
                                                'categories.departments.common.form.placeholder.departmentName'
                                            )
                                        "
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <b-col md="8">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.departments.common.form.label.parentDepartment'
                                    )
                                "
                                label-for="h-department-name"
                                label-cols-md="4"
                                :class="formGroupClass"
                            >
                                <tree-select
                                    v-model="newDepartment.parentId"
                                    :options="treeDepartments"
                                    label="text"
                                    :reduce="(option) => option.id"
                                    :disabled="disabledParent"
                                    :placeholder="
                                        $t(
                                            'categories.departments.common.form.placeholder.parentDepartment'
                                        )
                                    "
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageDepartment'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            :to="{ path: '/categories/departments/list' }"
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
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
export default {
    mixins: [authorizationMixin],
    components: {},
    data() {
        return {
            treeDepartments: [],
            disabledParent: false,
            newDepartment: {
                code: null,
                name: null,
                parentId: null,
                note: null,
                compId: null,
                isDelete: false,
                type: 1,
            },
        }
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
    },
    async created() {
        await this.getTreeDepartments()
        this.newDepartment.parentId =
            this.$route.query?.parentId !== undefined
                ? this.$route.query?.parentId
                : null
        this.disabledParent =
            this.$route.query?.parentId !== undefined ? true : false
    },
    methods: {
        async getTreeDepartments() {
            try {
                const res = await this.$services.get('/departments/tree/1')
                if (res.data && res.data.data) {
                    this.treeDepartments = this.convertTree(res.data.data)
                }
            } catch (error) {
                console.log('error')
            }
        },
        convertTree(data, ignoredId) {
            return data
                .filter((item) => item.id !== ignoredId)
                .map((item) => {
                    const { _hasChildren, _children, ...rest } = item
                    return {
                        ...rest,
                        hasChildren: _hasChildren,
                        children: _children
                            ? this.convertTree(_children, ignoredId)
                            : [],
                    }
                })
        },
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then((success) => {
                if (!success) {
                    return
                    // handle validation errors...
                } else {
                    this.submitForm()
                }
            })
        },
        async submitForm() {
            try {
                this.newDepartment.code = this.newDepartment.code
                    ? this.newDepartment.code.trim()
                    : null
                this.newDepartment.name = this.newDepartment.name
                    ? this.newDepartment.name.trim()
                    : null
                const res = await this.$services.post(
                    '/departments',
                    this.newDepartment
                )
                this.showSuccessToast(res.data)
                this.navigateToDepartmentsList()
            } catch (error) {
                this.showErrorToast(error)
            }
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(
                        `categories.departments.error.${data.errorCode}`
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
                    title: this.$t('categories.waterWarning.Label.name'),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(`${error.message}`),
                },
            })
        },
        navigateToDepartmentsList() {
            this.$router.push({ path: '/categories/departments/list' })
        },
    },
}
</script>

<style lang="scss"></style>
