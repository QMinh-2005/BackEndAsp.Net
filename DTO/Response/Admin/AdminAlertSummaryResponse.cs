namespace MyOwnLearning.DTO.Response.Admin
{
    public class AdminAlertSummaryResponse
    {
        public int PendingOrders { get; set; }
        public int PendingPayments { get; set; }
        public int LowStockVariants { get; set; }
        public int OutOfStockVariants { get; set; }
        public int DefectiveSerials { get; set; }
        public int NewLowRatingReviews { get; set; }
        public int HiddenReviews { get; set; }
        public int ExpiringVouchers { get; set; }
        public int AlmostUsedUpVouchers { get; set; }
        public int OpenServiceTickets { get; set; }
    }
}
