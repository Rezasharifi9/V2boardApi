using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.Mvc;
using NLog;
using NLog.Config;
using NLog.Targets;

public class LogActionFilter : ActionFilterAttribute
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private Stopwatch stopwatch;

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        stopwatch = Stopwatch.StartNew();

        var request = filterContext.HttpContext.Request;
        var ipAddress = GetIpAddress();
        var requestData = GetRequestBody(request);
        var userId = filterContext.HttpContext.User.Identity.IsAuthenticated ? filterContext.HttpContext.User.Identity.Name : "Anonymous";
        var userName = filterContext.HttpContext.User.Identity.Name;
        var userRole = filterContext.HttpContext.User.IsInRole("Admin") ? "Admin" : "User"; // Adjust as necessary
        var requestUrl = request.Url?.ToString() ?? "Unknown";
        var httpMethod = request.HttpMethod;
        var requestHeaders = request.Headers.ToString();
        var userAgent = request.UserAgent;
        var sessionId = HttpContext.Current.Session != null ? HttpContext.Current.Session.SessionID : "NoSession";
        var actionName = filterContext.ActionDescriptor.ActionName;
        var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
        var customData = "Some custom data"; // Replace with actual custom data if needed
        
        // Set context properties
        MappedDiagnosticsLogicalContext.Set("IpAddress", ipAddress);
        MappedDiagnosticsLogicalContext.Set("RequestData", requestData);
        MappedDiagnosticsLogicalContext.Set("UserId", userId);
        MappedDiagnosticsLogicalContext.Set("UserName", userName);
        MappedDiagnosticsLogicalContext.Set("UserRole", userRole);
        MappedDiagnosticsLogicalContext.Set("RequestUrl", requestUrl);
        MappedDiagnosticsLogicalContext.Set("HttpMethod", httpMethod);
        MappedDiagnosticsLogicalContext.Set("RequestHeaders", requestHeaders);
        MappedDiagnosticsLogicalContext.Set("UserAgent", userAgent);
        MappedDiagnosticsLogicalContext.Set("SessionId", sessionId);
        MappedDiagnosticsLogicalContext.Set("ActionName", actionName);
        MappedDiagnosticsLogicalContext.Set("ControllerName", controllerName);
        MappedDiagnosticsLogicalContext.Set("CustomData", customData);

        base.OnActionExecuting(filterContext);
    }

    public override void OnActionExecuted(ActionExecutedContext filterContext)
    {
        stopwatch.Stop();

        base.OnActionExecuted(filterContext);
    }

    private string GetIpAddress()
    {
        string ip = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
        if (string.IsNullOrEmpty(ip))
        {
            ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
        }
        return ip;
    }

    private string GetRequestBody(HttpRequestBase request)
    {
        if (request.InputStream.CanSeek)
        {
            request.InputStream.Position = 0;
        }

        using (var reader = new StreamReader(request.InputStream))
        {
            return reader.ReadToEnd();
        }
    }

    private string GetResponseBody(HttpResponseBase response)
    {
        try
        {
            if (response.Filter != null && response.Filter.CanRead)
            {
                response.Filter.Position = 0;
                using (var reader = new StreamReader(response.Filter))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        catch
        {
            // If unable to read response body, log an appropriate message
        }
        return string.Empty;
    }
}



