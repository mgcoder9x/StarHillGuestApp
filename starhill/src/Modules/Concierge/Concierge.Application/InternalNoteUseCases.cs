using Bedrock.Application.Ports.Persistence;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.UseCases;
using Bedrock.Domain.Results;
using Concierge.Contracts;
using Concierge.Domain;
using FluentValidation;

namespace Concierge.Application;

/// <summary>
/// Tạo ghi chú nội bộ của nhân viên (Req 9.4) — gắn phòng (<see cref="CreateInternalNoteInput.RoomId"/>) và/hoặc hội
/// thoại (<see cref="CreateInternalNoteInput.ConversationId"/>); ÍT NHẤT MỘT (validator). TUYỆT ĐỐI không lộ cho khách
/// (chỉ endpoint admin RequireStaff). <see cref="CreateInternalNoteInput.AuthorUserId"/> do Api cấp từ <c>ICurrentUser</c>.
/// Value-returning <see cref="IUseCase{TInput,TOutput}"/> tự quản SaveChanges. Nếu có <c>ConversationId</c> → VERIFY hội
/// thoại tồn tại (<see cref="IRepository{T}.AnyAsync"/>) → không có → <c>concierge_conversation_not_found</c> (tránh vi
/// phạm FK Restrict thô ở tầng DB, trả lỗi nghiệp vụ rõ ràng). Body PLAIN TEXT (trim).
/// </summary>
public sealed class CreateInternalNoteUseCase : IUseCase<CreateInternalNoteInput, CreateInternalNoteResult>
{
    private readonly IRepository<InternalNote> _notes;
    private readonly IRepository<Conversation> _conversations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateInternalNoteUseCase(
        IRepository<InternalNote> notes,
        IRepository<Conversation> conversations,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _notes = notes;
        _conversations = conversations;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CreateInternalNoteResult>> ExecuteAsync(
        CreateInternalNoteInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var body = (input.Body ?? string.Empty).Trim();
        if (body.Length == 0)
        {
            return Result.Failure<CreateInternalNoteResult>(ConciergeErrors.MessageEmpty);
        }

        if (input.ConversationId is not null)
        {
            var conversationExists = await _conversations
                .AnyAsync(c => c.Id == input.ConversationId.Value, ct)
                .ConfigureAwait(false);
            if (!conversationExists)
            {
                return Result.Failure<CreateInternalNoteResult>(ConciergeErrors.ConversationNotFound);
            }
        }

        var now = _clock.UtcNow;
        var note = new InternalNote
        {
            ResortId = input.ResortId,
            RoomId = input.RoomId,
            ConversationId = input.ConversationId,
            AuthorUserId = input.AuthorUserId,
            Body = body,
            CreatedAt = now,
        };
        _notes.Add(note);

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(new CreateInternalNoteResult(note.Id));
    }
}

/// <summary>Validate tạo ghi chú: có resort + tác giả + body không rỗng + gắn ÍT NHẤT phòng hoặc hội thoại.</summary>
public sealed class CreateInternalNoteValidator : AbstractValidator<CreateInternalNoteInput>
{
    public CreateInternalNoteValidator()
    {
        RuleFor(x => x.ResortId).NotEmpty();
        RuleFor(x => x.AuthorUserId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.RoomId is not null || x.ConversationId is not null)
            .WithMessage("Ghi chú phải gắn với phòng hoặc hội thoại.");
    }
}

/// <summary>
/// Sửa nội dung ghi chú nội bộ (Req 9.4). Void → <see cref="ICommandUseCase{TInput}"/> khai <see cref="PersistenceKey"/>.
/// Không tồn tại → <c>concierge_note_not_found</c>. Concurrency xmin (<c>IHasConcurrencyToken</c>) — hai người sửa đè →
/// base <c>ConcurrencyConflictException</c> → middleware 409 (CP15). Body PLAIN TEXT (trim). Cập nhật <c>UpdatedAt</c>.
/// </summary>
public sealed class UpdateInternalNoteUseCase : ICommandUseCase<UpdateInternalNoteInput>
{
    public string PersistenceKey => ConciergeModule.PersistenceKey;

    private readonly IRepository<InternalNote> _notes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateInternalNoteUseCase(IRepository<InternalNote> notes, IUnitOfWork unitOfWork, IClock clock)
    {
        _notes = notes;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> ExecuteAsync(UpdateInternalNoteInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var body = (input.Body ?? string.Empty).Trim();
        if (body.Length == 0)
        {
            return Result.Failure(ConciergeErrors.MessageEmpty);
        }

        var note = await _notes.FindByIdAsync(input.NoteId, ct).ConfigureAwait(false);
        if (note is null)
        {
            return Result.Failure(ConciergeErrors.NoteNotFound);
        }

        note.Body = body;
        note.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>Validate sửa ghi chú: có id + body không rỗng.</summary>
public sealed class UpdateInternalNoteValidator : AbstractValidator<UpdateInternalNoteInput>
{
    public UpdateInternalNoteValidator()
    {
        RuleFor(x => x.NoteId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty();
    }
}

/// <summary>
/// Xóa ghi chú nội bộ (Req 9.4). Void → <see cref="ICommandUseCase{TInput}"/>. Không tồn tại → <c>concierge_note_not_found</c>.
/// Ghi chú KHÔNG soft-delete (không nghiệp vụ lưu vết note đã xóa) → xóa cứng qua <see cref="IRepository{T}.Remove"/>.
/// </summary>
public sealed class DeleteInternalNoteUseCase : ICommandUseCase<DeleteInternalNoteInput>
{
    public string PersistenceKey => ConciergeModule.PersistenceKey;

    private readonly IRepository<InternalNote> _notes;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteInternalNoteUseCase(IRepository<InternalNote> notes, IUnitOfWork unitOfWork)
    {
        _notes = notes;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(DeleteInternalNoteInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var note = await _notes.FindByIdAsync(input.NoteId, ct).ConfigureAwait(false);
        if (note is null)
        {
            return Result.Failure(ConciergeErrors.NoteNotFound);
        }

        _notes.Remove(note);

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
