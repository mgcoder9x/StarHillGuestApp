<template>
    <svg
        v-if="zones.length > 0"
        class="zone-overlay"
        viewBox="0 0 1 1"
        preserveAspectRatio="none"
    >
        <polygon
            v-for="(zone, idx) in zones"
            :key="idx"
            :points="toSvgPoints(zone)"
            :stroke="strokeColor"
            :stroke-width="strokeWidth"
            fill="none"
        />
    </svg>
</template>

<script>
export default {
    name: 'ZoneOverlay',
    props: {
        /**
         * Array of zone objects. Mỗi zone hỗ trợ 2 dạng tọa độ:
         *
         * Dạng 1 – flat array (legacy):
         *   { points: [x1, y1, x2, y2, ...] }          // normalized 0-1
         *
         * Dạng 2 – object array (polygonAI từ API):
         *   { pointPolygon: [{ x: 0.39, y: 0.27 }, ...] }
         *
         * Cả hai dạng đều yêu cầu tọa độ normalized 0-1.
         */
        zones: {
            type: Array,
            default: () => [],
        },
        /** Màu viền polygon */
        strokeColor: {
            type: String,
            default: '#f2eb0f',
        },
        /** Độ dày viền (đơn vị SVG viewBox, 0-1) */
        strokeWidth: {
            type: [String, Number],
            default: 0.004,
        },
    },
    methods: {
        /**
         * Chuyển zone data → SVG points string "x1,y1 x2,y2 ..."
         * Hỗ trợ:
         *  - zone.pointPolygon: [{ x, y }, ...] (polygonAI format)
         *  - zone.points: [x1, y1, x2, y2, ...] (flat array)
         *  - zone.points: [{ x, y }, ...] (object array)
         */
        toSvgPoints(zone) {
            // Ưu tiên pointPolygon (từ polygonAI response)
            const pts = zone.pointPolygon || zone.points
            if (!pts || pts.length === 0) return ''

            // Object array: [{ x, y }, ...]
            if (typeof pts[0] === 'object' && pts[0] !== null) {
                return pts.map((p) => `${p.x},${p.y}`).join(' ')
            }

            // Flat array: [x1, y1, x2, y2, ...]
            if (pts.length < 4) return ''
            const pairs = []
            for (let i = 0; i < pts.length; i += 2) {
                pairs.push(`${pts[i]},${pts[i + 1]}`)
            }
            return pairs.join(' ')
        },
    },
}
</script>

<style scoped>
.zone-overlay {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    z-index: 2;
    pointer-events: none;
}
</style>
