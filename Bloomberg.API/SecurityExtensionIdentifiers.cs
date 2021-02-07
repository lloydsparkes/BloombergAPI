using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bloomberg.API
{
    /// <remarks>
    /// http://stackoverflow.com/questions/16140753/how-to-validate-a-international-securities-identification-number-isin-number/29170156#29170156
    /// </remarks>
    public static class SecurityIdentifierHelper
    {
        private static readonly Regex IsinPattern = new Regex("^(XS|AD|AE|AF|AG|AI|AL|AM|AO|AQ|AR|AS|AT|AU|AW|AX|AZ|BA|BB|BD|BE|BF|BG|BH|BI|BJ|BL|BM|BN|BO|BQ|BR|BS|BT|BV|BW|BY|BZ|CA|CC|CD|CF|CG|CH|CI|CK|CL|CM|CN|CO|CR|CU|CV|CW|CX|CY|CZ|DE|DJ|DK|DM|DO|DZ|EC|EE|EG|EH|ER|ES|ET|FI|FJ|FK|FM|FO|FR|GA|GB|GD|GE|GF|GG|GH|GI|GL|GM|GN|GP|GQ|GR|GS|GT|GU|GW|GY|HK|HM|HN|HR|HT|HU|ID|IE|IL|IM|IN|IO|IQ|IR|IS|IT|JE|JM|JO|JP|KE|KG|KH|KI|KM|KN|KP|KR|KW|KY|KZ|LA|LB|LC|LI|LK|LR|LS|LT|LU|LV|LY|MA|MC|MD|ME|MF|MG|MH|MK|ML|MM|MN|MO|MP|MQ|MR|MS|MT|MU|MV|MW|MX|MY|MZ|NA|NC|NE|NF|NG|NI|NL|NO|NP|NR|NU|NZ|OM|PA|PE|PF|PG|PH|PK|PL|PM|PN|PR|PS|PT|PW|PY|QA|RE|RO|RS|RU|RW|SA|SB|SC|SD|SE|SG|SH|SI|SJ|SK|SL|SM|SN|SO|SR|SS|ST|SV|SX|SY|SZ|TC|TD|TF|TG|TH|TJ|TK|TL|TM|TN|TO|TR|TT|TV|TW|TZ|UA|UG|UM|US|UY|UZ|VA|VC|VE|VG|VI|VN|VU|WF|WS|YE|YT|ZA|ZM|ZW)([0-9A-Z]{9})([0-9]{1})$", RegexOptions.Compiled);
        private static readonly Regex CusipPattern = new Regex("([A-Z0-9]){8}[0-9]", RegexOptions.Compiled);

        /// <summary>
        /// Test a string to see if it is an Isin
        /// </summary>
        public static bool IsIsin(this string isin)
        {
            if (isin.Length != 12)
            {
                return false;
            }
            if (string.IsNullOrEmpty(isin))
            {
                return false;
            }
            if (!IsinPattern.IsMatch(isin))
            {
                return false;
            }

            return IsChecksumCorrect(isin);
        }

        /// <summary>
        /// Test a string to see if it is a Cusip
        /// </summary>
        public static bool IsCusip(this string cusip)
        {
            if (cusip.Length != 9)
            {
                return false;
            }
            if (string.IsNullOrEmpty(cusip))
            {
                return false;
            }
            if (!CusipPattern.IsMatch(cusip))
            {
                return false;
            }

            return IsChecksumCorrect(cusip, true, true);
        }

        #region "Code from Stack Overflow"

        private static bool IsChecksumCorrect(string code, bool reverseLuhn = false, bool allowSymbols = false)
        {
            try
            {
                var checksum = code.Last().ToInt();
                return checksum == CalculateChecksum(code.Take(code.Length - 1), reverseLuhn, allowSymbols);
            }
            catch
            {
                return false;
            }
        }

        private static int CalculateChecksum(IEnumerable<char> cleanCode, bool reverseLuhn = false, bool allowSymbols = false)
        {
            return reverseLuhn
            ? cleanCode
                .Select((c, i) => c.OrdinalPosition(allowSymbols).ConditionalMultiplyByTwo(i.IsOdd()).SumDigits())
                .Sum()
                .TensComplement()
            : cleanCode
                .ToArray()
                .ToDigits(allowSymbols)
                .Select((d, i) => d.ConditionalMultiplyByTwo(i.IsEven()).SumDigits())
                .Sum()
                .TensComplement();
        }

        /// <summary>
        /// Be careful here. This method is probably inapropriate for anything other than its designed purpose of Luhn-algorithm based validation.
        /// Specifically:
        ///    - numbers are assigned a value equal to the number('0' == 0, '1' == 1).
        ///    - letters are assigned a value indicating the number 9 plus the letters ordinal position in the English alphabet('A' == 10, 'B' == 11).
        ///    - if symbols are allowed(eg: for CUSIP validation), they are assigned values beginning from 36 ('*' == 36, '@' == 37).
        /// </summary>
        /// <param name="c"></param>
        /// <param name="allowSymbols"></param>
        /// <returns></returns>
        private static int OrdinalPosition(this char c, bool allowSymbols = false)
        {
            if (char.IsLower(c))
                return char.ToUpper(c) - 'A' + 10;

            if (char.IsUpper(c))
                return c - 'A' + 10;

            if (char.IsDigit(c))
                return c.ToInt();

            if (allowSymbols)
                switch (c)
                {
                    case '*':
                        return 36;
                    case '@':
                        return 37;
                    case '#':
                        return 38;
                }
            throw new ArgumentOutOfRangeException(nameof(c), "Specified character is not a letter, digit or allowed symbol.");
        }

        private static IEnumerable<int> ToDigits(this char[] s, bool allowSymbols = false)
        {
            var digits = new List<int>();
            for (var i = s.Length - 1; i >= 0; i--)
            {
                var ordinalPosition = s[i].OrdinalPosition(allowSymbols);
                digits.Add(ordinalPosition % 10);
                if (ordinalPosition > 9)
                    digits.Add(ordinalPosition / 10);
            }
            return digits;
        }

        private static int ToInt(this char digit)
        {
            if (char.IsDigit(digit))
                return digit - '0';
            throw new ArgumentOutOfRangeException(nameof(digit), "Specified character is not a digit.");
        }

        private static bool IsEven(this int x)
        {
            return (x % 2 == 0);
        }

        private static bool IsOdd(this int x)
        {
            return !IsEven(x);
        }

        private static int SumDigits(this int value)
        {
            //return value > 9 ? ((value / 10) + (value % 10)) : value;
            return ((value / 10) + (value % 10));
        }

        private static int ConditionalMultiplyByTwo(this int value, bool condition)
        {
            return condition ? value * 2 : value;
        }

        private static int TensComplement(this int value)
        {
            return (10 - (value % 10)) % 10;
        }

        #endregion
    }
}
