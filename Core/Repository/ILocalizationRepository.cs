using System.Collections.Generic;
using System.Globalization;

namespace Knoema.Localization
{
	public interface ILocalizationRepository
	{
		IEnumerable<CultureInfo> GetCultures();
		ILocalizedObject Create();
		ILocalizedObject Get(int key);
		IEnumerable<ILocalizedObject> GetAll(CultureInfo culture);
		void Save(params ILocalizedObject[] list);
		void Delete(params ILocalizedObject[] list);
	}
}
