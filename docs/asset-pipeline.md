# Asset pipeline

## Compile the plugin

Install the .NET 10 SDK. Copy local.props.example to local.props and set LethalCompanyDir and BepInExCoreDir to the game and BepInEx core directories. GameManagedDir defaults to Lethal Company_Data/Managed inside the game installation. These values can also be supplied as MSBuild properties.

~~~powershell
dotnet build GojoBracken.sln -c Release
~~~

The plugin targets netstandard2.1. The Release DLL is in src/GojoBracken/bin/Release/netstandard2.1.

## Author and export

Use Unity 2022.3.62f2. Follow the [Unity project guide](../UnityProject/README.md) for prefab authoring, native aura and production export. Record sources and permissions in the [release asset credits](release-assets.md).

For the private bundle, use Prepare-LocalModel.ps1 with the original model archive in Downloads. Use the GojoBracken editor menu to prepare and bake the standing model. Build the HDRP candidates, then build the voice bundle.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-HdrpMaterials.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-SoloLiveTestBundle.ps1 -CandidateProject $candidateProject -VoiceWav $voiceWav -VoiceCredits $voiceCredits
~~~

Set candidateProject to the generated u directory. Supply the local mono WAV and a credits text file. The run produces bundles/gojobracken-hdrp-material-check for material checks and live-test-voice-bundle/gojobracken for the private voice build. Capture-HdrpPreview.ps1 accepts the same candidate project for offline screenshots. See [packaging](packaging.md) for the ZIP.
