# Installs what infrastructure/scripts/deploy-all.sh needs on Windows, with winget and only official
# packages, and then runs "aws configure" so the user types the credentials in their own terminal.
# Nothing here stores or shows credentials.
#
#   powershell -ExecutionPolicy Bypass -File infrastructure\scripts\setup-aws-tools.ps1
$ErrorActionPreference = 'Stop'

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    throw 'winget no esta disponible. Instala "App Installer" desde Microsoft Store y repite.'
}

$packages = @(
    @{ Id = 'Amazon.AWSCLI';       Command = 'aws' },
    @{ Id = 'Hashicorp.Terraform'; Command = 'terraform' },
    @{ Id = 'Helm.Helm';           Command = 'helm' },
    @{ Id = 'Kubernetes.kubectl';  Command = 'kubectl' }
)

foreach ($package in $packages) {
    Write-Host "==> $($package.Id)"
    & winget install --id $package.Id --exact --source winget --silent `
        --accept-package-agreements --accept-source-agreements
    # winget returns a non-zero code when the package is already installed and up to date.
    if ($LASTEXITCODE -ne 0) { Write-Host "winget termino con codigo $LASTEXITCODE (normal si ya estaba instalado)." }
}

# See the new tools without opening another terminal.
$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' +
            [Environment]::GetEnvironmentVariable('Path', 'User')

$absent = $packages | Where-Object { -not (Get-Command $_.Command -ErrorAction SilentlyContinue) }
if ($absent) {
    throw "No se encuentran en PATH: $(($absent | ForEach-Object { $_.Command }) -join ', '). Abre otra terminal y repite."
}

Write-Host ''
& aws --version
& terraform version | Select-Object -First 1
& helm version --short
& kubectl version --client | Select-Object -First 1
# Docker Desktop ships an old kubectl that can come first in PATH.
Write-Host "kubectl en uso: $((Get-Command kubectl).Source)"

Write-Host ''
Write-Host 'Ahora escribe tus credenciales de AWS (se guardan en %USERPROFILE%\.aws, nunca en el repositorio).'
Write-Host 'Region: us-east-1. Formato de salida: json.'
& aws configure

Write-Host ''
& aws sts get-caller-identity --query Arn --output text
if ($LASTEXITCODE -ne 0) { throw 'Las credenciales no funcionan. Vuelve a ejecutar: aws configure' }
Write-Host 'Listo. Continua con docs/deploy/aws-staging.md desde Git Bash.'
