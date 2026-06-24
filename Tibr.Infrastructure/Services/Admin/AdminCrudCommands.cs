using MediatR;
using Microsoft.EntityFrameworkCore;
using Tibr.Domain.Entities;

namespace Tibr.Application.Services.AdminManagement
{
    // Get all admins with pagination and filtering
    public record GetAllAdminsQuery(int PageNumber = 1, int PageSize = 10, string? SearchTerm = null, string SortBy = "Name", bool SortDescending = false)
        : IRequest<GetAllAdminsResult>;

    public record GetAllAdminsResult(List<AdminDto> Admins, int TotalCount);

    public record AdminDto(long Id, string Name, string Email, string Status, DateTime CreatedAt, DateTime? UpdatedAt);

    public class GetAllAdminsQueryHandler : IRequestHandler<GetAllAdminsQuery, GetAllAdminsResult>
    {
        private readonly DbContext _context;

        public GetAllAdminsQueryHandler(DbContext context)
        {
            _context = context;
        }

        public async Task<GetAllAdminsResult> Handle(GetAllAdminsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _context.Set<Domain.Entities.Admin>().AsQueryable();

                // Search filter
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    query = query.Where(a => a.Name.ToLower().Contains(searchTerm) || a.Email.ToLower().Contains(searchTerm));
                }

                // Total count before pagination
                var totalCount = await query.CountAsync(cancellationToken);

                // Sorting
                query = request.SortBy switch
                {
                    "Email" => request.SortDescending ? query.OrderByDescending(a => a.Email) : query.OrderBy(a => a.Email),
                    "Status" => request.SortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
                    _ => request.SortDescending ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
                };

                // Pagination
                var admins = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(a => new AdminDto(a.Id, a.Name, a.Email, a.Status, a.CreatedAt, a.UpdatedAt))
                    .ToListAsync(cancellationToken);

                return new GetAllAdminsResult(admins, totalCount);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving admins from database.", ex);
            }
        }
    }

    // Get single admin by ID
    public record GetAdminByIdQuery(long Id) : IRequest<AdminDto?>;

    public class GetAdminByIdQueryHandler : IRequestHandler<GetAdminByIdQuery, AdminDto?>
    {
        private readonly DbContext _context;

        public GetAdminByIdQueryHandler(DbContext context)
        {
            _context = context;
        }

        public async Task<AdminDto?> Handle(GetAdminByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var admin = await _context.Set<Domain.Entities.Admin>()
                    .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

                if (admin == null)
                    return null;

                return new AdminDto(admin.Id, admin.Name, admin.Email, admin.Status, admin.CreatedAt, admin.UpdatedAt);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving admin with ID {request.Id}.", ex);
            }
        }
    }

    // Create new admin
    public record CreateAdminCommand(string Name, string Email, string Status = "Active") : IRequest<AdminDto>;

    public class CreateAdminCommandHandler : IRequestHandler<CreateAdminCommand, AdminDto>
    {
        private readonly DbContext _context;

        public CreateAdminCommandHandler(DbContext context)
        {
            _context = context;
        }

        public async Task<AdminDto> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if email already exists
                var existingAdmin = await _context.Set<Domain.Entities.Admin>()
                    .FirstOrDefaultAsync(a => a.Email == request.Email, cancellationToken);

                if (existingAdmin != null)
                    throw new InvalidOperationException($"Admin with email '{request.Email}' already exists.");

                var admin = new Domain.Entities.Admin
                {
                    Name = request.Name,
                    Email = request.Email,
                    Status = request.Status,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Set<Domain.Entities.Admin>().AddAsync(admin, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                return new AdminDto(admin.Id, admin.Name, admin.Email, admin.Status, admin.CreatedAt, admin.UpdatedAt);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error creating admin.", ex);
            }
        }
    }

    // Update admin
    public record UpdateAdminCommand(long Id, string Name, string Email, string Status) : IRequest<AdminDto>;

    public class UpdateAdminCommandHandler : IRequestHandler<UpdateAdminCommand, AdminDto>
    {
        private readonly DbContext _context;

        public UpdateAdminCommandHandler(DbContext context)
        {
            _context = context;
        }

        public async Task<AdminDto> Handle(UpdateAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var admin = await _context.Set<Domain.Entities.Admin>()
                    .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

                if (admin == null)
                    throw new KeyNotFoundException($"Admin with ID {request.Id} not found.");

                // Check if email is being changed to an existing one
                if (admin.Email != request.Email)
                {
                    var existingAdmin = await _context.Set<Domain.Entities.Admin>()
                        .FirstOrDefaultAsync(a => a.Email == request.Email, cancellationToken);

                    if (existingAdmin != null)
                        throw new InvalidOperationException($"Admin with email '{request.Email}' already exists.");
                }

                admin.Name = request.Name;
                admin.Email = request.Email;
                admin.Status = request.Status;
                admin.UpdatedAt = DateTime.UtcNow;

                _context.Set<Domain.Entities.Admin>().Update(admin);
                await _context.SaveChangesAsync(cancellationToken);

                return new AdminDto(admin.Id, admin.Name, admin.Email, admin.Status, admin.CreatedAt, admin.UpdatedAt);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating admin with ID {request.Id}.", ex);
            }
        }
    }

    // Delete admin
    public record DeleteAdminCommand(long Id) : IRequest<bool>;

    public class DeleteAdminCommandHandler : IRequestHandler<DeleteAdminCommand, bool>
    {
        private readonly DbContext _context;

        public DeleteAdminCommandHandler(DbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var admin = await _context.Set<Domain.Entities.Admin>()
                    .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

                if (admin == null)
                    throw new KeyNotFoundException($"Admin with ID {request.Id} not found.");

                _context.Set<Domain.Entities.Admin>().Remove(admin);
                await _context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting admin with ID {request.Id}.", ex);
            }
        }
    }
}