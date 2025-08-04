FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Services/NeoServiceLayer.Services.ZeroKnowledge.Minimal/NeoServiceLayer.Services.ZeroKnowledge.Minimal.csproj .
RUN dotnet restore
COPY src/Services/NeoServiceLayer.Services.ZeroKnowledge.Minimal/Program.cs .
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/build .
EXPOSE 80
ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.ZeroKnowledge.Minimal.dll"]
