<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <div>
                    <div>
                        <b-form
                            v-for="(item, index) in notificationSetting"
                            :key="index"
                            :ref="`form-${index}`"
                            class="repeater-form"
                            @submit.prevent="repeateAgain"
                        >
                            <b-row>
                                <!-- Mẫu thông báo -->
                                <b-col md="4">
                                    <b-form-group :label="$t('System.User.Detail.Label.WarningForm')">
                                        <validation-provider
                                            #default="{ errors }"
                                            :rules="{
                                                required: true,
                                                onlyOneTemplateId: {
                                                    lstNotificationSettings: notificationSetting,
                                                    currentIndex: index,
                                                },
                                            }"
                                            :name="$t('System.User.Detail.Label.WarningForm')"
                                            :vid="`templateId-${index}`"
                                        >
                                            <v-select
                                                v-model="item.templateId"
                                                :dir="$store.state.appConfig.isRTL ? 'rtl' : 'ltr'"
                                                label="text"
                                                :reduce="(item) => item.id"
                                                :options="lstTemplate"
                                                :state="errors.length > 0 ? false : null"
                                            />
                                            <small class="text-danger">{{ errors[0] }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>

                                <!-- Khu vực -->
                                <b-col md="4">
                                    <b-form-group :label="$t('System.User.Detail.Label.Area')">
                                        <validation-provider
                                            #default="{ errors }"
                                            rules="required"
                                            :name="$t('System.User.Detail.Label.Area')"
                                            :vid="`areaId-${index}`"
                                        >
                                            <v-select
                                                v-model="item.areaId"
                                                :dir="$store.state.appConfig.isRTL ? 'rtl' : 'ltr'"
                                                label="text"
                                                multiple
                                                :reduce="(item) => item.id"
                                                :state="errors.length > 0 ? false : null"
                                                :options="lstArea"
                                            />
                                            <small class="text-danger">{{ errors[0] }}</small>
                                        </validation-provider>
                                    </b-form-group>
                                </b-col>

                                <!-- Remove Button -->
                                <b-col lg="2" md="3" class="mb-50">
                                    <b-form-group>
                                        <b-button
                                            v-ripple.400="'rgba(234, 84, 85, 0.15)'"
                                            variant="outline-danger"
                                            class="mt-0 mt-md-2"
                                            @click="removeItem(index)"
                                        >
                                            <feather-icon icon="XIcon" class="mr-25" />
                                            <span>{{ $t('Button.Delete') }}</span>
                                        </b-button>
                                    </b-form-group>
                                </b-col>
                                <b-col cols="12">
                                    <hr />
                                </b-col>
                            </b-row>
                        </b-form>
                    </div>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                type="submit"
                                variant="primary"
                                class="mr-1"
                                @click="repeateAgain"
                            >
                                {{ $t('Button.CreateWarningForm') }}
                            </b-button>
                            <b-button
                                type="submit"
                                variant="primary"
                                class="mr-1"
                                @click.prevent="validationForm"
                            >
                                {{ $t('Button.Save') }}
                            </b-button>
                            <b-button
                                :to="{ path: '/systems/users/list' }"
                                type="reset"
                                variant="outline-secondary"
                            >
                                {{ $t('Button.Cancel') }}
                            </b-button>
                        </b-col>
                    </b-row>
                </div>
            </b-card-body>
        </b-card>
    </validation-observer>
</template>
<script>
/* eslint-disable */
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'
import { heightTransition } from '@core/mixins/ui/transition'
import Ripple from 'vue-ripple-directive'
import { extend } from 'vee-validate'

//Chỉ cho phép chọn 1 template
extend('onlyOneTemplateId', {
    params: ['lstNotificationSettings', 'currentIndex'],
    validate: (value, { lstNotificationSettings, currentIndex }) => {
        let countSetting = lstNotificationSettings.filter((x, i) => {
            return i !== currentIndex && x.templateId === value
        }).length;
        if (countSetting > 0) return false;
        else return true;
    },
    message: 'Mẫu cảnh báo đã tồn tại trong danh sách.',
});

export default {
    directives: {
        Ripple,
    },
    mixins: [heightTransition],
    data() {
        return {
            notificationSetting: [
                {
                    id: 0,
                    recipientId: this.$route.params.userId,
                    templateId: null,
                    areaId: [],
                    eventTypeId: null,
                    email: false,
                    app: false,
                    webbrowser: true,
                    startTime: null,
                    endTime: null
                },
            ],
            users: [
                {
                    email: false,
                    app: false,
                    webbrowser: false,
                    startTime: null,
                    endTime: null
                },
            ],
            nextTodoId: 2,
            lstTemplate: [],
            lstArea: [],
        }
    },
    mounted() {
        this.initTrHeight()
    },
    computed: {
        userId() {
            return this.$route.params.userId
        },
    },
    created() {
        window.addEventListener('resize', this.initTrHeight())
        this.loadNotificationEventTemplate()
        this.loadArea()
        this.getNotificationSetting()
    },
    destroyed() {
        window.removeEventListener('resize', this.initTrHeight())
    },
    methods: {
        //danh sách cấu hình thông báo
        loadNotificationEventTemplate() {
            this.$services
                .get('/lookup/notificationEventTemplates')
                .then((response) => {
                    this.lstTemplate = response.data.data
                })
        },
        //lấy cấu hình thông báo
        getNotificationSetting() {
            var vm = this
            this.$services
                .get(
                    `/notification/getAllNotificationEventSettings/${this.userId}`
                )
                .then((response) => {
                    if (response.data.data.length > 0) {
                        vm.users.app = response.data.data[0].app
                        vm.users.webbrowser = response.data.data[0].webbrowser
                        vm.users.email = response.data.data[0].email
                        vm.users.startTime = response.data.data[0].startTime
                        vm.users.endTime = response.data.data[0].endTime
                        this.notificationSetting = response.data.data
                        if (response.data.data) {
                            this.notificationSetting.map(function (key) {
                                key.templateId = key.templateId.toString()

                                for (let i = 0; i < key.areaId.length; i++) {
                                    key.areaId[i] = key.areaId[i].toString()
                                }
                            })
                        }
                    }

                    //Đẩy dòng
                    if (this.notificationSetting.length > 0) {
                        this.$nextTick(() => {
                            this.trAddHeight(
                                this.$refs.row[0].offsetHeight *
                                    (this.notificationSetting.length - 1)
                            )
                        })
                    }
                })
        },
        //danh sách khu vực
        loadArea() {
            this.$services.get('/lookup/areas').then((response) => {
                this.lstArea = response.data.data
            })
        },
        repeateAgain() {
            let addItem = {
                id: 0,
                recipientId: this.$route.params.userId,
                templateId: null,
                areaId: [],
                eventTypeId: null,
                email: this.users.app,
                app: this.users.app,
                webbrowser: this.users.app,
                startTime: this.users.startTime,
                endTime: this.users.endTime
            }

            this.notificationSetting.push(addItem)

            this.$nextTick(() => {
                this.trAddHeight(this.$refs.row[0].offsetHeight)
            })
        },
        removeItem(index) {
            this.notificationSetting.splice(index, 1)
            this.trTrimHeight(this.$refs.row[0].offsetHeight)
        },
        initTrHeight() {
            this.trSetHeight(null)
            this.$nextTick(() => {
                if (this.$refs.form) {
                    this.trSetHeight(this.$refs.form.scrollHeight)
                }
            })
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .post(
                            `/notification/notificationEventSetting/${this.userId}`,
                            this.notificationSetting
                        )
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: `Cấu hình cảnh báo thành công`,
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.$router.push({ path: '/systems/users/list' })
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: 'Error',
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${this.$t(error.response.data.message)}`,
                                },
                            })
                        })
                }
            })
        },
    },
}
</script>

<style lang="scss" scoped>
.repeater-form {
    transition: 0.35s height;
}
</style>
