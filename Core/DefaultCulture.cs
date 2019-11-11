using System.Configuration;
using System.Globalization;

namespace Knoema.Localization
{
	public static class DefaultCulture
	{
		private static CultureInfo _culture;
		private const string DefaultCultureSettingName = "localizerDefaultCulture";
		private static CultureInfo _cultureDefaultValue = new CultureInfo(1033);

		public static CultureInfo Value
		{
			get
			{
				if (_culture == null) 
				{
					string cultureName = ConfigurationManager.AppSettings[DefaultCultureSettingName];
					_culture = cultureName != null ? CultureInfo.GetCultureInfo(cultureName) : _cultureDefaultValue;
				}
				return _culture;
			}
		}

		public static bool IsDefault(this string name)
		{
			return name == DefaultCulture.Value.Name;
		}

		public static bool IsDefault(this CultureInfo culture)
		{
			return culture.Name == DefaultCulture.Value.Name;
		}
	}
}