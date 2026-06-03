
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("4", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(625, 338, 430, 398, "1", "", "4", "")
_SurveyWall("5", "", "", "", "", "", "")
_SurveyWall("3", "11", "", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")
_SurveyWallNoSurvey()

_ExportModel()
_KillRevit()
