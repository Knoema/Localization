using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Knoema.Localization.Mvc;
using Knoema.Localization;

namespace Sample
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication : System.Web.HttpApplication
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}

		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			routes.IgnoreRoute("_localization/{*route}");

			routes.MapRoute(
				"Default", // Route name
				"{controller}/{action}/{id}", // URL with parameters
				new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
			);

		}

		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();

			// Use LocalDB for Entity Framework by default
			Database.DefaultConnectionFactory = new SqlConnectionFactory(@"Data Source=(localdb)\v11.0; Integrated Security=True; MultipleActiveResultSets=True");

			RegisterGlobalFilters(GlobalFilters.Filters);
			RegisterRoutes(RouteTable.Routes);
			
			// initialize repository
			LocalizationManager.Repository = new Knoema.Localization.EFProvider.LocalizationRepository();

			// uncomment this to show strings only for specific scope
			//LocalizationManager.Instance.SetDomain("~/Views");
			
			// uncomment this to show strings only for cultures
			//LocalizationManager.Instance.SetCultures(new List<string>() { "ru-ru" });
			
			// initialize cache provider
			LocalizationCache.Initialize(new HttpCache());

			// configure localization of models
			ModelValidatorProviders.Providers.Clear();
			ModelValidatorProviders.Providers.Add(new ValidationLocalizer());
			ModelMetadataProviders.Current = new MetadataLocalizer();
		}
	}
}