using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Knoema.Localization.EFProvider
{
	public class LocalizationRepository : ILocalizationRepository
	{
		public IEnumerable<CultureInfo> GetCultures()
		{
			using (var context = new LocalizationContext())
			{
				return context.Objects.Select(x => x.LocaleId).ToList().Distinct().Select(x => new CultureInfo(x)).ToList();
			}
		}

		public ILocalizedObject Create()
		{
			return new LocalizedObject();
		}

		public ILocalizedObject Get(int key)
		{
			using (var context = new LocalizationContext())
			{
				return context.Objects.Where(obj => obj.Key == key).FirstOrDefault();
			}
		}

		public IEnumerable<ILocalizedObject> GetAll(CultureInfo culture)
		{
			using (var context = new LocalizationContext())
			{
				return context.Objects.Where(obj => obj.LocaleId == culture.LCID).ToList();
			}
		}

		public void Save(params ILocalizedObject[] list)
		{
			using (var context = new LocalizationContext())
			{
				context.Objects.AddOrUpdate(list.Cast<LocalizedObject>().ToArray());
				context.SaveChanges();
			}
		}

		public void Delete(params ILocalizedObject[] list)
		{
			using (var context = new LocalizationContext())
			{
				foreach (var obj in list)
				{
					var stored = context.Objects.Where(x => x.Key == obj.Key).FirstOrDefault();
					if (stored != null)
						context.Objects.Remove(stored as LocalizedObject);
				}
					
				context.SaveChanges();
			}
		}

		public int GetCount(CultureInfo culture)
		{
			using (var context = new LocalizationContext())
			{
				return context.Objects.Where(obj => obj.LocaleId == culture.LCID).Count();
			}
		}
	}
}
