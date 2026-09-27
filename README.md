# Onpiece 2D

Unity 2D Action RPG project infrastructure.

## Unity Version

Unity 6.3 LTS (`6000.3.9f1`). The authoritative version is recorded in `ProjectSettings/ProjectVersion.txt`.

## Project Structure

Game-owned assets live under:

```text
Assets/_Game/
```

Top-level structure:

```text
Assets/
  _Game/
    Art/
    Animations/
    Audio/
    Fonts/
    Materials/
    Prefabs/
    Scenes/
    Scripts/
    ScriptableObjects/
    Settings/
    Resources/
  Plugins/
  ThirdParty/
```

Use `Assets/_Game` for project-owned content only. Keep external or vendor-provided assets outside `_Game`, preferably in `Assets/Plugins` or `Assets/ThirdParty`.

## Naming Conventions

- Folders: PascalCase
- C# files: PascalCase
- Scenes: PascalCase
- Prefabs: PascalCase
- Materials: PascalCase
- Animation clips: PascalCase
- Sprites: `Character_Action_Direction_Frame`

Sprite examples:

```text
Zoro_Idle_Down_01
Zoro_Run_Right_01
Zoro_Run_Right_02
Zoro_Attack_Right_01
```

Apply these conventions to new assets only. Do not rename existing assets just to match the convention.

## Git And Meta Files

Commit these Unity project folders:

```text
Assets/
Packages/
ProjectSettings/
```

Do not commit generated Unity folders such as:

```text
Library/
Temp/
Obj/
Logs/
UserSettings/
MemoryCaptures/
```

Unity `.meta` files must be committed together with their matching assets and folders. Do not delete `.meta` files manually.

## Opening The Project

Open this folder with Unity Hub or the Unity Editor. If Unity generates missing project files, review them before committing.
