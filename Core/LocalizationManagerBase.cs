﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System;
using System.Threading;

namespace Knoema.Localization
{
	public class LocalizationManagerBase
	{
		public const string CookieName = "current-lang";
		public const string QueryParameter = "lang";
		public static ILocalizationProvider Provider { get; set; }
		
		private static readonly object _lock = new object();
		private static LocalizationManagerBase _instance;
		public static LocalizationManagerBase InstanceBase { get { return _instance; } }

		protected LocalizationManagerBase()
		{
			if (_instance == null)
			{
				lock (_lock)
				{
					if (_instance == null)
						_instance = this;
				}
			}
		}

		public string Translate(string scope, string text, bool readOnly = false, CultureInfo culture = null)
		{
			if (string.IsNullOrEmpty(scope))
				throw new ArgumentNullException("Scope cannot be null.");

			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException("Text cannot be null.");

			if (culture == null)
				culture = CultureInfo.CurrentCulture;

			var cultures = GetCultures().ToList();

			if (cultures.Count > 0 && !cultures.Contains(culture))
				return null;

			var obj = Provider.Get(culture, scope, text);

			if (readOnly && obj == null)
				return null;

			// if null save object to db for all cultures 
			if (obj == null)
			{
				if (!cultures.Contains(DefaultCulture.Value))
					cultures.Add(DefaultCulture.Value);

				foreach (var c in cultures)
					lock (_lock)
					{
						var stored = Provider.Get(c, scope, text);
						if (stored == null)
							Save(Create(c, scope, text));
					}
			}
			else
				return obj.IsDisabled() ? null : obj.Translation;

			return null;
		}

		public void AddTranslationsForCurrentCulture(string scope, IEnumerable<string> phrases)
		{
			if (string.IsNullOrEmpty(scope))
				throw new ArgumentNullException("Scope cannot be null.");

			if (phrases == null || !phrases.Any())
				throw new ArgumentNullException("Phrases cannot be null or empty.");

			var import = new List<ILocalizedObject>();
			var toAdd = phrases.Distinct();

			foreach (var phrase in toAdd)
			{
				var obj = Provider.Get(CultureInfo.CurrentCulture, scope, phrase);

				if (obj == null)
					import.Add(Create(CultureInfo.CurrentCulture, scope, phrase));
			}

			Save(import.ToArray());
		}

		public void CreateCulture(CultureInfo culture)
		{
			var res = new List<ILocalizedObject>();
			var lst = GetLocalizedObjects(DefaultCulture.Value);

			foreach (var obj in lst)
			{
				var stored = Provider.Get(culture, obj.Scope, obj.Text);
				if (stored == null)
					res.Add(Create(culture, obj.Scope, obj.Text));
			}

			Save(res.ToArray());
		}

		public ILocalizedObject Create(CultureInfo culture, string scope, string text)
		{
			return Provider.Create(culture, scope, text);
		}

		public ILocalizedObject Get(int key, bool ignoreDisabled = false)
		{
			var result = Provider.Get(key);

			if (result == null)
				return null;

			if (ignoreDisabled && result.IsDisabled())
				return null;

			return result;
		}

		public IEnumerable<ILocalizedObject> GetLocalizedObjects(CultureInfo culture, string text = null, bool strict = true)
		{
			if (string.IsNullOrEmpty(text))
				return Provider.GetAll(culture);

			if (strict)
				return Provider.GetAll(culture).Where(x => string.Equals(x.Text, text, StringComparison.InvariantCultureIgnoreCase));

			return Provider.GetAll(culture).Where(x => x.Text.ToUpperInvariant().Contains(text.ToUpperInvariant()));
		}

		public IEnumerable<ILocalizedObject> GetAllLocalizedObjects(string text = null, bool strict = true, string scope = null)
		{
			if (string.IsNullOrEmpty(text))
				return null;

			if (strict)
				return Provider.GetAll(null, scope).Where(x => string.Equals(x.Text, text, StringComparison.InvariantCultureIgnoreCase) && string.Equals(x.Scope, scope, StringComparison.InvariantCultureIgnoreCase));

			return Provider.GetAll(null, scope).Where(x => x.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 && string.Equals(x.Scope, scope, StringComparison.InvariantCultureIgnoreCase));
		}

		public IEnumerable<Object> GetScriptResources(CultureInfo culture)
		{
			if (!GetCultures().Contains(culture))
				return Enumerable.Empty<ILocalizedObject>();

			return GetLocalizedObjects(culture).Where(x => (x.Scope != null) && (x.Scope.EndsWith("js") || x.Scope.EndsWith("htm")))
				.Select(x => new
				{
					Scope = x.Scope,
					Text = x.Text,
					Translation = x.Translation,
					IsDisabled = x.IsDisabled()
				});
		}

		public IEnumerable<CultureInfo> GetCultures()
		{
			return Provider.GetCultures();
		}

		public void Delete(params ILocalizedObject[] list)
		{
			Provider.Delete(list);
		}

		public void ClearDB(CultureInfo culture = null)
		{
			if (culture == null)
			{
				foreach (var item in GetCultures())	
					Delete(GetLocalizedObjects(item).Where(obj => obj.IsDisabled()).ToArray());	
			}
			else
				Delete(GetLocalizedObjects(culture).Where(obj => obj.IsDisabled()).ToArray());
		}

		public void Disable(params ILocalizedObject[] list)
		{
			foreach (var obj in list)
				obj.Disable();

			Provider.Save(list);
		}

		public void Save(params ILocalizedObject[] list)
		{
			Provider.Save(list);	
		}

		public void Import(params ILocalizedObject[] list)
		{
			var import = new Dictionary<int, List<ILocalizedObject>>();
			var root = Provider.GetRoot();
			
			foreach (var obj in list)
			{
				if (!string.IsNullOrEmpty(root) && !obj.Scope.StartsWith(root, StringComparison.OrdinalIgnoreCase))
					continue;

				var stored = Provider.Get(new CultureInfo(obj.LocaleId), obj.Scope, obj.Text);
				if (stored != null)
				{
					if (!string.IsNullOrEmpty(obj.Translation))
					{
						stored.Translation = obj.Translation;
						
						if (!import.ContainsKey(obj.LocaleId))
							import.Add(obj.LocaleId, new List<ILocalizedObject>());
						
						import[obj.LocaleId].Add(stored);
					}
				}
				else
				{
					var imported = Create(new CultureInfo(obj.LocaleId), obj.Scope, obj.Text);
					imported.Translation = obj.Translation;

					if (!import.ContainsKey(obj.LocaleId))
						import.Add(obj.LocaleId, new List<ILocalizedObject>());
					
					import[obj.LocaleId].Add(imported);
				}

				// check object for default culture
				var def = Provider.Get(DefaultCulture.Value, obj.Scope, obj.Text);
				if (def == null)
				{
					if (!import.ContainsKey(obj.LocaleId))
						import.Add(obj.LocaleId, new List<ILocalizedObject>());

					import[obj.LocaleId].Add(Create(DefaultCulture.Value, obj.Scope, obj.Text));
				}
			}

			foreach (var localeId in import.Keys)
				Save(import[localeId].ToArray());
		}

		public string FormatScope(Type type)
		{
			var scope = type.Assembly.FullName.Split(',').Length > 0
					? type.FullName.Replace(type.Assembly.FullName.Split(',')[0], "~")
					: type.FullName;

			return scope.Replace(".", "/");
		}

		public virtual void SetCulture(CultureInfo culture, string cookieName = CookieName)
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = culture;
		}

		public string GetCulture()
		{
			return Thread.CurrentThread.CurrentCulture.Name;
		}

		public virtual IList<string> GetBrowserCultures()
		{
			var cultures = new List<string>();
			return cultures;
		}

		public virtual string GetCultureFromCookie(string cookieName = CookieName)
		{
			return null;
		}

		public string GetCookieName(string prefix = null)
		{
			if (prefix == null)
				return CookieName;

			return string.Format("{0}-{1}", prefix, CookieName);
		}

		public virtual string GetCultureFromQuery()
		{
			return null;
		}

		public virtual void InsertScope(string path)
		{
		}

		public virtual List<string> GetScope()
		{
			return null;
		}

		public string GetRoot()
		{
			return Provider.GetRoot();
		}
	}
}
