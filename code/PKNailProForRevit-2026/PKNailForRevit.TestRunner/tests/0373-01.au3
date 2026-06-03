
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("15", "", "", "3", "", "7", "", "", "", "", "")
_SurveyWall("10", "", "", "", "", "6", "5")

Sleep(100)
_ClickInPlanView(553, 228)
Sleep(100)
Send("{CTRLDOWN}")
Sleep(100)
_ClickInPlanView(712, 346)
Sleep(100)
Send("{CTRLUP}")
Sleep(100)
_CopyWalls("2", "1", "None", "All", "None")

_ActivateWallNoSurvey(280, 239)
_SurveyWindow("2", "", "", "3", "", "4", "", "2", "", "")
_SurveyWindow("8", "", "", "", "", "", "", "", "", "")

_ClickInPlanView(460, 270)
Sleep(100)
_ClickInPlanView(460, 230)
Sleep(100)
_CopyWalls("0", "1", "Selected", "", "")

_ExportModel()
_KillRevit()
