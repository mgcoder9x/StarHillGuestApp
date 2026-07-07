<template>
    <b-carousel id="carousel-example-generic" indicators controls>
        <b-carousel-slide
            v-for="item in listEventFiles"
            :key="item.id"
            style="width: 100%; height: 100%"
        >
            <template v-slot:img>
                <template
                    v-if="
                        item.fileType === FILETYPE.IMAGE ||
                        item.fileType === FILETYPE.LICENSE_PLATE_IMAGE ||
                        (item.fileType > 100 && item.fileType < 200)
                    "
                >
                    <img
                        alt="Ảnh"
                        :src="`${baseURL}${item.filePath}`"
                        :class="
                            item.fileType === FILETYPE.IMAGE ||
                            item.fileType !== FILETYPE.LICENSE_PLATE_IMAGE
                                ? 'carousel-image'
                                : ''
                        "
                        loading="lazy"
                        :style="
                            item.fileType === FILETYPE.LICENSE_PLATE_IMAGE ||
                            item.fileType === FILETYPE.IMAGE_LICENSE_PLATE
                                ? {
                                      width: '200px',
                                      height:
                                          item.width && item.height
                                              ? `${(item.height * 200) / item.width}px`
                                              : 'auto',
                                  }
                                : {}
                        "
                    />
                </template>
                <template
                    v-if="
                        item.fileType === FILETYPE.VIDEO ||
                        (item.fileType > 200 && item.fileType < 300)
                    "
                >
                    <div class="video-container">
                        <video
                            :src="`${baseURL}${item.filePath}`"
                            class="carousel-video"
                            controls
                        />
                    </div>
                </template>
            </template>
        </b-carousel-slide>
    </b-carousel>
</template>
<script>
import { FILETYPE } from '@/constants'

export default {
    name: 'EventCarousel', // Tên component
    props: {
        listEventFiles: {
            type: Array,
            required: true,
        },
        baseURL: {
            type: String,
            required: true,
        },
    },
    data() {
        return {
            FILETYPE,
        }
    },
}
</script>

<style>
.carousel-image,
.carousel-video {
    width: 100%;
    height: 100%;
    max-width: 100%;
    max-height: calc(80vh - 180px);
    object-fit: contain;
    margin: auto;
    display: block;
}

.video-container {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100%;
    width: 100%;
}

/* Đảm bảo carousel-item hiển thị đúng */
.carousel-item {
    text-align: center;
    height: 100%;
    display: none; /* Ẩn mặc định, chỉ hiển thị khi active */
    align-items: center;
    justify-content: center;
}

.carousel-item.active {
    display: flex !important; /* Chỉ hiển thị khi active */
}

/* Đảm bảo carousel-caption không ảnh hưởng đến hiển thị */
.carousel-caption {
    position: relative;
    right: 0;
    left: 0;
    text-align: center;
    overflow: hidden;
    width: 100%;
    height: 100%;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
}

/* Tăng z-index cho các điều khiển để đảm bảo chúng không bị che khuất */
.carousel-control-prev,
.carousel-control-next {
    z-index: 100; /* Tăng z-index cao hơn nữa */
    width: 50px;
    height: 50px;
    background-color: rgba(255, 255, 255, 0.4);
    border-radius: 50%;
    top: 50%;
    transform: translateY(-50%);
    margin: 0 15px;
    opacity: 0.9;
    box-shadow: 0 3px 6px rgba(0, 0, 0, 0.3);
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s ease;
    position: absolute;
    padding: 0;
    border: 1px solid rgba(200, 200, 200, 0.2); /* Light gray border */
    outline: none;
    pointer-events: auto;
}

/* Make sure the buttons maintain their circular shape and are properly centered */
#carousel-example-generic .carousel-control-prev,
#carousel-example-generic .carousel-control-next {
    aspect-ratio: 1 / 1;
    max-width: 50px;
    max-height: 50px;
    min-width: 50px;
    min-height: 50px;
    top: 50%;
    transform: translateY(-50%);
    pointer-events: auto;
}

#carousel-example-generic .carousel-control-prev {
    left: 20px;
}

#carousel-example-generic .carousel-control-next {
    right: 20px;
}

.carousel-control-prev:active,
.carousel-control-next:active {
    transform: translateY(-50%) scale(0.95);
    background-color: rgba(255, 255, 255, 1);
    box-shadow: 0 2px 5px rgba(0, 0, 0, 0.2) inset;
}

.carousel-control-prev:hover,
.carousel-control-next:hover {
    background-color: rgba(255, 255, 255, 0.8);
    box-shadow: 0 5px 10px rgba(0, 0, 0, 0.25);
}

.carousel-control-prev-icon,
.carousel-control-next-icon {
    background-image: none;
    display: inline-block;
    width: 20px;
    height: 20px;
    position: relative;
}

.carousel-control-prev-icon:after,
.carousel-control-next-icon:after {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
}

.carousel-control-prev-icon:after {
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%23333' stroke-width='3' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='15 18 9 12 15 6'%3E%3C/polyline%3E%3C/svg%3E");
    background-size: contain;
    opacity: 0.9;
    width: 110%;
    height: 110%;
}

.carousel-control-next-icon:after {
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%23333' stroke-width='3' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='9 18 15 12 9 6'%3E%3C/polyline%3E%3C/svg%3E");
    background-size: contain;
    opacity: 0.9;
    width: 110%;
    height: 110%;
}

.carousel-control-prev:hover .carousel-control-prev-icon:after,
.carousel-control-next:hover .carousel-control-next-icon:after {
    opacity: 1;
}

/* Đảm bảo indicators hiển thị đúng */
.carousel-indicators {
    z-index: 100;
    pointer-events: auto;
    bottom: 15px;
}

.carousel-indicators li {
    width: 10px !important;
    height: 10px !important;
    border-radius: 50% !important;
    background-color: rgba(255, 255, 255, 0.5);
    border: none !important; /* Loại bỏ viền */
    margin: 0 5px;
    opacity: 0.7;
    transition: all 0.3s ease;
    padding: 0;
    max-width: 10px !important;
    min-width: unset !important;
    box-sizing: border-box !important;
}

.carousel-indicators li.active {
    background-color: #ffffff;
    transform: scale(1.2);
    box-shadow: 0 0 3px rgba(255, 255, 255, 0.8);
    opacity: 1;
}

@media (max-width: 768px) {
    .carousel-control-prev,
    .carousel-control-next {
        width: 40px;
        height: 40px;
    }

    .carousel-indicators li {
        width: 10px;
        height: 10px;
        margin: 0 4px;
    }
}

@media (max-width: 480px) {
    .carousel-control-prev,
    .carousel-control-next {
        width: 35px;
        height: 35px;
        margin: 0 10px;
    }

    .carousel-indicators li {
        width: 8px;
        height: 8px;
        margin: 0 3px;
    }
}
</style>
