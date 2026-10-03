@echo off
echo ========================================================
echo  Siemens TIA Portal V21 Openness - Whitelist Registration
echo ========================================================
echo Requesting administrator privileges...
powershell -Command "Start-Process powershell -Verb RunAs -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"%~dp0Scripts\register-whitelist.ps1\"'"
pause
