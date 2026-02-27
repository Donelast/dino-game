using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox;

namespace Sandbox;

public sealed class GameStatus : Component
{
	Random _random = new Random();
	
	public enum PlayerStates
	{
		Playing,
		Dead,
		MainMenu
	}

	public enum DifficultyLevel
	{
		Easy,
		Medium,
		Hard
	}

	[Property] public PlayerStates CurrentState = PlayerStates.Playing;
	[Property] public ulong CurrentScore { get; private set; } = 0;
	[Property] public float CurrentTime = 0;
	[Property, Group( "Difficulty" )] float PointsToNight = 250f;
	
	[Property, Group( "Difficulty" )] public DifficultyLevel CurrentDifficulty { get; private set; }
	[Group( "Difficulty" )] public float EasyDuration = 15f;
	[Group( "Difficulty" )] public float MediumDuration = 15f;
	[Group( "Difficulty" )] public float HardDuration = 7f;
	[Property, Group( "Difficulty" )] public float SpeedDistanceMultiplier = 1.5f;

	[Property, Group( "Day/Night" )] float TransitionSpeed = 0.5f;
	[Property, Group( "Day/Night" )] float NightHoldSeconds = 25.0f;
	[Property, Group( "Day/Night" )] List<GameObject> StarsGroup = null;
	[Property, Group( "Day/Night" )] List<GameObject> ConstellationsGroup = null;
	
	[Property, Group("Effects")] public Blur CameraBlur { get; set; } 
	
	[Property, Group( "Difficulty" ), Range( 80f, 1400f )] private float _scoreDelay = 100f;

	public float ScoreDelay
	{
		get => _scoreDelay;
		set => _scoreDelay = Math.Clamp( value, 80f, 1400f );
	}

	public bool PterodactylsUnlocked { get; private set; } = false;

	float _defaultScoreDelay;

	ObstacleGenerator _obstacleGeneratorComponent;
	PlayerCharacter _playerCharacterComponent;
	ColorGrading _colorGrading;

	bool canAddScore = true;
	float _nightHoldTimer = 0f;
	float _difficultyTimer = 0f;

	ulong _nextNightAt = 0;
	ulong _lastInterval = 0;

	GameObject _activeConstellation = null;

	protected override void OnStart()
	{
		_obstacleGeneratorComponent = GetComponent<ObstacleGenerator>();
		_playerCharacterComponent = _obstacleGeneratorComponent?.Player?.GetComponent<PlayerCharacter>();
		_colorGrading = _obstacleGeneratorComponent?.Player?.GetComponent<ColorGrading>();
		
		_defaultScoreDelay = ScoreDelay;

		if ( _obstacleGeneratorComponent == null || !_obstacleGeneratorComponent.IsValid )
		{
			Log.Error( "Game Status Component is missing or not enabled" );
			Enabled = false;
			return;
		}

		if ( _playerCharacterComponent == null || !_playerCharacterComponent.IsValid )
		{
			Log.Error( "Player was not specified in the inspector" );
			Enabled = false;
			return;
		}

		if ( CameraBlur != null )
		{
			CameraBlur.Enabled = false;
		}

		SetDay();
		RecomputeNextNightThreshold( force: true );

		var PlayerScore = Sandbox.Services.Stats.LocalPlayer.Get( "globalscore" );
	}

	protected override void OnFixedUpdate()
	{
		if ( CurrentState != PlayerStates.Playing )
			return;

		RecomputeNextNightThreshold();

		if ( _nextNightAt > 0 && CurrentScore >= _nextNightAt && _nightHoldTimer <= 0f )
		{
			SetNight();
			ulong interval = (ulong)MathF.Round( MathF.Max( 1f, PointsToNight ) );
			if ( interval == 0 ) interval = 1;
			_nextNightAt += interval;
		}

		if ( _nightHoldTimer > 0f )
		{
			_nightHoldTimer -= Time.Delta;
			
			if ( _nightHoldTimer <= 0f ) 
			{
				_nightHoldTimer = 0f;
				PterodactylsUnlocked = true; 
			}
		}

		float target = _nightHoldTimer > 0f ? 1f : 0f;
		CurrentTime = MoveTowards( CurrentTime, target, TransitionSpeed * Time.Delta );
		Scene.RenderAttributes.Set( "InvertAmount", CurrentTime );

		if ( _obstacleGeneratorComponent != null )
		{
			_obstacleGeneratorComponent.StopGeneration = (_nightHoldTimer > 0f);
		}

		if ( StarsGroup != null )
		{
			bool starsActive = (_nightHoldTimer > 0f);
			foreach ( var star in StarsGroup )
			{
				if ( star != null )
					star.Enabled = starsActive;
			}
		}

		if ( ConstellationsGroup != null )
		{
			bool constellationActive = (_nightHoldTimer > 0f);

			if ( constellationActive )
			{
				if ( _activeConstellation == null && ConstellationsGroup.Count > 0 )
				{
					int idx = _random.Next( ConstellationsGroup.Count );
					_activeConstellation = ConstellationsGroup[idx];
				}

				foreach ( var obj in ConstellationsGroup )
				{
					if ( obj != null )
						obj.Enabled = (obj == _activeConstellation);
				}
			}
			else
			{
				foreach ( var obj in ConstellationsGroup )
				{
					if ( obj != null )
						obj.Enabled = false;
				}
				_activeConstellation = null;
			}
		}

		IncreaseDifficulty();
		UpdateDifficulty();
		AddScore();
		GiveAchievements();
	}

	protected override void OnUpdate()
	{
	}

	public async void KillPlayer()
	{
		if (CurrentState == PlayerStates.Dead) return;

		CurrentState = PlayerStates.Dead;
		
		Input.TriggerHaptics( 0.15f, 0.05f ); 
		
		if ( CameraBlur != null )
		{
			CameraBlur.Enabled = true;
			CameraBlur.Size = 0.25f; 
		}

		_playerCharacterComponent._soundPoint.StopSound();
		_playerCharacterComponent._soundPoint.Pitch = 1.0f;
		_playerCharacterComponent._soundPoint.SoundEvent = _playerCharacterComponent._hitHurtSound;
		_playerCharacterComponent._soundPoint.StartSound();
		
		await Task.Delay(100);

		if ( CameraBlur != null )
		{
			CameraBlur.Size = 0.15f; 
		}
		
		await Task.Delay(150);

		DisplayMainMenu();
		RestartGame();
		SetDay();
	}

	async void AddScore()
	{
		if ( canAddScore && CurrentState == PlayerStates.Playing )
		{
			canAddScore = false;
			await Task.Delay( (int)ScoreDelay );
			CurrentScore++;
			canAddScore = true;
		}
	}

	void SetDay()
	{
		_nightHoldTimer = 0f;
		CurrentTime = 0f;
		Scene.RenderAttributes.Set( "InvertAmount", CurrentTime );
	}

	void SetNight()
	{
		_nightHoldTimer = NightHoldSeconds;

		for ( int i = _obstacleGeneratorComponent.SpawnedObjects.Count - 1; i >= 0; i-- )
		{
			GameObject obj = _obstacleGeneratorComponent.SpawnedObjects[i];

			if ( obj != null && obj.IsValid )
			{
				float dist = Vector3.DistanceBetween( _playerCharacterComponent.WorldPosition, obj.WorldPosition );

				if ( dist > 1200f )
				{
					obj.Destroy();
					_obstacleGeneratorComponent.SpawnedObjects.RemoveAt( i );
				}
			}
		}
	}

	void RecomputeNextNightThreshold( bool force = false )
	{
		ulong interval = (ulong)MathF.Round( MathF.Max( 1f, PointsToNight ) );
		if ( interval == 0 ) interval = 1;

		if ( force || interval != _lastInterval )
		{
			_lastInterval = interval;
			ulong k = (CurrentScore / interval) + 1;
			_nextNightAt = k * interval;
		}
	}

	static float MoveTowards( float current, float target, float maxDelta )
	{
		if ( MathF.Abs( target - current ) <= maxDelta )
			return target;
		return current + MathF.Sign( target - current ) * maxDelta;
	}

	void DisplayMainMenu()
	{
		CurrentState = GameStatus.PlayerStates.MainMenu;
		_playerCharacterComponent._scorePanel.Enabled = false;
		_playerCharacterComponent._mainMenu.Enabled = true;
	}

	public void HideMainMenu()
	{
		CurrentState = GameStatus.PlayerStates.Playing;
		_playerCharacterComponent._scorePanel.Enabled = true;
		_playerCharacterComponent._mainMenu.Enabled = false;
		
		if ( CameraBlur != null )
		{
			CameraBlur.Enabled = false; 
		}
	}

	void RestartGame()
	{
		_playerCharacterComponent._rigidbody.Locking = new PhysicsLock { X = true, Y = true, Z = true, Pitch = true, Roll = true, Yaw = true };
		_obstacleGeneratorComponent.Player.WorldPosition = _playerCharacterComponent.StartPosition.WorldPosition;

		foreach ( GameObject obj in _obstacleGeneratorComponent.SpawnedObjects )
		{
			obj.Destroy();
		}

		_obstacleGeneratorComponent.SpawnedObjects.Clear();
		
		ScoreDelay = _defaultScoreDelay;
		_playerCharacterComponent.PlayerSpeed = _playerCharacterComponent.DefaultPlayerSpeed;
		
		_obstacleGeneratorComponent.SpawnDelay = _obstacleGeneratorComponent.DefaultSpawnDelay;
		_obstacleGeneratorComponent.SpawnDistance = _obstacleGeneratorComponent.DefaultSpawnDistance;
		
		_difficultyTimer = 0f;
		
		PterodactylsUnlocked = false;
		
		_playerCharacterComponent._soundPoint.SoundEvent = _playerCharacterComponent._jumpSound;
		_playerCharacterComponent._soundPoint.Pitch = 1.0f; 
	}

	public void StartGame()
	{
		SetDay();
		CurrentScore = 0;
		_nightHoldTimer = 0f;
		
		PterodactylsUnlocked = false;

		RecomputeNextNightThreshold( force: true );

		CurrentState = PlayerStates.Playing;

		_playerCharacterComponent._rigidbody.Locking = new PhysicsLock
		{
			X = true,
			Y = false,
			Z = false,
			Pitch = true,
			Roll = true,
			Yaw = true
		};
	}

	void IncreaseDifficulty()
	{
		if(CurrentTime == 0)
		{
			ScoreDelay -= 0.6f;
			_playerCharacterComponent.PlayerSpeed += 0.1f;
		}
	}

	void UpdateDifficulty()
	{
		_difficultyTimer -= Time.Delta;

		if ( _difficultyTimer <= 0f )
		{
			int rand = _random.Next( 0, 3 );
			
			if ( rand == 0 ) CurrentDifficulty = DifficultyLevel.Easy;
			else if ( rand == 1 ) CurrentDifficulty = DifficultyLevel.Medium;
			else CurrentDifficulty = DifficultyLevel.Hard;

			switch ( CurrentDifficulty )
			{
				case DifficultyLevel.Easy:
					_difficultyTimer = EasyDuration;
					break;
				case DifficultyLevel.Medium:
					_difficultyTimer = MediumDuration;
					break;
				case DifficultyLevel.Hard:
					_difficultyTimer = HardDuration;
					break;
			}
		}

		float speedBonus = _playerCharacterComponent.PlayerSpeed * SpeedDistanceMultiplier;

		switch ( CurrentDifficulty )
		{
			case DifficultyLevel.Easy:
				_obstacleGeneratorComponent.SpawnDelay = 2800;
				_obstacleGeneratorComponent.SpawnDistance = 1400f + speedBonus;
				break;

			case DifficultyLevel.Medium:
				_obstacleGeneratorComponent.SpawnDelay = 1900;
				_obstacleGeneratorComponent.SpawnDistance = 1300f + speedBonus;
				break;

			case DifficultyLevel.Hard:
				_obstacleGeneratorComponent.SpawnDelay = 1300;
				_obstacleGeneratorComponent.SpawnDistance = 1250f + speedBonus;
				break;
		}
	}

	void GiveAchievements()
	{
		if(CurrentScore >= 500)
		{
			Sandbox.Services.Achievements.Unlock( "500-points" );
		}
		if ( CurrentScore >= 1500 )
		{
			Sandbox.Services.Achievements.Unlock( "1500-points" );
		}
		if(CurrentTime == 1)
		{
			Sandbox.Services.Achievements.Unlock( "thefirstnight" );
		}
	}
}