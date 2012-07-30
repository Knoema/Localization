using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Cassette;

namespace Knoema.Localization.Cassette
{
	public class LocalizationResourceTransformer : IAssetTransformer
	{
		private string _path;
		public LocalizationResourceTransformer(string path)
		{
			_path = path;
		}

		public Func<Stream> Transform(Func<Stream> openSourceStream, IAsset asset)
		{
			return delegate
			{
				using (var input = new StreamReader(openSourceStream()))
				{
					var stream = new MemoryStream();
					var writer = new StreamWriter(stream);

					var regex = new Regex("__R\\([\"']{1}.*?[\"']{1}\\)", RegexOptions.Compiled);
					var script = regex.Replace(input.ReadToEnd(), delegate(Match match)
					{
						var resource = match.Value
							.Remove(match.Value.LastIndexOf(")"), 1)
							.Replace("__R", "$.localize");

						resource = string.Format("{0}, \"{1}\")", resource, _path);

						return Path.GetExtension(_path) == ".htm" ? "${" + resource + "}" : resource;
					});

					writer.Write(script);
					writer.Flush();
					stream.Position = 0;

					return stream;
				}
			};
		}
	}
}
