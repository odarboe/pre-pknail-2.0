
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_EditRoundingSettings("0.5", "", "", "")

Send("ze")

_ActivateWallSurveyLength(296, 360, "", "")
_SurveyDoor("2", "3 1/2", "", "2", "6", "7", "", "", "", "", "")
_ActivateWallRefOpening(282, 496, 262, 344, "", "", "15", "9")
_SurveyWall("6", "", "", "", "", "", "")
_ActivateWallNoSurvey(496, 416)
_SurveyWall("", "", "", "", "", "", "")
_ActivateWallSurveyLength(630, 174, "", "")
_SurveyWall("4", "8 1/2", "", "", "", "", "")
_SurveyWall("6", "5", "", "", "", "", "")

_ExportModel()
_KillRevit()
