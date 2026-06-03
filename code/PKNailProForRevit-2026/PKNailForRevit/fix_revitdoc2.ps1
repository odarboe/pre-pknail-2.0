$f = "RevitDocument.cs"

function Apply-Replace($old, $new) {
    $c = Get-Content $f -Raw
    $c2 = $c -replace $old, $new
    Set-Content $f $c2 -Encoding UTF8
    $changed = $c.Length - $c2.Length
    Write-Host ("  '{0}' -> '{1}' (delta bytes: {2})" -f $old, $new, $changed)
}

# Already done: Dictionary<int, int> -> Dictionary<long, long>

Write-Host "=== Remaining dictionary types ==="
Apply-Replace 'Dictionary<int,int>', 'Dictionary<long, long>'
Apply-Replace 'Dictionary<int, Wall>', 'Dictionary<long, Wall>'
Apply-Replace 'Dictionary<int, preserveGeometryInfo>', 'Dictionary<long, preserveGeometryInfo>'
Apply-Replace 'Dictionary<int, wallDefinition>', 'Dictionary<long, wallDefinition>'
Apply-Replace 'Dictionary<int, List<ReferenceWithContext>>', 'Dictionary<long, List<ReferenceWithContext>>'

Write-Host "=== RevitSurveyWall app arg removal ==="
Apply-Replace 'RevitSurveyWall\.WallEdge\((app|m_revitApp),\s*', 'RevitSurveyWall.WallEdge('
Apply-Replace 'RevitSurveyWall\.Centerline\((app|m_revitApp),\s*', 'RevitSurveyWall.Centerline('
Apply-Replace 'RevitSurveyWall\.WallIntersectCorner\((app|m_revitApp),\s*', 'RevitSurveyWall.WallIntersectCorner('

Write-Host "=== List<int> for element ID containers ==="
Apply-Replace 'List<int> openingIdVals', 'List<long> openingIdVals'
Apply-Replace 'List<int> excludeWallIDs', 'List<long> excludeWallIDs'
Apply-Replace 'List<int> excludedWallIDs', 'List<long> excludedWallIDs'
Apply-Replace 'List<int> excludeRefWall', 'List<long> excludeRefWall'
Apply-Replace 'List<int> excludeUwwList', 'List<long> excludeUwwList'
Apply-Replace 'List<int> excludedEndWalls', 'List<long> excludedEndWalls'
Apply-Replace 'List<int> traversedWallIds', 'List<long> traversedWallIds'
Apply-Replace 'private List<int> m_faceWallIDs', 'private List<long> m_faceWallIDs'
Apply-Replace 'private List<int> m_preserveCornerWallIds', 'private List<long> m_preserveCornerWallIds'
Apply-Replace 'private List<int> m_validOpeningIDs', 'private List<long> m_validOpeningIDs'
Apply-Replace 'List<int> openingIDs\b', 'List<long> openingIDs'
Apply-Replace 'List<int> validOpeningIDs', 'List<long> validOpeningIDs'
Apply-Replace 'new List<int>\(\)', 'new List<long>()'

# Restore parallelAngleIdxs (index list, NOT element IDs)
Apply-Replace 'List<long> parallelAngleIdxs', 'List<int> parallelAngleIdxs'

Write-Host "=== foreach and variable types ==="
Apply-Replace 'foreach \(int wID\b', 'foreach (long wID'
Apply-Replace '\bint wID = ', 'long wID = '
Apply-Replace '\bint m_origElementId\b', 'long m_origElementId'
Apply-Replace '\bint m_familySymbolID\b', 'long m_familySymbolID'
Apply-Replace '\bint m_instID\b', 'long m_instID'

Write-Host "=== Final counts ==="
(sls "Dictionary<int," $f | measure).Count
(sls "WallEdge\(app\|WallEdge\(m_revitApp\|Centerline\(app" $f | measure).Count
(sls "List<int>" $f | measure).Count
Write-Host "DONE"
