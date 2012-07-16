if exist NuGet. (
	del /S /Q NuGet 
) else (
	mkdir NuGet
)
nuget pack -Build Core\Knoema.Localization.Core.csproj -Symbols -OutputDirectory .\NuGet -Prop Configuration=Release
nuget pack -Build Mvc\Knoema.Localization.Mvc.csproj -Symbols -OutputDirectory .\NuGet -Prop Configuration=Release
