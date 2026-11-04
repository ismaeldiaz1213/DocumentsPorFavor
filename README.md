# Documents, Por Favor!

A border-inspection game made in Unity for Fall 2026 Game Design.
Credits and sources: [CREDITS.md](CREDITS.md).

## Opening the project on any computer (Windows, Mac, Linux)

1. Install **Unity 6000.3.25f1** through Unity Hub (the exact version matters).
   Add the **WebGL Build Support** module if you want to build for the web.
2. Get the code:
   ```
   git clone <this repo's URL>
   ```
   or, if you already have it: `git pull`
3. In Unity Hub, click **Add > Add project from disk** and pick this folder.
   The first open takes a few minutes while Unity rebuilds its `Library` folder.
4. The game scene, `Assets/Scenes/Checkpoint.unity`, opens by itself. If you ever see an empty
   "Untitled" scene, double-click that file in the Project window.
5. Press **Play**. For a bigger view, set the Game view's play mode to **Play Maximized**.

`Library/`, `Temp/`, `Logs/`, `UserSettings/` and `Builds/` are left out of git on purpose.
Unity rebuilds them on each computer. Line endings are kept the same on every system by `.gitattributes`.

## Building for the web

In Unity: **Documents Por Favor > Build for Web**. The first time, Unity switches the project
to WebGL, which can take several minutes. The finished game is in `Builds/Web/`:

```
Builds/Web/
  index.html
  Build/
  TemplateData/
```

Upload the **whole folder** (not just `index.html`) to your website and open `index.html`.
It runs on plain static hosting (GitHub Pages, Vercel, Netlify, itch.io as an HTML game)
with no special server settings. To put it inside another page:

```html
<iframe src="/documents-por-favor/index.html" width="960" height="540" allowfullscreen></iframe>
```

Building from the command line works too (with Unity closed):

```
Unity -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod WebBuilder.BuildNow
```

## Controls

| Key | Action |
|---|---|
| N | Call the next car |
| Space / Enter | Next line of dialogue, continue |
| A / R / I | Approve / Reject / Imprison |
| T | Take a bribe |
| Tab | Rulebook (arrow keys turn pages, Esc closes) |
