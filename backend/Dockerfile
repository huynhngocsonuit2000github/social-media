# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["SocialMedia.Api/SocialMedia.Api.csproj", "SocialMedia.Api/"]
RUN dotnet restore "SocialMedia.Api/SocialMedia.Api.csproj"

# Copy source and publish
COPY SocialMedia.Api/ SocialMedia.Api/
WORKDIR "/src/SocialMedia.Api"
RUN dotnet publish "SocialMedia.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SocialMedia.Api.dll"]
