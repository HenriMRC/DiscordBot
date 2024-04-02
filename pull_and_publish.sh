#To run: sh pull_and_publish.sh

echo starting
git fetch -atp
echo fetched
git pull --ff
echo pulled
sudo rm /apps/DiscordBot/*
echo cleaned
sudo dotnet publish -o=/apps/DiscordBot/
echo published
