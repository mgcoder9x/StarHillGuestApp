/**
 * Serial Port Service - Singleton để quản lý kết nối Serial Port
 * Duy trì kết nối ngay cả khi đóng UI component
 */

class SerialPortService {
    constructor() {
        if (SerialPortService.instance) {
            return SerialPortService.instance
        }

        this.port = null
        this.reader = null
        this.writer = null
        this.keepReading = true
        this.isConnected = false
        this.lastActivityTime = null
        this.connectionCheckInterval = null
        this.readableStreamClosed = null
        this.writableStreamClosed = null

        // Event listeners
        this.dataReceivedCallbacks = []
        this.connectionStatusCallbacks = []
        this.logCallbacks = []

        SerialPortService.instance = this
    }

    // Đăng ký callback khi nhận được dữ liệu
    onDataReceived(callback) {
        this.dataReceivedCallbacks.push(callback)
        return () => {
            this.dataReceivedCallbacks = this.dataReceivedCallbacks.filter(
                (cb) => cb !== callback
            )
        }
    }

    // Đăng ký callback khi thay đổi trạng thái kết nối
    onConnectionStatusChange(callback) {
        this.connectionStatusCallbacks.push(callback)
        return () => {
            this.connectionStatusCallbacks =
                this.connectionStatusCallbacks.filter((cb) => cb !== callback)
        }
    }

    // Đăng ký callback để nhận log
    onLog(callback) {
        this.logCallbacks.push(callback)
        return () => {
            this.logCallbacks = this.logCallbacks.filter(
                (cb) => cb !== callback
            )
        }
    }

    // Thông báo dữ liệu nhận được
    notifyDataReceived(data) {
        this.dataReceivedCallbacks.forEach((callback) => {
            try {
                callback(data)
            } catch (err) {
                console.error('Error in data received callback:', err)
            }
        })
    }

    // Thông báo thay đổi trạng thái
    notifyConnectionStatus(message, type = 'info') {
        this.connectionStatusCallbacks.forEach((callback) => {
            try {
                callback({ message, type })
            } catch (err) {
                console.error('Error in connection status callback:', err)
            }
        })
    }

    // Thông báo log
    notifyLog(message, type = 'info') {
        const timestamp = new Date().toLocaleTimeString()
        const log = { timestamp, message, type }
        this.logCallbacks.forEach((callback) => {
            try {
                callback(log)
            } catch (err) {
                console.error('Error in log callback:', err)
            }
        })
    }

    // Kiểm tra Web Serial API có được hỗ trợ không
    isSupported() {
        return 'serial' in navigator
    }

    // Lấy trạng thái kết nối
    getConnectionStatus() {
        return {
            isConnected: this.isConnected,
            lastActivityTime: this.lastActivityTime,
            port: this.port,
        }
    }

    // Yêu cầu người dùng chọn cổng Serial
    async requestPort() {
        try {
            const port = await navigator.serial.requestPort()
            this.notifyLog('Đã chọn cổng thành công', 'success')
            return port
        } catch (err) {
            this.notifyLog(`Lỗi chọn cổng: ${err.message}`, 'error')
            throw err
        }
    }

    // Mở kết nối Serial Port
    async openPort(port, config = {}) {
        try {
            const {
                baudRate = 115200,
                dataBits = 8,
                parity = 'none',
                stopBits = 1,
            } = config

            if (this.isConnected) {
                this.notifyLog('Cổng đã được mở rồi', 'warning')
                return
            }

            this.notifyConnectionStatus(
                'Đang kết nối với cổng Serial...',
                'info'
            )

            // Kiểm tra xem port đã mở chưa
            try {
                await port.open({
                    baudRate,
                    dataBits,
                    parity,
                    stopBits,
                    flowControl: 'none',
                })
            } catch (openErr) {
                if (
                    openErr.message.includes('already open') ||
                    openErr.message.includes('đã mở')
                ) {
                    this.notifyLog(
                        'Cổng đã được mở trước đó, đang sử dụng kết nối hiện tại',
                        'warning'
                    )
                } else {
                    throw openErr
                }
            }

            this.port = port

            // Thiết lập đọc dữ liệu
            const textDecoder = new TextDecoderStream()
            this.readableStreamClosed = this.port.readable.pipeTo(
                textDecoder.writable
            )
            this.reader = textDecoder.readable.getReader()

            // Thiết lập ghi dữ liệu
            const textEncoder = new TextEncoderStream()
            this.writableStreamClosed = textEncoder.readable.pipeTo(
                this.port.writable
            )
            this.writer = textEncoder.writable.getWriter()

            this.isConnected = true
            this.lastActivityTime = Date.now()
            this.notifyLog(`Đã mở cổng Serial (baud ${baudRate})`, 'success')
            this.notifyConnectionStatus(
                'Kết nối thành công! Sẵn sàng gửi/nhận dữ liệu.',
                'success'
            )

            this.keepReading = true
            this.startReadLoop()
            this.startConnectionMonitoring()
        } catch (err) {
            this.notifyLog(`Lỗi mở port: ${err.message}`, 'error')
            this.notifyConnectionStatus(
                `Không thể mở cổng: ${err.message}`,
                'error'
            )
            throw err
        }
    }

    // Vòng lặp đọc dữ liệu
    async startReadLoop() {
        try {
            while (this.keepReading) {
                // eslint-disable-next-line no-await-in-loop
                const { value, done } = await this.reader.read()
                if (done) {
                    this.notifyLog('Kết nối đã bị đóng', 'warning')
                    break
                }
                if (value) {
                    this.lastActivityTime = Date.now()
                    const trimmedValue = value.trim()
                    this.notifyLog(trimmedValue, 'recv')
                    // Thông báo dữ liệu nhận được cho các component lắng nghe
                    this.notifyDataReceived(trimmedValue)
                }
            }
        } catch (err) {
            this.notifyLog(`Lỗi đọc: ${err.message}`, 'error')
            this.notifyConnectionStatus(
                `Lỗi đọc dữ liệu: ${err.message}`,
                'error'
            )
        }
    }

    // Giám sát kết nối
    startConnectionMonitoring() {
        this.connectionCheckInterval = setInterval(() => {
            if (this.isConnected && this.port) {
                if (!this.port.readable || !this.port.writable) {
                    this.notifyLog(
                        'Phát hiện mất kết nối với thiết bị',
                        'error'
                    )
                    this.notifyConnectionStatus(
                        'Kết nối bị mất! Vui lòng kiểm tra thiết bị.',
                        'error'
                    )
                    this.closePort()
                }
            }
        }, 2000)
    }

    // Gửi dữ liệu
    async sendData(data) {
        if (!this.writer || !data.trim()) {
            throw new Error('Writer không sẵn sàng hoặc dữ liệu rỗng')
        }

        const dataToSend = data.trim()
        const sendTime = Date.now()

        try {
            if (!this.port || !this.port.writable) {
                throw new Error(
                    'Cổng không sẵn sàng. Thiết bị có thể đã ngắt kết nối.'
                )
            }

            await this.writer.write(`${dataToSend}\r\n`)
            this.lastActivityTime = Date.now()
            const responseTime = this.lastActivityTime - sendTime

            this.notifyLog(`${dataToSend} [${responseTime}ms]`, 'send')
            this.notifyConnectionStatus(
                `✓ Gửi thành công! (${responseTime}ms)`,
                'success'
            )

            return { success: true, responseTime }
        } catch (err) {
            this.notifyLog(`Lỗi gửi: ${err.message}`, 'error')
            this.notifyConnectionStatus(
                `✗ Gửi thất bại: ${err.message}`,
                'error'
            )

            if (
                err.message.includes('writable') ||
                err.message.includes('closed') ||
                err.message.includes('disconnect')
            ) {
                this.notifyLog(
                    'Phát hiện mất kết nối. Đang đóng cổng...',
                    'warning'
                )
                await this.closePort()
            }

            throw err
        }
    }

    // Đóng cổng Serial
    async closePort() {
        try {
            this.keepReading = false

            if (this.connectionCheckInterval) {
                clearInterval(this.connectionCheckInterval)
                this.connectionCheckInterval = null
            }

            if (this.reader) {
                try {
                    await this.reader.cancel()
                    this.reader.releaseLock()
                } catch (err) {
                    console.log('Reader already released:', err.message)
                }
                this.reader = null
            }

            if (this.writer) {
                try {
                    await this.writer.close()
                } catch (err) {
                    console.log('Writer already closed:', err.message)
                }
                this.writer = null
            }

            await new Promise((resolve) => setTimeout(resolve, 100))

            if (this.port) {
                try {
                    await this.port.close()
                } catch (err) {
                    console.log('Port already closed:', err.message)
                }
                this.port = null
            }

            this.isConnected = false
            this.notifyLog('Đã đóng cổng Serial', 'info')
            this.notifyConnectionStatus('Đã ngắt kết nối', 'info')
        } catch (err) {
            this.notifyLog(`Lỗi đóng port: ${err.message}`, 'error')
            this.isConnected = false
            this.reader = null
            this.writer = null
            this.port = null
        }
    }
}

// Export singleton instance
export default new SerialPortService()
