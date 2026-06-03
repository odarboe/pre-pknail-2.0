
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

Send("ze")
Sleep(100)
_ActivateWallRefOpening(453, 503, 523, 226, "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
