using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Application.Dtos.TicketDtos;
using Tibr.Application.Services.SupportServices;
using Tibr.Application.Services.TicketServices;
using Tibr.MVC.Models.Support;

namespace Tibr.MVC.Controllers
{
    public class SupportController : Controller
    {
        private readonly ISupportService _supportService;
        private readonly ITicketService _ticketService;
        private readonly ILogger<SupportController> _logger;

        // Temporary hardcoded admin ID until auth team delivers session-based auth.
        // Replace with: long adminId = GetCurrentAdminId(); from session/claims.
        private const long TEMP_ADMIN_ID = 1L;

        public SupportController(
            ISupportService supportService,
            ITicketService ticketService,
            ILogger<SupportController> logger)
        {
            _supportService = supportService;
            _ticketService = ticketService;
            _logger = logger;
        }

        //  GET /Support 
        // Shows all tickets with stats strip, filter tabs, and search.
        public async Task<IActionResult> Index(
            string? status = null,
            string? search = null)
        {
            var result = await _supportService.GetAllSupportsAsync();

            if (result.IsFailure)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(new SupportListViewModel());
            }

            var all = result.Data!
                .Select(s => new SupportRowViewModel
                {
                    Id = s.Id,
                    TicketNumber = $"TK-{s.Id:D4}",
                    Subject = s.Subject,
                    // UserFullName is empty from GetAllAsync (no Include) — show ID fallback
                    CustomerName = string.IsNullOrEmpty(s.UserFullName)
                        ? $"Customer #{s.UserId}"
                        : s.UserFullName,
                    CustomerId = s.UserId,
                    Status = s.Status.ToString(),
                    CreatedAt = DateTime.UtcNow   // SupportResponse has no CreatedAt — see note below
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            // Apply search filter in memory (list is small — no SQL needed)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.ToLower();
                all = all.Where(s =>
                    s.Subject.ToLower().Contains(q) ||
                    s.CustomerName.ToLower().Contains(q) ||
                    s.TicketNumber.ToLower().Contains(q)).ToList();
            }

            // Quick stats
            var vm = new SupportListViewModel
            {
                SearchKeyword = search,
                StatusFilter = status,
                TotalAll = all.Count,
                TotalOpen = all.Count(s => s.Status == "Open"),
                TotalPending = all.Count(s => s.Status == "Pending"),
                TotalResolved = all.Count(s => s.Status == "Resolved"),
                TotalClosed = all.Count(s => s.Status == "Closed"),
                Tickets = all,
                Filtered = string.IsNullOrEmpty(status)
                    ? all
                    : all.Where(s => s.Status == status).ToList()
            };

            return View(vm);
        }

        //  GET /Support/Details/5 
        // Shows the full ticket thread with all admin replies.
        public async Task<IActionResult> Details(long id)
        {
            var result = await _supportService.GetSupportByIdAsync(id);

            if (result.IsFailure || result.Data == null)
            {
                TempData["Error"] = "Ticket not found.";
                return RedirectToAction(nameof(Index));
            }

            var support = result.Data;

            // Build the conversation thread.
            // Message 0 = original customer request (Subject field, no Ticket record).
            // Messages 1..N = admin replies (Ticket records).
            var messages = new List<MessageViewModel>();

            // Customer's original message
            messages.Add(new MessageViewModel
            {
                Id = 0,
                Text = support.Subject,
                SenderName = string.IsNullOrEmpty(support.UserFullName)
                    ? $"Customer #{support.UserId}"
                    : support.UserFullName,
                IsFromAdmin = false,
                SentAt = DateTime.UtcNow   // SupportResponse has no CreatedAt — placeholder
            });

            // TicketDto.CreatedAt is a string from the mapper.
            // We parse it here. SupportResponse does not expose Tickets in the list call,
            // but GetSupportByIdAsync uses GetSupportWithTicketsAsync which includes them.
            // However, SupportResponse itself only has basic fields —
            // Tickets are NOT in SupportResponse DTO. We need to access them differently.
            // Since ISupportService.GetSupportByIdAsync returns SupportResponse (not Support entity),
            // and SupportResponse has no Tickets collection, we can only show the subject line.
            // The correct fix is to extend SupportResponse with a Tickets list — but we cannot
            // change the Application layer here. So we note this limitation clearly.
            //
            // WORKAROUND: SupportResponse as currently defined only has:
            //   Id, UserId, UserFullName, Subject, Status
            // There is NO Tickets field. We show only the subject as customer message.
            // When the team extends SupportResponse.Tickets, add them here.

            var vm = new SupportDetailViewModel
            {
                Id = support.Id,
                TicketNumber = $"TK-{support.Id:D4}",
                Subject = support.Subject,
                Status = support.Status.ToString(),
                CustomerName = string.IsNullOrEmpty(support.UserFullName)
                    ? $"Customer #{support.UserId}"
                    : support.UserFullName,
                CustomerId = support.UserId,
                CreatedAt = DateTime.UtcNow,
                Messages = messages,
                Reply = new ReplyViewModel { SupportId = id }
            };

            return View(vm);
        }

        //  POST /Support/Reply 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(ReplyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please enter a valid reply message (5–2000 characters).";
                return RedirectToAction(nameof(Details), new { id = model.SupportId });
            }

            var dto = new CreateTicketDto
            {
                SupportId = model.SupportId,
                Message = model.Message.Trim()
            };

            // TEMP_ADMIN_ID: replace with session-based admin ID when auth is ready
            var result = await _ticketService.ReplyToTicketAsync(dto, TEMP_ADMIN_ID);

            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess
                    ? "Reply sent successfully."
                    : result.ErrorMessage;

            return RedirectToAction(nameof(Details), new { id = model.SupportId });
        }

        //  POST /Support/UpdateStatus 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(UpdateStatusViewModel model)
        {
            var dto = new UpdateSupportDto { Status = model.Status };
            var result = await _supportService.UpdateSupportAsync(model.SupportId, dto);

            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess
                    ? $"Ticket status updated to \"{model.Status}\"."
                    : result.ErrorMessage;

            return RedirectToAction(nameof(Details), new { id = model.SupportId });
        }

        //  POST /Support/Delete/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _supportService.DeleteSupportAsync(id);

            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess ? "Ticket deleted successfully." : result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }

        //  POST /Support/DeleteMessage/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(long ticketId, long supportId)
        {
            var result = await _ticketService.DeleteMessageAsync(ticketId);

            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess ? "Message deleted." : result.ErrorMessage;

            return RedirectToAction(nameof(Details), new { id = supportId });
        }
    }
}
