using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Concierge.Application;
using Concierge.Contracts;
using GuestAccess.Contracts.Events;
using Housekeeping.Application;
using Microsoft.Extensions.Logging.Abstractions;
using StarHill.Api;
using Xunit;

namespace StarHill.Api.Tests.Messaging;

/// <summary>
/// C-GA.5b — GUARD handler cascade <see cref="GuestVisitEndedCascadeHandler"/> (Docker-free, fake use case). Biên unit
/// ĐÚNG: handler chỉ điều phối (gọi close-conversation + cancel-tickets với đúng visitId + fail→ném cho redeliver);
/// HIỆU ỨNG THẬT của hai use case (đóng hội thoại / huỷ ticket idempotent) đã được gác riêng ở ConciergeStaffUseCaseTests
/// + HousekeepingUseCaseTests. Runtime RabbitMQ/inbox = messaging overlay (Docker/CI).
/// </summary>
public sealed class GuestVisitEndedCascadeHandlerTests
{
    private sealed class FakeCloseConversation : ICommandUseCase<CloseConversationForVisitInput>
    {
        public string PersistenceKey => ConciergeModule.PersistenceKey;
        public Result Next { get; set; } = Result.Success();
        public int Calls { get; private set; }
        public Guid? LastVisitId { get; private set; }

        public Task<Result> ExecuteAsync(CloseConversationForVisitInput input, CancellationToken ct = default)
        {
            Calls++;
            LastVisitId = input.GuestVisitId;
            return Task.FromResult(Next);
        }
    }

    private sealed class FakeCancelTickets : IUseCase<CancelOpenTicketsForVisitInput, CancelOpenTicketsForVisitResult>
    {
        public Result<CancelOpenTicketsForVisitResult> Next { get; set; } = Result.Success(new CancelOpenTicketsForVisitResult(0));
        public int Calls { get; private set; }
        public Guid? LastVisitId { get; private set; }

        public Task<Result<CancelOpenTicketsForVisitResult>> ExecuteAsync(CancelOpenTicketsForVisitInput input, CancellationToken ct = default)
        {
            Calls++;
            LastVisitId = input.GuestVisitId;
            return Task.FromResult(Next);
        }
    }

    private static GuestVisitEndedCascadeHandler Build(FakeCloseConversation close, FakeCancelTickets cancel) =>
        new(close, cancel, NullLogger<GuestVisitEndedCascadeHandler>.Instance);

    private static GuestVisitEndedIntegrationEvent Event(Guid visitId) =>
        new(Guid.CreateVersion7(), DateTimeOffset.UnixEpoch, visitId);

    [Fact]
    public async Task Handles_by_calling_both_use_cases_with_event_visit_id()
    {
        var close = new FakeCloseConversation();
        var cancel = new FakeCancelTickets();
        var handler = Build(close, cancel);
        var visitId = Guid.CreateVersion7();

        await handler.HandleAsync(Event(visitId));

        Assert.Equal(1, close.Calls);
        Assert.Equal(1, cancel.Calls);
        Assert.Equal(visitId, close.LastVisitId);
        Assert.Equal(visitId, cancel.LastVisitId);
    }

    [Fact]
    public async Task Throws_and_skips_cancel_when_close_fails()
    {
        var close = new FakeCloseConversation { Next = Result.Failure(Error.Unexpected("concurrency_conflict", "đụng")) };
        var cancel = new FakeCancelTickets();
        var handler = Build(close, cancel);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(Event(Guid.CreateVersion7())));
        Assert.Equal(0, cancel.Calls); // close fail trước → không tới cancel; redeliver retry cả hai.
    }

    [Fact]
    public async Task Throws_when_cancel_fails()
    {
        var close = new FakeCloseConversation();
        var cancel = new FakeCancelTickets
        {
            Next = Result.Failure<CancelOpenTicketsForVisitResult>(Error.Unexpected("concurrency_conflict", "đụng")),
        };
        var handler = Build(close, cancel);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(Event(Guid.CreateVersion7())));
        Assert.Equal(1, close.Calls);
    }

    [Fact]
    public async Task Is_repeatable_when_both_succeed()
    {
        var close = new FakeCloseConversation();
        var cancel = new FakeCancelTickets();
        var handler = Build(close, cancel);
        var evt = Event(Guid.CreateVersion7());

        await handler.HandleAsync(evt);
        await handler.HandleAsync(evt); // redeliver at-least-once → handler không giữ state chặn; use case idempotent.

        Assert.Equal(2, close.Calls);
        Assert.Equal(2, cancel.Calls);
    }
}
