
rmdir /s /q  build.windows
rmdir /s /q  build.mono
rmdir /s /q  Output
cd src
Msbuild Jackett.sln /t:Clean,Build /p:Configuration=Release /verbosity:minimal
cd ..

xcopy src\Jackett.Console\bin\Release build.windows\  /e /y
copy /Y src\Jackett.Service\bin\Release\JackettService.exe build.windows\JackettService.exe
copy /Y src\Jackett.Service\bin\Release\JackettService.exe.config build.windows\JackettService.exe.config
copy /Y src\Jackett.Tray\bin\Release\JackettTray.exe build.windows\JackettTray.exe
copy /Y src\Jackett.Tray\bin\Release\JackettTray.exe.config build.windows\JackettTray.exe.config
copy /Y LICENSE build.windows\LICENSE
copy /Y README.md build.windows\README.md


cd src
Msbuild Jackett.sln /t:Clean
call "C:\Program Files (x86)\Mono\bin\xbuild.bat"  Jackett.sln /t:Build /p:Configuration=Release /verbosity:minimal
cd ..

xcopy src\Jackett.Console\bin\Release build.mono\  /e /y
copy /Y src\Jackett.Service\bin\Release\JackettService.exe build.mono\JackettService.exe
copy /Y src\Jackett.Service\bin\Release\JackettService.exe.config build.mono\JackettService.exe.config
copy /Y src\Jackett.Tray\bin\Release\JackettTray.exe build.mono\JackettTray.exe
copy /Y src\Jackett.Tray\bin\Release\JackettTray.exe.config build.mono\JackettTray.exe.config
copy /Y LICENSE build.mono\LICENSE
copy /Y README.md build.mono\README.md
copy /Y Upstart.config build.mono\Upstart.config

iscc Installer.iss

