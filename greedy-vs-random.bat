@echo off

dotnet build -c Release

if not exist Results mkdir Results

LostCities.Game\bin\Release\net7.0\LostCities.Game.exe ^
    --player1 Examples\bin\Release\net7.0\Greedy.exe ^
    --player2 Examples\bin\Release\net7.0\Random.exe ^
    --games 100 ^
    --parallel 8 ^
    --output Results\greedy-vs-random-100.json
