
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("15", "", "", "Idx:3", "", "Idx:2", "")
_SurveyWindow("2", "", "", "3", "", "4", "", "12", "0", "")

_OpenView("Level 2")

Send("ze")
_WaitForIdle(5)
Send("wa")
Sleep(200)
_ClickInPlanView(636, 332)
Sleep(200)
_ClickInPlanView(636, 50)
Sleep(3000)
Send("{ESC}")
Sleep(200)
Send("{ESC}")
_WaitForIdle(10)

_ActivateWallRefOpening(517, 332, 244, 336, "", "", "", "")

_ExportModel()
_KillRevit()
