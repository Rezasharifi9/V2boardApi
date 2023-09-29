using DataLayer.DomainModel;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace V2boardApi.Tools
{
    public static class Utility
    {
        public static string ToSha256(this string text)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            return hashString;
        }

        public static double ConvertDatetimeToSecond(this DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public static DateTime ConvertSecondToDatetime(double unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        public static string ConvertNumerals(this string input)
        {

            return input.Replace('0', '\u06f0')
                    .Replace('1', '\u06f1')
                    .Replace('2', '\u06f2')
                    .Replace('3', '\u06f3')
                    .Replace('4', '\u06f4')
                    .Replace('5', '\u06f5')
                    .Replace('6', '\u06f6')
                    .Replace('7', '\u06f7')
                    .Replace('8', '\u06f8')
                    .Replace('9', '\u06f9');

        }

        public static int CalculateLeftDayes(DateTime date)
        {
            DateTime today = DateTime.Today;
            DateTime next = date;

            if (next < today)
                return 0;

            int numDays = (next - today).Days;

            return numDays;
        }
        public static string ConvertDateTimeToShamsi(DateTime dt)
        {
            return dt.ToString("yyyy MMMM dd", CultureInfo.GetCultureInfo("fa-IR")); ;
        }

        public static string GetMonthName(DateTime date)
        {
            PersianCalendar pc = new PersianCalendar();
            var month = pc.GetMonth(date);

            switch (month)
            {
                case 1:
                    {
                        return "فروردین";
                    }
                case 2:
                    {
                        return "اردیبهشت";
                    }
                case 3:
                    {
                        return "خرداد";
                    }
                case 4:
                    {
                        return "تیر";
                    }
                case 5:
                    {
                        return "مرداد";
                    }
                case 6:
                    {
                        return "شهریور";
                    }
                case 7:
                    {
                        return "مهر";
                    }
                case 8:
                    {
                        return "آبان";
                    }
                case 9:
                    {
                        return "آذر";
                    }
                case 10:
                    {
                        return "دی";
                    }
                case 11:
                    {
                        return "بهمن";
                    }
                case 12:
                    {
                        return "اسفند";
                    }
            }
            return "";
        }

        public static double ConvertByteToGB(double Byte)
        {
            var result = ((Byte / 1024) / 1024) / 1024; 

            return result;
        }
        public static double ConvertByteToMG(double Byte)
        {
            var result = ((Byte / 1024) / 1024);

            return result;
        }
        public static double ConvertGBToMG(double GB)
        {
            var result = ((GB * 1024) * 1024);

            return result;
        }


        
    }
}