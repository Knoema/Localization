using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace Knoema.Localization.Web
{
	public class LocalizationHandler : IHttpHandler
	{
		private static readonly LocalizationManager _manager =  LocalizationManager.Instance;

		public static string RenderIncludes(bool admin, List<string> scope)
		{
			var include = GetResource(GetResourcePath("include.html"));

			if(admin)
			{
				include += GetResource(GetResourcePath("include-admin.html"));			
				include = include.Replace("{localizationScope}",
					scope == null 
						? string.Empty 
						: "'" + string.Join("','", scope) + "'"
				);
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

			return include.Replace("{appPath}", GetAppPath());
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
			string response;

			if (context.Request.Url.AbsolutePath.Contains("/_localization/api"))
				response = Api(
						context,
						context.Request.Url.Segments[context.Request.Url.Segments.Length - 1],
						context.Request.Params);
			else
				response = R(
						context,
						GetResourcePath(context.Request.AppRelativeCurrentExecutionFilePath));

			context.Response.Write(response);
		}

		private string Api(HttpContext context, string endpoint, NameValueCollection query)
		{
			var serializer = new JavaScriptSerializer();
			var response = string.Empty;

			switch (endpoint)
			{
				case "cultures":				
					response = serializer.Serialize(
						_manager.GetCultures().Where(x => x.LCID != DefaultCulture.Value.LCID).Select(x => x.Name));				
					break;

				case "tree":
					var resources = _manager.GetAll(
						string.IsNullOrEmpty(query["culture"])
							? DefaultCulture.Value
							: new CultureInfo(query["culture"])
						);
					response = serializer.Serialize(
						GetTree(resources).Where(x => x.IsRoot));
					break;

				case "table":	
					response = serializer
						.Serialize(
							_manager.GetAll(new CultureInfo(query["culture"]))
									.Where(x => (x.Scope != null) && x.Scope.StartsWith(query["scope"], StringComparison.InvariantCultureIgnoreCase)));

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
					var filepath = Path.GetTempFileName();
					var data = _manager.GetAll(new CultureInfo(query["culture"])).Select(x =>
						new
						{
							LocaleId = x.LocaleId,
							Hash = x.Hash,
							Scope = x.Scope,
							Text = x.Text,
							Translation = x.Translation
						});

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
						response = serializer.Serialize(
							_manager.GetLocalizedObjects(
								new CultureInfo(query["culture"]), query["text"])
								.Where(x => !string.IsNullOrEmpty(x.Translation))
								.Select(x => x.Translation)
								.Distinct()
						);
					}
					catch (CultureNotFoundException) { }
					break;
				case "search":
					try
					{
						response = serializer.Serialize(
							_manager.GetLocalizedObjects(
								new CultureInfo(query["culture"]), query["text"], false)
						);
					}
					catch (CultureNotFoundException) { }
					break;
			}

			return response;
		}

		static bool IgnoreLocalization()
		{
			if (LocalizationManager.Repository == null)
				return true;
			
			var current = LocalizationManager.Instance.GetCulture();

			if (current.IsDefault())
				return true;

			var cultures = LocalizationManager.Instance.GetCultures();
			if (cultures.Count() > 0 && !cultures.Contains(new CultureInfo(current)))
				return true;
				
			return false;		
		}
		
		private string R(HttpContext context, string path)
		{
			var response = context.Response;
			var output = string.Empty;

			switch (Path.GetExtension(path))
			{
				case ".js":
					response.ContentType = "application/javascript";
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

			var cache = response.Cache;
			cache.SetCacheability(System.Web.HttpCacheability.Public);
			cache.SetExpires(DateTime.Now.AddDays(7));
			cache.SetValidUntilExpires(true);	
			return output;
		}

		static string GetJsFile(string path)
		{
			var current = LocalizationManager.Instance.GetCulture();
			var output = GetResource(path).Replace("{appPath}", GetAppPath()).Replace("{currentCulture}", current);

			if (IgnoreLocalization())				
				return output.Replace("{ignoreLocalization}", "true");			
		
			var resources = new JavaScriptSerializer().Serialize(_manager.GetScriptResources(new CultureInfo(current)));
			
			return output.Replace("{data}", HttpUtility.JavaScriptStringEncode(resources)).Replace("{ignoreLocalization}", "false");
		}
		
		private static string GetResourceHash(string path)
		{
			var hash = string.Empty;
			
			switch (Path.GetExtension(path).ToLowerInvariant())
			{
				case ".js":				
					var content = "1.51" + 
					 (path.EndsWith("jquery-localize.js") && LocalizationManager.Repository != null
						? GetJsFile(path)
						: GetStreamHash(GetResourceStream(path))); 

					hash = GetStringHash(content);										
					break;
				case ".css":
					hash = GetStreamHash(GetResourceStream(path));
					break;
				default:
					break;
			}

			return hash;
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
				case ".png": case ".gif":					
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

		private static string GetStringHash(string text)
		{
			return GetHash(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(text)));
		}

		private static string GetStreamHash(Stream stream)
		{
			return GetHash(new MD5CryptoServiceProvider().ComputeHash(stream));
		}

		private static string GetHash(byte[] bytes)
		{
			var stringBuilder = new StringBuilder();

			for (var i = 0; i < bytes.Length; i++)
				stringBuilder.Append(bytes[i].ToString("x2"));

			return stringBuilder.ToString();
		}

		private List<TreeNode> GetTree(IEnumerable<ILocalizedObject> lst)
		{
			var tree = new List<TreeNode>();

			foreach (var obj in lst)
			{
				if (obj.Scope == null)
					continue;

				var labels = obj.Scope.Split('/');

				for (int i = 0; i < labels.Length; i++)
				{
					var path = string.Empty;
					for (int j = 0; j <= i; j++)
						path += labels[j] + "/";

					path = path.Remove(path.LastIndexOf("/"));

					var node = tree.FirstOrDefault(x => x.Scope.ToLowerInvariant() == path.ToLowerInvariant());
					if (node == null)
					{
						node = new TreeNode(labels[i], path, i == 0, true);						
						tree.Add(node);
					}

					if (i == labels.Length - 1)
						node.Translated = !string.IsNullOrEmpty(obj.Translation) && node.Translated;	

					if (i > 0)
					{
						var parent = tree.FirstOrDefault(x => x.Scope.ToLowerInvariant() == path.Remove(path.LastIndexOf("/")).ToLowerInvariant());
						if (!parent.Children.Contains(node))						
							parent.Children.Add(node);						
					}
				}
			}

			return tree;
		}
	}

	public class TreeNode
	{
		public string Label { get; set; }
		public string Scope { get; set; }
		public bool IsRoot { get; set; }
		public bool Translated { get; set; }
		public List<TreeNode> Children { get; set; }

		public TreeNode(string label, string scope, bool isRoot, bool translated)
		{
			Label = label;
			Scope = scope;
			IsRoot = isRoot;
			Translated = translated;
			Children = new List<TreeNode>();
		}

		public override string ToString()
		{
			return Scope;
		}
	}
}
