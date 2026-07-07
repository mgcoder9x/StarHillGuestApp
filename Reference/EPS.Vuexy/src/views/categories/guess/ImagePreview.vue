<template>
    <b-row class="mt-3">
        <!-- Mặt trước -->
        <b-col cols="6" class="text-center mb-4">
            <label
                v-if="!viewOnly"
                class="d-flex justify-content-center align-items-center mb-2"
                style="cursor: pointer"
            >
                <b-form-radio
                    v-model="selectedSide"
                    value="front"
                    name="image-side"
                />
                <strong class="mb-0">Mặt trước</strong>
            </label>

            <b-img
                v-if="cardFront"
                :src="cardFront"
                fluid
                alt="Ảnh mặt trước"
                class="border p-2"
                style="max-height: 200px"
            />
            <b-img
                v-else-if="frontImgBase64"
                :src="`data:image/jpeg;base64,${frontImgBase64}`"
                fluid
                alt="Ảnh mặt trước"
                class="border p-2"
                style="max-height: 200px"
            />

            <!-- Upload mặt trước -->
            <b-form-file
                v-if="selectedSide === 'front' && !viewOnly"
                accept="image/*"
                class="mt-2"
                plain
                @change="handleImageUpload('front', $event)"
            />
        </b-col>

        <!-- Mặt sau -->
        <b-col cols="6" class="text-center">
            <label
                v-if="!viewOnly"
                class="d-flex justify-content-center align-items-center mb-2"
                style="cursor: pointer"
            >
                <b-form-radio
                    v-model="selectedSide"
                    value="back"
                    name="image-side"
                />
                <strong class="mb-0">Mặt sau</strong>
            </label>

            <b-img
                v-if="cardBack"
                :src="cardBack"
                fluid
                alt="Ảnh mặt sau"
                class="border p-2"
                style="max-height: 200px"
            />
            <b-img
                v-else-if="backImgBase64"
                :src="`data:image/jpeg;base64,${backImgBase64}`"
                fluid
                alt="Ảnh mặt sau"
                class="border p-2"
                style="max-height: 200px"
            />

            <!-- Upload mặt sau -->
            <b-form-file
                v-if="selectedSide === 'back' && !viewOnly"
                @change="handleImageUpload('back', $event)"
                accept="image/*"
                class="mt-2"
                plain
            />
        </b-col>
    </b-row>
</template>

<script>
// import Services from '@/utils/services'

export default {
    props: {
        cardFront: {
            type: String,
            default: null,
        },
        cardBack: {
            type: String,
            default: null,
        },
        viewOnly: {
            type: Boolean,
            default: false,
        },
    },
    data() {
        return {
            frontImgBase64: '',
            backImgBase64: '',
            selectedSide: 'front',
        }
    },
    watch: {
        frontImgBase64(val) {
            if (val && val.length > 0) {
                this.selectedSide = 'back'
            }
        },
    },
    methods: {
        handleImageUpload(side, event) {
    const file = event.target.files[0]
    if (!file) return

    const reader = new FileReader()
    reader.onload = (e) => {
      const base64 = e.target.result.split(',')[1]
      if (side === 'front') {
        this.frontImgBase64 = base64
      } else {
        this.backImgBase64 = base64
      }
     // Báo cho cha biết ảnh nào đã đổi (lưu cả vào storage ở cha)
     this.$emit('change', { side, base64 })
    }
    reader.readAsDataURL(file)
  },

        clear() {
            this.frontImgBase64 = ''
            this.backImgBase64 = ''
        },
    },
}
</script>
