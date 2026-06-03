
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_EditRoundingSettings("0.0625", "0.0625", "", "")

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("10", "", "", "Idx:3", "", "", "")
_SurveyDoor("4", "", "", "2", "", "7", "", "", "", "", "")
_ActivateWallRefOpening(300, 218, 264, 387, "4", "6", "31", "10 1/16")
_SurveyDoor("14", "10 1/16", "", "", "", "", "", "", "", "", "")
_SurveyDoor("25", "4 1/16", "", "", "", "", "", "", "", "", "")
_ActivateWallRefOpening(421, 373, 436, 339, "4", "", "10", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("", "", "", "Idx:3", "", "", "")
_ActivateWallSurveyLength(361, 524, "10", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("", "", "", "Idx:3", "", "", "")
_ActivateWallSurveyLength(345, 524, "11", "1/2")
_SurveyWall("31", "10 1/16", "Idx:1", "Unknown", "", "", "")

_ConvertUnknownWidthWalls("AllCurrentLevel")
 
_ExportModel()
_KillRevit()
