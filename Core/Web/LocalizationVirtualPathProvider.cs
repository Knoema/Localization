using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace Knoema.Localization.Web
{
	public class LocalizationVirtualPathProvider : VirtualPathProvider
	{
		private string _namespace;
		public string Namespace
		{
			get 
			{
				if (string.IsNullOrEmpty(_namespace))
					_namespace = System.IO.Path.GetFileNameWithoutExtension(typeof(LocalizationVirtualPathProvider).Assembly.ManifestModule.Name) + ".";

				return _namespace;
			}
		}

		public override bool FileExists(string virtualPath)
		{
			if (IsEmbedPath(virtualPath))
				return IsEmbeddedResource(virtualPath);
			else
				return base.FileExists(virtualPath);
		}

		public override VirtualFile GetFile(string virtualPath)
		{
			if (IsEmbedPath(virtualPath))
			{
				var stream = GetResourseStream(virtualPath);
				if (stream == null)
					return null;
				else
					return new EmbeddedFile(virtualPath, FixView(virtualPath, stream));
			}
			else
				return base.GetFile(virtualPath);
		}
		
		public override System.Web.Caching.CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
		{
			if (IsEmbedPath(virtualPath))
				return null;
			else
				return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
		}

		private bool IsEmbeddedResource(string virtualPath)
		{
			var path = Path(virtualPath);

			var names = typeof(LocalizationVirtualPathProvider).Assembly
				.GetManifestResourceNames()
				.Select(x => x.Replace(Namespace, string.Empty));

			return names.Any(x => x.Equals(path, StringComparison.OrdinalIgnoreCase));
		}

		private Stream GetResourseStream(string virtualPath)
		{
			var assembly = typeof(LocalizationVirtualPathProvider).Assembly;
			var path = Path(virtualPath);			
			var names = assembly.GetManifestResourceNames();

			foreach (var n in names)
			{				
				var shortName = n.Replace(Namespace, string.Empty);
				if (shortName.Equals(path, StringComparison.OrdinalIgnoreCase))
					return assembly.GetManifestResourceStream(n);				
			}

			return null;
		}

		private string Path(string virtualPath)
		{
			return VirtualPathUtility.ToAbsolute(virtualPath).ToLowerInvariant().Trim('/').Replace('/', '.');
		}

		private bool IsEmbedPath(string virtualPath)
		{
			return VirtualPathUtility.ToAbsolute(virtualPath).ToLowerInvariant().Contains("/localization");
		}

		public Stream FixView(string virtualPath, Stream stream)
		{
			using (stream)
			{
				var view = string.Empty;
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					view = reader.ReadToEnd();
				}

				var viewStream = new MemoryStream();
				var writer = new StreamWriter(viewStream, Encoding.UTF8);

				var model = string.Empty;

				var start = view.IndexOf("@model");
				if (start != -1)
				{
					var end = view.IndexOfAny(new[] { '\r', '\n' }, start);
					model = view.Substring(start, end - start);
					view = view.Remove(0, end);
				}

				writer.WriteLine("@using System.Web.Mvc");
				writer.WriteLine("@using System.Web.Mvc.Ajax");
				writer.WriteLine("@using System.Web.Mvc.Html");
				writer.WriteLine("@using System.Web.Routing");

				var basepage = "@inherits System.Web.Mvc.WebViewPage";

				if (!string.IsNullOrEmpty(model))
				{
					if (model == "@model object")
						writer.WriteLine(basepage + "<dynamic>");
					else
						writer.WriteLine(basepage + "<" + model.Substring(7) + ">");
				}
				else
					writer.WriteLine(basepage);

				writer.Write(view);
				writer.Flush();

				viewStream.Position = 0;
				return viewStream;
			}
		}

		private class EmbeddedFile : VirtualFile
		{
			private Stream _stream;

			public EmbeddedFile(string path, Stream stream)
				: base(path)
			{
				_stream = stream;
			}		

			public override Stream Open()
			{
				return _stream;
			}
		}
	}
}
