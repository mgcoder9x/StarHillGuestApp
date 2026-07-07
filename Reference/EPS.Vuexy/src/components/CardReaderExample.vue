<template>
    <div class="card-reader-example">
        <b-card title="Ví dụ sử dụng Đầu đọc thẻ">
            <!-- Trạng thái kết nối -->
            <b-alert
                :show="true"
                :variant="serialPortConnected ? 'success' : 'warning'"
            >
                <div class="d-flex align-items-center">
                    <feather-icon
                        :icon="serialPortConnected ? 'WifiIcon' : 'WifiOffIcon'"
                        size="18"
                        class="mr-50"
                    />
                    <span>
                        {{
                            serialPortConnected
                                ? 'Đầu đọc thẻ đã kết nối'
                                : 'Đầu đọc thẻ chưa kết nối'
                        }}
                    </span>
                </div>
            </b-alert>

            <!-- Form nhập thông tin -->
            <b-form @submit.prevent="onSubmit">
                <b-row>
                    <b-col md="6">
                        <b-form-group
                            label="ID Thẻ"
                            label-for="card-id"
                            description="Quẹt thẻ hoặc nhập thủ công"
                        >
                            <b-input-group>
                                <b-form-input
                                    id="card-id"
                                    v-model="formData.cardId"
                                    placeholder="Quẹt thẻ..."
                                    :state="cardIdState"
                                />
                                <b-input-group-append>
                                    <b-button
                                        variant="outline-primary"
                                        @click="openSerialPortConfig"
                                    >
                                        <feather-icon icon="SettingsIcon" />
                                    </b-button>
                                </b-input-group-append>
                            </b-input-group>
                            <b-form-invalid-feedback :state="cardIdState">
                                Vui lòng nhập ID thẻ
                            </b-form-invalid-feedback>
                        </b-form-group>
                    </b-col>

                    <b-col md="6">
                        <b-form-group label="Tên thẻ" label-for="card-name">
                            <b-form-input
                                id="card-name"
                                v-model="formData.cardName"
                                placeholder="Nhập tên thẻ"
                            />
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-row>
                    <b-col md="12">
                        <b-form-group label="Ghi chú" label-for="note">
                            <b-form-textarea
                                id="note"
                                v-model="formData.note"
                                placeholder="Nhập ghi chú"
                                rows="3"
                            />
                        </b-form-group>
                    </b-col>
                </b-row>

                <b-button type="submit" variant="primary">
                    <feather-icon icon="SaveIcon" class="mr-50" />
                    Lưu thẻ
                </b-button>
            </b-form>

            <!-- Log nhận thẻ -->
            <b-card v-if="cardReadLogs.length" class="mt-2" title="Log đọc thẻ">
                <b-list-group>
                    <b-list-group-item
                        v-for="(log, index) in cardReadLogs"
                        :key="index"
                    >
                        <div class="d-flex justify-content-between">
                            <span>{{ log.cardId }}</span>
                            <small class="text-muted">{{ log.time }}</small>
                        </div>
                    </b-list-group-item>
                </b-list-group>
            </b-card>
        </b-card>

        <!-- Modal Serial Port Config -->
        <b-modal
            id="serial-port-config"
            v-model="showSerialPortModal"
            title="Cấu hình đầu đọc thẻ"
            size="lg"
            hide-footer
        >
            <SerialPortPopup />
        </b-modal>
    </div>
</template>

<script>
import SerialPortPopup from '@/components/SerialPortPopup.vue'
import serialPortMixin from '@/mixins/serialPortMixin'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    name: 'CardReaderExample',
    components: {
        SerialPortPopup,
    },
    mixins: [serialPortMixin],
    data() {
        return {
            showSerialPortModal: false,
            formData: {
                cardId: null,
                cardName: null,
                note: null,
            },
            cardReadLogs: [],
            cardIdState: null,
        }
    },
    methods: {
        // Override method từ serialPortMixin
        onSerialDataReceived(data) {
            console.log('📡 Nhận dữ liệu từ đầu đọc thẻ:', data)

            // Validate dữ liệu (ví dụ: chỉ chấp nhận số)
            const cleanData = data.trim()
            if (this.validateCardId(cleanData)) {
                // Fill vào form
                this.fillCardIdFromSerial(cleanData, 'cardId', 'formData')

                // Lưu log
                this.cardReadLogs.unshift({
                    cardId: cleanData,
                    time: new Date().toLocaleTimeString(),
                })

                // Giới hạn log (chỉ giữ 10 log gần nhất)
                if (this.cardReadLogs.length > 10) {
                    this.cardReadLogs.pop()
                }

                // Focus vào field tiếp theo
                this.$nextTick(() => {
                    const cardNameInput = document.getElementById('card-name')
                    if (cardNameInput) {
                        cardNameInput.focus()
                    }
                })

                this.cardIdState = true
            } else {
                this.showInvalidCardToast(cleanData)
                this.cardIdState = false
            }
        },

        validateCardId(cardId) {
            // Ví dụ: chỉ chấp nhận chuỗi số từ 6-12 ký tự
            return /^\d{6,12}$/.test(cardId)
        },

        showInvalidCardToast(cardId) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: 'ID thẻ không hợp lệ',
                    icon: 'AlertTriangleIcon',
                    variant: 'warning',
                    text: `Format yêu cầu: 6-12 chữ số. Nhận được: ${cardId}`,
                },
            })
        },

        openSerialPortConfig() {
            this.showSerialPortModal = true
        },

        onSubmit() {
            if (!this.formData.cardId) {
                this.cardIdState = false
                return
            }

            this.cardIdState = true

            // Xử lý submit form
            console.log('Submit form:', this.formData)

            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: 'Thành công',
                    icon: 'CheckIcon',
                    variant: 'success',
                    text: 'Đã lưu thông tin thẻ',
                },
            })

            // Reset form
            this.resetForm()
        },

        resetForm() {
            this.formData = {
                cardId: null,
                cardName: null,
                note: null,
            }
            this.cardIdState = null
        },
    },
}
</script>

<style lang="scss" scoped>
.card-reader-example {
    padding: 1rem;
}
</style>
