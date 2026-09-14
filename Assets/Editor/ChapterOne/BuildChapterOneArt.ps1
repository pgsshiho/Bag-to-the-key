param(
    [Parameter(Mandatory = $true)][string]$TrackSource,
    [Parameter(Mandatory = $true)][string]$HoleSource,
    [Parameter(Mandatory = $true)][string]$ParentSource,
    [Parameter(Mandatory = $true)][string]$DoorSource
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$outputDirectory = Join-Path $PSScriptRoot '..\..\Sprites\ChapterOneFinal'
$outputDirectory = [System.IO.Path]::GetFullPath($outputDirectory)

function New-Canvas([int]$width, [int]$height) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $bitmap.SetResolution(96, 96)
    return $bitmap
}

function New-NearestGraphics([System.Drawing.Bitmap]$bitmap) {
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighSpeed
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    return $graphics
}

function Save-Png([System.Drawing.Bitmap]$bitmap, [string]$name) {
    $path = Join-Path $outputDirectory $name
    $temporaryPath = "$path.rebuild.tmp"
    $bitmap.Save($temporaryPath, [System.Drawing.Imaging.ImageFormat]::Png)
    Copy-Item -LiteralPath $temporaryPath -Destination $path -Force
    Remove-Item -LiteralPath $temporaryPath -Force
}

function Get-AlphaBounds([System.Drawing.Bitmap]$bitmap, [System.Drawing.Rectangle]$region) {
    $minX = $region.Right
    $minY = $region.Bottom
    $maxX = $region.Left - 1
    $maxY = $region.Top - 1
    for ($y = $region.Top; $y -lt $region.Bottom; $y++) {
        for ($x = $region.Left; $x -lt $region.Right; $x++) {
            if ($bitmap.GetPixel($x, $y).A -gt 8) {
                if ($x -lt $minX) { $minX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }
    if ($maxX -lt $minX) { throw 'Source region has no visible pixels.' }
    return [System.Drawing.Rectangle]::FromLTRB($minX, $minY, $maxX + 1, $maxY + 1)
}

function Draw-Fitted(
    [System.Drawing.Graphics]$graphics,
    [System.Drawing.Bitmap]$source,
    [System.Drawing.Rectangle]$sourceRect,
    [System.Drawing.Rectangle]$targetRect
) {
    $scale = [Math]::Min($targetRect.Width / $sourceRect.Width, $targetRect.Height / $sourceRect.Height)
    $width = [Math]::Max(1, [int][Math]::Round($sourceRect.Width * $scale))
    $height = [Math]::Max(1, [int][Math]::Round($sourceRect.Height * $scale))
    $x = $targetRect.X + [int][Math]::Floor(($targetRect.Width - $width) / 2)
    $y = $targetRect.Y + [int][Math]::Floor(($targetRect.Height - $height) / 2)
    $destination = [System.Drawing.Rectangle]::new($x, $y, $width, $height)
    $graphics.DrawImage($source, $destination, $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
}

# One approved straight segment is the sole source for A/B/C and the assembled 3x1 track.
$track = [System.Drawing.Bitmap]::FromFile($TrackSource)
try {
    $bounds = Get-AlphaBounds $track ([System.Drawing.Rectangle]::new(0, 0, $track.Width, $track.Height))
    $tile = New-Canvas 256 256
    $graphics = New-NearestGraphics $tile
    try {
        $height = [Math]::Max(1, [int][Math]::Round($bounds.Height * 256.0 / $bounds.Width))
        $destination = [System.Drawing.Rectangle]::new(0, [int][Math]::Floor((256 - $height) / 2), 256, $height)
        $graphics.DrawImage($track, $destination, $bounds, [System.Drawing.GraphicsUnit]::Pixel)
    }
    finally { $graphics.Dispose() }

    foreach ($name in 'PathPieceA.png', 'PathPieceB.png', 'PathPieceC.png') { Save-Png $tile $name }
    $completed = New-Canvas 768 256
    $graphics = New-NearestGraphics $completed
    try {
        for ($index = 0; $index -lt 3; $index++) {
            $graphics.DrawImageUnscaled($tile, $index * 256, 0)
        }
    }
    finally { $graphics.Dispose() }
    Save-Png $completed 'CompletedTrack.png'
    $completed.Dispose()
    $tile.Dispose()
}
finally { $track.Dispose() }

# The approved naturally broken hole already uses a square composition.
$hole = [System.Drawing.Bitmap]::FromFile($HoleSource)
try {
    $output = New-Canvas 256 256
    $graphics = New-NearestGraphics $output
    try { $graphics.DrawImage($hole, [System.Drawing.Rectangle]::new(0, 0, 256, 256)) }
    finally { $graphics.Dispose() }
    Save-Png $output 'CrawlHole.png'
    $output.Dispose()
}
finally { $hole.Dispose() }

# Antigravity returned the four approved poses in a 2x2 grid; normalize them to a 4x1 sheet.
$parent = [System.Drawing.Bitmap]::FromFile($ParentSource)
try {
    $sheet = New-Canvas 1024 256
    $graphics = New-NearestGraphics $sheet
    try {
        $cellWidth = [int]($parent.Width / 2)
        $cellHeight = [int]($parent.Height / 2)
        for ($index = 0; $index -lt 4; $index++) {
            $column = $index % 2
            $row = [int][Math]::Floor($index / 2)
            $sourceRect = [System.Drawing.Rectangle]::new($column * $cellWidth, $row * $cellHeight, $cellWidth, $cellHeight)
            $targetRect = [System.Drawing.Rectangle]::new($index * 256 + 6, 6, 244, 244)
            Draw-Fitted $graphics $parent $sourceRect $targetRect
        }
    }
    finally { $graphics.Dispose() }
    Save-Png $sheet 'Parent.png'
    $sheet.Dispose()
}
finally { $parent.Dispose() }

# Split the approved matching doorway/frame artwork into independently renderable parts.
$door = [System.Drawing.Bitmap]::FromFile($DoorSource)
try {
    $halfWidth = [int]($door.Width / 2)
    $parts = @(
        @{ Name = 'ExitDoorFrame.png'; Region = [System.Drawing.Rectangle]::new(0, 0, $halfWidth, $door.Height) },
        @{ Name = 'ExitDoor.png'; Region = [System.Drawing.Rectangle]::new($halfWidth, 0, $door.Width - $halfWidth, $door.Height) }
    )
    foreach ($part in $parts) {
        $bounds = Get-AlphaBounds $door $part.Region
        $output = New-Canvas 256 512
        $graphics = New-NearestGraphics $output
        try { Draw-Fitted $graphics $door $bounds ([System.Drawing.Rectangle]::new(8, 8, 240, 496)) }
        finally { $graphics.Dispose() }
        Save-Png $output $part.Name
        $output.Dispose()
    }
}
finally { $door.Dispose() }

# Exact silhouettes are derived from the existing in-game dolls, not reinterpreted by a model.
foreach ($socket in @(
    @{ Input = Join-Path $outputDirectory '..\RoomSprite\FirstRoom\곰 옆.png'; Output = 'BearSocket.png' },
    @{ Input = Join-Path $outputDirectory '..\RoomSprite\FirstRoom\토끼 옆.png'; Output = 'RabbitSocket.png' }
)) {
    $source = [System.Drawing.Bitmap]::FromFile([System.IO.Path]::GetFullPath($socket.Input))
    try {
        $bounds = Get-AlphaBounds $source ([System.Drawing.Rectangle]::new(0, 0, $source.Width, $source.Height))
        $mask = New-Canvas $bounds.Width $bounds.Height
        for ($y = 0; $y -lt $bounds.Height; $y++) {
            for ($x = 0; $x -lt $bounds.Width; $x++) {
                $alpha = $source.GetPixel($bounds.X + $x, $bounds.Y + $y).A
                if ($alpha -gt 8) { $mask.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, 67, 42, 48)) }
            }
        }
        $output = New-Canvas 256 256
        $graphics = New-NearestGraphics $output
        try { Draw-Fitted $graphics $mask ([System.Drawing.Rectangle]::new(0, 0, $mask.Width, $mask.Height)) ([System.Drawing.Rectangle]::new(18, 18, 220, 220)) }
        finally { $graphics.Dispose() }
        Save-Png $output $socket.Output
        $output.Dispose()
        $mask.Dispose()
    }
    finally { $source.Dispose() }
}

# Preserve the existing open drawer art, square it, and derive a clear closed state for before completion.
$drawerPath = Join-Path $outputDirectory 'SecretDrawer.png'
$drawerFile = [System.Drawing.Bitmap]::FromFile($drawerPath)
try { $drawer = [System.Drawing.Bitmap]$drawerFile.Clone() }
finally { $drawerFile.Dispose() }
try {
    $bounds = Get-AlphaBounds $drawer ([System.Drawing.Rectangle]::new(0, 0, $drawer.Width, $drawer.Height))
    $open = New-Canvas 256 256
    $graphics = New-NearestGraphics $open
    try { Draw-Fitted $graphics $drawer $bounds ([System.Drawing.Rectangle]::new(16, 16, 224, 224)) }
    finally { $graphics.Dispose() }
    Save-Png $open 'SecretDrawerOpen.png'

    $closed = [System.Drawing.Bitmap]$open.Clone()
    $graphics = [System.Drawing.Graphics]::FromImage($closed)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
        $dark = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 54, 30, 30))
        $wood = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 145, 75, 58))
        $light = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 202, 125, 84))
        $brass = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 225, 168, 89))
        $graphics.FillRectangle($dark, 35, 65, 186, 116)
        $graphics.FillRectangle($wood, 41, 72, 174, 101)
        $graphics.FillRectangle($light, 47, 78, 162, 7)
        $graphics.FillRectangle($dark, 113, 116, 30, 25)
        $graphics.FillRectangle($brass, 120, 121, 16, 13)
        $dark.Dispose(); $wood.Dispose(); $light.Dispose(); $brass.Dispose()
    }
    finally { $graphics.Dispose() }
    Save-Png $closed 'SecretDrawerClosed.png'
    $closed.Dispose()
    $open.Dispose()
}
finally { $drawer.Dispose() }

Write-Output "Chapter One art rebuilt in $outputDirectory"
