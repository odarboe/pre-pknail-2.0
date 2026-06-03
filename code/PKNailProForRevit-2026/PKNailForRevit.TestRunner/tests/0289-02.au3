
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("2", "", "0", "3", "", "10", "", "", "", 1, 1)
_ActivateWallRefOpening(622, 338, 402, 336, "4", "6", "7", "6")
_SurveyWall("5", "", "", "Idx:3", "", "", "")
_ActivateWallNoSurvey(461, 483)
_SurveyWall("5", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
