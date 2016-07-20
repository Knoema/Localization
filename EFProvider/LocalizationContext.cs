using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;

namespace Knoema.Localization.EFProvider
{
	public class LocalizationContext: DbContext
	{
		public IDbSet<LocalizedObject> Objects { get; set; }

		public LocalizationContext()
		{
		}

		public LocalizationContext(string nameOrConnectionString) :
			base(nameOrConnectionString)
		{
		}

		public LocalizationContext(DbConnection existingConnection, bool contextOwnsConnection) :
			base(existingConnection, contextOwnsConnection)
		{
		}
	}
}
