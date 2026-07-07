<template>
    <b-card no-body>
        <b-card-body>
            <b-row>
                <b-col md="6">
                    <b-form-group
                        :label="$t('Table.AccessDate')"
                        label-for="h-camera-code"
                        label-cols-md="4"
                    >
                        <b-form-input
                            id="h-camera-code"
                            v-model="event.accessTime"
                            readonly
                        />
                    </b-form-group>
                </b-col>
                <b-col md="6">
                    <b-form-group
                        :label="$t('Table.AreaName')"
                        label-for="h-camera-code"
                        label-cols-md="4"
                    >
                        <b-form-input
                            id="h-camera-code"
                            v-model="event.areaName"
                            readonly
                        />
                    </b-form-group>
                </b-col>
                <b-col md="6">
                    <b-form-group
                        :label="$t('Table.DeviceName')"
                        label-for="h-camera-code"
                        label-cols-md="4"
                    >
                        <b-form-input
                            id="h-camera-code"
                            v-model="event.deviceName"
                            readonly
                        />
                    </b-form-group>
                </b-col>
                <b-col md="6">
                    <b-form-group
                        :label="$t('Table.Classify')"
                        label-for="h-camera-code"
                        label-cols-md="4"
                    >
                        <b-form-input
                            id="h-camera-code"
                            v-model="warningLevelName"
                            readonly
                        />
                    </b-form-group>
                </b-col>
                <b-col md="6">
                    <b-form-group
                        :label="$t('Table.Status')"
                        label-for="h-camera-code"
                        label-cols-md="4"
                    >
                        <b-form-input
                            id="h-camera-code"
                            v-model="safeText"
                            readonly
                        />
                    </b-form-group>
                </b-col>
                <b-col cols="12">
                    <b-form-group
                        :label="$t('FaceEvent.Detail.Image')"
                        label-cols="12"
                        label-class="mb-1"
                    >
                        <media-swiper :uuid="eventId" />
                    </b-form-group>
                </b-col>
                <b-col md="12" class="text-center">
                    <b-button
                        :to="{ path: '/event/protectiveEquipmentEvent/list' }"
                        type="button"
                        class="mx-50 mb-50 btn-120"
                        variant="outline-secondary"
                    >
                        {{ this.$t('Button.Back') }}
                    </b-button>
                </b-col>
            </b-row>
        </b-card-body>
    </b-card>
</template>

<script>
// import ProtectiveEquipment from './warningLevelData'
import moment from 'moment'

export default {
    data() {
        return {
            event: {
                accessTime: '',
            },
            warningLevelName: null,
            ProtectiveEquipment: {
                200: 'ProtectiveEquipmentType.Enough',
                201: 'ProtectiveEquipmentType.Helmet',
                202: 'ProtectiveEquipmentType.FaceMask',
                203: 'ProtectiveEquipmentType.Gloves',
                204: 'ProtectiveEquipmentType.HelmetGloves',
                205: 'ProtectiveEquipmentType.HelmetMask',
                206: 'ProtectiveEquipmentType.MaskGloves',
                207: 'ProtectiveEquipmentType.MissingEverything',
            },
        }
    },
    computed: {
        eventId() {
            return this.$route.params.id
        },
    },
    async created() {
        await this.loadEventType()
        await this.loadEventDetail()
        this.warningLevelName = this.$t(
            this.ProtectiveEquipment[this.event.warningLevelId] ||
                'ProtectiveEquipmentType.Unknown'
        )
        this.safeText =
            this.event.warningLevelId === 200
                ? this.$t('ProtectiveEquipmentType.Safe')
                : this.$t('ProtectiveEquipmentType.Unsafe')
    },
    methods: {
        async loadEventType() {
            const response = await this.$services.get('/lookup/eventType')
            if (response.data.data.length > 0) {
                this.listEventType = response.data.data.filter(
                    (item) => item.eventTypeId === this.eventTypeId
                )
            }
        },
        async loadEventDetail() {
            const response = await this.$services.get(`/event/${this.eventId}`)
            debugger
            this.event = response.data.data
            this.event.accessTime = moment.utc(this.event.accessTime).format('DD/MM/YYYY HH:mm:ss')

        },
    },
}
</script>

<style></style>
