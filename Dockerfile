FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["signup-verification.csproj", "./"]
RUN dotnet restore "./signup-verification.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "signup-verification.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "signup-verification.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS http://*:4000

ENTRYPOINT ["dotnet", "signup-verification.dll"]
