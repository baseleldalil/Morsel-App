using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class SentPhone
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public int? CampaignId { get; set; }

    public DateTime SentAt { get; set; }

    public string? MessageContent { get; set; }

    public string Status { get; set; } = null!;
}
