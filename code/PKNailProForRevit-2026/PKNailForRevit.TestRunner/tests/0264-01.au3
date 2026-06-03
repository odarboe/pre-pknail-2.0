
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:3", "")
_SurveyWindow("3", "", "", "3", "", "Typical", "", "", "", "")
_SurveyWindow("9", "", "", "", "", "Stacked", "", "", "", "")
_SurveyWindow("15", "", "", "", "", "5", "1", "1", "5", "")
_OpenView("Level 2")
_ActivateWallCancelRefOpening("275", "344", "7", "")
_SurveyWall("6", "", "", "", "", "Idx:1", "")
_ActivateWallRefOpening("717", "498", "802", "505", "", "", "6", "6")
_SurveyWall("6", "", "", "", "", "Idx:2", "")

_ExportModel()
_KillRevit()
