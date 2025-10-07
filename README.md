# 🌿 P2 Garden of Zen

This is a Unity project for the P2 assignment.  
We use **GitHub + Git LFS** for version control.

---

## 🧰 Requirements

- Unity Editor **2022.3.x (LTS)**  
- Git  
- Git LFS (`git lfs install` after cloning)  
- macOS / Windows / Linux supported

---

## 📥 Cloning the Project

Follow these steps to set up the project locally:

```bash
# 1. Clone the repository
git clone https://github.com/USERNAME/p2goz.git

# 2. Enter the project folder
cd p2goz

# 3. Install Git LFS (first time only)
git lfs install

# 4. Pull large assets
git lfs pull
```

> ⚠️ The first time you open the project, Unity will regenerate the `Library/` folder. This may take a few minutes.

---

## 🌿 Project Structure

The repository tracks only the essential folders:

- `Assets/` – Scenes, scripts, prefabs, textures, etc.  
- `ProjectSettings/` – Unity project and build settings  
- `Packages/` – Dependency info (`manifest.json`)

Ignored folders:

- `Library/`, `Temp/`, `Logs/`, `Build/` → local-only, not pushed to Git

---

## 👥 Collaboration Guidelines

- **Always pull** before you start working:
  ```bash
  git pull
  ```
- Create a **feature branch** for your work:
  ```bash
  git checkout -b feature/yourname/short-description
  ```
- Commit regularly with clear messages.
- Push your branch and create a Pull Request (PR) on GitHub.
- Avoid working on the same `.unity` scene file simultaneously.  
  → Prefer working with **prefabs** or **additive sub-scenes** to prevent merge conflicts.

---

## 🧠 Common Git Commands

```bash
# Stage all changes
git add .

# Commit with a clear message
git commit -m "feat: added meditation area"

# Push your branch
git push -u origin feature/yourname/short-description

# Pull latest changes
git pull
```

---

## ⚠️ Important Notes

- Do **not** commit `Library/` or build folders.  
- Large assets (e.g., `.png`, `.fbx`, `.wav`) are tracked with **Git LFS**.  
  Make sure everyone has run `git lfs install` at least once.  

---

## 📝 Credits

Team Members:
- Bailey Watkins
- Cynthia Liu
- Jimin Kwak
- Junsang Park

---

## 🪄 Tips

- If you see line-ending warnings, it's safe to ignore them.  
- When pulling new branches, always run `git lfs pull` if large assets are missing. 
