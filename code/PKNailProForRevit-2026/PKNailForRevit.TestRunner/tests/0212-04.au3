
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_EditRoundingSettings("0.5", "", "", "")

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("3", "", "", "2", "8", "7", "", "", "", 1, 1)
_SurveyWindow("7", "", "", "1", "6", "4", "", "2", "8", "")
_ActivateWallRefOpening(281, 338, 503, 418, "", "", "", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("10", "1/2", "", "", "", "", "")
_SurveyWall("10", "1/2", "", "", "", "", "")

_ExportModel()
_KillRevit()
