using System;
namespace Sandbox;

public sealed class ObstacleGenerator : Component
{
	readonly static Random _random = new Random();

	[Property] public GameObject Player { get; private set; }
	[Property, Range( 950f, 1300 ), Group( "Difficulty" )] float _spawnDistance = 950f;
	[Property, Range( 5000, 2500 ), Group( "Difficulty" )] int _spawnDelay = 5000;
	[Property] public bool StopGeneration = false;

	GameStatus _gameStatusComponent;
	readonly Model[] _cactusModels = { Model.Load( "models/vmdl/cactus.vmdl" ), Model.Load( "models/vmdl/cactus2.vmdl" ) };
	readonly Vector3 _defaultObjectPosition = new Vector3( -32641.779f, 0, 115.137f );
	bool _generateObstacle = true;

	protected override void OnStart()
	{
		_gameStatusComponent = GetComponent<GameStatus>();

		if ( _gameStatusComponent == null || !_gameStatusComponent.IsValid )
		{
			Log.Error( "Game Status Component is missing or not enabled" );
			this.Enabled = false;
		}
		if ( Player == null )
		{	
			Log.Error( "Player was not specified in the inspector" );
			this.Enabled = false;
		}
	}

	protected override void OnFixedUpdate()
	{
		ObstacleGeneration();
		CheckingPlayerPosition();
	}

	void CheckingPlayerPosition()
	{
		const float MaxPlayerZ = 360f; const float MinPlayerZ = 35f;
		if ( Player.WorldPosition.z > MaxPlayerZ || Player.WorldPosition.z < MinPlayerZ )
		{
			Log.Warning( "The player has left the map area." );
		}
	}

	async void ObstacleGeneration()
	{
		if( StopGeneration == false && _generateObstacle && _gameStatusComponent.CurrentState == GameStatus.PlayerStates.Playing )
		{
			_generateObstacle = false;
			await Task.Delay( _random.Next( Convert.ToInt32( _spawnDelay * 0.5 ), _spawnDelay ) );
			SpawnObject( RandomCactusPrefab() );
			_generateObstacle = true;
		}
	}	

	void SpawnObject( string prefabName )
	{
		GameObject obj = GameObject.Clone( prefabName, new Transform( new Vector3( _defaultObjectPosition.x, Player.WorldPosition.y - _spawnDistance, _defaultObjectPosition.z ), new Rotation(), scale: 1 ) );
		if ( obj.Tags.Has( "cactus" ) )
		{
			obj.GetComponent<ModelRenderer>().Model = RandomCactusModel();
		}
		else if ( obj.Tags.Has( "three_cactus" ) || obj.Tags.Has( "two_cactus" ) )
		{
			foreach ( GameObject child in obj.Children )
			{
				child.GetComponent<ModelRenderer>().Model = RandomCactusModel();
			}
		}
	}

	string RandomCactusPrefab()
	{
		float cactusChance = _random.NextSingle();
		if ( cactusChance >= 0.25f )
		{
			return "prefabs/cactus.prefab";
		}
		else if ( cactusChance > 0.15f )
		{
			return "prefabs/two_cactus.prefab";
		}
		else
		{
			return "prefabs/three_cactus.prefab";
		}
	}

	Model RandomCactusModel()
	{
		return _cactusModels[_random.Next( 0, _cactusModels.Length )];
	}

}
