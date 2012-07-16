using System;
using System.Globalization;
using System.Threading;
using System.Web;

namespace Knoema.Localization.Web
{
	public class LocalizationModule: IHttpModule
	{
		private const string _cookieName = "localization-current-lang";
		public void Init(HttpApplication context)
		{
			context.BeginRequest += new EventHandler(BeginRequest);
		}

		private void BeginRequest(Object sender, EventArgs e)
		{
			if (LocalizationManager.Repository != null)
			{
				var context = ((HttpApplication)sender).Context;
				var lang = context.Request.QueryString["lang"];

				if (!string.IsNullOrEmpty(lang))
				{
					try
					{
						Thread.CurrentThread.CurrentCulture =
							Thread.CurrentThread.CurrentUICulture =
								new CultureInfo(lang);

						context.Response.Cookies.Add(new HttpCookie(_cookieName, lang)
						{
							Expires = DateTime.Now.AddYears(1),
						});
					}
					catch (CultureNotFoundException)
					{
					}
				}
				else if (context.Request.Cookies[_cookieName] != null)
					Thread.CurrentThread.CurrentCulture =
						Thread.CurrentThread.CurrentUICulture =
							new CultureInfo(context.Request.Cookies[_cookieName].Value);
			}
		}

		public void Dispose()
		{
		}
	}
}
