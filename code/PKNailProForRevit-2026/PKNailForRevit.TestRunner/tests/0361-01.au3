
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

Send("ze")
Sleep(100)

_ActivateWallSurveyLength(425, 118, "", "")
_ActivateWallSurveyLength(521, 471, "", "")
_SurveyWall("6", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
