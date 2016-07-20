using System.Collections.Generic;
using System.Globalization;

namespace Knoema.Localization
{
	public interface ILocalizationProvider
	{
		IEnumerable<ILocalizedObject> GetAll(CultureInfo culture);

		ILocalizedObject Get(int key);
		ILocalizedObject Get(CultureInfo culture, string scope, string text);
		ILocalizedObject Create(CultureInfo culture, string scope, string text);

		void Delete(params ILocalizedObject[] list);
		void Save(params ILocalizedObject[] list);
		
		IEnumerable<CultureInfo> GetCultures();

		string GetRoot();
	}
}
