# lasteroids
Asteroid game using Unity ECS (Entities 0.51.1-preview.21)

## Game Loop (in build)
- The game spawns 70000 asteroids with an initial velocity
- Asteroid will be destroyed when they go out of bounds or when colliding with bullets
- The user spawns a player by interaction [SPACE] after the game starts
- The player moves with thrusters [WASD]
- The player moves with mouse, hold [RMB]

## DOTS
The key systems revolve around the asteroids, bullets and the player, but also the game settings.
The program uses a sub scene that hold game objects and this is where the size of the level, number of asteroids etc. can be set in a game settings system.
### Asteroids:
	- Spawn system that spawns asteroids within in the level
	- System for checking if asteroids are out of bounds
	-  Destruction system that removes entities (only asteroids) when they get tagged for destruction

### Bullets:
	- The bullet spawn is a game object (child to the player) and will spawn a bullet when shoot is registered for the player entity
	- The bullets have an aging system that simply removes an entity if it has "lived too long"
	- The bullets have a limited spawn rate as well


	

