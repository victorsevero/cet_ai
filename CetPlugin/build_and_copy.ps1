Get-Content .env | ForEach-Object {
    $name, $value = $_.split('=')
    Set-Content env:\$name $value
}

dotnet build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$framework = "net46"
$projectName = "CetPlugin"
$destPath = $env:DEST_PATH

Copy-Item "bin\Debug\$framework\$projectName.dll" -Destination $destPath
