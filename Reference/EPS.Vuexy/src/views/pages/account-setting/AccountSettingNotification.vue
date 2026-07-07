<template>
    <b-card no-body>
        <b-card-body>
            <validation-observer ref="rules">
                <b-form>
                    <b-row>
                        <b-col cols="12" class="mb-2">
                            <b-form-checkbox
                                id="accountSwitch1"
                                v-model="notification.email"
                                name="check-button"
                                switch
                                inline
                            >
                                <span>Email</span>
                            </b-form-checkbox>
                        </b-col>
                        <b-col cols="12" class="mb-2">
                            <b-form-checkbox
                                id="accountSwitch2"
                                v-model="notification.webbrowser"
                                name="check-button"
                                switch
                                inline
                            >
                                <span>Browser</span>
                            </b-form-checkbox>
                        </b-col>
                        <b-col cols="12" class="mb-2">
                            <b-form-checkbox
                                id="accountSwitch3"
                                v-model="notification.app"
                                name="check-button"
                                switch
                                inline
                            >
                                <span>App</span>
                            </b-form-checkbox>
                        </b-col>
                        <b-col cols="12" class="mb-2">
                            <b-form-checkbox
                                id="accountSwitch4"
                                v-model="notification.zalo"
                                name="check-button"
                                switch
                                inline
                            >
                                <span>Zalo</span>
                            </b-form-checkbox>
                        </b-col>

                        <b-col cols="12" class="mb-2">
                            <b-form-checkbox
                                id="accountSwitch5"
                                v-model="notification.sound"
                                name="check-button"
                                switch
                                inline
                            >
                                <span>{{
                                    $t('AccountSetting.Feature.Sound')
                                }}</span>
                            </b-form-checkbox>
                        </b-col>

                        <b-col cols="3" class="mb-2">
                            <validation-provider
                                rules="required"
                                name="Giờ bắt đầu"
                                v-slot="{ errors }"
                            >
                                <b-form-group
                                    :label="$t('AccountSetting.Time.Start')"
                                    label-cols-md="4"
                                    label-class="required"
                                >
                                    <b-form-timepicker
                                        v-model="notification.startTime"
                                        locale="vi"
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

                        <b-col cols="3" class="mb-2">
                            <validation-provider
                                rules="required"
                                name="Giờ kết thúc"
                                v-slot="{ errors }"
                            >
                                <b-form-group
                                    :label="$t('AccountSetting.Time.End')"
                                    label-cols-md="4"
                                    label-class="required"
                                >
                                    <b-form-timepicker
                                        v-model="notification.endTime"
                                        locale="vi"
                                        :state="
                                            errors.length > 0 || isTimeInvalid
                                                ? false
                                                : null
                                        "
                                    />
                                    <small class="text-danger">
                                        {{
                                            errors[0] ||
                                            (isTimeInvalid
                                                ? $t(
                                                      'AccountSetting.Time.InValid'
                                                  )
                                                : '')
                                        }}
                                    </small>
                                </b-form-group>
                            </validation-provider>
                        </b-col>

                        <b-col cols="12">
                            <b-button
                                type="submit"
                                variant="primary"
                                class="mr-1"
                                @click.prevent="updateNotificationSetting"
                            >
                                {{ $t('Button.Save') }}
                            </b-button>
                        </b-col>
                    </b-row>
                </b-form>
            </validation-observer>
        </b-card-body>
    </b-card>
</template>

<script>
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import notification from '@/router/routes/notification'
import {
    BButton,
    BRow,
    BCol,
    BCard,
    BFormCheckbox,
    BFormTimepicker,
} from 'bootstrap-vue'
import { ValidationObserver, ValidationProvider } from 'vee-validate'
import { required } from '@validations'

export default {
    components: {
        BButton,
        BRow,
        BCol,
        BCard,
        BFormCheckbox,
        BFormTimepicker,
        ValidationObserver,
        ValidationProvider,
    },
    props: {
        notificationData: {
            type: Object,
            default: () => {},
        },
    },
    data() {
        return {
            notification: {
                email: false, // Sử dụng giá trị mặc định là false
                webbrowser: false,
                app: false,
                zalo: false,
                sound: false, // Thêm trường dữ liệu cho âm thanh
                startTime: null,
                endTime: null,
            },
            required, // Khai báo rule required
        }
    },
    computed: {
        isTimeInvalid() {
            const start = this.notification.startTime
            const end = this.notification.endTime
            if (!start || !end) return false
            return end <= start
        },
    },
    created() {
        this.getNotificationSetting()
    },
    methods: {
        getNotificationSetting() {
            const accessToken = this.$services.getUserData()
            const infoId = accessToken.userId

            this.$services
                .get(`/notification/notificationSetting/${infoId}`)
                .then((rs) => {
                    if (rs.data.data != null) {
                        // Chuyển đổi các giá trị số từ API thành boolean
                        const data = rs.data.data
                        this.notification = {
                            email: data.email === true,
                            webbrowser: data.webbrowser === true,
                            app: data.app === true,
                            zalo: data.zalo === true,
                            sound: data.sound === true, // Xử lý trường mới
                            startTime: data.startTime || '00:00:00',
                            endTime: data.endTime || '23:59:59',
                        }
                    }
                })
        },
        updateNotificationSetting() {
            this.$refs.rules.validate().then((valid) => {
                if (!valid || this.isTimeInvalid) return

                const accessToken = this.$services.getUserData()
                const infoId = accessToken.userId

                const startTime = this.notification.startTime
                    ? this.notification.startTime.split(':').length === 2
                        ? this.notification.startTime + ':00'
                        : this.notification.startTime
                    : '00:00:00'
                const endTime = this.notification.endTime
                    ? this.notification.endTime.split(':').length === 2
                        ? this.notification.endTime + ':00'
                        : this.notification.endTime
                    : '23:59:59'
                // Chuẩn bị dữ liệu để gửi đi, chuyển boolean thành 0 hoặc 1
                const payload = {
                    ...this.notification,
                    email: this.notification.email ? 1 : 0,
                    webbrowser: this.notification.webbrowser ? 1 : 0,
                    app: this.notification.app ? 1 : 0,
                    zalo: this.notification.zalo ? 1 : 0,
                    sound: this.notification.sound ? 1 : 0, // Xử lý trường mới
                    endTime: endTime,
                    startTime: startTime,
                    startTime: this.notification.startTime || '00:00:00',
                    endTime: this.notification.endTime || '23:59:59',
                }

                this.$services
                    .put(`/notification/notificationSetting/${infoId}`, payload)
                    .then(() => {
                        this.$toast({
                            component: ToastificationContent,
                            position: 'top-right',
                            props: {
                                title: this.$t('Success.CreateNotification'),
                                icon: 'CheckIcon',
                                variant: 'success',
                            },
                        })
                    })
                    .catch((error) => {
                        this.$toast({
                            component: ToastificationContent,
                            position: 'top-right',
                            props: {
                                title: this.$t('Error.Error'),
                                icon: 'AlertTriangleIcon',
                                variant: 'danger',
                                text: `${this.$t(error.response?.data?.message || 'Lỗi không xác định')}`,
                            },
                        })
                    })
            })
        },
    },
}
</script>
