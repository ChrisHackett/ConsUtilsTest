$localList = Test-Path gitSrcDirs.txt
$sttdir = Get-Location
if (!$localList) {
    $dirs = Get-ChildItem -Directory
}
else {
    $dirs = Get-Content gitSrcDirs.txt
}
$dirs
$sttdir
foreach ($dir in $dirs) {
   Set-Location -Path $dir
   Get-Location
   git pull
   Set-Location -Path $sttdir
 }
