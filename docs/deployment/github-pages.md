# GitHub Pages WebGL deployment

This project can publish the playable WebGL build through GitHub Pages using `.github/workflows/webgl-pages.yml`.

## Required repository setup

1. In GitHub, open **Settings > Pages** and set **Build and deployment > Source** to **GitHub Actions**.
2. Add Unity license secrets under **Settings > Secrets and variables > Actions**.
3. For a Unity Personal license, set `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD`.
4. For a Unity Professional license, set `UNITY_SERIAL`, `UNITY_EMAIL`, and `UNITY_PASSWORD`.

The workflow runs EditMode tests, builds WebGL with Unity `6000.3.0f1`, uploads `Builds/WebGL`, and deploys it to Pages.

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
