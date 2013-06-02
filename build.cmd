rem USAGE: build release rebuild
rem USAGE: build nunit rebuild test

@SET THISPATH=%~dp0
@SET THISFILENAME=%~n0

@SET NANT=C:\tools\nant\bin\nant.exe
@SET NANT_TARGETS=%*
@SET NANT_PROPERTIES=

@SET NANT_PROPERTIES=-D:buildroot=%THISPATH%build

@%NANT% -v- -debug- -nologo -buildfile:xserializer.build %NANT_TARGETS% %NANT_PROPERTIES% -logfile:%THISFILENAME%.log
