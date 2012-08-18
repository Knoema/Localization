using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Web.Mvc;

namespace Knoema.Localization.Mvc
{
	public class ValidationLocalizer : DataAnnotationsModelValidatorProvider
	{
		private ResourceManager _resourceManager;
		public ValidationLocalizer()
		{
			var resourceStringType = typeof(RequiredAttribute).Assembly.GetType("System.ComponentModel.DataAnnotations.Resources.DataAnnotationsResources");
			if (resourceStringType != null)
				_resourceManager = new ResourceManager(resourceStringType);
		}

		protected override IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, ControllerContext context,	IEnumerable<Attribute> attributes)
		{			
			if (metadata.ContainerType == null)
				return base.GetValidators(metadata, context, attributes);			

			if (CultureInfo.CurrentCulture.LCID == DefaultCulture.Value.LCID)
				return base.GetValidators(metadata, context, attributes);	

			if (metadata.ContainerType.GetCustomAttributes(typeof(LocalizedAttribute), true).Length == 0)
				return base.GetValidators(metadata, context, attributes);

			var attrs = attributes.OfType<ValidationAttribute>();
			if (attrs.Count() > 0)
			{
				var list = new List<Attribute>();
				foreach (var a in attrs)
				{
					var attr = CopyAttribute(a);

					var text = metadata.PropertyName + "_" + attr.GetType().Name;
					var obj = LocalizationManager.Instance.FormatScope(metadata.ContainerType);

					attr.ErrorMessage = LocalizationManager.Instance.Translate(obj, text);
					if (string.IsNullOrEmpty(attr.ErrorMessage))
						attr.ErrorMessage = a.ErrorMessage ?? GetValidationString(attr.GetType());

					list.Add(attr);
				}
				return base.GetValidators(metadata, context, list);	
			}

			return base.GetValidators(metadata, context, attributes);
		}

		private ValidationAttribute CopyAttribute(ValidationAttribute attribute)
		{
			ValidationAttribute result = null;

			if (attribute is RangeAttribute)
			{
				var attr = (RangeAttribute)attribute;
				result = new RangeAttribute((double)attr.Minimum, (double)attr.Maximum);
			}

			if (attribute is RegularExpressionAttribute)
			{
				var attr = (RegularExpressionAttribute)attribute;
				result = new RegularExpressionAttribute(attr.Pattern);
			}

			if (attribute is RequiredAttribute)			
				result = new RequiredAttribute();
			
			if (attribute is StringLengthAttribute)
			{
				var attr = (StringLengthAttribute)attribute;
				result = new StringLengthAttribute(attr.MaximumLength)
				{
					MinimumLength = attr.MinimumLength
				};
			}

			if (attribute is CompareAttribute)
			{
				var attr = (CompareAttribute)attribute;
				result = new CompareAttribute(attr.OtherProperty);
			}

			if (attribute is DataTypeAttribute)
			{
				var attr = (DataTypeAttribute)attribute;
				result = new DataTypeAttribute(attr.DataType);
			}
			return result;
		}		

		private string GetValidationString(Type type)
		{
			return _resourceManager == null
					   ? null
					   : _resourceManager.GetString(string.Format("{0}_ValidationError", type.Name));
		}		
	}
}