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

public partial class FarmRoomSchema : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public FarmRoomSchema() { }
	[Type(0, "map", typeof(MapSchema<PlayerSchema>))]
	public MapSchema<PlayerSchema> players = null;
}

