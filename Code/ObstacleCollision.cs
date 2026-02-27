using Sandbox;
using System;

namespace Sandbox;

public sealed class ObstacleCollision : Component, Component.ICollisionListener, Component.ITriggerListener
{
	public void OnCollisionStart( Collision collision )
	{
		CheckHit( collision.Other.GameObject );
	}

	public void OnCollisionUpdate( Collision collision ) { }

	public void OnCollisionStop( Collision collision ) { }

	public void OnTriggerEnter( Collider other )
	{
		CheckHit( other.GameObject );
	}

	public void OnTriggerExit( Collider other ) { }

	void CheckHit( GameObject obj )
	{
		if ( obj == null || !obj.IsValid ) return;

		if ( obj.Tags.Has( "player" ) || obj.Tags.Has( "dino" ) )
		{
			var playerCharacter = obj.GetComponent<PlayerCharacter>();
			
			if ( playerCharacter != null && playerCharacter.IsValid )
			{
				if ( playerCharacter.GameStatusComponent != null && playerCharacter.GameStatusComponent.IsValid )
				{
					playerCharacter.GameStatusComponent.KillPlayer();
				}
			}
		}
	}
}