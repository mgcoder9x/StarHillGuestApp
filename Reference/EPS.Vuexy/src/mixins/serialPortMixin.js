/**
 * Mixin để sử dụng Serial Port Service trong các component
 * Tự động lắng nghe dữ liệu từ đầu đọc thẻ và xử lý
 */
import serialPortService from '@/services/serialPortService'
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    data() {
        return {
            unsubscribeSerialData: null,
            serialPortConnected: false,
        }
    },
    mounted() {
        // Kiểm tra trạng thái kết nối
        const connectionStatus = serialPortService.getConnectionStatus()
        this.serialPortConnected = connectionStatus.isConnected

        // Đăng ký lắng nghe dữ liệu từ Serial Port
        this.unsubscribeSerialData = serialPortService.onDataReceived(
            (data) => {
                if (this.onSerialDataReceived) {
                    console.log(`Đã đọc thẻ: ${data}`)
                    this.onSerialDataReceived(data)
                }
            }
        )

        // Đăng ký lắng nghe thay đổi trạng thái kết nối
        this.unsubscribeSerialStatus =
            serialPortService.onConnectionStatusChange((status) => {
                this.serialPortConnected = status.type === 'success'
            })
    },
    beforeDestroy() {
        // Hủy đăng ký khi component bị destroy
        if (this.unsubscribeSerialData) {
            this.unsubscribeSerialData()
        }
        if (this.unsubscribeSerialStatus) {
            this.unsubscribeSerialStatus()
        }
    },
    methods: {
        // Method mặc định để xử lý dữ liệu từ Serial Port
        // Component con có thể override method này
        onSerialDataReceived(data) {
            console.log('Serial data received:', data)
        },

        // Helper method để fill dữ liệu vào field
        fillCardIdFromSerial(data, fieldName = 'cardId', objectName = null) {
            if (data && data.trim()) {
                const cleanData = data
                    .trim()
                    .replace(/[\x00-\x1F\x7F-\x9F]/g, '')
                console.log('data read:', cleanData)
                if (!cleanData) return null
                // Nếu có objectName (ví dụ: 'newVehicleCard'), set value cho object.field
                if (objectName && this[objectName]) {
                    this[objectName][fieldName] = cleanData
                } else {
                    // Ngược lại, set trực tiếp cho field
                    this[fieldName] = cleanData
                }

                // Hiển thị toast notification
                this.showCardReadSuccessToast(cleanData)

                return cleanData
            }
            return null
        },
        stringToHex(str) {
            if (!str) return ''
            let hex = ''
            for (let i = 0; i < str.length; i++) {
                const charCode = str.charCodeAt(i)
                hex += `${charCode.toString(16).padStart(2, '0').toUpperCase()} `
            }
            return hex.trim()
        },

        // Toast notification khi đọc thẻ thành công
        showCardReadSuccessToast(cardId) {
            this.$toast({
                component: ToastificationContent,
                position: 'top-right',
                props: {
                    title: 'Đã đọc thẻ thành công',
                    icon: 'CreditCardIcon',
                    variant: 'success',
                    text: `ID thẻ: ${cardId}`,
                },
            })
        },

        // Kiểm tra Serial Port có đang kết nối không
        isSerialPortConnected() {
            return serialPortService.getConnectionStatus().isConnected
        },

        // Gửi dữ liệu qua Serial Port
        async sendToSerialPort(data) {
            try {
                await serialPortService.sendData(data)
                return true
            } catch (err) {
                console.error('Error sending data to serial port:', err)
                return false
            }
        },
    },
}
