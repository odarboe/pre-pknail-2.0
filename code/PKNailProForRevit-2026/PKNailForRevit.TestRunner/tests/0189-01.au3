
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("4", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(650, 338, 428, 399, "4", "0", "9", "6")
_SurveyWall("9", "", "", "", "", "", "")
_ActivateWallNoSurvey(617, 524)
_SurveyWall("", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
