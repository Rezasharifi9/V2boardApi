using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot;

namespace V2boardApi.Tools
{
    public class BotUtility
    {
        TelegramBotClient botClient;
        string Token;
        public BotUtility(string token)
        {
            botClient = new TelegramBotClient(token);
            Token = token;
        }
        public async Task GetUserProfilePictureAsync(long userId)
        {
            // دریافت عکس‌های پروفایل کاربر
            var userProfilePhotos = await botClient.GetUserProfilePhotosAsync(userId);

            // بررسی اینکه آیا کاربر عکس پروفایل دارد یا خیر
            if (userProfilePhotos.TotalCount > 0)
            {
                // گرفتن اولین عکس (سایز کوچکترین تصویر)
                var photo = userProfilePhotos.Photos.First()[0];

                // دریافت فایل عکس
                var file = await botClient.GetFileAsync(photo.FileId);

                // ساخت URL برای دانلود عکس
                var fileUrl = $"https://api.telegram.org/file/bot{Token}/{file.FilePath}";

                // اینجا می‌توانید عکس را دانلود کنید یا از URL استفاده کنید
                Console.WriteLine($"Profile picture URL: {fileUrl}");
            }
            else
            {
                Console.WriteLine("User has no profile picture.");
            }
        }
    }
}