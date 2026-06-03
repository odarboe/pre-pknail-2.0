
#include "globals.au3"
#include "functions.au3"

_InitRevitWindow()

_WinWaitActivate($g_revitWin_handle, "")
Send($gc_kb_surveyWall)

; Set custom values in first wall dialog
WinWait($gc_wt_firstWall, "", 10)
If WinExists($gc_wt_firstWall, "") Then
	_WinWaitActivate($gc_wt_firstWall, "")
	_SetCBSelIndex($gc_wt_firstWall, "[NAME:PerspectiveCB]", 1)
	_SetCBSelIndex($gc_wt_firstWall, "[NAME:DirectionCB]", 2)
	_UserPause()
	Send("{ENTER}")
	_WaitForIdle(5)
	_UserPause()
EndIf

; Finish up the survey wall dialog manually
_WinWaitActivate($gc_wt_surveyWall, "")
_SetDC($gc_wt_surveyWall, "LengthDC", "10", "")
_SetCBSelIndex($gc_wt_surveyWall, "[NAME:WidthCB]", "3")
_SetCBSelIndex($gc_wt_surveyWall, "[NAME:ConnectedLevelCB]", "1")

_UserPause()
ControlClick($gc_wt_surveyWall, "", "[NAME:OKBtn]")
_WaitForIdle(5)
_UserPause()

; Survey another wall normally
_SurveyWall("10", "", "", "", "", "", "")

_ExportModel()
_KillRevit()
