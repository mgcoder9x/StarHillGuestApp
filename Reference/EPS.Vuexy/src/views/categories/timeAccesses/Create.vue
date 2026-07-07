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
                                        'categories.timeAccesses.common.form.label.compName'
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
                                            'categories.timeAccesses.common.form.label.compName'
                                        )
                                    "
                                >
                                    <tree-select
                                        v-model="newTimeAccess.compId"
                                        :options="compTree"
                                        label="text"
                                        :reduce="(option) => option.id"
                                        :placeholder="
                                            $t(
                                                'categories.timeAccesses.common.form.placeholder.compName'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <!-- Tên khung giờ -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.timeAccesses.common.form.label.name'
                                    )
                                "
                                label-for="h-timeAccess-code"
                                label-cols-md="4"
                                label-class="required"
                                :class="formGroupClass"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.timeAccesses.common.form.label.name'
                                        )
                                    "
                                >
                                    <b-form-input
                                        v-model.trim="newTimeAccess.name"
                                        :placeholder="
                                            $t(
                                                'categories.timeAccesses.common.form.placeholder.name'
                                            )
                                        "
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <!-- Chọn khung giờ -->
                        <b-col md="12">
                            <b-form-group
                                :label="
                                    $t(
                                        'categories.timeAccesses.common.form.label.weekDays'
                                    )
                                "
                                v-slot="{ ariaDescribedby }"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="
                                        $t(
                                            'categories.timeAccesses.common.form.label.weekDays'
                                        )
                                    "
                                >
                                    <b-form-checkbox-group
                                        v-model="weekDaySelected"
                                        :options="weekDayOptions"
                                        :aria-describedby="ariaDescribedby"
                                        buttons
                                        button-style="margin-right: 20px"
                                        button-variant="primary"
                                    ></b-form-checkbox-group>
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <hr />
                    <!-- ----WeekDays---- -->
                    <div
                        v-for="(weekDay, weekDayIndex) in weekDaySelected
                            .slice()
                            .sort((a, b) => a - b)"
                        :key="weekDayIndex"
                    >
                        <hr />
                        <h2 class="font-weight-bold text-center">
                            {{ getWeekDayText(weekDay) }}
                        </h2>
                        <b-row cols="2" align-h="start" class="m-2">
                            <b-col
                                v-for="(itimeSlot, index) in timeSlotsTemp[
                                    weekDay
                                ]"
                                :key="index"
                                md="3"
                            >
                                <div class="font-weight-bold text-center">
                                    <h6>
                                        {{
                                            `${$t('categories.timeAccesses.common.form.label.timeSlot')}
                                        ${index + 1}`
                                        }}
                                        <a
                                            @click="
                                                timeSlotsTemp[weekDay].splice(
                                                    index,
                                                    1
                                                )
                                            "
                                            ><Icon
                                                icon="gravity-ui:delete"
                                                style="color: #ff0000"
                                        /></a>
                                    </h6>
                                </div>
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    :name="'S' + weekDay + '.' + index"
                                >
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.timeAccesses.common.form.label.startTime'
                                            )
                                        "
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <b-form-timepicker
                                            placeholder=""
                                            v-model="itimeSlot.startTime"
                                            locale="vi"
                                            :state="
                                                errors.length > 0 ? false : null
                                            "
                                        />
                                        <small class="text-danger">{{
                                            errors[0] ? 'Bắt buộc nhập' : null
                                        }}</small>
                                    </b-form-group>
                                </validation-provider>
                                <validation-provider
                                    #default="{ errors }"
                                    :rules="`required|time_after:${itimeSlot.startTime}`"
                                    :name="'E' + weekDay + '.' + index"
                                >
                                    <b-form-group
                                        :label="
                                            $t(
                                                'categories.timeAccesses.common.form.label.endTime'
                                            )
                                        "
                                        label-cols-md="4"
                                        label-class="required"
                                        :class="formGroupClass"
                                    >
                                        <b-form-timepicker
                                            placeholder=""
                                            v-model="itimeSlot.endTime"
                                            locale="vi"
                                            :state="
                                                errors.length > 0 ? false : null
                                            "
                                        />
                                        <small class="text-danger">{{
                                            errors[0] || null
                                        }}</small>
                                    </b-form-group>
                                </validation-provider>
                            </b-col>
                            <b-col
                                md="3"
                                class="d-flex align-items-center justify-content-center"
                            >
                                <b-button
                                    v-if="authorize(['ManageTimeAccess'])"
                                    variant="primary"
                                    @click="initTimeSlot(weekDay)"
                                >
                                    {{ $t('Button.Create') }}
                                </b-button>
                            </b-col>
                        </b-row>
                    </div>
                    <!-- Button Action -->
                    <div class="text-center">
                        <b-button
                            v-if="authorize(['ManageTimeAccess'])"
                            type="submit"
                            variant="primary"
                            title="Save"
                            class="mx-50 mb-50 btn-120"
                        >
                            {{ $t('common.button.save') }}
                        </b-button>
                        <b-button
                            @click="navigateTotimeAccessList()"
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
/**
 * Định dạng dữ liệu cho thời gian slot.
 *
 * @param {object} timeSlotsTemp - Đối tượng chứa các thời gian slot phân theo ngày trong tuần.
 *
 * @example
 * const timeSlotsTemp = {
 *     2: [
 *         { startTime: '08:00', endTime: '10:00' },
 *         { startTime: '10:30', endTime: '12:00' }
 *     ],
 *     3: [
 *         { startTime: '09:00', endTime: '11:00' }
 *     ],
 *     4: [
 *         { startTime: '13:00', endTime: '15:00' }
 *     ],
 *     5: [],
 *     6: [
 *         { startTime: '10:00', endTime: '12:00' },
 *         { startTime: '14:00', endTime: '16:00' }
 *     ],
 *     7: [],
 *     8: [
 *         { startTime: '09:00', endTime: '11:00' },
 *         { startTime: '12:00', endTime: '14:00' }
 *     ]
 * };
 */

import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { authorizationMixin } from '@core/mixins/ui/forms'
import dayOfWeek from '@/constants/dayOfWeek'

export default {
    components: {},
    mixins: [authorizationMixin],
    data() {
        const weekDayOptions = dayOfWeek
            ? dayOfWeek.map((day) => ({
                  ...day,
                  text: this.$t(`dayOfWeek.${day.text}`),
              }))
            : []
        return {
            compTree: [],
            newTimeAccess: {
                compId: null,
                name: null,
                timeSlots: [],
            },
            timeSlotsTemp: {
                1: [],
                2: [],
                3: [],
                4: [],
                5: [],
                6: [],
                0: [],
            },
            weekDaySelected: [],
            weekDayOptions,
        }
    },
    watch: {
        // Xóa dữ liệu timeSlotsTemp bị bỏ chọn
        weekDaySelected(val, oldVal) {
            const unUsed = oldVal.find((item) => !val.includes(item))
            if (unUsed) {
                this.timeSlotsTemp[unUsed] = []
            }
        },
        language() {
            this.weekDayOptions = dayOfWeek
                ? dayOfWeek.map((day) => ({
                      ...day,
                      text: this.$t(`dayOfWeek.${day.text}`),
                  }))
                : []
        },
    },
    computed: {
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        language() {
            return this.$i18n.locale
        },
    },
    async created() {
        this.loadCompanyTree()
        const accessToken = this.$services.getUserData()
        this.newTimeAccess.compId = accessToken.companyId
    },
    methods: {
        loadCompanyTree() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.compTree = response.data
            })
        },
        initTimeSlot(weekDay) {
            var slot = {
                startTime: null,
                endTime: null,
            }
            this.timeSlotsTemp[weekDay].push(slot)
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        onSubmit(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                this.newTimeAccess.name = this.trimField(
                    this.newTimeAccess.name
                )
                if (!success) {
                    // handle validation errors...
                } else {
                    try {
                        this.newTimeAccess.timeSlots = this.flattenTimeSlots(
                            this.timeSlotsTemp
                        )
                        const res = await this.$services.post(
                            '/timeAccesses',
                            this.newTimeAccess
                        )
                        this.showSuccessToast(res.data)
                        this.navigateTotimeAccessList()
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
                        `categories.timeAccesses.error.${data.errorCode}`
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
                        `categories.timeAccesses.error.${error.response.data.errorCode}`
                    ),
                },
            })
        },
        navigateTotimeAccessList() {
            this.$router.push({ path: '/categories/timeAccesses/list' })
        },
        flattenTimeSlots(data) {
            const result = []
            for (const [weekDays, slots] of Object.entries(data)) {
                slots.forEach((slot) => {
                    result.push({ weekDays: parseInt(weekDays), ...slot })
                })
            }
            return result
        },
        getWeekDayText(value) {
            const day = this.weekDayOptions.find((x) => x.value === value)
            return day ? day.text : ''
        },
    },
}
</script>

<style lang="scss"></style>
