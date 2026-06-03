
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
Send("wa")
Sleep(200)
_ClickInPlanView(400, 338)
_ClickInPlanView(400, 90)
Send("{ESC}")
Sleep(200)
Send("{ESC}")
Sleep(200)
_SurveyWall("10", "", "", "", "", "", "")
_SurveyDoor("3", "", "", "3", "", "7", "", "", "", 0, 0)
_ActivateWallRefOpening(650, 464, 719, 294, "", "", "", "")
_SurveyWall("5", "", "", "", "", "", "")
_ActivateWallSurveyLength(437, 175, "", "")
_SurveyWall("10", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
