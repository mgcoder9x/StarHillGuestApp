<template>
    <b-container fluid class="p-0">
        <b-card>
            <validation-observer ref="rules">
                <b-form>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.workingShifts.common.form.label.code'
                                    )
                                "
                                label-for="detail-working-shift-code"
                                label-cols-md="3"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required|noSpecialCharsExceptUnderscore"
                                    :name="
                                        $t(
                                            'categories.workingShifts.common.form.label.code'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="detail-working-shift-code"
                                        v-model="updatedWorkingShift.code"
                                        :disabled="!editing"
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
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.workingShifts.common.form.label.name'
                                    )
                                "
                                label-for="detail-working-shift-name"
                                label-cols-md="3"
                                label-class="required"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.workingShifts.common.form.label.name'
                                        )
                                    "
                                >
                                    <b-form-input
                                        id="detail-working-shift-name"
                                        v-model="updatedWorkingShift.name"
                                        :disabled="!editing"
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

                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                :rules="{
                                    required: true,
                                    validateStartTime:
                                        updatedWorkingShift.endTime,
                                }"
                                :name="
                                    $t(
                                        'categories.workingShifts.common.form.label.startTime'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.workingShifts.common.form.label.startTime'
                                        )
                                    "
                                    label-for="detail-working-shift-start-time"
                                    label-cols-md="3"
                                    label-class="required"
                                >
                                    <date-picker
                                        id="detail-working-shift-start-time"
                                        v-model="updatedWorkingShift.startTime"
                                        type="time"
                                        :locale="currentLocale"
                                        format="HH:mm"
                                        value-type="HH:mm:ss"
                                        style="width: 100%"
                                        :disabled="!editing"
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
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                :rules="{
                                    required: true,
                                    validateEndTime:
                                        updatedWorkingShift.startTime,
                                }"
                                :name="
                                    $t(
                                        'categories.workingShifts.common.form.label.endTime'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.workingShifts.common.form.label.endTime'
                                        )
                                    "
                                    label-for="detail-working-shift-end-time"
                                    label-cols-md="3"
                                    label-class="required"
                                >
                                    <date-picker
                                        id="detail-working-shift-end-time"
                                        v-model="updatedWorkingShift.endTime"
                                        type="time"
                                        :locale="currentLocale"
                                        format="HH:mm"
                                        value-type="HH:mm:ss"
                                        style="width: 100%"
                                        :disabled="!editing"
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
                        <b-col md="6">
                            <validation-provider
                                #default="{ errors }"
                                rules="required"
                                :name="
                                    $t(
                                        'categories.workingShifts.common.form.label.status'
                                    )
                                "
                            >
                                <b-form-group
                                    :label="
                                        $t(
                                            'categories.workingShifts.common.form.label.status'
                                        )
                                    "
                                    label-for="detail-working-shift-status"
                                    label-cols-md="3"
                                    label-class="required"
                                >
                                    <v-select
                                        v-model="updatedWorkingShift.status"
                                        :options="statusOptions"
                                        label="text"
                                        :reduce="(opt) => opt.value"
                                        :clearable="false"
                                        :disabled="!editing"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                    />
                                    <small class="text-danger">
                                        {{ errors[0] }}
                                    </small>
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
                                            authorize(['ManageWorkingShift'])
                                        "
                                        v-waves
                                        type="button"
                                        variant="primary"
                                        class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                        @click="validateAndSubmitForm"
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
                                            !editing &&
                                            authorize(['ManageWorkingShift'])
                                        "
                                        v-waves
                                        type="button"
                                        variant="primary"
                                        class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                                        @click="startEdit"
                                    >
                                        <Icon
                                            icon="line-md:edit-twotone"
                                            class="sm-icon"
                                        />
                                        <span class="ml-25">
                                            {{ $t('common.button.edit') }}
                                        </span>
                                    </b-button>
                                </Transition>
                                <b-button
                                    v-if="!editing"
                                    v-waves
                                    :to="{
                                        path: '/categories/workingShifts/list',
                                    }"
                                    type="button"
                                    variant="outline-secondary"
                                    class="mx-50 mb-50 btn-120 btn-hover-linear-secondary border-0"
                                >
                                    <Icon
                                        icon="line-md:arrow-small-left"
                                        class="sm-icon"
                                    />
                                    <span class="ml-25">
                                        {{ $t('common.button.back') }}
                                    </span>
                                </b-button>
                                <b-button
                                    v-if="editing"
                                    v-waves
                                    type="button"
                                    class="mx-50 mb-50 btn-120"
                                    variant="outline-secondary btn-hover-linear-secondary border-0"
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
            </validation-observer>
        </b-card>
    </b-container>
</template>

<script>
/* eslint-disable */
import { extend } from 'vee-validate'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import i18n from '@/libs/i18n'

// Custom validation rule for start time
extend('validateStartTime', {
    params: ['endTime'],
    validate(value, { endTime }) {
        if (!value || !endTime) {
            return true
        }
        // Compare time strings in HH:mm format
        const [startHour, startMin] = value.split(':').map(Number)
        const [endHour, endMin] = endTime.split(':').map(Number)
        const startTimeInMin = startHour * 60 + startMin
        const endTimeInMin = endHour * 60 + endMin
        return startTimeInMin < endTimeInMin
    },
    message: () => i18n.t('categories.workingShifts.error.InvalidStartTime'),
})

// Custom validation rule for end time
extend('validateEndTime', {
    params: ['startTime'],
    validate(value, { startTime }) {
        if (!value || !startTime) {
            return true
        }
        // Compare time strings in HH:mm format
        const [startHour, startMin] = startTime.split(':').map(Number)
        const [endHour, endMin] = value.split(':').map(Number)
        const startTimeInMin = startHour * 60 + startMin
        const endTimeInMin = endHour * 60 + endMin
        return endTimeInMin > startTimeInMin
    },
    message: () => i18n.t('categories.workingShifts.error.InvalidEndTime'),
})

export default {
    mixins: [authorizationMixin],
    data() {
        return {
            updatedWorkingShift: {
                code: null,
                name: null,
                startTime: null,
                endTime: null,
                status: 1,
                compId: null,
            },
            editing: false,
        }
    },
    computed: {
        workingShiftId() {
            return this.$route.params.id
        },
        currentLocale() {
            return this.$i18n.locale
        },
        statusOptions() {
            return [
                {
                    value: 1,
                    text: this.$t(
                        'categories.workingShifts.list.searchForm.options.active'
                    ),
                },
                {
                    value: 0,
                    text: this.$t(
                        'categories.workingShifts.list.searchForm.options.inactive'
                    ),
                },
            ]
        },
    },
    async created() {
        const accessToken = this.$services.getUserData()
        this.updatedWorkingShift.compId = accessToken.companyId
        await this.getWorkingShift()
    },
    methods: {
        async getWorkingShift() {
            try {
                const res = await this.$services.get(
                    `/working-shifts/${this.workingShiftId}`
                )
                this.updatedWorkingShift = res.data.data
                // Format startTime and endTime to HH:mm format
                // this.updatedWorkingShift.startTime = this.$moment(
                //     this.updatedWorkingShift.startTime
                // ).format('HH:mm')
                // this.updatedWorkingShift.endTime = this.$moment(
                //     this.updatedWorkingShift.endTime
                // ).format('HH:mm')
            } catch (error) {
                console.log(error)
                this.showErrorToast(error)
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
                this.updatedWorkingShift.code =
                    this.updatedWorkingShift.code.trim()
                this.updatedWorkingShift.name =
                    this.updatedWorkingShift.name.trim()
                const res = await this.$services.put(
                    `/working-shifts/${this.workingShiftId}`,
                    this.updatedWorkingShift
                )
                this.showSuccessToast(res.data)
                this.stopEdit()
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
                    title: this.$t(
                        `categories.workingShifts.error.${data.errorCode}`
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
                    title: this.$t('categories.workingShifts.error.UpdateFail'),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(
                        `categories.workingShifts.error.${error.message}`
                    ),
                },
            })
        },
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getWorkingShift()
        },
    },
}
</script>

<style lang="scss"></style>
