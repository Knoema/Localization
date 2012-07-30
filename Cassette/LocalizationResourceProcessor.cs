using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cassette.BundleProcessing;
using Cassette.Configuration;
using Cassette.HtmlTemplates;
using Cassette.Scripts;

namespace Knoema.Localization.Cassette
{
	public class LocalizationResourceProcessor : IBundleProcessor<ScriptBundle>, IBundleProcessor<HtmlTemplateBundle>
	{
		public void Process(ScriptBundle bundle, CassetteSettings settings)
		{
			foreach (var asset in bundle.Assets)
				asset.AddAssetTransformer(new LocalizationResourceTransformer(asset.SourceFile.FullPath));
		}

		public void Process(HtmlTemplateBundle bundle, CassetteSettings settings)
		{
			foreach (var asset in bundle.Assets)
				asset.AddAssetTransformer(new LocalizationResourceTransformer(asset.SourceFile.FullPath));
		}
	}
}
