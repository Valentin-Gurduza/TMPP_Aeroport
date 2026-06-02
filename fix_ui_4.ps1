$dir = "g:\VS Projects\pj_TMPP\TMPP_Aeroport\Views"
$files = Get-ChildItem -Path $dir -Recurse -Filter "*.cshtml"
$enc = New-Object System.Text.UTF8Encoding($false)

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName, $enc)
    $changed = $false

    if ($content -match "material-symbols-outlined") {
        $content = $content -replace "material-symbols-outlined", "material-symbols-rounded"
        $changed = $true
    }

    if ($content -match '<(select|textarea)') {
        $content = [regex]::Replace($content, '(<(?:select|textarea)[^>]*class=")([^"]*)(")', {
            param($m)
            $cls = $m.Groups[2].Value
            if ($cls -notmatch 'dark:bg-') {
                $cls += " dark:bg-slate-900 dark:border-slate-700 dark:text-slate-200"
            }
            return $m.Groups[1].Value + $cls + $m.Groups[3].Value
        })
        $changed = $true
    }

    if ($content -match '<input') {
        $content = [regex]::Replace($content, '(<input[^>]*class=")([^"]*)(")', {
            param($m)
            $fullTag = $m.Groups[1].Value + $m.Groups[2].Value + $m.Groups[3].Value
            # Ignore hidden, submit, button, checkbox, radio
            if ($fullTag -match 'type="hidden"' -or $fullTag -match 'type="submit"' -or $fullTag -match 'type="button"' -or $fullTag -match 'type="checkbox"' -or $fullTag -match 'type="radio"') {
                return $m.Value
            }
            $cls = $m.Groups[2].Value
            if ($cls -notmatch 'dark:bg-') {
                $cls += " dark:bg-slate-900 dark:border-slate-700 dark:text-slate-200 focus:dark:border-blue-500"
            }
            return $m.Groups[1].Value + $cls + $m.Groups[3].Value
        })
        $changed = $true
    }

    if ($changed) {
        [System.IO.File]::WriteAllText($file.FullName, $content, $enc)
        Write-Host "Fixed Icons and Inputs in $($file.Name)"
    }
}
Write-Host "Fix 4 Complete."
