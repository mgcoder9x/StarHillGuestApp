/**
 * Hướng dẫn sử dụng CardReaderService:
 *
 * CardReaderService là một lớp cung cấp dịch vụ kết nối tới WebSocket để đọc dữ liệu từ đầu đọc thẻ.
 * 
 * Phương thức chính:
 * 
 * - constructor(): Khởi tạo các biến cần thiết cho kết nối WebSocket.
 *   - webSocket: Đối tượng WebSocket để kết nối tới server.
 *   - connectedUrl: URL kết nối WebSocket để nhận dữ liệu.
 *   - submitUrl: URL WebSocket dùng để gửi dữ liệu.
 *   - deviceName: Tên của thiết bị sau khi kết nối thành công.
 *   - isConnected: Trạng thái kết nối hiện tại.
 * 
 * - connectSocket(callback): Kết nối tới WebSocket và lắng nghe dữ liệu từ server. Khi có sự kiện kết nối thành công, sẽ gọi callback với dữ liệu phù hợp.
 * 
 * - handleDisconnection(callback): Xử lý sự kiện mất kết nối. Tự động thử kết nối lại và gọi callback để thông báo cho người dùng.
 * 
 * Cách sử dụng:
 * 
 * 1. Import và khởi tạo service:
 *    ```javascript
 *    import cardReaderService from './path/to/cardReaderSocket.js';
 *    ```
 * 
 * 2. Kết nối tới WebSocket:
 *    ```javascript
 *    cardReaderService.connectSocket((data) => {
 *      // Xử lý dữ liệu từ server
 *      console.log(data);
 *    });
 *    ```
 * 
 * 3. Xử lý khi mất kết nối:
 *    ```javascript
 *    cardReaderService.handleDisconnection((status) => {
 *      // Thông báo hoặc xử lý khi mất kết nối
 *      console.warn(status.message);
 *    });
 *    ```
 */
class CardReaderService {
    constructor() {
        this.webSocket = null
        this.connectedUrl = 'wss://127.0.0.1:2003/broadcast'
        this.submitUrl = 'wss://127.0.0.1:2003/ingest'
        this.deviceName = null;
        this.isConnected = false;
    }
    // Khởi động kết nối socket hoặc WebSocket
    async connectSocket(callback) {
      // Kết nối WebSocket thuần
         try {
           this.webSocket = await this.attemptWebSocketConnection(this.connectedUrl);
           console.log(`Connected to WebSocket at ${this.connectedUrl}`);
        
           // Lắng nghe sự kiện từ WebSocket và xử lý dữ liệu
           this.webSocket.onmessage = (event) => {
                const data = JSON.parse(event.data)
             switch (data.type) {
               case "connected":
                 this.isConnected = true;
                 this.deviceName = data.name;
                 break;
               case "disconnect":
                 this.isConnected = false;
                 break;
               case "getDevice":
                 this.deviceName = data.name;
                 break;
               default:
                 break;
             }
             this.handleEvent(data, callback);
           };
           this.ingestEvent("get_device");
           // Đợi đến khi có tên thiết bị
           await new Promise((resolve) => {
             const interval = setInterval(() => {
               if (this.deviceName) {
                 this.isConnected = true;
                 clearInterval(interval);
                 resolve();
               } else {
                 console.log("Waiting for connect cardreader...");
               }
             }, 1000);
           });
        
           // Lắng nghe sự kiện mất kết nối từ WebSocket
           this.webSocket.onclose = () => {
             console.log("WebSocket connection lost.");
             this.isConnected = false;
             this.handleDisconnection(callback);
           };
        
           return;
         } catch (error) {
            console.error(
                `Failed to connect to WebSocket at ${this.connectedUrl}:`,
                error
            )
        }
    }

    // Hàm thử kết nối WebSocket thuần
    attemptWebSocketConnection(url) {
      return new Promise((resolve, reject) => {
        const webSocket = new WebSocket(url, null, {
          rejectUnauthorized: false,
        });
  
        webSocket.onopen = () => {
          resolve(webSocket);
        };
  
        webSocket.onerror = (error) => {
          webSocket.close();
          reject(error);
        };
      });
    }
  
    // Xử lý sự kiện nhận được từ socket hoặc WebSocket
    handleEvent(data, callback) {
      callback(data);
    }
  
    // Xử lý khi mất kết nối
    handleDisconnection(callback) {
      this.connectedUrl = null;
      this.deviceName = null;
      this.isConnected = false;
  
      // Thông báo cho người dùng rằng kết nối đã mất
      console.warn("Connection lost. Trying to reconnect...");
      callback({
        message: "Mất kết nối, đang thử kết nối lại...",
        status: "warning",
      });
  
      // Đặt interval để thử kết nối lại mỗi giây
      this.reconnectInterval = setInterval(async () => {
        try {
          await this.connectSocket(callback);
  
          // Nếu kết nối thành công, dừng thử lại
          clearInterval(this.reconnectInterval);
          this.reconnectInterval = null;
          console.log("Reconnected successfully.");
        } catch (error) {
          console.error("Reconnection attempt failed.");
        }
      }, 1000);
    }
  
    // Ngắt kết nối socket hoặc WebSocket
    disconnectSocket() {
      if (this.webSocket) {
        this.webSocket.close();
        this.webSocket = null;
        console.log("Disconnected from WebSocket.");
      }
      this.isConnected = false;
      this.connectedUrl = null;
      this.deviceName = null;
    }
  
    // Lấy dữ liệu thiết bị
    ingestEvent(data) {
      // Tạo kết nối WebSocket tới server
      const ws = new WebSocket(this.submitUrl);
  
      // Lắng nghe sự kiện 'open' để kiểm tra kết nối
      ws.onopen = () => {
        ws.send(data);
        ws.close();
      };
  
      ws.onerror = (error) => {
        this.isConnected = false;
        console.error("WebSocket error:", error);
      };
    }
  }
  
  // Khởi tạo một instance của service này để dùng trong toàn bộ ứng dụng
  const cardReaderService = new CardReaderService();
  export default cardReaderService;
  