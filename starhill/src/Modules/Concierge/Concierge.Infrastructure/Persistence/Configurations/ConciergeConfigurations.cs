using Concierge.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concierge.Infrastructure.Persistence.Configurations;

// RowVersion (xmin) do PlatformDbContext.OnModelCreating tự map cho entity IHasConcurrencyToken trên Npgsql (CP15).
// Id là UUIDv7 client-side → ValueGeneratedNever (mirror Housekeeping/Rules/Faq/Rooms/GuestAccess).

/// <summary>
/// EF config <see cref="Conversation"/> — unique "1 hội thoại/visit" (<c>ux_conversation_visit</c> trên
/// <see cref="Conversation.GuestVisitId"/>, Req 5.2; reopen KHÔNG tạo mới). Index (resort, room, thời điểm tin gần
/// nhất) cho board dashboard gom theo phòng + sắp mới nhất (Req 5.3). Status lưu string.
/// </summary>
public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("conversation");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        // 1 hội thoại MỖI visit (Req 5.2) — reopen mở lại đúng hội thoại này, không tạo hội thoại thứ hai.
        builder.HasIndex(x => x.GuestVisitId)
            .IsUnique()
            .HasDatabaseName("ux_conversation_visit");

        builder.HasIndex(x => new { x.ResortId, x.RoomId, x.LastMessageAt })
            .HasDatabaseName("ix_conversation_resort_room_last");
    }
}

/// <summary>
/// EF config <see cref="Message"/> — append-only; FK→<see cref="Conversation"/> Cascade (tin thuộc hội thoại); index
/// (hội thoại, thời điểm) để đọc lịch sử theo thứ tự. SenderType lưu string. <see cref="Message.Body"/> plain text
/// (không giới hạn ở tầng cột — giới hạn động theo cấu hình MaxMessageLength ở use case K-Con.2).
/// </summary>
public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("message");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SenderType).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt }).HasDatabaseName("ix_message_conversation");

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF config <see cref="InternalNote"/> — ghi chú nội bộ nhân viên; FK nội-schema <see cref="InternalNote.ConversationId"/>?
/// →<see cref="Conversation"/> Restrict (giữ vết — không xoá hội thoại còn ghi chú trỏ tới); index (resort, room) để
/// liệt kê ghi chú theo phòng. <see cref="InternalNote.Body"/> plain text.
/// </summary>
public sealed class InternalNoteConfiguration : IEntityTypeConfiguration<InternalNote>
{
    public void Configure(EntityTypeBuilder<InternalNote> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("internal_note");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasIndex(x => new { x.ResortId, x.RoomId }).HasDatabaseName("ix_note_resort_room");

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
