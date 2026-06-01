using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities
{
    public class DeliveryRequest : BaseEntity<long>
    {
        public Guid UserId { get; set; }

        public AssetType AssetType { get; set; }

        public decimal Quantity { get; set; }

        public Guid AddressId { get; set; }

        public Address Address { get; set; } = default!;

        public DeliveryStatus Status { get; set; }

        public string? TrackingNumber { get; set; }
    }
}
