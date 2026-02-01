# D2CPhotoStation (WinForms .NET Framework 4.8)

This is a complete Visual Studio solution for a photo capture workstation using:
- AForge.Video
- AForge.Video.DirectShow

## Features
- Auto-start webcam stream when the form opens
- Live preview in `picLive`
- `Take Picture` fills Front then Back; prompts overwrite when both exist
- `Save` prompts for folder name and saves:
  - Front.jpg
  - Back.jpg
  into: `<BaseSavePath>\<FolderName>\`
- After saving: clears Front/Back for next device
- Reset Front / Reset Back buttons
- Settings button (password protected: ctdi / Ctdi123@)
  - select a different camera
  - select destination base folder
- Exit button
- Safe UI updates via BeginInvoke
- Maintains latest frame buffer for snapshot capture
- Proper bitmap disposal to prevent memory leaks
- Safe camera stop on form close

## NuGet restore
Open the solution in Visual Studio and:
- Right click solution -> Restore NuGet Packages
(or enable "NuGet Package Restore" and build once)

Packages used (2.2.5):
- AForge.Video
- AForge.Video.DirectShow

## Default save path
`AppState.BaseSavePath` defaults to:
`C:\Users\sumit\OneDrive\Desktop\D2C\`
Change it from Settings inside the app.
