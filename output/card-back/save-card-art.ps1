param([Parameter(Mandatory=$true)][string]$Source, [Parameter(Mandatory=$true)][string]$Destination)
Add-Type -AssemblyName System.Drawing
$sourceImage = [System.Drawing.Image]::FromFile($Source)
try {
    $resultImage = [System.Drawing.Bitmap]::new(832,1216,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($resultImage)
        try {
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.DrawImage($sourceImage,0,0,832,1216)
        } finally { $graphics.Dispose() }
        $resultImage.Save($Destination,[System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $resultImage.Dispose() }
} finally { $sourceImage.Dispose() }
$verified = [System.Drawing.Bitmap]::new($Destination)
try { Write-Output "$Destination : $($verified.Width)x$($verified.Height), center alpha=$($verified.GetPixel(416,608).A)" }
finally { $verified.Dispose() }
