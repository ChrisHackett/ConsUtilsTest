$args[0]
$scrPath = Split-Path -parent $PSCommandPath
$scrFile = "\gitSrcDirs.txt"
$pullsFile = "\GitPulls.ps1" 
$srcPulls = $scrPath + $pullsFile
$srcClones = $scrPath + "\gitclones.ps1"
$srcFiles = $scrPath + $scrFile
$srcFilesIgnoreError
[System.Collections.ArrayList]$dirs = "initializeValue1IgnoreError","initializeValue2"
if (!$args[0]) {
	$localList = Test-Path gitSrcDirs.txt
	if (!$localList) {
        $remoteList = Test-Path $srcFiles
        if ($remoteList) {
		    $dirs = Get-Content $srcFiles
		    copy-Item $srcPulls .
		    copy-Item $srcFiles .
		    copy-Item $srcClones  .
        }
	}
	else {
		$dirs = Get-Content gitSrcDirs.txt
	}
}
else {
    $remoteList = Test-Path $srcFiles
    if ($remoteList) {
        $dirs = Get-Content $args[0]
    }
    else {
        $sdirs = Get-ChildItem -Directory -Path $args[0]
        foreach ($dir in $sdirs) {
            $fdir = $args[0]+"\"+$dir
            $dirs.Add($fdir)
        }
    }

}
$sttdir = Get-Location
$dirs
$sttdir
foreach ($dir in $dirs) {
   $dir
   git clone $dir
}
