
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_ActivateWallSurveyLength(623, 338, "2", "")
_SurveyWall("20", "", "", "Unknown", "", "", "")
_SurveyWindow("1", "", "", "2", "", "4", "", "2", "6", "")
_SurveyWindow("4", "", "", "2", "", "4", "", "2", "6", "1")
_SurveyWallHostedObject("8", "", "7", "", "", "", "", "", "3", "2", "", "")
_ActivateWallSurveyLength(352, 516, "7", "5")
_SurveyWall("20", "", "", "Unknown", "", "", "")
_SurveyWallHostedObject("11", "", "7", "", "", "", "", "", "", "", "", "")
_SurveyDoor("13", "6", "0", "2", "", "7", "", "", "", "", "")
_SurveyDoor("16", "6", "0", "2", "", "7", "", "", "", "1", "1")
_ConvertUnknownWidthWalls("AllCurrentLevel")

_ExportModel()
_KillRevit()
