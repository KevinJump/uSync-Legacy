@ECHO OFF
ECHO.
ECHO ===============================================================
ECHO.
ECHO Building: %1
ECHO.
ECHO ===============================================================
ECHO.

Call "C:\Program Files (x86)\MSBuild\12.0\Bin\MsBuild.exe" %1 /p:Configuration=Release /p:PostBuildEvent=
