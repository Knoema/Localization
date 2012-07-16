using System.Web;
using System.Web.Mvc;
using Knoema.Localization.Web;
using System.Globalization;

namespace Knoema.Localization.Mvc
{
	public abstract class LocalizedWebViewPage : WebViewPage
	{
		public string R(string text, params object[] formatterArguments)
		{
			if (CultureInfo.CurrentCulture.LCID == DefaultCulture.Value.LCID)
				return text;

			if (LocalizationManager.Repository == null)
				return text;

			var translation = LocalizationManager.Instance.Translate(VirtualPath, text);

			if (string.IsNullOrEmpty(translation))
				translation = text;

			return formatterArguments.Length == 0 ? translation : string.Format(translation, formatterArguments);
		}

		public MvcHtmlString RenderLocalizationIncludes(bool admin)
		{
			return MvcHtmlString.Create(LocalizationHandler.RenderIncludes(admin));
		}
	}

	public abstract class LocalizedWebViewPage<TModel> : WebViewPage<TModel>
	{
		public string R(string text, params object[] formatterArguments)
		{
			if (CultureInfo.CurrentCulture.LCID == DefaultCulture.Value.LCID)
				return text;

			if (LocalizationManager.Repository == null)
				return text;

			var translation = LocalizationManager.Instance.Translate(
				VirtualPathUtility.ToAppRelative(VirtualPath), text);
			
			if (string.IsNullOrEmpty(translation))
				translation = text;

			return formatterArguments.Length == 0 ? translation : string.Format(translation, formatterArguments);		
		}

		public MvcHtmlString RenderLocalizationIncludes(bool admin)
		{
			return MvcHtmlString.Create(LocalizationHandler.RenderIncludes(admin));
		}
	}
}
