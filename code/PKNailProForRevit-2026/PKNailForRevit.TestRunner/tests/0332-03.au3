
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

Send("ze")
Sleep(500)

_ActivateWallSurveyLength(409, 452, "", "")
_SurveyDoor("7", "", "2", "3", "", "10", "", "", "", "", "")

Send("ze")
Sleep(500)

_ActivateWallSurveyLength(823, 105, "", "")
_SurveyWall("", "", "", "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")
_ActivateWallNoSurvey(546, 534)
_SurveyWall("", "", "", "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")
_SurveyWall("", "", "", "", "", "", "")
_SurveyWallNoSurvey()

_ExportModel()
_KillRevit()
