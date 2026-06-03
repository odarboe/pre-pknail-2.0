
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("5", "", "", "Unknown", "", "Idx:1", "")
_SurveyWall("5", "", "", "Idx:3", "", "", "")
_ActivateWallSurveyLength(664, 457, "5", "4")
_SurveyWall("10", "", "Idx:1", "Idx:3", "", "", "")

; Dismiss the "highlighted walls overlap" error messsage
_WinWaitActivate("[REGEXPTITLE:Autodesk Revit Architecture .*]", "")
Send("{ENTER}")
Sleep(3000)

_SurveyWall("5", "", "", "Idx:3", "", "", "")

_ExportModel()
_KillRevit()
