
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_ActivateWallSurveyLength(301, 338, "10", "6")
_SurveyWall("3", "", "Idx:1", "", "", "", "")
_SurveyWall("10", "", "120", "", "", "", "")
_SurveyWall("3", "", "Idx:1", "", "", "", "")
_SurveyWall("7", "", "98", "", "", "", "")
_SurveyWall("4", "", "Idx:3", "", "", "", "")
_SetCoordinateSystem(342, 38)
_SurveyWall("20", "", "Idx:3", "", "", "", "")
_SetCoordinateSystem(278, 39)
_SurveyWall("5", "", "Idx:3", "", "", "", "")

_ExportModel()
_KillRevit()
