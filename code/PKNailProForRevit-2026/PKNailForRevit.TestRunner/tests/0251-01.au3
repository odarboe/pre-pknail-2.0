
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWindow("Center", "", "", "2", "10", "4", "", "2", "", "")
_SurveyWall("20", "", "113", "", "", "", "")
_SetCoordinateSystem(353, 184)
_SurveyWall("12", "5", "", "", "", "", "")
_ActivateWallSurveyLength(456, 199, "21", "1")
_SurveyDoor("Center", "", "", "4", "", "7", "", "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")
_SurveyLine("Center", "", "3", "", "", "")

_ExportModel()
_KillRevit()
