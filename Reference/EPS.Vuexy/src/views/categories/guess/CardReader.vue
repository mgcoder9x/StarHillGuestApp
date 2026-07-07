<template>
    <div>
        <!-- Tiêu đề -->
        <!-- <h5 class="text-center mb-2">Loại giấy tờ</h5> -->

        <!-- 3 nút chọn loại giấy tờ chính -->
        <b-button-group id="doc-type-buttons" toggle class="w-100" size="sm">
            <b-button
                v-model="captureEnabled"
                class="rounded-pill font-weight-bold"
                @click="onScan()"
            >
                Scan
            </b-button>

            <b-button
                v-for="opt in docOptions.slice(0, 3)"
                :key="opt.value"
                class="rounded-pill font-weight-bold"
                size="sm"
                :variant="
                    documentType === opt.value ? 'primary' : 'outline-secondary'
                "
                @click="
                    documentType = opt.value
                    readQrCode(opt.value)
                "
            >
                {{ opt.label }}
            </b-button>
        </b-button-group>

        <!-- Toggle chụp giấy tờ -->

        <!-- Hiển thị 2 ảnh demo khi bật capture -->
        <b-modal v-model="captureEnabled" size="lg" hide-footer>
            <div
                v-if="captureEnabled || person.cardFront || person.cardBack"
                class="mt-3"
            >
                <ImagePreview
                    ref="imagePreview"
                    :card-front="person.cardFront"
                    :card-back="person.cardBack"
                    @change="onPreviewChange"
                />
            </div>
        </b-modal>
    </div>
</template>

<script>
import CardReaderSocket from '@/utils/cardReaderSocket-v2'
import ImagePreview from './ImagePreview.vue'
import { setStorage, getStorage, clearStorage } from '@/utils/cacheHelper'


export default {
    components: { ImagePreview },
    data() {
        return {
            STORAGE_KEYS: {
               front: 'cardReader.frontBase64',
               back:  'cardReader.backBase64',
            },
            docOptions: [
                { value: 1, label: 'CCCD' },
                { value: 2, label: 'QRCode' },
                { value: 4, label: 'Passport' },
            ],
            captureEnabled: false,
            person: {
                frontImgBase64: null,
                backImgBase64: null,
                cardFront: null,
                cardBack: null,
            },
            documentType: 1,
        }
    },
    mounted() {
       // Nạp lại ảnh đã lưu (nếu có)
       const f = sessionStorage.getItem(this.STORAGE_KEYS.front)
       const b = sessionStorage.getItem(this.STORAGE_KEYS.back)
       if (f) {
         this.person.frontImgBase64 = f
         this.person.cardFront = `data:image/jpeg;base64,${f}`
       }
       if (b) {
         this.person.backImgBase64 = b
         this.person.cardBack = `data:image/jpeg;base64,${b}`
       }
     },
    watch: {
        captureEnabled(newValue) {
            if (newValue) {
                this.selectedOption(5)
            }
        },
        documentType(newValue) {
            if (newValue) {
                this.selectedOption(newValue)
            }
        },
    },
    async created() {
        this.connectToCardReader()
        this.loadImagesFromStorage()
    },
    methods: {
        onPreviewChange({ side, base64 }) {
         // Cập nhật state cha
         if (side === 'front') {
           this.person.frontImgBase64 = base64
           this.person.cardFront = `data:image/jpeg;base64,${base64}`
         } else {
           this.person.backImgBase64 = base64
           this.person.cardBack = `data:image/jpeg;base64,${base64}`
         }
         this.saveToStorage()
       },
    
       saveToStorage() {
         if (this.person.frontImgBase64) {
           sessionStorage.setItem(this.STORAGE_KEYS.front, this.person.frontImgBase64)
         }
         if (this.person.backImgBase64) {
           sessionStorage.setItem(this.STORAGE_KEYS.back, this.person.backImgBase64)
         }
       },
    
       clearStorage() {
         sessionStorage.removeItem(this.STORAGE_KEYS.front)
         sessionStorage.removeItem(this.STORAGE_KEYS.back)
         this.person.frontImgBase64 = null
         this.person.backImgBase64 = null
         this.person.cardFront = null
         this.person.cardBack = null
         // Nếu modal đang mở, cũng clear child
         if (this.$refs.imagePreview) {
           this.$refs.imagePreview.clear?.()
         }
       },
        async connectToCardReader() {
            CardReaderSocket.ingestEvent(1)
            // Kết nối với socket và thiết lập callback để nhận dữ liệu
            await CardReaderSocket.connectSocket((data) => {
                if (this.captureEnabled) {
                    this.setImagePreview(data.base64)
                    return
                }
                if (
                    data.type !== 'Unknown' &&
                    data.type !== 'getDevice' &&
                    data.name !== 'EA09' &&
                    data.name !== 'Sino'
                ) {
                    this.$emit('person-info', {
                        ...data,
                        gender:
                            data.gender === 'Nam' || data.gender === 1 ? 1 : 0,
                    })
                }
            })
        },
        async onScan() {
            this.captureEnabled = !this.captureEnabled

            // Đợi modal được render xong
            this.$nextTick(() => {
                if (this.person.frontImgBase64) {
                    this.$refs.imagePreview.frontImgBase64 =
                        this.person.frontImgBase64
                }
                if (this.person.backImgBase64) {
                    this.$refs.imagePreview.backImgBase64 =
                        this.person.backImgBase64
                }
            })
        },
        setImagePreview(src) {
            if (this.$refs.imagePreview) {
                if (this.$refs.imagePreview.selectedSide === 'front') {
                    this.$refs.imagePreview.frontImgBase64 = src
                    this.person.frontImgBase64 = src
                    this.person.cardFront = `data:image/jpeg;base64,${src}`
                } else if (this.$refs.imagePreview.selectedSide === 'back') {
                    this.$refs.imagePreview.backImgBase64 = src
                    this.person.backImgBase64 = src
                    this.person.cardBack = `data:image/jpeg;base64,${src}`
                }
                this.saveToStorage()
            }
        },
        readQrCode(value) {
            if (value === 2) {
                CardReaderSocket.ingestEvent('get_qr')
            }
        },
        selectedOption(option) {
            if ([1, 2, 4].includes(option)) {
                this.captureEnabled = false
                CardReaderSocket.ingestEvent(option)
            } else {
                this.documentType = null
                CardReaderSocket.ingestEvent(option)
            }
        },
        loadImagesFromStorage() {
        const saved = getStorage('cardReader_images')  // Sử dụng getStorage (tự check expiry)
        if (saved) {
            const { front, back } = saved
            this.person.cardFront = front ? `data:image/jpeg;base64,${front}` : null
            this.person.cardBack = back ? `data:image/jpeg;base64,${back}` : null
            console.log('Loaded images from storage')
        } else {
            console.log('No valid images in storage (expired or empty)')
        }
    },

    // Cập nhật method save vào storage (gọi từ emit của ImagePreview)
    saveImagesToStorage({ side, base64 }) {
        try {
            const saved = getStorage('cardReader_images') || {}  // Load hiện tại nếu có
            saved[side] = base64  // Cập nhật side tương ứng
            setStorage('cardReader_images', saved, 600)  // 10 phút expiry (thay đổi nếu cần)
            console.log(`Saved ${side} image to storage (expires in 10 min)`)
        } catch (error) {
            console.error('Error saving images to storage:', error)
        }
    },

    // Listen emit từ ImagePreview (giữ nguyên, chỉ gọi save)
    onImageUpdated(payload) {
        const { side, base64 } = payload
        if (side === 'front') {
            this.person.cardFront = `data:image/jpeg;base64,${base64}`
        } else if (side === 'back') {
            this.person.cardBack = `data:image/jpeg;base64,${base64}`
        }
        this.saveImagesToStorage({ side, base64 })  // Lưu ngay với expiry
    },

    // Cập nhật method clear storage (gọi từ parent khi Cancel)
    clearImagesStorage() {
        clearStorage('cardReader_images')  // Sử dụng clearStorage
        this.person.cardFront = null
        this.person.cardBack = null
        if (this.$refs.imagePreview) {
            this.$refs.imagePreview.clear()
        }
        console.log('Cleared images storage')
    },
    },
}
</script>

<style scoped></style>
