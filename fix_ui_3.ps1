$dir = "g:\VS Projects\pj_TMPP\TMPP_Aeroport\Views"
$files = Get-ChildItem -Path $dir -Recurse -Filter "*.cshtml"
$enc = New-Object System.Text.UTF8Encoding($false)

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName, $enc)
    $changed = $false

    # Match class attributes containing bg-white without any dark:bg- class
    if ($content -match 'class="[^"]*\bbg-white\b[^"]*"') {
        $content = [regex]::Replace($content, '(?<prefix>class="[^"]*?)\bbg-white\b(?<suffix>[^"]*?")', {
            param($m)
            if ($m.Value -match 'dark:bg-') { return $m.Value }
            return $m.Groups['prefix'].Value + 'bg-white dark:bg-slate-900 dark:text-slate-200' + $m.Groups['suffix'].Value
        })
        $changed = $true
    }

    # Match bg-slate-50
    if ($content -match 'class="[^"]*\bbg-slate-50\b[^"]*"') {
        $content = [regex]::Replace($content, '(?<prefix>class="[^"]*?)\bbg-slate-50\b(?<suffix>[^"]*?")', {
            param($m)
            if ($m.Value -match 'dark:bg-') { return $m.Value }
            return $m.Groups['prefix'].Value + 'bg-slate-50 dark:bg-slate-800 dark:text-slate-200' + $m.Groups['suffix'].Value
        })
        $changed = $true
    }

    # Match hover:bg-slate-50
    if ($content -match 'class="[^"]*\bhover:bg-slate-50\b[^"]*"') {
        $content = [regex]::Replace($content, '(?<prefix>class="[^"]*?)\bhover:bg-slate-50\b(?<suffix>[^"]*?")', {
            param($m)
            if ($m.Value -match 'dark:hover:bg-') { return $m.Value }
            return $m.Groups['prefix'].Value + 'hover:bg-slate-50 dark:hover:bg-slate-800' + $m.Groups['suffix'].Value
        })
        $changed = $true
    }
    
    # Text colors text-slate-700
    if ($content -match 'class="[^"]*\btext-slate-700\b[^"]*"') {
        $content = [regex]::Replace($content, '(?<prefix>class="[^"]*?)\btext-slate-700\b(?<suffix>[^"]*?")', {
            param($m)
            if ($m.Value -match 'dark:text-') { return $m.Value }
            return $m.Groups['prefix'].Value + 'text-slate-700 dark:text-slate-300' + $m.Groups['suffix'].Value
        })
        $changed = $true
    }
    
    # Text colors text-slate-800
    if ($content -match 'class="[^"]*\btext-slate-800\b[^"]*"') {
        $content = [regex]::Replace($content, '(?<prefix>class="[^"]*?)\btext-slate-800\b(?<suffix>[^"]*?")', {
            param($m)
            if ($m.Value -match 'dark:text-') { return $m.Value }
            return $m.Groups['prefix'].Value + 'text-slate-800 dark:text-slate-200' + $m.Groups['suffix'].Value
        })
        $changed = $true
    }

    if ($changed) {
        [System.IO.File]::WriteAllText($file.FullName, $content, $enc)
        Write-Host "Updated UI for $($file.Name)"
    }
}
Write-Host "UI Fix Complete."
