using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Knoema.Localization.EFProvider
{
	public class LocalizedObject: ILocalizedObject
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Key { get; set; }

		public int LocaleId {get; set;}

		public string Scope {get; set;}

		public string Text {get; set;}

		public string Hash {get; set;}

		public string Translation {get; set;}
	}
}
