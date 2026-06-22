using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Infrastructure.Services.Admin;
using Tibr.MVC.Models;

namespace Tibr.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IMediator mediator, IMapper mapper, ILogger<AdminController> logger)
        {
            _mediator = mediator;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: Admin/Index
        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string sortBy = "Name", bool sortDescending = false)
        {
            try
            {
                _logger.LogInformation("Retrieving admin list - Page: {PageNumber}, PageSize: {PageSize}, Search: {SearchTerm}", pageNumber, pageSize, searchTerm);

                var query = new GetAllAdminsQuery(pageNumber, pageSize, searchTerm, sortBy, sortDescending);
                var result = await _mediator.Send(query);

                var viewModel = new AdminListViewModel
                {
                    Admins = result.Admins.Select(a => new AdminViewModel
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Email = a.Email,
                        Status = a.Status,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt
                    }).ToList(),
                    TotalCount = result.TotalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin list");
                TempData["Error"] = "An error occurred while retrieving the admin list.";
                return View(new AdminListViewModel());
            }
        }

        // GET: Admin/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                _logger.LogInformation("Retrieving admin details for ID: {AdminId}", id);

                var query = new GetAdminByIdQuery(id);
                var admin = await _mediator.Send(query);

                if (admin == null)
                {
                    _logger.LogWarning("Admin with ID {AdminId} not found", id);
                    TempData["Error"] = "Admin not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new AdminViewModel
                {
                    Id = admin.Id,
                    Name = admin.Name,
                    Email = admin.Email,
                    Status = admin.Status,
                    CreatedAt = admin.CreatedAt,
                    UpdatedAt = admin.UpdatedAt
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin details for ID: {AdminId}", id);
                TempData["Error"] = "An error occurred while retrieving admin details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateAdminViewModel());
        }

        // POST: Admin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAdminViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for creating admin");
                    return View(model);
                }

                _logger.LogInformation("Creating new admin with email: {Email}", model.Email);

                var command = new CreateAdminCommand(model.Name, model.Email, model.Status);
                var result = await _mediator.Send(command);

                _logger.LogInformation("Admin created successfully with ID: {AdminId}", result.Id);
                TempData["Success"] = "Admin created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating admin");
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin");
                ModelState.AddModelError("", "An error occurred while creating the admin.");
                return View(model);
            }
        }

        // GET: Admin/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                _logger.LogInformation("Retrieving admin for edit with ID: {AdminId}", id);

                var query = new GetAdminByIdQuery(id);
                var admin = await _mediator.Send(query);

                if (admin == null)
                {
                    _logger.LogWarning("Admin with ID {AdminId} not found for edit", id);
                    TempData["Error"] = "Admin not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new EditAdminViewModel
                {
                    Id = admin.Id,
                    Name = admin.Name,
                    Email = admin.Email,
                    Status = admin.Status
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin for edit with ID: {AdminId}", id);
                TempData["Error"] = "An error occurred while retrieving the admin.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EditAdminViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    _logger.LogWarning("Admin ID mismatch during edit. Route ID: {RouteId}, Model ID: {ModelId}", id, model.Id);
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for editing admin with ID: {AdminId}", id);
                    return View(model);
                }

                _logger.LogInformation("Updating admin with ID: {AdminId}", id);

                var command = new UpdateAdminCommand(id, model.Name, model.Email, model.Status);
                var result = await _mediator.Send(command);

                _logger.LogInformation("Admin updated successfully with ID: {AdminId}", id);
                TempData["Success"] = "Admin updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Admin not found during edit with ID: {AdminId}", id);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error while editing admin");
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin with ID: {AdminId}", id);
                ModelState.AddModelError("", "An error occurred while updating the admin.");
                return View(model);
            }
        }

        // GET: Admin/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                _logger.LogInformation("Retrieving admin for delete with ID: {AdminId}", id);

                var query = new GetAdminByIdQuery(id);
                var admin = await _mediator.Send(query);

                if (admin == null)
                {
                    _logger.LogWarning("Admin with ID {AdminId} not found for delete", id);
                    TempData["Error"] = "Admin not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new DeleteAdminViewModel
                {
                    Id = admin.Id,
                    Name = admin.Name,
                    Email = admin.Email,
                    Status = admin.Status
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin for delete with ID: {AdminId}", id);
                TempData["Error"] = "An error occurred while retrieving the admin.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            try
            {
                _logger.LogInformation("Deleting admin with ID: {AdminId}", id);

                var command = new DeleteAdminCommand(id);
                await _mediator.Send(command);

                _logger.LogInformation("Admin deleted successfully with ID: {AdminId}", id);
                TempData["Success"] = "Admin deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Admin not found during delete with ID: {AdminId}", id);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting admin with ID: {AdminId}", id);
                TempData["Error"] = "An error occurred while deleting the admin.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}