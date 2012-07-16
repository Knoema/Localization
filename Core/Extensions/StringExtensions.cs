using System;
using System.Web;
using System.Globalization;

namespace Knoema.Localization
{
	public static class StringExtensions
	{
		public static string Resource(this string value, Type type)
		{
			if (type == null)
				throw new ArgumentNullException();

			if (CultureInfo.CurrentCulture.LCID == DefaultCulture.Value.LCID)
				return value;

			if (LocalizationManager.Repository == null)
				return value;

			var translation = LocalizationManager.Instance.Translate(LocalizationManager.Instance.FormatScope(type), value);
			return string.IsNullOrEmpty(translation)
					? value
					: translation;
		}

		public static string Resource(this string value, object obj)
		{
			return value.Resource(obj.GetType());
		}
	}
}
