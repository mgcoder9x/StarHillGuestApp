using ResortQr.Domain.Rooms;

namespace ResortQr.Application.Rooms;

// ---- Create ----
public sealed record CreateRoomInput(string RoomNumber, string? Building, int? Floor);

public sealed record CreateRoomResult(Guid RoomId, string Token, string TokenPreview);

// ---- Rotate/revoke token ----
public sealed record RotateRoomTokenInput(Guid RoomId, string? Reason);

public sealed record RotateRoomTokenResult(string Token, string TokenPreview);

// ---- Update / status / delete ----
public sealed record UpdateRoomInput(Guid RoomId, string RoomNumber, string? Building, int? Floor);

public sealed record ChangeRoomStatusInput(Guid RoomId, RoomStatus Status);

// ---- Read model (queries) ----
public sealed record RoomListItem(
    Guid Id,
    string RoomNumber,
    string? Building,
    int? Floor,
    RoomStatus Status,
    string? ActiveTokenPreview,
    DateTimeOffset CreatedAt);

// ---- QR render ----
public sealed record RenderRoomQrPngInput(Guid RoomId);

public sealed record RenderRoomQrPngResult(byte[] Png);
