
#include "globals.au3"
#include "functions.au3"

$g_isSurveyFirstWallLegacyOrientation = True

_InitRevitWindow()

_EditRoundingSettings("", "5", "", "")

_SurveyWall("6000", "", "", "Idx:3", "", "Idx:1", "")
_SurveyDoor("900", "", "1", "1500", "", "2250", "", "", "", "", "0")
_SurveyDoor("3000", "", "2", "1055", "", "2400", "", "", "", "", "")
_SurveyWall("6000", "", "", "", "", "", "")
_SurveyDoor("900", "", "4", "1350", "", "2250", "", "", "", "", "1")
_SurveyDoor("3000", "", "1", "", "", "", "", "", "", "", "")
_SurveyWall("6000", "", "", "", "", "", "")
_SurveyDoor("750", "", "5", "600", "", "1950", "", "", "", "", "")
_SurveyDoor("2400", "", "1", "", "", "", "", "", "", "", "1")
_SurveyDoor("4800", "", "3", "975", "", "2250", "", "", "", "", "")
_SurveyWall("6000", "", "", "", "", "", "")
_SurveyDoor("900", "", "0", "800", "", "2250", "", "", "", "1", "1")
_SurveyDoor("2400", "", "1", "1050", "", "", "", "", "", "", "")
_SurveyDoor("4200", "", "", "", "", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
