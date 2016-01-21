
# How to install:
* Install from nuget https://www.nuget.org/packages/Knoema.Localization.Mvc
* Implement repository where localized strings will be stored. You can look at LocalizationRepository.cs class to have an idea.
* In Global.asax need to add localization route:
``` csharp 
routes.IgnoreRoute("_localization/{*route}");
```
* In web.config add base class for web pages to have an @R() extension:
``` xml 
 <system.web.webPages.razor>
    ...
    <pages pageBaseType="Knoema.Localization.Mvc.LocalizedWebViewPage">
    ...
  </system.web.webPages.razor>
```
* For models localization add this code into Application_Start() in Global.asax:
``` csharp 
ModelValidatorProviders.Providers.Clear();
ModelValidatorProviders.Providers.Add(new ValidationLocalizer());
ModelMetadataProviders.Current = new MetadataLocalizer();
``` 
* For localization if scripts add this to View:
``` csharp 
@RenderLocalizationIncludes(User.IsInRole("Admin"))	 
``` 
Admins will see admin tool, where they can translate strings or import/export them.
# How to use:

## Localization in cshtml files:
``` csharp 
<p>@R("Hello world!")</p>
``` 
With parameteres:
``` csharp 
<p>@R("Hello {0}!", username)</p>
``` 
##  Localization in cs files:
``` csharp 
"Hello world!".Resource(this)
``` 
##  Localization of models:
``` csharp 
[Localized]
public class SignInViewModel
{
	[Required(ErrorMessage = "Please provide your e-mail")]
	[Display(Name = "E-mail")]
	public string EMail { get; set; }

	[Required(ErrorMessage = "Please type your password")]
	public string Password { get; set; }
}
``` 
##  Localization in javascript  
``` javascript 
$.localize(text, scope);
``` 
For scope you can use path to script file e.g. "~/scripts/shared/site.js".



