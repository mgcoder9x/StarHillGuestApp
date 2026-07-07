<template>
    <div
        ref="wrap"
        class="webrtc-wrap"
        :class="wrapperClass"
        @click="handleWrapperClick"
    >
        <slot />

        <!-- AI Bbox overlay -->
        <BboxOverlay :detections="localDetections" />
        <!-- Persistent polygon overlay -->
        <ZoneOverlay :zones="persistentZones" />

        <!-- Placeholder khi chưa có video -->
        <div v-if="!source || !isConnected" class="webrtc-placeholder">
            <div class="placeholder-content">
                <feather-icon icon="VideoOffIcon" size="48" />
                <p v-if="!source" class="mt-2">Chưa chọn camera</p>
                <p v-else-if="!isConnected" class="mt-2">Đang kết nối...</p>
            </div>
        </div>

        <video
            :id="'video' + videoIndex"
            ref="video"
            :class="['webrtc-video', customClass]"
            autoplay
            playsinline
            muted
        ></video>
        <div>
            <p v-if="source && cameraName" class="btn-icon webrtc-cam-name">
                {{ cameraName }}
            </p>

            <b-button
                v-if="source && isPause"
                variant="outline-primary-player"
                class="btn-icon webrtc-pause"
                @click="play"
            >
                <feather-icon icon="PlayIcon" />
            </b-button>

            <b-button
                v-else-if="source"
                variant="outline-primary-player"
                class="btn-icon webrtc-pause"
                @click="pause"
            >
                <feather-icon icon="PauseIcon" />
            </b-button>

            <b-button
                v-if="source && showScreenshot"
                variant="outline-primary-player"
                class="btn-icon webrtc-screenshot"
                @click="screenshot"
            >
                <feather-icon icon="CameraIcon" />
            </b-button>

            <b-button
                v-if="source"
                variant="outline-primary-player"
                class="btn-icon webrtc-fullscreen"
                @click="toggleFullscreen"
            >
                <feather-icon icon="MaximizeIcon" />
            </b-button>
        </div>
    </div>
</template>

<script>
/* eslint-disable */
import BboxOverlay from '@/components/Live/BboxOverlay.vue'
import ZoneOverlay from '@/components/Live/ZoneOverlay.vue'
export default {
    name: 'WebRTCPlayer',
    components: {
        BboxOverlay,
        ZoneOverlay,
    },
    props: {
        videoIndex: { type: Number, default: 0 },
        source: { type: String, default: '' },
        showScreenshot: { type: Boolean, default: true },
        customClass: { type: String, default: '' },
        wrapperClass: { type: [String, Array], default: '' },
    },
    data() {
        return {
            peerConnection: null,
            isPause: false,
            width: 0,
            height: 0,
            camId: 0,
            cameraName: '',
            fps: 0,
            cameraCode: '',

            // AI overlay data
            localDetections: [],
            localZones: [],
            persistentZones: [],
            _bboxTimer: null,
            _zoneTimers: {},

            // fullscreen state
            isFs: false,
            fsMode: 'fill', // 'fill' or 'sharp'

            // WebRTC state
            isConnected: false,
            reconnectAttempts: 0,
            maxReconnectAttempts: 3,
        }
    },
    watch: {
        source(val) {
            if (val) {
                this.isPause = false
                this.changeSource()
            } else if (this.peerConnection) {
                this.stop()
            }
        },
    },
    methods: {
        getVideoEl() {
            return this.$refs.video
        },

        async init() {
            const video = this.getVideoEl()
            if (!video) return

            // Lắng nghe sự kiện metadata để lấy kích thước video
            video.addEventListener('loadedmetadata', () => {
                this.width = video.videoWidth
                this.height = video.videoHeight
            })

            // Lắng nghe fullscreen change
            document.addEventListener(
                'fullscreenchange',
                this.onFullscreenChange
            )
            // Phím tắt đổi chế độ trong fullscreen (Shift + F)
            window.addEventListener('keydown', this.onKey, { passive: true })

            if (this.source) {
                await this.connectWebRTC()
            }
        },

        async connectWebRTC() {
            try {
                // Tạo RTCPeerConnection
                this.peerConnection = new RTCPeerConnection({
                    iceServers: [
                        { urls: 'stun:stun.l.google.com:19302' },
                        { urls: 'stun:stun1.l.google.com:19302' },
                    ],
                })

                // Xử lý track từ remote
                this.peerConnection.ontrack = (event) => {
                    const video = this.getVideoEl()
                    if (video && event.streams[0]) {
                        video.srcObject = event.streams[0]
                        this.isConnected = true
                        this.reconnectAttempts = 0
                    }
                }

                // Xử lý ICE connection state
                this.peerConnection.oniceconnectionstatechange = () => {
                    const state = this.peerConnection.iceConnectionState
                    console.log('ICE Connection State:', state)

                    if (state === 'disconnected' || state === 'failed') {
                        this.handleConnectionFailure()
                    } else if (state === 'connected') {
                        this.isConnected = true
                    }
                }

                // Tạo offer
                const offer = await this.peerConnection.createOffer({
                    offerToReceiveAudio: true,
                    offerToReceiveVideo: true,
                })

                await this.peerConnection.setLocalDescription(offer)

                // Gửi offer đến server qua HTTP (WHEP protocol)
                await this.sendOfferToServer(offer)
            } catch (error) {
                console.error('WebRTC connection error:', error)
                this.handleConnectionFailure()
            }
        },

        async sendOfferToServer(offer) {
            try {
                // WHEP protocol: POST offer SDP to source URL
                const response = await fetch(this.source, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/sdp',
                    },
                    body: offer.sdp,
                })

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`)
                }

                // Nhận answer SDP từ server
                const answerSdp = await response.text()
                const answer = {
                    type: 'answer',
                    sdp: answerSdp,
                }

                await this.peerConnection.setRemoteDescription(
                    new RTCSessionDescription(answer)
                )
            } catch (error) {
                console.error('Error sending offer to server:', error)
                // Fallback: thử custom signaling nếu WHEP không work
                await this.tryCustomSignaling(offer)
            }
        },

        async tryCustomSignaling(offer) {
            try {
                // Custom signaling endpoint: thêm /webrtc vào cuối URL
                const signalingUrl = this.source.replace(/\/$/, '') + '/webrtc'

                const response = await fetch(signalingUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        type: 'offer',
                        sdp: offer.sdp,
                    }),
                })

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`)
                }

                const data = await response.json()
                const answer = {
                    type: 'answer',
                    sdp: data.sdp || data.answer,
                }

                await this.peerConnection.setRemoteDescription(
                    new RTCSessionDescription(answer)
                )
            } catch (error) {
                console.error('Custom signaling also failed:', error)
                throw error
            }
        },

        handleConnectionFailure() {
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                this.reconnectAttempts++
                console.log(`Reconnecting... attempt ${this.reconnectAttempts}`)
                setTimeout(() => {
                    this.changeSource()
                }, 2000 * this.reconnectAttempts) // Exponential backoff
            } else {
                console.error('Max reconnection attempts reached')
                this.isConnected = false
            }
        },

        async changeSource() {
            if (!this.source) return

            this.stop()
            await this.$nextTick()
            await this.connectWebRTC()
        },

        screenshot() {
            const video = this.getVideoEl()
            if (!video) return

            // Tạo canvas để capture frame
            const canvas = document.createElement('canvas')
            canvas.width = video.videoWidth || 1920
            canvas.height = video.videoHeight || 1080

            const ctx = canvas.getContext('2d')
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height)

            // Tải xuống ảnh
            canvas.toBlob(
                (blob) => {
                    const url = URL.createObjectURL(blob)
                    const a = document.createElement('a')
                    a.href = url
                    a.download = `screenshot-${Date.now()}.jpeg`
                    a.click()
                    URL.revokeObjectURL(url)
                },
                'image/jpeg',
                0.8
            )
        },

        stop() {
            if (this.peerConnection) {
                this.peerConnection.close()
                this.peerConnection = null
            }

            const video = this.getVideoEl()
            if (video && video.srcObject) {
                video.srcObject.getTracks().forEach((track) => track.stop())
                video.srcObject = null
            }

            this.isPause = false
            this.isConnected = false
        },

        pause() {
            const video = this.getVideoEl()
            if (video && !video.paused) {
                video.pause()
                this.isPause = true
            }
        },

        play() {
            const video = this.getVideoEl()
            if (video && video.paused) {
                video.play()
                this.isPause = false
            } else if (!this.isConnected && this.source) {
                // Nếu chưa connect thì connect lại
                this.changeSource()
            }
        },

        async toggleFullscreen() {
            if (document.fullscreenElement) {
                await document.exitFullscreen?.()
                return
            }
            await this.$refs.wrap.requestFullscreen?.()
        },

        onKey(e) {
            if (!this.isFs) return
            if (e.key?.toLowerCase() === 'f' && e.shiftKey) {
                this.fsMode = this.fsMode === 'fill' ? 'sharp' : 'fill'
                this.applyFsMode()
            }
        },

        applyFsMode() {
            const video = this.getVideoEl()
            if (!video) return

            if (this.fsMode === 'fill') {
                // Cover: full khung, có thể cắt bớt
                video.style.objectFit = 'cover'
            } else {
                // Contain: giữ tỷ lệ, có viền đen
                video.style.objectFit = 'contain'
            }
        },

        onFullscreenChange() {
            const isSelfFs = document.fullscreenElement === this.$refs.wrap
            const video = this.getVideoEl()

            if (isSelfFs && !this.isFs) {
                // Vừa vào fullscreen
                this.isFs = true
                if (video) {
                    video.classList.add('fs-video-fill')
                    this.applyFsMode()
                }
            } else if (!document.fullscreenElement && this.isFs) {
                // Vừa thoát fullscreen
                this.isFs = false
                if (video) {
                    video.classList.remove('fs-video-fill')
                    video.style.objectFit = 'contain'
                }
            }
        },

        getFrame() {
            const video = this.getVideoEl()
            if (!video) return

            const canvas = document.createElement('canvas')
            canvas.width = video.videoWidth || 1920
            canvas.height = video.videoHeight || 1080

            const ctx = canvas.getContext('2d')
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height)

            const base64 = canvas.toDataURL('image/jpeg', 0.8)

            setTimeout(() => {
                this.$emit('base64-img', base64)
            }, 100)
        },

        handleWrapperClick(e) {
            // Emit click event for parent to handle
            this.$emit('wrapper-click', e)
        },

        /**
         * Nhận bbox detections từ AI (bbox.json format)
         * Tự động clear sau duration ms
         */
        setDetections(detections, duration = 3000) {
            console.log(detections)
            this.localDetections = detections || []
            if (this._bboxTimer) clearTimeout(this._bboxTimer)
            if (duration > 0) {
                this._bboxTimer = setTimeout(() => {
                    this.localDetections = []
                }, duration)
            }
        },

        clearDetections() {
            this.localDetections = []
            if (this._bboxTimer) clearTimeout(this._bboxTimer)
        },

        /**
         * Nhận zone event (event.json format)
         * Highlight zone rồi tự tắt sau duration
         */
        setZones(zones, duration = 5000) {
            this.localZones = zones || []
            // Tự clear từng zone sau duration
            zones.forEach((z) => {
                const key = z.zoneId
                if (this._zoneTimers[key]) clearTimeout(this._zoneTimers[key])
                this._zoneTimers[key] = setTimeout(() => {
                    this.localZones = this.localZones.filter(
                        (item) => item.zoneId !== key
                    )
                    delete this._zoneTimers[key]
                }, duration)
            })
        },

        addZone(zone, duration = 5000) {
            // Thêm hoặc cập nhật 1 zone
            const idx = this.localZones.findIndex(
                (z) => z.zoneId === zone.zoneId
            )
            if (idx >= 0) {
                this.$set(this.localZones, idx, zone)
            } else {
                this.localZones.push(zone)
            }
            const key = zone.zoneId
            if (this._zoneTimers[key]) clearTimeout(this._zoneTimers[key])
            this._zoneTimers[key] = setTimeout(() => {
                this.localZones = this.localZones.filter(
                    (item) => item.zoneId !== key
                )
                delete this._zoneTimers[key]
            }, duration)
        },

        clearZones() {
            this.localZones = []
            if (this._zoneTimers) {
                Object.values(this._zoneTimers).forEach(clearTimeout)
            }
            this._zoneTimers = {}
        },

        /**
         * Hiển thị polygon cố định (không tự clear) - dùng để vẽ vùng nhận diện đã lưu
         * @param {Array} zones - Mảng zone objects với points đã normalized (0-1)
         */
        setPersistentZones(zones) {
            this.persistentZones = zones || []
        },

        clearPersistentZones() {
            this.persistentZones = []
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
        this.stop()
    },
}
</script>

<style lang="scss" scoped>
.webrtc-wrap {
    position: relative;
    width: 100%;
    height: 100%;
    background-color: #000;
    aspect-ratio: 16 / 9;
    min-height: 200px;
}

.webrtc-video {
    display: block;
    width: 100%;
    height: 100%;
    object-fit: contain;
    background-color: #000;
}

/* Placeholder khi chưa có video */
.webrtc-placeholder {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: #000;
    color: #6c757d;
    z-index: 1;
}

.placeholder-content {
    text-align: center;
    opacity: 0.7;
}

.placeholder-content p {
    margin: 0;
    font-size: 14px;
    color: #adb5bd;
}

/* Khi fullscreen: cho video chiếm khung chứa */
.fs-video-fill {
    display: block !important;
    width: 100% !important;
    height: 100% !important;
}

/* Wrapper ở fullscreen mode không cần aspect-ratio */
.webrtc-wrap:fullscreen {
    aspect-ratio: unset;
    min-height: unset;
    width: 100vw;
    height: 100vh;
}

/* Nút overlay */
.webrtc-screenshot {
    position: absolute;
    bottom: 10px;
    right: 45px;
}

.webrtc-fullscreen {
    position: absolute;
    bottom: 10px;
    right: 5px;
}

.webrtc-pause {
    position: absolute;
    bottom: 10px;
    right: 85px;
}

.webrtc-cam-name {
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
