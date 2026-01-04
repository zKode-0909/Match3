In this example, each time the sprite breaks, it shatters in a new way. 
The BREAK button calls GenerateAndBreak() on SpriteBreaker instance, which uses Generate rollout parameters to generate
new data before calling Break().

Play scene, then press BREAK button to watch the sprite break. Pieces will fly away from sprite center and fall using
fake physics (without collisions) before fading away.