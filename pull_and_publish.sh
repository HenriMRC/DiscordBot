#!/bin/sh

echo starting
echo -n 'This will run "git reset --hard" and "git clean -fdx". Are you sure you want to continue? [yes/no] '
while [ true ]
do
    read answer
    if [ "$answer" = 'yes' ]
    then
        break
    elif [ "$answer" = 'no' ]
    then    
	exit
    else
        echo -n 'Invalid answer, input "yes" or "no": '
    fi
done
echo reseting
git reset --hard
echo reset
echo cleaning repo
git clean -fdx
echo cleaned repo
echo switching to main
git switch main
echo switched to main
echo fetching
git fetch -apt
echo fetched
echo pulling
git pull --ff
echo pulled
echo cleaning output folder
sudo rm /apps/DiscordBot/*
echo cleaned output folder
echo publishing
sudo dotnet publish -o=/apps/DiscordBot/
echo pubblished
