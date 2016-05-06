NuGet.exe install FAKE  -OutputDirectory src\packages -ExcludeVersion -Version 4.25.5
"src\packages\FAKE\tools\Fake.exe" build.fsx MultiNodeDevSetup
pause