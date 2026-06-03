
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "110", "", "", "", "")
_ActivateWallNoSurvey(291, 197)
_SurveyLine("4", "", "3", "", "", "")
_SurveyLine("", "", "", "", "Idx:1", "")

_ExportModel()
_KillRevit()
