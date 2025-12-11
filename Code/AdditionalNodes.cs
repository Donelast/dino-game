using Sandbox;
using Sandbox.ModelEditor.Nodes;
using System;
using System.Threading.Tasks;

public static class AdditionalNodes
{
	[ActionGraphNode( "kill.player" )]
	[Title( "kill player" ), Category( "Scene" ), Icon( "back_hand" )]
	public static void CallMethod( GameStatus gameStatus )
	{
		if(gameStatus == null || !gameStatus.IsValid )
		{
			Log.Error( "You cannot kill a player because GameStatus is missing or not enabled." );
		}
		else
		{
			gameStatus.KillPlayer();
		}
	}
}
