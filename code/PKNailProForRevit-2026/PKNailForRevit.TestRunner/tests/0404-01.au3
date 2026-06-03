
#include "globals.au3"
#include "functions.au3"


_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "Idx:1", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("9", "11", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("9", "9", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("5", "", "", "Idx:3", "", "Idx:1", "")

_ExportModel()
_KillRevit()
