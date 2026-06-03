
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("10", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("10", "", "45", "", "", "", "")
_SurveyDoor("3", "", "", "3", "", "7", "",  "", "", "", "")
_ActivateWallRefOpening(664, 229, 626, 348, "", "", "10", "")
_SetCoordinateSystem(367, 194)
_SurveyWall("10", "", "45", "", "", "", "")

_ExportModel()
_KillRevit()
