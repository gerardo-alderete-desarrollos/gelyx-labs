namespace GelyxLabs.Application.Features.Auth.ForgotPassword;
using GelyxLabs.Application.Options;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Application.Abstractions.Token;
using GelyxLabs.Domain.Entities;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Application.Abstractions.Email;
using GelyxLabs.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailQueue _emailQueue;
    private readonly AppOptions _appOptions;

    public ForgotPasswordCommandHandler(
        UserManager<AppUser> userManager,
        IEmailQueue emailQueue,
        IOptions<AppOptions> appOptions)
    {
        _userManager = userManager;
        _emailQueue = emailQueue;
        _appOptions = appOptions.Value;
    }

    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(command.Email);
        
        if (user is null || user.IsDeleted || !user.IsActive)
        {
            return Result.Success();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token));
        var resetLink = $"{_appOptions.WebBaseUrl}/reset-password?email={Uri.EscapeDataString(command.Email)}&token={Uri.EscapeDataString(encodedToken)}";

        await _emailQueue.EnqueueAsync(
            EmailTemplates.PasswordReset(user.Email!, user.Locale, resetLink),
            cancellationToken);

        return Result.Success();
    }
}
