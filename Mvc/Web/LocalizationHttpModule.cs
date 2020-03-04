using System;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Linq;

namespace Knoema.Localization.Web
{
	public class LocalizationModule : IHttpModule
	{
		public void Init(HttpApplication context)
		{
			context.BeginRequest += new EventHandler(BeginRequest);
		}

		private void BeginRequest(Object sender, EventArgs e)
		{
			if (LocalizationManager.Provider != null)
			{
				var context = ((HttpApplication)sender).Context;
				var lang = string.Empty;

				// try to get language from request query, cookie or browser language

				if (context.Request.QueryString["lang"] != null)
					lang = context.Request.QueryString["lang"];

				else if (context.Request.Cookies[LocalizationManager.CookieName] != null)
					lang = context.Request.Cookies[LocalizationManager.CookieName].Value;

				else if (context.Request.UserLanguages != null)
				{
					lang = context.Request.UserLanguages[0];
					if (lang.Length < 3)
						lang = string.Format("{0}-{1}", lang, lang.ToUpper());
				}

				try
				{
					LocalizationManager.Instance.SetCulture(new CultureInfo(lang));
				}
				catch (CultureNotFoundException) { }
				catch (ArgumentNullException) { }
			}
		}

		public void Dispose() { }
	}
}
