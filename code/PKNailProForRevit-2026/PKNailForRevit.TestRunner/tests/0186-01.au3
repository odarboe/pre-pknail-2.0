
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("5", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyDoor("7", "", "", "2", "", "7", "", "", "", 0, 1)
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallRefOpening(564, 498, 516, 413, "1", "2", "5", "")
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallNoSurvey(592, 206)
_SurveyWallNoSurvey()
_SurveyWall("5", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
