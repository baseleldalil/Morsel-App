using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class UsageStatistic
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public DateTime Date { get; set; }

    public int MessagesSent { get; set; }

    public int MessagesDelivered { get; set; }

    public int MessagesFailed { get; set; }

    public decimal TotalCost { get; set; }

    public int ApiCallsMade { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}
