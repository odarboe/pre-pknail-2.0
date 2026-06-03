
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("15", "9 1/4", "", "Idx:3", "", "Idx:1", "")
_SurveyWindow("2", "", "", "3", "3 3/16", "3", "11 7/8", "2", "", "")
_SurveyWindow("9", "", "", "", "", "", "", "", "", "")


_ExportModel()
_KillRevit()
