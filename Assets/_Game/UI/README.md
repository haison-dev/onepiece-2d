# UI module

All player-facing UI assets and runtime code live under this folder.

## Structure

```text
UI/
|-- Art/
|   |-- HUD/        Character frames, bars, portraits, and HUD decoration
|   `-- Skills/     Skill and combat button artwork
`-- Runtime/
    |-- HUD/        Character status HUD behaviour
    `-- Skills/     Skill layout and input UI behaviour
```

Create `Prefabs/HUD` or `Prefabs/Skills` when the first reusable UI prefab is
added. Create `Editor` only for custom inspectors or editor tooling.

## Naming

- HUD artwork: `<Feature>_<Role>.png`, for example `PlayerHUD_Frame.png`.
- Skill icons: `Skill_01_Icon.png`, `Skill_02_Icon.png`, and so on.
- Skill slot frames: `Skill_01_Frame.png`, `Skill_02_Frame.png`, and so on.
- Special actions use descriptive names such as `BasicAttack.png`.
- Runtime components use PascalCase and end in `HUD` when they own a screen
  overlay.

## Import settings

UI textures should normally use:

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Alpha Is Transparency: enabled
- Generate Mip Maps: disabled

## Layout rules

- Reference resolution: 1920 x 1080.
- Canvas scaling: Scale With Screen Size, Match = 0.5.
- Anchor HUD elements to their nearest screen corner.
- Respect `Screen.safeArea` for mobile devices.
- Keep artwork separate from runtime scripts so skins can change without
  changing behaviour.
- Keep skill icons separate from numbered slot frames. Assign or replace the
  icon through `PlayerSkillHUD.SetSkillIcon`; the frame remains owned by the slot.

Always move an asset together with its `.meta` file (or move it inside the
Unity Project window) so scene and prefab references keep the same GUID.
