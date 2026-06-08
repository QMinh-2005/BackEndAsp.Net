namespace MyOwnLearning.Enums
{
    public static class ReturnRequestConstants
    {
        public const string StatusPending = "Chờ xử lý";
        public const string StatusApproved = "Đã chấp thuận";
        public const string StatusRejected = "Đã từ chối";
        public const string StatusRefunded = "Đã hoàn tiền";

        public const string MainDamagedOrDefective = "Đã nhận được hàng nhưng hàng bị vỡ, lỗi.";
        public const string MainWrongOrNotAsDescribed = "Đã nhận hàng nhưng hàng không đúng mẫu mã, mô tả.";
        public const string MainNotReceivedOrMissing = "Chưa nhận được hàng hoặc nhận thiếu hàng";

        public const string DetailBroken = "Hàng bể vỡ";
        public const string DetailDefective = "Hàng lỗi, không hoạt động";
        public const string DetailWrongItem = "Người bán gửi sai hàng";
        public const string DetailNotAsDescribed = "Khác với mô tả";
        public const string DetailUsedItem = "Hàng đã qua sử dụng";
        public const string DetailFakeItem = "Hàng giả, nhái";
        public const string DetailNotReceived = "Chưa nhận được hàng";
        public const string DetailMissingItem = "Thiếu hàng";
        public const string DetailEmptyPackage = "Thùng hàng rỗng";

        public static readonly HashSet<string> FinalStatuses = new()
        {
            StatusApproved,
            StatusRejected,
            StatusRefunded
        };

        public static bool RequiresCustomerEvidence(string detailReason)
        {
            return !string.Equals(detailReason, DetailNotReceived, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValidStatus(string status)
        {
            return string.Equals(status, StatusPending, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, StatusApproved, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, StatusRejected, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, StatusRefunded, StringComparison.OrdinalIgnoreCase);
        }
    }
}
