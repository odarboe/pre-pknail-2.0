
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "120", "", "", "", "")
_SetCoordinateSystem(381, 224)
_SurveyWall("10", "", "Idx:1", "", "", "", "")
_SetCoordinateSystem(471, 261)
_SurveyWall("10", "", "Idx:1", "", "", "", "")

_ExportModel()
_KillRevit()
