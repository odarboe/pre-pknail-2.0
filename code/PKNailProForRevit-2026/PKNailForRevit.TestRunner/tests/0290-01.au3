
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("5", "", "", "", "", "", "")
_SurveyDoor("0", "", "2", "5", "", "7", "", "", "", "", "")
_ActivateWallSurveyLength(304, 225, "", "")

_ExportModel()
_KillRevit()
