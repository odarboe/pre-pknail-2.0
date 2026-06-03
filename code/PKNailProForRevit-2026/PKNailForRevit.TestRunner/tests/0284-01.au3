
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("3", "", "", "Unknown", "", "", "")

_ExportModel()
_KillRevit()
