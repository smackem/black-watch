FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ./*.sln ./
COPY ./src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p src/${file%.*} && mv $file src/${file%.*}; done
COPY ./tests/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p tests/${file%.*} && mv $file tests/${file%.*}; done
RUN dotnet restore
COPY . .
RUN dotnet build "src/BlackWatch.Api/BlackWatch.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/BlackWatch.Api/BlackWatch.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlackWatch.Api.dll"]
