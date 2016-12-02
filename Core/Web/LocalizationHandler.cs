using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Microsoft.Ajax.Utilities;

namespace Knoema.Localization.Web
{
	public class LocalizationHandler : IHttpHandler
	{
		private static readonly LocalizationManager _manager = LocalizationManager.Instance;

		public static string RenderIncludes(bool admin, List<string> scope)
		{
			var include = GetResource(GetResourcePath("include.html"));

			if (admin)
			{
				include += GetResource(GetResourcePath("include-admin.html"));
				include = include.Replace("{root}", _manager.GetRoot());
				include = include.Replace("{scope}", scope == null || scope.Count == 0 ? null : "'" + string.Join("','", scope) + "'");
			}
			
			var names = typeof(LocalizationHandler).Assembly.GetManifestResourceNames();

			foreach (var n in names)
			{
				var ext = Path.GetExtension(n);
				if (ext == ".js" || ext == ".css")
				{
					var p = n.Split('.');
					include = include.Replace("{" + p[p.Length - 2] + "}", GetResourceHash(n));
				}
			}

			include = include.Replace("{initialCulture}", LocalizationManager.Instance.GetCulture());

			return include.Replace("{appPath}", GetAppPath());
		}

		public static string JqueryLocalizeUrl()
		{
			return "{appPath}/_localization/jquery-localize.{hash}.js".Replace("{appPath}", GetAppPath()).Replace("{hash}", GetResourceHash("jquery-localize.js"));
		}

		public static string InitialCultureInputElementStr()
		{
			return "<input type=\"hidden\" id=\"initialCulture\" value=\"" + LocalizationManager.Instance.GetCulture() + "\" />";
		}

		public bool IsReusable
		{
			get
			{
				return true;
			}
		}

		static string GetAppPath()
		{
			return HttpContext.Current.Request.ApplicationPath == "/"
				? string.Empty
				: HttpContext.Current.Request.ApplicationPath;
		}

		public void ProcessRequest(HttpContext context)
		{
			if (context.Request.Url.AbsolutePath.Contains("/_localization/api"))			
				context.Response.Write(Api(context, context.Request.Url.Segments[context.Request.Url.Segments.Length - 1], context.Request.Params));
			else
				R(context,GetResourcePath(context.Request.AppRelativeCurrentExecutionFilePath));
		}

		private string Api(HttpContext context, string endpoint, NameValueCollection query)
		{
			var serializer = new JavaScriptSerializer() { MaxJsonLength = 16777216 };
			var response = string.Empty;

			switch (endpoint)
			{
				case "cultures":
					response = serializer.Serialize(_manager.GetCultures().Where(x => x.LCID != DefaultCulture.Value.LCID).Select(x => x.Name));
					break;

				case "tree":
					response = serializer.Serialize(GetTree(
						_manager.GetLocalizedObjects(string.IsNullOrEmpty(query["culture"]) ? DefaultCulture.Value : new CultureInfo(query["culture"]))
					));
					break;

				case "table":
					response = serializer.Serialize(_manager.GetLocalizedObjects(new CultureInfo(query["culture"]))
						.Where(x => x.Scope != null && x.Scope.StartsWith(query["scope"], StringComparison.InvariantCultureIgnoreCase)));
					break;

				case "edit":

					if (!string.IsNullOrEmpty(query["id"]))
					{
						var key = 0;
						int.TryParse(query["id"], out key);

						if (key != 0)
						{
							var edit = _manager.Get(key);
							if (edit != null)
							{
								edit.Translation = query["translation"];
								_manager.Save(edit);
							}
						}
					}
					break;

				case "delete":
					var delete = _manager.Get(int.Parse(query["id"]));
					if (delete != null)
						_manager.Delete(delete);
					break;

				case "cleardb":
					_manager.ClearDB();
					break;

				case "disable":
					var disable = _manager.Get(int.Parse(query["id"]));
					if (disable != null)
					{
						_manager.Disable(disable);
						response = disable.Translation;
					}
					break;

				case "create":
					try
					{
						var culture = new CultureInfo(query["culture"]);
						_manager.CreateCulture(new CultureInfo(query["culture"]));
						response = culture.Name;
					}
					catch (CultureNotFoundException) { }
					break;

				case "export":

					var res = new List<ILocalizedObject>();
					var objects = _manager.GetLocalizedObjects(new CultureInfo(query["culture"]));
					var scope = query["scope"];

					if (!string.IsNullOrEmpty(scope))
					{
						var scopes = scope.Split(',').Select(s => s.Trim());

						foreach (var s in scopes)
							res.AddRange(objects.Where(obj => obj.Scope.ToLowerInvariant().Contains(s.ToLowerInvariant())));
					}
					else
						res.AddRange(objects);

					var root = _manager.GetRoot();

					var data = res.Select(x =>
						new
						{
							LocaleId = x.LocaleId,
							Hash = x.Hash,
							Scope = string.Format("{0}{1}", root, x.Scope),
							Text = x.Text,
							Translation = x.Translation
						});

					var filepath = Path.GetTempFileName();

					File.WriteAllText(filepath, serializer.Serialize(data));

					context.Response.ContentType = "application/json";
					context.Response.AppendHeader("Content-Disposition", "attachment; filename=" + query["culture"] + ".json");
					context.Response.TransmitFile(filepath);
					context.Response.End();

					break;

				case "import":
					if (context.Request.Files.Count > 0)
					{
						for (int i = 0; i < context.Request.Files.Count; i++)
							using (var reader = new StreamReader(context.Request.Files[i].InputStream))
							{
								var json = reader.ReadToEnd();
								_manager.Import(
									serializer.Deserialize<IEnumerable<Repository.LocalizedObject>>(json).ToArray());
							}
					}
					break;
				case "bulkimport":
					if (context.Request.Files.Count > 0)
					{
						var lst = new List<ILocalizedObject>();

						for (int i = 0; i < context.Request.Files.Count; i++)
							using (var reader = new StreamReader(context.Request.Files[i].InputStream))
							{
								var row = 0;

								while (!reader.EndOfStream)
								{
									row++;

									var line = reader.ReadLine();

									if (row == 1)
										continue;

									if (!string.IsNullOrEmpty(line))
									{
										var values = line.Split('\t');

										if (values.Length == 4)
											lst.Add(new Repository.LocalizedObject()
											{
												Scope = values[0],
												Text = values[1],
												Translation = values[2],
												LocaleId = int.Parse(values[3])
											});
									}

								}
							}
						_manager.Import(lst.ToArray());

					}
					break;
				case "push":
					try
					{
						if (string.IsNullOrEmpty(query["scope"]) || string.IsNullOrEmpty(query["text"]))
							BadRequest(context);

						else if (!LocalizationManager.Instance.GetCulture().IsDefault())
						{
							_manager.Translate(query["scope"], query["text"]);
							response = "Success: \"" + query["text"] + "\" was added to scope \"" + query["scope"] + "\"";
						}
						else
							response = "Success: default culture, localization was not added";
					}
					catch (Exception e)
					{
						response = "Error: " + e.Message;
					}
					break;
				case "hint":
					try
					{
						response = serializer.Serialize(_manager.GetLocalizedObjects(new CultureInfo(query["culture"]), query["text"])
							.Where(x => !string.IsNullOrEmpty(x.Translation))
							.Select(x => x.Translation)
							.Distinct());
					}
					catch (CultureNotFoundException) { }
					break;
				case "search":
					try
					{
						response = serializer.Serialize(_manager.GetLocalizedObjects(new CultureInfo(query["culture"]), query["text"], false));
					}
					catch (CultureNotFoundException) { }
					break;
			}

			return response;
		}

		static bool IgnoreLocalization()
		{
			if (LocalizationManager.Provider == null)
				return true;

			var current = LocalizationManager.Instance.GetCulture();

			if (current.IsDefault())
				return true;

			var cultures = LocalizationManager.Instance.GetCultures();
			if (cultures.Count() > 0 && !cultures.Contains(new CultureInfo(current)))
				return true;

			return false;
		}

		private void R(HttpContext context, string path)
		{
			var response = context.Response;
			var output = string.Empty;

			switch (Path.GetExtension(path))
			{
				case ".js":

					response.ContentType = "application/javascript";

					if (IsGZipSupported())
					{
						var encoding = HttpContext.Current.Request.Headers["Accept-Encoding"];

						if (encoding.Contains("gzip"))
						{
							response.Filter = new GZipStream(response.Filter, CompressionMode.Compress);
							response.AppendHeader("Content-Encoding", "gzip");
						}
						else
						{
							response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress);
							response.AppendHeader("Content-Encoding", "deflate");
						}
					}

					output = GetJsFile(path);

					break;
				case ".css":
					response.ContentType = "text/css";
					output = GetResource(path);
					break;
				case ".png":
					response.ContentType = "image/png";
					Bitmap.FromStream(GetResourceStream(path)).Save(context.Response.OutputStream, ImageFormat.Png);
					break;
				case ".gif":
					response.ContentType = "image/gif";
					Bitmap.FromStream(GetResourceStream(path)).Save(context.Response.OutputStream, ImageFormat.Gif);
					break;
				case ".html":
					response.ContentType = "text/html";
					output = GetResource(path);
					break;
				default:
					NotFound(context);
					break;
			}

			var hash = GetHash(output);

			if (ResourceNotModified(context, hash))
				return;

			response.Write(output);
			CacheResponse(response, hash);
		}

		private static bool ResourceNotModified(HttpContext context, string hash)
		{
			if (context.Request.Headers["If-None-Match"] == hash)
			{
				SendNotModified(context.Response, hash);
				return true;
			}

			return false;
		}

		private static void SendNotModified(HttpResponse response, string hash)
		{
			CacheResponse(response, hash);
			response.StatusCode = 304;
			response.SuppressContent = true;
		}

		private static void CacheResponse(HttpResponse response, string hash)
		{
			response.Cache.SetCacheability(System.Web.HttpCacheability.ServerAndPrivate);
			response.Cache.SetExpires(DateTime.Now.AddDays(7));
			response.Cache.SetValidUntilExpires(true);
			response.Cache.SetETag(hash);
		}

		private static string GetJsFile(string path)
		{
			var current = LocalizationManager.Instance.GetCulture();
			var output = GetResource(path).Replace("{appPath}", GetAppPath()).Replace("{currentCulture}", current);

			if (path.EndsWith("jquery-localize.js"))
			{
				if (IgnoreLocalization())
					output = output.Replace("{ignoreLocalization}", "true");
				else
				{
					var resources = new JavaScriptSerializer().Serialize(_manager.GetScriptResources(new CultureInfo(current)));
					output = output.Replace("{data}", HttpUtility.JavaScriptStringEncode(resources)).Replace("{ignoreLocalization}", "false");
				}
			}

			if (!HttpContext.Current.IsDebuggingEnabled)
				return MinifyJsFile(output);

			return output;
		}

		private static string MinifyJsFile(string source)
		{
			var minifier = new Minifier();
			var result = minifier.MinifyJavaScript(source);

			if (minifier.ErrorList.Any())
				result += string.Join(", ", minifier.ErrorList);

			return result;
		}

		private static bool IsGZipSupported()
		{
			var acceptEncoding = HttpContext.Current.Request.Headers["Accept-Encoding"];

			if (!string.IsNullOrEmpty(acceptEncoding) && (acceptEncoding.Contains("gzip") || acceptEncoding.Contains("deflate")))
				return true;

			return false;
		}

		private static string GetResourceHash(string path)
		{
			var content = string.Empty;

			switch (Path.GetExtension(path).ToLowerInvariant())
			{
				case ".js":
					content = GetJsFile(path);
					break;
				case ".css":
					content = GetResource(path);
					break;
				default:
					break;
			}

			return GetHash(	content);
		}

		private static string GetResourcePath(string filename)
		{
			var path = "Knoema.Localization.Resources.";

			switch (Path.GetExtension(filename).ToLowerInvariant())
			{
				case ".js":
					path = path + "Js.";
					break;
				case ".css":
					path = path + "Css.";
					break;
				case ".png":
				case ".gif":
					path = path + "Img.";
					break;
				case ".html":
					path = path + "Html.";
					break;
				default:
					break;
			}

			var p = filename.Split('.');

			return p.Length > 2
				? path + Path.GetFileName(filename).Replace("." + p[p.Length - 2], string.Empty)
				: path + Path.GetFileName(filename);
		}

		private static Stream GetResourceStream(string path)
		{
			return typeof(LocalizationHandler).Assembly.GetManifestResourceStream(path);
		}

		private static string GetResource(string path)
		{
			var result = string.Empty;

			using (var stream = GetResourceStream(path))
			{
				if (stream != null)
					using (var reader = new StreamReader(stream))
					{
						result = reader.ReadToEnd();
					}
			}

			return result;
		}

		private void NotFound(HttpContext context)
		{
			context.Response.StatusCode = 404;
			context.Response.ContentType = "text/plain";
		}

		private void BadRequest(HttpContext context)
		{
			context.Response.StatusCode = 400;
			context.Response.ContentType = "text/plain";
		}

		private static string GetHash(string text)
		{
			return GetHash(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(text)));
		}

		private static string GetHash(byte[] bytes)
		{
			var stringBuilder = new StringBuilder();

			for (var i = 0; i < bytes.Length; i++)
				stringBuilder.Append(bytes[i].ToString("x2"));

			return stringBuilder.ToString();
		}

		private ScopeEntryCollection GetTree(IEnumerable<ILocalizedObject> lst)
		{
			var result = new ScopeEntryCollection();
			var scope = lst.Select(x => x.Scope).Distinct().OrderBy(x => x).ToList();
			var scopeWithoutTranslation = lst.Where(x => x.Translation == null).Select(x => x.Scope).Distinct().ToList();

			foreach (var item in scope)
				result.AddEntry(item, scopeWithoutTranslation.Contains(item));

			return result;
		}
	}
}
