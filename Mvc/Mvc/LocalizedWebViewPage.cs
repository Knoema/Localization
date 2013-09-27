using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Web.UI;

using Knoema.Localization;
using Knoema.Localization.Web;
using System.Diagnostics.CodeAnalysis;

namespace Knoema.Localization.Mvc
{
	public abstract class LocalizedWebViewPage : WebViewPage
	{
		public string R(string text, params object[] formatterArguments)
		{
			return this.Translate(text, formatterArguments: formatterArguments);
		}

		public string R(string text, CultureInfo culture, params object[] formatterArguments)
		{
			return this.Translate(text, culture, formatterArguments);
		}

		public HtmlString R2(string text, params object[] formatterArguments)
		{
			return this.TranslateMarkup(text, formatterArguments: formatterArguments);
		}

		public HtmlString R2(string text, CultureInfo culture, params object[] formatterArguments)
		{
			return this.TranslateMarkup(text, culture, formatterArguments);
		}

		public MvcHtmlString RenderLocalizationIncludes(bool admin)
		{
			return MvcHtmlString.Create(LocalizationHandler.RenderIncludes(admin, LocalizationManager.Instance.GetScope()));
		}
	}

	public abstract class LocalizedWebViewPage<TModel> : LocalizedWebViewPage
	{
		private ViewDataDictionary<TModel> _viewData;

		public new AjaxHelper<TModel> Ajax { get; set; }

		public new HtmlHelper<TModel> Html { get; set; }

		public new TModel Model
		{
			get
			{ 
				return ViewData.Model;
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is the mechanism by which the ViewPage gets its ViewDataDictionary object.")]
		public new ViewDataDictionary<TModel> ViewData
		{
			get
			{
				if (_viewData == null)				
					SetViewData(new ViewDataDictionary<TModel>());				

				return _viewData;
			}
			set 
			{ 
				SetViewData(value); 
			}
		}

		public override void InitHelpers()
		{
			base.InitHelpers();

			Ajax = new AjaxHelper<TModel>(ViewContext, this);
			Html = new HtmlHelper<TModel>(ViewContext, this);
		}

		protected override void SetViewData(ViewDataDictionary viewData)
		{
			_viewData = new ViewDataDictionary<TModel>(viewData);

			base.SetViewData(_viewData);
		}
	}

	internal static class WebViewPageExtensions
	{
		internal static string Translate(this WebViewPage page, string text, CultureInfo culture = null, params object[] formatterArguments)
		{
			var translation = Translate(page.VirtualPath, text, culture: culture);

			if (formatterArguments.Length == 0)
				return translation;

			return formatterArguments.Length == 1 ? translation.FormatWith(formatterArguments[0]) : string.Format(translation, formatterArguments);
		}

		internal static HtmlString TranslateMarkup(this WebViewPage page, string text, CultureInfo culture = null, params object[] formatterArguments)
		{
			var translation = Translate(page.VirtualPath, text, true, culture);

			if (formatterArguments.Length == 1)
				translation = translation.FormatWith(formatterArguments[0]);
			else if (formatterArguments.Length > 1)
				translation = string.Format(translation, formatterArguments);

			return new HtmlString(translation.ParseMarkup());
		}

		private static string Translate(string virtualPath, string text, bool parseMarkup = false, CultureInfo culture = null)
		{
			LocalizationManager.Instance.InsertScope(VirtualPathUtility.ToAppRelative(virtualPath).ToLowerInvariant());

			if (culture == null)
				culture = CultureInfo.CurrentCulture;

			if (culture.IsDefault())
				return text;

			if (LocalizationManager.Repository == null)
				return text;

			var translation = LocalizationManager.Instance.Translate(virtualPath, text, culture: culture);

			return string.IsNullOrEmpty(translation) ? text : translation;
		}
	}

	internal static class StringExtensions
	{
		internal static string ParseMarkup(this string value)
		{
			if (string.IsNullOrEmpty(value) || !value.Contains("["))
				return value;

			var result = value;
			var regex = new Regex(@"\[(.*?)\]");
			foreach (Match match in regex.Matches(result))
			{
				var items = match.Value.Trim('[', ']').Split('|');
				var innerText = items[0];

				var tagBuilder = new System.Text.StringBuilder("<a");
				if (items.Length > 1)
				{
					var attrs = new string[items.Length - 1];
					Array.Copy(items, 1, attrs, 0, items.Length - 1);
					foreach (var attr in attrs)
					{
						var attrName = attr.Split('=')[0];
						tagBuilder.Append(" " + attrName);
						if (attr.Split('=').Length > 1)
							tagBuilder.Append("=\"" + attr.Substring(attrName.Length + 1) + "\"");
					}
				}
				tagBuilder.Append(">" + innerText + "</a>");

				result = result.Replace(match.Value, tagBuilder.ToString());
			}

			return result;
		}

		internal static string FormatWith(this string format, object source)
		{
			return FormatWith(format, null, source);
		}

		internal static string FormatWith(this string format, IFormatProvider provider, object source)
		{
			if (format == null)
				throw new ArgumentNullException("format");

			var r = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
			var values = new List<object>();
			
			string rewrittenFormat = r.Replace(format, delegate(Match m)
			{
				try
				{
					Group startGroup = m.Groups["start"];
					Group propertyGroup = m.Groups["property"];
					Group formatGroup = m.Groups["format"];
					Group endGroup = m.Groups["end"];

					values.Add((propertyGroup.Value == "0") ? source : DataBinder.Eval(source, propertyGroup.Value));

					return new string('{', startGroup.Captures.Count) + (values.Count - 1) + formatGroup.Value + new string('}', endGroup.Captures.Count);
				}
				catch
				{
					return null;
				}
			});

			return string.Format(provider, rewrittenFormat, values.ToArray());
		}
	}
}
