using System;
using System.ComponentModel.DataAnnotations;

namespace TeamYellow.Models;

public class Subscription
{
    public int PlanFeaturedId;

    [MaxLength(120)]
    public required string featureName;

    [MaxLength(300)]
    public required string featureDescription;

}
