
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_EditRoundingSettings("0.25", "0.25", "", "0.25")

_SurveyWall("2", "6", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("4", "", "Idx:1", "", "", "", "")
_SurveyWall("2", "", "", "", "", "", "")
_SurveyWall("12", "6", "", "", "", "", "")
_SurveyWall("12", "6", "", "", "", "", "")
_SurveyWall("2", "", "", "", "", "", "")
_SurveyWall("4", "", "", "", "", "", "")
_SurveyWall("2", "6", "Idx:1", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")
_SurveyDoor("1", "", "", "2", "", "7", "", "", "", "", "1")
_ActivateWallRefOpening(482, 209, 413, 428, "", "", "", "")
_SurveyWall("3", "3 1/2", "", "", "", "", "")
_ActivateWallNoSurvey(353, 446)
_SurveyWall("", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
