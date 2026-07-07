<template>
    <b-card no-body>
        <b-card-body>
            <b-form>
                <b-row>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('fireEvent.common.date')"
                            label-cols-md="4"
                        >
                            <b-form-input
                                v-model="event.accessTimeStr"
                                disabled
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('fireEvent.common.area')"
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.areaName" disabled />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('fireEvent.common.device')"
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.deviceName" disabled />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('fireEvent.common.level')"
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.levelStr" disabled />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('fireEvent.common.fireType')"
                            label-cols-md="4"
                        >
                            <b-form-input
                                v-model="event.fireTypeStr"
                                disabled
                            />
                        </b-form-group>
                    </b-col>
                    <b-col md="12">
                        <b-form-group
                            :label="$t('common.form.label.media')"
                            label-cols-md="12"
                        >
                            <media-swiper :uuid="eventId" />
                        </b-form-group>
                    </b-col>
                    <b-col class="text-center">
                        <b-button
                            @click="handleBack"
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
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import { lookupService } from '@/services'

export default {
    mixins: [authorizationMixin],
    data() {
        const lstFireType = [
            {
                id: 1,
                text: 'fireEvent.fireType.smoke',
            },
            {
                id: 2,
                text: 'fireEvent.fireType.fire',
            },
        ]
        const lstLevel = [
            {
                id: 1,
                text: 'fireEvent.level.low',
            },
            {
                id: 2,
                text: 'fireEvent.level.medium',
            },
            {
                id: 3,
                text: 'fireEvent.level.high',
            },
            {
                id: 4,
                text: 'fireEvent.level.critical',
            },
        ]
        return {
            lstFireType,
            lstLevel,
            event: {
                accessTimeStr: null,
                areaName: null,
                deviceName: null,
                fireType: null,
                fireTypeStr: null,
                level: null,
                levelStr: null,
                image: null,
            },
            lstArea: [],
            lstDevice: [],
        }
    },
    computed: {
        eventId() {
            return this.$route.params.eventId
        },
        currentLocale() {
            return this.$i18n.locale
        },
    },
    watch: {
        currentLocale() {
            this.refreshLanguage()
        },
    },
    async created() {
        const [areas, devices] = await Promise.all([
            lookupService.getAreas(),
            lookupService.getDevices(),
        ])

        this.lstArea = areas
        this.lstDevice = devices

        await this.loadEventDetail()
    },
    methods: {
        handleBack() {
            debugger
            const from = this.$route.query?.from;

            if (from === 'Screen1') {
                this.$router.push('/event/fireEvent/List');
            } else {
                this.$router.push('/notification/allNotification/list');
            }
        },

        refreshLanguage() {
            this.event.fireTypeStr = this.getDynamicName(
                this.event.fireType,
                this.lstFireType
            )
            this.event.levelStr = this.getDynamicName(
                this.event.level,
                this.lstLevel
            )
        },
        getStaticName(id, lst) {
            // eslint-disable-next-line eqeqeq
            return lst.find((x) => x.id == id)?.text
        },
        getDynamicName(id, lst) {
            const item = lst.find(
                // eslint-disable-next-line eqeqeq
                (x) => x.id == id
            )
            return this.$t(item?.text)
        },
        getDate(date) {
            return date ? this.$moment(date).format('HH:mm DD/MM/YYYY') : null
        },
        async loadEventDetail() {
            const fireEvent = await this.$services.get(
                `/fireEvents/${this.eventId}`
            )
            this.event = fireEvent.data.data

            this.event.accessTimeStr = this.getDate(this.event.accessTime)
            this.event.areaName = this.getStaticName(
                this.event.areaId,
                this.lstArea
            )
            this.event.deviceName = this.getStaticName(
                this.event.deviceId,
                this.lstDevice
            )

            this.refreshLanguage()
        },
    },
}
</script>

<style lang="scss"></style>
