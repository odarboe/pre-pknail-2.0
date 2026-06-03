
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("16", "", "", "Idx:3", "", "Idx:1", "")
_ActivateWallSurveyLength(647, 344, "12", "")
_SurveyWall("5", "", "49", "Unknown", "", "", "")
_SetCoordinateSystem(436, 414)
_SurveyWall("5", "", "", "Idx:3", "", "", "")
_ActivateWallSurveyLength(62, 460, "3", "4")
_SurveyWall("8", "", "130", "Unknown", "", "", "")
_SetCoordinateSystem(307, 499)
_SurveyWall("5", "", "", "Idx:3", "", "", "")

_ConvertUnknownWidthWalls("AllCurrentLevel")
 
_ExportModel()
_KillRevit()
