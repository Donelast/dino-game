using System;
namespace Sandbox;

public sealed class MapControllerComponent : Component
{
	readonly static Random _random = new Random();

	[Property] GameObject _player = null;
	//[Property] GameObject _playerStartPosition;
	[Property, Range( 950f, 1300 )] float _spawnDistance = 950f;
	[Property, Range( 5000, 2500 )] int _spawnDelay = 5000;

	readonly Model[] _cactusModels = { Model.Load( "models/vmdl/cactus.vmdl" ), Model.Load( "models/vmdl/cactus2.vmdl" ) };
	readonly Vector3 _defaultObjectPosition = new Vector3( -32641.779f, 0, 115.137f );

	bool _generateObstacle = true;

	protected override void OnFixedUpdate()
	{
		if ( _generateObstacle && _player.GetComponent<PlayerMovement>().CurrentState == PlayerMovement.PlayerStates.Playing )
		{
			ObstacleGeneration();
		}
		CheckingPlayerPosition();
	}

	void CheckingPlayerPosition()
	{
		const float MaxPlayerZ = 360f; const float MinPlayerZ = 35f;
		if ( _player.WorldPosition.z > MaxPlayerZ || _player.WorldPosition.z < MinPlayerZ )
		{
			GameRestart();
			Log.Warning( "The player has left the map area." );
		}
	}

	async void ObstacleGeneration()
	{
		_generateObstacle = false;
		await Task.Delay( _random.Next( Convert.ToInt32( _spawnDelay * 0.5 ), _spawnDelay ) );
		SpawnObject( RandomCactusPrefab() );
		_generateObstacle = true;
	}

	void SpawnObject( string prefabName )
	{
		GameObject obj = GameObject.Clone( prefabName, new Transform( new Vector3( _defaultObjectPosition.x, _player.WorldPosition.y - _spawnDistance, _defaultObjectPosition.z ), new Rotation(), scale: 1 ) );
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

	void GameRestart()
	{
		_player.GetComponent<PlayerMovement>().CurrentState = PlayerMovement.PlayerStates.Dead;
		_player.Enabled = false;
	}
}
