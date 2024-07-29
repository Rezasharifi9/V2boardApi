using DataLayer.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using Telegram.Bot.Types;

namespace V2boardApi.Tools
{
    public class AuthorizeApp: AuthorizeAttribute
    {
        private static readonly char[] _splitParameter = new char[1] { ',' };

        private readonly object _typeId = new object();

        private string _roles;

        private string[] _rolesSplit = new string[0];

        private string _users;

        private string[] _usersSplit = new string[0];

        //
        // Summary:
        //     Gets or sets the user roles that are authorized to access the controller or action
        //     method.
        //
        // Returns:
        //     The user roles that are authorized to access the controller or action method.
        public string Roles
        {
            get
            {
                return _roles ?? string.Empty;
            }
            set
            {
                _roles = value;
                _rolesSplit = SplitString(value);
            }
        }

        //
        // Summary:
        //     Gets the unique identifier for this attribute.
        //
        // Returns:
        //     The unique identifier for this attribute.
        public override object TypeId => _typeId;

        //
        // Summary:
        //     Gets or sets the users that are authorized to access the controller or action
        //     method.
        //
        // Returns:
        //     The users that are authorized to access the controller or action method.
        public string Users
        {
            get
            {
                return _users ?? string.Empty;
            }
            set
            {
                _users = value;
                _usersSplit = SplitString(value);
            }
        }

        //
        // Summary:
        //     When overridden, provides an entry point for custom authorization checks.
        //
        // Parameters:
        //   httpContext:
        //     The HTTP context, which encapsulates all HTTP-specific information about an individual
        //     HTTP request.
        //
        // Returns:
        //     true if the user is authorized; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The httpContext parameter is null.
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            IPrincipal user = httpContext.User;
            if (!user.Identity.IsAuthenticated)
            {
                return false;
            }

            if (_usersSplit.Length != 0 && !_usersSplit.Contains(user.Identity.Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            var Role = httpContext.Request.Cookies["Role"];
            if (Role == null)
            {
                return false;
            }


            using (var db = new Entities())
            {
                var Use = db.tbUsers.Where(p => p.Username == user.Identity.Name && p.Status == true).FirstOrDefault();
                if(Use != null)
                {
                    foreach(var item in _rolesSplit)
                    {
                        if (item == httpContext.Request.Cookies["Role"].Value && Use.Role.Value.ToString() == item)
                        {
                            return true;
                        }
                    }
                    httpContext.Response.Redirect("~/App/Error/Error401");
                }
            }

            
            return false;
        }

        private void CacheValidateHandler(HttpContext context, object data, ref HttpValidationStatus validationStatus)
        {
            validationStatus = OnCacheAuthorization(new HttpContextWrapper(context));
        }


        //
        // Summary:
        //     Processes HTTP requests that fail authorization.
        //
        // Parameters:
        //   filterContext:
        //     Encapsulates the information for using System.Web.Mvc.AuthorizeAttribute. The
        //     filterContext object contains the controller, HTTP context, request context,
        //     action result, and route data.
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new HttpUnauthorizedResult();
        }

        //
        // Summary:
        //     Called when the caching module requests authorization.
        //
        // Parameters:
        //   httpContext:
        //     The HTTP context, which encapsulates all HTTP-specific information about an individual
        //     HTTP request.
        //
        // Returns:
        //     A reference to the validation status.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The httpContext parameter is null.
        protected override HttpValidationStatus OnCacheAuthorization(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            if (!AuthorizeCore(httpContext))
            {
                return HttpValidationStatus.IgnoreThisRequest;
            }

            return HttpValidationStatus.Valid;
        }

        internal static string[] SplitString(string original)
        {
            if (string.IsNullOrEmpty(original))
            {
                return new string[0];
            }

            return (from piece in original.Split(_splitParameter)
                    let trimmed = piece.Trim()
                    where !string.IsNullOrEmpty(trimmed)
                    select trimmed).ToArray();
        }
    }
}