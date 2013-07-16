using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Globalization;

namespace Knoema.Localization.Mvc
{
	public class MetadataLocalizer : DataAnnotationsModelMetadataProvider
	{
		protected override ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type containerType,
														Func<object> modelAccessor, Type modelType, string propertyName)
		{
			var metadata = base.CreateMetadata(attributes, containerType, modelAccessor, modelType, propertyName);

			if (containerType == null || propertyName == null)
				return metadata;

			if (containerType.GetCustomAttributes(typeof(LocalizedAttribute), true).Length == 0)
				return metadata;

			if (CultureInfo.CurrentCulture.IsDefault())
				return metadata;
		
			var obj = LocalizationManager.Instance.FormatScope(containerType);
			var translation = LocalizationManager.Instance.Translate(obj, propertyName + "_DisplayName");

			metadata.DisplayName = string.IsNullOrEmpty(translation) 
				? metadata.DisplayName ?? propertyName 
				: translation;						

			// process other metadata if it needed
			// Watermark, Description, NullDisplayText, ShortDisplayName

			return metadata;
		}

	}
}