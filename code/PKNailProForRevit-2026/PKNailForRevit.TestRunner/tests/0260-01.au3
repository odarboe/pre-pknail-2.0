
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyLine("4", "", "5", "", "", "")
_SurveyLine("", "", "5", "", "Idx:1", "")
_SurveyLine("", "", "", "", "Idx:1", "")
_ActivateWallSurveyLength(292, 284, "", "")
_SurveyLine("4", "", "5", "", "", "")
_SurveyLine("", "", "5", "", "Idx:1", "")
_SurveyLine("", "", "", "", "Idx:1", "")
_ActivateWallNoSurvey(646, 403)
_SurveyLine("4", "", "5", "", "", "")
_SurveyLine("", "", "5", "", "Idx:1", "")
_SurveyLine("", "", "", "", "Idx:1", "")
_ActivateWallNoSurvey(289, 415)
_SurveyWall("20", "", "", "", "", "", "")
_SurveyWall("20", "", "", "", "", "", "")
_SurveyLine("18", "", "20", "", "", "")
_ActivateWallNoSurvey(351, 183)
_SurveyLine("18", "", "20", "", "", "")

_ExportModel()
_KillRevit()
