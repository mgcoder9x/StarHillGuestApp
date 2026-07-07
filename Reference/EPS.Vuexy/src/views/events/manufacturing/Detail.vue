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
                        <b-col>
                            <b-form-group
                                :label="this.$t('WaterEvents.Label.Warning')"
                                label-cols-md="4"
                            >
                                <b-img
                                    :src="this.eventData.image"
                                    alt="image"
                                    thumbnail
                                ></b-img>
                            </b-form-group>
                        </b-col>
                    </b-row>

                    <!-- WaterLevel, Warning	 -->
                    <b-row class="mt-3 justify-content-center">
                        <b-col class="text-center">
                            <button class="btn btn-primary" @click="back()">
                                Quay lại
                            </button>
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
/*eslint-disable*/
export default {
    data() {
        return {
            eventData: null,
            id: null,
            accessTime: null,
            accessDate: null,
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
                    this.accessDate = this.formatDate(this.eventData.accessTime)
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
        back() {
            this.$router.push({ name: 'event-waterEvent-list' })
        },
    },
}
</script>
<style></style>
