
#include-once

;
; Common options
;

Opt("WinDetectHiddenText", 1)
Opt("MouseCoordMode", 2)
Opt("WinTitleMatchMode", 1)
Opt("MustDeclareVars", 1)
Opt("SendKeyDelay", 20)   ; milliseconds; delay between keystrokes

;
; Window Titles
;

Global Const $gc_wt_revit = "[REGEXPTITLE:Autodesk .* 2017 \- .* \- \[.*rvt.*\]]"
Global Const $gc_ctlId_view = "[CLASS:AfxFrameOrView140u; INSTANCE:1]"
Global Const $gc_wt_surveyWall = "Capture Wall"
Global Const $gc_wt_surveyDoor = "Capture Door"
Global Const $gc_wt_surveyWindow = "Capture Window"
Global Const $gc_wt_surveyLine = "Capture Line"
Global Const $gc_wt_surveyObject = "Capture Wall-Hosted Object"
Global Const $gc_wt_selectFamilyCategory = "Select a Family Category"
Global Const $gc_wt_refOpening = "Capture Wall Relative To Opening"
Global Const $gc_wt_copyWalls = "Copy Level-to-Level"
Global Const $gc_wt_convertWalls = "Convert Unknown Width Walls"
Global Const $gc_wt_firstWall = "Orient First Wall"
Global Const $gc_wt_settings = "PKNail Pro Settings"
Global Const $gc_wt_openView = "Open View"

;
; Keyboard Shortcuts
;

Global Const $gc_kb_activateWall = "pka"
Global Const $gc_kb_surveyWall = "pkwa"
Global Const $gc_kb_surveyDoor = "pkdr"
Global Const $gc_kb_surveyWindow = "pkwn"
Global Const $gc_kb_surveyLine = "pkdl"
Global Const $gc_kb_surveyObject = "pkob"
Global Const $gc_kb_copyWalls = "pkcw"
Global Const $gc_kb_convertWalls = "pkuw"
Global Const $gc_kb_setCoordinateSys = "pkco"
Global Const $gc_kb_exportModel = "pkem"
Global Const $gc_kb_openView = "pkov"
Global Const $gc_kb_settings = "pkst"

;
; Misc
;

Global Const $gc_isImportKeyboardShortcuts = False
; Path (relative to working dir of test process) to keyboard shortcuts file
Global Const $gc_fn_kbshortcuts = "PKNailForRevit.TestRunner\tests\KeyboardShortcuts.xml"

Global Const $gc_viewCtl_width = 950
Global Const $gc_viewCtl_height = 700
Global Const $gc_winWaitTimeout = 120  ; seconds

Global Const $gc_isRemoteDesktop = False
Global Const $gc_isUserPauseAtStart = False
Global Const $gc_isUserPauseBeforeExportModel = False
Global Const $gc_userPauseTimeout = 0  ; seconds (0 = no user pause)
Global Const $gc_mouseSpeed = 50  ; the speed to move the mouse in the range 1 (fastest) to 100 (slowest)

; Time to wait after detecting the main window before starting script
Global Const $gc_revitWin_startDelay = 5000  ; milliseconds

; Varibles to control the behavior of the "first wall" dialog

; Flag that indicates whether we've checked to see if the first
; wall dialog has come up; after we have checked this once, there
; is no need to check anymore
Global $g_isSurveyFirstWallChecked = False
; "Legacy" orientation is the old default orientation; this variable
; is overidden to True in the old tests (before the default was changed)
Global $g_isSurveyFirstWallLegacyOrientation = False

; Revit main window handle
Global $g_revitWin_handle = ""

; Flag to indicate whether the script is currently running on the build server
Global $g_is_running_on_build_server = False
