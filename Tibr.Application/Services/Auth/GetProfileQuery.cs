using MediatR;
using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;

namespace Tibr.Application.Services.Auth;

public record GetProfileQuery(long UserId) : IRequest<UserProfileDto>;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, UserProfileDto>
{
    private readonly DbContext _context;

    public GetProfileQueryHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Set<User>().FindAsync(new object[] { request.UserId }, cancellationToken);

        if (user is null)
            return new UserProfileDto();

        return new UserProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            KycStatus = user.KycStatus,
            CreatedAt = user.CreatedAt,
        };
    }
}
