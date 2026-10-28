Add-Type -AssemblyName System.IO.Compression.FileSystem
$book = [IO.Compression.ZipFile]::OpenRead((Join-Path $PSScriptRoot '../../../docs/Ai/Arena3Q_Chuong1_Asset_2D_va_Nguon.xlsx'))
function Read-Part($part) {
    $reader = [IO.StreamReader]::new($book.GetEntry($part).Open())
    try { [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
}
try {
    $strings = @((Read-Part 'xl/sharedStrings.xml').sst.si | ForEach-Object { $_.InnerText })
    $wb = Read-Part 'xl/workbook.xml'
    $rels = Read-Part 'xl/_rels/workbook.xml.rels'
    $all = @()
    foreach ($sheet in $wb.workbook.sheets.sheet) {
        $rid = $sheet.GetAttribute('id', 'http://schemas.openxmlformats.org/officeDocument/2006/relationships')
        $target = ($rels.Relationships.Relationship | Where-Object { $_.Id -eq $rid }).Target
        if ($target.StartsWith('/')) { $part = $target.TrimStart('/') } else { $part = 'xl/' + $target }
        $rows = @()
        foreach ($row in (Read-Part $part).worksheet.sheetData.row) {
            $cells = [ordered]@{}
            foreach ($cell in $row.c) {
                $value = [string]$cell.v
                if ($cell.t -eq 's') { $value = $strings[[int]$cell.v] }
                elseif ($cell.t -eq 'inlineStr') { $value = $cell.is.InnerText }
                $cells[($cell.r -replace '\d', '')] = $value
            }
            $rows += [pscustomobject]$cells
        }
        $all += [pscustomobject]@{ sheet = [string]$sheet.name; rows = $rows }
    }
    $all | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $PSScriptRoot 'workbook-extract.json') -Encoding UTF8
} finally { $book.Dispose() }
