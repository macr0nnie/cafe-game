# Tests

EditMode tests live in `Assets/Tests/EditMode` (assembly `Cafe.Tests.EditMode`).
Game code compiles into `Cafe` (`Assets/Scripts`) and editor tools into `Cafe.Editor` (`Assets/Editor`),
so tests can reference game systems directly and test them in isolation.

| Test | Guards |
|---|---|
| `SceneBuilderTests` | Cafe > 1. Build Cafe Scene runs with no errors and saves the scene; generated materials stay on the locked palette (batch mode only, restores the files it rewrites) |
| `ProjectRulesTests` | Tonemapping stays Neutral; each MonoBehaviour has its own file named after the class |
| `CafeMeshTests` | Example plain unit test of runtime code |
| `DrinkGuideTests` | Drink guide step order (cup, ingredients in a technique-safe order, serve), which order a cup belongs to, where a cup can be set down |

## Running

- In the editor: Window > General > Test Runner > EditMode > Run All. (The scene builder tests are skipped there.)
- From a terminal with the project closed: `powershell -ExecutionPolicy Bypass -File tools/run-tests.ps1`
- Automatically before each push: run `git config core.hooksPath .githooks` once. If Unity has the project open the hook skips with a warning.
- On GitHub: `.github/workflows/tests.yml` runs on every pull request and push to main once the Unity licence secrets are set.

## CI secrets (Settings > Secrets and variables > Actions)

- `UNITY_LICENSE`: full contents of `C:\ProgramData\Unity\Unity_lic.ulf` from a machine where Unity Hub is signed in.
- `UNITY_EMAIL`, `UNITY_PASSWORD`: the Unity account that licence belongs to.

## Adding tests

Put pure logic (scoring, recipes, events) in plain C# classes or static methods and test them with `[Test]`.
Add a `Tests/PlayMode` assembly when something needs a running scene.
