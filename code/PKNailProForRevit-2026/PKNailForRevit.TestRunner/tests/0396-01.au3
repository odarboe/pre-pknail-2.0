
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("20", "", "", "", "", "", "")
_ActivateWallSurveyLength(340, 175, "20", "6")
_ActivateWallSurveyLength(340, 183, "8", "")
_SurveyWall("10", "", "", "", "", "", "")
_ActivateWallSurveyLength(399, 175, "", "")
_SurveyWall("", "", "", "", "", "", "")
_ActivateWallSurveyLength(649, 489, "15", "")
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallSurveyLength(424, 227, "5", "")
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallSurveyLength(338, 107, "4", "6")
_SurveyWall("5", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
