<!-- eslint-disable vue/html-self-closing -->
<template>
    <b-card no-body>
        <b-card-body>
            <b-form>
                <b-row>
                    <b-col md="6">
                        <b-form-group
                            :label="
                                $t('TrafficViolationEvent.Detail.AccessTime')
                            "
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.accessTime" readonly />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="
                                $t('TrafficViolationEvent.Detail.LicensePlate')
                            "
                            label-cols-md="4"
                        >
                            <b-form-input
                                v-model="event.licensePlate"
                                readonly
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="
                                $t('TrafficViolationEvent.Detail.WarningLevel')
                            "
                            label-cols-md="4"
                        >
                            <b-form-input
                                v-model="event.warningLevel"
                                readonly
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="
                                $t('TrafficViolationEvent.Detail.VehicleType')
                            "
                            label-cols-md="4"
                        >
                            <b-form-input
                                v-model="vehicleType[event.vehicleType]"
                                readonly
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('TrafficViolationEvent.Detail.AreaName')"
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.areaName" readonly />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('TrafficViolationEvent.Detail.Device')"
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.deviceName" readonly />
                        </b-form-group>
                    </b-col>
                    <b-col cols="12">
                        <b-form-group
                            :label="$t('TrafficViolationEvent.Detail.Image')"
                            label-cols="12"
                            label-class="mb-1"
                        >
                            <media-swiper :uuid="eventId" />
                        </b-form-group>
                    </b-col>
                    <b-col md="12" class="text-center">
                        <b-button
                            :to="{ path: '/event/faceEvent/list' }"
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
</template>

<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import { $themeConfig } from '@themeConfig'

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
            event: {
                accessTime: null,
                licensePlate: null,
                warningLevelId: null,
                vehicleType: null,
                areaName: null,
                deviceName: null,
            },
            vehicleType: {
                1: this.$t('common.options.vehicleType.motorbike'),
                2: this.$t('common.options.vehicleType.car'),
            },
        }
    },
    computed: {
        eventId() {
            return this.$route.params.id
        },
    },
    async created() {
        await this.loadEventDetail()
    },
    methods: {
        loadEventDetail() {
            this.$services
                .get(`/trafficViolationEvents/${this.eventId}`)
                .then((response) => {
                    this.event = response.data.data
                    this.event.accessTime = this.$moment(
                        this.event.accessTime
                    ).format('DD/MM/YYYY HH:mm:ss')
                    // this.event.statusName = response.data.data.status =
                    //     this.getStatusName(response.data.data.status)
                })
        },
    },
}
</script>

<style lang="scss"></style>
