
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_EditRoundingSettings("", "0.25", "", "")

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("3", "", "1", "5", "", "7", "6", "", "", "", 0)
_SurveyDoor("10", "", "2", "3", "6 1/4", "8", "0", "", "", "", "")
_SurveyWall("20", "", "", "", "", "", "")
_SurveyDoor("3", "", "4", "4", "6", "7", "6", "", "", "", "1")
_SurveyDoor("10", "", "1", "", "", "", "", "", "", "", "")
_SurveyWall("20", "", "", "", "", "", "")
_SurveyDoor("2", "6", "5", "2", "", "6", "6", "", "", "", "")
_SurveyDoor("8", "", "1", "", "", "", "", "", "", "", "1")
_SurveyDoor("16", "", "3", "3", "3", "7", "6", "", "", "", "")
_SurveyWall("20", "", "", "", "", "", "")
_SurveyDoor("3", "", "0", "2", "8", "7", "6", "", "", "1", "1")
_SurveyDoor("8", "", "1", "3", "6", "", "", "", "", "", "")
_SurveyDoor("14", "", "", "", "", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
