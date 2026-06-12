# GitHub Pages WebGL deployment

This project publishes the playable WebGL build through GitHub Pages using `.github/workflows/webgl-pages.yml`.

## Required repository setup

1. In GitHub, open **Settings > Pages** and set **Build and deployment > Source** to **GitHub Actions**.
2. Keep the generated WebGL artifact in `docs/webgl`.

The workflow validates `docs/webgl`, uploads it as the Pages artifact, and deploys it. It does not need Unity credentials in GitHub Actions.

## Local verification

Run the same build method locally after installing WebGL Build Support for Unity `6000.3.0f1`:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.0f1\Editor\Unity.exe" `
  -batchmode -quit -nographics `
  -projectPath (Resolve-Path ".").Path `
  -executeMethod Gwent.Editor.GwentWebGlPagesBuilder.Build `
  -logFile Logs\Unity-WebGLBuild.log
```

Serve the generated folder with any static server, then open the local URL in a browser:

```powershell
python -m http.server 8080 --directory Builds\WebGL
```

After verifying the local build, refresh the committed Pages artifact:

```powershell
Remove-Item -LiteralPath docs\webgl -Recurse -Force
Copy-Item -LiteralPath Builds\WebGL -Destination docs\webgl -Recurse
```
