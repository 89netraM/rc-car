$dockerfilePath = Join-Path $PSScriptRoot "publish.containerfile";
$outputPath = Join-Path $PSScriptRoot "release";

podman build --tag rc-car-publish --file $dockerfilePath $PSScriptRoot;
if (-not $?) {
    return;
}

$containerId = podman create rc-car-publish;

try {
    Remove-Item $outputPath -Recurse -ErrorAction SilentlyContinue;
    podman cp ${containerId}:/out $outputPath;
}
finally {
    podman rm $containerId;
}
