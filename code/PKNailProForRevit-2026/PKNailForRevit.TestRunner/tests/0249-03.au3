
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_EditRoundingSettings("0.5", "", "", "")

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("10", "1/2", "", "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
