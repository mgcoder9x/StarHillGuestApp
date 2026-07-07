# Requirements Document

## Introduction

Hệ thống "Resort QR Portal" (Star Hill Guest App) là một nền tảng web cho phép khách của resort quét mã QR (dán tại phòng) bằng camera điện thoại để mở một trang web. Trang web hiển thị nội quy resort (bắt buộc đọc hết theo flow), câu hỏi thường gặp (FAQ) do lễ tân tự soạn, cho phép khách gửi tin nhắn/phản hồi tới lễ tân, và tạo yêu cầu dọn phòng. Mỗi QR gắn với một phòng cụ thể nên hệ thống tự nhận biết số phòng. Giao diện hỗ trợ nhiều ngôn ngữ và tự động nhận diện ngôn ngữ của điện thoại.

Phía quản trị có một dashboard cho lễ tân/admin: tạo phòng, sinh mã QR, quản lý nội quy (draft → publish), tạo/tổ chức FAQ theo flow, xem và trả lời tin nhắn của khách gom theo phòng, ghi chú nội bộ, và xử lý yêu cầu dọn phòng.

**Giả định triển khai quan trọng:** hệ thống chạy trong **mạng WiFi nội bộ của resort, KHÔNG publish ra internet công cộng**. Khách phải kết nối WiFi resort mới truy cập được portal. Điều này giúp giảm mạnh bề mặt tấn công và giảm rủi ro "khách đã rời đi vẫn dùng link".

Lưu ý: việc "chỉ truy cập trong nội bộ" là **yêu cầu triển khai mạng (network isolation)** do hạ tầng resort đảm bảo (VLAN/tường lửa/không NAT ra ngoài), **bản thân ứng dụng không tự enforce** điều này. Rủi ro còn lại (ví dụ thiết bị khách cũ vẫn vào lại WiFi nếu mật khẩu không đổi) được kiểm soát thêm bằng cơ chế lượt lưu trú (GuestVisit) + lễ tân đóng phiên khi checkout, chứ hệ thống không tuyên bố "tuyệt đối an toàn".

Công nghệ: Backend ASP.NET Core + EF Core; Frontend Vue 3 (2 SPA riêng: Guest Web và Admin Dashboard); realtime bằng SignalR; sinh QR bằng QRCoder.

## Glossary

- **Guest (Khách)**: người dùng cuối, không đăng nhập, truy cập qua QR khi đang ở trong WiFi resort.
- **Staff (Lễ tân)**: người dùng nội bộ đã đăng nhập; xử lý tin nhắn, ghi chú, nội quy, FAQ, và ticket dọn phòng.
- **Admin**: người dùng nội bộ có toàn quyền cấu hình (phòng, QR, người dùng, ngôn ngữ, settings) ngoài các quyền của Staff.
- **PublicToken**: chuỗi ngẫu nhiên, duy nhất, không đoán được, gắn với một phòng và nhúng vào URL trong QR.
- **GuestSession**: định danh **thiết bị** của khách (cookie dài hạn ~30–90 ngày); không phải "phiên 30 phút".
- **GuestVisit**: *lượt lưu trú của một phòng*. Active từ lần quét đầu; quét lại cùng thiết bị + cùng phòng sẽ **nối lại đúng visit đang Active**. Kết thúc khi lễ tân đóng (checkout) hoặc quá hạn nhàn rỗi dài (mặc định 24h). Chưa map PMS/booking ở giai đoạn này. Hội thoại và acknowledgement gắn theo GuestVisit.
- **Cửa sổ thao tác (Portal window)**: khoảng thời gian ngắn (mặc định 30') không thao tác thì khách phải **quét QR lại để tiếp tục**; việc quét lại chỉ mở khoá và nối lại visit, không tạo visit mới.
- **RuleSet / RulePublication**: `RuleSet` là bản **Draft** đang soạn (nội bộ). Mỗi lần **Publish** tạo một `RulePublication` — **snapshot bất biến** của nội quy (kèm `RulePublicationSection`+bản dịch); khách chỉ đọc publication có `IsCurrent=true`.
- **RuleVersion**: số phiên bản nội quy đang published; dùng để xác định khách đã xác nhận phiên bản nào.
- **Acknowledgement**: bản ghi khách đã bấm xác nhận đã đọc nội quy (bản ghi vận hành, không phải chứng cứ pháp lý).
- **Force-read flow**: luồng khuyến khích khách đọc hết nội quy trước khi dùng các tính năng khác.
- **FAQ flow**: câu hỏi thường gặp dạng cha-con để dẫn dắt khách theo luồng, do lễ tân tự soạn.
- **HousekeepingTicket**: yêu cầu dọn phòng do khách (hoặc nhân viên) tạo; nhân viên đánh dấu hoàn tất trong app hoặc bằng cách quét QR phòng.
- **ResortSettings**: bộ cấu hình vận hành của resort (bật/tắt ack, thời hạn phiên, base URL cho QR, giới hạn tin nhắn, rate limit...).

---

## Requirements

### Requirement 1: Truy cập qua QR & nhận diện phòng

**User Story:** Là một khách, tôi muốn quét QR trong phòng bằng camera điện thoại và được đưa tới trang web của đúng phòng mình, để không phải nhập thủ công số phòng.

#### Acceptance Criteria
1. WHEN admin/lễ tân tạo một phòng THEN hệ thống SHALL sinh một `PublicToken` ngẫu nhiên, duy nhất, không đoán được.
2. WHEN khách quét QR (đang ở trong WiFi resort) THEN camera điện thoại SHALL mở URL dạng `https://<host>/r/{token}` trong trình duyệt (không yêu cầu cài app).
3. WHEN backend nhận `token` hợp lệ THEN hệ thống SHALL phân giải ra phòng, số phòng và resort tương ứng.
4. IF `token` không tồn tại HOẶC đã bị thu hồi HOẶC phòng bị vô hiệu THEN hệ thống SHALL hiển thị trang lỗi thân thiện và KHÔNG tiết lộ thông tin phòng khác.
5. WHERE cần thay QR THE hệ thống SHALL cho phép admin/lễ tân thu hồi và cấp lại token mới, làm QR cũ vô hiệu.
6. THE hệ thống SHALL KHÔNG nhúng số phòng dạng trần (ví dụ `?room=101`) vào URL QR.

### Requirement 2: Đa ngôn ngữ & tự động nhận diện ngôn ngữ

**User Story:** Là một khách quốc tế, tôi muốn trang web tự hiển thị bằng ngôn ngữ điện thoại của tôi và có thể đổi ngôn ngữ, để đọc hiểu dễ dàng.

#### Acceptance Criteria
1. WHEN khách mở trang lần đầu THEN hệ thống SHALL chọn ngôn ngữ theo thứ tự ưu tiên: (1) `?lang=`, (2) localStorage, (3) `navigator.language`, (4) ngôn ngữ mặc định của resort (**tiếng Anh `en`**).
2. IF ngôn ngữ điện thoại không nằm trong danh sách ngôn ngữ được bật THEN hệ thống SHALL fallback về ngôn ngữ mặc định của resort (`en`).
3. WHEN khách đổi ngôn ngữ thủ công THEN hệ thống SHALL cập nhật text giao diện và nội dung (nội quy, FAQ) sang ngôn ngữ đó và lưu lựa chọn vào localStorage.
4. THE hệ thống SHALL phân biệt hai loại text: text giao diện (i18n JSON ở frontend) và nội dung do lễ tân/admin nhập (lưu theo ngôn ngữ trong database).
5. IF một nội dung chưa có bản dịch cho ngôn ngữ đang chọn THEN hệ thống SHALL hiển thị bản dịch ngôn ngữ mặc định và đánh dấu `isFallback` để không để trống.
6. THE **guest web** SHALL mặc định tiếng Anh; THE **admin dashboard** SHALL dùng giao diện tiếng Việt (`vi`).

### Requirement 3: Hiển thị & ép đọc hết nội quy (force-read flow)

**User Story:** Là quản lý resort, tôi muốn khách đọc hết nội quy và xác nhận trước khi dùng các tính năng khác, để khách nắm được quy định.

#### Acceptance Criteria
1. THE hệ thống SHALL cho phép chia nội quy thành nhiều section có thứ tự, mỗi section hỗ trợ đa ngôn ngữ.
2. WHEN khách vào trang phòng mà chưa xác nhận nội quy phiên bản published hiện tại THEN hệ thống SHALL bắt buộc đi qua flow đọc nội quy trước khi truy cập FAQ, nhắn tin và tạo ticket — **mặc định bật, có thể tắt từng phần qua ResortSettings** (ruleAckRequiredForFaq/Chat/Housekeeping).
3. WHERE một section bật `RequireScrollEnd` THE hệ thống SHALL chỉ đánh dấu section là "đã xem" khi khách cuộn tới cuối nội dung (IntersectionObserver).
4. WHERE một section có `MinReadSeconds > 0` THE hệ thống SHALL vô hiệu hoá nút "Tiếp tục" cho tới khi hết đếm ngược.
5. THE hệ thống SHALL hiển thị thanh tiến độ (ví dụ 2/6).
6. WHEN khách hoàn tất các section bắt buộc THEN hệ thống SHALL yêu cầu tick checkbox xác nhận trước khi tiếp tục.
7. WHEN khách xác nhận THEN hệ thống SHALL lưu bản ghi acknowledgement gắn với **GuestVisit** gồm: phòng, guest session/visit, publication + version nội quy, ngôn ngữ, thời điểm. Đây là **bản ghi vận hành để biết khách đã bấm đồng ý**, không nhằm mục đích làm chứng cứ pháp lý.
8. WHEN khách đã xác nhận version published hiện tại trong **lượt lưu trú (GuestVisit) hiện tại** THEN hệ thống SHALL KHÔNG bắt đọc lại — kể cả khi quét QR lại để nối lại phiên — TRỪ KHI nội quy được publish phiên bản mới HOẶC bắt đầu một lượt lưu trú mới (visit mới sau khi lễ tân đóng phiên/hết hạn).
9. WHEN admin/lễ tân **publish** nội quy THEN hệ thống SHALL tăng version; việc lưu **draft** KHÔNG ảnh hưởng tới khách.
10. THE hệ thống SHALL cho phép khách xem lại nội quy bất cứ lúc nào sau khi đã xác nhận.
11. THE backend SHALL enforce rule gate (không chỉ chặn ở frontend): các endpoint FAQ/nhắn tin/tạo ticket SHALL trả `403 rule_ack_required` nếu chưa xác nhận (khi cấu hình yêu cầu).

### Requirement 4: Câu hỏi thường gặp (FAQ) do lễ tân tự soạn

**User Story:** Là lễ tân, tôi muốn tự tạo FAQ vì tôi biết khách hay hỏi gì, để khách tự tìm câu trả lời mà không cần liên hệ.

#### Acceptance Criteria
1. THE hệ thống SHALL cho phép lễ tân/admin tạo FAQ qua giao diện, tổ chức theo danh mục (category) và cấu trúc cha-con (câu hỏi dẫn tới câu hỏi con) để tạo flow.
2. THE hệ thống SHALL hiển thị FAQ theo ngôn ngữ đang chọn của khách (fallback ngôn ngữ mặc định nếu thiếu).
3. WHEN khách chọn một câu hỏi THEN hệ thống SHALL hiển thị câu trả lời và các câu hỏi con liên quan (nếu có).
4. THE hệ thống SHALL chỉ hiển thị các FAQ đang active cho khách.
5. THE hệ thống SHALL cho phép sắp xếp thứ tự (drag-drop) của category và từng câu hỏi.
6. THE hệ thống SHALL cho phép một FAQ item có CTA "Gửi tin nhắn về vấn đề này"; khi khách bấm, chat được prefill ngữ cảnh câu hỏi.

### Requirement 5: Nhắn tin giữa khách và lễ tân, gom theo phòng

**User Story:** Là lễ tân, tôi muốn xem tin nhắn của khách gom theo phòng với dấu hiệu chưa đọc, và bấm vào để đọc/trả lời, để xử lý nhanh.

#### Acceptance Criteria
1. WHEN khách đã xác nhận nội quy (hoặc khi ResortSettings tắt yêu cầu ack cho chat) THEN hệ thống SHALL cho phép khách mở hội thoại và gửi tin nhắn văn bản. (Đính kèm ảnh để giai đoạn sau.)
2. WHEN khách gửi tin nhắn THEN hệ thống SHALL gắn tin nhắn với phòng và **lượt lưu trú (GuestVisit) hiện tại**, tạo hội thoại cho lượt lưu trú đó nếu chưa có (mỗi GuestVisit tối đa một hội thoại đang mở).
3. THE dashboard SHALL **gom hội thoại theo phòng**, hiển thị **badge số tin chưa đọc**, sắp xếp theo tin mới nhất; một phòng có thể có nhiều hội thoại theo các lượt lưu trú khác nhau (hiện tại + lịch sử).
4. WHEN lễ tân mở một hội thoại THEN hệ thống SHALL hiển thị lịch sử tin nhắn, đánh dấu đã đọc, và cho phép trả lời.
5. WHEN có tin nhắn mới từ khách THEN hệ thống SHALL cập nhật realtime tới lễ tân đang online (SignalR); IF realtime không khả dụng THEN fallback polling.
6. WHEN lễ tân trả lời THEN hệ thống SHALL hiển thị realtime tới khách nếu đang mở trang, ngược lại hiển thị khi khách quay lại (trong cùng lượt lưu trú).
7. THE hệ thống SHALL áp dụng rate limiting nhẹ và giới hạn độ dài tin nhắn để tránh nghịch/spam.
8. THE hệ thống SHALL cho phép lễ tân đóng hội thoại; giữ đơn giản ở MVP (không bắt buộc assignment/priority/SLA).
9. THE khách SHALL chỉ thấy hội thoại của **chính lượt lưu trú của mình**; khách mới của cùng phòng (lượt lưu trú mới) SHALL KHÔNG thấy tin nhắn của lượt trước.
10. IF hội thoại đã bị lễ tân đóng nhưng khách **trong cùng lượt lưu trú còn Active** gửi tin tiếp THEN hệ thống SHALL **mở lại chính hội thoại đó** (reopen) và append, KHÔNG tạo hội thoại mới (mỗi GuestVisit đúng một hội thoại).

### Requirement 6: Yêu cầu dọn phòng (Housekeeping ticket)

**User Story:** Là một khách, tôi muốn yêu cầu dọn phòng ngay trên portal; và là nhân viên, tôi muốn đánh dấu đã dọn xong một cách tiện lợi, có lưu vết ai làm.

#### Acceptance Criteria
1. WHEN khách chọn "Yêu cầu dọn phòng" THEN hệ thống SHALL tạo một `HousekeepingTicket` gắn với phòng và guest session/visit, trạng thái ban đầu `Requested`.
2. IF phòng đã có ticket đang mở (`Requested`/`InProgress`) THEN hệ thống SHALL KHÔNG tạo trùng mà cập nhật/nhắc ticket hiện có.
3. THE dashboard SHALL hiển thị danh sách ticket dọn phòng theo phòng và trạng thái để nhân viên xử lý.
4. THE cách hoàn tất **chính** SHALL là: nhân viên **chọn phòng trong app → xác nhận đã dọn** (chuyển `InProgress`/`Done`), ghi người thực hiện và thời điểm.
5. THE hệ thống SHALL cung cấp **lối tắt cho tiện**: nhân viên (đã đăng nhập) **quét QR phòng** để hoàn tất ticket mở của đúng phòng đó — QR chỉ dùng để xác định phòng, không thay thế việc đăng nhập.
6. THE hệ thống SHALL phân biệt ngữ cảnh bằng app đang dùng: khách dùng guest-web (không đăng nhập) → mở portal; nhân viên dùng admin-web (đã đăng nhập) → hành động vận hành.
7. THE hệ thống SHALL cho phép khách thấy trạng thái yêu cầu dọn phòng của mình (đã tiếp nhận / đang làm / đã xong).
8. THE hệ thống SHALL **ghi nhật ký** mỗi lần chuyển trạng thái ticket (`HousekeepingEvent`: ai, lúc nào, phương thức App/StaffScan) để đối soát.
9. THE nhân viên (đã đăng nhập) SHALL cũng có thể chủ động tạo ticket dọn phòng cho một phòng.

### Requirement 7: Quản lý phòng & sinh QR (Admin/Staff)

**User Story:** Là admin, tôi muốn tạo phòng và sinh mã QR để in dán tại phòng, kể cả in hàng loạt.

#### Acceptance Criteria
1. THE hệ thống SHALL cho phép tạo/sửa/vô hiệu/soft-delete phòng với thuộc tính: số phòng, toà nhà, tầng, trạng thái (`Active`/`Inactive`/`Maintenance`).
2. WHEN yêu cầu sinh QR cho một phòng THEN hệ thống SHALL tạo ảnh QR chứa URL `https://<host>/r/{token}` và cho tải PNG.
3. THE hệ thống SHALL hỗ trợ xuất PDF chứa QR của nhiều phòng (kèm số phòng + logo) để in hàng loạt.
4. WHEN thu hồi QR của một phòng THEN hệ thống SHALL cấp token mới, làm token cũ vô hiệu, và giữ lịch sử token (không xoá cứng).
5. THE hệ thống SHALL đảm bảo mỗi phòng chỉ có tối đa một token đang `Active`.
6. THE việc **tạo/sửa/xoá phòng, sinh/thu hồi QR** SHALL chỉ dành cho **Admin**; Staff chỉ được **xem** danh sách/thông tin phòng phục vụ vận hành.

### Requirement 8: Quản lý nội dung nội quy & FAQ (Draft → Publish)

**User Story:** Là lễ tân/admin, tôi muốn soạn nội quy và FAQ theo nhiều ngôn ngữ trên giao diện, và chỉ khi publish mới ảnh hưởng tới khách.

#### Acceptance Criteria
1. THE hệ thống SHALL cho phép lễ tân/admin tạo/sửa/xoá section nội quy với nội dung rich text cho từng ngôn ngữ được bật, ở trạng thái **Draft**.
2. THE hệ thống SHALL cho phép cấu hình từng section: bắt buộc, `RequireScrollEnd`, `MinReadSeconds`, thứ tự.
3. WHEN người dùng **Publish** nội quy THEN hệ thống SHALL tạo một publication mới (tăng version) và bắt khách xác nhận lại; lưu Draft KHÔNG ảnh hưởng khách.
4. THE hệ thống SHALL cho phép **xem trước (preview) như khách** trước khi publish, và xem lịch sử publication.
5. THE hệ thống SHALL cho phép lễ tân/admin tạo/sửa/xoá category & item FAQ, quan hệ cha-con, thứ tự, active/inactive, nội dung theo từng ngôn ngữ.
6. THE hệ thống SHALL sanitize nội dung HTML do lễ tân/admin nhập (nội quy/FAQ) trước khi lưu và trước khi trả về, để chống XSS lẫn nhau.
7. THE hệ thống SHALL chỉ báo ngôn ngữ nào còn thiếu bản dịch cho một mục nội dung.

### Requirement 9: Dashboard vận hành (Staff/Admin)

**User Story:** Là lễ tân, tôi muốn một dashboard theo phòng để xử lý tin nhắn, ghi chú, và ticket dọn phòng hiệu quả.

#### Acceptance Criteria
1. THE hệ thống SHALL hiển thị tổng quan vận hành: số hội thoại chưa đọc, số hội thoại mở, số ticket dọn phòng mở, số phòng active, số lượt xác nhận nội quy trong ngày.
2. THE hệ thống SHALL hiển thị hội thoại gom theo phòng với badge chưa đọc và cho phép lọc theo phòng/trạng thái.
3. WHEN lễ tân mở hội thoại THEN hệ thống SHALL hiển thị lịch sử + ngữ cảnh phòng (thông tin phòng, trạng thái phiên khách, ghi chú nội bộ, ticket liên quan).
4. THE hệ thống SHALL cho phép thêm ghi chú nội bộ gắn với phòng hoặc hội thoại; ghi chú này KHÔNG bao giờ hiển thị cho khách.
5. THE hệ thống SHALL cho phép lễ tân đóng phiên khách (GuestVisit) thủ công khi cần (ví dụ khách checkout).

### Requirement 10: Lượt lưu trú & phiên thao tác (GuestSession / GuestVisit)

**User Story:** Là chủ hệ thống, tôi muốn khách trong cùng một lượt lưu trú dùng portal liền mạch, nhưng khi đổi khách thì trạng thái được làm mới, tránh sót dữ liệu của khách trước.

#### Acceptance Criteria
1. WHEN thiết bị quét QR phòng lần đầu THEN hệ thống SHALL tạo `GuestSession` (cookie thiết bị) và một `GuestVisit` cho phòng đó, trạng thái `Active`.
2. WHEN cùng thiết bị quét lại QR của cùng phòng và GuestVisit vẫn `Active` THEN hệ thống SHALL **nối lại đúng GuestVisit đó** (không tạo mới), giữ nguyên hội thoại và acknowledgement.
3. THE **cửa sổ thao tác** SHALL có thời hạn ngắn cấu hình được (mặc định **30 phút**) tính từ hoạt động gần nhất; WHEN quá hạn và khách thao tác tiếp THEN hệ thống SHALL yêu cầu **quét QR lại để tiếp tục** (thao tác này chỉ mở khoá và nối lại GuestVisit đang Active).
4. THE `GuestVisit` SHALL kết thúc khi: (a) lễ tân **đóng thủ công** (checkout), hoặc (b) **quá hạn nhàn rỗi dài** cấu hình được (mặc định 24 giờ không hoạt động) → chuyển `Closed`/`Expired`.
5. WHEN GuestVisit đã kết thúc và khách quét QL lại THEN hệ thống SHALL tạo **GuestVisit mới** (dữ liệu lượt trước không hiển thị cho lượt mới).
6. THE hệ thống SHALL cho phép lễ tân đóng `GuestVisit` thủ công bất cứ lúc nào.
7. THE hệ thống SHALL thiết kế đường mở để sau này map `GuestVisit` với PMS/booking (không triển khai ở giai đoạn này).
8. WHEN một `GuestVisit` kết thúc (lễ tân đóng hoặc idle hết hạn) THEN hệ thống SHALL **đóng hội thoại Open** của visit đó và **huỷ ticket dọn phòng đang mở** do visit đó tạo (chuyển `Cancelled`), và khách của visit đã kết thúc SHALL KHÔNG gửi thêm tin/tạo ticket được (phải quét lại tạo visit mới). Lịch sử vẫn giữ để lễ tân tra cứu.

### Requirement 11: Xác thực, phân quyền & bảo mật (bối cảnh mạng nội bộ)

**User Story:** Là chủ hệ thống, tôi muốn kiểm soát truy cập phù hợp với việc chạy trong WiFi nội bộ, không cần bảo mật quá mức của một dịch vụ public internet.

#### Acceptance Criteria
1. THE hệ thống SHALL yêu cầu đăng nhập cho Admin và Staff, phân quyền theo Role (access token + refresh token).
2. THE guest web SHALL KHÔNG yêu cầu đăng nhập nhưng SHALL cấp một cookie `GuestSession` (HttpOnly) **định danh thiết bị, thời hạn dài (~30–90 ngày)** để gắn lượt lưu trú/hội thoại/acknowledgement/ticket. (Việc "phiên 30 phút" là **cửa sổ thao tác** ở tầng ứng dụng, không phải thời hạn cookie — xem Req 10.)
3. THE hệ thống SHALL giới hạn quyền: Staff xử lý tin nhắn/ghi chú/nội quy/FAQ/ticket; Admin có thêm quyền quản lý phòng/QR/người dùng/settings.
4. THE hệ thống SHALL sanitize nội dung HTML và validate input phía guest để chống XSS/injection giữa những người dùng chung mạng.
5. THE hệ thống SHALL áp dụng rate limiting nhẹ cho resolve token, gửi tin nhắn và tạo ticket.
6. THE hệ thống SHALL KHÔNG ghi full QR token, mật khẩu hay refresh token vào log.
7. THE tài liệu và UI SHALL KHÔNG tuyên bố acknowledgement nội quy là chứng cứ pháp lý; đây là bản ghi vận hành.

### Requirement 12: Vận hành nhẹ & khả năng quan sát

**User Story:** Là dev vận hành, tôi muốn đủ log để kiểm tra sự cố, không cần hệ thống backup/observability nặng.

#### Acceptance Criteria
1. THE hệ thống SHALL ghi **log có cấu trúc** (request id, room id khi resolve, conversation id, error code) đủ để dev debug.
2. THE hệ thống SHALL cung cấp health check cơ bản (`/health/live`, `/health/ready`).
3. THE hệ thống SHALL KHÔNG yêu cầu backup/restore tự động ở giai đoạn MVP; tuy nhiên trước khi lên **production thật**, SHALL có tối thiểu một **script dump DB thủ công** (khuyến nghị) để phòng mất dữ liệu.
4. THE hệ thống SHALL có seed data khởi tạo: resort, `ResortSettings`, tài khoản admin, ngôn ngữ (`ResortLanguage`, mặc định `en`).
5. THE hệ thống SHALL chạy trên **HTTPS với chứng chỉ hợp lệ** (secure context) sau reverse proxy, DNS nội bộ trỏ về server resort — bắt buộc để camera (StaffScan/QR) hoạt động trên điện thoại và tránh cảnh báo self-signed.

### Requirement 13: Hiệu năng & trải nghiệm mobile

**User Story:** Là một khách dùng điện thoại trong WiFi resort, tôi muốn trang tải nhanh và dễ dùng.

#### Acceptance Criteria
1. THE guest web SHALL mobile-first, tối ưu bundle size (SPA riêng biệt với admin), nút lớn, contrast tốt, dùng được một tay.
2. WHEN khách mở trang THEN nội dung chính (nội quy/FAQ) SHALL tải nhanh; SignalR/chat lazy-load khi vào mục nhắn tin.
3. THE hệ thống SHALL hỗ trợ trình duyệt di động phổ biến (Safari iOS, Chrome Android).

### Requirement 14: Cấu hình vận hành (ResortSettings)

**User Story:** Là admin, tôi muốn chỉnh các thông số vận hành ở một nơi để không phải sửa code.

#### Acceptance Criteria
1. THE hệ thống SHALL lưu một bộ `ResortSettings` cho mỗi resort gồm tối thiểu: bật/tắt yêu cầu ack trước FAQ/chat/ticket; `PortalWindowMinutes` (mặc định 30); `VisitIdleExpiryHours` (mặc định 24); `GuestWebBaseUrl` (dùng để dựng URL trong QR); `MaxMessageLength`; ngưỡng rate limit gửi tin/tạo ticket.
2. THE endpoint `/resolve` SHALL trả về các cờ tính năng liên quan (faqEnabled, chatEnabled, ruleAckRequiredForFaq/Chat/Housekeeping) lấy từ ResortSettings để guest web biết cách hiển thị.
3. THE việc sinh QR SHALL dùng `GuestWebBaseUrl` từ ResortSettings, không hardcode host.
4. THE Admin SHALL xem/sửa ResortSettings qua giao diện (`GET/PUT /api/admin/settings`); Staff KHÔNG có quyền sửa settings.
5. IF một giá trị settings không được đặt THEN hệ thống SHALL dùng giá trị mặc định an toàn (đọc từ `appsettings` hoặc hằng số).
