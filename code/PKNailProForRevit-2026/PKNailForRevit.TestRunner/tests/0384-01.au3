
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("15", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("6", "", "225", "", "", "", "")

Send("ze")
Sleep(1000)
Send("wa")
Sleep(1000)
_ClickInPlanView(497, 418)
Sleep(1000)
_ClickInPlanView(497, 145)
Sleep(2000)
Send("{ESC}")
Sleep(200)
Send("{ESC}")
Sleep(3000)

_ActivateWallSurveyLength(504, 364, "5", "")

_ExportModel()
_KillRevit()
