if %1=='' (
	echo error: url is not specified!
	exit -1
)

if %2=='' (	
	powershell -Command "(new-object System.Net.WebClient).DownloadFile('%1', [System.IO.Path]::GetFileName((new-object System.Uri('%1')).LocalPath))"
) else (
	powershell -Command "(new-object System.Net.WebClient).DownloadFile('%1', '%2')"
)