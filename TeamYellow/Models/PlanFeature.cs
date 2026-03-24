using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models;

/// <summary>
/// Represents a feature included in a subscription plan.
/// </summary>
[Table("PlanFeature")]
public class PlanFeature
{
    [Key]
    [Column("pkPlanFeatureId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PlanFeatureId { get; set; }

    [Column("featureName")]
    [Required, MaxLength(120)]
    public string FeatureName { get; set; } = string.Empty;

    [Column("featureDescription")]
    [Required, MaxLength(300)]
    public string FeatureDescription { get; set; } = string.Empty;

    [Column("sortOrder")]
    public int SortOrder { get; set; }

    [Column("fkPlanId")]
    public int PlanId { get; set; }

    public virtual Plan Plan { get; set; } = null!;
}
