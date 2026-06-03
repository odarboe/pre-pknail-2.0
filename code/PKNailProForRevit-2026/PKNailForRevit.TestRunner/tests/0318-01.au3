
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_EditRoundingSettings("", "0.125", "", "")

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWindow("2", "", "", "3", "3", "4", "6", "2", "0", "")
_SurveyWindow("11", "", "", "5", "5 5/8", "4", "10", "1", "0", "")

_ClickInPlanView(399, 355)
Sleep(2000)

_CopyWalls("1", "1", "All", "", "")

_ExportModel()
_KillRevit()
