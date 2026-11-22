# Renders the Purgely icon (same design as assets/purgely.svg) with GDI+
# and writes a multi-size assets/purgely.ico (16..256 px, PNG-compressed entries)
# plus assets/purgely-256.png.
# Usage: powershell -ExecutionPolicy Bypass -File tools/make-icon.ps1

Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'
$sizes = 16, 24, 32, 48, 64, 128, 256

function New-RoundRectPath([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function Get-Color([string]$hex, [double]$opacity = 1.0) {
    $c = [System.Drawing.ColorTranslator]::FromHtml($hex)
    return [System.Drawing.Color]::FromArgb([int](255 * $opacity), $c.R, $c.G, $c.B)
}

function Render-Icon([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 256.0
    $g.ScaleTransform($s, $s)

    # background
    $bgPath = New-RoundRectPath 16 16 224 224 52
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (
        (New-Object System.Drawing.PointF 16, 16),
        (New-Object System.Drawing.PointF 240, 240),
        (Get-Color '#7C3AED'), (Get-Color '#2563EB'))
    $g.FillPath($bgBrush, $bgPath)

    # app window
    $white = New-Object System.Drawing.SolidBrush (Get-Color '#FFFFFF')
    $g.FillPath($white, (New-RoundRectPath 52 72 104 112 14))
    $g.SetClip((New-RoundRectPath 52 72 104 112 14))
    $g.FillRectangle((New-Object System.Drawing.SolidBrush (Get-Color '#DDD6FE')), 52, 72, 104, 24)
    $g.ResetClip()
    if ($size -ge 32) {
        $dot = New-Object System.Drawing.SolidBrush (Get-Color '#7C3AED')
        foreach ($cx in 66, 79, 92) { $g.FillEllipse($dot, $cx - 4, 80, 8, 8) }
    }

    # particles: x, y, size, radius, opacity
    $particles = @(
        @(166, 82, 16, 3, 0.9), @(164, 112, 14, 3, 0.85), @(190, 100, 12, 2, 0.7),
        @(166, 142, 12, 2, 0.75), @(192, 130, 10, 2, 0.55), @(166, 168, 10, 2, 0.6),
        @(212, 116, 7, 1.5, 0.4), @(190, 158, 8, 1.5, 0.45), @(214, 146, 6, 1.5, 0.3))
    if ($size -le 24) { $particles = $particles[0..5] }  # fewer, bigger dots at tiny sizes
    foreach ($pt in $particles) {
        $b = New-Object System.Drawing.SolidBrush (Get-Color '#FFFFFF' $pt[4])
        $g.FillPath($b, (New-RoundRectPath $pt[0] $pt[1] $pt[2] $pt[2] $pt[3]))
    }

    $g.Dispose()
    return $bmp
}

$pngs = @()
foreach ($size in $sizes) {
    $bmp = Render-Icon $size
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($size -eq 256) { $bmp.Save((Join-Path $assets 'purgely-256.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
    $pngs += , @($size, $ms.ToArray())
}

# ICO container: ICONDIR + ICONDIRENTRY[] + PNG payloads
$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$pngs.Count)
$offset = 6 + 16 * $pngs.Count
foreach ($p in $pngs) {
    $dim = if ($p[0] -ge 256) { 0 } else { $p[0] }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$p[1].Length); $w.Write([uint32]$offset)
    $offset += $p[1].Length
}
foreach ($p in $pngs) { $w.Write([byte[]]$p[1]) }
$w.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $assets 'purgely.ico'), $out.ToArray())
Write-Output "Wrote purgely.ico ($($sizes -join ', ') px) and purgely-256.png"
