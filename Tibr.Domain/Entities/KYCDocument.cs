using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class KYCDocument : BaseEntity<long>
    {
        public long UserId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public string DocumentFront { get; set; } = string.Empty;
        public string DocumentBack { get; set; } = string.Empty;
        public string SelfieImage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long ReviewedBy { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Admin ReviewedByAdmin { get; set; } = null!;
    }
}
