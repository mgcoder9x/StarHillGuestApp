using Rooms.Domain;

namespace Rooms.Application;

// ---- Create ----
// QR-DV-004: THÊM ResortId vào input (khác resort-qr — nơi CreateRoom đọc single-Resort từ repo). Lý do: Resort
// sống ở module ResortConfig (schema khác, KHÔNG FK chéo-schema — QR-DV-003) → Rooms.Application KHÔNG có repo
// Resort. Caller (Rooms.Api ở slice sau) phân giải resortId (single-resort qua IResortSettingsQuery/claim) rồi
// truyền vào. Giữ Rooms.Application KHÔNG phụ thuộc ResortConfig.Contracts ở 2b-i (I10 — chỉ thêm khi RenderQrPng cần).
public sealed record CreateRoomInput(Guid ResortId, string RoomNumber, string? Building, int? Floor);

public sealed record CreateRoomResult(Guid RoomId, string Token, string TokenPreview);

// ---- Rotate/revoke token ----
public sealed record RotateRoomTokenInput(Guid RoomId, string? Reason);

public sealed record RotateRoomTokenResult(string Token, string TokenPreview);

// ---- Update / status / delete ----
public sealed record UpdateRoomInput(Guid RoomId, string RoomNumber, string? Building, int? Floor);

public sealed record ChangeRoomStatusInput(Guid RoomId, RoomStatus Status);

// ---- QR render (B-Rooms.2b-ii) ----
public sealed record RenderRoomQrPngInput(Guid RoomId);

public sealed record RenderRoomQrPngResult(byte[] Png);

// NOTE: RoomListItem (read-model) → slice query sau (I10).
