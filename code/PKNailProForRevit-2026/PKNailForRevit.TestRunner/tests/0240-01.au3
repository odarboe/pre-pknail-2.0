
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("25", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("3", "0", "", "3", "", "7", "", "", "", "", "")
_ActivateWallRefOpening(375, 345, 296, 317, "", "", "", "")

_ExportModel()
_KillRevit()
