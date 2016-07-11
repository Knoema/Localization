using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Knoema.Localization
{
	public class LocalizationProvider : ILocalizationProvider
	{
		private ILocalizationRepository _repository;

		public LocalizationProvider(ILocalizationRepository repository)
		{
			_repository = repository;
		}

		public IEnumerable<ILocalizedObject> GetAll(CultureInfo culture)
		{
			var result = LocalizationCache.Get<IEnumerable<ILocalizedObject>>(culture.Name);

			if (result == null || !result.Any())
			{
				result = _repository.GetAll(culture).ToList();
				LocalizationCache.Set(culture.Name, result);
			}

			return result;
		}

		public ILocalizedObject GetLocalizedObject(CultureInfo culture, string hash)
		{
			return GetAll(culture).FirstOrDefault(x => x.Hash == hash);
		}

		public void Delete(CultureInfo culture, params ILocalizedObject[] list)
		{
			_repository.Delete(list);
			LocalizationCache.Clear();
		}

		public void Disable(CultureInfo culture, params ILocalizedObject[] list)
		{
			foreach (var obj in list)
				obj.Disable();

			_repository.Save(list);
			LocalizationCache.Clear();
		}

		public void Save(CultureInfo culture, params ILocalizedObject[] list)
		{
			_repository.Save(list);
			LocalizationCache.Clear();
		}

		public ILocalizedObject Get(int key, bool ignoreDisabled = false)
		{
			var obj = LocalizationCache.Get<ILocalizedObject>(key.ToString());
			if (obj == null)
			{
				obj = _repository.Get(key);
				LocalizationCache.Set(key.ToString(), obj);
			}

			if (ignoreDisabled && obj.IsDisabled())
				return null;

			return obj;
		}

		public IEnumerable<CultureInfo> GetCultures()
		{
			return _repository.GetCultures().ToList();
		}

		public ILocalizedObject Create(string hash, int localeId, string scope, string text)
		{
			var result = _repository.Create();
			result.Hash = hash;
			result.LocaleId = localeId;
			result.Scope = scope;
			result.Text = text;
			return result;
		}

		public IEnumerable<CultureInfo> GetVisibleCultures()
		{
			return GetCultures();
		}

		public IEnumerable<ILocalizedObject> GetVisibleLocalizedObjects(CultureInfo culture)
		{
			return GetAll(culture);
		}

		public void InsertScope(string path)
		{
			if (HttpContext.Current == null)
				return;

			var scope = HttpContext.Current.Items["localizationScope"] as List<string> ?? new List<string>();

			if (!scope.Contains(path))
				scope.Add(path);

			HttpContext.Current.Items["localizationScope"] = scope;
		}
	}
}
