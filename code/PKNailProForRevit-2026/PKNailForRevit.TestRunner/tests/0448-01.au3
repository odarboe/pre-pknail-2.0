
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_ConvertUnknownWidthWalls("AllCurrentLevel")
Sleep(5000)
; Dismiss the unconverted walls warning
Send("{ENTER}")
Sleep(2000)
Send("{ESC}")
Sleep(2000)

_ExportModel()
_KillRevit()
