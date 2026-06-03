
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

Send("ze")
_ActivateWallNoSurvey(138, 490)

_ExportModel()
_KillRevit()
