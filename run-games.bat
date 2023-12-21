@echo off

set PLAYER1=Greedy
set PLAYER2=Random
set /A GAME_COUNT=100
set /A PARALLEL=8

set CONFIG=Release

if NOT "%1"=="" (
    set PLAYER1=%1
    set PLAYER2=%1
    
    if NOT "%2"=="" (
        set PLAYER2=%2
    )
)

set GAME_EXE=LostCities.Game\bin\%CONFIG%\net7.0\LostCities.Game.exe
set BOT_EXE=Examples\bin\%CONFIG%\net7.0
set OUTPUT=Results\%PLAYER1%-vs-%PLAYER2%-%GAME_COUNT%.json

dotnet build -c %CONFIG%

if not exist Results mkdir Results

echo %PLAYER1% vs %PLAYER2%

%GAME_EXE% ^
    --player1 %BOT_EXE%\Greedy.exe ^
    --player2 %BOT_EXE%\Random.exe ^
    --games %GAME_COUNT% ^
    --parallel %PARALLEL% ^
    --output %OUTPUT%
