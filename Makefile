all: test

get-deps:
	bash get-nuget
	echo 'getting dependencies'
	# TODO use packages.config
	# bash nuget install Moq
	# cp ./Moq.4.2.1402.2112/lib/net35/Moq.dll ./Moq.dll
	bash nuget install Newtonsoft.Json
	cp Newtonsoft.Json.6.0.2/lib/net35/Newtonsoft.Json.dll Newtonsoft.Json.dll

compile: get-deps
	gmcs @xserializer.rsp

test: get-deps
	gmcs -pkg:nunit /define:NUNIT @xserializer.rsp
	nunit-console TsvBits.XSerializer.dll
