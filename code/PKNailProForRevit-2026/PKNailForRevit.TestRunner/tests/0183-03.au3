
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("5", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("2", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(677, 325, 434, 514, "1", "", "5", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("5", "", "270", "Idx:3", "", "", "")
_SurveyDoor("2", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(401, 192, 489, 123, "1", "", "4", "5")
_SurveyWall("8", "4", "", "Unknown", "", "", "")

_ExportModel()
_KillRevit()
