
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("6000", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWallHostedObject("600", "", "1", "", "", "", "", "", "2100", "0", "", "")
_SurveyWallHostedObject("1500", "", "2", "", "", "", "", "", "2400", "0", "", "")
_SurveyWallHostedObject("2400", "", "3", "", "", "", "", "", "1800", "0", "", "")
_SurveyWallHostedObject("3600", "", "4", "", "", "", "", "", "1200", "0", "", "")
_SurveyWallHostedObject("4500", "", "5", "", "", "", "", "", "900", "0", "", "")
_ActivateWallNoSurvey(647, 344)
_SurveyWallHostedObject("600", "", "1", "", "", "", "", "", "", "", "", "")
_SurveyWallHostedObject("1500", "", "2", "", "", "", "", "", "", "", "", "")
_SurveyWallHostedObject("2400", "", "3", "", "", "", "", "", "", "", "", "")
_SurveyWallHostedObject("3600", "", "4", "", "", "", "", "", "", "", "", "")
_SurveyWallHostedObject("4500", "", "5", "", "", "", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
