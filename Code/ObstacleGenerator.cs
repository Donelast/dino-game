using System;
using System.Collections.Generic;
using Sandbox; 

namespace Sandbox;

public sealed class ObstacleGenerator : Component
{
	readonly static Random _random = new Random();

	[Property] public GameObject Player { get; private set; }
	public List<GameObject> SpawnedObjects = new List<GameObject>();
	[Property] public bool StopGeneration = false;

	[Property, Range( 500f, 10000f ), Group( "Difficulty" )] private float _spawnDistance;
	public float SpawnDistance
	{
		get => _spawnDistance;
		set => _spawnDistance = Math.Clamp( value, 500f, 10000f );
	}

	[Property, Range( 1000, 9000 ), Group( "Difficulty" )] private int _spawnDelay;
	public int SpawnDelay
	{
		get => _spawnDelay;
		set => _spawnDelay = Math.Clamp( value, 1000, 9000 );
	}

	public float DefaultSpawnDistance;
	public int DefaultSpawnDelay;
	GameStatus _gameStatusComponent;
	PlayerCharacter _playerCharacterComponent;

	readonly Model[] _cactusModels = { Model.Load( "models/vmdl/cactus.vmdl" ), Model.Load( "models/vmdl/cactus2.vmdl" ) };
	readonly Vector3 _defaultObjectPosition = new Vector3( -32641.779f, 0, 115.137f );
	
	float _timeSinceLastSpawn = 0f;
	float _nextSpawnDelayTimer = 0f;

	protected override void OnStart()
	{
		_gameStatusComponent = GetComponent<GameStatus>();
		_playerCharacterComponent = Player?.GetComponent<PlayerCharacter>();
		
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
		
		_nextSpawnDelayTimer = 0.1f; 
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

	void ObstacleGeneration()
	{
		if ( StopGeneration == false && _gameStatusComponent.CurrentState == GameStatus.PlayerStates.Playing )
		{
			_timeSinceLastSpawn += Time.Delta;
			
			if ( _timeSinceLastSpawn >= _nextSpawnDelayTimer )
			{
				SpawnRandomObstacle();
				_timeSinceLastSpawn = 0f;
			}
		}
	}	

	void SpawnRandomObstacle()
	{
		float obstacleChance = _random.NextSingle();
		string prefabName;
		bool isPterodactyl = false;
		
		Vector3 spawnPos = new Vector3( _defaultObjectPosition.x, Player.WorldPosition.y - SpawnDistance, _defaultObjectPosition.z );

		if ( obstacleChance < 0.09f && _gameStatusComponent.PterodactylsUnlocked )
		{
			prefabName = "prefabs/pterodactyl.prefab";
			isPterodactyl = true;
			
			int heightType = _random.Next( 0, 2 );
			if ( heightType == 0 ) spawnPos.z += 15f; 
			else spawnPos.z += 65f; 
		}
		else
		{
			float cactusChance = _random.NextSingle();
			if ( cactusChance >= 0.25f ) prefabName = "prefabs/cactus.prefab";
			else if ( cactusChance > 0.15f ) prefabName = "prefabs/two_cactus.prefab";
			else prefabName = "prefabs/three_cactus.prefab";
		}

		GameObject obj = GameObject.Clone( prefabName, new Transform( spawnPos, new Rotation(), scale: 1 ) );
		
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

		CalculateNextSpawnDelay( isPterodactyl );
	}

	void CalculateNextSpawnDelay( bool isPterodactyl )
	{
		float playerSpeed = _playerCharacterComponent != null ? _playerCharacterComponent.PlayerSpeed : 400f;
		float pteroSpeed = 250f; 
		
		float currentClosingSpeed = playerSpeed + (isPterodactyl ? pteroSpeed : 0f);
		float maxPossibleClosingSpeed = playerSpeed + pteroSpeed;

		float minGapDelay = (SpawnDelay * 0.4f) / 1000f; 
		float maxGapDelay = (SpawnDelay * 0.8f) / 1000f; 
		float safeGap = minGapDelay + _random.NextSingle() * (maxGapDelay - minGapDelay);

		_nextSpawnDelayTimer = safeGap + (SpawnDistance / currentClosingSpeed) - (SpawnDistance / maxPossibleClosingSpeed);

		if ( _nextSpawnDelayTimer < 0.2f ) 
		{
			_nextSpawnDelayTimer = 0.2f;
		}
	}

	Model RandomCactusModel()
	{
		return _cactusModels[_random.Next( 0, _cactusModels.Length )];
	}
}