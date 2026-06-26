using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using V2boardApi.Tools;

namespace V2boardApi.Areas.App.Controllers
{
    [LogActionFilter]
    public class ChangelogController : Controller
    {
        [AuthorizeApp(Roles = "1,2,3,4")]
        public async Task<ActionResult> Index()
        {
            PanelChangelogService.InvalidateVersionCache();
            var role = JwtToken.GetUserRole();
            var model = await PanelChangelogService.GetPageForRoleAsync(role);

            int userId;
            if (int.TryParse(JwtToken.GetUser_ID(), out userId))
                await PanelChangelogService.MarkVersionSeenAsync(userId, model.CurrentVersion);

            return View(model);
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
        [HttpGet]
        public async Task<ActionResult> GetPendingPopup()
        {
            int userId;
            if (!int.TryParse(JwtToken.GetUser_ID(), out userId))
                return Json(new { show = false }, JsonRequestBehavior.AllowGet);

            var role = JwtToken.GetUserRole();
            var popup = await PanelChangelogService.GetPendingPopupAsync(userId, role);

            if (popup == null)
                return Json(new { show = false }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                show = true,
                version = popup.Version,
                releaseDate = popup.ReleaseDate,
                items = popup.Items.Select(i => new
                {
                    title = i.Title,
                    description = i.Description
                }).ToList()
            }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeApp(Roles = "1,2,3,4")]
        [HttpGet]
        public async Task<ActionResult> MarkSeen(string version)
        {
            int userId;
            if (!int.TryParse(JwtToken.GetUser_ID(), out userId))
                return Content("Error");

            await PanelChangelogService.MarkVersionSeenAsync(userId, version);
            return Content("Ok");
        }
    }
}
