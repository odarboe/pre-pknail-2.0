
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:1", "", "Idx:1", "")
_SurveyWindow("3", "", "", "3", "", "4", "", "3", "", "")
_SurveyWindow("9", "", "", "2", "", "6", "", "2", "", "")
_SurveyWindow("14", "", "", "1", "", "8", "", "1", "", "")
_SurveyWall("10", "", "", "Idx:3", "", "", "")
_SurveyWindow("1", "", "", "", "", "", "", "", "", "")
_SurveyWindow("7", "", "", "", "", "", "", "", "", "")

_ClickInPlanView(340, 232)
Sleep(100)
Send("{CTRLDOWN}")
Sleep(200)
_ClickInPlanView(575, 232)
Sleep(200)
_ClickInPlanView(710, 273)
Sleep(200)
_ClickInPlanView(413, 238)
Sleep(200)
_ClickInPlanView(700, 319)
Sleep(200)
Send("{CTRLUP}")
Sleep(2000)

_CopyWalls("", "1", "Selected", "", "")

_ExportModel()
_KillRevit()
