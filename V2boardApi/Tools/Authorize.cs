using DataLayer.DomainModel;
using DataLayer.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Security;
using System.Web.UI.WebControls;

namespace V2boardApi.Tools
{
    public class Authorize : AuthorizeAttribute
    {
        
        private Repository<tbUsers> RepositoryUser { get; set; }
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            RepositoryUser = new Repository<tbUsers>();
            var currentIdentity = System.Threading.Thread.CurrentPrincipal.Identity;
            if (!currentIdentity.IsAuthenticated)
            {
                var auth = actionContext.Request.Headers.Authorization;
                if (auth != null)
                {
                    if (!string.IsNullOrEmpty(auth.Scheme))
                    {
                        var User = RepositoryUser.table.Where(p => p.Token == auth.Scheme && p.Status == true).FirstOrDefault();
                        if(User == null)
                        {

                            HandleUnauthorizedRequest(actionContext);

                        }
                        else
                        {
                            IsAuthorized(actionContext);
                        }
                    }
                    else { HandleUnauthorizedRequest(actionContext); }
                }
                else { HandleUnauthorizedRequest(actionContext); }
            }
            else { HandleUnauthorizedRequest(actionContext); }

        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            return true;
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            actionContext.Response = actionContext.ControllerContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "دسترسی رد شد لطفا مجدد وارد شوید");
        }
    }
}