using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox;

namespace Sandbox;

public sealed class GameStatus : Component
{
	Random _random = new Random();
	[Property] public PlayerStates CurrentState = PlayerStates.Playing;
	[Property] public ulong CurrentScore { get; private set; } = 0;
	// 0 = день, 1 = ночь
	[Property] public float CurrentTime = 0;
	// Интервал очков между ночами: 250 -> ночи на 250, 500, 750, ...
	[Property, Group( "Difficulty" )] float PointsToNight = 250f;
	[Property, Group( "Day/Night" )] float TransitionSpeed = 0.5f;
	[Property, Group( "Day/Night" )] float NightHoldSeconds = 25.0f;
	[Property, Group( "Day/Night" )] List<GameObject> StarsGroup = null;
	[Property, Group( "Difficulty" ), Range( 900f, 80f )] private float _scoreDelay;

	public float ScoreDelay
	{
		get => _scoreDelay;
		set => _scoreDelay = Math.Clamp( value, 80f, 900f );
	}

	float _defaultScoreDelay;
	ObstacleGenerator _obstacleGeneratorComponent;
	PlayerCharacter _playerCharacterComponent;
	ColorGrading _colorGrading;

	bool canAddScore = true;
	float _nightHoldTimer = 0f;

	// Внутреннее: следующий порог для ночи
	ulong _nextNightAt = 0;
	ulong _lastInterval = 0;

	public enum PlayerStates
	{
		Playing,
		Dead,
		MainMenu
	}

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

		// Начинаем с дня
		SetDay();

		// Инициализируем первый порог
		RecomputeNextNightThreshold( force: true );

		var PlayerScore = Sandbox.Services.Stats.LocalPlayer.Get( "globalscore" );

	}

	protected override void OnFixedUpdate()
	{
		if ( CurrentState != PlayerStates.Playing )
			return;

		// Если в инспекторе изменили интервал — пересчитать ближайший следующий порог
		RecomputeNextNightThreshold();

		// Триггер ночи по достижению порога (и только если сейчас не удерживаем ночь)
		if ( _nextNightAt > 0 && CurrentScore >= _nextNightAt && _nightHoldTimer <= 0f )
		{
			SetNight();
			ulong interval = (ulong)MathF.Round( MathF.Max( 1f, PointsToNight ) );
			if ( interval == 0 ) interval = 1;
			_nextNightAt += interval;
		}

		// Тикаем удержание ночи
		if ( _nightHoldTimer > 0f )
		{
			_nightHoldTimer -= Time.Delta;
			if ( _nightHoldTimer < 0f ) _nightHoldTimer = 0f;
		}

		// Цель: 1 при ночи, иначе 0
		float target = _nightHoldTimer > 0f ? 1f : 0f;

		// Плавный переход
		CurrentTime = MoveTowards( CurrentTime, target, TransitionSpeed * Time.Delta );
		Scene.RenderAttributes.Set( "InvertAmount", CurrentTime );

		// Останавливаем/запускаем генерацию препятствий в ночи/днём
		if ( _obstacleGeneratorComponent != null )
		{
			_obstacleGeneratorComponent.StopGeneration = (_nightHoldTimer > 0f);
		}

		// Переключаем звёзды
		if ( StarsGroup != null )
		{
			bool starsActive = (_nightHoldTimer > 0f);
			foreach ( var star in StarsGroup )
			{
				if ( star != null )
					star.Enabled = starsActive;
			}
		}

		IncreaseDifficulty();
		AddScore();
	}

	protected override void OnUpdate()
	{
		if ( CurrentState == PlayerStates.Dead )
		{
			DisplayMainMenu();
			RestartGame();
			SetDay(); // смерть → вернуть день
		}
	}

	public void KillPlayer()
	{
		CurrentState = PlayerStates.Dead;
		_playerCharacterComponent._soundPoint.SoundEvent = _playerCharacterComponent._hitHurtSound;
		_playerCharacterComponent._soundPoint.StartSound();
		_playerCharacterComponent._soundPoint.SoundEvent = _playerCharacterComponent._jumpSound;
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
		_nightHoldTimer = NightHoldSeconds; // всегда сброс
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
	}

	public void StartGame()
	{
		// всегда стартуем днём
		SetDay();

		// сброс счёта и ночного таймера
		CurrentScore = 0;
		_nightHoldTimer = 0f;

		// пересчитать ночной порог заново
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

	public void IncreaseDifficulty()
	{
		ScoreDelay -= 0.4f;
		_playerCharacterComponent.PlayerSpeed += 0.15f;
		_obstacleGeneratorComponent.SpawnDelay -= 1;
		_obstacleGeneratorComponent.SpawnDistance -= 1;
	}

}
