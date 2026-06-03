
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_SurveyWall("5", "", "", "Idx:3", "", "Idx:1", "")
_SurveyWall("5", "", "", "Unknown", "", "", "")
_SurveyWall("", "6", "", "", "", "", "")
_SurveyWall("1", "6", "Idx:1", "", "", "", "")
_SurveyWall("", "6", "Idx:1", "", "", "", "")
_SurveyWall("5", "", "", "", "", "", "")
_SurveyWall("5", "", "", "Idx:3", "", "", "")
_SurveyWall("5", "", "Idx:1", "", "", "", "")
_SurveyWall("2", "", "Idx:1", "", "", "", "")
_SurveyWall("1", "", "", "Unknown", "", "", "")
_SurveyWall("1", "", "Idx:1", "", "", "", "")
_SurveyWall("1", "", "Idx:1", "", "", "", "")
_SurveyWall("2", "6", "", "Idx:3", "", "", "")
_SurveyWall("19", "", "Idx:1", "Unknown", "", "", "")

_ConvertUnknownWidthWalls("AllCurrentLevel")
 
_ExportModel()
_KillRevit()
