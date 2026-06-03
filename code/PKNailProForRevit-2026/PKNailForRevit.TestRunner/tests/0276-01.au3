
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("2", "", "", "3", "", "7", "", "", "7", "", "")
_SurveyDoor("8", "", "1", "5", "", "6", "", "1", "3", "", "")
_SurveyDoor("15", "", "0", "", "", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
