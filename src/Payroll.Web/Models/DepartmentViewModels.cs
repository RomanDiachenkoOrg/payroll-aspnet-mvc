using System.ComponentModel.DataAnnotations;
using Payroll.Domain.Entities;

namespace Payroll.Web.Models;

public class DepartmentFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required.")]
    [StringLength(10, MinimumLength = 2)]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Code may only contain letters, digits and dashes.")]
    public string Code { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(20)]
    [Display(Name = "Cost center")]
    public string? CostCenter { get; set; }

    public static DepartmentFormViewModel FromEntity(Department d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Code = d.Code,
        Description = d.Description,
        CostCenter = d.CostCenter
    };

    public Department ToEntity() => new()
    {
        Id = Id,
        Name = Name,
        Code = Code,
        Description = Description,
        CostCenter = CostCenter
    };
}

public class DepartmentListItem
{
    public Department Department { get; set; } = default!;
    public int ActiveEmployees { get; set; }
}
