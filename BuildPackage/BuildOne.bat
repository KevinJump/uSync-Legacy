@ECHO OFF
ECHO.
ECHO ===============================================================
ECHO.
ECHO Building: %1
ECHO.
ECHO ===============================================================
ECHO.

Call "C:\Program Files (x86)\MSBuild\14.0\Bin\MsBuild.exe" %1 /p:Configuration=Release /consoleloggerparameters:Summary;ErrorsOnly /nologo /p:PostBuildEvent=
