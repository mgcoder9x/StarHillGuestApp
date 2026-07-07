<!-- eslint-disable vue/html-self-closing -->
<template>
    <b-card no-body>
        <b-card-body>
            <b-form>
                <b-row>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('FaceEvent.Detail.AccessTime')"
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
                    <!-- <b-col md="6">
                        <b-form-group
                            :label="$t('FaceEvent.Detail.UserCode')"
                            label-for="h-camera-name"
                            label-cols-md="4"
                        >
                            <b-form-input
                                id="h-camera-name"
                                v-model="event.userCode"
                                readonly
                            />
                        </b-form-group>
                    </b-col> -->
                    <!-- <b-col md="6">
                        <b-form-group
                            :label="$t('FaceEvent.Detail.UserName')"
                            label-for="h-camera-area"
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.userName" readonly />
                        </b-form-group>
                    </b-col> -->
                    <!-- <b-col md="6">
                        <b-form-group
                            :label="$t('FaceEvent.Detail.DepName')"
                            label-for="h-camera-event-type"
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.depName" readonly />
                        </b-form-group>
                    </b-col> -->
                    <b-col md="6">
                        <b-form-group
                            :label="$t('FaceEvent.Detail.AreaName')"
                            label-for="h-camera-area"
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.areaName" readonly />
                        </b-form-group>
                    </b-col>
                    <b-col md="6">
                        <b-form-group
                            :label="$t('FaceEvent.Detail.Device')"
                            label-for="h-camera-area"
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.deviceName" readonly />
                        </b-form-group>
                    </b-col>
                    <!-- <b-col md="6">
                        <b-form-group
                            :label="$t('FaceEvent.Detail.EventType')"
                            label-for="h-camera-event-type"
                            label-cols-md="4"
                        >
                            <b-form-input
                                v-model="event.eventTypeName"
                                readonly
                            />
                        </b-form-group>
                        <b-form-group
                            :label="$t('FaceEvent.Detail.RecognitionObject')"
                            label-for="h-camera-event-type"
                            label-cols-md="4"
                        >
                            <b-form-input
                                v-model="event.personTypeName"
                                readonly
                            />
                        </b-form-group>
                    </b-col> -->
                    <!-- <b-col md="6">
                        <b-form-group
                            :label="$t('FaceEvent.Detail.Status')"
                            label-for="h-camera-area"
                            label-cols-md="4"
                        >
                            <b-form-input v-model="event.statusName" readonly />
                        </b-form-group>
                    </b-col> -->
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
                    <b-col cols="12">
                        <b-form-group
                            :label="$t('FaceEvent.Detail.Image')"
                            label-cols="12"
                            label-class="mb-1"
                        >
                            <media-swiper :uuid="eventId" />
                        </b-form-group>
                    </b-col>
                    <!-- <b-col md="6">
                        <img
                            :src="`${baseURL}${event.image}`"
                            alt="Image"
                            width="100px"
                            height="80px"
                            style="margin: 0 auto"
                        />
                    </b-col> -->
                    <b-col md="12" class="text-center">
                        <b-button
                            type="button"
                            class="mx-50 mb-50 btn-120"
                            variant="outline-secondary"
                            @click="goBack"
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
            event: {
                accessTime: null,
                userCode: null,
                userName: null,
                depName: null,
                areaName: null,
                deviceName: null,
                eventTypeName: null,
                status: null,
            },
            listEventType: [],
            baseURL: isDevEnv ? 'http://localhost:1938' : this.apiURL,
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
    },
    methods: {
        getStatusName(value) {
            switch (value) {
                case 0:
                    return this.$t('accessType.allow')
                case 1:
                    return this.$t('accessType.deny')
                case 2:
                    return this.$t('accessType.expired')
                case 3:
                    return this.$t('accessType.noPermission')
                case 4:
                    return this.$t('accessType.wrongArea')
                case 5:
                    return this.$t('accessType.wrongTime')
                default:
                    return 'Không xác định'
            }
        },

        loadEventDetail() {
            this.$services
                .get(`/peopleCountInOutEvent/${this.eventId}`)
                .then((response) => {
                    this.event = response.data.data
                    // this.event.eventTypeName = this.listEventType.find(
                    //     (x) => x.id == this.event.eventTypeId
                    // ).text
                    this.event.accessTime = this.$moment(
                        this.event.accessTime
                    ).format('DD/MM/YYYY HH:mm:ss')
                    // Sửa logic: không ghi đè lên status/gender gốc
                    this.event.statusName = this.getStatusName(
                        this.event.status
                    )
                })
        },
        //lookup data
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.listEventType = response.data.data
            })
        },
        goBack() {
            // Sử dụng history.back() để quay về trang trước đó
            // Điều này sẽ giữ nguyên state của component List
            // this.$router.go(-1)
            const from = this.$route.query?.from
            this.$router.push('/event/faceGateEvent/list')
            // if (from === 'Screen1') {
            //     this.$router.push('/event/faceGateEvent/list');
            // } else {
            //     this.$router.push('/notification/allNotification/list');
            // }
        },
    },
}
</script>

<style lang="scss"></style>
