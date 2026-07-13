# Design — P1-14: Tách business CorrelationId khỏi W3C trace context (traceparent + tracestate)

> Trạng thái: **DESIGN-FIRST — CHỜ USER VALID trước khi triển khai** (theo process: chuẩn bị thiết kế rõ → valid → mới code).
> Ngày: 2026-07-13 · Nguồn finding: `re-audit-architecture-implementation-2026-07-12.md` §P1-14 (PARTIAL/A-29).
> Phạm vi: BASE `platform/` (envelope/producer/dispatcher/mapper/consumer/persistence) + đồng bộ `starhill/` (dùng base qua `$(PlatformSrc)` + có migration Identity riêng).

---

## 1. Vấn đề (BẢN CHẤT — trích code THẬT, không suy đoán)

Luồng trace hiện tại (đã đọc code):

| Nơi | Code hiện tại | Vấn đề |
|---|---|---|
| `EfOutboxWriter.EnqueueAsync` | `CorrelationId = Activity.Current?.Id` | Lưu **traceparent** (W3C id) vào field tên `CorrelationId`. **Vứt `Activity.Current.TraceStateString`** (vendor sampling/state). |
| `OutboxMessage` (persistence) | 1 cột `correlation_id` (nullable string) | Cột "correlation" thực chất chứa traceparent → sai ngữ nghĩa; không có chỗ chứa tracestate. |
| `RabbitMqMessageMapper.PropertiesOf` | `properties.CorrelationId = message.CorrelationId` | Đặt traceparent vào **AMQP `BasicProperties.CorrelationId`** — property này theo quy ước AMQP là **business/RPC correlation**, không phải trace. Vendor/adapter khác interop sai. |
| `RabbitMqConsumer.OnReceivedAsync` | `CorrelationId = ea.BasicProperties.CorrelationId` | Đọc ngược traceparent từ property sai ngữ nghĩa. |
| `EfIntegrationEventDispatcher.DispatchAsync` | `ActivityContext.TryParse(message.CorrelationId, null, out parentContext)` | Parse traceparent từ 1 string; **KHÔNG có tracestate** (tham số thứ 2 = null) → mất vendor sampling state; không dùng propagator chuẩn. |

**Ba lỗi cốt lõi:**
1. **Trộn semantic:** một field/cột/AMQP-property `CorrelationId` gánh 2 khái niệm khác nhau — *business correlation* (đối soát nghiệp vụ xuyên log/service) và *distributed trace context* (traceparent). AMQP `BasicProperties.CorrelationId` bị dùng sai mục đích.
2. **Mất `tracestate`:** chỉ traceparent (`Activity.Id`) được mang; `TraceStateString` (quyết định sampling của vendor, W3C `tracestate`) bị bỏ → downstream OTel/vendor mất thông tin sampling.
3. **Không dùng propagation chuẩn:** tự parse 1 string thay vì `DistributedContextPropagator` (chuẩn .NET/OTel, xử lý traceparent + tracestate + baggage) → khó interop hệ ngoài.

> **Lưu ý quan trọng:** đây là lỗi **đúng-chuẩn/interop + observability**, KHÔNG phải bug chức năng — trace HIỆN VẪN propagate (AD-084 + guard `IntegrationEventTracePropagationTests` chứng minh consume span là con của trace gốc). Vì vậy đây là hardening chất lượng, không phải fix crash.

---

## 2. Mục tiêu / Ngoài phạm vi

**Mục tiêu:**
- G1. Mang **traceparent + tracestate** đúng chuẩn W3C xuyên bus (không mất tracestate).
- G2. Tách **business CorrelationId** thành khái niệm RIÊNG, độc lập trace.
- G3. AMQP dùng **header `traceparent`/`tracestate`** (chuẩn messaging OTel) cho trace; `BasicProperties.CorrelationId` chỉ cho business correlation.
- G4. Consumer span dựng từ traceparent+tracestate qua cơ chế chuẩn (giữ tracestate).
- G5. Verify được KHÔNG cần Docker (ActivityListener + unit mapper + snapshot migration).

**Ngoài phạm vi (defer có lý do):**
- N1. **Producer publish span** (`ActivityKind.Producer` lúc dispatch): consume span đã link về trace enqueue; thêm publish span là enhancement → defer.
- N2. **Business-correlation-context port** (nguồn business correlation id thật, vd từ header `X-Correlation-ID` request): là feature riêng; phiên này để `CorrelationId = null` (chưa có nguồn) — xem Quyết định D4.
- N3. **Baggage propagation**: chưa có nhu cầu; defer.

---

## 3. Thiết kế đích

### D1 — Hai khái niệm TÁCH BẠCH
- **W3C trace context** = `traceparent` (bắt buộc) + `tracestate` (tuỳ chọn, vendor). Dùng cho distributed tracing.
- **Business CorrelationId** = string tuỳ chọn, ổn định theo một luồng nghiệp vụ, độc lập trace.

### D2 — Persistence `OutboxMessage` (QUYẾT ĐỊNH: additive, không rename)
- **THÊM** 2 cột nullable: `trace_parent` (string, maxlen ~55 cho W3C traceparent nhưng để 512 an toàn) + `trace_state` (string, maxlen 1024 — tracestate có thể dài).
- **GIỮ** cột `correlation_id` (nullable) → nay dành cho **business correlation** (hiện null — xem D4).
- **Vì sao additive (không rename `correlation_id`→`trace_parent`):** rename cột = data-migration rủi ro (giá trị cũ là traceparent, không phải business) + không đảo được dễ. Additive = an toàn, cột cũ đơn giản đổi Ý NGHĨA (thôi ghi traceparent vào nó). Migration chỉ ADD COLUMN nullable → không đụng data cũ.
- **Blast radius migration:** schema `outbox_message` chia sẻ qua `AddOutboxInbox` → mọi DbContext có outbox cần migration: **platform Identity + starhill Identity** (2 migration). Sinh bằng `dotnet ef migrations add` (không cần Docker); verify qua model snapshot + `PendingModelChangesWarning` test; DB apply chạy CI (chuẩn AD-087).

### D3 — Envelope
- `OutgoingIntegrationMessage`: THÊM `TraceParent` (string?) + `TraceState` (string?); GIỮ `CorrelationId` (string?, business).
- `IncomingIntegrationMessage`: THÊM `TraceParent` + `TraceState`; GIỮ `CorrelationId` (business). Sửa docstring (hiện nói CorrelationId là traceparent — sai sau đổi).

### D4 — Producer (`EfOutboxWriter.EnqueueAsync`)
```
TraceParent = Activity.Current?.Id                 // W3C id (khi IdFormat=W3C)
TraceState  = Activity.Current?.TraceStateString   // vendor sampling — TRƯỚC ĐÂY BỊ VỨT
CorrelationId = null                               // business correlation: CHƯA có nguồn (xem Q1)
```
- **Quyết định D4 (cần valid — Q1):** `CorrelationId = null` phiên này (chưa có business-correlation port). KHÔNG suy diễn từ TraceId (tránh tái-trộn trace vào business). Business-correlation port là follow-up (N2).

### D5 — Wire (`RabbitMqMessageMapper.PropertiesOf`)
- Header `traceparent` = envelope.TraceParent (nếu có); header `tracestate` = envelope.TraceState (nếu có). (Tên header chuẩn W3C/OTel messaging.)
- `BasicProperties.CorrelationId` = envelope.CorrelationId (business) — chỉ set nếu có (hiện null → không set).
- Bỏ dòng cũ set CorrelationId = traceparent.

### D6 — Consumer (`RabbitMqConsumer.OnReceivedAsync`)
- Đọc header `traceparent`/`tracestate` (qua `DecodeHeader`) → `IncomingIntegrationMessage.TraceParent/TraceState`.
- Đọc `ea.BasicProperties.CorrelationId` → business `CorrelationId`.

### D7 — Dispatcher (`EfIntegrationEventDispatcher.DispatchAsync`)
- Dựng parent context GIỮ tracestate: `ActivityContext.Parse(traceParent, traceState)` (overload có tracestate) khi traceParent hợp lệ → `StartActivity(Consumer, parentContext)`. Không parse được → fallback ambient (như hiện tại).
- Nếu business `CorrelationId` có → set tag `bedrock.correlation_id` (không trộn vào trace context).

> **Cân nhắc `DistributedContextPropagator` vs `ActivityContext.Parse`:** propagator chuẩn dùng khi extract từ carrier headers đa khoá (traceparent+tracestate+baggage). Ở đây ta ĐÃ có 2 string tách rời (traceParent, traceState) từ envelope → `ActivityContext.Parse(traceParent, traceState)` là API BCL trực tiếp, đủ + rõ, không cần dựng carrier giả. (Propagator sẽ cần khi thêm baggage — N3.) → **Quyết định D7: dùng `ActivityContext.Parse` (2 tham số).**

### D8 — Producer publish span: DEFER (N1).

---

## 4. Kế hoạch verify (KHÔNG cần Docker)

1. **`IntegrationEventTracePropagationTests` (mở rộng):** producer Activity W3C + **set `TraceStateString`** → dispatcher dựng consume span → assert (a) cùng TraceId, (b) ParentSpanId = span gốc, (c) **`consume.TraceStateString` == tracestate gốc** (điểm MỚI — chứng minh không mất tracestate). Cập nhật input dùng `TraceParent`/`TraceState` thay `CorrelationId`.
2. **Mapper unit test (mới/mở rộng):** `OutgoingIntegrationMessage{TraceParent,TraceState,CorrelationId}` → `BasicProperties`: header `traceparent`/`tracestate` đúng; `CorrelationId` chỉ set khi có business.
3. **Consumer:** (Docker e2e đã có) — thêm assert header trace nếu khả thi ở unit; hoặc dựa e2e CI.
4. **Migration:** `dotnet ef migrations add AddOutboxTraceContext` (platform + starhill Identity) → model snapshot đồng bộ; `PendingModelChangesWarning` test xanh; DB apply CI.
5. **Snapshot bundle:** `dotnet ef migrations bundle` build OK (như QR-N-019).

---

## 5. Blast radius (file dự kiến đổi)
- Application: `OutgoingIntegrationMessage.cs`, `IncomingIntegrationMessage.cs`, `OutboxMessage.cs`.
- Infrastructure: `EfOutboxWriter.cs`, `EfIntegrationEventDispatcher.cs`, `OutboxInboxModelBuilderExtensions.cs` (map 2 cột mới) + migration + snapshot (platform Identity).
- Adapter: `RabbitMqMessageMapper.cs`, `RabbitMqConsumer.cs`.
- Tests: `IntegrationEventTracePropagationTests`, mapper tests, `EfOutboxDispatcher`/writer tests nếu chạm CorrelationId.
- starhill: migration Identity + snapshot (mirror); dùng chung base src nên code tự nhận.

---

## 6. Backward-compat
- Header `traceparent`/`tracestate` MỚI — consumer cũ bỏ qua (không vỡ). Producer cũ (chưa deploy) không gửi 2 header → consumer mới fallback ambient (graceful).
- Cột DB additive nullable → không đụng data cũ. Row outbox cũ (correlation_id = traceparent) sẽ KHÔNG được đọc làm trace nữa (trace_parent null → fallback ambient). Chấp nhận: chỉ ảnh hưởng event tồn đọng lúc migrate (hiếm, và chỉ mất linkage trace của số ít event cũ — không mất data/event).

---

## 7. Trade-offs (TO dự kiến ghi journal khi triển khai)
- **TO-a:** additive columns vs rename. Chọn additive (an toàn data, đảo được) — đổi lại có cột `correlation_id` tạm null (chưa dùng).
- **TO-b:** business CorrelationId = null now vs derive từ TraceId. Chọn null + follow-up port (tránh tái-trộn; đúng bản chất). Đổi lại: tạm thời không có business-correlation trong log (trace vẫn đủ để đối soát).
- **TO-c:** publish producer span defer. Đổi lại: trace enqueue→consume có link, nhưng bước publish không thành span riêng (đủ dùng, thêm sau).

---

## 8. Câu hỏi cần USER chốt (trước khi code)
- **Q1 (business CorrelationId):** đồng ý để `CorrelationId = null` phiên này + business-correlation port là follow-up? (Khuyến nghị: CÓ — tách khái niệm sạch, không suy diễn từ trace.)
- **Q2 (migration cross-tree):** đồng ý thêm 2 cột nullable `trace_parent`/`trace_state` vào `outbox_message` (migration platform Identity + starhill Identity)? Máy không Docker → verify qua snapshot + `PendingModelChangesWarning` + CI apply (chuẩn AD-087). (Khuyến nghị: CÓ.)
- **Q3 (publish span):** đồng ý DEFER producer publish span? (Khuyến nghị: CÓ.)

---

## 9. Thứ tự triển khai (khi được valid) — increment nhỏ, mỗi bước verify
1. Envelope + OutboxMessage cột (Application) — build.
2. EF map 2 cột + migration platform Identity + snapshot — `PendingModelChangesWarning` test.
3. EfOutboxWriter (capture traceparent+tracestate) + EfIntegrationEventDispatcher (parse 2 tham số) — ActivityListener test (assert tracestate).
4. Mapper + Consumer (header traceparent/tracestate) — mapper unit test.
5. starhill migration Identity + snapshot.
6. `vp all` + `vp journal` cả 2 cây; AD mới (AD-102) + guard map + TO entries.
```
```
