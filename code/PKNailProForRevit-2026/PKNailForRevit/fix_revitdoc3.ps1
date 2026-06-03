# Correct fix script for RevitDocument.cs
# Uses [string]::Replace for literal replacements, -replace only for regex patterns
# Runs from the PKNailForRevit directory

$f = "RevitDocument.cs"

function Lit($src, $old, $new) {
    # Literal string replacement (no regex)
    return $src.Replace($old, $new)
}

$c = Get-Content $f -Raw

# === PASS 1: Removed Revit API replacements ===
# 1. ElementId.IntegerValue -> .Value
$c = Lit $c '.IntegerValue' '.Value'

# 2. new ElementId(int.Parse( -> new ElementId(long.Parse(
$c = Lit $c 'new ElementId(int.Parse(' 'new ElementId(long.Parse('
$c = Lit $c 'new ElementId(Int32.Parse(' 'new ElementId(Int64.Parse('

# 3. NewDetailCurve -> DetailLine.Create
$c = Lit $c 'doc.Create.NewDetailCurve(' 'DetailLine.Create('

# 4. ToRevitLine with Application arg
$c = $c -replace 'Utilities\.ToRevitLine\(m_revitApp,\s*', 'Utilities.ToRevitLine('
$c = $c -replace 'Utilities\.ToRevitLine\(app,\s*', 'Utilities.ToRevitLine('

# 5. RevitSurveyWall calls - remove Application arg
$c = $c -replace 'RevitSurveyWall\.WallEdge\(m_revitApp,\s*', 'RevitSurveyWall.WallEdge('
$c = $c -replace 'RevitSurveyWall\.WallEdge\(app,\s*', 'RevitSurveyWall.WallEdge('
$c = $c -replace 'RevitSurveyWall\.Centerline\(m_revitApp,\s*', 'RevitSurveyWall.Centerline('
$c = $c -replace 'RevitSurveyWall\.Centerline\(app,\s*', 'RevitSurveyWall.Centerline('
$c = $c -replace 'RevitSurveyWall\.WallIntersectCorner\(m_revitApp,\s*', 'RevitSurveyWall.WallIntersectCorner('
$c = $c -replace 'RevitSurveyWall\.WallIntersectCorner\(app,\s*', 'RevitSurveyWall.WallIntersectCorner('

# === PASS 2: Element ID container type changes ===
# Dictionary  types
$c = Lit $c 'Dictionary<int, int>' 'Dictionary<long, long>'
$c = Lit $c 'Dictionary<int,int>' 'Dictionary<long, long>'
$c = Lit $c 'Dictionary<int, Wall>' 'Dictionary<long, Wall>'
$c = Lit $c 'Dictionary<int, preserveGeometryInfo>' 'Dictionary<long, preserveGeometryInfo>'
$c = Lit $c 'Dictionary<int, wallDefinition>' 'Dictionary<long, wallDefinition>'
$c = Lit $c 'Dictionary<int, List<ReferenceWithContext>>' 'Dictionary<long, List<ReferenceWithContext>>'

# List<int> for element ID variables
$c = Lit $c 'List<int> openingIdVals' 'List<long> openingIdVals'
$c = Lit $c 'List<int> excludeWallIDs' 'List<long> excludeWallIDs'
$c = Lit $c 'List<int> excludedWallIDs' 'List<long> excludedWallIDs'
$c = Lit $c 'List<int> excludeRefWall' 'List<long> excludeRefWall'
$c = Lit $c 'List<int> excludeUwwList' 'List<long> excludeUwwList'
$c = Lit $c 'List<int> excludedEndWalls' 'List<long> excludedEndWalls'
$c = Lit $c 'List<int> traversedWallIds' 'List<long> traversedWallIds'
$c = Lit $c 'private List<int> m_faceWallIDs' 'private List<long> m_faceWallIDs'
$c = Lit $c 'private List<int> m_preserveCornerWallIds' 'private List<long> m_preserveCornerWallIds'
$c = Lit $c 'private List<int> m_validOpeningIDs' 'private List<long> m_validOpeningIDs'
$c = Lit $c 'List<int> openingIDs' 'List<long> openingIDs'
$c = Lit $c 'List<int> validOpeningIDs' 'List<long> validOpeningIDs'
$c = Lit $c 'new List<int>()' 'new List<long>()'

# Restore parallelAngleIdxs (index list, NOT element IDs - must stay int)
$c = Lit $c 'List<long> parallelAngleIdxs' 'List<int> parallelAngleIdxs'
$c = Lit $c 'new List<int>() parallelAngleIdxs' 'new List<int>() parallelAngleIdxs'  # safety noop

# foreach loop variable for wall IDs
$c = Lit $c 'foreach (int wID' 'foreach (long wID'
$c = $c -replace '\bint wID = ', 'long wID = '

# field declarations for element IDs  
$c = $c -replace '\bint m_origElementId\b', 'long m_origElementId'
$c = $c -replace '\bint m_familySymbolID\b', 'long m_familySymbolID'
$c = $c -replace '\bint m_instID\b', 'long m_instID'

Set-Content $f $c -Encoding UTF8

# Verify
Write-Host "=== Verification ==="
$cnt1 = (sls '.IntegerValue' $f | measure).Count; Write-Host "IntegerValue remaining: $cnt1"
$cnt2 = (sls 'NewDetailCurve' $f | measure).Count; Write-Host "NewDetailCurve remaining: $cnt2"
$cnt3 = (sls 'Dictionary<int,' $f | measure).Count; Write-Host "Dictionary-int remaining: $cnt3"
$cnt4 = (sls 'List<int>' $f | measure).Count; Write-Host "List-int remaining: $cnt4"
$sz = (Get-Item $f).Length; Write-Host "File size: $sz"
Write-Host "DONE"
