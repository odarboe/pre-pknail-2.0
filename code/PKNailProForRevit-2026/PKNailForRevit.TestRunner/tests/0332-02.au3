
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("2", "", "", "3", "", "6", "6", "2", "4", "", "")
_ActivateWallRefOpeningWithLevelOffset(443, 344, 286, 303, "10", "", "18", "", "2", "4")
_SurveyWindow("2", "", "", "2", "3", "1", "6", "4", "6", "")

_DragInPlanView(160, 100, 925, 540)
Sleep(2000)
_CopyWalls("1", "1", "All", "All", "All")

_ExportModel()
_KillRevit()
