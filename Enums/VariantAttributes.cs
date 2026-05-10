namespace MyOwnLearning.Enums
{
    public class VariantAttributes
    {
        public static readonly List<string> WeightClasses = new()
        {
            "N/A (Không áp dụng)",
            "2U (90-94g)",
            "3U (85-89g)",
            "4U (80-84g)",
            "5U (<80g)",
        };
        public static readonly List<string> GripSizes = new()
        {
            "N/A (Không áp dụng)",
            "G2",
            "G3",
            "G4",
            "G5",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
        };
        public static readonly List<string> BalancePoints = new()
        {
            "N/A (Không áp dụng)",
            "Head Heavy",
            "Balanced",
            "Head Light"
        };
        public static readonly List<String> Stiffness = new()
        {
            "N/A (Không áp dụng)",
            "Flexible",
            "Medium",
            "Stiff"
        };
        public static readonly List<int> MaxTension = new()
        {
            10,
            11,
            12,
            13,
            14,
            15,
            16,
        };
    }
}
