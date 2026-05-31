using Mapster;
using MapsterMapper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Application.Services.SupportServices;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.SuppoertServices
{
    public class SupportService : ISupportService
    {
        private readonly ISupportRepository _supportRepository;
        private readonly IMapper _mapper;
       
        public SupportService(ISupportRepository supportRepository, IMapper mapper)
        {
            _supportRepository = supportRepository;
            _mapper = mapper;
        }
        public async Task<Result<string>> AddSupportAsync(CreateSupportRequestDto createSupportRequestDto0)
        {
            var support = _mapper.Map<Support>(createSupportRequestDto0);
            await _supportRepository.AddAsync(support);
            
            var result = await _supportRepository.SaveChangesAsync();
            if (result <= 0)
            {
                return Result<string>.Failure("Failed to add support request.");
            }

            return "Support request added successfully";
        }

        public async Task<Result<string>> DeleteSupportAsync(long id)
        {
            var support = await _supportRepository.GetById(id);
            if (support == null)
            {
                return Result<string>.Failure("Support request not found.");
            }

            await _supportRepository.DeleteAsync(support);
            var result = await _supportRepository.SaveChangesAsync();
            if (result <= 0)
            {
                return Result<string>.Failure("Failed to delete support request.");
            }

            return Result<string>.Success("Support request deleted successfully.");
        }

        public async Task<Result<List<SupportResponse>>> GetAllSupportsAsync()
        {
            var supportsEntities = await _supportRepository.GetAllAsync();

            var supportResponses = supportsEntities.Adapt<List<SupportResponse>>();

            return Result<List<SupportResponse>>.Success(supportResponses);
        }

        public async Task<Result<SupportResponse>> GetSupportByIdAsync(long id)
        {
            var support = await _supportRepository.GetById(id);
            if(support == null)
            {
                return Result<SupportResponse>.Failure("Support request not found.");
            }

            var supportResponse = support.Adapt<SupportResponse>();
            return Result<SupportResponse>.Success(supportResponse);
        }

        public async Task<Result<string>> UpdateSupportAsync(UpdateSupportDto updateSupportDto)
        {
            if(updateSupportDto == null)
            {
                return Result<string>.Failure("Invalid support request data.");
            }
            var support = _mapper.Map<Support>(updateSupportDto);
            
            await _supportRepository.UpdateAsync(support);
            
            var result = await _supportRepository.SaveChangesAsync();
            if (result <= 0)
            {
                return Result<string>.Failure("Failed to update support request.");
            }

            return Result<string>.Success("Support request updated successfully.");
        }
    }
}
