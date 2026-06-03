
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("6", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "3", "Idx:2", "", "", "", "")
_SurveyDoor("2", "", "", "2", "", "7", "", "", "", "", "")
_ActivateWallRefOpening(560, 214, 521, 456, "6", "6", "10", "0")
_ActivateWallNoSurvey(575, 478)
_SurveyWall("10", "", "", "", "12", "", "")
_ActivateWallNoSurvey(307, 233)
_SurveyWall("5", "", "", "", "", "", "")
_SurveyWall("10", "", "", "Unknown", "", "", "")
_SurveyWall("5", "", "", "Unknown", "", "", "")

_ExportModel()
_KillRevit()
