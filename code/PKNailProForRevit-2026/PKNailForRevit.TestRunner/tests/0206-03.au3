
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("16", "", "", "Idx:3", "", "Idx:1", "")
_ActivateWallSurveyLength(647, 344, "12", "")
_SurveyWall("15", "", "49", "Unknown", "", "", "")
_SetCoordinateSystem(490, 510)
_SurveyWall("5", "", "", "Idx:3", "", "", "")
_ActivateWallSurveyLength(171, 516, "3", "4")
_SurveyWall("18", "", "130", "Unknown", "", "", "")
_SetCoordinateSystem(330, 512)
_SurveyWall("5", "", "", "Idx:3", "", "", "")

_ConvertUnknownWidthWalls("AllCurrentLevel")
 
_ExportModel()
_KillRevit()
