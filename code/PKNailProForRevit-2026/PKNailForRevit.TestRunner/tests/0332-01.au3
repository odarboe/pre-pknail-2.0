
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWallWithLevelOffset("15", "", "", "", "", "", "", "", "19 1/2")
_SurveyWindow("1", "", "", "2", "", "2", "", "2", "", "")
_SurveyDoor("4", "", "", "2", "6", "7", "", "", "", "", "")
_SurveyWall("10", "", "", "", "", "", "")

_ActivateWallSurveyLengthWithLevelOffset(393, 525, "10", "6", "1", "1")
_SurveyWall("7", "9", "", "", "", "", "")
_SurveyDoor("1", "", "", "", "", "", "", "", "", "", "")
_SurveyWall("6", "", "", "Unknown", "", "", "")
_SurveyWall("7", "3", "", "Idx:2", "", "", "")
Send("ze")
_ActivateWallRefOpeningWithLevelOffset(511, 331, 547, 253, "", "6", "7", "", "", "5")
_SurveyWall("6", "", "", "", "", "", "")
_ActivateWallNoSurvey(346, 230)
_SurveyWall("6", "", "", "Unknown", "", "", "")
_SurveyWall("", "", "", "", "", "", "")

_ConvertUnknownWidthWalls("AllProject")

_ExportModel()
_KillRevit()
