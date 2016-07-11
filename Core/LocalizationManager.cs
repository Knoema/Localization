using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Web;
using System.Threading;

namespace Knoema.Localization
{
	public sealed class LocalizationManager
	{
		public const string CookieName = "current-lang";
		public const string QueryParameter = "lang";
		public static ILocalizationProvider Provider { get; set; }
		
		private static object _lock = new object();

		private static readonly LocalizationManager _instanse = new LocalizationManager();
		public static LocalizationManager Instance
		{
			get
			{
				return _instanse;
			}
		}

		private LocalizationManager() { }

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

			var hash = GetHash(scope, text);

			// get object from cache...
			var obj = Provider.GetLocalizedObject(culture, hash);

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
						var stored = Provider.GetLocalizedObject(c, hash);
						if (stored == null)
							Save(c, Create(hash, c.LCID, scope, text));
					}
			}
			else
				return obj.IsDisabled() ? null : obj.Translation;

			return null;
		}

		public void AddTranslatinsForCurrentCulture(string scope, IEnumerable<string> phrases)
		{
			if (string.IsNullOrEmpty(scope))
				throw new ArgumentNullException("Scope cannot be null.");

			if (phrases == null || !phrases.Any())
				throw new ArgumentNullException("Phrases cannot be null or empty.");

			var import = new List<ILocalizedObject>();
			var toAdd = phrases.Distinct();

			foreach (var phrase in toAdd)
			{
				var hash = GetHash(scope, phrase);
				var obj = Provider.GetLocalizedObject(CultureInfo.CurrentCulture, hash);

				if (obj == null)
					import.Add(Create(hash, CultureInfo.CurrentCulture.LCID, scope, phrase));
			}

			Save(CultureInfo.CurrentCulture, import.ToArray());
		}

		public void CreateCulture(CultureInfo culture)
		{
			var res = new List<ILocalizedObject>();

			var lst = GetAll(DefaultCulture.Value);
			foreach (var obj in lst)
			{
				var stored = Provider.GetLocalizedObject(culture, obj.Hash);
				if (stored == null)
					res.Add(Create(obj.Hash, culture.LCID, obj.Scope, obj.Text));
			}

			Save(culture, res.ToArray());
		}

		public ILocalizedObject Create(string hash, int localeId, string scope, string text)
		{
			return Provider.Create(hash, localeId, scope, text);
		}

		public ILocalizedObject Get(int key, bool ignoreDisabled = false)
		{
			return Provider.Get(key, ignoreDisabled);
		}

		public IEnumerable<ILocalizedObject> GetAll(CultureInfo culture)
		{
			return Provider.GetAll(culture);
		}

		public IEnumerable<Object> GetScriptResources(CultureInfo culture)
		{
			if (!GetCultures().Contains(culture))
				return Enumerable.Empty<ILocalizedObject>();

			return GetAll(culture).Where(x => (x.Scope != null) && (x.Scope.EndsWith("js") || x.Scope.EndsWith("htm")))
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

		public void Delete(CultureInfo culture, params ILocalizedObject[] list)
		{
			Provider.Delete(culture, list);
		}

		public void ClearDB(CultureInfo culture = null)
		{
			if (culture == null)			
				foreach (var item in GetCultures())
				{
					var disabled = GetAll(item).Where(obj => obj.IsDisabled());
					Delete(item, disabled.ToArray());
				}			
			else			
				Delete(culture, GetAll(culture).Where(obj => obj.IsDisabled()).ToArray());
		}

		public void Disable(CultureInfo culture, params ILocalizedObject[] list)
		{
			Provider.Disable(culture, list);
		}

		public void Save(CultureInfo culture, params ILocalizedObject[] list)
		{
			Provider.Save(culture, list);	
		}

		public void Import(params ILocalizedObject[] list)
		{
			var import = new Dictionary<int, List<ILocalizedObject>>();
			
			foreach (var obj in list)
			{
				if (obj.Hash == null)
					obj.Hash = GetHash(obj.Scope, obj.Text);
				
				var stored = Provider.GetLocalizedObject(new CultureInfo(obj.LocaleId), obj.Hash);
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
					var imported = Create(obj.Hash, obj.LocaleId, obj.Scope, obj.Text);
					imported.Translation = obj.Translation;

					if (!import.ContainsKey(obj.LocaleId))
						import.Add(obj.LocaleId, new List<ILocalizedObject>());
					
					import[obj.LocaleId].Add(imported);
				}

				// check object for default culture
				var def = Provider.GetLocalizedObject(DefaultCulture.Value, obj.Hash);
				if (def == null)
				{
					if (!import.ContainsKey(obj.LocaleId))
						import.Add(obj.LocaleId, new List<ILocalizedObject>());
					
					import[obj.LocaleId].Add(Create(obj.Hash, DefaultCulture.Value.LCID, obj.Scope, obj.Text));
				}
			}

			foreach (var localeId in import.Keys)
				Save(new CultureInfo(localeId), import[localeId].ToArray());
		}

		public string FormatScope(Type type)
		{
			var scope = type.Assembly.FullName.Split(',').Length > 0
					? type.FullName.Replace(type.Assembly.FullName.Split(',')[0], "~")
					: type.FullName;

			return scope.Replace(".", "/");
		}

		public IEnumerable<ILocalizedObject> GetLocalizedObjects(CultureInfo culture, string text, bool strict = true)
		{
			if (strict)
				return GetAll(culture).Where(x => x.Text.ToLowerInvariant() == text.ToLowerInvariant());
		
			return GetAll(culture).Where(x => x.Text.ToLowerInvariant().Contains(text.ToLowerInvariant()));
		}

		public void SetCulture(CultureInfo culture, string cookieName = LocalizationManager.CookieName)
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = culture;

			var cookie = HttpContext.Current.Response.Cookies[cookieName];

			if (cookie == null)
			{
				cookie = new HttpCookie(cookieName, culture.Name);
				HttpContext.Current.Response.Cookies.Add(cookie);
			}

			cookie.Expires = DateTime.Now.AddYears(1);
			cookie.Value = culture.Name;
		}

		public string GetCulture()
		{
			return Thread.CurrentThread.CurrentCulture.Name;
		}

		public IList<string> GetBrowserCultures()
		{
			var cultures = new List<string>();

			if (HttpContext.Current == null)
				return cultures;

			var browser = HttpContext.Current.Request.UserLanguages;
			if (browser != null)
				foreach (var culture in browser)
				{
					var lang = culture.IndexOf(';') > -1
						? culture.Split(';')[0]
						: culture;

					cultures.Add(lang);
				}

			return cultures.Distinct().ToList();
		}

		public string GetCultureFromCookie(string cookieName = LocalizationManager.CookieName)
		{
			var cookie = HttpContext.Current.Request.Cookies[cookieName] ?? HttpContext.Current.Request.Cookies[LocalizationManager.CookieName];

			if (cookie != null)
				return cookie.Value;

			return null;
		}

		public string GetCookieName(string prefix = null)
		{
			if (prefix == null)
				return LocalizationManager.CookieName;

			return string.Format("{0}-{1}", prefix, LocalizationManager.CookieName);
		}

		public string GetCultureFromQuery()
		{
			return HttpContext.Current.Request.QueryString[LocalizationManager.QueryParameter];
		}

		public void InsertScope(string path)
		{
			Provider.InsertScope(path);
		}

		public List<string> GetScope()
		{
			if (HttpContext.Current == null)
				return null;

			return HttpContext.Current.Items["localizationScope"] as List<string>;
		}

		public IEnumerable<CultureInfo> GetVisibleCultures()
		{
			return Provider.GetVisibleCultures();
		}

		public IEnumerable<ILocalizedObject> GetVisibleLocalizedObjects(CultureInfo culture)
		{
			return Provider.GetVisibleLocalizedObjects(culture);
		}

		private string GetHash(string scope, string text)
		{
			var hash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(scope.ToLowerInvariant() + text));
			var stringBuilder = new StringBuilder();

			for (var i = 0; i < hash.Length; i++)
				stringBuilder.Append(hash[i].ToString("x2"));

			return stringBuilder.ToString();
		}		
	}
}
