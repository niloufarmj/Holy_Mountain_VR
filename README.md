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

- **Playable build (zip):** [Google Drive](https://drive.google.com/file/d/1cT01Ly9-xfOuwSYaXGYq_J7_j-M96Xmh/view?usp=sharing)

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

## 👤 Author

**Niloufar Moradijam** — Master’s Semester Project (Interactive Media)  
Hagenberg, September 2025

---

## 🧾 License

TBD (add a LICENSE file—MIT recommended unless assets impose restrictions).
