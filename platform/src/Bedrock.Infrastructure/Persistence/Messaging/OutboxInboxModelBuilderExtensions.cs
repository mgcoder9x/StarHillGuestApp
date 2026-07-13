using Bedrock.Application.Messaging.Dispatch;
using Microsoft.EntityFrameworkCore;

namespace Bedrock.Infrastructure.Persistence.Messaging;

/// <summary>
/// Map <c>outbox_message</c> + <c>inbox_message</c> vào CHÍNH DbContext/schema của module gọi (per-module —
/// design §4.6). Cùng DbContext = cùng transaction hiển nhiên → CP6 (state + outbox nguyên tử) đúng BẰNG
/// CẤU TRÚC, không cần distributed transaction. Module gọi trong <c>OnModelCreating</c> của DbContext dẫn xuất.
/// <para>
/// Tên cột dựa quy ước snake_case (<c>AddBedrockPersistence</c> luôn bật <c>UseSnakeCaseNamingConvention</c>) →
/// filter partial index dùng literal snake_case khớp cột thật. <paramref name="isNpgsql"/> quyết định kiểu cột
/// payload: <c>jsonb</c> (Npgsql — index/query được) hay mặc định text (provider khác, vd SQLite test).
/// </para>
/// </summary>
public static class OutboxInboxModelBuilderExtensions
{
    public static ModelBuilder AddOutboxInbox(this ModelBuilder modelBuilder, bool isNpgsql, string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_message", schema);
            entity.HasKey(m => m.Id);

            entity.Property(m => m.EventType).IsRequired();
            entity.Property(m => m.Payload).IsRequired();
            entity.Property(m => m.LastError).HasMaxLength(OutboxMessage.MaxLastErrorLength); // A-28: bound diagnostic text.
            entity.Property(m => m.TraceParent).HasMaxLength(OutboxMessage.MaxTraceParentLength);  // P1-14: W3C traceparent.
            entity.Property(m => m.TraceState).HasMaxLength(OutboxMessage.MaxTraceStateLength);    // P1-14: W3C tracestate.
            if (isNpgsql)
            {
                // jsonb: query/index được trên Postgres; provider khác giữ text (SQLite không có jsonb).
                entity.Property(m => m.Payload).HasColumnType("jsonb");
            }

            // Partial index cho dispatcher poll (design §4.5): chỉ index row còn pending → nhỏ + nhanh.
            // Filter dùng literal snake_case khớp quy ước cột (processed_at/dead_lettered_at). Hỗ trợ cả Npgsql + SQLite.
            entity.HasIndex(m => m.OccurredAt)
                .HasDatabaseName("ix_outbox_pending")
                .HasFilter("processed_at IS NULL AND dead_lettered_at IS NULL");
            entity.HasIndex(m => new { m.ClaimedUntil, m.NextAttemptAt, m.OccurredAt })
                .HasDatabaseName("ix_outbox_claimable")
                .HasFilter("processed_at IS NULL AND dead_lettered_at IS NULL");
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("inbox_message", schema);
            entity.HasKey(m => new { m.MessageId, m.Consumer }); // idempotency chống xử lý trùng
        });

        return modelBuilder;
    }
}
