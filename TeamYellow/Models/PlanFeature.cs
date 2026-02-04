using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models
{
    [Table("PlanFeature")]
    public class PlanFeature
    {   
        [Key]
        [Column("pkPlanFeatureId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlanFeatureId { get; set; }

        [Column("featureName")]
        [Required, MaxLength(120)]
        public string FeatureName { get; set; } = String.Empty;


        [Column("featureDescription")]
        [Required, MaxLength(300)]
        public string FeatureDescription { get; set; } = String.Empty;
        
        [Column("sortOrder")]
        public int sortOrder { get; set; }

       
        [Column("fkPlanId")]
        public int PlanId { get; set; }
        public Plan Plan { get; set; } = null!;
    }
}
