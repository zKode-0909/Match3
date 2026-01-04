This example demonstrates using a custom shard prototype (prefab), and OnShardCreated event.

The shard prototype prefab (Sample 4 Custom Shard Prefab) has custom script (Sample4CustomShard) attached. 
Sample4CustomShard has InitShard method, added to SpriteBreaker OnShardCreated, which initializes each shard when 
Break() is called. In this example, it stores a reference to SpriteBreakerPolygon object, which SpriteBreaker associates
with each created shard, and keeps its fake physics state. 

Sample4CustomShard also has Update() method which this example uses to apply custom motion to shards after they're 
spawned.