
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:2", "", "Idx:1", "")
_SurveyWindow("2", "", "", "2", "", "4", "", "3", "", "")
_SurveyWindow("6", "", "", "", "", "", "", "", "", "")
_SurveyWindow("10", "", "", "", "", "", "", "", "", "")
_SurveyWall("10", "", "270", "", "", "", "")
_SurveyWindow("2", "", "", "", "", "", "", "", "", "")
_SurveyDoor("6", "", "", "2", "", "7", "", "", "", "", "")

_ClickInPlanView(402, 461)
Sleep(100)
Send("{CTRLDOWN}")
Sleep(200)
_ClickInPlanView(681, 324)
Sleep(200)
_ClickInPlanView(702, 352)
Sleep(200)
_ClickInPlanView(618, 458)
Sleep(200)
Send("{CTRLUP}")
Sleep(2000)

_CopyWalls("3", "", "Selected", "None", "")

_ExportModel()
_KillRevit()
