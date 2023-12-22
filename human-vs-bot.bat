@echo off

set PLAYER2=Random
set CONFIG=Release

if NOT "%1"=="" (
    set PLAYER2=%1
)

set GAME_EXE=LostCities.Game\bin\%CONFIG%\net7.0\LostCities.Game.exe
set BOT_EXE=Examples\bin\%CONFIG%\net7.0

dotnet build -c %CONFIG%

%GAME_EXE% ^
    --player2 %BOT_EXE%\Random.exe ^
    --human
