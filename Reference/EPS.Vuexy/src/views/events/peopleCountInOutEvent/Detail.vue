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
                                        v-model="event.accessTimeStr"
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
                                :label="this.$t('Events.Table.Direction')"
                                label-for="h-camera-event-type"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceEventType"
                                >
                                    <b-form-input
                                        v-if="!editing"
                                        v-model="event.directionStr"
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
                                :label="this.$t('Events.Table.NumOfPeople')"
                                label-for="h-camera-event-type"
                                label-cols-md="4"
                            >
                                <validation-provider
                                    #default="{ errors }"
                                    rules="required"
                                    name="DeviceEventType"
                                >
                                    <b-form-input
                                        v-if="!editing"
                                        v-model="event.numberOfPeople"
                                        :disabled="!editing"
                                    />
                                    <small class="text-danger">{{
                                        errors[0]
                                    }}</small>
                                </validation-provider>
                            </b-form-group>
                        </b-col>
                        <!-- <b-col md="6">
                            <b-form-group
                                :label="$t('FaceEvent.Detail.Age')"
                                label-for="h-camera-event-age"
                                label-cols-md="4"
                            >
                                <b-form-input :value="age" readonly />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('FaceEvent.Detail.Gender')"
                                label-for="h-camera-gender"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    v-model="event.genderName"
                                    readonly
                                />
                            </b-form-group>
                        </b-col> -->
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-if="!editing"
                                :to="{
                                    path: '/event/peopleCountInOutEvent/list',
                                }"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                            >
                                {{ this.$t('Button.Back') }}
                            </b-button>
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
                directionStr: null,
                numberOfPeople: null,
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
            return this.$route.params.id
        },
        age() {
            return this.event.age <= 0 ? null : this.event.age
        },
    },
    created() {
        const accessToken = this.$services.getUserData()
        this.loadEventDetail()
    },
    methods: {
        loadEventDetail() {
            this.$services
                .get(`/peopleCountInOutEvent/${this.eventId}`)
                .then((response) => {
                    this.event = response.data.data
                    // this.event.genderName = this.getGenderName(
                    //     this.event.gender
                    // )
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
        getGenderName(value) {
            switch (value) {
                case 1:
                    return this.$t('FaceEvent.Detail.Female')
                case 0:
                    return this.$t('FaceEvent.Detail.Male')
            }
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
