
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("15", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("6", "", "", "", "", "", "")
_SurveyWall("15", "", "", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")
_SurveyWallNoSurvey()
_SurveyDoor("3", "", "", "3", "", "7", "", "", "", 0, 0)
_SurveyWindow("7", "", "", "2", "", "4", "", "2", "", "")
_ActivateWallRefOpening(618, 246, 343, 201, "6", "6", "12", "6")
_SurveyWall("6", "", "", "", "", "", "")
_SurveyWall("13", "", "", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
