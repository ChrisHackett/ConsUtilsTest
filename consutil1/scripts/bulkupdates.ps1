$dirs = Get-Content bulkUpdatesList.txt
$pullCmd="k:\_git\gitpulls.ps1"
$pullCmd1="dir"
foreach ($dir in $dirs) {
   Set-Location -Path $dir
   Get-Location
   powershell k:\_git\gitpulls.ps1
 }