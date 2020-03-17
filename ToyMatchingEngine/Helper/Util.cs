using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using QuickFix.Fields;

namespace ToyMatchingEngine.Helper
{
    public static class Util
    {
        public static bool ValidateStringInteger(string num)
        {
            if (!ValidateStringDecimal(num))
            {
                return false;
            }
            decimal parsed = decimal.Parse(num, CultureInfo.InvariantCulture);
            if (Math.Floor(parsed) != parsed)
            {
                return false;
            }
            return true;
        }
        public static bool ValidateStringDecimal(string num)
        {
            bool dotFound = false;
            foreach(char c in num)
            {
                if(c == '.')
                {
                    if (dotFound)
                    {
                        return false;
                    }
                    dotFound = true;
                }
                else if(c > '9' || c < '0')
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ValidateSide(char side)
        {
            return !(side == Side.BUY || side == Side.SELL);
        }

        public static string GetTodayString()
        {
            DateTime today = DateTime.Today;
            string year = today.Year.ToString(CultureInfo.InvariantCulture);
            string month = today.Month.ToString(CultureInfo.InvariantCulture);
            string day = today.Day.ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < 4 - year.Length; i++)
            {
                year = "0" + year;
            }
            for (int i = 0; i < 2 - month.Length; i++)
            {
                month = "0" + month;
            }
            for (int i = 0; i < 2 - day.Length; i++)
            {
                day = "0" + day;
            }
            return year + month + day;
        }
    }
}
