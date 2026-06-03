
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

Send("ze")

_ActivateWallSurveyLength(43, 197, "10", "1/2")
_SurveyWall("10", "6", "", "", "", "", "")
_SurveyWall("5", "2 1/2", "", "", "", "", "")
_SurveyWall("10", "1", "Idx:1", "", "", "Idx:1", "")
_ActivateWallSurveyLength(298, 481, "", "")
_SurveyWall("", "", "", "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
