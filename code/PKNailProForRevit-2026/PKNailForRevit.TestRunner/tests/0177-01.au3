
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("15", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("3", "", "", "3", "", "7", "", "", "", 1, 1)
_ActivateWallRefOpening(611, 342, 424, 398, "9", "", "15", "")
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallNoSurvey(640, 433)
_SurveyWall("5", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
