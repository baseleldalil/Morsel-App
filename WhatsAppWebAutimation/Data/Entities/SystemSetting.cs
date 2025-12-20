using System;
using System.Collections.Generic;

namespace WhatsAppWebAutomation.Data.Entities;

public partial class SystemSetting
{
    public int Id { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string? Description { get; set; }

    public string Category { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
