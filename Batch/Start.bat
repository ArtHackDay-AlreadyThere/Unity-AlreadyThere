REM "�N�����ꂽ�玩�g���ŏ���������ԂŋN��������"
if not "%~0"=="%~dp0.\%~nx0" (
     start /min cmd /c,"%~dp0.\%~nx0" %*
     exit
)

timeOut /T 30

REM "�t�H���_��=appname"
for %%* in (.) do set appname=%%~nx*

:begin
taskkill /F /IM explorer.exe

start /WAIT %appname%.exe -screen-width 1920 -screen-height 1080 -popupwindow

start explorer.exe
timeout /T 10
Goto begin