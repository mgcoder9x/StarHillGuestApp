import * as signalR from '@microsoft/signalr'

class SignalrService {
    constructor() {
        this.connection = null
        this.eventHandlers = {}
        this._endpoint = null
    }

    /**
     * Connect to a SignalR hub endpoint.
     * Uses automatic reconnect with exponential back-off intervals.
     * Re-registers all event handlers whenever the connection is re-established.
     */
    async connect(endpoint) {
        this._endpoint = endpoint
        const url = process.env.VUE_APP_BASE_URL + `/${endpoint}`

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build()

        // Re-register all event handlers after automatic reconnect
        this.connection.onreconnected(() => {
            console.log('SignalR reconnected — re-registering event handlers.')
            this._registerAllHandlers()
        })

        this.connection.onclose((err) => {
            if (err) {
                console.warn('SignalR connection closed with error:', err)
            }
        })

        await this._startConnection()
    }

    async disconnect() {
        if (this.connection) {
            await this.connection.stop()
            this.connection = null
        }
    }

    _startConnection = async () => {
        try {
            await this.connection.start()
            console.log('SignalR Connected.')
        } catch (err) {
            console.warn(
                'SignalR initial connection failed, retrying in 10s…',
                err
            )
            setTimeout(() => this._startConnection(), 10000)
        }
    }

    /**
     * Re-register all buffered event handlers on the current connection.
     * Called after automatic reconnect so no events are lost.
     */
    _registerAllHandlers() {
        Object.entries(this.eventHandlers).forEach(([event, callbacks]) => {
            if (callbacks.length === 0) return
            // Remove stale listener and re-attach a fresh one
            this.connection.off(event)
            this.connection.on(event, (data) => {
                this.eventHandlers[event]?.forEach((cb) => cb(data))
            })
        })
    }

    /**
     * Subscribe to a hub event.
     * Safe to call multiple times — only one native SignalR listener is created per event.
     */
    on(event, callback) {
        if (!this.connection) {
            throw new Error('SignalR not connected. Call connect() first.')
        }
        if (!this.eventHandlers[event]) {
            this.eventHandlers[event] = []
            this.connection.on(event, (data) => {
                this.eventHandlers[event]?.forEach((cb) => cb(data))
            })
        }
        if (!this.eventHandlers[event].includes(callback)) {
            this.eventHandlers[event].push(callback)
        }
    }

    off(event, callback) {
        if (!this.eventHandlers[event]) return
        this.eventHandlers[event] = this.eventHandlers[event].filter(
            (cb) => cb !== callback
        )
        if (this.eventHandlers[event].length === 0) {
            delete this.eventHandlers[event]
            if (this.connection) this.connection.off(event)
        }
    }

    sendMessage = async (message) => {
        try {
            await this.connection.invoke('SendNotification', message)
        } catch (err) {
            console.error('SignalR sendMessage error:', err)
        }
    }
}

const signalRService = new SignalrService()
export default signalRService
