# EnhancedCombineCsFiles.ps1
# With options to exclude certain folders and files

param(
    [string]$OutputFile = "AllCSharpFiles_Combined.txt",
    [string]$RootPath = ".",
    [string[]]$ExcludeFolders = @("bin", "obj", "node_modules", ".git", "packages"),
    [string[]]$ExcludeFiles = @("AssemblyInfo.cs", "GlobalSuppressions.cs"),
    [switch]$IncludeHeaders = $true,
    [switch]$AddLineNumbers = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "C# File Combiner Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Output file: $OutputFile" -ForegroundColor White
Write-Host "Root path: $RootPath" -ForegroundColor White
Write-Host "Excluding folders: $($ExcludeFolders -join ', ')" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan

# Clear output file if it exists
if (Test-Path $OutputFile) {
    Remove-Item $OutputFile
}

# Build exclusion filter for Get-ChildItem
$excludeFilter = @()
foreach ($folder in $ExcludeFolders) {
    $excludeFilter += "-notpath", "*/$folder/*"
}

# Get all .cs files recursively, excluding certain folders
$getChildItemParams = @{
    Path = $RootPath
    Filter = "*.cs"
    Recurse = $true
    File = $true
    ErrorAction = "SilentlyContinue"
}

# Add excludes if we're using -notpath (requires -Path parameter with wildcards)
$csFiles = Get-ChildItem @getChildItemParams | Where-Object {
    $path = $_.FullName
    $exclude = $false
    foreach ($folder in $ExcludeFolders) {
        if ($path -like "*\$folder\*" -or $path -like "*/$folder/*") {
            $exclude = $true
            break
        }
    }
    foreach ($excludeFile in $ExcludeFiles) {
        if ($_.Name -eq $excludeFile) {
            $exclude = $true
            break
        }
    }
    return -not $exclude
}

Write-Host "Found $($csFiles.Count) C# files (after exclusions)" -ForegroundColor Green

# Process each file
$fileCount = 0
$totalLines = 0

foreach ($file in $csFiles | Sort-Object FullName) {
    $fileCount++
    
    # Get relative path
    $relativePath = Resolve-Path -Path $file.FullName -Relative
    
    # Add file header
    if ($IncludeHeaders) {
        "`n`n========== FILE: $relativePath ==========" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
        
        # Add timestamp
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        "// Generated: $timestamp" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
        "// File size: $([math]::Round($file.Length / 1KB, 2)) KB" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
    }
    
    # Read file content
    $content = Get-Content -Path $file.FullName -Encoding UTF8
    
    if ($AddLineNumbers) {
        $lineNumber = 1
        foreach ($line in $content) {
            "{0,6}: {1}" -f $lineNumber, $line | Out-File -FilePath $OutputFile -Append -Encoding UTF8
            $lineNumber++
            $totalLines++
        }
    } else {
        $content | Out-File -FilePath $OutputFile -Append -Encoding UTF8
        $totalLines += $content.Count
    }
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "✅ Completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "Files processed: $fileCount" -ForegroundColor White
Write-Host "Total lines: $totalLines" -ForegroundColor White
Write-Host "Output file: $OutputFile" -ForegroundColor White
Write-Host "File size: $([math]::Round((Get-Item $OutputFile).Length / 1KB, 2)) KB" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Green