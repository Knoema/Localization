using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace Knoema.Localization
{
	public class DebugLog
	{
		private static ILog _logger = null;

		public static ILog Logger
		{
			get
			{
				if (_logger == null)
					_logger = LogManager.GetLogger("Knoema.Localization");
				return _logger;
			}
		}
	}
}
