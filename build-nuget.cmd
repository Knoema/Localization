if exist NuGet. (
	del /S /Q NuGet 
) else (
	mkdir NuGet
)
nuget pack -Build Core\Knoema.Localization.Core.csproj -OutputDirectory .\NuGet -Prop Configuration=Release
nuget pack -Build EFProvider\Knoema.Localization.EFProvider.csproj -OutputDirectory .\NuGet -Prop Configuration=Release
nuget pack -Build Mvc\Knoema.Localization.Mvc.csproj -OutputDirectory .\NuGet -Prop Configuration=Release
