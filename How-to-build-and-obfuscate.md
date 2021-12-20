### Build and Obfuscate

It works only on Windows OS.

* Clone the project recursively because it contains submodules, checkout `obfuscation-test` branch
* Open solution `Source/PT.PM.sln` in Visual Studio or Rider and build it in `Debug` configuration
* Execute `xcopy /s /y /q bin\Debug\net472\* bin\Obfuscated\`. It moves all not obfuscated binaries (dependencies) to output directory
* Run `<path to Confuser.CLI.exe> Obfuscate.crproj`
* Run `bin\Obfuscated\PT.PM.Cli.exe`

### Expected result

The tool prints some info without exception

### Actual result

```
Unhandled Exception: System.TypeLoadException: Could not load type 'PT.PM.Matching.PatternRoot' from assembly 'PT.PM.Matching, Version=1.7.1.0, Culture=neutral, PublicKeyToken=null'.
   at _PT_PM_Cli_Program._Main(String[] )
```

But everything works fine if `renPublic=false`