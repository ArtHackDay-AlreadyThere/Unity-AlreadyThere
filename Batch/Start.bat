timeOut /T 30
:begin
taskkill /F /IM explorer.exe
start /WAIT Unity-AlreadyThere.exe -screen-width 1920 -screen-height 1080 -popupwindow
start explorer.exe
timeout /T 10
Goto begin