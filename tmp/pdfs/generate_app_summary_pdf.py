from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas
from reportlab.lib.utils import simpleSplit

OUT = r"output/pdf/app_repo_summary_one_page.pdf"

c = canvas.Canvas(OUT, pagesize=letter)
W, H = letter

left = 46
right = W - 46
y = H - 42


def draw_heading(text):
    global y
    c.setFont("Helvetica-Bold", 11)
    c.drawString(left, y, text)
    y -= 15


def draw_paragraph(text, font="Helvetica", size=9, leading=11):
    global y
    c.setFont(font, size)
    for line in simpleSplit(text, font, size, right - left):
        c.drawString(left, y, line)
        y -= leading


def draw_bullet(text, size=9, leading=11):
    global y
    c.setFont("Helvetica", size)
    wrapped = simpleSplit(text, "Helvetica", size, right - left - 12)
    if not wrapped:
        return
    c.drawString(left, y, "- " + wrapped[0])
    y -= leading
    for line in wrapped[1:]:
        c.drawString(left + 12, y, line)
        y -= leading

c.setFont("Helvetica-Bold", 14)
c.drawString(left, y, "App Summary - Abyss Hunter: Xenocide (Repo Evidence)")
y -= 18
c.setFont("Helvetica", 8)
c.drawString(left, y, "Evidence sources: Assets/ThirdPersonController, Assets/GameDesign docs, scene/script assets")
y -= 16

# What it is
draw_heading("What it is")
draw_paragraph(
    "A Unity third-person action project focused on musou-style combat, level progression, and boss encounters. "
    "Repo evidence shows game systems, scenes, and design docs under Assets for a PC Steam-targeted title.")
y -= 2

# Who it's for
draw_heading("Who it's for")
draw_bullet("Primary persona: PC core action players (GameDesignDocument: target platform and audience notes).")
y -= 2

# What it does
draw_heading("What it does")
features = [
    "Player control stack with movement, camera, climbing, combat, health, and input handling scripts.",
    "Enemy gameplay with AI, health, archetypes, wave members, projectiles, and boss controllers/templates.",
    "Combat services for damage, stamina, hit reactions, combo momentum rewards, and musou-style systems.",
    "Skill system with ScriptableObject-based skills, loadouts, and runtime skill manager/timeline hooks.",
    "Progression systems including quests, experience, levels, talents, pearls, economy, rewards, and inventory.",
    "UI layer for HP/stamina/experience, combo, skills, boss health, quest tracker, wave and level-complete panels.",
    "Steam integration hooks for achievements, stats, and cloud save bridge scripts.",
]
for f in features:
    draw_bullet(f)
y -= 1

# How it works
draw_heading("How it works (compact architecture from repo)")
arch = [
    "Components/services: Player, Enemy, Combat, Skills, Progression, UI, Core managers, Steam adapters, DOTS prototypes.",
    "Data assets: LevelData, ChapterData, QuestDatabase, EconomyConfig, ProgressionMilestones, skill and pearl assets.",
    "Runtime flow: PlayerInputHandler -> movement/combat/skills -> EnemyHealth/DamageService -> GameEvents publish -> UI updates.",
    "Progression flow: kill/level events -> XP, rewards, drops, quest updates -> SaveManager persistence -> next level/chapter unlock.",
    "Content flow: scenes (MainMenu + Level_01..Level_10) reference level/runtime config and stronghold-wave controllers.",
]
for a in arch:
    draw_bullet(a)
y -= 1

# How to run
draw_heading("How to run (minimal getting started)")
run_steps = [
    "Open this Unity project in Unity 2022.3 LTS or later (documented in ThirdPersonController/README.md).",
    "In Package Manager, ensure Input System and AI Navigation are installed.",
    "Open Assets/Scenes/MainMenu.unity or Assets/Scenes/Level_01_TrenchRift.unity and press Play.",
    "Set required layers used by scripts/scenes (Ground, Enemy, Climbable) if missing.",
    "CLI build/run command: Not found in repo.",
]
for s in run_steps:
    draw_bullet(s)

if y < 30:
    # Safety marker if content overflows unexpectedly.
    c.setFont("Helvetica-Oblique", 8)
    c.drawString(left, 20, "Layout warning: content exceeded page target.")

c.showPage()
c.save()
print(OUT)
