
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyDoor("2", "", "", "3", "", "7", "", "", "", "", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_ActivateWallRefOpening(652, 377, 722, 254, "5", "", "10", "")
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallNoSurvey(400, 237)
_SurveyWall("5", "", "", "Unknown", "", "", "")

_ExportModel()
_KillRevit()
