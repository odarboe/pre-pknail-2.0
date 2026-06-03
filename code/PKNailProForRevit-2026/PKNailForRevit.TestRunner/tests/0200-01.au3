
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:3", "")
_SurveyWindow("3", "", "", "3", "", "Typical", "", "", "", "")
_SurveyWindow("9", "", "", "", "", "Stacked", "", "", "", "")
_SurveyWindow("15", "", "", "", "", "5", "1", "1", "5", "")

_ExportModel()
_KillRevit()
