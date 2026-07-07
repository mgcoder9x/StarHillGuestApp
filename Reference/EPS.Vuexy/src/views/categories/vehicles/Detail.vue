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
                                        v-model.trim="updateVehicle.licensePlate"
                                        type="text"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :placeholder="
                                            $t('common.input.placeholder')
                                        "
                                        :disabled="!isEdit"
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
                                        v-model="updateVehicle.type"
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
                                        :disabled="!isEdit"
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
                                        v-model="updateVehicle.departmentType"
                                        :options="departmentType"
                                        :disabled="!isEdit"
                                        @change="changeDepartmentType($event)"
                                    >
                                    </b-radio-group>
                                </div>
                            </b-form-group>
                        </b-col>
                        <b-col v-if="updateVehicle.departmentType == 1" md="7">
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
                                        v-model="updateVehicle.departmentId"
                                        :options="options.departmentTree"
                                        :placeholder="
                                            $t('common.select.placeholder')
                                        "
                                        label="text"
                                        :multiple="false"
                                        track-by="id"
                                        :reduce="(item) => parseInt(item.id)"
                                        :disabled="!isEdit"
                                    >
                                    </tree-select>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col
                            v-else-if="updateVehicle.departmentType == 2"
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
                                        v-model="updateVehicle.departmentId"
                                        :options="options.contractorTree"
                                        :placeholder="
                                            $t('common.select.placeholder')
                                        "
                                        label="text"
                                        :multiple="false"
                                        track-by="id"
                                        :reduce="(item) => parseInt(item.id)"
                                        :disabled="!isEdit"
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
                                updateVehicle.departmentType == 1 &&
                                updateVehicle.departmentId
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
                                    v-model="updateVehicle.employeeId"
                                    :options="options.employees"
                                    :placeholder="
                                        $t('common.select.placeholder')
                                    "
                                    label="text"
                                    :multiple="false"
                                    track-by="id"
                                    :reduce="(item) => item.id"
                                    :disabled="!isEdit"
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
                                        v-model="updateVehicle.groupId"
                                        :options="options.groups"
                                        :placeholder="
                                            $t('common.select.placeholder')
                                        "
                                        label="text"
                                        :reduce="(item) => parseInt(item.id)"
                                        :disabled="!isEdit"
                                    >
                                    </v-select>
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
                                <Transition mode="out-in">
                                    <b-button
                                        v-if="
                                            isEdit &&
                                            authorize(['ManageVehicle'])
                                        "
                                        type="button"
                                        variant="primary"
                                        class="mx-50 mb-50 btn-120 btn-hover-linear-primary"
                                        @click="onSubmit"
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
                                        v-if="
                                            !isEdit &&
                                            authorize(['ManageVehicle'])
                                        "
                                        type="button"
                                        class="mx-50 mb-50 btn-120 btn-hover-linear-primary"
                                        variant="primary"
                                        @click="startEdit"
                                    >
                                        <Icon
                                            icon="material-symbols:edit-square-outline-rounded"
                                            class="sm-icon"
                                        />
                                        <span class="ml-50">
                                            {{ $t('common.button.edit') }}
                                        </span>
                                    </b-button>
                                </Transition>
                                <b-button
                                    v-if="!isEdit"
                                    type="button"
                                    class="mx-50 mb-50 btn-120 btn-hover-linear-secondary"
                                    variant="outline-secondary"
                                    @click="navigateToVehiclesList"
                                >
                                    <Icon
                                        icon="mingcute:back-2-line"
                                        class="sm-icon"
                                    />
                                    <span class="ml-50">
                                        {{ $t('common.button.back') }}
                                    </span>
                                </b-button>
                                <b-button
                                    v-if="isEdit"
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary btn-hover-linear-secondary"
                                    @click="stopEdit"
                                >
                                    <Icon icon="mdi:cancel" class="sm-icon" />
                                    <span class="ml-50">
                                        {{ $t('common.button.cancel') }}
                                    </span>
                                </b-button>
                            </div>
                        </b-col>
                    </b-row>
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

export default {
    mixins: [authorizationMixin],
    components: {},
    data() {
        return {
            isEdit: false,
            updateVehicle: {
                licensePlate: '',
                departmentId: null,
                type: 1,
                groupId: null,
                departmentType: 1,
                employeeId: null,
            },
            employeeId: null,
            options: {
                employees: [],
                departmentTree: [],
                contractorTree: [],
                groups: [],
            },
        }
    },
    computed: {
        vehicleId() {
            return this.$route.params.vehicleId
        },
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
        'updateVehicle.departmentType': function (val) {
            this.updateVehicle.departmentId = null
            if (this.tempDepartmentId) {
                this.updateVehicle.departmentId = this.tempDepartmentId
            }
        },
        'updateVehicle.departmentId': function (val) {
            if (this.updateVehicle.departmentType == 1 && val) {
                this.loadEmployeesByDepartment()
            }
            if (this.isEdit) {
                this.updateVehicle.employeeId = null
            }
        },
    },
    // ...
    async created() {
        await this.loadOptions()
    },
    methods: {
        changeDepartmentType() {
            this.updateVehicle.departmentId = null
        },
        async loadOptions() {
            this.loadDepartmentTree()
            this.loadContractorTree()
            this.loadGroup()
            await this.loadVehicle(this.$route.params.id)
            this.updateVehicle.employeeId =
                this.updateVehicle.employeeId || null
        },
        async loadVehicle(id) {
            try {
                const response = await this.$services.get(
                    `/vehicles/${this.vehicleId}`
                )
                this.updateVehicle = response.data.data
                this.employeeId = this.updateVehicle.employeeId
                this.tempDepartmentId = response.data.data.departmentId
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.G_VEHICLES_500`)}`,
                    },
                })
            }
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
        async loadEmployeesByDepartment() {
            try {
                const response = await this.$services.get(
                    `/lookup/departments/${this.updateVehicle.departmentId}/employees`
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
                this.updateVehicle.licensePlate = this.updateVehicle
                    .licensePlate
                    ? this.updateVehicle.licensePlate.trim()
                    : null

                const res = await this.$services.put(
                    `/vehicles/${this.vehicleId}`,
                    this.updateVehicle
                )
                this.showSuccessToast(res.data)
                this.navigateToVehiclesList()
            } catch (error) {
                console.log(error)
                this.showErrorToast(error)
            }
        },
        showSuccessToast(data) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: this.$t(`${data.errorCode}`),
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
        startEdit() {
            this.isEdit = true
        },
        stopEdit() {
            this.loadVehicle(this.$route.params.id)
            this.isEdit = false
        },
    },
}
</script>

<style lang="scss"></style>
