
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "135", "", "", "", "")
_SurveyDoor("3", "", "", "3", "", "7", "", "", "", "", "")
_ActivateWallRefOpening(534, 265, 606, 279, "", "", "10", "")
_SetCoordinateSystem(199, 193)
_SurveyWall("10", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
