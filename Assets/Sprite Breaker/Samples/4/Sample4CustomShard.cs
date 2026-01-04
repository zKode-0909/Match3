using UnityEngine;

// example script for custom shard
// added to custom shard prefab, and event InitShard is added to SpriteShatter instance
public class Sample4CustomShard : MonoBehaviour {

    // holds on to the underlying polygon. Assigned in InitShard event
    private SpriteBreakerPolygon _polygon;

    // assigned in prefab
    public TrailRenderer trail;
    
    // Added as event to SpriteShatter component
    // since event's "this" target is the prefab (not an instance of it),
    // we look up instance of Sample4CustomShard on the passed GameObject 
    public void InitShard ( GameObject shard, SpriteBreakerPolygon poly, SpriteBreaker spriteBreaker ) {
        
        // remember polygon and spriteShatter in script instance
        Sample4CustomShard customShardInstance = shard.GetComponent<Sample4CustomShard>();
        customShardInstance._polygon = poly;
        
        // scale trail size with polygon size
        customShardInstance.trail.startWidth = 10 * poly.Area();
        
        // disable trail if special polygon (floatValue == 1)
        // this value is set in the SpriteBreakerEditor, polygon's properties
        customShardInstance.trail.enabled = (poly.floatValue < 1);
    }

    
    private void Update() {

        // if poly is special (its floatValue is set in the Sprite Shatter Editor), make it pulse
        if ( _polygon.floatValue > 0 ) {
            float pulse = 0.6f + 0.2f * Mathf.Sin( Time.time * 4 );
            _polygon.shardColor = Color.Lerp( Color.white, Color.red, pulse );
            transform.localScale = Vector3.one * pulse;
            
        // regular shard
        } else {

            // make it wiggle
            float wiggle = 2 * Mathf.Cos( Time.time * 2 );
            _polygon.fakePhysicsRotationalVelocity.z = wiggle;
            _polygon.fakePhysicsVelocity = Quaternion.Euler( 0, 0, wiggle ) * _polygon.fakePhysicsVelocity;
            
            // make trail fade with the shard
            trail.startColor = _polygon.currentColor;
        }
        
    }
}
