using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please give the category a name, e.g. Under 20 or 21km."), StringLength(50)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose Age or Distance."), RegularExpression("^(Age|Distance)$", ErrorMessage = "Category type must be Age or Distance.")]
    [Display(Name = "Category type")]
    public string CategoryType { get; set; } = "Age";
}

public class CategoriesPageViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EnrolmentsPageViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRowInput
{
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string EnrolmentStatus { get; set; } = string.Empty;

    [RegularExpression(@"^([01]?\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Use the format hh:mm:ss, e.g. 01:23:45.")]
    public string? FinishTime { get; set; }

    [Range(1, 1000000, ErrorMessage = "Position must be 1 or higher.")]
    public int? Position { get; set; }

    public bool Published { get; set; }
}

public class ResultsEntryViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public List<ResultRowInput> Rows { get; set; } = new();
}
