@ECHO OFF
ECHO.
ECHO ===============================================================
ECHO.
ECHO Building: %1
ECHO.
ECHO ===============================================================
ECHO.

Call "MsBuild.exe" %1 /p:Configuration=Release /consoleloggerparameters:Summary;ErrorsOnly /nologo /p:PostBuildEvent=
