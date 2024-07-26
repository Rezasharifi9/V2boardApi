using System.Linq;
using System.Text;
using System.Web.Mvc;

public static class ModelStateExtensions
{
    public static string GetError(this ModelStateDictionary modelState)
    {
        var errors = modelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(e => !string.IsNullOrEmpty(e));

        var errorString = new StringBuilder();
        foreach (var error in errors)
        {
            return error;
        }

        return errorString.ToString();
    }
}
