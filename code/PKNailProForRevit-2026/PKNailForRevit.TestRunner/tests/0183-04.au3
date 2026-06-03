
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyDoor("4", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(650, 462, 598, 327, "4", "", "10", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyDoor("4", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(593, 175, 452, 222, "4", "", "10", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyDoor("4", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallRefOpening(300, 207, 351, 372, "4", "", "10", "")
_SurveyWall("10", "", "", "", "", "", "")
_SurveyDoor("4", "", "", "2", "", "7", "", "", "", 0, 1)
_ActivateWallNoSurvey(344, 524)
_SurveyWallNoSurvey()

_ExportModel()
_KillRevit()
