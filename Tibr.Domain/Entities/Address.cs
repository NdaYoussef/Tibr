using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities
{
    public class Address : BaseEntity<long>
    {
        public long UserId { get; set; }

        public string City { get; set; } = string.Empty;

        public string Area { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string Building { get; set; } = string.Empty;

        public string? PostalCode { get; set; }
    }
}
