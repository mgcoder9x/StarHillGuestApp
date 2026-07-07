<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Events.Table.Time')"
                                label-for="h-camera-code"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceCode"
                                >
                                    <b-form-input
                                        id="h-camera-code"
                                        v-model="event.accessTime"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Events.Table.Device')"
                                label-for="h-camera-name"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceName"
                                >
                                    <b-form-input
                                        id="h-camera-name"
                                        v-model="event.deviceName"
                                        :state="
                                            errors.length > 0 ? false : null
                                        "
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Events.Table.Area')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceArea"
                                >
                                    <b-form-input
                                        v-if="!editing"
                                        v-model="event.areaName"
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('Events.Table.EventType')"
                                label-for="h-camera-event-type"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceEventType"
                                >
                                    <b-form-input
                                        v-if="!editing && $i18n.locale === 'vi'"
                                        v-model="event.eventTypeName"
                                        :disabled="!editing"
                                    />
                                    <b-form-input
                                        v-if="!editing && $i18n.locale === 'en'"
                                        v-model="event.englishEventTypeName"
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col>
                            <b-form-group
                                :label="$t('Events.Table.Image')"
                                label-cols-md="12"
                            >
                                <media-swiper :uuid="eventId" />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="!editing"
                                v-waves
                                :to="{ path: '/event/allEvents/list' }"
                                type="button"
                                variant="outline-secondary"
                                class="mx-50 mb-50 btn-120 btn-hover-linear-secondary border-0"
                            >
                                <Icon
                                    icon="line-md:arrow-small-left"
                                    class="md-icon"
                                />
                                <span class="ml-25">
                                    {{ $t('common.button.back') }}
                                </span>
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
import { $themeConfig } from '@themeConfig'
import moment from 'moment'
const isDevEnv = process.env.NODE_ENV == 'development'
export default {
    mixins: [authorizationMixin],
    components: {},
    setup() {
        // App Name
        const { apiURL } = $themeConfig.app
        return {
            apiURL,
        }
    },
    data() {
        return {
            device: {
                code: null,
                name: null,
                link: null,
                areaId: null,
                serverId: null,
                eventTypeId: null,
                status: null,
                compId: null,
                license: null,
            },
            event: {
                accessTimeStr: null,
                areaName: null,
                deviceName: null,
                eventTypeName: null,
                image: null,
            },
            listArea: [],
            listEventType: [],
            listServer: [],
            editing: false,
            baseURL: isDevEnv ? 'http://localhost:1938' : this.apiURL,
        }
    },
    computed: {
        eventId() {
            return this.$route.params.eventId
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.loadEventDetail()
    },
    methods: {
        loadEventDetail() {
            this.$services.get(`/event/${this.eventId}`).then((response) => {
                this.event = response.data.data
                this.event.accessTime = moment
                    .utc(this.event.accessTime)
                    .format('DD/MM/YYYY HH:mm:ss')
            })
        },
        validationForm() {
            this.$refs.rules.validate().then((success) => {
                if (success) {
                    this.$services
                        .put(`/device/${this.deviceId}`, this.device)
                        .then((response) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Success.Update'),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                            this.cancel()
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${this.$t(
                                        error.response.data.message
                                    )}`,
                                },
                            })
                        })
                }
            })
        },
        //lookup data
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.listEventType = response.data.data
            })
        },
        loadArea() {
            this.$services.get('/lookup/areas').then((response) => {
                this.listArea = response.data.data
            })
        },
        loadServer() {
            this.$services.get('/lookup/server').then((response) => {
                this.listServer = response.data.data
            })
        },
        edit() {
            this.editing = true
        },
        cancel() {
            this.editing = false
            this.loadDeviceDetail()
        },
    },
}
</script>

<style lang="scss"></style>
