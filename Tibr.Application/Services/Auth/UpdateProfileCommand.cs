using MediatR;
using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Services.Auth;

public record UpdateProfileCommand(long UserId, UpdateProfileDto Data) : IRequest<AuthResponse>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, AuthResponse>
{
    private readonly DbContext _context;

    public UpdateProfileCommandHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<AuthResponse> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Set<User>().FindAsync(new object[] { request.UserId }, cancellationToken);

        if (user is null)
            return new AuthResponse(false, "المستخدم غير موجود.", "User not found.");

        if (string.IsNullOrWhiteSpace(request.Data.FirstName) || string.IsNullOrWhiteSpace(request.Data.LastName))
            return new AuthResponse(false, "الاسم الأول واسم العائلة مطلوبان.", "First name and last name are required.");

        if (string.IsNullOrWhiteSpace(request.Data.Phone))
            return new AuthResponse(false, "رقم الهاتف مطلوب.", "Phone number is required.");

        user.FirstName = request.Data.FirstName;
        user.LastName = request.Data.LastName;
        user.Phone = request.Data.Phone;

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(true, "تم تحديث الملف الشخصي بنجاح.", "Profile updated successfully.");
    }
}
