using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tibr.Application.Dtos.SupportDtos
{
    public class CreateSupportRequestDto
    {

        [Required]
        public long? UserId { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = string.Empty;
    }
}
