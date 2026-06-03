
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "Idx:4", "", "", "")
_SurveyWall("10", "", "", "Idx:3", "", "", "")

_ExportModel()
_KillRevit()
