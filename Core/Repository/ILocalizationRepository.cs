using System.Collections.Generic;
using System.Globalization;

namespace Knoema.Localization
{
	public interface ILocalizationRepository
	{
		/// <summary>
		/// Function returns list of supported cultures
		/// This method is constantly called, it should be as fast as possible. It isn't cached with common caching mechanism due to performance issues
		/// </summary>
		/// <returns></returns>
		IEnumerable<CultureInfo> GetCultures();
		ILocalizedObject Create();
		ILocalizedObject Get(int key);
		IEnumerable<ILocalizedObject> GetAll(CultureInfo culture);
		int GetCount(CultureInfo culture);
		void Save(params ILocalizedObject[] list);
		void Delete(params ILocalizedObject[] list);
	}
}
