using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Knoema.Localization.EFProvider
{
	public class LocalizationRepository: DbContext, ILocalizationRepository
	{

		public LocalizationRepository()
		{
		}

		public LocalizationRepository(string nameOrConnectionString) :
			base(nameOrConnectionString)
		{
		}

		public LocalizationRepository(System.Data.Common.DbConnection existingConnection, bool contextOwnsConnection) :
			base(existingConnection, contextOwnsConnection)
		{
		}

		public IDbSet<LocalizedObject> Objects { get; set; }

		IEnumerable<System.Globalization.CultureInfo> ILocalizationRepository.GetCultures()
		{
			return Objects.Select(x => x.LocaleId)
					.ToList()
					.Distinct()
					.Select(x => new CultureInfo(x));
		}

		ILocalizedObject ILocalizationRepository.Create()
		{
			return new LocalizedObject();
		}

		ILocalizedObject ILocalizationRepository.Get(int key)
		{
			return Objects.Where(obj => obj.Key == key).FirstOrDefault();
		}

		IEnumerable<ILocalizedObject> ILocalizationRepository.GetAll(CultureInfo culture)
		{
			return Objects.Where(obj => obj.LocaleId == culture.LCID);
		}

		void ILocalizationRepository.Save(params ILocalizedObject[] list)
		{
			Objects.AddOrUpdate(list.Cast<Knoema.Localization.EFProvider.LocalizedObject>().ToArray());
			SaveChanges();
		}

		void ILocalizationRepository.Delete(params ILocalizedObject[] list)
		{
			foreach (var obj in list)
				Objects.Remove(obj as Knoema.Localization.EFProvider.LocalizedObject);

			SaveChanges();
		}

	}
}
