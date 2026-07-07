<template>
    <div class="container mt-1">
        <!-- <h4>🔌 Serial Console</h4> -->

        <!-- Chọn cổng COM đã được chuyển sang khi bấm mở cổng -->

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
                    @click="toggleConnection"
                >
                    {{
                        isConnected
                            ? `🔴 ${$t('serialPort.disconnect')}`
                            : `🟢 ${$t('serialPort.connect')}`
                    }}
                </button>
            </div>
        </div>

        <!-- Log terminal -->
        <!-- <div class="mt-3">
            <label class="form-label fw-bold">Terminal log</label>
            <pre
                class="bg-dark text-success p-3 rounded"
                style="height: 250px; overflow-y: auto; white-space: pre-wrap"
            >
        <span v-for="(line, index) in logs" :key="index">
          {{ line }}
        </span>
      </pre>
        </div> -->

        <!-- Gửi dữ liệu -->
        <!-- <div class="input-group mt-2">
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
        </div> -->

        <!-- Trạng thái kết nối -->
        <!-- <div v-if="connectionStatus" class="mt-2">
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
        </div> -->
        <!-- 
        <div v-if="error" class="alert alert-danger mt-3">{{ error }}</div> -->
    </div>
</template>

<script>
import serialPortService from '@/services/serialPortService'

export default {
    data() {
        return {
            baudRates: [9600, 19200, 38400, 57600, 115200],
            baudRate: 9600,
            dataBits: 8,
            parity: 'none',
            stopBits: 1,
            isConnected: false,
            logs: [],
            sendText: '',
            error: '',
            selectedPort: null,
            isSending: false,
            connectionStatus: null,
            // Unsubscribe functions
            unsubscribeLog: null,
            unsubscribeData: null,
            unsubscribeStatus: null,
        }
    },
    async mounted() {
        // Đồng bộ trạng thái từ service
        const status = serialPortService.getConnectionStatus()
        this.isConnected = status.isConnected

        // Đăng ký các callback
        this.unsubscribeLog = serialPortService.onLog((log) => {
            this.addLog(log.message, log.type)
        })

        this.unsubscribeStatus = serialPortService.onConnectionStatusChange(
            (statusItem) => {
                this.showConnectionStatus(statusItem.message, statusItem.type)
            }
        )
    },
    beforeDestroy() {
        // Cleanup callbacks khi component bị destroy
        // KHÔNG đóng port để duy trì kết nối
        if (this.unsubscribeLog) this.unsubscribeLog()
        if (this.unsubscribeData) this.unsubscribeData()
        if (this.unsubscribeStatus) this.unsubscribeStatus()
    },
    methods: {
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

        async toggleConnection() {
            if (this.isConnected) {
                await this.closePort()
            } else {
                // Yêu cầu chọn cổng khi mở mới
                try {
                    this.error = ''
                    const port = await serialPortService.requestPort()
                    this.selectedPort = port
                    await this.openPort()
                } catch (err) {
                    this.error = err.message
                }
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

                await serialPortService.openPort(this.selectedPort, {
                    baudRate: this.baudRate,
                    dataBits: this.dataBits,
                    parity: this.parity,
                    stopBits: this.stopBits,
                })

                this.isConnected = true
            } catch (err) {
                this.error = err.message
            }
        },

        async sendData() {
            if (!this.sendText.trim()) return

            this.isSending = true
            const dataToSend = this.sendText.trim()

            try {
                await serialPortService.sendData(dataToSend)
                this.sendText = ''
            } catch (err) {
                this.error = err.message
            } finally {
                this.isSending = false
            }
        },

        async closePort() {
            try {
                await serialPortService.closePort()
                this.isConnected = false
                this.connectionStatus = null
            } catch (err) {
                this.error = err.message
                this.isConnected = false
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
