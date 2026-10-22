param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$composedRoot = Join-Path $ProjectRoot 'Assets/Art/Cards/CROSSION/Composed'
$files = Get-ChildItem -LiteralPath $composedRoot -File -Filter '*.png'

foreach ($file in $files) {
    $source = [System.Drawing.Bitmap]::new($file.FullName)
    try {
        $minX = $source.Width
        $minY = $source.Height
        $maxX = -1
        $maxY = -1

        for ($y = 0; $y -lt $source.Height; $y++) {
            for ($x = 0; $x -lt $source.Width; $x++) {
                if ($source.GetPixel($x, $y).A -gt 8) {
                    if ($x -lt $minX) { $minX = $x }
                    if ($x -gt $maxX) { $maxX = $x }
                    if ($y -lt $minY) { $minY = $y }
                    if ($y -gt $maxY) { $maxY = $y }
                }
            }
        }

        if ($maxX -lt $minX -or $maxY -lt $minY) {
            Write-Warning "Skipped empty image: $($file.Name)"
            continue
        }

        $sourceRect = [System.Drawing.Rectangle]::new(
            $minX,
            $minY,
            $maxX - $minX + 1,
            $maxY - $minY + 1)
        $target = [System.Drawing.Bitmap]::new(
            832,
            1216,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

        try {
            $graphics = [System.Drawing.Graphics]::FromImage($target)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage(
                    $source,
                    [System.Drawing.Rectangle]::new(0, 0, 832, 1216),
                    $sourceRect,
                    [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally {
                $graphics.Dispose()
            }

            $temporaryPath = "$($file.FullName).normalized.tmp.png"
            $target.Save($temporaryPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $target.Dispose()
        }
    }
    finally {
        $source.Dispose()
    }

    Move-Item -LiteralPath $temporaryPath -Destination $file.FullName -Force
    Write-Host "Normalized $($file.Name): $sourceRect -> 832x1216"
}
