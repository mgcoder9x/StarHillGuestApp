<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('FireWorkEvent.Detail.Date')"
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
                                        v-model="event.accessDateStr"
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
                                :label="$t('FireWorkEvent.Detail.Device')"
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
                                :label="$t('FireWorkEvent.Detail.CascadeFW')"
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
                                        v-model="event.cascadeFWName"
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
                                :label="$t('FireWorkEvent.Detail.ClassFW')"
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
                                        v-model="event.classFWName"
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
                                :label="$t('FireWorkEvent.Detail.FireWorkType')"
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
                                        v-model="event.fireWorkStr"
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
                                :label="$t('FireWorkEvent.Detail.Hole')"
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
                                        v-model="event.holeIds"
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
                                :label="$t('FireWorkEvent.Detail.Image')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                            >
                                <img
                                    :src="`${baseURL}${event.filePath}`"
                                    loading="lazy"
                                    alt="Image"
                                    width="150px"
                                    height="120px"
                                    style="margin: 0 auto"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('FireWorkEvent.Detail.Status')"
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
                                        v-model="event.statusStr"
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
                        <b-col class="text-center">
                            <b-button
                                v-if="!editing"
                                @click="handleBack"
                                type="button"
                                class="mx-50 mb-50 btn-120"
                                variant="outline-secondary"
                            >
                                {{ this.$t('Button.Back') }}
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
            event:{
                accessTimeStr: null,
                areaName: null,
                deviceName: null,
                eventTypeName: null,
                image: null,
                accessDateStr: null,
                licensePlates: null,
                colorStr: null,
                manufacturerName: null,
                carTypeStr: null,
                vehicleTypeStr: null,
                isBlackListStr: null,
                totalNumberOfSeats: null,
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
        handleBack() {
            debugger
            const from = this.$route.query?.from;

            if (from === 'Screen1') {
                this.$router.push('/event/fireWorkEvents/List');
            } else {
                this.$router.push('/notification/allNotification/list');
            }
        },
        loadEventDetail() {
            this.$services.get(`/fireWorkEvents/${this.eventId}`).then((response) => {
                response.data.data.fireWorkStr = this.$t(response.data.data.fireWorkStr)
                response.data.data.statusStr = this.$t(response.data.data.statusStr)
                this.event = response.data.data
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
