namespace ERP.Common.Infrastructure.Reporting;

public static class NumberToWordsConverter
{
    private static readonly string[] UnitsMap =
    {
        "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
    };

    private static readonly string[] TensMap =
    {
        "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    };

    public static string ConvertToWords(decimal amount, string currencyName = "BDT")
    {
        if (amount == 0)
            return $"Zero {currencyName} Only";

        var integerPart = (long)Math.Floor(amount);
        var decimalPart = (int)Math.Round((amount - integerPart) * 100);

        var words = NumberToWordsHelper(integerPart).Trim();
        words = $"{words} {currencyName}";

        if (decimalPart > 0)
        {
            var paisaWords = NumberToWordsHelper(decimalPart).Trim();
            words += $" and {paisaWords} Paisa";
        }

        return $"{words} Only";
    }

    private static string NumberToWordsHelper(long number)
    {
        if (number == 0)
            return "Zero";

        if (number < 0)
            return "Minus " + NumberToWordsHelper(Math.Abs(number));

        var words = "";

        if (number / 10000000 > 0)
        {
            words += NumberToWordsHelper(number / 10000000) + " Crore ";
            number %= 10000000;
        }

        if (number / 100000 > 0)
        {
            words += NumberToWordsHelper(number / 100000) + " Lakh ";
            number %= 100000;
        }

        if (number / 1000 > 0)
        {
            words += NumberToWordsHelper(number / 1000) + " Thousand ";
            number %= 1000;
        }

        if (number / 100 > 0)
        {
            words += NumberToWordsHelper(number / 100) + " Hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            if (words != "")
                words += "and ";

            if (number < 20)
                words += UnitsMap[number];
            else
            {
                words += TensMap[number / 10];
                if (number % 10 > 0)
                    words += "-" + UnitsMap[number % 10];
            }
        }

        return words;
    }
}
