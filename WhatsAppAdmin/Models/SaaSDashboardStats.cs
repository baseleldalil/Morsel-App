namespace WhatsAppAdmin.Models
{
    public class SaaSDashboardStats
    {
        public int TotalUsers { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int TotalMessagesSent { get; set; }
        public int NewUsersThisMonth { get; set; }
        public decimal AverageRevenuePerUser { get; set; }
    }
}
