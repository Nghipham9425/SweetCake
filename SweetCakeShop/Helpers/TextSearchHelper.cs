using System.Globalization;
using System.Text;

namespace SweetCakeShop.Helpers
{
    public static class TextSearchHelper
    {
        public static bool ContainsNormalized(string? source, string? keyword)
        {
            var normalizedSource = NormalizeForSearch(source);
            var normalizedKeyword = NormalizeForSearch(keyword);

            return normalizedKeyword.Length == 0
                || normalizedSource.Contains(normalizedKeyword, StringComparison.Ordinal);
        }

        public static string NormalizeForSearch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                    continue;

                builder.Append(c switch
                {
                    'đ' or 'Đ' => 'd',
                    _ => char.ToLowerInvariant(c)
                });
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
