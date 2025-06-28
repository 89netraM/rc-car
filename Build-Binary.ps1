[CmdletBinding(DefaultParameterSetName = "All")]
param (
    [Parameter(ParameterSetName = "Choose")]
    [switch]
    $Backend,
    [Parameter(ParameterSetName = "Choose")]
    [switch]
    $Frontend
)

$outputPath = Join-Path $PSScriptRoot "release";

if ($Frontend -or $PSCmdlet.ParameterSetName -eq "All") {
    $frontendRoot = Join-Path $PSScriptRoot "frontend";
    $wwwroot = Join-Path $outputPath "wwwroot";

    npm --prefix $frontendRoot run build;
    if (-not $?) {
        return;
    }

    New-Item $wwwroot -Type Directory -Force;
    Copy-Item (Join-Path $frontendRoot "dist" "*") $wwwroot -Recurse -Force;
}

if ($Backend -or $PSCmdlet.ParameterSetName -eq "All") {
    $containerfilePath = Join-Path $PSScriptRoot "publish.containerfile";

    docker build --tag rc-car-publish --file $containerfilePath $PSScriptRoot;
    if (-not $?) {
        return;
    }

    $containerId = docker create rc-car-publish;
    try {
        docker cp ${containerId}:/out $outputPath;
    }
    finally {
        docker rm $containerId;
    }

    $outputOutPath = Join-Path $outputPath "out";
    if (Test-Path $outputOutPath) {
        Move-item "$outputOutPath/*" $outputPath -Force;
        Remove-Item $outputOutPath;
    }
}
