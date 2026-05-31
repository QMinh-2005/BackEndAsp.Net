using MyOwnLearning.Enums;

namespace MyOwnLearning.Helpers
{
    public static class VariantValidationHelper
    {
        public static string? ValidateAndMapStringAttribute(string? input, List<string> VariantAttributes)
        {
            if (string.IsNullOrWhiteSpace(input) || input == "N/A (Không áp dụng)")
            {
                return null;
            }

            string trimmedInput = input.Trim();
            if (!VariantAttributes.Contains(trimmedInput))
            {
                throw new Exception($"Giá trị '{trimmedInput}' không hợp lệ.");
            }
            return trimmedInput;
        }
        public static int? ValidateAndMapMaxTension(int? input)
        {
            if (!input.HasValue)
            {
                return null;
            }

            if (!VariantAttributes.MaxTension.Contains(input.Value))
            {
                throw new Exception($"Giá trị '{input}' không hợp lệ.");
            }

            return input;
        }
    }
}
