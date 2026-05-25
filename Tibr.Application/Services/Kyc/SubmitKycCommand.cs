using MediatR;
using Microsoft.EntityFrameworkCore;
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Microsoft.AspNetCore.Http;
namespace Tibr.Application.Services.Kyc
{
    public record SubmitKycCommand(
            long UserId,
            string DocumentType,
            string DocumentNumber,
            IFormFile DocumentFrontFile,
            IFormFile DocumentBackFile,
            IFormFile SelfieImageFile
        ) : IRequest<AuthResponse>;

    public class SubmitKycCommandHandler : IRequestHandler<SubmitKycCommand, AuthResponse>
    {
        private readonly DbContext _context;
        public SubmitKycCommandHandler(DbContext context) => _context = context;

        public async Task<AuthResponse> Handle(SubmitKycCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Set<User>().FindAsync(new object[] { (long)request.UserId }, cancellationToken);
            if (user == null) return new AuthResponse(false, "المستخدم غير موجود.", "User not found.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "kyc_documents");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var frontFileName = Guid.NewGuid() + Path.GetExtension(request.DocumentFrontFile.FileName);
            var backFileName = Guid.NewGuid() + Path.GetExtension(request.DocumentBackFile.FileName);
            var selfieFileName = Guid.NewGuid() + Path.GetExtension(request.SelfieImageFile.FileName);

            var frontPath = Path.Combine(uploadsFolder, frontFileName);
            var backPath = Path.Combine(uploadsFolder, backFileName);
            var selfiePath = Path.Combine(uploadsFolder, selfieFileName);

            using (var stream = new FileStream(frontPath, FileMode.Create)) await request.DocumentFrontFile.CopyToAsync(stream, cancellationToken);
            using (var stream = new FileStream(backPath, FileMode.Create)) await request.DocumentBackFile.CopyToAsync(stream, cancellationToken);
            using (var stream = new FileStream(selfiePath, FileMode.Create)) await request.SelfieImageFile.CopyToAsync(stream, cancellationToken);

            var kycDoc = new KYCDocument
            {
                UserId =(long)request.UserId,
                DocumentType = request.DocumentType,
                DocumentNumber = request.DocumentNumber,
                DocumentFront = "/kyc_documents/" + frontFileName,
                DocumentBack = "/kyc_documents/" + backFileName,
                SelfieImage = "/kyc_documents/" + selfieFileName,
                Status = "Pending",
                ReviewedBy = null
            };

            user.KycStatus = "Pending";

            await _context.Set<KYCDocument>().AddAsync(kycDoc, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new AuthResponse(true, "تم رفع مستندات التوثيق بنجاح وهي قيد المراجعة حاليًا من قبل الإدارة.", "The documentation documents have been successfully uploaded and are currently under review by management.");
        }
    }
}
