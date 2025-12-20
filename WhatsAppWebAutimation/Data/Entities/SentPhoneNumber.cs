using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class SentPhoneNumber
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public DateTime FirstSentAt { get; set; }

    public DateTime LastSentAt { get; set; }

    public int SendCount { get; set; }

    public int? LastCampaignId { get; set; }

    public string? LastStatus { get; set; }
}
