@echo off
echo Opening firewall port 5216 for Tourism App...
netsh advfirewall firewall add rule name="Allow Port 5216 TCP" dir=in action=allow protocol=TCP localport=5216
echo Done! Port 5216 is now open.
pause
