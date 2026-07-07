<template>
    <div>
        <swiper class="swiper" :options="swiperOption">
            <swiper-slide
                v-for="(data, index) in swiperData"
                :key="index"
                class="m-0 px-2 swiper-slide-wraper"
            >
                <div class="swiper-slide-container">
                    <b-img
                        v-if="
                            data.type === FILETYPE.IMAGE ||
                            data.type === FILETYPE.IMAGE_OVERVIEW ||
                            data.type === FILETYPE.IMAGE_CARGO
                        "
                        :src="data.src"
                        thumbnail
                        fluid
                        class="w-100 h-100"
                        @click="showMediaModal(data)"
                    />
                    <div
                        v-else-if="
                            data.type === FILETYPE.VIDEO ||
                            (data.type > 200 && data.type < 300)
                        "
                        fluid
                        class="video-container"
                    >
                        <b-embed
                            type="video"
                            class="w-100 h-100 img-thumbnail"
                            @click="showMediaModal(data)"
                        >
                            <source :src="data.src" type="video/mp4" />
                        </b-embed>
                        <div class="play-icon">
                            <Icon icon="line-md:play-twotone" class="md-icon" />
                        </div>
                    </div>

                    <b-img
                        v-else-if="
                            data.type === FILETYPE.LICENSE_PLATE_IMAGE ||
                            data.type === FILETYPE.IMAGE_LICENSE_PLATE
                        "
                        :src="data.src"
                        thumbnail
                        class="w-100 h-100"
                        @click="showMediaModal(data)"
                    />
                    <!-- <b-img v-else-if="data.type === FILETYPE.LICENSE_PLATE_IMAGE"
                            :src="data.src"
                            thumbnail
                            :style=" {
                                    width: '200px',
                                    height:
                                        data.width && data.height
                                            ? `${(data.height * 200) / data.width}px`
                                            : 'auto',
                                }
                                "
                            @click="showMediaModal(data)"
                            
                        /> -->

                    <b-img v-else :src="data.src" fluid />
                </div>
            </swiper-slide>
            <div slot="pagination" class="swiper-pagination"></div>
        </swiper>
        <b-modal
            v-model="mediaModal"
            :title="this.$t(`file.image`)"
            hide-footer
            size="lg"
            class="event-carousel-modal"
            no-close-on-backdrop
        >
            <!-- Sử dụng EventCarousel với toàn bộ dữ liệu, không chỉ ảnh được click -->
            <event-carousel
                v-if="formattedEventFiles.length > 0"
                :listEventFiles="formattedEventFiles"
                :baseURL="''"
                ref="eventCarousel"
            ></event-carousel>
        </b-modal>
    </div>
</template>

<script>
import { Swiper, SwiperSlide } from 'vue-awesome-swiper'
import { BImg, BEmbed } from 'bootstrap-vue'
import { FILETYPE, FILETYPE_NAME } from '@/constants/fileType'
import EventCarousel from '@/components/EventCarousel.vue'
import 'swiper/css/swiper.css'
import { $themeConfig } from '@themeConfig'

export default {
    components: {
        Swiper,
        SwiperSlide,
        BImg,
        BEmbed,
        EventCarousel,
    },
    props: {
        uuid: {
            type: String,
            default: '00000000-0000-0000-0000-000000000000',
        },
    },
    data() {
        const baseURL = $themeConfig.app.apiURL
        return {
            media: {
                src: '',
                type: 1,
            },
            mediaModal: false,
            baseURL,
            media: {
                src: '',
                type: 1,
                width: 0,
                height: 0,
            },
            FILETYPE,
            FILETYPE_NAME,
            swiperOption: {
                keyboard: {
                    enabled: true,
                },
                navigation: {
                    nextEl: '.swiper-button-next',
                    prevEl: '.swiper-button-prev',
                },
                centeredSlides: false,
                pagination: {
                    el: '.swiper-pagination',
                    clickable: true,
                },
                breakpoints: {
                    1440: {
                        slidesPerView: 5,
                        spaceBetween: 30,
                    },
                    1024: {
                        slidesPerView: 4,
                        spaceBetween: 30,
                    },
                    768: {
                        slidesPerView: 3,
                        spaceBetween: 30,
                    },
                    640: {
                        slidesPerView: 2,
                        spaceBetween: 20,
                    },
                },
            },
            swiperData: [],
            formattedEventFiles: [], // Dữ liệu đã định dạng để truyền vào EventCarousel
        }
    },
    async created() {
        if (this.baseURL.includes('localhost')) {
            //this.baseURL = 'https://demo.atin.vn/Service'
            this.baseURL = process.env.VUE_APP_BASE_URL
        }
        this.getData()
    },
    methods: {
        async getData() {
            const response = await this.$services.get(
                `/event/eventFilesById/${this.uuid}`
            )
            this.swiperData = response.data.map((x) => ({
                src: this.baseURL + x.filePath,
                type: x.fileType,
                width: x.width || 0,
                height: x.height || 0,
            }))
        },

        showMediaModal(data) {
            this.media = data

            // Chuyển đổi TẤT CẢ dữ liệu để truyền vào EventCarousel
            this.formattedEventFiles = this.swiperData.map((item, index) => ({
                id: index,
                fileType: item.type,
                filePath: item.src,
                width: item.width || 0,
                height: item.height || 0,
            }))

            // Nếu là ảnh biển số, load kích thước thật
            if (data.type === FILETYPE.LICENSE_PLATE_IMAGE) {
                const img = new Image()
                img.src = data.src
                img.onload = () => {
                    this.media.width = img.naturalWidth
                    this.media.height = img.naturalHeight

                    // Cập nhật kích thước cho ảnh trong formattedEventFiles
                    const clickedIndex = this.swiperData.findIndex(
                        (item) => item.src === data.src
                    )
                    if (clickedIndex >= 0) {
                        this.formattedEventFiles[clickedIndex].width =
                            img.naturalWidth
                        this.formattedEventFiles[clickedIndex].height =
                            img.naturalHeight
                    }

                    this.mediaModal = true
                    this.setActiveSlide(data.src)
                }
            } else {
                this.mediaModal = true
                this.setActiveSlide(data.src)
            }
        },

        // Thêm phương thức để thiết lập slide active
        setActiveSlide(src) {
            // Đảm bảo thiết lập slide active sau khi modal được render
            this.$nextTick(() => {
                const index = this.formattedEventFiles.findIndex(
                    (item) => item.filePath === src
                )
                if (index > 0 && this.$refs.eventCarousel) {
                    // Nếu EventCarousel có phương thức setSlide, gọi nó
                    // Hoặc cần thêm phương thức này vào EventCarousel
                    if (this.$refs.eventCarousel.$refs.carousel) {
                        this.$refs.eventCarousel.$refs.carousel.setCurrentSlide(
                            index
                        )
                    }
                }
            })
        },
    },
}
</script>

<style lang="scss">
.swiper {
    width: 100%;
    height: 200px;

    .swiper-slide {
        display: flex;
        justify-content: center;
        align-items: center;
        text-align: center;
        font-weight: bold;
        font-size: 2rem;
        background-color: transparent;
        height: 200px;
    }
}

.swiper .swiper-pagination-bullet-custom {
    $size: 20px;
    width: $size !important;
    height: $size !important;
    line-height: $size !important;
    text-align: center;
    color: #fff;
    opacity: 0.7;
    background: rgba(0, 0, 0, 0.2);
    //transition: all $transition-time-normal;

    &:hover {
        opacity: 1;
    }

    &.swiper-pagination-bullet-active {
        opacity: 1;
        color: #ffffff;
        background: #007aff;
    }
}
.swiper-button-prev,
.swiper-button-next {
    color: #ffffff;
}
.swiper-slide-container {
    width: 100%;
    height: 100%;
    img {
        object-fit: cover;
    }
    :first-child {
        object-fit: cover;
    }
    .embed-responsive {
        height: 100%;
        width: 100%;
    }
    video {
        border: 1px solid #dae1e7 !important;
    }
}
.embed-responsive-item.img-thumbnail {
    border: 1px solid #dae1e7;
}
.video-container {
    position: relative;
    width: 100%;
    height: 100%;
}
.play-icon {
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    font-size: 2rem; /* Kích thước của biểu tượng play */
    color: white;
    background: rgba(0, 0, 0, 0.5); /* Nền mờ */
    border-radius: 50%;
    padding: 5px;
    display: flex;
    align-items: center;
    justify-content: center;
    pointer-events: none; /* Đảm bảo click sẽ truyền đến video-wrapper */
}
.swiper-slide-wraper {
    width: 300px !important;
}

/* CSS để đảm bảo modal với EventCarousel hiển thị đẹp */
.event-carousel-modal {
    .modal-content {
        background-color: rgba(0, 0, 0, 0.85);
    }

    .modal-header {
        border-bottom: none;
        color: white;
        padding: 0.5rem 1rem;

        .close {
            color: white;
            opacity: 0.8;

            &:hover {
                opacity: 1;
            }
        }
    }

    .modal-body {
        padding: 0;
        height: 425px;
        overflow: hidden;
    }

    .modal-dialog {
        max-width: 95%;
        margin-top: 2%;
        height: calc(100% - 4%);
    }

    /* Đảm bảo nút điều hướng luôn hiển thị */
    .carousel-control-prev,
    .carousel-control-next {
        opacity: 1;
        z-index: 1050;
        pointer-events: auto;
    }
}
</style>
