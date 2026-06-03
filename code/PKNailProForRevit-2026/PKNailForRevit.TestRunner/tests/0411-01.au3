
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("20", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWallHostedObject("2", "", "1", "", "", "", "", "", "7", "0", "", "")
_SurveyWallHostedObject("5", "", "2", "", "", "", "", "", "8", "0", "", "")
_SurveyWallHostedObject("8", "", "3", "", "", "", "", "", "6", "0", "", "")
_SurveyWallHostedObject("12", "", "4", "", "", "", "", "", "4", "0", "", "")
_SurveyWallHostedObject("15", "", "5", "", "", "", "", "", "3", "0", "", "")
_ActivateWallNoSurvey(647, 344)
_SurveyWallHostedObject("2", "", "1", "", "", "", "", "", "", "", "", "")
_SurveyWallHostedObject("5", "", "2", "", "", "", "", "", "", "", "", "")
_SurveyWallHostedObject("8", "", "3", "", "", "", "", "", "", "", "", "")
_SurveyWallHostedObject("12", "", "4", "", "", "", "", "", "", "", "", "")
_SurveyWallHostedObject("15", "", "5", "", "", "", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
