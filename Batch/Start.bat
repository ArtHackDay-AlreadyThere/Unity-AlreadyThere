REM "起動されたら自身を最小化した状態で起動し直す"
if not "%~0"=="%~dp0.\%~nx0" (
     start /min cmd /c,"%~dp0.\%~nx0" %*
     exit
)

timeOut /T 30

REM "フォルダ名=appname"
for %%* in (.) do set appname=%%~nx*

:begin
taskkill /F /IM explorer.exe

start /WAIT %appname%.exe -screen-width 1920 -screen-height 1080 -popupwindow

start explorer.exe
timeout /T 10
Goto begin