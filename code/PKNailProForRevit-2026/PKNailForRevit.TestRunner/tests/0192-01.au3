
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("5", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallSurveyLength(650, 438, "5", "0")
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallSurveyLength(568, 175, "7", "0")
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallSurveyLength(237, 227, "5", "0")
_SurveyWall("3", "", "", "", "", "", "")
_ActivateWallNoSurvey(424, 525)

_ExportModel()
_KillRevit()
