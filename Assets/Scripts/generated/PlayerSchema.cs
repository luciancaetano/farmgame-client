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

	[Type(3, "number")]
	public float vx = default(float);

	[Type(4, "number")]
	public float vy = default(float);

	[Type(5, "number")]
	public float lastSeq = default(float);

	[Type(6, "number")]
	public float moveSpeed = default(float);
}

