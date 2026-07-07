<template>
    <div>
        <!-- group giúp cả line & circles hoạt động như một khối -->
        <v-group
            :config="{ draggable: active }"
            @click="handleClick"
            @dragend="onGroupDragEnd"
            @mouseenter="onGroupEnter"
            @mouseleave="onGroupLeave"
            @mousemove="onGroupMove"
            @contextmenu="onContextMenu"
        >
            <!-- Polygon chính -->
            <v-line
                :config="{
                    points,
                    stroke: color,
                    strokeWidth: 2,
                    closed: true,
                }"
            />

            <!-- Các điểm điều khiển (chỉ hiển thị nếu active) -->
            <div v-if="active">
                <v-circle
                    v-for="(pt, i) in pointPairs"
                    :key="i"
                    :config="{
                        x: pt[0],
                        y: pt[1],
                        radius: 6,
                        fill: '#ff5555',
                        stroke: '#222',
                        strokeWidth: 1,
                        draggable: true,
                    }"
                    @dragmove="onDrag(i, $event)"
                />
            </div>

            <v-label :config="{ x: tooltipX, y: tooltipY, listening: true }">
                <template v-if="contextMenu.labelText && showTooltip">
                    <!-- Bubble -->
                    <v-tag
                        :config="{
                            fill: '#2b2d42',
                            cornerRadius: 6,
                            pointerDirection: 'left',
                            pointerWidth: 10,
                            pointerHeight: 10,
                            lineJoin: 'round',
                            shadowColor: 'rgba(0,0,0,0.4)',
                            shadowBlur: 6,
                            shadowOffsetX: 4,
                            shadowOffsetY: 4,
                            shadowOpacity: 0.5,
                        }"
                    />
                    <v-text
                        :config="{
                            text: contextMenu.labelText,
                            fontSize: 14,
                            padding: 6,
                            fill: '#ffffff',
                            fontStyle: 'bold',
                        }"
                    />
                </template>

                <!-- Menu chọn nhãn khi đã đóng băng -->
                <template v-if="contextMenu.isFrozen">
                    <!-- nền menu -->
                    <v-rect
                        :config="{
                            x: 0,
                            y: 28, // đặt ngay dưới bubble
                            width: contextMenu.menuW,
                            height: contextMenu.optionH * labelOptions.length,
                            fill: '#111827',
                            cornerRadius: 6,
                            shadowColor: 'rgba(0,0,0,0.25)',
                            shadowBlur: 8,
                            shadowOffsetX: 4,
                            shadowOffsetY: 4,
                            shadowOpacity: 0.6,
                        }"
                    />
                    <!-- từng option -->
                    <v-text
                        v-for="(opt, i) in labelOptions"
                        :key="opt"
                        :config="{
                            x: 0,
                            y: 28 + i * contextMenu.optionH,
                            width: contextMenu.menuW,
                            height: contextMenu.optionH,
                            text: opt,
                            padding: 6,
                            fontSize: 14,
                            align: 'left',
                            fill:
                                opt === contextMenu.labelText
                                    ? '#60a5fa'
                                    : '#ffffff', // highlight option đang chọn
                        }"
                        @click="
                            contextMenu.labelText = opt
                            contextMenu.isFrozen = false
                        "
                        @mouseenter="contextMenu.hoverIdx = i"
                    />
                    <!-- hiệu ứng hover (tuỳ chọn) -->
                    <v-rect
                        v-if="contextMenu.hoverIdx !== null"
                        :config="{
                            x: 0,
                            y: 28 + contextMenu.hoverIdx * contextMenu.optionH,
                            width: contextMenu.menuW,
                            height: contextMenu.optionH,
                            fill: 'rgba(255,255,255,0.06)',
                            listening: false,
                        }"
                    />
                </template>
            </v-label>
        </v-group>
    </div>
</template>

<script>
export default {
    props: {
        points: { type: Array, required: true }, // [x1,y1,x2,y2,...]
        active: { type: Boolean, default: false },
        color: { type: String, default: '#ff0000' },
        metaData: {
            type: Array,
            default: () => {
                return [
                    {
                        type: 'vehicleType',
                        label: 'Xe máy',
                        value: 1, // 1: Xe máy, 2: Oto
                    },
                    {
                        type: 'function',
                        label: 'Đèn',
                        value: 1, // 1: đèn, 2: nhận diện, 3: vạch vượt, 4: vạch rẽ phải
                    },
                ]
            },
        },
    },
    data() {
        return {
            isPointDragging: false,
            showTooltip: false,
            tooltipX: 0,
            tooltipY: 0,

            contextMenu: {
                labelText: null,
                isFrozen: false,
                menuW: 120,
                optionH: 28,
                hoverIdx: null,
            },
        }
    },
    computed: {
        pointPairs() {
            const arr = []
            for (let i = 0; i < this.points.length; i += 2) {
                arr.push([this.points[i], this.points[i + 1]])
            }
            return arr
        },
        labelOptions() {
            // return ['Xe máy', 'Ô tô', 'Xe tải', 'Khác'],
            return this.metaData.map((item) => item.label)
        },
    },
    methods: {
        // Thay đổi hình dạng con trỏ chuột trên stage
        setCursor(e, cursor) {
            const stage = e.target.getStage()
            if (stage) stage.container().style.cursor = cursor
        },

        // Khi chuột đi vào group polygon
        onGroupEnter(e) {
            this.$emit('isInPolygon', true) // báo cho cha biết đang ở trong polygon
            this.showTooltip = true // hiện tooltip
            this.setCursor(e, this.active ? 'grab' : 'pointer') // con trỏ: grab nếu active, pointer nếu không
        },

        // Khi chuột rời khỏi group polygon
        onGroupLeave(e) {
            this.$emit('isInPolygon', false) // báo cho cha biết đã ra khỏi polygon
            this.showTooltip = false // ẩn tooltip
            this.setCursor(e, 'default') // trả con trỏ về mặc định
        },

        onGroupMove(e) {
            if (this.contextMenu.isFrozen) return
            const pos = e.evt
            this.tooltipX = pos.offsetX + 15
            this.tooltipY = pos.offsetY + 15
        },

        onContextMenu(e) {
            e.evt.preventDefault()
            if (!this.active) return
            this.contextMenu.isFrozen = true
        },

        onGroupDragEnd(e) {
            if (this.isPointDragging) {
                this.isPointDragging = false
                return
            }

            if (!this.active) return
            const group = e.target

            const { x, y } = group.position()

            const movedPoints = this.points.map((val, idx) =>
                idx % 2 === 0 ? val + x : val + y
            )

            this.$emit('update:points', movedPoints)

            // Sau khi cập nhật xong → reset group về (0, 0)
            group.position({ x: 0, y: 0 })
            group.getLayer().batchDraw()
        },

        handleClick(e) {
            // nếu bấm chuột phải thì bỏ qua
            if (e.evt.button === 2) {
                return
            }
            this.$emit('polygon-click')
        },
        addPoint(x, y) {
            const next = [...this.points]
            next.push(x, y)
            this.$emit('update:points', next)
        },

        onDrag(idx, e) {
            const newPoints = [...this.points]
            newPoints[idx * 2] = e.target.x()
            newPoints[idx * 2 + 1] = e.target.y()
            this.$emit('update:points', newPoints)

            this.isPointDragging = true
        },

        getMetaData() {
            const selected = this.metaData.find(
                (item) => item.label === this.contextMenu.labelText
            )
            return selected
        },
    },
}
</script>
