<!-- eslint-disable -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row cols="2" align-h="start">
                        <!-- Start: input Data -->
                        <!-- Đơn vị -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.machines.common.form.label.compName'
                                    )
                                "
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    :name="
                                        $t(
                                            'categories.machines.common.form.label.compName'
                                        )
                                    "
                                >
                                    <tree-select
                                        v-model="newMachine.compId"
                                        :options="compTree"
                                        label="text"
                                        :reduce="(option) => option.id"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.compName'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <!-- Chức năng thiết bị -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.machines.common.form.label.machineFunction'
                                    )
                                "
                                label-for="h-machines-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.machines.common.form.label.machineFunction'
                                        )
                                    "
                                >
                                    <b-form-select
                                        v-model="newMachine.machineFunction"
                                        :options="machineFunction"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.machineFunction'
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
                        <!-- Tên thiết bị -->
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.machineName'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.machineName'
                                        )
                                    "
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        v-model.trim="newMachine.machineName"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.machineName'
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
                        <!-- Mô tả -->
                        <b-col md="6">
                            <validation-provider
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.description'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.description'
                                        )
                                    "
                                    label-cols-md="4"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        v-model.trim="newMachine.description"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.description'
                                            )
                                        "
                                        maxlength="250"
                                    />
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <!-- Số IMEI -->
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required|imei"
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.imei'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.imei'
                                        )
                                    "
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        v-model.trim="newMachine.imei"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.imei'
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
                        <!-- Địa chỉ Mac -->
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required|mac"
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.mac'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.mac'
                                        )
                                    "
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        v-model.trim="newMachine.mac"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.mac'
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
                        <!-- Địa chỉ máy chủ -->
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required|serverIp"
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.serverIp'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.serverIp'
                                        )
                                    "
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        v-model.trim="newMachine.serverIp"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.serverIp'
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
                        <!-- Ngưỡng điểm nhận diện -->
                        <b-col md="6">
                            <validation-provider
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.identificationThreshold'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.identificationThreshold'
                                        )
                                    "
                                    label-cols-md="4"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        v-model="
                                            newMachine.identificationThreshold
                                        "
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.identificationThreshold'
                                            )
                                        "
                                        type="number"
                                        step="any"
                                    />
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <!-- Ngưỡng cảnh báo nhiệt độ (°C) -->
                        <b-col md="6">
                            <validation-provider
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.temperatureThreshold'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.temperatureThreshold'
                                        )
                                    "
                                    label-cols-md="4"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        v-model="
                                            newMachine.temperatureThreshold
                                        "
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.temperatureThreshold'
                                            )
                                        "
                                        type="number"
                                    />
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <!-- Khoảng cách nhận diện (cm) -->
                        <b-col md="6">
                            <validation-provider
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.identificationDistance'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.identificationDistance'
                                        )
                                    "
                                    label-cols-md="4"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        v-model="
                                            newMachine.identificationDistance
                                        "
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.identificationDistance'
                                            )
                                        "
                                        type="number"
                                    />
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <!-- Bật/Tắt thời gian khởi động -->
                        <b-col md="6" @change="newMachine.restartTime = null">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.autoStart'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.autoStart'
                                        )
                                    "
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-select
                                        v-model="newMachine.autoStart"
                                        :options="onOff"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.autoStart'
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
                        <!-- Thời gian giữa 2 lần nhận diện (mili giây) -->
                        <b-col md="6">
                            <validation-provider
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.delay'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.delay'
                                        )
                                    "
                                    label-cols-md="4"
                                    :class="formGroupClass"
                                >
                                    <b-form-input
                                        v-model="newMachine.delay"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.delay'
                                            )
                                        "
                                        type="number"
                                    />
                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <!-- Thời gian khởi động lại -->
                        <b-col md="6" v-if="newMachine.autoStart">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.restartTime'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.restartTime'
                                        )
                                    "
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-timepicker
                                        v-model="newMachine.restartTime"
                                        locale="vi"
                                        show-seconds
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
                        <!-- Ngôn ngữ -->
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.language'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.language'
                                        )
                                    "
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-select
                                        v-model="newMachine.language"
                                        :options="language"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.language'
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
                        <!-- Chế độ bảo vệ màn hình -->
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.screenSaver'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.screenSaver'
                                        )
                                    "
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-select
                                        v-model="newMachine.screenSaver"
                                        :options="onOff"
                                        :placeholder="
                                            $t(
                                                'categories.machines.common.form.placeholder.screenSaver'
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
                        <!-- Khu vực -->
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.areaName'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.machines.common.form.label.areaName'
                                        )
                                    "
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-select
                                        v-model="newMachine.areaId"
                                        :options="areaList"
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
                        <!-- Ảnh logo -->
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.machines.common.form.label.logoSelect'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="('Logo')"
                                    label-cols-md="4"
                                    label-class="required"
                                    :class="formGroupClass"
                                >
                                    <b-form-file
                                    v-model="newMachine.file"
                                    :placeholder="
                                        $t(
                                            'categories.machines.common.form.label.logoSelect'
                                        )
                                    "
                                    :drop-placeholder="
                                        $t(
                                            'categories.machines.common.form.placeholder.logoSelect'
                                        )
                                    "
                                ></b-form-file>
                                <small class="text-danger">{{
                                    errors[0]
                                }}</small>

                                </b-form-group>
                            </validation-provider>
                        </b-col>
                        <!-- End: input Data -->
                    </b-row>
                    <!-- ----CHECKBOX---- -->
                    <b-row cols="2" align-h="start" class="m-2">
                        <b-col md="4">
                            <b-form-checkbox
                                v-model="newMachine.fraudProof"
                                name="checkbox-1"
                                :value="true"
                                :unchecked-value="false"
                            >
                                {{
                                    $t(
                                        'categories.machines.common.form.label.fraudProof'
                                    )
                                }}
                            </b-form-checkbox>
                        </b-col>
                        <b-col md="4">
                            <b-form-checkbox
                                v-model="newMachine.led"
                                name="checkbox-1"
                                :value="true"
                                :unchecked-value="false"
                            >
                                {{
                                    $t(
                                        'categories.machines.common.form.label.led'
                                    )
                                }}
                            </b-form-checkbox>
                        </b-col>
                        <b-col md="4">
                            <b-form-checkbox
                                v-model="newMachine.autoLaunch"
                                name="checkbox-1"
                                :value="true"
                                :unchecked-value="false"
                            >
                                {{
                                    $t(
                                        'categories.machines.common.form.label.autoLaunch'
                                    )
                                }}
                            </b-form-checkbox>
                        </b-col>
                    </b-row>
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageMachine'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            :to="{ path: '/categories/machines/list' }"
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
import regexValidation from '@/utils/validation-regex'

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        return {
            compTree: [],
            areaList: [],
            // machineFuncList: [],
            machineFunction: [],
            onOff: [
                { value: true, text: 'ON' },
                { value: false, text: 'OFF' },
            ],
            language: [
                { value: 'EN', text: 'English' },
                { value: 'VI', text: 'Tiếng việt' },
            ],
            newMachine: {
                compId: null,
                machineFunction: null,
                machineName: null,
                description: null,
                imei: null,
                mac: null,
                serverIp: null,
                identificationThreshold: 0.8,
                temperatureThreshold: null,
                identificationDistance: 80,
                autoStart: false,
                delay: 2000,
                language: null,
                screenSaver: null,
                areaId: null,
                fraudProof: null,
                led: null,
                autoLaunch: null,
                file: null,
                restartTime: null,
            },
        }
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
    },
    async created() {
        this.createValidator()
        this.loadCompanyTree()
        this.loadArea()
        this.loadAppParams()
    },
    methods: {
        createValidator() {
            regexValidation(
                'imei',
                '^\\d{15}$',
                `${this.$t('categories.machines.validation.imei')}`
            )
            regexValidation(
                'mac',
                '^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$',
                `${this.$t('categories.machines.validation.mac')}`
            )
            regexValidation(
                'serverIp',
                '^(https?:\\/\\/)?((([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,})|(\\d{1,3}\\.){3}\\d{1,3}|(\\[([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}\\])|(\\[::(ffff:(\\d{1,3}\\.){3}\\d{1,3})\\]))(:\\d{1,5})?$',
                `${this.$t('categories.machines.validation.serverIp')}`,
                'gm'
            )
        },
        loadCompanyTree() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.compTree = response.data
            })
        },
        loadArea() {
            this.$services.get('/lookup/areas').then((response) => {
                var data = response.data.data.map((item) => {
                    return { value: item.id, text: item.text }
                })
                this.areaList = data
            })
        },
        loadAppParams() {
            this.$services.get('/lookup/appParams/SMF_MCF').then((response) => {
                const data = response.data.data.map((item) => {
                    return { value: item.id, text: item.text }
                })
                this.machineFunction = data
            })
        },
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                if (!success) {
                    // handle validation errors...
                } else {
                    try {
                        const formData = new FormData()
                        // Thêm tất cả các thuộc tính của newMachine vào formData
                        Object.entries(this.newMachine).forEach(
                            ([key, value]) => {
                                if (value !== undefined && value !== null) {
                                    formData.append(key, value)
                                }
                            }
                        )
                        const res = await this.$services.post(
                            '/machines',
                            formData
                        )
                        this.showSuccessToast(res.data)
                        this.navigateTomachinesList()
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
                        `categories.machines.error.${data.errorCode}`
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
                    text: this.$t(
                        `categories.machines.error.${error.errorCode}`
                    ),
                },
            })
        },
        navigateTomachinesList() {
            this.$router.push({ path: '/categories/machines/list' })
        },
    },
}
</script>

<style lang="scss"></style>
