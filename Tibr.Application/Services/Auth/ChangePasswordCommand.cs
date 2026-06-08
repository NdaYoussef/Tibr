using MediatR;
using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Services.Auth;

public record ChangePasswordCommand(long UserId, ChangePasswordDto Data) : IRequest<AuthResponse>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, AuthResponse>
{
    private readonly DbContext _context;

    public ChangePasswordCommandHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<AuthResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Set<User>().FindAsync(new object[] { request.UserId }, cancellationToken);

        if (user is null)
            return new AuthResponse(false, "المستخدم غير موجود.", "User not found.");

        if (request.Data.NewPassword != request.Data.ConfirmNewPassword)
            return new AuthResponse(false, "كلمة المرور الجديدة وتأكيدها غير متطابقين.", "New password and confirmation do not match.");

        if (!BCrypt.Net.BCrypt.Verify(request.Data.OldPassword, user.Password))
            return new AuthResponse(false, "كلمة المرور القديمة غير صحيحة.", "Old password is incorrect.");

        if (request.Data.OldPassword == request.Data.NewPassword)
            return new AuthResponse(false, "كلمة المرور الجديدة يجب أن تختلف عن القديمة.", "New password must differ from the old one.");

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.Data.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(true, "تم تغيير كلمة المرور بنجاح.", "Password changed successfully.");
    }
}
