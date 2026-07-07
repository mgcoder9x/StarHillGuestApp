<template>
    <div>
        <!-- Ảnh nhỏ -->
        <img
            :src="imgSrc"
            :style="{ width: width, cursor: 'zoom-in' }"
            @click="openViewer"
            @error="handleImageError"
        />

        <!-- Overlay -->
        <div v-if="visible" class="overlay" @click.self="closeViewer">
            <button class="close-btn" @click="closeViewer">×</button>

            <div
                class="image-container"
                @mousedown="startDrag"
                @mousemove="onDrag"
                @mouseup="endDrag"
                @mouseleave="endDrag"
            >
                <img
                    :src="imgSrc"
                    class="zoom-image"
                    draggable="false"
                    :style="{
                        transform:
                            'translate(' +
                            translateX +
                            'px,' +
                            translateY +
                            'px) scale(' +
                            scale +
                            ')',
                    }"
                    @error="handleImageError"
                />
            </div>

            <div class="toolbar">
                <button @click="zoomIn">+</button>
                <button @click="zoomOut">-</button>
                <button @click="reset">Reset</button>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    name: 'ImageViewer',

    props: {
        src: {
            type: String,
            required: true,
        },
        width: {
            type: String,
            default: '200px',
        },
    },

    data() {
        return {
            visible: false,
            scale: 1,
            translateX: 0,
            translateY: 0,
            isDragging: false,
            startX: 0,
            startY: 0,
            imgSrc: this.src,
        }
    },

    watch: {
        src(newSrc) {
            this.imgSrc = newSrc
        },
    },

    methods: {
        handleImageError() {
            this.imgSrc = '/No-Image.jpg'
        },

        openViewer() {
            this.visible = true
            this.reset()
        },

        closeViewer() {
            this.visible = false
        },

        zoomIn() {
            this.scale += 0.2
        },

        zoomOut() {
            if (this.scale > 0.4) {
                this.scale -= 0.2
            }
        },

        reset() {
            this.scale = 1
            this.translateX = 0
            this.translateY = 0
        },

        startDrag(e) {
            if (this.scale <= 1) return
            this.isDragging = true
            this.startX = e.clientX - this.translateX
            this.startY = e.clientY - this.translateY
        },

        onDrag(e) {
            if (!this.isDragging) return
            this.translateX = e.clientX - this.startX
            this.translateY = e.clientY - this.startY
        },

        endDrag() {
            this.isDragging = false
        },
    },
}
</script>

<style scoped>
.overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.85);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 9999;
}

.close-btn {
    position: fixed;
    top: 20px;
    right: 20px;
    background: rgba(255, 255, 255, 0.2);
    border: none;
    color: white;
    font-size: 40px;
    cursor: pointer;
    width: 50px;
    height: 50px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    line-height: 1;
}

.close-btn:hover {
    background: rgba(255, 255, 255, 0.3);
}

.image-container {
    width: 80%;
    height: 80%;
    overflow: hidden;
    cursor: grab;
    display: flex;
    align-items: center;
    justify-content: center;
}

.image-container:active {
    cursor: grabbing;
}

.zoom-image {
    transition: transform 0.1s linear;
    user-select: none;
    max-width: 100%;
    max-height: 100%;
}

.toolbar {
    position: fixed;
    bottom: 40px;
    display: flex;
    gap: 10px;
}

.toolbar button {
    background: rgba(255, 255, 255, 0.2);
    border: none;
    color: white;
    padding: 10px 20px;
    font-size: 16px;
    cursor: pointer;
    border-radius: 5px;
}

.toolbar button:hover {
    background: rgba(255, 255, 255, 0.3);
}
</style>
