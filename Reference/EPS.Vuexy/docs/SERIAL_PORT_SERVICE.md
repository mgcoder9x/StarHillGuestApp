# Serial Port Service - Hướng dẫn sử dụng

## Tổng quan

Serial Port Service là một singleton service để quản lý kết nối Serial Port trong ứng dụng. Service này duy trì kết nối ngay cả khi đóng UI component, cho phép nhiều component cùng lắng nghe và xử lý dữ liệu từ đầu đọc thẻ.

## Cấu trúc Files

```
src/
├── services/
│   └── serialPortService.js    # Singleton service quản lý Serial Port
├── mixins/
│   └── serialPortMixin.js      # Mixin để sử dụng trong components
└── components/
    └── SerialPortPopup.vue     # UI component để cấu hình và monitor Serial Port
```

## Tính năng

### 1. Serial Port Service (`serialPortService.js`)

- ✅ Singleton pattern - duy nhất một instance trong toàn app
- ✅ Duy trì kết nối khi đóng UI component
- ✅ Event-based architecture với callback listeners
- ✅ Auto reconnect và connection monitoring
- ✅ Support đọc/ghi dữ liệu Serial

### 2. Serial Port Mixin (`serialPortMixin.js`)

Mixin cung cấp các method tiện ích:

- `onSerialDataReceived(data)` - Override để xử lý dữ liệu nhận được
- `fillCardIdFromSerial(data, fieldName, objectName)` - Tự động fill dữ liệu vào field
- `showCardReadSuccessToast(cardId)` - Hiển thị notification
- `isSerialPortConnected()` - Kiểm tra trạng thái kết nối
- `sendToSerialPort(data)` - Gửi dữ liệu qua Serial Port

## Cách sử dụng

### Bước 1: Import Serial Port Mixin

```vue
<script>
import serialPortMixin from '@/mixins/serialPortMixin'

export default {
    mixins: [serialPortMixin],
    data() {
        return {
            newVehicleCard: {
                cardId: null,
                cardName: null,
                // ...
            },
        }
    },
}
</script>
```

### Bước 2: Override method `onSerialDataReceived`

```vue
<script>
export default {
    mixins: [serialPortMixin],
    methods: {
        // Override method này để xử lý dữ liệu từ đầu đọc thẻ
        onSerialDataReceived(data) {
            // Tự động fill vào trường cardId
            this.fillCardIdFromSerial(data, 'cardId', 'newVehicleCard')

            // Hoặc xử lý custom
            if (data && data.trim()) {
                this.newVehicleCard.cardId = data.trim()
                // Xử lý thêm...
            }
        },
    },
}
</script>
```

### Bước 3: Thêm Serial Port Popup vào component

```vue
<template>
    <div>
        <!-- Nút mở Serial Port Config -->
        <b-button @click="showSerialPortModal = true">
            Cấu hình Serial Port
        </b-button>

        <!-- Modal Serial Port -->
        <b-modal
            id="serial-port-modal"
            v-model="showSerialPortModal"
            title="Serial Port Connection"
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

export default {
    components: {
        SerialPortPopup,
    },
    mixins: [serialPortMixin],
    data() {
        return {
            showSerialPortModal: false,
        }
    },
}
</script>
```

## Ví dụ thực tế

### Example 1: Auto fill Card ID khi đọc thẻ

```vue
<template>
    <b-form-input
        v-model="newVehicleCard.cardId"
        placeholder="Quẹt thẻ hoặc nhập ID thủ công"
    />
</template>

<script>
import serialPortMixin from '@/mixins/serialPortMixin'

export default {
    mixins: [serialPortMixin],
    data() {
        return {
            newVehicleCard: { cardId: null },
        }
    },
    methods: {
        onSerialDataReceived(data) {
            // Tự động fill và focus next field
            this.fillCardIdFromSerial(data, 'cardId', 'newVehicleCard')
            this.$nextTick(() => {
                document.getElementById('next-field')?.focus()
            })
        },
    },
}
</script>
```

### Example 2: Validate dữ liệu trước khi fill

```vue
<script>
export default {
    mixins: [serialPortMixin],
    methods: {
        onSerialDataReceived(data) {
            const cleanData = data.trim()

            // Validate format (ví dụ: chỉ chấp nhận số, 10 ký tự)
            if (/^\d{10}$/.test(cleanData)) {
                this.fillCardIdFromSerial(cleanData, 'cardId', 'newVehicleCard')
            } else {
                this.$toast({
                    component: ToastificationContent,
                    props: {
                        title: 'ID thẻ không hợp lệ',
                        icon: 'AlertTriangleIcon',
                        variant: 'warning',
                        text: `Format yêu cầu: 10 chữ số. Nhận được: ${cleanData}`,
                    },
                })
            }
        },
    },
}
</script>
```

### Example 3: Parse dữ liệu phức tạp

```vue
<script>
export default {
    mixins: [serialPortMixin],
    methods: {
        onSerialDataReceived(data) {
            // Giả sử đầu đọc gửi format: "CARD:123456789|NAME:John Doe"
            const match = data.match(/CARD:(\w+)\|NAME:(.+)/)
            if (match) {
                const [, cardId, cardName] = match
                this.newVehicleCard.cardId = cardId
                this.newVehicleCard.cardName = cardName
                this.showCardReadSuccessToast(cardId)
            }
        },
    },
}
</script>
```

### Example 4: Sử dụng trực tiếp Service (không dùng mixin)

```vue
<script>
import serialPortService from '@/services/serialPortService'

export default {
    data() {
        return {
            unsubscribe: null,
        }
    },
    mounted() {
        // Lắng nghe dữ liệu
        this.unsubscribe = serialPortService.onDataReceived((data) => {
            console.log('Received:', data)
        })
    },
    beforeDestroy() {
        if (this.unsubscribe) {
            this.unsubscribe()
        }
    },
    methods: {
        async connectToPort() {
            const port = await serialPortService.requestPort()
            await serialPortService.openPort(port, {
                baudRate: 115200,
                dataBits: 8,
                parity: 'none',
                stopBits: 1,
            })
        },
        async sendCommand() {
            await serialPortService.sendData('READ_CARD')
        },
    },
}
</script>
```

## API Reference

### SerialPortService

#### Methods

- `isSupported()` - Kiểm tra Web Serial API có được hỗ trợ
- `getConnectionStatus()` - Lấy trạng thái kết nối hiện tại
- `requestPort()` - Yêu cầu chọn cổng Serial
- `openPort(port, config)` - Mở kết nối Serial Port
- `closePort()` - Đóng kết nối
- `sendData(data)` - Gửi dữ liệu qua Serial Port
- `onDataReceived(callback)` - Đăng ký callback nhận dữ liệu
- `onConnectionStatusChange(callback)` - Đăng ký callback thay đổi trạng thái
- `onLog(callback)` - Đăng ký callback nhận log

#### Config Options

```javascript
{
    baudRate: 115200,    // 9600, 19200, 38400, 57600, 115200
    dataBits: 8,         // 7, 8
    parity: 'none',      // 'none', 'even', 'odd'
    stopBits: 1          // 1, 2
}
```

## Lưu ý quan trọng

### 1. Kết nối được duy trì khi đóng popup

Khi bạn đóng `SerialPortPopup` component, kết nối Serial Port **KHÔNG bị đóng**. Điều này cho phép:

- Đọc thẻ liên tục mà không cần giữ popup mở
- Nhiều component cùng nhận dữ liệu từ một kết nối
- Tiết kiệm thời gian mở/đóng kết nối

### 2. Cleanup khi component destroy

Mixin tự động cleanup callbacks khi component bị destroy. Nếu không dùng mixin, nhớ cleanup thủ công:

```javascript
beforeDestroy() {
    if (this.unsubscribe) {
        this.unsubscribe()
    }
}
```

### 3. Web Serial API Browser Support

Web Serial API chỉ được hỗ trợ trên:

- Chrome/Edge 89+
- Opera 76+
- **KHÔNG hỗ trợ**: Firefox, Safari, Mobile browsers

Kiểm tra trước khi sử dụng:

```javascript
if (serialPortService.isSupported()) {
    // Sử dụng Serial Port
} else {
    // Hiển thị thông báo hoặc fallback
}
```

### 4. HTTPS Required

Web Serial API chỉ hoạt động trên:

- `https://` (production)
- `localhost` (development)

### 5. User Gesture Required

Việc mở cổng Serial phải được trigger bởi user action (click button), không thể tự động mở khi page load.

## Troubleshooting

### Lỗi: "Failed to open serial port"

**Nguyên nhân**: Port đã được mở bởi app khác

**Giải pháp**:

1. Đóng các app khác đang sử dụng port (Arduino IDE, PuTTY, etc.)
2. Thử lại

### Lỗi: "DOMException: Failed to execute 'open'"

**Nguyên nhân**: Browser không hỗ trợ Web Serial API

**Giải pháp**: Sử dụng Chrome/Edge phiên bản mới nhất

### Không nhận được dữ liệu

**Kiểm tra**:

1. Baud rate có đúng không?
2. Data format (line ending) có đúng không?
3. Đầu đọc có gửi `\r\n` ở cuối không?

## License

Internal use only - EPS Vuexy Project
