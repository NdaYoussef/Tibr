using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Tibr.Domain.Entities.Support;

namespace Tibr.Application.Dtos.SupportDtos
{
    public class UpdateSupportDto
    {
        [Required]
        public SupportStatus Status { get; set; }
    }
}
