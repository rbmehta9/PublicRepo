FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /src

# Copy project files
COPY ["ConsumerTests/Api.Consumer.Tests.csproj", "ConsumerTests/"]
COPY ["API/API.csproj", "API/"]

# Restore dependencies
RUN dotnet restore "ConsumerTests/Api.Consumer.Tests.csproj"

# Copy source code
COPY ["ConsumerTests/", "ConsumerTests/"]
COPY ["API/", "API/"]

# Build and run consumer tests
WORKDIR /src/ConsumerTests
RUN dotnet build -c Release
CMD ["dotnet", "test", "--logger", "trx", "--results-directory", "/testresults"]