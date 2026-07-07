<template>
    <div v-if="detections.length > 0" ref="overlayEl" class="bbox-overlay">
        <!-- SVG chỉ dùng cho bbox rectangles (cần stretch theo video) -->
        <svg
            class="bbox-svg-layer"
            viewBox="0 0 1 1"
            preserveAspectRatio="none"
        >
            <rect
                v-for="(det, idx) in detections"
                :key="'r-' + idx"
                :x="det.bbox[0]"
                :y="det.bbox[1]"
                :width="det.bbox[2] - det.bbox[0]"
                :height="det.bbox[3] - det.bbox[1]"
                :stroke="det.color || '#00cc44'"
                stroke-width="0.003"
                fill="none"
                :class="{ 'bbox-flash': det.isNew }"
            />
        </svg>

        <!-- HTML labels (percentage-based, scale cùng bbox) -->
        <span
            v-for="(det, idx) in labelPositions"
            :key="'l-' + idx"
            class="bbox-label"
            :style="det.style"
            >{{ det.text }}</span
        >
    </div>
</template>

<script>
export default {
    name: 'BboxOverlay',
    props: {
        detections: {
            type: Array,
            default: () => [],
        },
    },
    computed: {
        labelPositions() {
            return this.detections.map((det) => {
                const x1 = det.bbox[0]
                const y1 = det.bbox[1]
                const color = det.color || '#00cc44'
                const label = det.label || ''

                return {
                    text: label,
                    style: {
                        top: `${y1 * 100}%`,
                        left: `${x1 * 100}%`,
                        transform: 'translateY(-100%)',
                        backgroundColor: color,
                    },
                }
            })
        },
    },
}
</script>

<style scoped>
.bbox-overlay {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    z-index: 2;
    pointer-events: none;
}

.bbox-svg-layer {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
}

.bbox-label {
    position: absolute;
    color: #fff;
    font-family: Arial, Helvetica, sans-serif;
    font-weight: bold;
    font-size: 12px;
    line-height: 1;
    padding: 3px 5px;
    border-radius: 2px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    opacity: 0.88;
    max-width: 200px;
}

@keyframes bboxFlash {
    0% {
        opacity: 1;
    }
    50% {
        opacity: 0.4;
    }
    100% {
        opacity: 1;
    }
}
.bbox-flash {
    animation: bboxFlash 0.6s ease-in-out 3;
}
</style>
