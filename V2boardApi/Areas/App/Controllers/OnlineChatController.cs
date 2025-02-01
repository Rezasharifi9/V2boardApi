using DataLayer;
using DataLayer.DomainModel;
using DataLayer.Repository;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Telegram.Bot.Types;
using V2boardApi.Models.OnlineChatModel;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    [AuthorizeApp(Roles = "1,2,3,4")]
    public class OnlineChatController : Controller
    {
        private readonly IHubContext<ChatHub> _chatHubContext;
        private Repository<tbHistoryChats> HistoryChatRepository;
        private Repository<tbUsers> UsersRepository;
        public OnlineChatController(IHubContext<ChatHub> chatHubContext)
        {
            _chatHubContext = chatHubContext;

        }
        public OnlineChatController()
        {
            HistoryChatRepository = new Repository<tbHistoryChats>();
            UsersRepository = new Repository<tbUsers>();
        }


        // GET: App/OnlineChat
        public ActionResult Index()
        {

            return View();
        }

        public ActionResult GetChatUsers()
        {
            var userId = Convert.ToInt32(JwtToken.GetUser_ID());
    
            var Role = JwtToken.GetUserRole();

            var UserMe = UsersRepository.Where(s => s.User_ID == userId).FirstOrDefault();

            var users = new List<tbUsers>();
            if (Role == "2")
            {
                users = UsersRepository.Where(s => s.User_ID == UserMe.Parent_ID).ToList();
            }
            else if (Role == "1" || Role == "3" || Role == "4")
            {

                users = UsersRepository.Where(s => s.Parent_ID == UserMe.User_ID && (s.tbHistoryChats.Count > 0 || s.tbHistoryChats1.Count > 0)).ToList();
            }


            return PartialView(users);

        }
        public ActionResult GetContacts()
        {
            var userId = Convert.ToInt32(JwtToken.GetUser_ID());

            var users = UsersRepository.Where(s => s.Parent_ID == userId).OrderByDescending(s => s.Wallet).ToList();
            return PartialView(users);
        }

        public ActionResult GetChat(int userid)
        {
            var userId = Convert.ToInt32(JwtToken.GetUser_ID());
            var History = HistoryChatRepository.Where(s => s.fk_fromUser == userid || s.fk_toUser == userid).OrderBy(s => s.createDatetime).ToList();
            List<ChatViewModel> Chats = new List<ChatViewModel>();
            foreach (var item in History)
            {
                ChatViewModel chat = new ChatViewModel();
                if (item.fk_fromUser == userId)
                {
                    chat.SenderIsMe = true;
                    chat.Seened = item.seened;
                }
                else
                {
                    chat.SenderIsMe = false;
                    chat.Seened = true;
                    item.seened = true;
                }

                chat.MessageText = item.message;
                chat.MessageTime = Utility.GetTime(item.createDatetime);
                Chats.Add(chat);
            }
            HistoryChatRepository.Save();

            return PartialView(Chats);
        }


        public ActionResult GetInfoUser(int userid)
        {
            var User = UsersRepository.Where(s => s.User_ID == userid).FirstOrDefault();

            var Role = "";
            if (User.Role == 2)
            {
                Role = "نماینده";
            }
            else if (User.Role == 3 || User.Role == 4)
            {
                Role = "نماینده ارشد";
            }

            return Json(new { username = User.Username, role = Role });
        }
    }
}