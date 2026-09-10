using System.Globalization;
using System.Text;

namespace ElectricalBilling.Helpers
{
    /// <summary>
    /// Converts a rupee amount into Indian-currency words using the Indian
    /// numbering system (Thousand / Lakh / Crore), e.g. 5900 -> "Rupees Five
    /// Thousand Nine Hundred Only". Used on the invoice details screen and,
    /// from Phase 4 onward, on the printed PDF.
    /// </summary>
    public static class AmountInWordsConverter
    {
        private static readonly string[] Ones =
        {
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
            "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
            "Seventeen", "Eighteen", "Nineteen"
        };

        private static readonly string[] Tens =
        {
            "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
        };

        public static string Convert(decimal amount)
        {
            if (amount < 0)
            {
                return "Minus " + Convert(-amount);
            }

            var rupees = (long)Math.Floor(amount);
            var paise = (int)Math.Round((amount - rupees) * 100, MidpointRounding.AwayFromZero);

            var words = new StringBuilder("Rupees ");
            words.Append(rupees == 0 ? "Zero" : ConvertIndianGroup(rupees));

            if (paise > 0)
            {
                words.Append(" and ").Append(ConvertUpToTwoDigits(paise)).Append(" Paise");
            }

            words.Append(" Only");
            return words.ToString();
        }

        private static string ConvertIndianGroup(long number)
        {
            var segments = new List<string>();
            var divisors = new (long Divisor, string Name)[]
            {
                (10000000, "Crore"),
                (100000, "Lakh"),
                (1000, "Thousand"),
                (100, "Hundred")
            };

            foreach (var (divisor, name) in divisors)
            {
                var value = number / divisor;
                number %= divisor;
                if (value > 0)
                {
                    segments.Add($"{ConvertUpToTwoDigits(value)} {name}");
                }
            }

            if (number > 0)
            {
                segments.Add(ConvertUpToTwoDigits(number));
            }

            return string.Join(" ", segments);
        }

        private static string ConvertUpToTwoDigits(long n)
        {
            if (n <= 0) return "";
            if (n < 20) return Ones[n];
            if (n < 100) return (Tens[n / 10] + (n % 10 > 0 ? " " + Ones[n % 10] : "")).Trim();

            // A business invoice should never reach a group this large; fall
            // back to plain digits rather than throw.
            return n.ToString(CultureInfo.InvariantCulture);
        }
    }
}
