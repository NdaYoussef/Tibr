using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tibr.Application.Dtos
{
    public class AddressDto
    {
        public long Id { get; set; }
        public string FullAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class CreateAddressDto
    {
        public long UserId { get; set; }
        public string City { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public bool IsDefault { get; set; }
    }
}
