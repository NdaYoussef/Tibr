using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.SupportServices
{
    public interface ISupportService
    {
            Task<Result<string>> AddSupportAsync(CreateSupportDto createSupportRequestDto0);
            Task<Result<string>> UpdateSupportAsync(UpdateSupportDto updateSupportDto);
            Task<Result<string>> DeleteSupportAsync(long id);
            Task<Result<SupportResponse>> GetSupportByIdAsync(long id);
            Task<Result<List<SupportResponse>>> GetAllSupportsAsync();
    }
}
