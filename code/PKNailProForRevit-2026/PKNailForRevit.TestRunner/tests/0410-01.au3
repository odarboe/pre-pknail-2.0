
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("10", "", "", "", "", "", "")
_ActivateWallSurveyLength(315, 441, "5", "")
_SurveyWall("", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
