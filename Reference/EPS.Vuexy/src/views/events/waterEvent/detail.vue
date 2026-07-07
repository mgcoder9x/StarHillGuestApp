<!-- eslint-disable -->
<template>
    <validation-observer ref="result">
        <b-card>
            <b-card-body>
                <b-form>
                    <!-- Areas, Devices -->
                    <b-row>
                        <b-col>
                            <b-form-group
                                :label="this.$t('WaterEvents.Label.Areas')"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    :readonly="true"
                                    v-model="this.eventData.areaName"
                                ></b-form-input>
                            </b-form-group>
                        </b-col>
                        <b-col>
                            <b-form-group
                                :label="this.$t('WaterEvents.Label.Devices')"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    :readonly="true"
                                    v-model="this.eventData.deviceName"
                                ></b-form-input>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <!-- DateTime-->
                    <b-row>
                        <b-col>
                            <b-form-group
                                :label="this.$t('WaterEvents.Label.Date')"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    :readonly="true"
                                    v-model="this.accessDate"
                                ></b-form-input>
                            </b-form-group>
                        </b-col>
                        <b-col>
                            <b-form-group
                                :label="this.$t('WaterEvents.Label.Time')"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    :readonly="true"
                                    v-model="this.accessTime"
                                ></b-form-input>
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <!-- WaterLevel, Warning	 -->
                    <b-row>
                        <b-col>
                            <b-form-group
                                :label="this.$t('WaterEvents.Label.WaterLevel')"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    :readonly="true"
                                    v-model="this.eventData.waterLevel"
                                ></b-form-input>
                            </b-form-group>
                        </b-col>
                        <b-col>
                            <b-form-group
                                :label="this.$t('WaterEvents.Label.Warning')"
                                label-cols-md="4"
                            >
                                <b-form-input
                                    :readonly="true"
                                    v-model="this.eventData.warning"
                                ></b-form-input>
                            </b-form-group>
                        </b-col>
                    </b-row>
                    <!-- Image -->
                    <b-row>
                        <b-col md="6">
                            <b-form-group
                                :label="this.$t('WaterEvents.Label.Image')"
                                label-cols-md="4"
                                class="text-left"
                            >
                                <b-img
                                    :src="`${baseURL}${eventData.image}`"
                                    alt="image"
                                    width="120"
                                    height="80"
                                    thumbnail
                                    @click="showModalEvent(eventData.eventId)"
                                ></b-img>
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <!-- WaterLevel, Warning	 -->
                    <b-row class="mt-3 justify-content-center">
                        <b-col class="text-center">
                            <b-button
                                type="button"
                                variant="outline-secondary"
                                class="mx-50 mb-50 btn-120 btn-hover-linear-secondary border-0"
                                @click="back()"
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

        <!-- Image Slider -->
        <b-modal
            v-model="modalImgEventShow"
            title="Ảnh sự kiện"
            ok-title="Đóng"
            hide-header-close
            ok-only
            size="lg"
        >
            <b-carousel id="carousel-example-generic" indicators controls>
                <b-carousel-slide
                    v-for="item in listEventFiles"
                    :key="item.id"
                    :img-src="`${baseURL}${item.filePath}`"
                />
            </b-carousel>
        </b-modal>
    </validation-observer>
</template>
<script>
import { $themeConfig } from '@themeConfig'
const isDevEnv = process.env.NODE_ENV == 'development'

/*eslint-disable*/
export default {
    data() {
        return {
            eventData: null,
            id: null,
            accessTime: null,
            accessDate: null,
            listEventFiles: [],
            modalImgEventShow: false,
            baseURL: isDevEnv
                ? $themeConfig.app.apiURLDev
                : $themeConfig.app.apiURL,
        }
    },
    watch: {},
    created() {
        this.id = this.$route.params.id
        this.loadEventDetail()
    },

    methods: {
        loadEventDetail() {
            this.$services
                .get('/waterEvents/' + this.id)
                .then((response) => {
                    this.eventData = response.data.data
                    this.accessTime = this.formatTime(this.eventData.accessTime)
                    //this.accessDate = this.formatDate(this.eventData.accessTime)
                    this.accessDate = this.eventData.accessTimeString
                })
                .catch((error) => {
                    console.error('Error fetching data:', error)
                })
        },
        formatDate(accessTime) {
            if (!accessTime) return ''
            const dateTime = new Date(accessTime)
            return dateTime.toISOString().slice(0, 10)
        },
        formatTime(accessTime) {
            if (!accessTime) return ''
            const dateTime = new Date(accessTime)
            return dateTime.toTimeString().slice(0, 8)
        },
        showModalEvent(eventId) {
            this.loadEventFiles(eventId)
            this.modalImgEventShow = true
        },
        loadEventFiles(eventId) {
            this.$services
                .get(`/event/eventFilesById/${eventId}`)
                .then((response) => {
                    this.listEventFiles = response.data
                })
        },
        back() {
            this.$router.push({ name: 'event-waterEvent-list' })
        },
    },
}
</script>
<style lang="scss" scoped>
@import '@/assets/scss/_custom-tree-select.scss';
</style>
