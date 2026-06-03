
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("11", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("21", "8", "270", "Idx:3", "", "Idx:1", "")
_SurveyDoor("4", "6", "", "2", "", "7", "", "", "", "", "")
_SurveyDoor("15", "2", "", "", "", "", "", "", "", "", "")
_ActivateWallRefOpening(556, 207, 542, 452, "4", "", "10", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_ActivateWallNoSurvey(656, 446)
_ActivateWallRefOpening(556, 194, 538, 278,"4", "", "10", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("10", "", "", "Idx:3", "", "", "")
_SurveyWall("10", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
