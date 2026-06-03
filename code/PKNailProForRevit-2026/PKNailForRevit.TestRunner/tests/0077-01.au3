
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "93", "", "", "", "")
_SetCoordinateSystem(378, 191)
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
