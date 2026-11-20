namespace GelyxLabs.Application.Features.Leads;

using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Domain.Common;
using GelyxLabs.Domain.Entities;

public sealed class CreateLeadHandler : ICommandHandler<CreateLeadCommand, LeadDto>
{
    private readonly ILeadsRepository _repository;
    private readonly IAuditLogger _auditLogger;
    public CreateLeadHandler(ILeadsRepository repository, IAuditLogger auditLogger)
    {
        _repository = repository;
        _auditLogger = auditLogger;
    }
    public async Task<Result<LeadDto>> Handle(CreateLeadCommand command, CancellationToken cancellationToken)
    {
        var lead = new Lead
        {
            Name = command.Name.Trim(),
            Email = command.Email.Trim(),
            Phone = command.Phone,
            Company = command.Company,
            Service = command.Service,
            Budget = command.Budget,
            Message = command.Message.Trim(),
            Status = "new"
        };
        var dto = await _repository.AddAsync(lead, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync("lead.created", "lead", lead.Id.ToString(), new Dictionary<string, object?> { ["leadId"] = lead.Id, ["email"] = lead.Email }, cancellationToken);
        return Result.Success(dto);
    }
}

public sealed class GetLeadsHandler : IQueryHandler<GetLeadsQuery, IReadOnlyList<LeadDto>>
{
    private readonly ILeadsRepository _repository;
    public GetLeadsHandler(ILeadsRepository repository) => _repository = repository;
    public async Task<Result<IReadOnlyList<LeadDto>>> Handle(GetLeadsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await _repository.GetLeadsAsync(query.Status, cancellationToken));
}

public sealed class GetLeadByIdHandler : IQueryHandler<GetLeadByIdQuery, LeadDto>
{
    private readonly ILeadsRepository _repository;
    public GetLeadByIdHandler(ILeadsRepository repository) => _repository = repository;
    public async Task<Result<LeadDto>> Handle(GetLeadByIdQuery query, CancellationToken cancellationToken)
    {
        var lead = await _repository.GetLeadAsync(query.Id, cancellationToken);
        return lead is null ? Result.Failure<LeadDto>(LeadsError.NotFound) : Result.Success(lead);
    }
}

public sealed class UpdateLeadHandler : ICommandHandler<UpdateLeadCommand, LeadDto>
{
    private readonly ILeadsRepository _repository;
    private readonly IAuditLogger _auditLogger;
    public UpdateLeadHandler(ILeadsRepository repository, IAuditLogger auditLogger)
    {
        _repository = repository;
        _auditLogger = auditLogger;
    }
    public async Task<Result<LeadDto>> Handle(UpdateLeadCommand command, CancellationToken cancellationToken)
    {
        var lead = await _repository.FindByIdAsync(command.Id, cancellationToken);
        if (lead is null) return Result.Failure<LeadDto>(LeadsError.NotFound);
        lead.Status = command.Status;
        lead.Notes = command.Notes;
        if (command.Status != "new" && lead.ContactedAt is null) lead.ContactedAt = DateTimeOffset.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync("lead.updated", "lead", lead.Id.ToString(), new Dictionary<string, object?> { ["leadId"] = lead.Id, ["status"] = lead.Status }, cancellationToken);
        var dto = await _repository.GetLeadAsync(lead.Id, cancellationToken)
            ?? throw new InvalidOperationException("The updated lead could not be loaded.");
        return Result.Success(dto);
    }
}

public sealed class DeleteLeadHandler : ICommandHandler<DeleteLeadCommand>
{
    private readonly ILeadsRepository _repository;
    private readonly IAuditLogger _auditLogger;
    public DeleteLeadHandler(ILeadsRepository repository, IAuditLogger auditLogger)
    {
        _repository = repository;
        _auditLogger = auditLogger;
    }
    public async Task<Result> Handle(DeleteLeadCommand command, CancellationToken cancellationToken)
    {
        var lead = await _repository.FindByIdAsync(command.Id, cancellationToken);
        if (lead is null) return Result.Failure(LeadsError.NotFound);
        await _repository.DeleteAsync(lead, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync("lead.deleted", "lead", command.Id.ToString(), new Dictionary<string, object?> { ["leadId"] = command.Id }, cancellationToken);
        return Result.Success();
    }
}

public sealed class RespondLeadHandler : ICommandHandler<RespondLeadCommand, LeadDto>
{
    private readonly ILeadsRepository _repository;
    private readonly IEmailSender _emailSender;
    private readonly IAuditLogger _auditLogger;
    public RespondLeadHandler(ILeadsRepository repository, IEmailSender emailSender, IAuditLogger auditLogger)
    {
        _repository = repository;
        _emailSender = emailSender;
        _auditLogger = auditLogger;
    }
    public async Task<Result<LeadDto>> Handle(RespondLeadCommand command, CancellationToken cancellationToken)
    {
        var lead = await _repository.FindByIdAsync(command.Id, cancellationToken);
        if (lead is null) return Result.Failure<LeadDto>(LeadsError.NotFound);

        if (command.Channel == "email")
        {
            var subject = command.Subject ?? $"Cotización GELYX Labs - {lead.Service ?? "desarrollo"}";
            var body = command.Body ?? $"Hola {lead.Name},<br/><br/>Gracias por contactarnos. Adjuntamos nuestra propuesta.<br/><br/>Saludos,<br/>GELYX Labs";
            await _emailSender.SendAsync(new EmailMessage(lead.Email, subject, body, "es"), cancellationToken);
        }

        lead.Status = "responded";
        lead.RespondedAt = DateTimeOffset.UtcNow;
        lead.ResponseChannel = command.Channel;
        if (lead.ContactedAt is null) lead.ContactedAt = DateTimeOffset.UtcNow;
        await _repository.SaveChangesAsync(cancellationToken);
        await _auditLogger.LogAsync("lead.responded", "lead", lead.Id.ToString(), new Dictionary<string, object?> { ["leadId"] = lead.Id, ["channel"] = command.Channel }, cancellationToken);
        var dto = await _repository.GetLeadAsync(lead.Id, cancellationToken)
            ?? throw new InvalidOperationException("The responded lead could not be loaded.");
        return Result.Success(dto);
    }
}