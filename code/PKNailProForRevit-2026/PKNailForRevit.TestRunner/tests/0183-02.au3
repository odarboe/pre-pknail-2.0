
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("7", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(692, 338, 661, 405, "1", "", "5", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("5", "", "270", "Idx:3", "", "", "")
_SurveyDoor("2", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(406, 192, 489, 127, "1", "", "4", "5")
_SurveyWall("9", "4", "", "Unknown", "", "", "")

_ExportModel()
_KillRevit()
