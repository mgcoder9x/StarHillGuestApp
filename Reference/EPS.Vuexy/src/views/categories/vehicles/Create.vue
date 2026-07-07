<template>
    <validation-observer ref="rules">
        <b-container fluid>
            <b-card>
                <b-form @submit="onSubmit">
                    <b-row cols="2" align-h="center">
                        <b-col md="7">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.vehicles.common.form.label.licensePlate'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|max:9"
                                    :name="
                                        $t(
                                            'categories.vehicles.common.form.label.licensePlate'
                                        )
                                    "
                                >
                                    <b-input
                                        v-model.trim="createVehicle.licensePlate"
                                        type="text"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :placeholder="
                                            $t('common.input.placeholder')
                                        "
                                    >
                                    </b-input>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="7">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.vehicles.common.form.label.type'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.vehicles.common.form.label.type'
                                        )
                                    "
                                >
                                    <v-select
                                        v-model="createVehicle.type"
                                        :options="vehicleType"
                                        label="text"
                                        :reduce="(item) => item.id"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :clearable="false"
                                        :placeholder="
                                            $t('common.select.placeholder')
                                        "
                                    >
                                    </v-select>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="7">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.vehicles.common.form.label.departmentType'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                            >
                                <div
                                    class="h-100 d-flex align-items-center jussify-content-center"
                                >
                                    <b-radio-group
                                        v-model="createVehicle.departmentType"
                                        :options="departmentType"
                                    >
                                    </b-radio-group>
                                </div>
                            </b-form-group>
                        </b-col>
                        <b-col v-if="createVehicle.departmentType == 1" md="7">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.vehicles.common.form.label.department'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.vehicles.common.form.label.department'
                                        )
                                    "
                                >
                                    <tree-select
                                        v-model="createVehicle.departmentId"
                                        :options="options.departmentTree"
                                        :placeholder="
                                            $t('common.select.placeholder')
                                        "
                                        label="text"
                                        :multiple="false"
                                        track-by="id"
                                        :reduce="(item) => parseInt(item.id)"
                                    >
                                    </tree-select>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col
                            v-else-if="createVehicle.departmentType == 2"
                            md="7"
                        >
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.vehicles.common.form.label.contractor'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.vehicles.common.form.label.contractor'
                                        )
                                    "
                                >
                                    <tree-select
                                        v-model="createVehicle.departmentId"
                                        :options="options.contractorTree"
                                        :placeholder="
                                            $t('common.select.placeholder')
                                        "
                                        label="text"
                                        :multiple="false"
                                        track-by="id"
                                        :reduce="(item) => parseInt(item.id)"
                                    >
                                    </tree-select>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col
                            v-if="
                                createVehicle.departmentType == 1 &&
                                createVehicle.departmentId
                            "
                            md="7"
                        >
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.vehicles.common.form.label.employee'
                                    )
                                "
                                label-cols-md="4"
                            >
                                <v-select
                                    v-model="createVehicle.employeeId"
                                    :options="options.employees"
                                    :placeholder="
                                        $t('common.select.placeholder')
                                    "
                                    label="text"
                                    :multiple="false"
                                    track-by="id"
                                    :reduce="(item) => item.id"
                                >
                                </v-select>
                            </b-form-group>
                        </b-col>
                        <b-col md="7">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.vehicles.common.form.label.group'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.vehicles.common.form.label.group'
                                        )
                                    "
                                >
                                    <v-select
                                        v-model="createVehicle.groupId"
                                        :options="options.groups"
                                        :placeholder="
                                            $t('common.select.placeholder')
                                        "
                                        label="text"
                                        :reduce="(item) => parseInt(item.id)"
                                    >
                                    </v-select>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageVehicle'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120 btn-hover-linear-primary"
                            :disabled="isSubmitting"
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
                            :to="{ path: '/categories/vehicles/list' }"
                            type="reset"
                            variant="secondary"
                            title="Cancel"
                            class="btn-120 mb-50 btn-hover-linear-secondary"
                        >
                            <Icon icon="mdi:cancel" class="sm-icon" />
                            <span class="ml-50">
                                {{ $t('common.button.cancel') }}
                            </span>
                        </b-button>
                    </div>
                </b-form>
            </b-card>
        </b-container>
    </validation-observer>
</template>
<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import TreeHelper from '@/utils/treeHelper'
import vueSelect from 'vue-select'

export default {
    mixins: [authorizationMixin],
    components: {},
    data() {
        return {
            isSubmitting: false,
            createVehicle: {
                licensePlate: '',
                departmentId: null,
                type: 1,
                groupId: null,
                departmentType: 1,
                employeeId: null,
            },
            options: {
                employees: [],
                departmentTree: [],
                contractorTree: [],
                groups: [],
            },
        }
    },
    computed: {
        departmentType() {
            return [
                {
                    text: this.$t(
                        'common.options.departmentVehicleType.departmentVehicle'
                    ),
                    value: 1,
                },
                {
                    text: this.$t(
                        'common.options.departmentVehicleType.contractorVehicle'
                    ),
                    value: 2,
                },
            ]
        },
        vehicleType() {
            return [
                {
                    id: 1,
                    text: this.$t('common.options.vehicleType.motorbike'),
                },
                {
                    id: 2,
                    text: this.$t('common.options.vehicleType.car'),
                },
            ]
        },
    },
    watch: {
        'createVehicle.departmentType': function (val) {
            this.createVehicle.departmentId = null
        },
        'createVehicle.departmentId': function (val) {
            if (this.createVehicle.departmentType == 1 && val) {
                this.loadEmployeesByDepartment()
            }
            this.createVehicle.employeeId = null
        },
    },
    async created() {
        await this.loadOptions()
    },
    methods: {
        async loadOptions() {
            this.loadDepartmentTree()
            this.loadContractorTree()
            this.loadGroup()
        },
        async loadDepartmentTree() {
            try {
                const response = await this.$services.get(
                    '/lookup/departments-tree?type=1'
                )
                this.options.departmentTree = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.LU_DEPARTMENTS_TREE_500`)}`,
                    },
                })
            }
        },
        async loadContractorTree() {
            try {
                const response = await this.$services.get(
                    '/lookup/departments-tree?type=2'
                )
                this.options.contractorTree = TreeHelper.removeEmptyChildren(
                    response.data.data
                )
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.LU_CONTRACTORS_TREE_500`)}`,
                    },
                })
            }
        },
        async loadGroup() {
            try {
                const response = await this.$services.get('/lookup/groups')
                this.options.groups = response.data.data
            } catch (error) {
                console.log('error', error)
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.LU_GROUPS_500`)}`,
                    },
                })
            }
        },
        async loadEmployeesByDepartment() {
            try {
                const response = await this.$services.get(
                    `/lookup/departments/${this.createVehicle.departmentId}/employees`
                )
                this.options.employees = response.data.data
            } catch (error) {
                console.log('error', error)
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.LU_DEPARTMENTS/EMPLOYEES_500`)}`,
                    },
                })
            }
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
            if (this.isSubmitting) return
            this.isSubmitting = true
            try {
                this.createVehicle.licensePlate = this.createVehicle
                    .licensePlate
                    ? this.createVehicle.licensePlate.trim()
                    : null

                const res = await this.$services.post(
                    '/vehicles',
                    this.createVehicle
                )
                this.showSuccessToast(res.data)
                this.navigateToVehiclesList()
            } catch (error) {
                this.showErrorToast(error)
            } finally {
                this.isSubmitting = false
            }
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(`errorCode.${data.errorCode}`),
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
        navigateToVehiclesList() {
            this.$router.push({ path: '/categories/vehicles/list' })
        },
    },
}
</script>

<style lang="scss"></style>
