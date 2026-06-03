
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
_ActivateWallRefOpening(622, 246, 343, 196, "13", "6", "24", "")

_ExportModel()
_KillRevit()
