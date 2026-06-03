@echo off
::
:: Batch script for signing PKNail setup executables
:: 
:: usage: signSetup.bat <path to cert file> <cert file password> <target setup file>
::
:: typical values for arguments
:: <path to cert file>:  F:\PKSigningCert.p12
:: <target setup file>:  PKNailProForRevit20XX_vA.B.VCS.BN_setup.exe
::

"c:\Program Files (x86)\Microsoft SDKs\Windows\v7.0a\bin\signtool.exe" sign /f %1 /p %2 /d "PKNail Pro For Revit Setup" /t http://timestamp.comodoca.com/authenticode /v %3
