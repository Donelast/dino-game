using System;
using System.Collections.Generic;
namespace Sandbox;

public sealed class ObstacleGenerator : Component
{
	readonly static Random _random = new Random();

	[Property] public GameObject Player { get; private set; }
	public List<GameObject> SpawnedObjects = new List<GameObject>();
	[Property] public bool StopGeneration = false;

	[Property, Range( 950f, 2600f ), Group( "Difficulty" )] private float _spawnDistance;
	public float SpawnDistance
	{
		get => _spawnDistance;
		set => _spawnDistance = Math.Clamp( value, 950f, 2600f );
	}

	[Property, Range( 3800, 9000 ), Group( "Difficulty" )] private int _spawnDelay;
	public int SpawnDelay
	{
		get => _spawnDelay;
		set => _spawnDelay = Math.Clamp( value, 3800, 9000 );
	}

	public float DefaultSpawnDistance;
	public int DefaultSpawnDelay;
	GameStatus _gameStatusComponent;
	readonly Model[] _cactusModels = { Model.Load( "models/vmdl/cactus.vmdl" ), Model.Load( "models/vmdl/cactus2.vmdl" ) };
	readonly Vector3 _defaultObjectPosition = new Vector3( -32641.779f, 0, 115.137f );
	bool _generateObstacle = true;

	protected override void OnStart()
	{
		_gameStatusComponent = GetComponent<GameStatus>();
		DefaultSpawnDistance = SpawnDistance;
		DefaultSpawnDelay = SpawnDelay;

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
			await Task.Delay( _random.Next( Convert.ToInt32( SpawnDelay * 0.5 ), SpawnDelay ) );
			if(StopGeneration == false)
			{
				SpawnObject( RandomCactusPrefab() );
			}
			_generateObstacle = true;
		}
	}	

	void SpawnObject( string prefabName )
	{
		GameObject obj = GameObject.Clone( prefabName, new Transform( new Vector3( _defaultObjectPosition.x, Player.WorldPosition.y - SpawnDistance, _defaultObjectPosition.z ), new Rotation(), scale: 1 ) );
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
		SpawnedObjects.Add( obj );
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
