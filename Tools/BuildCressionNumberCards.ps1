param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

Add-Type -AssemblyName System.Drawing

$sourceRoot = Join-Path $ProjectRoot "skullking"
$assetRoot = Join-Path $ProjectRoot "Assets/Art/Cards/CROSSION"
$outputRoot = Join-Path $assetRoot "Composed/Numbers"
$previewPath = Join-Path $ProjectRoot "CROSSION-number-cards-preview.png"
$representativeNumbers = @(1, 4, 7, 11)

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$worlds = @(
    # FrameLeft/Right/Top/Bottom은 프레임 직선부의 실제 바깥 경계입니다.
    # 이 네 기준선을 카드 캔버스 끝에 맞춰 상단만 비거나 비율이 달라 보이는 현상을 없앱니다.
    @{ Name = "Wuxia"; Frame = "wuxia-soldier-frame-v1-832x1216.png"; KeyBlack = $false; FrameLeft = 21; FrameRight = 828; FrameTop = 54; FrameBottom = 1212; KeyThreshold = 0; FillX = 52; FillY = 76; FillW = 728; FillH = 1090; ArtX = 78; ArtY = 109; ArtW = 676; ArtH = 1040 },
    @{ Name = "Fantasy"; Frame = "fantasy-soldier-frame-v1-832x1216.png"; KeyBlack = $false; FrameLeft = 22; FrameRight = 822; FrameTop = 58; FrameBottom = 1213; KeyThreshold = 0; FillX = 50; FillY = 76; FillW = 732; FillH = 1090; ArtX = 70; ArtY = 108; ArtW = 691; ArtH = 1052 },
    @{ Name = "Cyberpunk"; Frame = "cyberpunk-soldier-frame-v1-832x1216.png"; KeyBlack = $true; FrameLeft = 23; FrameRight = 812; FrameTop = 38; FrameBottom = 1200; KeyThreshold = 7; FillX = 55; FillY = 72; FillW = 722; FillH = 1080; ArtX = 94; ArtY = 110; ArtW = 644; ArtH = 1022 },
    @{ Name = "Void"; Frame = "frame_void.png"; KeyBlack = $true; FrameLeft = 30; FrameRight = 801; FrameTop = 50; FrameBottom = 1136; KeyThreshold = 4; FillX = 55; FillY = 72; FillW = 722; FillH = 1065; ArtX = 77; ArtY = 100; ArtW = 678; ArtH = 1010 }
)

foreach ($world in $worlds) {
    Copy-Item -LiteralPath (Join-Path $sourceRoot $world.Frame) -Destination (Join-Path $assetRoot $world.Frame) -Force
}

function Get-VisibleBounds([System.Drawing.Bitmap]$Bitmap) {
    $minX = $Bitmap.Width
    $minY = $Bitmap.Height
    $maxX = -1
    $maxY = -1

    # 흰색/투명 배경은 제외하되, 희미한 이펙트와 무기는 내용으로 보존합니다.
    for ($y = 0; $y -lt $Bitmap.Height; $y += 2) {
        for ($x = 0; $x -lt $Bitmap.Width; $x += 2) {
            $pixel = $Bitmap.GetPixel($x, $y)
            if ($pixel.A -gt 20 -and ($pixel.R -lt 240 -or $pixel.G -lt 240 -or $pixel.B -lt 240)) {
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }

    if ($maxX -lt 0 -or $maxY -lt 0) {
        return [System.Drawing.Rectangle]::new(0, 0, $Bitmap.Width, $Bitmap.Height)
    }

    # 자동 크롭이 머리카락, 무기 끝, 오라를 건드리지 않도록 원본 기준 안전 여백을 복원합니다.
    $marginX = [int][Math]::Round($Bitmap.Width * 0.025)
    $marginY = [int][Math]::Round($Bitmap.Height * 0.02)
    $left = [Math]::Max(0, $minX - $marginX)
    $top = [Math]::Max(0, $minY - $marginY)
    $right = [Math]::Min($Bitmap.Width - 1, $maxX + $marginX)
    $bottom = [Math]::Min($Bitmap.Height - 1, $maxY + $marginY)
    return [System.Drawing.Rectangle]::new($left, $top, $right - $left + 1, $bottom - $top + 1)
}

foreach ($world in $worlds) {
    $framePath = Join-Path $assetRoot $world.Frame
    $frame = [System.Drawing.Bitmap]::new($framePath)
    try {
        # 프레임 직선부의 네 바깥 경계를 캔버스 끝에 맞추는 변환입니다.
        # 내부 흰 받침과 원화 창에도 같은 변환을 적용해야 위쪽 띠나 비율 차이가 생기지 않습니다.
        $frameScaleX = 831.0 / ($world.FrameRight - $world.FrameLeft)
        $frameScaleY = 1215.0 / ($world.FrameBottom - $world.FrameTop)
        $frameWidth = [int][Math]::Round(832 * $frameScaleX)
        $frameHeight = [int][Math]::Round(1216 * $frameScaleY)
        $frameX = [int][Math]::Round(-$world.FrameLeft * $frameScaleX)
        $frameY = [int][Math]::Round(-$world.FrameTop * $frameScaleY)

        $fillX = [int][Math]::Round(($world.FillX - $world.FrameLeft) * $frameScaleX)
        $fillY = [int][Math]::Round(($world.FillY - $world.FrameTop) * $frameScaleY)
        $fillW = [int][Math]::Round($world.FillW * $frameScaleX)
        $fillH = [int][Math]::Round($world.FillH * $frameScaleY)

        $artX = [int][Math]::Round(($world.ArtX - $world.FrameLeft) * $frameScaleX)
        $artY = [int][Math]::Round(($world.ArtY - $world.FrameTop) * $frameScaleY)
        $artW = [int][Math]::Round($world.ArtW * $frameScaleX)
        $artH = [int][Math]::Round($world.ArtH * $frameScaleY)

        for ($artIndex = 1; $artIndex -le 4; $artIndex++) {
            $number = $representativeNumbers[$artIndex - 1]
            $artPath = Join-Path $assetRoot ("{0}_private_{1:00}.png" -f $world.Name, $artIndex)
            $extendedPath = Join-Path $assetRoot ("ExtendedSources/{0}_private_{1:00}_extended.png" -f $world.Name, $artIndex)
            if (Test-Path -LiteralPath $extendedPath) {
                $artPath = $extendedPath
            }
            $art = [System.Drawing.Bitmap]::new($artPath)
            try {
                $canvas = [System.Drawing.Bitmap]::new(832, 1216, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
                $graphics = [System.Drawing.Graphics]::FromImage($canvas)
                try {
                    $graphics.Clear([System.Drawing.Color]::Transparent)
                    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
                    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
                    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

                    # 프레임 안쪽 전체에 흰색 받침을 먼저 깔아 원화와 테두리 사이의
                    # 1~2px 투명 틈도 게임 화면에 나타나지 않게 합니다.
                    $graphics.FillRectangle(
                        [System.Drawing.Brushes]::White,
                        [System.Drawing.Rectangle]::new($fillX, $fillY, $fillW, $fillH))

                    # 원화 바깥의 흰색/투명 여백만 제거한 뒤 실제 내용 전체가
                    # 프레임 안쪽 창에 들어오도록 비율을 유지합니다.
                    $artWindow = [System.Drawing.Rectangle]::new($artX, $artY, $artW, $artH)
                    $graphics.SetClip($artWindow)
                    $graphics.FillRectangle([System.Drawing.Brushes]::White, $artWindow)
                    $sourceBounds = Get-VisibleBounds $art
                    $fitScale = [Math]::Min($artW / $sourceBounds.Width, $artH / $sourceBounds.Height)
                    $drawWidth = [int][Math]::Round($sourceBounds.Width * $fitScale)
                    $drawHeight = [int][Math]::Round($sourceBounds.Height * $fitScale)
                    $drawX = $artX + [int][Math]::Round(($artW - $drawWidth) / 2)
                    # 남는 세로 공간은 아래쪽에 두어 머리 위가 비어 보이지 않게 합니다.
                    $drawY = $artY
                    $graphics.DrawImage(
                        $art,
                        [System.Drawing.Rectangle]::new($drawX, $drawY, $drawWidth, $drawHeight),
                        $sourceBounds.X, $sourceBounds.Y, $sourceBounds.Width, $sourceBounds.Height,
                        [System.Drawing.GraphicsUnit]::Pixel)
                    $graphics.ResetClip()

                    if ($world.KeyBlack) {
                        $attributes = [System.Drawing.Imaging.ImageAttributes]::new()
                        try {
                            $attributes.SetColorKey(
                                [System.Drawing.Color]::FromArgb(0, 0, 0),
                                # 검은 외곽선까지 제거하면 흰 톱니가 생기므로 낮은 범위의
                                # 배경색만 제거하고 굵기는 FrameScale로 조절합니다.
                                [System.Drawing.Color]::FromArgb($world.KeyThreshold, $world.KeyThreshold, $world.KeyThreshold),
                                [System.Drawing.Imaging.ColorAdjustType]::Bitmap)
                            $graphics.DrawImage($frame, [System.Drawing.Rectangle]::new($frameX, $frameY, $frameWidth, $frameHeight), 0, 0, $frame.Width, $frame.Height, [System.Drawing.GraphicsUnit]::Pixel, $attributes)
                        }
                        finally { $attributes.Dispose() }
                    }
                    else {
                        $graphics.DrawImage($frame, [System.Drawing.Rectangle]::new($frameX, $frameY, $frameWidth, $frameHeight))
                    }
                }
                finally { $graphics.Dispose() }

                $outputPath = Join-Path $outputRoot ("{0}_{1:00}.png" -f $world.Name, $number)
                $canvas.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
                $canvas.Dispose()
            }
            finally { $art.Dispose() }
        }
    }
    finally { $frame.Dispose() }
}

# 한 화면에서 네 세계와 모든 숫자를 확인할 수 있는 검수용 시트입니다.
$thumbWidth = 165
$thumbHeight = 152
$labelHeight = 24
$columns = 4
$rows = 4
$sheet = [System.Drawing.Bitmap]::new($columns * $thumbWidth, $rows * ($thumbHeight + $labelHeight), [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$sheetGraphics = [System.Drawing.Graphics]::FromImage($sheet)
$sheetFont = [System.Drawing.Font]::new("Arial", 13, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$sheetBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
$sheetGraphics.Clear([System.Drawing.Color]::FromArgb(22, 24, 30))
$sheetGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
for ($row = 0; $row -lt $worlds.Count; $row++) {
    for ($column = 0; $column -lt $representativeNumbers.Count; $column++) {
        $number = $representativeNumbers[$column]
        $cardPath = Join-Path $outputRoot ("{0}_{1:00}.png" -f $worlds[$row].Name, $number)
        $card = [System.Drawing.Bitmap]::new($cardPath)
        try {
            $x = $column * $thumbWidth
            $y = $row * ($thumbHeight + $labelHeight)
            $sheetGraphics.DrawImage($card, [System.Drawing.Rectangle]::new($x, $y, $thumbWidth, $thumbHeight))
            $sheetGraphics.DrawString(("{0} private {1:00}" -f $worlds[$row].Name, ($column + 1)), $sheetFont, $sheetBrush, $x + 3, $y + $thumbHeight + 3)
        }
        finally { $card.Dispose() }
    }
}
$sheet.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$sheetBrush.Dispose()
$sheetFont.Dispose()
$sheetGraphics.Dispose()
$sheet.Dispose()

Write-Output "Generated 16 grouped cards in $outputRoot"
Write-Output "Preview: $previewPath"
