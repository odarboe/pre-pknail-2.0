
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyLine("3", "", "4", "", "", "1")
_SurveyLine("", "", "2", "", "Idx:1", "0")
_SurveyLine("", "", "2", "", "", "3")
_SurveyLine("", "", "2", "", "Idx:1", "")
_SurveyLine("", "", "", "", "Idx:3", "2")

_ExportModel()
_KillRevit()
