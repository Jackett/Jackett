
rmdir /s /q  build
cd src
Msbuild Jackett.sln /t:Clean,Build /p:Configuration=Release
cd ..

xcopy src\Jackett.Console\bin\Release Build\  /e /y
copy /Y src\Jackett.Service\bin\Release\JackettService.exe build\JackettService.exe
copy /Y src\Jackett.Service\bin\Release\JackettService.exe.config build\JackettService.exe.config
copy /Y src\Jackett.Tray\bin\Release\JackettTray.exe build\JackettTray.exe
copy /Y src\Jackett.Tray\bin\Release\JackettTray.exe.config build\JackettTray.exe.config
cd build
del *.pdb
del *.xml
cd ..
