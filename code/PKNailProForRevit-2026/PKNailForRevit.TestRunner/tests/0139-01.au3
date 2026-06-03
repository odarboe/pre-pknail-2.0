
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "", "4 3/4", "Idx:1", "")
_SurveyWall("10", "", "", "Idx:4", "", "", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("Close", "", "", "2", "3 1/2", "", "")

_ExportModel()
_KillRevit()
