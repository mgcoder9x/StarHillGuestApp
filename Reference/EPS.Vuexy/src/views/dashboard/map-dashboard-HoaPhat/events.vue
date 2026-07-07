<template>
    <div class="bg-light">
        <MapVn
            ref="mapVN"
            :url="mapConfig.url"
            :coordinate="mapConfig.coordinate"
            style="width: 100%"
        ></MapVn>
    </div>
</template>

<script>
import getBaseUrl from '@/utils/get-baseUrl'
import signalRService from '@/utils/signalr-service'
import MapVn from './mapvn.vue'

export default {
    components: {
        MapVn,
    },
    data() {
        return {
            mapConfig: {
                coordinate: {
                    lat: 15.38420,
                    lng: 108.79383,
                    zoom: 16,
                },
                url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
            },
            eventType: [],
            items: [],
            searchForm: {},
            isModalOpen: false,
            eventId: null,
            eventTypeSelected: null,
            isShowPoints: false,
        }
    },
    computed: {
        imgUrl() {
            return getBaseUrl()
        },
    },
    async created() {
        const accessToken = this.$services.getUserData()
        if(accessToken.coordinates != null){
            const coordinates = JSON.parse(accessToken.coordinates)
            debugger
            this.mapConfig.coordinate.lat = coordinates[0]
            this.mapConfig.coordinate.lng = coordinates[1]
            this.mapConfig.coordinate.zoom = coordinates[2]
        }
        this.getEvents()
        this.loadEventType()

        // await signalRService.connect('notificationHub')
        // signalRService.on('ReceiveNotification', (data) => {
        //     if (data) {
        //         this.getEvents(this.eventTypeSelected)
        //     }
        // })
    },
    methods: {
        async selectMapImg(isShowPoints) {
            this.isShowPoints = isShowPoints
            if (isShowPoints) {
                this.lstPoints = []
            }
        },
        async selectArea(item) {
            this.areaSelected = item
            await this.loadDevice(item)
            this.loadPoints()
            this.$refs.mapVN.getAreas(item)
        },
        normalizer(node) {
            if (node.children.length === 0) {
                delete node.children
                node.isNew = true
            }
            return {
                label: node.name,
                value: node.id,
                isDisabled:
                    !(node.coordinates || node.mapType === 1) &&
                    !node.isCompany,
                isNew: node.isNew,
            }
        },
        async loadAreas() {
            const areas = await this.$services.get(`lookup/company-areas-tree`)
            this.areas = areas
            this.$refs.mapVN.areasList = areas
            this.$refs.mapVN.getAreas()
        },
        async selectDevice(id) {
            this.deviceIdSelected = id
            this.lstPoints = this.allDevices[id]
        },

        moveToCoordinate(lat, lng) {
            this.$refs.mapVN.moveToCoordinate(lat, lng)
        },
        openModal(eventId) {
            this.eventId = eventId
            this.isModalOpen = true
        },
        changeEventType(event) {
            this.eventTypeSelected = event
            this.getEvents(this.eventTypeSelected)
        },
        loadEventType() {
            this.$services.get('/lookup/eventType').then((response) => {
                this.eventType = [
                    { value: null, text: 'Loại sự kiện' },
                    ...response.data.data,
                ]
            })
        },
        getEvents(eventTypeId) {
            const formData = this.getFormData(eventTypeId)
            this.$services.get(`/event?${formData}`).then((response) => {
                this.items = []
                response.data.data.data.forEach((item) => {
                    const temp = {
                        eventId: item.eventId,
                        image: `${this.imgUrl}${item.image}`,
                        areaName: item.areaName,
                        deviceName: item.deviceName,
                        accessTime: this.$moment(item.accessTime).format(
                            'DD/MM/YY HH:mm:ss'
                        ),
                        eventTypeName: item.eventTypeName,
                        lat: item.deviceLat,
                        lng: item.deviceLng,
                    }
                    this.items.push(temp)
                })
            })
        },
        getFormData(eventTypeId) {
            const pagination = {
                page: 1,
                itemsPerPage: 20,
                sortBy: 'accessTime',
                eventTypeId,
            }
            const formData = `${new URLSearchParams(pagination).toString()}&${new URLSearchParams(this.searchForm).toString()}`
            return formData
        },
    },
}
</script>

<style lang="scss" scoped>
.bg-light {
    position: relative;
    z-index: 1;
}
</style>
