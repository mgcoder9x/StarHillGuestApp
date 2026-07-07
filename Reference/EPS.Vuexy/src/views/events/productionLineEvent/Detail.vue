<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <!-- Time -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Events.Table.Time')"
                                label-for="h-event-time"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-event-time"
                                    v-model="event.accessTimeStr"
                                    disabled
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Step -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('ProductionLineEvent.Table.Step')"
                                label-for="h-event-step"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-event-step"
                                    v-model="event.stepName"
                                    disabled
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Production Line -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t(
                                        'ProductionLineEvent.Table.ProductionLine'
                                    )
                                "
                                label-for="h-event-productionLine"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-event-productionLine"
                                    v-model="event.productionLineName"
                                    disabled
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Device -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Events.Table.Device')"
                                label-for="h-event-device"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-event-device"
                                    v-model="event.deviceName"
                                    disabled
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Person -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('ProductionLineEvent.Table.Person')"
                                label-for="h-event-person"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-event-person"
                                    v-model="event.personNo"
                                    disabled
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Event Type -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t('ProductionLineEvent.Table.EventType')
                                "
                                label-for="h-event-type"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-event-type"
                                    v-model="event.eventTypeName"
                                    disabled
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Warning Level -->
                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t('ProductionLineEvent.Table.WarningLevel')
                                "
                                label-for="h-event-warning"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-event-warning"
                                    v-model="event.warningLevelName"
                                    disabled
                                />
                            </b-form-group>
                        </b-col>
                        <!-- Status -->
                        <b-col md="6">
                            <b-form-group
                                :label="$t('ProductionLineEvent.Table.Status')"
                                label-for="h-event-status"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-event-status"
                                    :value="statusText"
                                    disabled
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col>
                            <b-form-group
                                :label="$t('Events.Table.Image')"
                                label-cols-md="12"
                            >
                                <media-swiper
                                    v-if="event.eventId"
                                    :uuid="event.eventId"
                                />
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <b-row>
                        <b-col class="text-center">
                            <b-button
                                v-waves
                                :to="{
                                    path: '/event/productionLineEvent/list',
                                }"
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
import { authorizationMixin } from '@core/mixins/ui/forms'
import { $themeConfig } from '@themeConfig'
import moment from 'moment'

export default {
    mixins: [authorizationMixin],
    setup() {
        const { apiURL } = $themeConfig.app
        return {
            apiURL,
        }
    },
    data() {
        return {
            event: {
                id: null,
                eventId: null,
                accessTime: null,
                accessTimeStr: null,
                stepName: null,
                personNo: null,
                eventTypeName: null,
                productionLineName: null,
                deviceName: null,
                warningLevelName: null,
                status: null,
            },
        }
    },
    computed: {
        eventId() {
            return this.$route.params.id
        },
        baseURL() {
            return process.env.VUE_APP_BASE_URL
        },
        statusText() {
            if (this.event.status === 1) {
                return this.$t('ProductionLineEvent.Status.Completed')
            }
            if (this.event.status === 0) {
                return this.$t('ProductionLineEvent.Status.Violation')
            }
            return ''
        },
    },
    created() {
        this.loadEvent()
    },
    methods: {
        loadEvent() {
            this.$services
                .get(`/production-line-event/${this.eventId}`)
                .then((response) => {
                    if (response.data.data) {
                        this.event = response.data.data
                        this.event.accessTimeStr = moment
                            .utc(this.event.accessTime)
                            .format('DD/MM/YYYY HH:mm:ss')
                    }
                })
                .catch((error) => {
                    console.error('Error loading event:', error)
                })
        },
    },
}
</script>

<style lang="scss" scoped></style>
