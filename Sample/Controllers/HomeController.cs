using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Knoema.Localization;
using System.Globalization;
namespace Sample.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			ViewBag.Message = "Welcome to ASP.NET MVC!".Resource(this);

			return View();
		}

		public ActionResult About()
		{
			return View();
		}

		public ActionResult Lang(string culture)
		{
			LocalizationManager.Instance.SetCulture(new CultureInfo(culture));
			return new RedirectResult(Request.UrlReferrer.ToString());
		}
	}
}
