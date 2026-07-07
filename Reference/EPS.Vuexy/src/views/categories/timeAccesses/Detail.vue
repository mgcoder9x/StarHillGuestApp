<!-- eslint-disable -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
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
                                        v-model="updateTimeAccess.compId"
                                        :options="compTree"
                                        label="text"
                                        :reduce="(option) => option.id"
                                        :disabled="!editing"
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
                                        v-model.trim="updateTimeAccess.name"
                                        :disabled="!editing"
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
                                v-slot="{ ariaDescribedby }"
                                :label="
                                    $t(
                                        'categories.timeAccesses.common.form.label.weekDays'
                                    )
                                "
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
                                        :disabled="!editing"
                                        :options="weekDayOptions"
                                        :aria-describedby="ariaDescribedby"
                                        buttons
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
                                <div
                                    class="font-weight-bold text-center d-flex align-items-center justify-content-center"
                                >
                                    <h6>
                                        {{
                                            `${$t('categories.timeAccesses.common.form.label.timeSlot')}
                                        ${index + 1}`
                                        }}
                                        <button
                                            @click="
                                                timeSlotsTemp[weekDay].splice(
                                                    index,
                                                    1
                                                )
                                            "
                                            :disabled="!editing"
                                        >
                                            <Icon
                                                icon="gravity-ui:delete"
                                                style="color: #ff0000"
                                            />
                                        </button>
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
                                            v-model="itimeSlot.startTime"
                                            :disabled="!editing"
                                            placeholder=""
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
                                            v-model="itimeSlot.endTime"
                                            :disabled="!editing"
                                            placeholder=""
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
                                    v-if="
                                        authorize(['ManageTimeAccess']) &&
                                        editing
                                    "
                                    variant="primary"
                                    @click="initTimeSlot(weekDay)"
                                >
                                    {{ $t('Button.Create') }}
                                </b-button>
                            </b-col>
                        </b-row>
                    </div>
                    <b-row>
                        <b-col class="text-center">
                            <Transition mode="out-in">
                                <b-button
                                    v-if="
                                        editing &&
                                        authorize(['ManageTimeAccess'])
                                    "
                                    type="button"
                                    variant="primary"
                                    class="mx-50 mb-50 btn-120"
                                    @click="onSubmit"
                                >
                                    {{ $t('common.button.save') }}
                                </b-button>
                                <b-button
                                    v-if="
                                        !editing &&
                                        authorize(['ManageTimeAccess'])
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
                                :to="{ path: '/categories/timeAccesses/list' }"
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
import dayOfWeek from '@/constants/dayOfWeek'
import { ValidationObserver, ValidationProvider, extend } from 'vee-validate'

export default {
    components: { ValidationObserver, ValidationProvider },
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
            updateTimeAccess: {
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
            editing: false,
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
        timeAccessId() {
            return this.$route.params.timeAccessId
        },
        formGroupClass() {
            return 'mb-50 mb-md-1'
        },
        language() {
            return this.$i18n.locale
        },
    },
    async created() {
        extend('endTimeGreater', {
            params: ['startTime'],
            validate(value, { startTime }) {
                if (!value || !startTime) return true // Bỏ qua nếu một trong hai rỗng
                return value > startTime
            },
            message: 'Giờ kết thúc phải lớn hơn giờ bắt đầu',
        })
        this.loadCompanyTree()
        await this.getTimeAccess()
    },
    methods: {
        initTimeSlot(weekDay) {
            var slot = {
                startTime: null,
                endTime: null,
            }
            this.timeSlotsTemp[weekDay].push(slot)
        },
        loadCompanyTree() {
            this.$services.get('/lookup/company-tree').then((response) => {
                this.compTree = response.data
            })
        },
        async getTimeAccess() {
            try {
                const res = await this.$services.get(
                    `/timeAccesses/${this.timeAccessId}`
                )
                this.updateTimeAccess = res.data.data
                const timeSlots = this.nestTimeSlots(
                    this.updateTimeAccess.timeSlots
                )
                Object.keys(timeSlots).forEach((key) => {
                    this.timeSlotsTemp[key] = timeSlots[key]
                })
            } catch (error) {
                console.log('error')
            }
        },
        trimField(field) {
            return field && field.trim() ? field.trim() : null
        },
        onSubmit() {
            this.$refs.rules.validate().then(async (isValid) => {
                this.updateTimeAccess.name = this.trimField(
                    this.updateTimeAccess.name
                )
                if (isValid) {
                    try {
                        this.updateTimeAccess.timeSlots = this.flattenTimeSlots(
                            this.timeSlotsTemp
                        )
                        const res = await this.$services.put(
                            '/timeAccesses/' + this.timeAccessId,
                            this.updateTimeAccess
                        )
                        this.showSuccessToast(res.data)
                        this.stopEdit()
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
        startEdit() {
            this.editing = true
        },
        stopEdit() {
            this.editing = false
            this.getTimeAccess()
        },
        // Hàm trải phẳng về dạng flatten
        flattenTimeSlots(data) {
            const result = []
            for (const [weekDays, slots] of Object.entries(data)) {
                slots.forEach((slot) => {
                    result.push({ weekDays: parseInt(weekDays), ...slot })
                })
            }
            return result
        },
        // Hàm để chuyển đổi mảng phẳng về dạng nested
        nestTimeSlots(array) {
            const result = {}
            var weekDaySelected = []
            array.forEach((item) => {
                const { weekDays, startTime, endTime } = item
                if (!result[weekDays]) {
                    weekDaySelected.push(weekDays)
                    result[weekDays] = []
                }
                result[weekDays].push({ startTime, endTime })
            })
            this.weekDaySelected = weekDaySelected.sort((a, b) => a - b)
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
