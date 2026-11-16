# Asset pipeline

## Compile the plugin

Install the .NET 10 SDK. Copy local.props.example to local.props. Set LethalCompanyDir to the game directory and BepInExCoreDir to the BepInEx core directory. GameManagedDir defaults to Lethal Company_Data/Managed inside the game installation. You can also pass these values as MSBuild properties.

~~~powershell
dotnet build GojoBracken.sln -c Release
~~~

The plugin targets netstandard2.1. The Release DLL is in src/GojoBracken/bin/Release/netstandard2.1.

## Author and export

Use Unity 2022.3.62f2. The [Unity project guide](../UnityProject/README.md) explains how to author prefabs, build the native aura, and export the production bundle. Record sources and permissions in the [release asset credits](release-assets.md).

For the private bundle, use Prepare-LocalModel.ps1 with the original model archive in Downloads. Use the GojoBracken editor menu to prepare and bake the standing model. Build the HDRP candidates, then build the voice bundle.

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-HdrpMaterials.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-SoloLiveTestBundle.ps1 -CandidateProject $candidateProject -VoiceWav $voiceWav -VoiceCredits $voiceCredits
~~~

Point candidateProject to the generated u directory and supply a local mono WAV and a credits text file. The voice build writes live-test-voice-bundle/gojobracken. For a private music test, pass the local MP3 with -MusicMp3 and credit its source. That build writes live-test-music-bundle/gojobracken. To test constant 2× playback gain, pass -UniformVoiceBoost with a voice WAV at its original level.

Use bundles/gojobracken-hdrp-material-check for material checks. Capture-HdrpPreview.ps1 takes the same candidate project and saves screenshots offline. Follow the [packaging guide](packaging.md) to build the ZIP.

When exporting assets, put the credits, source links, permissions, and changes in CREDITS.txt. The Unity exporter embeds that text in the bundle and writes ASSET-CREDITS.txt for the [package build](packaging.md).

Source archive SHA-256 for the model is B0969E72C9BD3C3D0D48BFF1DB6A9033297D8CF128552F51C66C8B355985F8A9. Current voice source SHA-256 is 80E2F04FFB0058B7EB46A0EB4E727392660FCD7370F6EDD807F09C4296610E36.

For the release voice mix, the mono PCM file uses 6.16x whole-clip gain (+15.79 dB), with an estimated RMS of -15.65 dBFS before playback gain. The export uses no limiter. UniformVoiceBoost applies another 2x gain during playback.
