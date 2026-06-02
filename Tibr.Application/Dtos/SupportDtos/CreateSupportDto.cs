using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Tibr.Domain.Entities.Support;

namespace Tibr.Application.Dtos.SupportDtos
{
    public class CreateSupportDto
    {

        [Required]
        public long? UserId { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;
        
    }
}
