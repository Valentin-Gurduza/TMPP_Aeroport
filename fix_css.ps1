$files = Get-ChildItem -Path "Views" -Recurse -Filter "*.cshtml"
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    # 1. Containers with bg-white but no dark mode background
    $content = [regex]::Replace($content, 'class="([^"]*)\bbg-white\b(?![^"]*dark:bg-)([^"]*)"', {
        param($m)
        $cls = $m.Value
        $cls = $cls -replace '\bbg-white\b', 'bg-white dark:bg-slate-900'
        if ($cls -notmatch 'dark:text-') {
            $cls = $cls -replace '\btext-slate-700\b', 'text-slate-700 dark:text-slate-300'
            $cls = $cls -replace '\btext-slate-800\b', 'text-slate-800 dark:text-slate-200'
        }
        if ($cls -notmatch 'dark:border-') {
            $cls = $cls -replace '\bborder-slate-200\b', 'border-slate-200 dark:border-slate-800'
            $cls = $cls -replace '\bborder-slate-300\b', 'border-slate-300 dark:border-slate-700'
        }
        return $cls
    })

    # 2. Containers with bg-slate-800 but no light mode background
    $content = [regex]::Replace($content, 'class="([^"]*)(?<!dark:)\bbg-slate-800\b(?![^"]*dark:bg-)([^"]*)"', {
        param($m)
        $cls = $m.Value
        
        # If it has text-white, we swap it to text-slate-800 dark:text-white
        if ($cls -match '\btext-white\b') {
            $cls = $cls -replace '\bbg-slate-800\b', 'bg-white dark:bg-slate-800'
            $cls = $cls -replace '\btext-white\b', 'text-slate-800 dark:text-white'
        } else {
            $cls = $cls -replace '\bbg-slate-800\b', 'bg-white dark:bg-slate-800'
        }
        
        if ($cls -notmatch 'dark:border-') {
            $cls = $cls -replace '\bborder-slate-700\b', 'border-slate-200 dark:border-slate-700'
        }
        
        return $cls
    })

    Set-Content -Path $file.FullName -Value $content
}
Write-Host "Replaced CSS classes"
