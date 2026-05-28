using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Application.Services.SupportServices;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.SuppoertServices
{
    public class SupportService : ISupportService
    {

        public SupportService()
        {
            
        }
        public Task<Result> AddSupportAsync(CreateSupportRequestDto createSupportRequestDto0)
        {
            throw new NotImplementedException();
        }

        public Task<Result> DeleteSupportAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<Result<List<SupportResponse>>> GetAllSupportsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Result<SupportResponse>> GetSupportByIdAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<Result> UpdateSupportAsync(UpdateSupportDto updateSupportDto)
        {
            throw new NotImplementedException();
        }
    }
}
