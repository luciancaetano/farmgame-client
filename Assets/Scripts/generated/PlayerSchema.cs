// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.60
// 

using Colyseus.Schema;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

public partial class PlayerSchema : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public PlayerSchema() { }
	[Type(0, "string")]
	public string id = default(string);

	[Type(1, "string")]
	public string name = default(string);

	[Type(2, "ref", typeof(Position2D))]
	public Position2D position = null;

	[Type(3, "int64")]
	public long lastSeq = default(long);

	[Type(4, "float32")]
	public float moveSpeed = default(float);

	[Type(5, "float32")]
	public float fixedDeltaTime = default(float);

	[Type(6, "float32")]
	public float vx = default(float);

	[Type(7, "float32")]
	public float vy = default(float);

	[Type(8, "int64")]
	public long currentTime = default(long);
}

