
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("32", "2", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("4", "6", "", "2", "", "7", "", "", "", "", "")
_SurveyDoor("15", "1", "", "", "", "", "", "", "", "", "")
_SurveyDoor("25", "8", "", "", "", "", "", "", "", "", "")
_ActivateWallRefOpening(354, 346, 305, 331, "4", "", "10", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("", "", "", "Unknown", "", "", "")
_ActivateWallRefOpening(714, 509, 789, 473, "4", "", "10", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("10", "", "", "Idx:3", "", "", "")
_SurveyWall("", "", "", "Unknown", "", "", "")
_ActivateWallRefOpening(886, 509, 783, 468, "4", "", "10", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("10", "", "", "Idx:3", "", "", "")
_SurveyWall("", "", "", "", "", "", "")

_ConvertUnknownWidthWalls("AllCurrentLevel")

Send("ze")
_DragInPlanView(20, 20, 930, 600)
Sleep(2000)
_CopyWalls("3", "", "All", "All", "All")

_ExportModel()
_KillRevit()
