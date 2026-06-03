
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWindow("2", "", "", "3", "", "7", "", "3", "", "")
_ActivateWallRefOpening(270, 338, 402, 336, "0", "0", "7", "6")

_ExportModel()
_KillRevit()
