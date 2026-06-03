
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_ActivateWallNoSurvey(597, 361)
_SurveyWall("10", "", "", "", "", "", "")
_SurveyWall("30", "", "135", "", "", "", "")
_SurveyDoor("4", "", "", "3", "", "7", "", "", "", "", "")
_ActivateWallRefOpening(366, 300, 399, 356, "", "", "12", "")
_SurveyWall("8", "", "", "", "", "", "")
_SurveyWall("18", "", "", "", "", "", "")
_SetCoordinateSystem(479, 71)
_SurveyWall("10", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
