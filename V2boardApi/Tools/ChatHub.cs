using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using DataLayer.DomainModel;
using DataLayer.Repository;


namespace V2boardApi.Tools
{
    public class ChatHub : Hub
    {

        private static readonly Dictionary<string, string> _connections = new Dictionary<string, string>();

        private Repository<tbHistoryChats> ChatHistoryes { get; set; }
        public ChatHub()
        {
            ChatHistoryes = new Repository<tbHistoryChats>();
        }

        // متد برای ارسال پیام به همه کاربران
        public void SendMessage(string message, string userid)
        {
            tbHistoryChats chat = new tbHistoryChats();
            if (_connections.ContainsKey(userid))
            {
                var ConnectionId = _connections[userid];
                Clients.Client(ConnectionId).broadcastMessage(message);
                chat.seened = true;
            }
            else
            {
                chat.seened = false;
            }
            
            chat.createDatetime = DateTime.Now;
            chat.fk_fromUser = Convert.ToInt32(JwtToken.GetUser_ID());
            chat.fk_toUser = Convert.ToInt32(userid);
            chat.message = message;
            ChatHistoryes.Insert(chat);
            ChatHistoryes.Save();
        }

        public string GetConnectionId(string userid)
        {
            return _connections[userid];
        }


        public void seen(string userid)
        {

            if (_connections.ContainsKey(userid))
            {
                var ConnectionId = _connections[userid];
                // ارسال پیام
                Clients.Client(ConnectionId).seenMessage();
            }
        }

        public Dictionary<string, string> GetOnlineUsers()
        {
            // ارسال ConnectionId به کلاینت
            return _connections;
        }


        public override Task OnConnected()
        {
            var userid = JwtToken.GetUser_ID();

            string userConnectionID = Context.ConnectionId;

            if (!_connections.ContainsKey(userid))
            {
                _connections[userid] = userConnectionID; // ذخیره ConnectionId برای این کاربر
            }

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string userId = _connections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;

            if (userId != null)
            {
                _connections.Remove(userId); // حذف ConnectionId از دیکشنری
            }

            return base.OnDisconnected(stopCalled);
        }
    }
}