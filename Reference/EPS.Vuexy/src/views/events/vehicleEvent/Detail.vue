<!-- eslint-disable vue/html-self-closing -->
<template>
    <validation-observer ref="rules">
        <b-card no-body>
            <b-card-body>
                <b-form>
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('Events.Table.Time')"
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
                                :label="$t('VehicleEvent.Field.LicensePlate')"
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
                                        v-model="event.licensePlates"
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
                                :label="$t('VehicleEvent.Field.Owner')"
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
                                        v-model="event.fullname"
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
                                :label="$t('VehicleEvent.Field.Manufacturer')"
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
                                        v-model="event.manufacturerName"
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
                                :label="$t('VehicleEvent.Field.VehicleType')"
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
                                        v-model="event.vehicleTypeStr"
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
                                :label="$t('VehicleEvent.Field.Direction')"
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
                                :label="$t('VehicleEvent.Field.Seats')"
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
                                        v-model="event.totalNumberOfSeats"
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
                                :label="$t('VehicleEvent.Field.AccessType')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    v-if="!editing"
                                    v-model="event.violationStr"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Classify')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    v-if="!editing"
                                    v-model="event.deptTypeStr"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.DeptOrContractor')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    v-if="!editing"
                                    v-model="event.departmentStr"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col md="6">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Area')"
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
                        <b-col md="6">
                            <b-form-group
                                :label="$t('VehicleEvent.Field.Device')"
                                label-for="h-camera-area"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    v-if="!editing"
                                    v-model="event.deviceName"
                                    :disabled="!editing"
                                />
                            </b-form-group>
                        </b-col>
                        <b-col cols="12">
                            <b-form-group
                                :label="$t('VehicleEvent.Detail.Media')"
                                label-cols="12"
                                label-class="mb-1"
                            >
                                <media-swiper :uuid="eventId" />
                            </b-form-group>
                        </b-col>
                        <b-col md="12" class="text-center">
                            <b-button
                                v-if="!editing"
                                @click="handleBack"
                                type="button"
                                variant="outline-secondary"
                                class="mx-50 mb-50 btn-120 border-0"
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
import { onMounted, ref } from '@vue/composition-api'
import { directions, accessTypes } from '@/constants'

const isDevEnv = process.env.NODE_ENV == 'development'
export default {
    mixins: [authorizationMixin],
    components: {},
    setup() {
        // App Name
        const { apiURL } = $themeConfig.app
        const listEventTypes = ref([])
        return {
            apiURL,
            listEventTypes,
            accessTypes,
            directions,
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
                accessDateStr: null,
                licensePlates: null,
                colorStr: null,
                manufacturerName: null,
                carTypeStr: null,
                vehicleTypeStr: null,
                isBlackListStr: null,
                totalNumberOfSeats: null,
                eventTypeId: null,
                eventTypeStr: null,
                violation: null,
                violationStr: null,
                direction: null,
                directionStr: null,
            },
            listArea: [],
            listDept:[],
            listEventType: [],
            listEventTypes: [],
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
    async created() {
        const accessToken = this.$services.getUserData()
        await this.loadEventTypes()
        await this.loadEventDetail()
        await this.loadDepartments()
    },
    methods: {
        handleBack() {
            debugger
            const from = this.$route.query?.from;

            if (from === 'Screen1') {
                this.$router.push('/event/vehicleEvent/list');
            } else {
                this.$router.push('/notification/allNotification/list');
            }
        },
        async loadDepartments() {
            try {
                const response = await this.$services.get('/lookup/departments')
                this.listDept = response.data.data
            } catch (error) {
                this.$toast({
                    component: ToastificationContent,
                    position: 'top-right',
                    props: {
                        title: this.$t('Error.Error'),
                        icon: 'AlertTriangleIcon',
                        variant: 'danger',
                        text: `${this.$t(`errorCode.LU_DEPARTMENTS_500`)}`,
                    },
                })
            }
        },
        getDeptName(deptId) {
            let deptName = ''
            this.listDept.forEach((dept) => {
                if (dept.id == deptId) {
                    deptName = this.$t(dept.text)
                }
            })
            return deptName
        },
        async loadEventTypes() {
            const response = await this.$services.get('/lookup/eventType')
            if (response.data.data.length > 0) {
                this.listEventTypes = response.data.data.filter(
                    (x) => parseInt(x.id) >= 300 && parseInt(x.id) < 400
                )
            }
        },
        loadEventDetail() {
            this.$services
                .get(`/vehicleEvent/${this.eventId}`)
                .then((response) => {
                    response.data.data.colorStr = this.$t(
                        response.data.data.colorStr
                    )
                    response.data.data.vehicleTypeStr = this.$t(
                        response.data.data.vehicleTypeStr
                    )
                    this.event = response.data.data
                    this.event.eventTypeStr = this.getEventTypeName(
                        this.event.eventTypeId
                    )
                    this.event.violationStr = this.getAccessTypeName(
                        this.event.violation
                    )
                    this.event.directionStr = this.getDirectionName(
                        this.event.direction
                    )
                    this.event.deptTypeStr = this.$t(response.data.data.deptTypeStr)
                    
                    this.event.departmentStr = this.getDeptName(this.event.departmentId)
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
        getEventTypeName(eventTypeId) {
            const eventType = this.listEventTypes.find(
                (x) => x.id == eventTypeId
            )
            return eventType ? this.$t(eventType.text.split('_')[1]) : ''
        },
        getAccessTypeName(accessTypeId) {
            const accessType = this.accessTypes.find(
                (x) => x.value == accessTypeId
            )
            return accessType ? this.$t(`accessType.${accessType.text}`) : ''
        },
        getDirectionName(directionId) {
            const direction = this.directions.find(
                (x) => x.value == directionId
            )
            return direction ? this.$t(`direction.${direction.text}`) : ''
        },
    },
}
</script>

<style lang="scss"></style>
