using System;
using System.Text;

namespace AbdouSales.Services
{
    /// <summary>
    /// يحوّل مبلغاً رقمياً إلى كتابة بالحروف العربية، مع لاحقة "دينار جزائري" و"سنتيم".
    /// التحويل مبسّط ويغطي الاستعمال الشائع في الفواتير (حتى المليارات).
    /// </summary>
    public static class NumberToArabicWords
    {
        private static readonly string[] Ones =
        {
            "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة"
        };
        private static readonly string[] OnesFeminine =
        {
            "", "واحدة", "اثنتان", "ثلاث", "أربع", "خمس", "ست", "سبع", "ثمان", "تسع"
        };
        private static readonly string[] Teens =
        {
            "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر",
            "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر"
        };
        private static readonly string[] Tens =
        {
            "", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"
        };
        private static readonly string[] Hundreds =
        {
            "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة",
            "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"
        };

        public static string Convert(decimal amount, string mainUnitSingular = "دينار جزائري", string subUnitSingular = "سنتيم")
        {
            amount = Math.Abs(Math.Round(amount, 2));
            long integerPart = (long)Math.Floor(amount);
            int fraction = (int)Math.Round((amount - integerPart) * 100);

            string words = integerPart == 0 ? "صفر" : ConvertInteger(integerPart);
            var sb = new StringBuilder();
            sb.Append(words).Append(' ').Append(mainUnitSingular);

            if (fraction > 0)
            {
                sb.Append(" و").Append(ConvertInteger(fraction)).Append(' ').Append(subUnitSingular);
            }

            return sb.ToString();
        }

        private static string ConvertInteger(long number)
        {
            if (number == 0) return "صفر";
            if (number < 0) return "سالب " + ConvertInteger(-number);

            var groups = new (long Value, string Singular, string Dual, string Plural)[]
            {
                (1_000_000_000L, "مليار", "ملياران", "مليارات"),
                (1_000_000L,     "مليون", "مليونان", "ملايين"),
                (1_000L,         "ألف",   "ألفان",   "آلاف"),
            };

            var parts = new System.Collections.Generic.List<string>();
            long remaining = number;

            foreach (var (value, singular, dual, plural) in groups)
            {
                long count = remaining / value;
                remaining %= value;
                if (count <= 0) continue;

                if (count == 1)
                {
                    parts.Add(singular);
                }
                else if (count == 2)
                {
                    parts.Add(dual);
                }
                else if (count >= 3 && count <= 10)
                {
                    parts.Add(ConvertUnderThousand(count, feminine: true) + " " + plural);
                }
                else
                {
                    parts.Add(ConvertUnderThousand(count, feminine: false) + " " + singular);
                }
            }

            if (remaining > 0)
            {
                parts.Add(ConvertUnderThousand(remaining, feminine: false));
            }

            return string.Join(" و", parts);
        }

        private static string ConvertUnderThousand(long number, bool feminine)
        {
            if (number == 0) return "";
            var onesArr = feminine ? OnesFeminine : Ones;
            var sb = new StringBuilder();

            long hundreds = number / 100;
            long rest = number % 100;

            if (hundreds > 0)
            {
                sb.Append(Hundreds[hundreds]);
                if (rest > 0) sb.Append(" و");
            }

            if (rest >= 10 && rest <= 19)
            {
                sb.Append(Teens[rest - 10]);
            }
            else
            {
                long tens = rest / 10;
                long ones = rest % 10;

                if (tens > 0 && ones > 0)
                {
                    sb.Append(onesArr[ones]).Append(" و").Append(Tens[tens]);
                }
                else if (tens > 0)
                {
                    sb.Append(Tens[tens]);
                }
                else if (ones > 0)
                {
                    sb.Append(onesArr[ones]);
                }
            }

            return sb.ToString();
        }
    }
}
