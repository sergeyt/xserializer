mkdir .nuget
powershell -Command "(new-object System.Net.WebClient).DownloadFile('http://download-codeplex.sec.s-msft.com/Download/Release?ProjectName=nuget&DownloadId=222685&FileTime=129528596690500000&Build=20588', '.nuget\NuGet.exe')"
.nuget\NuGet.exe
del .nuget\NuGet.exe.old
