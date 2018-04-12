using System.Web;
using System.Web.Mvc;

namespace MvcWeb_CustomAd
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
