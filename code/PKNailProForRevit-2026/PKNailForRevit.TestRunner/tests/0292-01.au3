
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

Send("ze")
Sleep(100)
_ActivateWallSurveyLength(329, 77, "", "")
_SurveyWall("", "", "", "", "", "", "")
_ActivateWallRefOpening(255, 224, 197, 272, "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
