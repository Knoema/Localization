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

					var regex = new Regex("__(?:R|R2)\\([\"']{1}.*?(?:[\"']{1}|[}]{1})\\)", RegexOptions.Compiled);
					var script = regex.Replace(input.ReadToEnd(), delegate(Match match)
					{
						var resource = match.Value.Replace("__R", "$.R");
						resource = resource.Substring(0, resource.IndexOf('(') + 1) + "'" + _path + "', " + resource.Substring(resource.IndexOf('(') + 1);

						if (Path.GetExtension(_path) == ".htm")
						{
							if (resource.Contains("$.R2"))
								return "{{html " + resource + "}}";
							if (resource.Contains("$.R"))
								return "{{= " + resource + "}}";
						}
						return resource;
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
