# The Holy Mountain — VR Nature Sandbox

**Collect → Select → Plant → Protect → Reseed.**  
Explore a stylized Alpine terrain, gather seeds from fallen trees, choose a species, plant saplings, and defend them from hungry animals. Trees age and die, dissolving into new seeds to keep the ecological loop alive. Throw stones to deter animals and listen for audio cues that mark each stage of the cycle. 

---

## ✨ Core Features
- **Ecological loop:** tree death → seed spawn → player planting → growth → animal interaction.  
- **Multi-species planting:** in-world Seed Selection Panel with live counts; planting consumes inventory.  
- **Animal–sapling interactions:** animals detect, approach, and eat young trees; growth pauses during attacks and resumes when safe.  
- **Stones & deterrence:** pick up, aim, and throw stones to interrupt eating.  
- **VR-first feedback:** one-shot/loop SFX for seed, plant, growth, eating, fall, and dissolve events.  
- **Terrain & lighting:** Gaia-driven Alpine environment with day–night transitions.

---

## 🎮 Controls (current build)

| Control | Action |
| --- | --- |
| **Left Stick** | Move (continuous) |
| **Right Stick** | Turn (snap or continuous) |
| **Right Trigger (hold)** | Teleport aim (arc/reticle) |
| **Right Trigger (release)** | Teleport confirm |
| **Grip (L/R)** | Grab / Select |
| **Trigger (L/R)** | Use / Activate (context) |
| **A (R) / X (L)** | Toggle Seed Selection Panel / Collect seed / Pick up or release stone (nearby) |
| **B (R) / Y (L)** | Back / Close panel |
| **Left Trigger (hold → release)** | Stone throw aim → throw |

> Bindings are action-based (OpenXR) and can be remapped per device in the input asset.

---

## 🛠 Tech Stack
- **Engine:** Unity 6.1 (URP)  
- **World & Lighting:** Gaia Pro (biomes, time-of-day)  
- **XR:** Ocules Integration (action-based controllers; hand meshes as visuals)  
- **Language:** C#  
- **VC:** Git (GitHub)

---

## 🚀 Getting Started

1. **Clone** the repo:
   ```bash
   git clone https://github.com/niloufarmj/Holy_Mountain_VR.git
   Open in Unity 2022.3 LTS (URP).
2. Open in **Unity 6000.1.0b12 (URP)**.
3. Ensure XR packages are installed (Oculus Integration).

4. Connect your headset (Quest via Link / PCVR) and Play in Editor, or switch Build Target and make a device build as usual.

Tip: Keep reflection and post-processing settings conservative on low-end GPUs for stable VR framerate.

---

## 📦 Downloads

- **Playable build (zip):** [Google Drive](https://drive.google.com/file/d/1cT01Ly9-xfOuwSYaXGYq_J7_j-M96Xmh/view?usp=drive_link)
- **Animals Pack:** [Google Drive](https://drive.google.com/drive/folders/15vJRhhoRFxcZWIs3Og38SsLiaffdhrts?usp=sharing)

---

## 🧭 Gameplay Loop (at a glance)

1. **Collect** seeds near recently dissolved/dead trees.  
2. **Select** a species on the Seed Panel.  
3. **Plant** at a suitable ground spot.  
4. **Protect** saplings from animals (stone throw / proximity).  
5. **Reseed:** mature trees eventually die and drop seeds—repeat.

---

## 🗺 Systems Overview

- **Tree Lifecycle:** periodic death near the player + seed instantiation; growth has sapling → mid → mature phases.  
- **Inventory & Planting:** simple counts per species; consumption on plant; world-space Seed Panel.  
- **Animals:** NavMesh wander, sapling detection, eating state with growth pause/resume.  
- **Interaction:** controller-driven input with hand-mesh visuals; smooth + teleport locomotion.  
- **Audio:** event-driven one-shots/loops for all major beats.  
- **Environment:** Gaia terrain, lighting phases, volume/cubemap transitions.

---

## ⚠️ Known Issues

- **Planar water reflections:** custom implementation currently unstable; a simpler water fallback is used.  
- **UI mirroring:** occasional mirror issue depending on panel follow/orientation.  
- **Hands:** controller-driven hand meshes are visual-only and not anatomically accurate.  
- **NavMesh on water:** if baked onto water, animals may ignore it—use area costs/filters.

---

## 🧭 Roadmap (next steps)

- **Gestures:** XR Hands for pinch/open-palm (e.g., “scare/ward”).  
- **Stone throw polish:** arc aim tuning, release feel, collision FX/haptics.  
- **Weather modifiers:** rain/wind affecting growth and AI.  
- **AI depth:** packs, hunger/energy cycles, better line-of-sight.  
- **UI ergonomics:** wrist-anchored variant, improved reticles, accessibility presets.

---

## 📚 Project Report & References

- **Term Report PDF:** see repository docs (includes system diagram, timeline, and reflections).  
- Inspirations: *Tree* (VR), *Wander*, *Cloud Garden*; research by Slater/Isbister.

---

## 📷 Screenshots

<img width="1596" height="583" alt="UI-SeedSelection" src="https://github.com/user-attachments/assets/ccb31e90-de76-42f5-aaa4-d05f2c63fd0e" />

<img width="268" height="457" alt="Tree Growth Phases (1)" src="https://github.com/user-attachments/assets/b9889050-8ac1-44b4-8383-c46fb7d06716" />
<img width="266" height="438" alt="Tree Growth Phases (2)" src="https://github.com/user-attachments/assets/690a1707-6c7a-4a5c-9253-abe77d670540" />
<img width="245" height="433" alt="Tree Growth Phases (3)" src="https://github.com/user-attachments/assets/2b91ee68-a308-4f7f-8ba0-3f58c1fc25af" />

<img width="1291" height="722" alt="Night Environment" src="https://github.com/user-attachments/assets/fd379978-b9fd-47ae-a366-4d0443dcc054" />
<img width="1074" height="600" alt="Sunset Environment" src="https://github.com/user-attachments/assets/6a2ed14f-53ac-4268-b403-800b5036d6fa" />
<img width="1296" height="719" alt="Environment" src="https://github.com/user-attachments/assets/b623d52e-bf54-4e54-b902-6eaf7dd7a344" />
<img width="1307" height="725" alt="Environment (2)" src="https://github.com/user-attachments/assets/d9b993d9-8e1c-4bbd-a189-575194ac0f3d" />
<img width="1288" height="709" alt="Animal" src="https://github.com/user-attachments/assets/21aed6f1-d88b-470f-80bd-f2b62f05325d" />

<img width="1296" height="721" alt="Highlited Stone" src="https://github.com/user-attachments/assets/d2bad99e-4f1c-4992-a587-03c34d222f9b" />
<img width="1295" height="713" alt="Stone In Hand Aim" src="https://github.com/user-attachments/assets/73a7b7e7-99e6-4afb-9184-45b5b1502225" />
<img width="1130" height="715" alt="Aim Animal" src="https://github.com/user-attachments/assets/3b25a0be-a8bf-47e6-a53c-fe2009e338e0" />



---

## 👤 Author

**Niloufar Moradijam** — Master’s Semester Project (Interactive Media)  
Hagenberg, September 2025

