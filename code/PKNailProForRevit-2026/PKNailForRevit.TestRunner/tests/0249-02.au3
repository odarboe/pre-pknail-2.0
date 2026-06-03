
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("20", "", "", "", "", "", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("3", "6", "", "", "", "", "")
_SurveyWall("5", "", "", "", "", "", "")
_SurveyWall("", "", "Idx:1", "", "", "", "")

_ExportModel()
_KillRevit()
