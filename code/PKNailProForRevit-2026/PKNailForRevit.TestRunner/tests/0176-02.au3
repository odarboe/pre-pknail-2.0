
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("15", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("6", "", "", "", "", "", "")
_SurveyWall("15", "", "", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")
_SurveyWallNoSurvey()
_ActivateWallNoSurvey(645, 260)
_SurveyDoor("3", "", "", "3", "", "7", "", "", "", 0, 0)
_ActivateWallRefOpening(310, 246, 522, 197, "13", "6", "20", "")
_SurveyWall("7", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
