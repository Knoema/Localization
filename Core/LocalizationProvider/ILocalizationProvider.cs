using System.Collections.Generic;
using System.Globalization;

namespace Knoema.Localization
{
	public interface ILocalizationProvider
	{
		ILocalizedObject Get(int key, bool ignoreDisabled = false);
		ILocalizedObject GetLocalizedObject(CultureInfo culture, string hash);
		ILocalizedObject Create(string hash, int localeId, string scope, string text);

		void Delete(CultureInfo culture, params ILocalizedObject[] list);
		void Disable(CultureInfo culture, params ILocalizedObject[] list);
		void Save(CultureInfo culture, params ILocalizedObject[] list);
		void InsertScope(string path);
		
		IEnumerable<CultureInfo> GetCultures();
		IEnumerable<CultureInfo> GetVisibleCultures();

		IEnumerable<ILocalizedObject> GetVisibleLocalizedObjects(CultureInfo culture);
		IEnumerable<ILocalizedObject> GetAll(CultureInfo culture);
	}
}
