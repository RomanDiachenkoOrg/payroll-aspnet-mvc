using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Domain.Services;
using Payroll.Web.Infrastructure;
using Payroll.Web.Models;

namespace Payroll.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class DepartmentsController : Controller
{
    private readonly IDepartmentService _departments;

    public DepartmentsController(IDepartmentService departments)
    {
        _departments = departments;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var departments = await _departments.GetAllAsync(ct);
        var counts = await _departments.GetHeadcountsAsync(ct);
        var items = departments.Select(d => new DepartmentListItem
        {
            Department = d,
            ActiveEmployees = counts.TryGetValue(d.Id, out var c) ? c : 0
        }).ToList();
        return View(items);
    }

    public IActionResult Create() => View(new DepartmentFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(DepartmentFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _departments.CreateAsync(model.ToEntity(), ct);
        if (!result.Succeeded)
        {
            ModelState.AddErrors(result);
            return View(model);
        }

        this.Success($"Department \"{result.Value!.Name}\" was created.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var department = await _departments.GetAsync(id, ct);
        if (department is null) return NotFound();
        return View(DepartmentFormViewModel.FromEntity(department));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, DepartmentFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var result = await _departments.UpdateAsync(model.ToEntity(), ct);
        if (!result.Succeeded)
        {
            ModelState.AddErrors(result);
            return View(model);
        }

        this.Success("Department was saved.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _departments.DeleteAsync(id, ct);
        if (result.Succeeded) this.Success("Department was deleted.");
        else this.Error(result.ErrorMessage);
        return RedirectToAction(nameof(Index));
    }
}
