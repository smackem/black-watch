FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["src/BlackWatch.WebApp/BlackWatch.WebApp.csproj", "BlackWatch.WebApp/"]
RUN dotnet restore "BlackWatch.WebApp/BlackWatch.WebApp.csproj"
COPY src/BlackWatch.WebApp BlackWatch.WebApp
WORKDIR "/src/BlackWatch.WebApp"
RUN dotnet build "BlackWatch.WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlackWatch.WebApp.csproj" -c Release -o /app/publish

FROM nginx:alpine
COPY --from=publish /app/publish/wwwroot /usr/local/webapp/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf