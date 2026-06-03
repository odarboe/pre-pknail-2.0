
#include-once
#include "globals.au3"
#include <GUIConstantsEx.au3>
#include <GUIComboBox.au3>
#include <GUIListBox.au3>
#include <GUIButton.au3>

; ********************************************************************************

Func _ActivateWallNoSurvey($x, $y)
	_ActivateWallSurveyLength($x, $y, "", "")
EndFunc

; ********************************************************************************

Func _ActivateWallSurveyLength($x, $y, $lengthFeet, $lengthInches)
	_ActivateWallSurveyLengthWithLevelOffset($x, $y, $lengthFeet, $lengthInches, _
	                                         "", "")
EndFunc

; ********************************************************************************

Func _ActivateWallSurveyLengthWithLevelOffset($x, $y, $lengthFeet, $lengthInches, _
	                                          $levelOffsetFeet, $levelOffsetInches)
	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_activateWall)
	_ClickInPlanView($x, $y)
	_WinWaitActivate($gc_wt_surveyWall, "")
	_SetDC($gc_wt_surveyWall, "LengthDC", $lengthFeet, $lengthInches)
	_SetDC($gc_wt_surveyWall, "LevelOffsetDC", $levelOffsetFeet, $levelOffsetInches)
	_UserPause()
	ControlClick($gc_wt_surveyWall, "", "[NAME:OKBtn]")
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _ActivateWallRefOpening($wall_x, $wall_y, $opening_x, $opening_y, _
	                         $cornerToOpeningFeet, $cornerToOpeningInches, _
	                         $wallExtentFeet, $wallExtentInches)
	_ActivateWallRefOpeningWithLevelOffset($wall_x, $wall_y, $opening_x, $opening_y, _
	                         $cornerToOpeningFeet, $cornerToOpeningInches, _
	                         $wallExtentFeet, $wallExtentInches, "", "")
EndFunc

; ********************************************************************************

Func _ActivateWallRefOpeningWithLevelOffset($wall_x, $wall_y, $opening_x, $opening_y, _
	                         $cornerToOpeningFeet, $cornerToOpeningInches, _
	                         $wallExtentFeet, $wallExtentInches, _
                             $levelOffsetFeet, $levelOffsetInches)
	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_activateWall)
	_ClickInPlanView($wall_x, $wall_y)
	_ClickInPlanView($opening_x, $opening_y)
	_WinWaitActivate($gc_wt_refOpening, "")

	; Set the near corner to opening distance
	_SetDC($gc_wt_refOpening, "OpeningRefDC", $cornerToOpeningFeet, _
	       $cornerToOpeningInches)

	; Set the wall extent
	_SetDC($gc_wt_refOpening, "WallRefDC", $wallExtentFeet, $wallExtentInches)

	; Set the level offset
	_SetDC($gc_wt_surveyWall, "LevelOffsetDC", $levelOffsetFeet, $levelOffsetInches)

	_UserPause()
	ControlClick($gc_wt_refOpening, "", "[NAME:OKBtn]")
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _ActivateWallCancelRefOpening($x, $y, $lengthFeet, $lengthInches)
	_ActivateWallCancelRefOpeningWithLevelOffset($x, $y, $lengthFeet, $lengthInches, "", "")

EndFunc

; ********************************************************************************

Func _ActivateWallCancelRefOpeningWithLevelOffset($x, $y, $lengthFeet, $lengthInches, _
	                                          $levelOffsetFeet, $levelOffsetInches)
	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_activateWall)
	_ClickInPlanView($x, $y)
	Sleep(100)
	Send("{ESC}")
	_WinWaitActivate($gc_wt_surveyWall, "")
	_SetDC($gc_wt_surveyWall, "LengthDC", $lengthFeet, $lengthInches)
	_SetDC($gc_wt_surveyWall, "LevelOffsetDC", $levelOffsetFeet, $levelOffsetInches)
	_UserPause()
	ControlClick($gc_wt_surveyWall, "", "[NAME:OKBtn]")
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _SetCoordinateSystem($wall_x, $wall_y)
	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_setCoordinateSys)
	Sleep(100)
	_ClickInPlanView($wall_x, $wall_y)
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _SurveyLine($locFeet, $locInches, $lengthFeet, $lengthInches, $cornerAngle, $styleIdx)
	Local $win = $gc_wt_surveyLine
	Local $ctlId

	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_surveyLine)

	_WinWaitActivate($win, "")

	; Set the location
	If $locFeet == "Center" Then
		ControlFocus($win, "", "[NAME:CenterChkBx]")
		ControlCommand($win, "", "[NAME:CenterChkBx]", "Check", "")
	Else
		_SetDC($win, "LocationDC", $locFeet, $locInches)
	EndIf

	; Set the length
	If $lengthFeet == "Close" Then
		ControlFocus($win, "", "[Name:CloseCheck]")
		ControlCommand($win, "", "[Name:CloseCheck]", "Check", "")
	Else
		_SetDC($win, "LengthDC", $lengthFeet, $lengthInches)
	EndIf

	; Set the corner angle
	If Not($cornerAngle == "") Then
		$ctlId = "[NAME:CornerAngleCB]"
		If StringInStr($cornerAngle, "Idx:") = 1 Then
			Local $idx = StringMid($cornerAngle, 5)
			_SetCBSelIndex($win, $ctlId, $idx)
		Else
			ControlFocus($win, "", $ctlId)
			ControlSetText($win, "", $ctlId, "")
			ControlSend($win, "", $ctlId, $cornerAngle)
		EndIf
	EndIf

	; Set the style
	_SetCBSelIndex($win, "[NAME:TypeCB]", $styleIdx)

	_UserPause()
	ControlClick($win, "", "[NAME:OKBtn]")
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _SurveyWall($lengthFeet, $lengthInches, $cornerAngle, _
	             $widthFeet, $widthInches, $heightFeet, $heightInches)
	_SurveyWallWorker($lengthFeet, $lengthInches, $cornerAngle, _
	                  $widthFeet, $widthInches, $heightFeet, $heightInches, _
	                  "", "")
EndFunc

; ********************************************************************************

Func _SurveyWallWithLevelOffset($lengthFeet, $lengthInches, $cornerAngle, _
	             $widthFeet, $widthInches, $heightFeet, $heightInches, _
	             $levelOffsetFeet, $levelOffsetInches)
	_SurveyWallWorker($lengthFeet, $lengthInches, $cornerAngle, _
	                  $widthFeet, $widthInches, $heightFeet, $heightInches, _
	                  $levelOffsetFeet, $levelOffsetInches)
EndFunc

; ********************************************************************************

Func _SurveyWallWorker($lengthFeet, $lengthInches, $cornerAngle, _
	                   $widthFeet, $widthInches, $heightFeet, $heightInches, _
	                   $levelOffsetFeet, $levelOffsetInches)
	Local $win = $gc_wt_surveyWall
	Local $ctlId

	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_surveyWall)

	; Check to see if the first wall dialog comes up; if so, OK it
	If $g_isSurveyFirstWallChecked == False Then
		WinWait($gc_wt_firstWall, "", 2)
		If WinExists($gc_wt_firstWall, "") Then
			_WinWaitActivate($gc_wt_firstWall, "")
			If $g_isSurveyFirstWallLegacyOrientation == True Then
				_SetCBSelIndex($gc_wt_firstWall, "[NAME:PerspectiveCB]", 1)
			EndIf
			Send("{ENTER}")
		EndIf
		$g_isSurveyFirstWallChecked = True
	EndIf

	_WinWaitActivate($win, "")

	; Set the length
	If $lengthFeet == "Close" Then
		ControlFocus($win, "", "[Name:CloseCheck]")
		ControlCommand($win, "", "[Name:CloseCheck]", "Check", "")
	Else
		_SetDC($win, "LengthDC", $lengthFeet, $lengthInches)
	EndIf

	; Set the corner angle
	If Not($cornerAngle == "") Then
		$ctlId = "[NAME:CornerAngleCB]"
		If StringInStr($cornerAngle, "Idx:") = 1 Then
			Local $idx = StringMid($cornerAngle, 5)
			_SetCBSelIndex($win, $ctlId, $idx)
		Else
			ControlFocus($win, "", $ctlId)
			ControlSetText($win, "", $ctlId, "")
			ControlSend($win, "", $ctlId, $cornerAngle)
		EndIf
	EndIf

	; Set the width
    $ctlId = "[NAME:UnknownWidthCheck]"
	If $widthFeet == "Unknown" Then
		ControlFocus($win, "", $ctlId)
		ControlSend($win, "", $ctlId, "+", 1)
    Else
	    If StringInStr($widthFeet, "Idx:") = 1 Then
		    Local $idx = StringMid($widthFeet, 5)
		    ControlFocus($win, "", $ctlId)
		    ControlSend($win, "", $ctlId, "-", 1)
		    _SetCBSelIndex($win, "[NAME:WidthCB]", $idx)
	    ElseIf $widthFeet <> "" Or $widthInches <> "" Then
		    ControlFocus($win, "", $ctlId)
		    ControlSend($win, "", $ctlId, "-", 1)
		    _SetDC($win, "WidthDC", $widthFeet, $widthInches)
        EndIf
	EndIf

	; Set the height
	If StringInStr($heightFeet, "Idx:") = 1 Then
		Local $idx = StringMid($heightFeet, 5)
		_SetCBSelIndex($win, "[NAME:ConnectedLevelCB]", $idx)
	Else
		_SetDC($win, "HeightDC", $heightFeet, $heightInches)
	EndIf

	; Set the level offset
	_SetDC($win, "LevelOffsetDC", $levelOffsetFeet, $levelOffsetInches)

	_UserPause()
	ControlClick($win, "", "[NAME:OKBtn]")
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _SurveyWallNoSurvey()
	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_surveyWall)
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _SurveyDoor($locFeet, $locInches, $typeIdx, $widthFeet, $widthInches, _
	             $heightFeet, $heightInches, $sillHeightFeet, $sillHeightInches, _
	             $latOrientationIdx, $facingOrientationIdx)
	Local $win = $gc_wt_surveyDoor

	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_surveyDoor)
	_WinWaitActivate($win, "")

	_SurveyObjectCommon($win, $locFeet, $locInches, $typeIdx, $widthFeet, $widthInches, _
						$heightFeet, $heightInches, $sillHeightFeet, $sillHeightInches)

	; Set the lateral orientation
	_SetCBSelIndex($win, "[NAME:LateralOrientationCB]", $latOrientationIdx)

	; Set the facing orientation
	_SetCBSelIndex($win, "[NAME:FacingOrientationCB]", $facingOrientationIdx)

	_UserPause()
	ControlClick($win, "", "[NAME:OKBtn]")
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _SurveyWindow($locFeet, $locInches, $typeIdx, $widthFeet, $widthInches, _
	               $heightFeet, $heightInches, $sillHeightFeet, $sillHeightInches, _
	               $facingOrientationIdx)
	Local $win = $gc_wt_surveyWindow

	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_surveyWindow)
	_WinWaitActivate($win, "")

	_SurveyObjectCommon($win, $locFeet, $locInches, $typeIdx, $widthFeet, $widthInches, _
						$heightFeet, $heightInches, $sillHeightFeet, $sillHeightInches)

	; Set the facing orientation
	_SetCBSelIndex($win, "[NAME:FacingOrientationCB]", $facingOrientationIdx)

	_UserPause()
	ControlClick($win, "", "[NAME:OKBtn]")
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _SurveyWallHostedObject($locFeet, $locInches, $famCategoryIdx, $typeIdx, $widthFeet, $widthInches, _
                             $heightFeet, $heightInches, $sillHeightFeet, $sillHeightInches, _
							 $depthFeet, $depthInches)
	Local $win = $gc_wt_surveyObject

	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_surveyObject)

	; Select the family category from the picklist
	_WinWaitActivate($gc_wt_selectFamilyCategory, "")
	If Not($famCategoryIdx == "0") Then
		Send("{DOWN " & $famCategoryIdx & "}")
	EndIf
	Send("{ENTER}")

	_WinWaitActivate($win, "")

	_SurveyObjectCommon($win, $locFeet, $locInches, $typeIdx, $widthFeet, $widthInches, _
					    $heightFeet, $heightInches, $sillHeightFeet, $sillHeightInches)

	; Set the depth
	_SetDC($win, "DepthDC", $depthFeet, $depthInches)

	_UserPause()
	ControlClick($win, "", "[NAME:OKBtn]")
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _SurveyObjectCommon($win, $locFeet, $locInches, $typeIdx, $widthFeet, $widthInches, _
                             $heightFeet, $heightInches, $sillHeightFeet, $sillHeightInches)
	; Set the location
	If $locFeet == "Center" Then
		ControlFocus($win, "", "[NAME:CenterChkBx]")
		ControlCommand($win, "", "[NAME:CenterChkBx]", "Check", "")
	Else
		_SetDC($win, "LocationDC", $locFeet, $locInches)
	EndIf

	; Set the family
	_SetCBSelIndex($win, "[NAME:TypeCB]", $typeIdx)

	; Set the width
	_SetDC($win, "WidthDC", $widthFeet, $widthInches)

	; Set the height/sill height
	If $heightFeet == "Typical" Then
		; The code below does not work; until we find something that works,
		; we must depend on the fact that this checkbox is checked by
		; default when typical heights are defined for the level
		;
		;Local $isChecked = ControlCommand($win, "", "[NAME:TypicalHeightsChkBx]", "IsChecked", "")
		;If Not($isChecked) Then
		;	ControlCommand($win, "", "[NAME:TypicalHeightsChkBx]", "Check", "")
		;EndIf
	ElseIf $heightFeet == "Stacked" Then
		; The code below does not work; until we find something that works,
		; we must depend on the fact that this checkbox is checked by
		; default when typical heights are defined for the level
		;
		;Local $isChecked = ControlCommand($win, "", "[NAME:TypicalHeightsChkBx]", "IsChecked", "")
		;If Not($isChecked) Then
		;	ControlCommand($win, "", "[NAME:TypicalHeightsChkBx]", "Check", "")
		;EndIf
		;ControlCommand($win, "", "[NAME:StackWindowsChkBx]", "Check", "")

		; Workaround: Send a click to the checkbox to check it
		ControlClick($win, "", "[NAME:StackWindowsChkBx]")
	Else
		_SetDC($win, "HeightDC", $heightFeet, $heightInches)
		_SetDC($win, "SillHeightDC", $sillHeightFeet, $sillHeightInches)
	EndIf
EndFunc

; ********************************************************************************

Func _CopyWalls($upperLvlIdx, $lowerLvlIdx, $copyWindows, $copyDoors, $copyObjects)
	Local $win = $gc_wt_copyWalls

	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_copyWalls)
	_WinWaitActivate($win, "")

	; Upper level
	_SetCBSelIndex($win, "[NAME:UpperLvlCB]", $upperLvlIdx)

	; Lower level
	_SetCBSelIndex($win, "[NAME:LowerLvlCB]", $lowerLvlIdx)

	; Copy windows option
	If $copyWindows == "None" Then
		ControlCommand($win, "", "[NAME:CopyWindowsNoneRB]", "Check", "")
	ElseIf $copyWindows == "Selected" Then
		ControlCommand($win, "", "[NAME:CopyWindowsSelRB]", "Check", "")
	ElseIf $copyWindows == "All" Then
		ControlCommand($win, "", "[NAME:CopyWindowsAllRB]", "Check", "")
	EndIf

	; Copy doors option
	If $copyDoors == "None" Then
		ControlCommand($win, "", "[NAME:CopyDoorsNoneRB]", "Check", "")
	ElseIf $copyDoors == "Selected" Then
		ControlCommand($win, "", "[NAME:CopyDoorsSelRB]", "Check", "")
	ElseIf $copyDoors == "All" Then
		ControlCommand($win, "", "[NAME:CopyDoorsAllRB]", "Check", "")
	EndIf

	; Copy other objects option
	If $copyObjects == "None" Then
		ControlCommand($win, "", "[NAME:CopyObjsNoneRB]", "Check", "")
	ElseIf $copyObjects == "Selected" Then
		ControlCommand($win, "", "[NAME:CopyObjsSelRB]", "Check", "")
	ElseIf $copyObjects == "All" Then
		ControlCommand($win, "", "[NAME:CopyObjsAllRB]", "Check", "")
	EndIf

	_UserPause()
	ControlClick($win, "", "[NAME:OKBtn]")
	_WaitForIdle(5)
	_UserPause()
EndFunc

; ********************************************************************************

Func _ConvertUnknownWidthWalls($scope)
	Local $win = $gc_wt_convertWalls

	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_convertWalls)
	_WinWaitActivate($win, "")

	If $scope == "Selected" Then
		ControlCommand($win, "", "[NAME:SelectedRB]", "Check", "")
    ElseIf $scope == "AllCurrentLevel" Then
		ControlCommand($win, "", "[NAME:AllCurLvlRB]", "Check", "")
	ElseIf $scope == "AllProject" Then
		ControlCommand($win, "", "[NAME:AllProjectRB]", "Check", "")
	EndIf

	_UserPause()
	ControlClick($win, "", "[NAME:OKBtn]")
	_WaitForIdle(5)
	_UserPause()
EndFunc

; ********************************************************************************

Func _EditRoundingSettings($locRounding, $featRounding, $wallRounding, $tolerance)
	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_settings)
	_WinWaitActivate($gc_wt_settings, "")
	Send("{DOWN}")
	Sleep(200)

	_SetSDC($gc_wt_settings, "LocationRoundingSDC", $locRounding)
	_SetSDC($gc_wt_settings, "FeatureRoundingSDC", $featRounding)
	_SetSDC($gc_wt_settings, "WallWidthSDC", $wallRounding)
	_SetSDC($gc_wt_settings, "GeometryAdjustmentSDC", $tolerance)

	_UserPause()
	ControlClick($gc_wt_settings, "", "[NAME:DoneBtn]")
	_WaitForIdle(2)
	_UserPause()
EndFunc

; ********************************************************************************

Func _SetDC($win, $dcName, $feet, $inches)
	Local $feetTBID = "[NAME:" & $dcName & ":FeetTB]"
	Local $inchesTBID = "[NAME:" & $dcName & ":InchesTB]"
	Local $metricTBID = "[NAME:" & $dcName & ":MetricTB]"
	If (ControlFocus($win, "", $feetTBID) == 0) Then
		If Not($feet == "") Then
			ControlFocus($win, "", $metricTBID)
			ControlSend($win, "", $metricTBID, $feet)
		EndIf
	Else
		If Not($feet == "") Or Not($inches == "") Then
			ControlFocus($win, "", $feetTBID)
			ControlSetText($win, "", $feetTBID, "")
			ControlFocus($win, "", $inchesTBID)
			ControlSetText($win, "", $inchesTBID, "")
	    EndIf
		If Not($feet == "") Then
			ControlFocus($win, "", $feetTBID)
			ControlSend($win, "", $feetTBID, $feet)
	    EndIf
		If Not($inches == "") Then
			ControlFocus($win, "", $inchesTBID)
			ControlSend($win, "", $inchesTBID, $inches)
	    EndIf
    EndIf
EndFunc

; ********************************************************************************

Func _SetSDC($win, $dcName, $dist)
	Local $inchesTBID = "[NAME:" & $dcName & ":InchesTB]"
	Local $metricTBID = "[NAME:" & $dcName & ":MetricTB]"
	If Not($dist == "") Then
		If (ControlFocus($win, "", $inchesTBID) == 0) Then
			ControlFocus($win, "", $metricTBID)
			ControlSend($win, "", $metricTBID, $dist)
		Else
			ControlFocus($win, "", $inchesTBID)
			ControlSend($win, "", $inchesTBID, $dist)
		EndIf
	EndIf
EndFunc

; ************

Func _SetCBSelIndex($win, $ctrlId, $idx)
	If Not($idx == "") Then
		Local $handle = ControlGetHandle($win, "", $ctrlId)
		ControlFocus($win, "", $handle)
		_GUICtrlComboBox_SetCurSel($handle, 0)
		If $idx == "0" Then
			ControlSend($win, "", $handle, "{DOWN}")
			ControlSend($win, "", $handle, "{UP}")
		Else
			ControlSend($win, "", $handle, "{DOWN " & $idx & "}")
		EndIf
	EndIf
EndFunc

; ************

Func _SetLBSelIndex($win, $ctrlId, $idx)
	If Not($idx == "") Then
		Local $handle = ControlGetHandle($win, "", $ctrlId)
		ControlFocus($win, "", $handle)
		_GUICtrlListBox_SetCurSel($handle, 0)
		If $idx == "0" Then
			ControlSend($win, "", $handle, "{DOWN}")
			ControlSend($win, "", $handle, "{UP}")
		Else
			ControlSend($win, "", $handle, "{DOWN " & $idx & "}")
		EndIf
	EndIf
EndFunc

; ************

Func _ExportModel()
	If $gc_isUserPauseBeforeExportModel Then _UserPause(True)
	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_exportModel)
	Sleep(100)
    _UserPause()
EndFunc

; ************

Func _OpenView($viewName)
	Local $win = $gc_wt_openView

	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_openView)
	_WinWaitActivate($win, "")

	Local $ctlId = "[NAME:ViewNameTB]"
	ControlFocus($win, "", $ctlId)
	ControlSetText($win, "", $ctlId, "")
	ControlSend($win, "", $ctlId, $viewName)
	
	WinClose($win, "")
    _UserPause()
	_WaitForIdle(5)
EndFunc

; ************

Func _UserPause($force=False)
	Local $timeout = $gc_userPauseTimeout
	If $force Then $timeout = 180
	If $g_is_running_on_build_server Then $timeout = 0
	If Not($timeout = 0) Then
		Local $res = MsgBox(1, "Pause", "OK = continue; Cancel = end script", $timeout)
		If $res = 2 Then
			Exit
		EndIf
	EndIf
EndFunc

; ************

Func _InitRevitWindow()
	If $gc_isUserPauseAtStart = True Then _UserPause(True)

	While $g_revitWin_handle == ""
		Sleep(500)
		$g_revitWin_handle = WinGetHandle($gc_wt_revit)
	WEnd

	Sleep($gc_revitWin_startDelay)

	If $CmdLine[0] >= 1 AND $CmdLine[1] = "runOnBuildServer" Then
		$g_is_running_on_build_server = True
		; Dismiss the message box announcing that Revit is in demo mode (if presented)
		Local $license_handle = WinWait("Revit Current License Status", "", 10)
		If Not ($license_handle = 0) Then
			If Not WinActive($license_handle) Then WinActivate($license_handle)
			WinWaitActive($license_handle, "", 2)
			Sleep(100)
			Send("{ENTER}")
        EndIf
	EndIf

	if $gc_isRemoteDesktop Then
	   Local $graphics_wt = "Graphics Options - Cannot Use Hardware Acceleration"
	   Local $graphics_handle = WinWait($graphics_wt, "", 10)
	   If Not($graphics_handle = 0) Then
	      If Not WinActive($graphics_handle) Then WinActivate($graphics_handle)
	      WinWaitActive($graphics_handle, "", 2)
	      WinClose($graphics_wt, "")
	   EndIf
	EndIf

	_MoveRevitMainWindow()

;	if $gc_isImportKeyboardShortcuts Then
;		; Import keyboard shortcuts file
;		_WinWaitActivate($g_revitWin_handle, "")
;		Send("ks")
;		_WinWaitActivate("Keyboard Shortcuts", "")
;		Send("{TAB}")
;		Send("{TAB}")
;		Send("{TAB}")
;		Send("{SPACE}")
;		_WinWaitActivate("Import Keyboard Shortcuts File", "")
;		Send(@WorkingDir & "\" & $gc_fn_kbshortcuts)
;		Send("{ENTER}")
;		_WinWaitActivate("Import Keyboard Shortcuts File", "")
;		Send("{SPACE}")
;		_WinWaitActivate("Keyboard Shortcuts", "")
;		Send("{ESC}")
;	EndIf

	; Disable auto-save
	_WinWaitActivate($g_revitWin_handle, "")
	Send($gc_kb_settings)
	Sleep(100)
	_WinWaitActivate($gc_wt_settings, "")
	Send("{DOWN}")
	Send("{DOWN}")
	Send("{DOWN}")
	Send("{DOWN}")
	Sleep(100)

	Local $ctlId = "[NAME:AutoSaveTB]"
	ControlFocus($gc_wt_settings, "", $ctlId)
	Sleep(100)
	ControlSend($gc_wt_settings, "", $ctlId, "0")
	Sleep(100)
	_UserPause()
	ControlClick($gc_wt_settings, "", "[Name:DoneBtn]")

	; Ensure mouse is in window so the cursor reflects this process's state
	Sleep(100)
	MouseMove(400, 400)
	Sleep(100)

	; Enable thin lines
	Send("tl")

	_UserPause()
EndFunc

; ************

Func _KillRevit()
     _WinWaitActivate($g_revitWin_handle, "")
     WinKill($g_revitWin_handle, "")
EndFunc

; ************

Func _ClickInPlanView($x, $y)
	Local $ctlPos[4]
	Sleep(100)
	$ctlPos = ControlGetPos($g_revitWin_handle, "", $gc_ctlId_view)
	MouseMove($ctlPos[0] + $x, $ctlPos[1] + $y, $gc_mouseSpeed)
	Sleep(500)
	MouseClick("primary")
	Sleep(100)
EndFunc

; ************

Func _DragInPlanView($x1, $y1, $x2, $y2)
	Local $ctlPos[4]
	$ctlPos = ControlGetPos($g_revitWin_handle, "", $gc_ctlId_view)
	MouseClickDrag("primary", $ctlPos[0] + $x1, $ctlPos[1] + $y1, $ctlPos[0] + $x2, $ctlPos[1] + $y2)
	Sleep(100)
EndFunc

; ************

Func _MoveRevitMainWindow()
	Local $winPos[4]
	Local $winWidth
	Local $winHeight
	Local $viewCtlPos[4]
	Local $viewCtlWidth
	Local $viewCtlHeight
	Local $newWinWidth
	Local $newWinHeight

	If Not WinActive($g_revitWin_handle) Then WinActivate($g_revitWin_handle)
	WinWaitActive($g_revitWin_handle, "", 10)
	Sleep(200)
	$winPos = WinGetPos($g_revitWin_handle)
	$winWidth = $winPos[2]
	$winHeight = $winPos[3]
	Sleep(200)
	$viewCtlPos = ControlGetPos($g_revitWin_handle, "", $gc_ctlId_view)
	$viewCtlWidth = $viewCtlPos[2]
	$viewCtlHeight = $viewCtlPos[3]
	Sleep(200)
	$newWinWidth = $winWidth + ($gc_viewCtl_width - $viewCtlWidth)
	$newWinHeight = $winHeight + ($gc_viewCtl_height - $viewCtlHeight)
	WinMove($g_revitWin_handle, "", 0, 0, $newWinWidth, $newWinHeight)
	Sleep(200)
EndFunc

; ************

Func _WaitForIdle($wait)
	Local Const $checkInterval = 20  ; msec

	Local Const $timeout = ($wait * 1000) / $checkInterval
    Local Const $waitCursorId = 15

	Local $countDown = $timeout
	Local $curCursorState
    Local $prevCursorState = -1

	Local $isIdle = False
	Do
		Sleep($checkInterval)
		$countDown -= 1
		If $countDown = 0 Then
			$isIdle = True
		Else
			$prevCursorState = $curCursorState
			$curCursorState = MouseGetCursor()
			If $prevCursorState = $waitCursorId AND $curCursorState <> $waitCursorId Then
				$countDown = $timeout
			EndIf
		EndIf
	Until $isIdle = True
EndFunc

; ************

Func _WinWaitActivate($title, $text)
    WinWait($title, $text, $gc_winWaitTimeout)
    If Not WinActive($title, $text) Then WinActivate($title, $text)
    WinWaitActive($title, $text, $gc_winWaitTimeout)
EndFunc
