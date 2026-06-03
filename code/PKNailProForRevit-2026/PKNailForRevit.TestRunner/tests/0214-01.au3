
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("3", "", "", "3", "", "7", "", "", "", 0, 0)
_ActivateWallRefOpening(634, 338, 383, 246, "4", "6", "11", "")
_SurveyWall("11", "", "270", "", "", "", "")
_SurveyWall("11", "", "270", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
