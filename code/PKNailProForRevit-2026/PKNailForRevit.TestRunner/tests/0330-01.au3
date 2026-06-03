
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_EditRoundingSettings("", "", "0.005208333", "")

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("10", "", "", "", "7 3/16", "", "")

_ExportModel()
_KillRevit()
