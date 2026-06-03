
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_EditRoundingSettings("0.25", "0.25", "", "0.25")

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallNoSurvey(622, 248)
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("5", "", "", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")
_SurveyDoor("1", "", "", "2", "", "7", "", "", "", "", "")
_ActivateWallRefOpening(498, 504, 570, 470, "1", "2 1/2", "7", "5 1/4")
_SurveyWall("5", "", "135", "", "", "", "")
_ActivateWallNoSurvey(675, 243)
_SurveyWallNoSurvey()

_ExportModel()
_KillRevit()
