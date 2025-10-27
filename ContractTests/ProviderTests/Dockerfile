FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /src

# Copy project files
COPY ["ProviderTests/Api.Provider.Tests.csproj", "ProviderTests/"]
COPY ["API/API.csproj", "API/"]

# Restore dependencies
RUN dotnet restore "ProviderTests/Api.Provider.Tests.csproj"

# Copy source code
COPY ["ProviderTests/", "ProviderTests/"]
COPY ["API/", "API/"]

# Build and run provider tests
WORKDIR /src/ProviderTests
RUN dotnet build -c Release
CMD ["dotnet", "test", "--logger", "trx", "--results-directory", "/testresults"]