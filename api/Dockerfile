FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS builder
WORKDIR /src

COPY Directory.Build.props ./
COPY App/App.csproj ./App/
COPY Infra/Infra.csproj ./Infra/
COPY Api/Api.csproj ./Api/
RUN dotnet restore Api/Api.csproj

COPY . .
RUN dotnet publish Api/Api.csproj \
        -c Release \
        -o /publish \
        /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=builder /publish ./

ENTRYPOINT ["dotnet", "Api.dll"]