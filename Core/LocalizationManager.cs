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

		private static object _lock = new object();
		public static ILocalizationRepository Repository { get; set; }

		private static object _lockBundlesCount = new object();
		private int _initialBundlesCount = 256;

		private static readonly LocalizationManager _instanse = new LocalizationManager();
		public static LocalizationManager Instance
		{
			get
			{
				return _instanse;
			}
		}

		private string _domain;
		private IEnumerable<string> _cultures;

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

			var hash = GetHash(scope.ToLowerInvariant() + text);

			// get object from cache...
			var obj = GetLocalizedObject(culture, hash);

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
						var stored = GetLocalizedObject(c, hash);
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

			var lowerScope = scope.ToLowerInvariant();
			var import = new List<ILocalizedObject>();
			var toAdd = phrases.Distinct();

			foreach (var phrase in toAdd)
			{
				var hash = GetHash(lowerScope + phrase);
				var obj = GetLocalizedObject(CultureInfo.CurrentCulture, hash);

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
				var stored = GetLocalizedObject(culture, obj.Hash);
				if (stored == null)
					res.Add(Create(obj.Hash, culture.LCID, obj.Scope, obj.Text));
			}

			Save(culture, res.ToArray());
		}

		public ILocalizedObject Create(string hash, int localeId, string scope, string text)
		{
			var result = Repository.Create();
			result.Hash = hash;
			result.LocaleId = localeId;
			result.Scope = scope;
			result.Text = text;

			return result;
		}

		public ILocalizedObject Get(int key, bool ignoreDisabled = false)
		{
			var obj = LocalizationCache.Get<ILocalizedObject>(key.ToString());
			if (obj == null)
			{
				obj = Repository.Get(key);
				LocalizationCache.Insert(key.ToString(), obj);
			}

			if (ignoreDisabled && obj.IsDisabled())
				return null;

			return obj;
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

		public IEnumerable<ILocalizedObject> GetAll(CultureInfo culture)
		{
			IEnumerable<ILocalizedObject> allObjects = null;
			var bundles = new LocalizedObjectList[_initialBundlesCount];
			
			for (int i = 0; i < _initialBundlesCount; i++)
			{
				bundles[i] = LocalizationCache.Get<LocalizedObjectList>(GetBundleName(culture, BundleIndexToHex(i)));
				
				if (bundles[i] == null)
				{
					if (allObjects == null)
						allObjects = Repository.GetAll(culture);

					bundles[i] = UpdateSingleBundle(culture, BundleIndexToHex(i), allObjects);
				}
			}

			var result = new LocalizedObjectList();

			foreach (var bundle in bundles)
				result.AddRange(bundle);

			return result.ToEnumerable();
		}

		private LocalizedObjectList UpdateSingleBundle(CultureInfo culture, string bundleHex, IEnumerable<ILocalizedObject> allObjects)
		{
			var bundleObjects = allObjects.Where(x => x.Hash.ToLowerInvariant().Substring(0, 2) == bundleHex);
			
			//only objects with correct Hash are loaded
			var newBundle = new LocalizedObjectList();
		
			foreach (var obj in bundleObjects)
				newBundle.Add(obj);

			LocalizationCache.Insert(GetBundleName(culture, bundleHex), newBundle);

			return newBundle;
		}

		private static string GetBundleName(CultureInfo culture, string bundleHex)
		{
			return string.Format("{0}_bundle0x{1}", culture.Name, bundleHex);
		}

		private static string BundleIndexToHex(int bundleIndex)
		{
			return bundleIndex.ToString("x2");
		}

		private static string GetBundleHex(string hash)
		{
			return hash.Substring(0, 2);
		}

		public LocalizedObjectList GetCachedListForHash(CultureInfo culture, string hash)
		{
			if (_initialBundlesCount <= 0)
				return null;

			return LocalizationCache.Get<LocalizedObjectList>(GetBundleName(culture, GetBundleHex(hash)));
		}

		public void SetSupportedCultures(IEnumerable<string> cultures)
		{
			_cultures = cultures;
		}

		public IEnumerable<string> GetSupportedCultures()
		{
			return _cultures;
		}

		public IEnumerable<CultureInfo> GetCultures()
		{
			return Repository.GetCultures().ToList();
		}

		public void Delete(CultureInfo culture, params ILocalizedObject[] list)
		{
			Repository.Delete(list);
			UpdateInCache(culture, list);
		}

		private void UpdateInCache(CultureInfo culture, ILocalizedObject[] list)
		{
			var bundlesToUpdate = new HashSet<string>();
			
			foreach (var obj in list)
				bundlesToUpdate.Add(GetBundleHex(obj.Hash));
			
			var allObjects = Repository.GetAll(culture);
			
			foreach (var bundle in bundlesToUpdate)
				UpdateSingleBundle(culture, bundle, allObjects);
		}

		public void ClearDB(CultureInfo culture = null)
		{
			if (culture == null)
			{
				foreach (var item in GetCultures())
				{
					var disabled = GetAll(item).Where(obj => obj.IsDisabled());
					Delete(item, disabled.ToArray());
				}
			}
			else
			{
				var disabled = Repository.GetAll(culture).Where(obj => obj.IsDisabled()).ToList();
				Delete(culture, disabled.ToArray());
			}

		}

		public void Disable(CultureInfo culture, params ILocalizedObject[] list)
		{
			foreach (var obj in list)
				obj.Disable();

			Repository.Save(list);
			UpdateInCache(culture, list);
		}

		public void Save(CultureInfo culture, params ILocalizedObject[] list)
		{
			Repository.Save(list);
			UpdateInCache(culture, list);
		}

		public void Import(params ILocalizedObject[] list)
		{
			var import = new Dictionary<int, List<ILocalizedObject>>();
			
			foreach (var obj in list)
			{
				if (!string.IsNullOrEmpty(_domain) && !obj.Scope.StartsWith(_domain))
					continue;

				if (obj.Hash == null)
					obj.Hash = GetHash(obj.Scope.ToLowerInvariant() + obj.Text);
				
				var stored = GetLocalizedObject(new CultureInfo(obj.LocaleId), obj.Hash);
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
				var def = GetLocalizedObject(DefaultCulture.Value, obj.Hash);
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
			else
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
			if (HttpContext.Current == null)
				return;
		
			var scope = HttpContext.Current.Items["localizationScope"] as List<string> ?? new List<string>();

			if (!scope.Contains(path))
				scope.Add(path);

			HttpContext.Current.Items["localizationScope"] = scope;
		}

		public List<string> GetScope()
		{
			if (HttpContext.Current == null)
				return null;

			var result = HttpContext.Current.Items["localizationScope"] as List<string>;
		
			if (!string.IsNullOrEmpty(_domain))
				result = result.Where(x => x.StartsWith(_domain, StringComparison.OrdinalIgnoreCase)).Select(x => x.ToLower().Replace(_domain.ToLower(), string.Empty)).ToList();

			return result.Select(x => x.StartsWith("/") ? x.Substring(1) : x).ToList();
		}

		public void SetDomain(string domain)
		{
			_domain = domain;
		}

		public string GetDomain()
		{
			return _domain;
		}

		private ILocalizedObject GetLocalizedObject(CultureInfo culture, string hash)
		{
			var lst = GetCachedListForHash(culture, hash);

			if (lst == null || !lst.Any())
				return GetAll(culture).FirstOrDefault(x => x.Hash == hash);
			
			return lst.FindItemByHash(hash);
		}

		private string GetHash(string text)
		{
			var hash = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(text));
			var stringBuilder = new StringBuilder();

			for (var i = 0; i < hash.Length; i++)
				stringBuilder.Append(hash[i].ToString("x2"));

			return stringBuilder.ToString();
		}		
	}
}
