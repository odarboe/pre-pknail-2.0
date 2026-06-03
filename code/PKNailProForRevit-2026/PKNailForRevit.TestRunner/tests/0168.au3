
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:2", "", "Idx:1", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("10", "", "", "Idx:2", "", "", "")
_ActivateWallNoSurvey(308, 233)
_SurveyWallNoSurvey()
_SurveyWallNoSurvey()

_ExportModel()
_KillRevit()
