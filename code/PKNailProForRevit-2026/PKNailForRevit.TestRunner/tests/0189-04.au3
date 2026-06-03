
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_EditRoundingSettings("0.125", "", "", "0.125")

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_ActivateWallNoSurvey(632, 361)
_SurveyWall("5", "", "", "", "", "", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyDoor("2", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(621, 480, 437, 404, "4.7929", "0", "9.2929", "")
_ActivateWallNoSurvey(297, 496)
_SurveyWall("5", "", "45", "", "", "", "")
_ActivateWallNoSurvey(294, 248)
_SurveyWall("", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
