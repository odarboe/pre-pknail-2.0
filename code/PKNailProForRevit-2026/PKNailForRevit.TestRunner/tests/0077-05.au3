
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "87", "", "", "", "")
_SurveyWall("10", "", "240", "", "", "", "")
_SetCoordinateSystem(384, 288)
_SurveyWall("10", "", "Idx:2", "", "", "", "")

_ExportModel()
_KillRevit()
