# Architecture Selection: Guest Web — Guest Journey (resolve → force-read → FAQ → chat → housekeeping)

> Phạm vi: kiến trúc **frontend guest-web SPA** cho hành trình khách (Req 2/3/4/5/6/10/13/14, mặt guest).
> Backend đã DONE (resolve trả context+features+visit; rule-gate enforce 403; conversation/housekeeping/faq guest endpoints; SignalR).
> Nguồn: `docs/resort-qr-portal/requirements.md`, guest-web hiện có (`guestSession`/`rules`/`guestApi`/router), journal QR-N-079.
> Mọi kết luận bám code/req đã ĐỌC — không suy đoán.

## Recommended Architecture: Candidate B — Journey Gate/Session Core + API Gateway (thin capability views)

### Rationale
Độ khó chi phối của feature KHÔNG phải là từng màn hình, mà là **các bất biến gating xuyên suốt** (INV1 unlock-theo-flag+ack, INV2 server-authoritative, INV3 hết-cửa-sổ→rescan) do Req 3/10/14 áp lên MỌI capability. Candidate B tập trung đúng ba bất biến này vào **một core + một API gateway** (cross-cutting invariants ~25% vs 62% của kiến trúc route-scattered hiện tại; 0 sync cycle; evolvability ~2). Trade-off: core trở thành **fan-in hub** và có nguy cơ thành god-object nếu ôm luôn dữ liệu capability — được chặn bằng ràng buộc "core chỉ giữ session/ack/flags/window; dữ liệu FAQ/chat/ticket nằm ở view/slice" (giữ god-object ~48% < 50%). Nếu ưu tiên là **mở rộng vô hạn số capability độc lập**, Candidate C tốt hơn (god-object ~25%, evolvability ~1) nhưng thêm boilerplate + fan-in hạ tầng dùng chung.

### Components
| Component | Owned State | Responsibility |
|-----------|-------------|----------------|
| `JourneyCore` (Pinia store/service) | resolvedContext(room/resort/visit), features(flags+ackRequired), **ruleAck** (ackedVersion/currentVersion/isAcked — server-authoritative), portalWindow(expiresAt/isLive), selectedLanguage | Nguồn sự thật DUY NHẤT cho "capability nào mở khoá" (derived: `canFaq/canChat/canHousekeeping/mustReadRules/mustRescan`). KHÔNG giữ dữ liệu FAQ/chat/ticket. |
| `ApiGateway` (fetch wrapper) | — (stateless) | Điểm ra/vào HTTP DUY NHẤT: đính ngữ cảnh, map ProblemDetails→lỗi có `code`; **chặn 401/`session_expired`→dispatch rescan**, **403 `rule_ack_required`→refresh ruleAck vào Core**. Không view nào gọi `fetch` trực tiếp. |
| `SessionPersistence` | sessionStorage keys | Boundary lưu/đọc context+language+ack-cache theo visit; nguồn khôi phục sau reload; xoá khi visit đổi. Tách khỏi Core (Core đọc/ghi qua nó). |
| `RealtimeChannel` | connection state, fallback timer | SignalR `/hubs/chat` + **polling fallback**; đẩy tin/nhận tin cho ChatView; không biết gate. |
| Capability Views (`ResolveView`,`RulesView`,`FaqView`,`ChatView`,`HousekeepingView`,`HomeView`) | dữ liệu + UI-state riêng của từng màn (faqTree, messages, ticketStatus, forceReadProgress) | Render + phát intent; đọc capability-availability từ Core; gọi dữ liệu qua ApiGateway. Mỏng, test được. |
| `Router` (guards) | — | Điều hướng; guard đọc Core (`mustReadRules`/`mustRescan`/`canX`) để chặn vào màn chưa mở khoá. |

### Information Flow
| From \ To | JourneyCore | ApiGateway | SessionPersistence | RealtimeChannel | Views | Router |
|-----------|-------------|-----------|--------------------|-----------------|-------|--------|
| JourneyCore | — | → (refresh ack) | ↔ | | | |
| ApiGateway | → (events: expired/ack) | — | | | | |
| SessionPersistence | | | — | | | |
| RealtimeChannel | → (new-message event) | | | — | → (ChatView) | |
| Views | → (read derived + intents) | → (data calls) | | → (chat subscribe) | — | |
| Router | → (read gate) | | | | → (navigate) | — |

(→ gọi/đẩy; ↔ hai chiều. Không có cạnh nào tạo chu trình đồng bộ: Views→Gateway→Core là một chiều; Core→Persistence hai chiều nhưng Persistence là leaf I/O, không gọi ngược logic.)

### Requirement Allocation
| Requirement | Component(s) |
|-------------|--------------|
| Req 1 (resolve/token, scrub URL) | ResolveView + ApiGateway (đã có QR-N-079) |
| Req 2 (đa ngôn ngữ + fallback) | JourneyCore (selectedLanguage) + i18n + Views (render isFallback) |
| Req 3 (force-read + gate) | **JourneyCore (ruleAck+mustReadRules)** + RulesView (forceReadProgress) + Router guard + ApiGateway (403→refresh) |
| Req 4 (FAQ tree + CTA→chat) | FaqView (+ intent sang ChatView qua Core context) |
| Req 5 (chat realtime + fallback) | ChatView + RealtimeChannel + ApiGateway |
| Req 6 (housekeeping ticket + status) | HousekeepingView + ApiGateway |
| Req 10 (visit/portal-window/rescan) | **JourneyCore (portalWindow) + ApiGateway (session_expired→rescan)** + SessionPersistence (reset khi visit đổi) |
| Req 13 (mobile/perf/lazy) | Router (route-level code-split: chat/realtime lazy) + Views |
| Req 14 (feature flags từ resolve) | JourneyCore (features) |

### Key Design-Induced Invariants
- **DI-1**: Chỉ `ApiGateway` được gọi `fetch`; view/store khác KHÔNG. (Đảm bảo INV2/INV3 xử lý một chỗ — chống drift kiểu `rules.ts` boolean hiện tại.)
- **DI-2**: `JourneyCore` chỉ giữ session/ack/flags/window; KHÔNG giữ dữ liệu FAQ/chat/ticket (chống god-object >50%).
- **DI-3**: Capability-availability là **derived getters** trên Core (không bang state trùng lặp ở view/guard) → một nguồn sự thật.
- **DI-4**: `ruleAck` trong Core phải đến từ server (resolve/endpoint ack), KHÔNG phải boolean client — client-gate chỉ là advisory UX; server 403 là chốt chặn thật (khắc phục drift `rules.ts`).
- **DI-5**: Đổi `visit.id` → `SessionPersistence.clear()` + reset Core (INV4: không rò dữ liệu visit trước).

### Alternatives Considered
| Candidate | Strength | Weakness | Why Not Selected |
|-----------|----------|----------|------------------|
| A — Route/View + scattered stores (quỹ đạo hiện tại) | Đơn giản, hợp Vue idiom, nhanh; ít component | Gate/window/ack **rải khắp** view+guard+store; ĐÃ sinh drift thật (`rules.ts` boolean ≠ ack server); cross-cutting invariants ~62% | Chính cross-cutting là độ khó lớn nhất — A làm nó tệ nhất; đã lộ bug |
| **B — Gate/Session Core + API Gateway** ✅ | Tập trung đúng 3 bất biến gating; 0 cycle; evolvability ~2; sửa tận gốc drift ack | Core là fan-in hub + nguy cơ god-object nếu ôm data (chặn bằng DI-2) | **CHỌN** |
| C — Capability slices + pure-gate + cache boundary | God-object thấp nhất (~25%), evolvability ~1, slice độc lập; cache boundary tường minh | Nhiều component/boilerplate cho app 4-capability; fan-in cao lên hạ tầng dùng chung (gate/http/cache) | Over-engineer cho quy mô MVP 4 capability; giá trị mở-rộng chưa cần |

### Metrics Summary
| Metric | Selected (B) | Alt A | Alt C |
|--------|--------------|-------|-------|
| Cross-cutting reqs % | ~25% | ~45% | ~20% |
| Cross-cutting invariants % | ~25% | ~62% | ~20% |
| Flow density | ~0.20 | ~0.22 | ~0.19 |
| God object score | ~48% (chặn <50% qua DI-2) | ~30% | ~25% |
| Sync cycles | 0 | 0–1 (guard↔store↔view) | 0 |
| Max fan-in | JourneyCore/ApiGateway (cao) | ApiCall/SessionStore | gate/http/cache (cao) |
| Max fan-out | Views→{Core,Gateway,Realtime} | Views→{nhiều store} | slice→{session,gate,http} |
| Evolvability cost | ~2 | ~3–4 | ~1 |

## Phụ lục — Biến & Bất biến (rút gọn từ phân tích)
**Biến chính**: `selectedLanguage`(Config/State), `resolvedContext`(State), `features{faq/chat/housekeeping Enabled + ruleAckRequiredFor*}`(Feature Flag), `portalWindowExpiresAt`(State), `ruleAck{ackedVersion,currentVersion,isAcked}`(State, server), `ruleSections[requireScrollEnd,minReadSeconds,order]`(Input), `forceReadProgress`(State-UI), `faqTree`(Input), `conversation{messages,unread,open}`(State+Event), `housekeepingTicket.status`(State), `sessionExpired`(Event), `ruleAckRequired 403`(Event).
**Bất biến**: INV1 unlock = flag∧(¬ackRequired∨acked-current); INV2 server-authoritative (client-gate advisory); INV3 hết cửa sổ→rescan (không silent-success); INV4 chỉ thấy dữ liệu visit hiện tại; INV5 ngôn ngữ áp cả UI+nội dung, isFallback không để trống; INV6 "Tiếp tục" bật khi đủ scroll-end+min-seconds+checkbox; INV7 không lộ token ở URL/log; INV8 rules/faq sanitized-server, chat plain-text.
