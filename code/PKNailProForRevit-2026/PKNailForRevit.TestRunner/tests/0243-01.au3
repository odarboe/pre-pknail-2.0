
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("20", "", "Idx:1", "", "", "", "")
_SurveyWall("30", "", "Idx:1", "", "", "", "")
_SurveyWall("10", "", "Idx:1", "", "", "", "")
_SurveyWall("10", "", "Idx:1", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
