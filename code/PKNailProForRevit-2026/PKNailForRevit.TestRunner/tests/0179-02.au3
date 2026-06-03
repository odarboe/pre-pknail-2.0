
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_EditRoundingSettings("0.0625", "", "", "0.125")

_SurveyWall("6", "", "", "Idx:3", "", "Idx:1", "")
_ActivateWallNoSurvey(621, 369)
_SurveyWall("6", "", "", "", "", "", "")
_SurveyWall("6", "6", "270", "", "", "", "")
_SurveyDoor("2", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(587, 497, 437, 575, "1", "6", "5", "6")
_SurveyWall("26", "", "", "", "", "", "")
_SurveyWall("5", "6 1/16", "", "", "", "", "")
_SurveyWall("20", "1/16", "", "", "", "", "")
_SurveyWall("4", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
