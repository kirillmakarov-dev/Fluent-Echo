# Fluent Echo UI Design System

This file is the visual source of truth for the Unity prototype. Scene objects remain the editable source of truth for layout and component styling.

## Direction

Fluent Echo is a calm, premium desktop learning studio. The interface combines a dark private workspace with a warm lesson surface. Mint communicates progress and primary action; coral is reserved for attention, recording, and coaching emphasis.

Avoid decorative gradients, excessive glow, tiny technical labels, and multiple competing primary actions.

The high-fidelity reference is `FluentEcho_UI_Concept.png` in this folder. It is a direction reference, not a flattened UI asset: text and controls remain native Unity components.

## Color Tokens

| Role | Hex | Unity usage |
|---|---|---|
| Workspace | `#07171D` | Canvas backdrop |
| Dark surface | `#0D252C` | Coach card and modal base |
| Raised surface | `#173740` | Secondary buttons and inset areas |
| Lesson surface | `#F8F3E7` | Lesson and result cards |
| Ink | `#102226` | Primary text on light surfaces |
| Muted | `#A9BDC1` | Secondary text on dark surfaces |
| Mint | `#45DFAE` | Primary action, success, active state |
| Mint ink | `#0F7358` | Success text on light surfaces |
| Coral | `#FF8066` | Recording, warning, coaching emphasis |
| Coral ink | `#B84938` | Coaching emphasis on light surfaces |

Contrast target: at least 4.5:1 for body text and 3:1 for large labels.

## Type Scale

Use the project TMP font asset consistently until a dedicated branded SDF font is imported.

| Role | Size |
|---|---|
| Brand | 32 |
| Lesson prompt | 42-44 |
| Modal title | 28-30 |
| Transcript | 24 |
| Body and feedback | 17-18 |
| Button | 17 |
| Metadata | 12-14 |

Use bold weight for headings and actions. Keep body text regular. Italic is reserved for recognized speech or a short coach note.

## Layout

- Reference resolution: `1920 x 1080`.
- Base rhythm: multiples of 8.
- Minimum interactive height: 48 px.
- Main composition: coach rail at 4.5-31%, lesson workspace at 33.5-96.5%.
- Keep one primary action per state: `Start Speaking` during practice and `Next Mission` on completion.
- Settings and Result are top-level Canvas panels. Their position and size are edited directly in the scene.
- Dropdown menus and modal panels must render above the lesson workspace.

## Components

### Buttons

- Primary: mint fill, ink text.
- Recording: near-black fill with mint label; coral may indicate active recording.
- Secondary: raised dark surface with warm light text.
- All scene buttons use the reusable 9-sliced `RoundedSurface.png` sprite, tinted by their `Image` component.
- Action icons come from `Assets/_FluentEcho/Demo/Visuals/Icons` and remain separate tinted `Image` children.
- Destructive or dismissive actions are not automatically coral; Close remains visually secondary.
- Hover and pressed feedback uses color tint only and lasts about 180 ms.
- Disabled buttons use visibly reduced opacity and remain non-interactive.

### Lesson Card

- Warm lesson surface with dark ink.
- Navigation is a single top row with lesson position and a live catalog progress bar.
- Prompt is the strongest typographic element.
- Transcript and status are separate zones.
- The status rail contains readiness, active model, and local-privacy information.
- Technical engine details stay secondary to the learning task.

### Settings Panel

- Centered top-level panel, default size `860 x 440`.
- Two equal columns: input device and recognition profile.
- Current value sits above the dropdown.
- Close is secondary and placed in the title row.

### Result Panel

- Centered top-level panel, default size `960 x 680`.
- Order: mission state, score summary, attempt details, coach note, actions.
- `Next Mission` is primary; `Try Again` is secondary.
- Result panel enters with a 220 ms fade and subtle scale transition.

## Scene Ownership

Editable UI lives in:

`Assets/_FluentEcho/Demo/Scenes/FluentEchoPrototype.unity`

The builder provides defaults for a future rebuild but does not automatically sync or reposition existing scene objects. Runtime code binds behavior and may animate scale or opacity; it must not rewrite authored anchors, positions, sizes, or colors.

## Brand Assets

| Asset | Inspector usage |
|---|---|
| `Assets/_FluentEcho/Demo/Visuals/AvaCoachPortrait.png` | `Fluent Echo UI > Teacher Card > Avatar > Image > Source Image` |
| `Assets/_FluentEcho/Demo/Visuals/RoundedSurface.png` | Buttons, lesson/settings/result cards, transcript card, dropdown backgrounds |
| `Assets/_FluentEcho/Demo/Visuals/Icons` | Button, navigation, transcript, model, and privacy icons |
| `design-system/fluent-echo/FluentEcho_UI_Concept.png` | Visual reference only; do not assign it as a full-screen UI image |

To restyle a control, select it in the scene and edit `Image > Color`, `Button > Color Tint`, its `RectTransform`, and the child `Label` TMP component. No runtime script is required for visual changes.

## Pre-Delivery Check

- No overlapping controls at `1920 x 1080` and `1366 x 768`.
- Settings and Result appear above all lesson controls.
- Lesson dropdown options are readable and clickable.
- Every button has a visible normal, hover, pressed, and disabled state.
- No body text below 16 px.
- Color is not the only success or error signal; status text remains explicit.
- Keyboard navigation follows the visual order.
