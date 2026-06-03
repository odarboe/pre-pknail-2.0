
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "270", "", "", "", "")
_SurveyWall("20", "", "270", "", "", "", "")
_SurveyWall("Close", "", "", "", "", "", "")
_SurveyDoor("4", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(249, 421, 209, 328, "", "", "", "")
_SurveyWall("12", "", "", "", "", "", "")
_SurveyWall("9.899414", "", "135", "", "", "", "")
_SurveyWall("2", "", "", "", "", "", "")
_SurveyWall("16", "", "", "", "", "", "")
_SurveyWall("9", "", "", "", "", "", "")

Sleep(100)
_ClickInPlanView(201, 221)
Sleep(100)
Send("{CTRLDOWN}")
Sleep(100)
_ClickInPlanView(367, 221)
Sleep(200)
_ClickInPlanView(615, 221)
Sleep(200)
_ClickInPlanView(705, 299)
Sleep(200)
_ClickInPlanView(705, 446)
Sleep(200)
_ClickInPlanView(461, 478)
Sleep(200)
_ClickInPlanView(201, 478)
Sleep(200)
_ClickInPlanView(166, 419)
Sleep(200)
Send("{CTRLUP}")
Sleep(2000)

_CopyWalls("2", "", "None", "None", "")

_ExportModel()
_KillRevit()
