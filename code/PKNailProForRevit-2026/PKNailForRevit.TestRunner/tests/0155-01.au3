
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:1", "", "Idx:1", "")
_SurveyWall("10", "", "102.5", "Idx:3", "", "", "")
_SetCoordinateSystem(499, 186)
_SurveyWall("10", "", "", "Idx:1", "", "", "")
_ActivateWallSurveyLength(331, 524, "", "")
_SurveyWall("10", "", "", "", "", "", "")
_SetCoordinateSystem(499, 186)
_SurveyWall("10", "", "", "Idx:1", "", "", "")
_ActivateWallSurveyLength(331, 524, "10", "0 1/2")
_SurveyWall("10", "", "", "", "", "", "")
_SetCoordinateSystem(499, 186)
_SurveyWall("10", "", "", "Idx:1", "", "", "")
_ActivateWallSurveyLength(331, 524, "10", "1 1/32")
_SurveyWall("10", "", "Idx:1", "", "", "", "")
_ActivateWallSurveyLength(483, 474, "5", "")
_SurveyWall("10", "", "68.75", "Idx:3", "", "", "")
_SetCoordinateSystem(261, 394)
_SurveyWall("10", "", "", "Idx:1", "", "", "")
_ActivateWallSurveyLength(597, 448, "", "")
_SurveyWall("10", "", "", "", "", "", "")
_ActivateWallNoSurvey(321, 259)
_SurveyWall("10", "", "120", "Idx:3", "", "", "")
_SetCoordinateSystem(381, 315)
_SurveyWall("10", "", "", "Idx:1", "", "", "")
_ActivateWallSurveyLength(625, 243, "", "")
_SurveyWall("10", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
