$file = "g:\VS Projects\pj_TMPP\TMPP_Aeroport\Views\Airport\Index.cshtml"
$enc1252 = [System.Text.Encoding]::GetEncoding(1252)
$encUTF8 = [System.Text.Encoding]::UTF8

$corruptedText = [System.IO.File]::ReadAllText($file, $encUTF8)
$bytes = $enc1252.GetBytes($corruptedText)
$fixedText = $encUTF8.GetString($bytes)

[System.IO.File]::WriteAllText($file + ".fixed", $fixedText, $encUTF8)
Write-Host "Done"
