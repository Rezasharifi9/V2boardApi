using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.OnlineChatModel
{
    public class ChatViewModel
    {
        public string MessageText { get; set; }
        public bool SenderIsMe { get; set; }
        public string MessageTime { get; set; }
        public bool Seened { get;set; }
    }

    public class ChatView
    {
        public List<ChatViewModel> Chats { get; set; }
        public string username { get; set; }
        public string role { get; set; }
    }
}