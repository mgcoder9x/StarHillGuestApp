<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form @submit="onSubmit">
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.workingShifts.common.form.label.code'
                                    )
                                "
                                label-for="create-working-shift-code"
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
                                        id="create-working-shift-code"
                                        v-model="newWorkingShift.code"
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
                                label-for="create-working-shift-name"
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
                                        id="create-working-shift-name"
                                        v-model="newWorkingShift.name"
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
                                    validateStartTime: newWorkingShift.endTime,
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
                                    label-for="create-area-parent"
                                    label-cols-md="3"
                                    label-class="required"
                                >
                                    <date-picker
                                        id="h-searchForm-dateFrom"
                                        v-model="newWorkingShift.startTime"
                                        type="time"
                                        :locale="currentLocale"
                                        format="HH:mm"
                                        value-type="HH:mm:ss"
                                        style="width: 100%"
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
                                    validateEndTime: newWorkingShift.startTime,
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
                                    label-for="h-camera-event-type"
                                    label-cols-md="3"
                                    label-class="required"
                                >
                                    <date-picker
                                        id="h-searchForm-dateFrom"
                                        v-model="newWorkingShift.endTime"
                                        type="time"
                                        :locale="currentLocale"
                                        format="HH:mm"
                                        value-type="HH:mm:ss"
                                        style="width: 100%"
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
                                    label-for="create-area-name"
                                    label-cols-md="3"
                                    label-class="required"
                                >
                                    <v-select
                                        v-model="newWorkingShift.status"
                                        :options="statusOptions"
                                        label="text"
                                        :reduce="(opt) => opt.value"
                                        :clearable="false"
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
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageWorkingShift'])"
                            v-waves
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120 btn-hover-linear-primary border-0"
                        >
                            <Icon
                                icon="material-symbols:save-outline"
                                class="sm-icon"
                            />
                            <span class="ml-25">
                                {{ $t('common.button.save') }}
                            </span>
                        </b-button>
                        <b-button
                            v-waves
                            :to="{ path: '/categories/workingShifts/list' }"
                            type="reset"
                            variant="secondary"
                            title="Cancel"
                            class="btn-120 mb-50 btn-hover-linear-secondary border-0"
                        >
                            <Icon icon="line-md:cancel" class="sm-icon" />
                            <span class="ml-25">
                                {{ $t('common.button.cancel') }}
                            </span>
                        </b-button>
                    </div>
                </b-form>
            </b-card-body>
        </b-card>
    </validation-observer>
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
            newWorkingShift: {
                code: null,
                name: null,
                startTime: null,
                endTime: null,
                status: 1,
                compId: null,
            },
        }
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.newWorkingShift.compId = accessToken.companyId
    },
    computed: {
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
    methods: {
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then((success) => {
                if (!success) {
                    return
                } else {
                    this.submitForm()
                }
            })
        },
        async submitForm() {
            try {
                this.newWorkingShift.code = this.newWorkingShift.code.trim()
                this.newWorkingShift.name = this.newWorkingShift.name.trim()
                const res = await this.$services.post(
                    '/working-shifts',
                    this.newWorkingShift
                )
                this.showSuccessToast(res.data)
                this.navigateToWorkingShiftsList()
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
                    title: this.$t('categories.workingShifts.error.CreateFail'),
                    icon: 'AlertTriangleIcon',
                    variant: 'danger',
                    text: this.$t(
                        `categories.workingShifts.error.${error.message}`
                    ),
                },
            })
        },
        navigateToWorkingShiftsList() {
            this.$router.push({ path: '/categories/workingShifts/list' })
        },
    },
}
</script>

<style lang="scss"></style>
