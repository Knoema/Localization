using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Web;
using System.Threading;

namespace Knoema.Localization
{
	public sealed class LocalizationManager : LocalizationManagerBase
	{
		private static readonly LocalizationManager _instance = new LocalizationManager();
		public static LocalizationManager Instance
		{
			get
			{
				return _instance;
			}
		}

		public override void SetCulture(CultureInfo culture, string cookieName = CookieName)
		{
			base.SetCulture(culture, cookieName);
			var cookie = HttpContext.Current.Response.Cookies[cookieName];

			if (cookie == null)
			{
				cookie = new HttpCookie(cookieName, culture.Name);
				HttpContext.Current.Response.Cookies.Add(cookie);
			}

			cookie.Expires = DateTime.Now.AddYears(1);
			cookie.Value = culture.Name;
		}

		public override IList<string> GetBrowserCultures()
		{
			if (HttpContext.Current == null)
				return base.GetBrowserCultures();

			var cultures = new List<string>();
			var browser = HttpContext.Current.Request.UserLanguages;
			if (browser != null)
				foreach (var culture in browser)
				{
					var lang = culture.IndexOf(';') > -1
						? culture.Split(';')[0]
						: culture;

					cultures.Add(lang);
				}

			return cultures.Distinct().ToList();
		}

		public override string GetCultureFromCookie(string cookieName = CookieName)
		{
			var cookie = HttpContext.Current.Request.Cookies[cookieName] ?? HttpContext.Current.Request.Cookies[LocalizationManager.CookieName];

			if (cookie != null)
				return cookie.Value;

			return base.GetCultureFromCookie(cookieName);
		}

		public override string GetCultureFromQuery()
		{
			return HttpContext.Current.Request.QueryString[LocalizationManager.QueryParameter];
		}

		public override void InsertScope(string path)
		{
			base.InsertScope(path);
			if (HttpContext.Current == null)
				return;

			var scope = HttpContext.Current.Items["localizationScope"] as List<string> ?? new List<string>();

			if (!scope.Contains(path))
				scope.Add(path);

			HttpContext.Current.Items["localizationScope"] = scope;
		}

		public override List<string> GetScope()
		{
			if (HttpContext.Current == null)
				return base.GetScope();

			return HttpContext.Current.Items["localizationScope"] as List<string>;
		}
	}
}
