param(
    [string]$Root = ".",
    [switch]$IncludeAgentApplications = $false,
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

function Write-Section($msg) {
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor DarkCyan
    Write-Host $msg -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor DarkCyan
}

function Write-Info($msg) {
    Write-Host "[INFO] $msg" -ForegroundColor Gray
}

function Write-Ok($msg) {
    Write-Host "[OK]   $msg" -ForegroundColor Green
}

function Write-Warn($msg) {
    Write-Host "[WARN] $msg" -ForegroundColor Yellow
}

function Write-Err($msg) {
    Write-Host "[ERR]  $msg" -ForegroundColor Red
}

function Resolve-RootPath([string]$path) {
    return (Resolve-Path $path).Path
}

function New-Backup {
    param([string]$ProjectRoot)

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupDir = Join-Path $ProjectRoot "_backup_before_agent_cleanup_$timestamp"

    Write-Info "Creating backup: $backupDir"

    if ($DryRun) {
        Write-Warn "DryRun enabled. Backup not physically created."
        return $backupDir
    }

    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

    Get-ChildItem -Path $ProjectRoot -Recurse -File | Where-Object {
        $_.FullName -notmatch '\\bin\\|\\obj\\|\\node_modules\\|\\\.git\\|\\_backup_before_agent_cleanup_'
    } | ForEach-Object {
        $relative = $_.FullName.Substring($ProjectRoot.Length).TrimStart('\','/')
        $dest = Join-Path $backupDir $relative
        $destDir = Split-Path $dest -Parent

        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }

        Copy-Item $_.FullName $dest -Force
    }

    Write-Ok "Backup created."
    return $backupDir
}

function Get-TargetFiles {
    param([string]$ProjectRoot)

    return Get-ChildItem -Path $ProjectRoot -Recurse -File -Include *.cs,*.csproj,*.json,*.md | Where-Object {
        $_.FullName -notmatch '\\bin\\|\\obj\\|\\node_modules\\|\\\.git\\|\\_backup_before_agent_cleanup_'
    }
}

function Replace-TextInFile {
    param(
        [string]$FilePath,
        [array]$Rules,
        [ref]$ChangedFiles,
        [ref]$TotalReplacements
    )

    $content = Get-Content -Path $FilePath -Raw
    $original = $content
    $fileReplacementCount = 0

    foreach ($rule in $Rules) {
        $pattern = $rule.Pattern
        $replacement = $rule.Replacement

        $matches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
        if ($matches.Count -gt 0) {
            $content = [regex]::Replace(
                $content,
                $pattern,
                $replacement,
                [System.Text.RegularExpressions.RegexOptions]::Multiline
            )
            $fileReplacementCount += $matches.Count
        }
    }

    if ($content -ne $original) {
        $ChangedFiles.Value += $FilePath
        $TotalReplacements.Value += $fileReplacementCount

        Write-Info "Updated: $FilePath ($fileReplacementCount replacements)"

        if (-not $DryRun) {
            Set-Content -Path $FilePath -Value $content -Encoding UTF8
        }
    }
}

function Remove-FilesByName {
    param(
        [string]$ProjectRoot,
        [string[]]$Names
    )

    $removed = @()

    foreach ($name in $Names) {
        $matches = Get-ChildItem -Path $ProjectRoot -Recurse -File -Filter $name -ErrorAction SilentlyContinue | Where-Object {
            $_.FullName -notmatch '\\bin\\|\\obj\\|\\node_modules\\|\\\.git\\|\\_backup_before_agent_cleanup_'
        }

        foreach ($file in $matches) {
            Write-Warn "Deleting file: $($file.FullName)"
            $removed += $file.FullName

            if (-not $DryRun) {
                Remove-Item $file.FullName -Force
            }
        }
    }

    return $removed
}

function Remove-DirectoriesByName {
    param(
        [string]$ProjectRoot,
        [string[]]$Names
    )

    $removed = @()

    foreach ($name in $Names) {
        $dirs = Get-ChildItem -Path $ProjectRoot -Recurse -Directory -ErrorAction SilentlyContinue | Where-Object {
            $_.Name -eq $name -and $_.FullName -notmatch '\\bin\\|\\obj\\|\\node_modules\\|\\\.git\\|\\_backup_before_agent_cleanup_'
        }

        foreach ($dir in $dirs) {
            Write-Warn "Deleting directory: $($dir.FullName)"
            $removed += $dir.FullName

            if (-not $DryRun) {
                Remove-Item $dir.FullName -Recurse -Force
            }
        }
    }

    return $removed
}

function Find-RemainingAgentReferences {
    param([string]$ProjectRoot)

    $results = @()

    Get-TargetFiles -ProjectRoot $ProjectRoot | ForEach-Object {
        $file = $_.FullName
        $lines = Get-Content -Path $file

        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]

            if ($line -match '\bAgent\b|\bAgents\b|\bAgentId\b|\bIAgentRepository\b|\bAgentRepository\b|\bIAgentService\b|\bAgentService\b') {
                if (-not $IncludeAgentApplications -and $line -match 'AgentApplication|AgentApplications') {
                    continue
                }

                $results += [PSCustomObject]@{
                    File = $file
                    Line = $i + 1
                    Text = $line.Trim()
                }
            }
        }
    }

    return $results
}

$projectRoot = Resolve-RootPath $Root

Write-Section "ZansiHustle Agent Cleanup Script"
Write-Info "Project root: $projectRoot"
Write-Info "IncludeAgentApplications: $IncludeAgentApplications"
Write-Info "DryRun: $DryRun"

$backupPath = New-Backup -ProjectRoot $projectRoot

Write-Section "Deleting Agent-specific files"

$fileNamesToDelete = @(
    "AgentsController.cs",
    "AgentService.cs",
    "IAgentService.cs",
    "AgentRepository.cs",
    "IAgentRepository.cs",
    "AgentConfiguration.cs",
    "Agent.cs",
    "AgentDto.cs",
    "AgentDetailsDto.cs",
    "AgentListItemDto.cs",
    "CreateAgentRequestDto.cs",
    "UpdateAgentRequestDto.cs"
)

if ($IncludeAgentApplications) {
    $fileNamesToDelete += @(
        "AgentApplicationsController.cs",
        "AgentApplicationService.cs",
        "IAgentApplicationService.cs",
        "AgentApplicationRepository.cs",
        "IAgentApplicationRepository.cs",
        "AgentApplicationConfiguration.cs",
        "AgentApplication.cs",
        "AgentApplicationDto.cs",
        "AgentApplicationDetailsDto.cs",
        "AgentApplicationListItemDto.cs",
        "CreateAgentApplicationRequestDto.cs",
        "UpdateAgentApplicationRequestDto.cs"
    )
}

$deletedFiles = Remove-FilesByName -ProjectRoot $projectRoot -Names $fileNamesToDelete

Write-Section "Deleting Agent-specific folders"

$dirNamesToDelete = @(
    "Agents"
)

if ($IncludeAgentApplications) {
    $dirNamesToDelete += @("AgentApplications")
}

$deletedDirs = Remove-DirectoriesByName -ProjectRoot $projectRoot -Names $dirNamesToDelete

Write-Section "Rewriting references"

$rules = @(
    # Remove Agent-related using statements
    @{ Pattern = '(?m)^\s*using\s+ZansiHustle\.Application\.Agents(\.[^;]+)?;\s*\r?\n'; Replacement = '' },
    @{ Pattern = '(?m)^\s*using\s+ZansiHustle\.Application\.Persistence\.Agents(\.[^;]+)?;\s*\r?\n'; Replacement = '' },
    @{ Pattern = '(?m)^\s*using\s+ZansiHustle\.Infrastructure\.Persistence\.Agents(\.[^;]+)?;\s*\r?\n'; Replacement = '' },
    @{ Pattern = '(?m)^\s*using\s+ZansiHustle\.Domain\.Agents(\.[^;]+)?;\s*\r?\n'; Replacement = '' },
    @{ Pattern = '(?m)^\s*using\s+ZansiHustle\.Shared\.Enums\.Agents(\.[^;]+)?;\s*\r?\n'; Replacement = '' },

    # Remove DbSet<Agent> Agents
    @{ Pattern = '(?m)^\s*public\s+DbSet<Agent>\s+Agents\s*\{\s*get;\s*set;\s*\}\s*\r?\n'; Replacement = '' },

    # Service registration removals
    @{ Pattern = '(?m)^\s*services\.AddScoped<IAgentRepository,\s*AgentRepository>\(\);\s*\r?\n'; Replacement = '' },
    @{ Pattern = '(?m)^\s*services\.AddScoped<IAgentService,\s*AgentService>\(\);\s*\r?\n'; Replacement = '' },

    # Constructor / field / interface rewrites
    @{ Pattern = '\bIAgentRepository\b'; Replacement = 'IUserRepository' },
    @{ Pattern = '\bAgentRepository\b'; Replacement = 'UserRepository' },
    @{ Pattern = '\b_agentRepository\b'; Replacement = '_userRepository' },

    # SellerLead linkage rewrites
    @{ Pattern = '\bAgentId\b'; Replacement = 'AssignedUserId' },
    @{ Pattern = '\bAgentName\b'; Replacement = 'AssignedUserName' },
    @{ Pattern = '\brequest\.AssignedUserId\.HasValue\b'; Replacement = 'request.AssignedUserId.HasValue' },
    @{ Pattern = '\bAgent\s*=\s*request\.AssignedUserId\b'; Replacement = 'AssignedUserId = request.AssignedUserId' },

    # Friendly messages/comments
    @{ Pattern = 'Selected agent was not found\.'; Replacement = 'Selected user was not found.' },
    @{ Pattern = '\bagent\b'; Replacement = 'user' },
    @{ Pattern = '\bAgent\b'; Replacement = 'User' },
    @{ Pattern = '\bagents\b'; Replacement = 'team members' },
    @{ Pattern = '\bAgents\b'; Replacement = 'TeamMembers' },

    # FK/index references in code or migration files
    @{ Pattern = 'FK_SellerLeads_Agents_AgentId'; Replacement = 'FK_SellerLeads_Users_AssignedUserId' },
    @{ Pattern = 'IX_SellerLeads_AgentId'; Replacement = 'IX_SellerLeads_AssignedUserId' },
    @{ Pattern = 'principalTable:\s*"Agents"'; Replacement = 'principalTable: "Users"' },
    @{ Pattern = 'name:\s*"FK_SellerLeads_Agents_AgentId"'; Replacement = 'name: "FK_SellerLeads_Users_AssignedUserId"' },
    @{ Pattern = 'HasIndex\(x\s*=>\s*x\.AssignedUserId\)\.HasDatabaseName\("IX_SellerLeads_AgentId"\)'; Replacement = 'HasIndex(x => x.AssignedUserId).HasDatabaseName("IX_SellerLeads_AssignedUserId")' },
    @{ Pattern = 'HasIndex\(x\s*=>\s*x\.AgentId\)'; Replacement = 'HasIndex(x => x.AssignedUserId)' },
    @{ Pattern = 'HasForeignKey\(x\s*=>\s*x\.AgentId\)'; Replacement = 'HasForeignKey(x => x.AssignedUserId)' },

    # Navigation naming
    @{ Pattern = '\.Agent\b'; Replacement = '.AssignedUser' },
    @{ Pattern = '\bAgent\s+\?'; Replacement = 'User ?' },
    @{ Pattern = '\bAgent\s'; Replacement = 'User ' }
)

$changedFiles = New-Object System.Collections.Generic.List[string]
$totalReplacements = 0

Get-TargetFiles -ProjectRoot $projectRoot | ForEach-Object {
    Replace-TextInFile -FilePath $_.FullName -Rules $rules -ChangedFiles ([ref]$changedFiles) -TotalReplacements ([ref]$totalReplacements)
}

Write-Section "Post-clean scan"

$remaining = Find-RemainingAgentReferences -ProjectRoot $projectRoot

if ($remaining.Count -eq 0) {
    Write-Ok "No remaining Agent references found (excluding skipped AgentApplications logic)."
}
else {
    Write-Warn "Remaining Agent references found:"
    $remaining | Select-Object -First 150 | ForEach-Object {
        Write-Host ("{0}:{1} -> {2}" -f $_.File, $_.Line, $_.Text) -ForegroundColor Yellow
    }

    if ($remaining.Count -gt 150) {
        Write-Warn "Only first 150 matches shown."
    }
}

Write-Section "Summary"

Write-Host ("Backup:              {0}" -f $backupPath)
Write-Host ("Deleted files:       {0}" -f $deletedFiles.Count)
Write-Host ("Deleted directories: {0}" -f $deletedDirs.Count)
Write-Host ("Changed files:       {0}" -f $changedFiles.Count)
Write-Host ("Total replacements:  {0}" -f $totalReplacements)
Write-Host ("Remaining refs:      {0}" -f $remaining.Count)

Write-Section "Next actions you must do"

Write-Host "1. Build the solution and fix any compile errors."
Write-Host "2. Update SellerLead entity class: AgentId -> AssignedUserId, Agent nav -> AssignedUser."
Write-Host "3. Update SellerLead DTOs/request models the same way."
Write-Host "4. Update SellerLead EF config to FK to Users."
Write-Host "5. Create EF migration to drop Agents table and move FK from Agents to Users."
Write-Host "6. Add /api/users/team endpoint."

Write-Ok "Done."