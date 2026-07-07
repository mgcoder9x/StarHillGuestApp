<template>
    <div>
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('VirtualFenceEvent.Field.Date')"
                                label-for="h-date"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-date"
                                    v-model="event.accessDateStr"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('VirtualFenceEvent.Field.Time')"
                                label-for="h-time"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-time"
                                    v-model="event.accessTimeStr"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('VirtualFenceEvent.Detail.Device')"
                                label-for="h-device"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    v-if="!editing"
                                    v-model="event.deviceName"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>

                        <b-col md="6">
                            <b-form-group
                                :label="
                                    $t('VirtualFenceEvent.Field.ObjectType')
                                "
                                label-for="h-trouble-type"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-trouble-type"
                                    :value="$t(event.troubleTypeStr)"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('VirtualFenceEvent.Field.Area')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    v-if="!editing"
                                    v-model="event.areaName"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col v-if="event.troubleType === 1" md="6">
                            <b-form-group
                                :label="$t('VirtualFenceEvent.Field.UserName')"
                                label-for="h-license-plate"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-user-name"
                                    v-model="event.userName"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col v-if="event.troubleType === 2" md="6">
                            <b-form-group
                                :label="
                                    $t('VirtualFenceEvent.Field.LicensePlate')
                                "
                                label-for="h-license-plate"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    id="h-license-plate"
                                    v-model="event.licensePlate"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <!-- <b-form-group
                                :label="this.$t('VirtualFenceEvent.Field.Category')"
                                label-for="h-category"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    v-if="!editing"
                                    :value="event.personType === 0? $t('VirtualFenceEvent.PersonType.Registered')
                                            : event.personType === 1 ? $t('VirtualFenceEvent.PersonType.Unregistered')
                                            : ''"
                                    :disabled="!editing"
                                />
                            </b-form-group> -->
                        </b-col>
                        <b-col md="12">
                            <!-- <div class="event-img-wrap">
                                <img
                                    :src="`${baseURL}${event.filePath}`"
                                    alt="Image"
                                    width="100px"
                                    height="80px"
                                    class="img-fluid"
                                />
                            </div> -->
                            <b-form-group
                                :label="$t('VirtualFenceEvent.Detail.Image')"
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
    </div>
</template>

<script>
import { authorizationMixin } from '@core/mixins/ui/forms'
import { reactive } from '@vue/composition-api'
import { TROUBLE_TYPES } from '../virtualFenceEvent/data'

export default {
    mixins: [authorizationMixin],
    setup() {
        const listTroubleType = reactive(TROUBLE_TYPES)
        const { VUE_APP_BASE_URL: baseURL } = process.env
        const editing = reactive(false)
        // const event = reactive({
        //     id: null,
        //     eventId: null,
        //     troubleTypeStr: null,
        //     licensePlate: null,
        //     compId: null,
        //     deviceName: null,
        //     accessDateStr: null,
        //     accessTimeStr: null,
        //     image: null,
        //     filePath: null,
        //     video: null,
        // })

        return { listTroubleType, baseURL, editing }
    },
    data() {
        return {
            event: {
                id: null,
                eventId: null,
                troubleTypeStr: null,
                troubleType: null,
                licensePlate: null,
                compId: null,
                deviceName: null,
                accessDateStr: null,
                accessTimeStr: null,
                image: null,
                filePath: null,
                video: null,
                personType: null,
            },
        }
    },
    computed: {
        eventId() {
            return this.$route.params.eventId
        },
    },
    async created() {
        await this.loadEventDetail()
    },
    methods: {
        handleBack() {
            debugger
            const from = this.$route.query?.from;

            if (from === 'Screen1') {
                this.$router.push('/event/virtualFenceEvent/list');
            } else {
                this.$router.push('/notification/allNotification/list');
            }
        },
        async loadEventDetail() {
            try {
                const {
                    data: { data },
                } = await this.$services.get(
                    `/virtualFenceEvents/${this.eventId}`
                )
                Object.assign(this.event, data)
                if (!this.event.userName) {
                    this.event.userName = this.$t('VirtualFenceEvent.Unknown')
                }else if (this.event.userName === 'Vãng lai') {
                    this.event.userName = this.$t('VirtualFenceEvent.Haunt')
                } 
            } catch (err) {
                console.log('Lỗi lấy sự kiện', err)
            }
        },
    },
}
</script>

<style></style>
