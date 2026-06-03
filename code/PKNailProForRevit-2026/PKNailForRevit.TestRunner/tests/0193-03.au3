
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "Idx:1", "", "", "")
_SurveyWindow("2", "", "", "3", "", "3", "", "3", "", "")
_SurveyWindow("6", "", "", "2", "", "4", "", "3", "", "")
_ActivateWallNoSurvey(302, 239)
_SurveyWindow("2", "", "", "3", "", "4", "", "3", "", "")
_SurveyWindow("7", "", "", "", "", "", "", "", "", "")
_SurveyWindow("12", "", "", "", "", "", "", "", "", "")

_ClickInPlanView(380, 239)
Sleep(200)
Send("{CTRLDOWN}")
Sleep(200)
_ClickInPlanView(704, 447)
Sleep(200)
Send("{CTRLUP}")
Sleep(2000)

_CopyWalls("0", "1", "All", "", "")

_ExportModel()
_KillRevit()
