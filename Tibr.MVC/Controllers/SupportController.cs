using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos.SupportDtos;
using Tibr.Application.Dtos.TicketDtos;
using Tibr.Application.Services.SupportServices;
using Tibr.Application.Services.TicketServices;
using Tibr.Application.Services.UserServices;
using Tibr.MVC.Models.Support;

namespace Tibr.MVC.Controllers
{

    public class SupportController : Controller
    {
        private readonly ISupportService _supportService;
        private readonly ITicketService _ticketService;
        private readonly IUserService _userService;
        private readonly ILogger<SupportController> _logger;

        // Replace with session/claims admin ID when auth team delivers login
        private const long TEMP_ADMIN_ID = 1L;

        public SupportController(
            ISupportService supportService,
            ITicketService ticketService,
            IUserService userService,
            ILogger<SupportController> logger)
        {
            _supportService = supportService;
            _ticketService = ticketService;
            _userService = userService;
            _logger = logger;
        }

        //  GET /Support 
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

            var supports = result.Data!;

            // ── Resolve customer names using existing IUserService ─
            // GetUsersAsync(null, null, null) loads all non-deleted users.
            // UserListItemDto.FullName = $"{FirstName} {LastName}".Trim()
            // — computed property, already exactly what we need.
            Dictionary<long, string> nameMap = [];

            var usersResult = await _userService.GetUsersAsync(
                searchQuery: null,
                statusFilter: null,
                kycStatusFilter: null);

            if (usersResult.IsSuccess)
            {
                nameMap = usersResult.Data!
                    .ToDictionary(u => u.Id, u => u.FullName);
            }
            // ─────────────────────────────────────────────────────

            var all = supports
                .Select(s => new SupportRowViewModel
                {
                    Id = s.Id,
                    TicketNumber = $"TK-{s.Id:D4}",
                    Subject = s.Subject,

                    // Look up the full name; fall back to ID only if user not found
                    CustomerName = nameMap.TryGetValue(s.UserId, out var name)
                                   && !string.IsNullOrWhiteSpace(name)
                        ? name
                        : $"Customer #{s.UserId}",

                    CustomerId = s.UserId,
                    Status = s.Status.ToString(),
                    CreatedAt = DateTime.UtcNow   // placeholder until SupportResponse exposes CreatedAt
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            // Search in-memory — list is small
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.ToLower();
                all = all.Where(s =>
                    s.Subject.ToLower().Contains(q) ||
                    s.CustomerName.ToLower().Contains(q) ||
                    s.TicketNumber.ToLower().Contains(q)).ToList();
            }

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

        // ── GET /Support/Details/5 ────────────────────────────────
        // GetSupportByIdAsync → GetSupportWithTicketsAsync already
        // includes User so UserFullName is populated here without
        // any extra call.
        public async Task<IActionResult> Details(long id)
        {
            var result = await _supportService.GetSupportByIdAsync(id);

            if (result.IsFailure || result.Data == null)
            {
                TempData["Error"] = "Ticket not found.";
                return RedirectToAction(nameof(Index));
            }

            var support = result.Data;
            var messages = new List<MessageViewModel>();

            // Customer's original message (Support.Subject)
            messages.Add(new MessageViewModel
            {
                Id = 0,
                Text = support.Subject,
                SenderName = string.IsNullOrWhiteSpace(support.UserFullName)
                    ? $"Customer #{support.UserId}"
                    : support.UserFullName,
                IsFromAdmin = false,
                SentAt = DateTime.UtcNow
            });

            // Admin replies (Ticket entities linked to this Support)
            if (support.Tickets != null)
            {
                messages.AddRange(support.Tickets
                    .OrderBy(t => t.CreatedAt)
                    .Select(t => new MessageViewModel
                    {
                        Id = t.Id,
                        Text = t.Message,
                        SenderName = "Admin",
                        IsFromAdmin = true,
                        SentAt = t.CreatedAt
                    }));
            }

            var vm = new SupportDetailViewModel
            {
                Id = support.Id,
                TicketNumber = $"TK-{support.Id:D4}",
                Subject = support.Subject,
                Status = support.Status.ToString(),
                CustomerName = string.IsNullOrWhiteSpace(support.UserFullName)
                    ? $"Customer #{support.UserId}"
                    : support.UserFullName,
                CustomerEmail = support.UserEmail,
                CustomerPhone = support.UserPhone,
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
        public async Task<IActionResult> Reply(ReplyViewModel Reply)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please enter a valid reply message (5–2000 characters).";
                return RedirectToAction(nameof(Details), new { id = Reply.SupportId });
            }

            var dto = new CreateTicketDto
            {
                SupportId = Reply.SupportId,
                Message = Reply.Message.Trim()
            };

            var result = await _ticketService.ReplyToTicketAsync(dto, TEMP_ADMIN_ID);

            if (result.IsSuccess)
            {
                TempData["Success"] = result.Data!.EmailSent
                    ? "Reply sent successfully and emailed to the customer."
                    : "Reply saved, but the email to the customer failed to send.";
            }
            else
            {
                TempData["Error"] = result.ErrorMessage;
            }

            return RedirectToAction(nameof(Details), new { id = Reply.SupportId });
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

        //  POST /Support/DeleteMessage 
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