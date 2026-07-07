<template>
    <div class="container mt-4">
        <h4>🔌 Serial Console</h4>

        <!-- Chọn cổng COM -->
        <div class="row g-3 mb-3">
            <div class="col-md-6">
                <label class="form-label">Cổng COM</label>
                <select
                    v-model="selectedPort"
                    class="form-select"
                    :disabled="isConnected"
                >
                    <option :value="null" disabled>-- Chọn cổng COM --</option>
                    <option
                        v-for="(portItem, index) in availablePorts"
                        :key="index"
                        :value="portItem"
                    >
                        {{
                            getPortInfo(portItem) || `Cổng Serial ${index + 1}`
                        }}
                    </option>
                </select>
            </div>
            <div class="col-md-3 d-flex align-items-end">
                <button
                    class="btn btn-info w-100"
                    :disabled="isConnected"
                    @click="refreshPorts"
                >
                    🔄 Làm mới
                </button>
            </div>
            <!-- <div class="col-md-3 d-flex align-items-end">
                <button
                    class="btn btn-warning w-100"
                    :disabled="isConnected"
                    @click="addNewPort"
                >
                    ➕ Thêm cổng mới
                </button>
            </div> -->
        </div>

        <!-- Cấu hình Serial -->
        <div class="row g-3">
            <div class="col-md-3">
                <label class="form-label">Baud rate</label>
                <select
                    v-model.number="baudRate"
                    class="form-select"
                    :disabled="isConnected"
                >
                    <option v-for="rate in baudRates" :key="rate" :value="rate">
                        {{ rate }}
                    </option>
                </select>
            </div>

            <div class="col-md-2">
                <label class="form-label">Data bits</label>
                <select
                    v-model.number="dataBits"
                    class="form-select"
                    :disabled="isConnected"
                >
                    <option>7</option>
                    <option>8</option>
                </select>
            </div>

            <div class="col-md-2">
                <label class="form-label">Parity</label>
                <select
                    v-model="parity"
                    class="form-select"
                    :disabled="isConnected"
                >
                    <option>none</option>
                    <option>even</option>
                    <option>odd</option>
                </select>
            </div>

            <div class="col-md-2">
                <label class="form-label">Stop bits</label>
                <select
                    v-model.number="stopBits"
                    class="form-select"
                    :disabled="isConnected"
                >
                    <option>1</option>
                    <option>2</option>
                </select>
            </div>

            <div class="col-md-3 d-flex align-items-end">
                <button
                    class="btn w-100"
                    :class="isConnected ? 'btn-danger' : 'btn-success'"
                    :disabled="!isConnected && !selectedPort"
                    @click="toggleConnection"
                >
                    {{ isConnected ? '🔴 Đóng cổng' : '🟢 Mở cổng' }}
                </button>
            </div>
        </div>

        <!-- Log terminal -->
        <div class="mt-3">
            <label class="form-label fw-bold">Terminal log</label>
            <pre
                class="bg-dark text-success p-3 rounded"
                style="height: 250px; overflow-y: auto; white-space: pre-wrap"
            >
        <span v-for="(line, index) in logs" :key="index">
          {{ line }}
        </span>
      </pre>
        </div>

        <!-- Gửi dữ liệu -->
        <div class="input-group mt-2">
            <input
                v-model="sendText"
                type="text"
                class="form-control"
                placeholder="Nhập dữ liệu cần gửi..."
                @keyup.enter="sendData"
            />
            <button
                class="btn btn-primary"
                :disabled="!isConnected || isSending"
                @click="sendData"
            >
                <span v-if="isSending">
                    <span
                        class="spinner-border spinner-border-sm me-1"
                        role="status"
                        aria-hidden="true"
                    ></span>
                    Đang gửi...
                </span>
                <span v-else>Gửi</span>
            </button>
        </div>

        <!-- Trạng thái kết nối -->
        <div v-if="connectionStatus" class="mt-2">
            <div
                class="alert"
                :class="{
                    'alert-success': connectionStatus.type === 'success',
                    'alert-warning': connectionStatus.type === 'warning',
                    'alert-danger': connectionStatus.type === 'error',
                    'alert-info': connectionStatus.type === 'info',
                }"
            >
                {{ connectionStatus.message }}
            </div>
        </div>

        <div v-if="error" class="alert alert-danger mt-3">{{ error }}</div>
    </div>
</template>

<script>
export default {
    data() {
        return {
            baudRates: [9600, 19200, 38400, 57600, 115200],
            baudRate: 115200,
            dataBits: 8,
            parity: 'none',
            stopBits: 1,
            isConnected: false,
            logs: [],
            sendText: '',
            error: '',
            port: null,
            reader: null,
            writer: null,
            keepReading: true,
            availablePorts: [],
            selectedPort: null,
            isSending: false,
            connectionStatus: null,
            lastActivityTime: null,
            connectionCheckInterval: null,
            readableStreamClosed: null,
            writableStreamClosed: null,
        }
    },
    async mounted() {
        // Tải danh sách cổng có sẵn khi component được mount
        await this.loadAvailablePorts()
    },
    beforeDestroy() {
        // Cleanup khi component bị destroy
        if (this.connectionCheckInterval) {
            clearInterval(this.connectionCheckInterval)
        }
        if (this.isConnected) {
            this.closePort()
        }
    },
    methods: {
        async loadAvailablePorts() {
            try {
                if ('serial' in navigator) {
                    this.availablePorts = await navigator.serial.getPorts()
                    console.log('availables:', this.availablePorts)
                    this.addLog(
                        `Đã tìm thấy ${this.availablePorts.length} cổng có sẵn`
                    )
                } else {
                    this.error =
                        'Trình duyệt không hỗ trợ Web Serial API. Vui lòng sử dụng Chrome, Edge hoặc Opera.'
                    this.addLog(this.error)
                }
            } catch (err) {
                this.error = `Lỗi khi tải danh sách cổng: ${err.message}`
                this.addLog(this.error)
            }
        },

        async refreshPorts() {
            await this.loadAvailablePorts()
        },

        async addNewPort() {
            try {
                this.error = ''
                const newPort = await navigator.serial.requestPort()
                await this.loadAvailablePorts()
                this.selectedPort = newPort
                this.addLog('Đã thêm cổng mới vào danh sách')
            } catch (err) {
                if (err.name !== 'NotFoundError') {
                    this.error = err.message
                    this.addLog(`Lỗi thêm cổng: ${err.message}`)
                }
            }
        },

        getPortInfo(portItem) {
            const info = portItem.getInfo()
            console.log('thông tin port:', info)
            // Tạo tên hiển thị dựa trên thông tin có sẵn
            let displayName = ''

            // Lấy tên từ metadata nếu có
            if (portItem.productName) {
                displayName = portItem.productName
            } else if (portItem.manufacturerName) {
                displayName = portItem.manufacturerName
            } else if (info.usbVendorId && info.usbProductId) {
                // Hiển thị VID/PID dưới dạng ngắn gọn hơn
                const vid = info.usbVendorId
                    .toString(16)
                    .toUpperCase()
                    .padStart(4, '0')
                const pid = info.usbProductId
                    .toString(16)
                    .toUpperCase()
                    .padStart(4, '0')
                displayName = `USB Device (${vid}:${pid})`
            }

            return displayName || null
        },

        addLog(message, type = 'info') {
            const timestamp = new Date().toLocaleTimeString()
            let prefix = 'ℹ️'
            if (type === 'send') {
                prefix = '➡️'
            } else if (type === 'recv') {
                prefix = '⬅️'
            } else if (type === 'success') {
                prefix = '✅'
            } else if (type === 'error') {
                prefix = '❌'
            } else if (type === 'warning') {
                prefix = '⚠️'
            }
            this.logs.push(`[${timestamp}] ${prefix} ${message}`)
            this.$nextTick(() => {
                const pre = this.$el.querySelector('pre')
                if (pre) pre.scrollTop = pre.scrollHeight
            })
        },

        showConnectionStatus(message, type = 'info', duration = 5000) {
            this.connectionStatus = { message, type }
            if (duration > 0) {
                setTimeout(() => {
                    this.connectionStatus = null
                }, duration)
            }
        },

        startConnectionMonitoring() {
            // Kiểm tra kết nối định kỳ
            this.connectionCheckInterval = setInterval(() => {
                if (this.isConnected && this.port) {
                    // Kiểm tra xem port có còn mở không
                    if (!this.port.readable || !this.port.writable) {
                        this.addLog(
                            'Phát hiện mất kết nối với thiết bị',
                            'error'
                        )
                        this.showConnectionStatus(
                            'Kết nối bị mất! Vui lòng kiểm tra thiết bị.',
                            'error',
                            0
                        )
                        this.closePort()
                    }
                }
            }, 2000) // Kiểm tra mỗi 2 giây
        },

        async toggleConnection() {
            if (this.isConnected) {
                await this.closePort()
            } else {
                await this.openPort()
            }
        },

        async openPort() {
            try {
                this.error = ''
                this.connectionStatus = null

                if (!this.selectedPort) {
                    this.error = 'Vui lòng chọn cổng COM trước'
                    this.addLog(this.error)
                    return
                }

                this.showConnectionStatus(
                    'Đang kết nối với cổng Serial...',
                    'info',
                    0
                )

                // Kiểm tra xem port đã mở chưa
                try {
                    await this.selectedPort.open({
                        baudRate: this.baudRate,
                        dataBits: this.dataBits,
                        parity: this.parity,
                        stopBits: this.stopBits,
                        flowControl: 'none',
                    })
                } catch (openErr) {
                    // Nếu port đã mở rồi, chỉ hiển thị cảnh báo và sử dụng port hiện tại
                    if (
                        openErr.message.includes('already open') ||
                        openErr.message.includes('đã mở')
                    ) {
                        this.addLog(
                            'Cổng đã được mở trước đó, đang sử dụng kết nối hiện tại',
                            'warning'
                        )
                        this.showConnectionStatus(
                            'Cổng đã được mở sẵn. Đang kết nối lại...',
                            'warning',
                            3000
                        )
                        // Không throw error, tiếp tục sử dụng port
                    } else {
                        // Lỗi khác thì throw lên
                        throw openErr
                    }
                }

                this.port = this.selectedPort

                // Thiết lập đọc dữ liệu
                const textDecoder = new TextDecoderStream()
                const readableStreamClosed = this.port.readable.pipeTo(
                    textDecoder.writable
                )
                this.reader = textDecoder.readable.getReader()

                // Thiết lập ghi dữ liệu
                const textEncoder = new TextEncoderStream()
                const writableStreamClosed = textEncoder.readable.pipeTo(
                    this.port.writable
                )
                this.writer = textEncoder.writable.getWriter()

                // Lưu lại promise để theo dõi
                this.readableStreamClosed = readableStreamClosed
                this.writableStreamClosed = writableStreamClosed

                this.isConnected = true
                this.lastActivityTime = Date.now()
                this.addLog(
                    `Đã mở cổng Serial (baud ${this.baudRate})`,
                    'success'
                )
                this.showConnectionStatus(
                    'Kết nối thành công! Sẵn sàng gửi/nhận dữ liệu.',
                    'success',
                    3000
                )

                this.keepReading = true
                this.readLoop()
                this.startConnectionMonitoring()
            } catch (err) {
                this.error = err.message
                this.addLog(`Lỗi mở port: ${err.message}`, 'error')
                this.showConnectionStatus(
                    `Không thể mở cổng: ${err.message}`,
                    'error',
                    0
                )
            }
        },

        async readLoop() {
            try {
                // eslint-disable-next-line no-await-in-loop
                while (this.keepReading) {
                    // eslint-disable-next-line no-await-in-loop
                    const { value, done } = await this.reader.read()
                    if (done) {
                        this.addLog('Kết nối đã bị đóng', 'warning')
                        break
                    }
                    if (value) {
                        this.lastActivityTime = Date.now()
                        this.addLog(value.trim(), 'recv')
                    }
                }
            } catch (err) {
                this.addLog(`Lỗi đọc: ${err.message}`, 'error')
                this.showConnectionStatus(
                    `Lỗi đọc dữ liệu: ${err.message}`,
                    'error',
                    0
                )
            }
        },

        async sendData() {
            if (!this.writer || !this.sendText.trim()) return

            this.isSending = true
            const dataToSend = this.sendText.trim()
            const sendTime = Date.now()

            try {
                // Kiểm tra kết nối trước khi gửi
                if (!this.port || !this.port.writable) {
                    throw new Error(
                        'Cổng không sẵn sàng. Thiết bị có thể đã ngắt kết nối.'
                    )
                }

                await this.writer.write(`${dataToSend}\r\n`)

                this.lastActivityTime = Date.now()
                const responseTime = this.lastActivityTime - sendTime

                this.addLog(`${dataToSend} [${responseTime}ms]`, 'send')
                this.showConnectionStatus(
                    `✓ Gửi thành công! (${responseTime}ms)`,
                    'success',
                    2000
                )
                this.sendText = ''

                // Kiểm tra có nhận phản hồi không sau 3 giây
                setTimeout(() => {
                    const timeSinceLastActivity = Date.now() - sendTime
                    if (timeSinceLastActivity > 3000 && this.isConnected) {
                        this.showConnectionStatus(
                            'Chưa nhận được phản hồi từ thiết bị. Kiểm tra kết nối vật lý.',
                            'warning',
                            5000
                        )
                    }
                }, 3000)
            } catch (err) {
                this.addLog(`Lỗi gửi: ${err.message}`, 'error')
                this.showConnectionStatus(
                    `✗ Gửi thất bại: ${err.message}`,
                    'error',
                    0
                )

                // Kiểm tra xem có phải lỗi kết nối không
                if (
                    err.message.includes('writable') ||
                    err.message.includes('closed') ||
                    err.message.includes('disconnect')
                ) {
                    this.addLog(
                        'Phát hiện mất kết nối. Đang đóng cổng...',
                        'warning'
                    )
                    await this.closePort()
                }
            } finally {
                this.isSending = false
            }
        },

        async closePort() {
            try {
                this.keepReading = false

                // Dừng monitoring
                if (this.connectionCheckInterval) {
                    clearInterval(this.connectionCheckInterval)
                    this.connectionCheckInterval = null
                }

                // Giải phóng reader trước
                if (this.reader) {
                    try {
                        await this.reader.cancel()
                        this.reader.releaseLock()
                    } catch (err) {
                        // Bỏ qua lỗi nếu reader đã được giải phóng
                        console.log('Reader already released:', err.message)
                    }
                    this.reader = null
                }

                // Giải phóng writer trước
                if (this.writer) {
                    try {
                        await this.writer.close()
                    } catch (err) {
                        // Bỏ qua lỗi nếu writer đã được đóng
                        console.log('Writer already closed:', err.message)
                    }
                    this.writer = null
                }

                // Đợi một chút để đảm bảo stream được giải phóng
                await new Promise((resolve) => setTimeout(resolve, 100))

                // Cuối cùng mới đóng port
                if (this.port) {
                    try {
                        await this.port.close()
                    } catch (err) {
                        // Bỏ qua lỗi nếu port đã đóng
                        console.log('Port already closed:', err.message)
                    }
                    this.port = null
                }

                this.isConnected = false
                this.connectionStatus = null
                this.addLog('Đã đóng cổng Serial', 'info')
            } catch (err) {
                this.addLog(`Lỗi đóng port: ${err.message}`, 'error')
                // Reset trạng thái ngay cả khi có lỗi
                this.isConnected = false
                this.reader = null
                this.writer = null
                this.port = null
            }
        },
    },
}
</script>

<style scoped>
pre {
    font-family: Consolas, monospace;
    font-size: 0.9rem;
}
</style>
