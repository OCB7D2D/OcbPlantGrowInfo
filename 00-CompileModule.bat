@echo off

SET NAME=PlantGrowInfo

call MC7D2D %NAME%.dll /reference:"%PATH_7D2D_MANAGED%\Assembly-CSharp.dll" Harmony\*.cs && ^
echo Successfully compiled %NAME%.dll

SET RV=%ERRORLEVEL%
if "%CI%"=="" pause
exit /B %RV%