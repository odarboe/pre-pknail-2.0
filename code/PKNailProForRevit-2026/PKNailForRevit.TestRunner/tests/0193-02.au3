
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

Send("ze")
Sleep(100)

_ActivateWallSurveyLength(64, 476, "", "")
_SurveyWall("5", "", "", "", "", "", "")
_SurveyWall("9", "", "", "", "", "", "")
_SurveyDoor("4", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(575, 449, 523, 335, "", "", "", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("9", "", "", "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")
_SurveyWindow("4", "", "", "3", "", "4", "", "2", "", "")

_ExportModel()
_KillRevit()
