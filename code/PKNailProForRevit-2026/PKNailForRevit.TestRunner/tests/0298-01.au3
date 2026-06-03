
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("15", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("5", "", "", "Unknown", "", "", "")
_SurveyWall("3", "", "Idx:1", "", "", "", "")
_SurveyWall("5", "", "Idx:1", "Idx:1", "", "", "")
Send("ze")
Sleep(2000)
_ActivateWallSurveyLength(169, 227, "15", "")
_ActivateWallNoSurvey(804, 487)
_SurveyWall("5", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
