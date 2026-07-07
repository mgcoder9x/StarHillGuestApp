<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row cols="1" align-h="center">
                        <b-col md="8">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.departments.common.form.label.departmentCode'
                                    )
                                "
                                label-for="h-department-code"
                                label-cols-md="4"
                                label-class="required"
                                class="mb-50 mb-md-1"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    name="DepartmentCode"
                                >
                                    <b-form-input
                                        id="h-department-code"
                                        v-model="updatedDepartment.code"
                                        :disabled="!editing"
                                        placeholder="Mã"
                                        :state="errors.length ? false : null"
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
                                >
                                    <b-form-input
                                        id="h-department-name"
                                        v-model="updatedDepartment.name"
                                        :disabled="!editing"
                                        :placeholder="
                                            $t(
                                                'categories.departments.common.form.label.departmentName'
                                            )
                                        "
                                        :state="errors.length ? false : null"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <b-col md="8">
                            <validation-provider
                                #default="{ errors }"
                                :rules="{ is_not: parseInt(departmentId) }"
                                name="ParentDepartmentName"
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.departments.common.form.label.parentDepartment'
                                        )
                                    "
                                    label-for="h-department-name"
                                    label-cols-md="4"
                                    class="mb-50 mb-md-1"
                                >
                                    <tree-select
                                        v-model="updatedDepartment.parentId"
                                        :disabled="!editing"
                                        :options="treeDepartments"
                                        label="text"
                                        :reduce="(option) => option.id"
                                        :placeholder="
                                            $t(
                                                'categories.departments.common.form.placeholder.parentDepartment'
                                            )
                                        "
                                        :state="errors.length ? false : null"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col>
                            <div class="text-center">
                                <Transition mode="out-in">
                                    <b-button
                                        v-if="
                                            editing &&
                                            authorize(['ManageDepartment'])
                                        "
                                        type="button"
                                        variant="primary"
                                        class="mx-50 mb-50 btn-120"
                                        @click="validateAndSubmitForm"
                                    >
                                        {{ $t('common.button.save') }}
                                    </b-button>
                                    <b-button
                                        v-if="
                                            !editing &&
                                            authorize(['ManageDepartment'])
                                        "
                                        type="button"
                                        class="mx-50 mb-50 btn-120"
                                        variant="primary"
                                        @click="startEdit"
                                    >
                                        {{ $t('common.button.edit') }}
                                    </b-button>
                                </Transition>
                                <b-button
                                    v-if="!editing"
                                    :to="{
                                        path: '/categories/departments/list',
                                    }"
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary"
                                >
                                    {{ $t('common.button.back') }}
                                </b-button>
                                <b-button
                                    v-if="editing"
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary"
                                    @click="stopEdit"
                                >
                                    {{ $t('common.button.cancel') }}
                                </b-button>
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
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
export default {
    mixins: [authorizationMixin],
    components: {},
    data() {
        return {
            treeDepartments: [],
            updatedDepartment: {
                code: '',
                name: '',
                parentId: null,
            },
            editing: false,
        }
    },
    computed: {
        departmentId() {
            return this.$route.params.departmentId
        },
    },
    async created() {
        await this.getTreeDepartments()
        await this.getDepartment()
    },
    methods: {
        async getTreeDepartments() {
            try {
                const res = await this.$services.get('/departments/tree/1')
                if (res.data && res.data.data) {
                    this.treeDepartments = this.convertTree(
                        res.data.data,
                        parseInt(this.departmentId)
                    )
                }
               
            } catch (error) {
                console.error(error)
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
        async getDepartment() {
            try {
                const res = await this.$services.get(
                    `/departments/${this.departmentId}`
                )
                this.updatedDepartment = res.data.data
            } catch (error) {
                console.log('error')
            }
        },
        validateAndSubmitForm() {
            this.$refs.rules.validate().then((isValid) => {
                if (isValid) {
                    this.submitForm()
                }
            })
        },
        async submitForm() {
            try {
                this.updatedDepartment.code = this.updatedDepartment.code
                    ? this.updatedDepartment.code.trim()
                    : null
                this.updatedDepartment.name = this.updatedDepartment.name
                    ? this.updatedDepartment.name.trim()
                    : null
                const res = await this.$services.put(
                    `/departments/${this.$route.params.departmentId}`,
                    this.updatedDepartment
                )
                this.showSuccessToast(res.data)
                this.stopEdit()
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
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getDepartment()
        },
    },
}
</script>

<style lang="scss"></style>
