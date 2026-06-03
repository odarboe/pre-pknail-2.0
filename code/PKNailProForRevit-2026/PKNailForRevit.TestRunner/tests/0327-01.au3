
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

Send("ze")
Sleep(100)

_ConvertUnknownWidthWalls("AllCurrentLevel")
 
_ExportModel()
_KillRevit()
