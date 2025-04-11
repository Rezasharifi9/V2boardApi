using DataLayer.DomainModel;
using DataLayer.Repository;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json.Linq;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        public static Dictionary<string, string> ToDictionary(object obj)
        {
            var dict = new Dictionary<string, string>();
            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                var value = prop.GetValue(obj);
                if (value != null)
                {
                    dict[prop.Name] = value.ToString();
                }
            }
            return dict;
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
        public static string ConvertDateTimeToShamsi(this DateTime dt)
        {
            return dt.ToString("yyyy MMMM dd", CultureInfo.GetCultureInfo("fa-IR")); ;
        }
        public static string ConvertDateTimeToMonthAndDay(this DateTime dt)
        {
            return dt.ToString("MM/dd", CultureInfo.GetCultureInfo("fa-IR")); ;
        }
        public static string ConvertDateTimeToShamsi2(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.GetCultureInfo("fa-IR")); ;
        }
        public static string ConvertDateTimeToShamsi3(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd", CultureInfo.GetCultureInfo("fa-IR")); ;
        }
        public static string ConvertDateTimeToShamsi4(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.GetCultureInfo("fa-IR")); ;
        }
        public static string ConvertDateTimeToShamsi5(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd", CultureInfo.GetCultureInfo("fa-IR")); ;
        }
        public static string GetTime(this DateTime dt)
        {
            return dt.ToString("HH:mm", CultureInfo.GetCultureInfo("fa-IR")); ;
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

        public static long ConvertGBToByte(long Byte)
        {

            var result = ((Byte * 1024) * 1024) * 1024;

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
        public static string Base64Encode(string text)
        {
            var textBytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(textBytes);
        }
        public static string Base64Decode(string base64)
        {
            var base64Bytes = System.Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(base64Bytes);
        }
        /// <summary>
        /// جدا جدا کردن سه رقم سه رقم برای پول
        /// </summary>
        /// <param name="mony"></param>
        /// <returns></returns>
        public static string ConvertToMony(this int mony)
        {
            // تبدیل عدد به رشته و اضافه کردن جداکننده
            string formattedNumber = String.Format("{0:#,0}", mony);

            return formattedNumber;
        }
        public static string ConvertToMony(this double mony)
        {
            // تبدیل عدد به رشته و اضافه کردن جداکننده
            string formattedNumber = String.Format("{0:#,0}", mony);

            return formattedNumber;
        }
        public static string ConvertToMony(this float mony)
        {
            // تبدیل عدد به رشته و اضافه کردن جداکننده
            string formattedNumber = String.Format("{0:#,0}", mony);

            return formattedNumber;
        }
        public static string ConvertToMony(this string mony)
        {
            // تبدیل عدد به رشته و اضافه کردن جداکننده
            string formattedNumber = String.Format("{0:#,0}", mony);

            return formattedNumber;
        }

        public static byte[] GenerateQRCode(string text)
        {
            var generator = new QRCodeGenerator();
            var qrCodeData = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCoder.QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);

            using (var stream = new MemoryStream())
            {
                qrCodeImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        public static PersianDayOfWeek PersionDayOfWeek(this DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Saturday:
                    return PersianDayOfWeek.Shanbe;
                case DayOfWeek.Sunday:
                    return PersianDayOfWeek.Yekshanbe;
                case DayOfWeek.Monday:
                    return PersianDayOfWeek.Doshanbe;
                case DayOfWeek.Tuesday:
                    return PersianDayOfWeek.Seshanbe;
                case DayOfWeek.Wednesday:
                    return PersianDayOfWeek.Charshanbe;
                case DayOfWeek.Thursday:
                    return PersianDayOfWeek.Panjshanbe;
                case DayOfWeek.Friday:
                    return PersianDayOfWeek.Jome;
                default:
                    throw new Exception();
            }
        }
        public enum PersianDayOfWeek
        {
            Shanbe = 0,
            Yekshanbe = 1,
            Doshanbe = 2,
            Seshanbe = 3,
            Charshanbe = 4,
            Panjshanbe = 5,
            Jome = 6
        }

        /// <summary>
        /// Convert Btye To GB Or MB
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static string ConvertToMbOrGb(this double volume)
        {
            //Data Type GB or MB
            string dataType = "GB";

            // Convert Byte To GB
            float data = (float)Math.Round((((volume / 1024) / 1024) / 1024), 2);

            // If Data Is MB True Convert To Mb
            if (data < 1)
            {
                data = (float)(data * 1024);
                dataType = "MB";
            }

            //Convert To String Object
            return data + dataType;
        }

        public static long ConvertGBToByte(this int GigaByte)
        {
            long result = ((long)GigaByte * 1024 * 1024 * 1024);

            return result;
        }


        public static long ConvertGBToByte(this double GigaByte)
        {

            var result = ((GigaByte * 1024) * 1024) * 1024;

            return Convert.ToInt64(result);
        }

        /// <summary>
        /// تبدیل میلی ثانیه به تاریخ شمسی
        /// </summary>
        /// <param name="data">میلی ثانیه</param>
        /// <returns>تاریخ شمسی</returns>
        public static string ConvertMillisecondToShamsiDate(this long data)
        {
            if (data != 0)
            {
                var date = DateTimeOffset.FromUnixTimeSeconds((long)data);
                PersianCalendar pc = new PersianCalendar();
                int year = pc.GetYear(date.DateTime);
                int month = pc.GetMonth(date.DateTime);
                int day = pc.GetDayOfMonth(date.DateTime);
                int hour = pc.GetHour(date.DateTime);
                int minute = pc.GetMinute(date.DateTime);
                int second = pc.GetSecond(date.DateTime);
                var PersianDate = year + "/" + month + "/" + day;

                return PersianDate;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// تبدیل میلی ثانیه به تاریخ شمسی
        /// </summary>
        /// <param name="data">میلی ثانیه</param>
        /// <returns>تاریخ شمسی</returns>
        public static string ConvertDatetimeToShamsiDate(this DateTime data)
        {
            if (data != default)
            {
                var date = data;
                PersianCalendar pc = new PersianCalendar();
                int year = pc.GetYear(date);
                int month = pc.GetMonth(date);
                int day = pc.GetDayOfMonth(date);
                int hour = pc.GetHour(date);
                int minute = pc.GetMinute(date);
                int second = pc.GetSecond(date);
                var PersianDate = year + "/" + month + "/" + day + " " + hour + ":" + minute + ":" + second;

                return PersianDate;
            }
            else
            {
                return "";
            }
        }


        public static async Task<string> GenerateQRCodeImageUrl(string text)
        {
            using (var httpClient = new HttpClient())
            {
                // Replace "http://api.qrserver.com/v1/create-qr-code/" with the desired QR code generation service
                var response = await httpClient.GetStringAsync($"http://api.qrserver.com/v1/create-qr-code/?data={text}");

                // Check if the response contains the QR code image URL
                if (response.Contains("http://chart.apis.google.com/chart"))
                {
                    // Extract the QR code image URL from the response
                    var startIndex = response.IndexOf("http://chart.apis.google.com/chart", StringComparison.Ordinal);
                    var endIndex = response.IndexOf("\"", startIndex, StringComparison.Ordinal);

                    // Check if both start and end indexes are valid
                    if (startIndex >= 0 && endIndex > startIndex)
                    {
                        return response.Substring(startIndex, endIndex - startIndex);
                    }
                }

                // If the response does not contain the expected URL, return an empty string
                return string.Empty;
            }
        }




        public static double? GetPriceUSDT()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://api.tetherland.com");
            var res = client.GetAsync("/currencies");
            if (res.Result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var model = JObject.Parse(res.Result.Content.ReadAsStringAsync().Result);
                var price = model["data"]["currencies"]["USDT"]["price"];

                return Convert.ToDouble(price);
            }
            else
            {
                return null;
            }
        }

        public static bool IsEnglishText(string input)
        {
            // الگوی منظم که فقط حروف انگلیسی، اعداد، فاصله و برخی کاراکترهای خاص را می‌پذیرد و از پذیرش @ جلوگیری می‌کند
            string pattern = @"^[a-zA-Z0-9\s.,?!]*$";

            // بررسی اینکه آیا ورودی مطابق با الگو هست یا نه
            return Regex.IsMatch(input, pattern);
        }

        public static bool IsPersian(string input)
        {
            // Check if the text contains any Persian characters
            return input.Any(c => (c >= '\u0600' && c <= '\u06FF') || (c >= '\u0750' && c <= '\u077F') || (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF'));
        }

        public static string GetTimeDifference(DateTime startDate, DateTime endDate)
        {
            TimeSpan timeDifference = endDate - startDate;

            if (timeDifference.TotalMinutes < 1)
            {
                return "الان";
            }
            else if (timeDifference.TotalMinutes < 60)
            {
                int minutes = (int)timeDifference.TotalMinutes;
                return $"{minutes} دقیقه قبل";
            }
            else if (timeDifference.TotalHours < 24)
            {
                int hours = (int)timeDifference.TotalHours;
                return $"{hours} ساعت قبل";
            }
            else if (timeDifference.TotalDays < 30)
            {
                int days = (int)timeDifference.TotalDays;
                return $"{days} روز قبل";
            }
            else if (timeDifference.TotalDays < 365)
            {
                int months = (int)(timeDifference.TotalDays / 30);
                return $"{months} ماه قبل";
            }
            else
            {
                int years = (int)(timeDifference.TotalDays / 365);
                return "خیلی وقت پیش";
            }
        }
    }
}