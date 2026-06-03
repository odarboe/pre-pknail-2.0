
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "", "", "", "")
_ActivateWallSurveyLength(650, 449, "15", "")
_SurveyWall("10", "", "100", "", "", "", "")
_SetCoordinateSystem(316, 312)
_SurveyWall("10", "", "Idx:2", "", "", "", "")

_ExportModel()
_KillRevit()
