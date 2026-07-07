<template>
    <b-card no-body>
        <b-card-body>
            <b-form>
                <b-row>
                    <b-col md="4">
                        <swiper :swiper-data="swiperData" />
                    </b-col>
                    <b-col md="8" />
                </b-row>
                <b-row />
            </b-form>
        </b-card-body>
    </b-card>
</template>

<script>
/* eslint-disable */
import { authorizationMixin } from '@core/mixins/ui/forms'
import Swiper from '@/components/MediaSwiper.vue'
export default {
    mixins: [authorizationMixin],
    components: {
        Swiper,
    },
    data() {
        return {
            event: {
                areaId: null,
                areaName: null,
                deviceId: null,
                deviceName: null,
                eventTypeId: null,
                eventTypeName: null,
                accessTime: null,
                accessTimeStr: null,
                images: [],
                videos: [],
            },
            swiperData: [],
        }
    },
    computed: {
        eventId() {
            return this.$route.params.eventId
        },
    },
    created() {
        this.loadEventDetail()
        this.getSwiperData()
    },
    methods: {
        getSwiperData() {
            const imageList = this.event.images.map((x) => ({
                path: x,
                type: 'image',
            }))
            const videoList = this.event.videos.map((x) => ({
                path: x,
                type: 'video',
            }))
            this.swiperData = [...imageList, ...videoList]
        },
        loadEventDetail() {
            this.$services.get(`/event/${this.eventId}`).then((response) => {
                this.event = response.data.data
            })
        },
        cancel() {
            this.loadEventDetail()
        },
    },
}
</script>

<style lang="scss"></style>
