
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("9", "", "", "", "", "", "")
_SurveyWall("4", "", "", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")
_SurveyDoor("2", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(302, 215, 520, 417, "7.750912", "", "12.0185", "")
_SurveyWall("", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
