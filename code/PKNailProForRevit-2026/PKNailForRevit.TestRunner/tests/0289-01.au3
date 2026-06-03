
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("2", "", "2", "3", "", "10", "", "", "", "", "")
_ActivateWallRefOpening(270, 338, 402, 336, "0", "0", "7", "6")
_SurveyWall("5", "", "", "Idx:3", "", "", "")
_ActivateWallNoSurvey(602, 483)
_SurveyWall("5", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
