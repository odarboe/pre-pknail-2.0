
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("4", "", "", "2", "", "7", "", "", "", "", "")
_ActivateWallRefOpening(414, 344, 334, 319, "4", "", "10", "")

_ExportModel()
_KillRevit()
