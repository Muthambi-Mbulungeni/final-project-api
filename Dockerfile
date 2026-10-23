# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first to restore dependencies
COPY ["Strategies.Api/Strategies.Api.csproj", "Strategies.Api/"]
RUN dotnet restore "Strategies.Api/Strategies.Api.csproj"

# Copy the rest of the source code and build
COPY . .
WORKDIR "/src/Strategies.Api"
RUN dotnet publish "Strategies.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the lighter ASP.NET Core runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose HTTP port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Strategies.Api.dll"]
