FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:b768b444028d3c531de90a356836047e48658cd1e26ba07a539a6f1a052a35d9

WORKDIR /app

RUN apt -qq update
RUN apt -qqy install clang llvm binutils-aarch64-linux-gnu gcc-aarch64-linux-gnu zlib1g-dev:arm64

COPY ./RcCar.slnx ./global.json ./Directory.Build.props ./Directory.Packages.props ./.editorconfig ./.gitignore ./
COPY ./RcCar.WebApi/RcCar.WebApi.csproj ./RcCar.WebApi/packages.lock.json ./RcCar.WebApi/

RUN dotnet restore --locked-mode --runtime linux-arm64

COPY ./RcCar.WebApi/*.cs ./RcCar.WebApi/appsettings.json ./RcCar.WebApi/

WORKDIR /app/RcCar.WebApi

RUN dotnet build --no-restore --runtime linux-arm64 --configuration Release

RUN dotnet publish --no-build --runtime linux-arm64 --configuration Release --output /out
