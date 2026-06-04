using Mapster;
using MapsterMapper;
using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Application.Dtos.TicketDtos;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;

namespace Tibr.Application.Services.TicketServices
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ISupportRepository _supportRepository; 
        private readonly IMapper _mapper;

        public TicketService(ITicketRepository ticketRepository, ISupportRepository supportRepository, IMapper mapper)
        {
            _ticketRepository = ticketRepository;
            _supportRepository = supportRepository;
            _mapper = mapper;
        }

        public async Task<Result<string>> DeleteMessageAsync(long ticketId)
        {
            var ticketMessage = await _ticketRepository.GetById(ticketId);
            if (ticketMessage == null)
            {
                return Result<string>.Failure("Message not found.");
            }

            await _ticketRepository.DeleteAsync(ticketMessage);

            var result = await _ticketRepository.SaveChangesAsync();
            if (result <= 0)
            {
                return Result<string>.Failure("Failed to delete the message.");
            }

            return Result<string>.Success("Message deleted successfully.");
        }

        public async Task<Result<TicketDto>> ReplyToTicketAsync(CreateTicketDto dto, long adminId)
        {
            var support = await _supportRepository.GetById(dto.SupportId);
            if (support == null)
            {
                return Result<TicketDto>.Failure("Support ticket not found.");
            }

          
            if (support.Status == Support.SupportStatus.Closed)
            {
                return Result<TicketDto>.Failure("Cannot reply to a closed support ticket.");
            }

            
            var ticketMessage = _mapper.Map<Ticket>(dto);
            ticketMessage.AdminId = adminId;
            ticketMessage.CreatedAt = DateTime.UtcNow;

          
            //if (adminId.HasValue)
            //{
            //    support.Status = Support.SupportStatus.Pending; 
            //}
            //else
            //{
            //    support.Status = Support.SupportStatus.Open; 
            //}

          
            await _ticketRepository.AddAsync(ticketMessage);
            await _supportRepository.UpdateAsync(support); 

            var result = await _ticketRepository.SaveChangesAsync();
            if (result <= 0)
            {
                return Result<TicketDto>.Failure("Failed to send your reply.");
            }

          
            var responseDto = ticketMessage.Adapt<TicketDto>();
            return Result<TicketDto>.Success(responseDto);
        }
    }
}
