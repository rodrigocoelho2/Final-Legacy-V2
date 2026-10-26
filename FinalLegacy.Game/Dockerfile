# Final Legacy in a container. Build from the repository root:
#   docker build -f FinalLegacy.Game/Dockerfile -t final-legacy .
# and play (-it gives it a real terminal: colours, arrow keys and the letter-by-letter text):
#   docker run -it --rm final-legacy

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY FinalLegacy.Library/ FinalLegacy.Library/
COPY FinalLegacy.Game/ FinalLegacy.Game/
RUN dotnet publish FinalLegacy.Game/FinalLegacy.Game.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
ENV TERM=xterm-256color
COPY --from=build /app .
ENTRYPOINT ["dotnet", "final-legacy.dll"]
