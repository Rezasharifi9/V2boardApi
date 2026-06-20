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
            var role = JwtToken.GetUserRole();
            var model = await PanelChangelogService.GetPageForRoleAsync(role);
            return View(model);
        }
    }
}
