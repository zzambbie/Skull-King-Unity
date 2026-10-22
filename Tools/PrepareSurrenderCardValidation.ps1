$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$validationRoot = Join-Path $projectRoot 'output/surrender-unity-validation'
New-Item -ItemType Directory -Force -Path "$validationRoot/Assets/Art/Cards/CROSSION/Composed", "$validationRoot/Assets/Scripts/Data", "$validationRoot/Assets/Editor", "$validationRoot/ProjectSettings", "$validationRoot/Packages" | Out-Null
foreach ($name in @('CardData', 'CardSpriteDatabase')) {
    Copy-Item -LiteralPath "$projectRoot/Assets/Scripts/Data/$name.cs", "$projectRoot/Assets/Scripts/Data/$name.cs.meta" -Destination "$validationRoot/Assets/Scripts/Data" -Force
}
Copy-Item -LiteralPath "$projectRoot/Assets/Editor/CressionCardAssetSetup.cs", "$projectRoot/Assets/Editor/CressionCardAssetSetup.cs.meta" -Destination "$validationRoot/Assets/Editor" -Force
Copy-Item -LiteralPath "$projectRoot/Assets/CardSpriteDatabase.asset", "$projectRoot/Assets/CardSpriteDatabase.asset.meta" -Destination "$validationRoot/Assets" -Force
Copy-Item -LiteralPath "$projectRoot/ProjectSettings/ProjectVersion.txt" -Destination "$validationRoot/ProjectSettings" -Force

Add-Type -AssemblyName System.Drawing
$preview = [System.Drawing.Bitmap]::new(1020, 830)
$graphics = [System.Drawing.Graphics]::FromImage($preview)
$graphics.Clear([System.Drawing.Color]::FromArgb(40, 43, 48))
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$font = [System.Drawing.Font]::new('Arial', 12)
try {
    $index = 0
    foreach ($prefix in @('surrender', 'alliance')) {
        $count = if ($prefix -eq 'surrender') { 4 } else { 3 }
        for ($i = 1; $i -le $count; $i++) {
            $name = '{0}_{1:00}_card.png' -f $prefix, $i
            $source = "$projectRoot/Assets/Art/Cards/CROSSION/Composed/$name"
            Copy-Item -LiteralPath $source, "$source.meta" -Destination "$validationRoot/Assets/Art/Cards/CROSSION/Composed" -Force
            $image = [System.Drawing.Bitmap]::new($source)
            try {
                if ($image.Width -ne 832 -or $image.Height -ne 1216) { throw "Wrong dimensions: $name" }
                $column = if ($prefix -eq 'surrender') { $i - 1 } else { $i - 1 }
                $row = if ($prefix -eq 'surrender') { 0 } else { 1 }
                $x = 12 + $column * 254
                $y = 10 + $row * 410
                $graphics.DrawImage($image, $x, $y, 240, 351)
                $graphics.DrawString($name, $font, [System.Drawing.Brushes]::White, $x, ($y + 360))
            } finally { $image.Dispose() }
            $index++
        }
    }
    $preview.Save("$projectRoot/output/card-back/surrender-cards-preview.png", [System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $font.Dispose()
    $graphics.Dispose()
    $preview.Dispose()
}
Write-Output "Prepared seven cards for isolated Unity validation: $validationRoot"
