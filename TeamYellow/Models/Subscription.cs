using System;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace TeamYellow.Models;

public class Subscription
{
    [Key]
    public int SubscriptionId { get; set; }
    public required bool IsActive { get; set; }
    public required DateTime CycleStart { get; set; }
    public required DateTime CycleEnd { get; set; }
    public required DateTime CreateAt { get; set; }
    public DateTime? UpdateAt { get; set; }

}
