Overview:
SideScrollGame is a 2D side-scrolling action game developed in Unity. The project focuses on implementing responsive combat mechanics, enemy AI behavior, boss phases, and polished animation systems.

The goal of this project was to deepen understanding of:
- Object-oriented design in C#
- State-based enemy behavior systems
- Player combat systems (combo attacks, charge attacks, parry windows)
- Unity physics and animation control
- Scalable game architecture

Core Features:
Player Mechanics:
- Combo attack system (Attack1 → Attack2 → Attack3)
- Charge attack with hold-release logic
- Sliding with cancel behavior
- Wall slide and wall jump mechanics
- Parry system with stun logic
- Downward aerial attack with impact effects
- Responsive movement with minimal cooldown delay

Enemy Systems:
- Regular enemy AI with state transitions
- Boss enemy with multiple phases
- Health-based phase changes
- Cooldown-managed attack cycles
- Projectile-based attack patterns
- Stun and parry reaction logic

Combat & Effects:
- Knockback system
- Raycast-based ground and wall detection
- Particle effects for impact feedback
- Animation-triggered damage windows

Technologies Used
-Unity (2D)
-C#
-Unity Animator Controller
-Unity Physics2D

Architecture Highlights
- Modular behavior scripts for enemies and bosses
- Coroutine-based cooldown management
- Raycast-based ground and wall detection
- Animation-synced attack windows
- Separation of movement, combat, and state logic

Controls:
Move: A/D
Jump: Space
Slide: shift
dash: Space + Shift
Attack: left Mouse
Charged Attack: Hold Left Mouse
Parry: Right Mouse
Wall CLimb: Hold A or D

Contributor:
Sean Pashaev
Email: seanpash19@gmail.com
