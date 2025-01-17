using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;

[assembly: OwinStartup(typeof(V2boardApi.Tools.StartUpOwin))]
namespace V2boardApi.Tools
{
    public class StartUpOwin
    {
        public void Configuration(IAppBuilder app)
        {
            // پیکربندی SignalR
            app.MapSignalR();
        }
    }
}



