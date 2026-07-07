<template>
    <div ref="wrap" class="flv-wrap">
        <slot />
        <div>
            <p class="btn-icon flv-cam-name">{{ cameraName }}</p>

            <b-button
                v-if="isPause"
                variant="outline-primary-player"
                class="btn-icon flv-pause"
                @click="play"
                ><feather-icon icon="PlayIcon"
            /></b-button>

            <b-button
                v-else
                variant="outline-primary-player"
                class="btn-icon flv-pause"
                @click="pause"
                ><feather-icon icon="PauseIcon"
            /></b-button>

            <b-button
                variant="outline-primary-player"
                class="btn-icon flv-screenshot"
                @click="screenshot"
                ><feather-icon icon="CameraIcon"
            /></b-button>

            <b-button
                variant="outline-primary-player"
                class="btn-icon flv-fullscreen"
                @click="toggleFullscreen"
                ><feather-icon icon="MaximizeIcon"
            /></b-button>
        </div>
    </div>
</template>

<script>
/* eslint-disable */
export default {
    name: 'flvPlayer',
    props: {
        videoIndex: { type: Number, default: 0 },
        source: { type: String, default: '' },
        showScreenshot: { type: Boolean, default: true },
    },
    data() {
        return {
            player: null,
            isPause: false,
            width: 0,
            height: 0,
            camId: 0,
            cameraName: '',
            fps: 0,
            imgDefault:
                'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAGQAAAAmCAIAAAC9EKlkAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAiSURBVGhD7cExAQAAAMKg9U9tCj8gAAAAAAAAAAAAAICDGiyuAAFol4RQAAAAAElFTkSuQmCC',

            // fullscreen state
            isFs: false,
            prevScaleMode: 0,

            // NEW: ưu tiên đầy màn hay ưu tiên nét
            // 'fill' = setScaleMode(1), 'sharp' = setScaleMode(0)
            fsMode: 'fill',
        }
    },
    watch: {
        source(val, oldVal) {
            // Chỉ xử lý khi thực sự có thay đổi
            if (val === oldVal) return
            
            if (val) {
                this.isPause = false
                this.changeSource()
            } else if (oldVal) {
                // Chỉ stop khi có oldVal (đang chơi mà bị set null)
                this.stopAndClear()
            }
        },
    },
    methods: {
        getCanvasEl() {
            return document.getElementById('video' + this.videoIndex)
        },

        init() {
            const canv = this.getCanvasEl()
            this.player = new window.NodePlayer()
            this.player.setView(canv.id)
            this.player.setScaleMode(0) // mặc định ngoài fullscreen: letterbox
            this.prevScaleMode = 0
            this.player.setBufferTime(500)
            this.player.skipLoopFilter(32)

            this.player.on('videoInfo', (w, h) => {
                this.width = w
                this.height = h
            })
            this.player.on('stats', (s) => {
                this.fps = s.fps
            })

            if (this.source) this.player.start(this.source)

            document.addEventListener(
                'fullscreenchange',
                this.onFullscreenChange
            )
            // phím tắt đổi chế độ trong fullscreen (Shift + F)
            window.addEventListener('keydown', this.onKey, { passive: true })
        },

        stopAndClear() {
            if (!this.player) return
            try {
                this.player.stop()
                this.player.clearView?.()
                this.isPause = true
            } catch (e) {
                console.warn('Error stopping player:', e)
            }
        },
        
        changeSource() {
            if (!this.player) return
            
            // Stop player hiện tại hoàn toàn
            this.stopAndClear()
            
            // Đợi cleanup xong hoàn toàn
            setTimeout(() => {
                if (!this.source || !this.player) return
                
                try {
                    this.player.start(this.source)
                    this.isPause = false
                } catch (e) {
                    console.error('Error starting player:', e)
                    // Retry với timeout dài hơn
                    setTimeout(() => {
                        if (this.source && this.player) {
                            try {
                                this.player.start(this.source)
                                this.isPause = false
                            } catch (e2) {
                                console.error('Retry failed:', e2)
                            }
                        }
                    }, 600)
                }
            }, 200)
        },

        screenshot() {
            this.player?.screenshot('screenshot.jpeg', 'jpeg', 0.8)
        },
        stop() {
            this.stopAndClear()
        },
        pause() {
            if (this.player) {
                this.isPause = true
                this.player.stop()
            }
        },
        play() {
            if (this.player && this.source) {
                this.isPause = false
                this.player.start(this.source)
            }
        },

        async toggleFullscreen() {
            if (document.fullscreenElement) {
                await document.exitFullscreen?.()
                return
            }
            await this.$refs.wrap.requestFullscreen?.()
        },

        getFrame() {
            this.player.getFrame('jpge', 0.8)
            setTimeout(() => {
                let data = localStorage.getItem('frame')
                this.$emit('base64-img', data)
                localStorage.setItem('frame', null)
            }, 2500)
        },

        // NEW: đổi chế độ giữa fill ↔ sharp khi đang fullscreen
        onKey(e) {
            if (!this.isFs) return
            if (e.key?.toLowerCase() === 'f' && e.shiftKey) {
                this.fsMode = this.fsMode === 'fill' ? 'sharp' : 'fill'
                // áp lại scaleMode ngay
                try {
                    this.player.setScaleMode(this.fsMode === 'fill' ? 1 : 0)
                } catch (e) {}
            }
        },

        forceRelayout() {
            const cvs = this.getCanvasEl()
            if (!cvs || !this.player) return

            // 1) Rebind view để NodePlayer lấy lại kích thước canvas hiện tại
            try {
                this.player.setView(cvs.id || cvs)
            } catch (e) {}

            // 2) Áp lại scaleMode cho đúng behavior mong muốn khi fullscreen
            // 0 = stretch, 1 = contain (có viền đen), 2 = cover (cắt bớt, không viền, không méo)
            const mode = this.fsMode === 'fill' ? 2 : 1 // gợi ý: fullscreen nên dùng 2 để “full khung”, không viền
            try {
                this.player.setScaleMode(mode)
            } catch (e) {}
        },
        onFullscreenChange() {
            const cvs = this.getCanvasEl()
            const isSelfFs = document.fullscreenElement === this.$refs.wrap

            cvs.width = document.body.clientWidth
            cvs.height = document.body.clientHeight
            this.$nextTick(() => {
                requestAnimationFrame(() => {
                    this.forceRelayout()
                })
            })
            if (isSelfFs && !this.isFs) {
                // vừa vào fullscreen
                this.isFs = true
                this.prevScaleMode =
                    typeof this.prevScaleMode === 'number'
                        ? this.prevScaleMode
                        : 0
            } else if (!document.fullscreenElement && this.isFs) {
                // vừa thoát fullscreen
                this.isFs = false
            }
        },
    },
    mounted() {
        this.init()
    },
    beforeDestroy() {
        document.removeEventListener(
            'fullscreenchange',
            this.onFullscreenChange
        )
        window.removeEventListener('keydown', this.onKey)
        
        // Cleanup player đúng cách
        if (this.player) {
            try {
                this.player.stop()
                this.player.clearView?.()
                this.player.release?.()
                this.player = null
            } catch (e) {
                console.warn('Error destroying player:', e)
            }
        }
    },
}
</script>

<style lang="scss">
.flv-wrap {
    position: relative;
    width: 100%;
    height: 100%;
}

/* Khi fullscreen: cho canvas chiếm khung chứa (đúng code của bạn) */
.fs-canvas-fill {
    display: block !important;
    width: 100% !important;
    height: 100% !important;
}

/* Nút overlay giữ nguyên */
.flv-screenshot {
    position: absolute;
    bottom: 10px;
    right: 45px;
}
.flv-fullscreen {
    position: absolute;
    bottom: 10px;
    right: 5px;
}
.flv-pause {
    position: absolute;
    bottom: 10px;
    right: 85px;
}
.flv-fps {
    position: absolute;
    top: 10px;
    right: 5px;
}
.flv-cam-name {
    position: absolute;
    top: 10px;
    left: 5px;
}

.btn-outline-primary-player {
    color: white !important;
}
.outline-primary-player {
    border: 1px solid transparent !important;
    background-color: transparent;
}
</style>
