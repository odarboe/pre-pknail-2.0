
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

Send("ze")
Sleep(500)
_ActivateWallRefOpening(246, 52, 292, 86, "", "", "10", "")
_SurveyWall("10", "0", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("", "", "Idx:1", "", "", "", "")
_ActivateWallRefOpening(531, 517, 455, 550, "", "", "12", "")
_SurveyWall("9", "", "", "", "", "", "")
_SurveyWall("", "", "Idx:1", "", "", "", "")
_ActivateWallRefOpening(761, 477, 760, 412, "", "", "25", "")

Sleep(100)
_ClickInPlanView(436, 235)
Sleep(500)
Send("{CTRLDOWN}")
Sleep(500)
_ClickInPlanView(300, 243)
Sleep(500)
_ClickInPlanView(195, 271)
Sleep(500)
_ClickInPlanView(195, 478)
Sleep(500)
_ClickInPlanView(229, 518)
Sleep(500)
_ClickInPlanView(365, 518)
Sleep(500)
_ClickInPlanView(471, 470)
Sleep(500)
_ClickInPlanView(471, 268)
Sleep(500)
_ClickInPlanView(471, 202)
Sleep(2000)
Send("{CTRLUP}")
Sleep(2000)

_CopyWalls("0", "", "All", "None", "None")

_ExportModel()
_KillRevit()
