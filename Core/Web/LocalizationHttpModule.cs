using System;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Linq;

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
				var lang = string.Empty;

				// try to get language from request query, cookie or browser language

				if (context.Request.QueryString["lang"] != null)				
					lang = context.Request.QueryString["lang"];		
				
				else if (context.Request.Cookies[_cookieName] != null)
					lang = context.Request.Cookies[_cookieName].Value;

				else if (context.Request.UserLanguages != null)
				{
					lang = context.Request.UserLanguages[0];
					if (lang.Length < 3)
						lang = string.Format("{0}-{1}", lang, lang.ToUpper());
				}

				try
				{
					Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
					context.Response.Cookies.Add(new HttpCookie(_cookieName, lang)
					{
						Expires = DateTime.Now.AddYears(1),
					});					
				}
				catch (CultureNotFoundException) 
				{ }
			}
		}

		public void Dispose() { }		
	}
}
