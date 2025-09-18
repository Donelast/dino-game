namespace Sandbox;

public sealed class GameStatus : Component
{
	[Property] public PlayerStates CurrentState = PlayerStates.Playing;
	ObstacleGenerator _obstacleGeneratorComponent;

	protected override void OnStart()
	{
		_obstacleGeneratorComponent = GetComponent<ObstacleGenerator>();

		if(_obstacleGeneratorComponent == null || !_obstacleGeneratorComponent.IsValid)
		{
			Log.Error( "Game Status Component is missing or not enabled" );
			this.Enabled = false;
		}
	}

	public enum PlayerStates
	{
		Playing,
		Dead,
		MainMenu
	}

	void KillPlayer()
	{
		CurrentState = PlayerStates.Dead;
	}

}
