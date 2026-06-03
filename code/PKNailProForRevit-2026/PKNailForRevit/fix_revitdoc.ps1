$f = "RevitDocument.cs"
$c = [System.IO.File]::ReadAllText($f)

# Remove Application arg from RevitSurveyWall calls  
$c = $c -replace 'RevitSurveyWall\.WallEdge\((app|m_revitApp),\s*', 'RevitSurveyWall.WallEdge('
$c = $c -replace 'RevitSurveyWall\.Centerline\((app|m_revitApp),\s*', 'RevitSurveyWall.Centerline('
$c = $c -replace 'RevitSurveyWall\.WallIntersectCorner\((app|m_revitApp),\s*', 'RevitSurveyWall.WallIntersectCorner('

# Element ID dictionaries
$c = $c -replace 'Dictionary<int, int>', 'Dictionary<long, long>'
$c = $c -replace 'Dictionary<int,int>', 'Dictionary<long, long>'
$c = $c -replace 'Dictionary<int, Wall>', 'Dictionary<long, Wall>'
$c = $c -replace 'Dictionary<int, preserveGeometryInfo>', 'Dictionary<long, preserveGeometryInfo>'
$c = $c -replace 'Dictionary<int, wallDefinition>', 'Dictionary<long, wallDefinition>'
$c = $c -replace 'Dictionary<int, List<ReferenceWithContext>>', 'Dictionary<long, List<ReferenceWithContext>>'

# List<int> for specific element ID containers
$c = $c -replace 'List<int> openingIdVals', 'List<long> openingIdVals'
$c = $c -replace 'List<int> excludeWallIDs', 'List<long> excludeWallIDs'
$c = $c -replace 'List<int> excludedWallIDs', 'List<long> excludedWallIDs'
$c = $c -replace 'List<int> excludeRefWall', 'List<long> excludeRefWall'
$c = $c -replace 'List<int> excludeUwwList', 'List<long> excludeUwwList'
$c = $c -replace 'List<int> excludedEndWalls', 'List<long> excludedEndWalls'
$c = $c -replace 'List<int> traversedWallIds', 'List<long> traversedWallIds'
$c = $c -replace 'private List<int> m_faceWallIDs', 'private List<long> m_faceWallIDs'
$c = $c -replace 'private List<int> m_preserveCornerWallIds', 'private List<long> m_preserveCornerWallIds'
$c = $c -replace 'private List<int> m_validOpeningIDs', 'private List<long> m_validOpeningIDs'
$c = $c -replace 'List<int> openingIDs\b', 'List<long> openingIDs'
$c = $c -replace 'List<int> validOpeningIDs', 'List<long> validOpeningIDs'
$c = $c -replace 'new List<int>\(\)', 'new List<long>()'

# Restore parallelAngleIdxs (index list, NOT element IDs)
$c = $c -replace 'List<long> parallelAngleIdxs', 'List<int> parallelAngleIdxs'

# foreach loop variable for wall IDs
$c = $c -replace 'foreach \(int wID\b', 'foreach (long wID'
$c = $c -replace '\bint wID = ', 'long wID = '

# field declarations for element IDs
$c = $c -replace '\bint m_origElementId\b', 'long m_origElementId'
$c = $c -replace '\bint m_familySymbolID\b', 'long m_familySymbolID'
$c = $c -replace '\bint m_instID\b', 'long m_instID'

[System.IO.File]::WriteAllText($f, $c, [System.Text.Encoding]::UTF8)
Write-Host "DONE"
(Select-String -Path $f -SimpleMatch "Dictionary<int,").Count
(Select-String -Path $f -SimpleMatch "WallEdge(app").Count
