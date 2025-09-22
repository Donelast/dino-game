using System;
using System.Threading.Tasks;
using Sandbox;

namespace Sandbox;

public sealed class GameStatus : Component
{
	[Property] public PlayerStates CurrentState = PlayerStates.Playing;

	[Property, Group( "Difficulty" )] public ulong Score { get; private set; } = 0;

	// 0 = день, 1 = ночь (плавный параметр в пост-проц)
	[Property] public float CurrentTime = 0;

	// Интервал очков между ночами: 250 -> ночи на 250, 500, 750, ...
	[Property, Group( "Difficulty" )]
	public float PointsToNight = 250f;

	[Property, Group( "Day/Night" )]
	public float TransitionSpeed = 0.5f;

	[Property, Group( "Day/Night" )]
	public float NightHoldSeconds = 25.0f;

	[Property, Group( "Difficulty" )] public int ScoreDelay = 1500;

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

		Scene.RenderAttributes.Set( "InvertAmount", CurrentTime );

		// Инициализируем первый порог
		RecomputeNextNightThreshold( force: true );
	}

	protected override void OnFixedUpdate()
	{
		if ( CurrentState != PlayerStates.Playing )
			return;

		// Если в инспекторе изменили интервал — пересчитать ближайший следующий порог
		RecomputeNextNightThreshold();

		// Триггер ночи по достижению порога (и только если сейчас не удерживаем ночь)
		if ( _nextNightAt > 0 && Score >= _nextNightAt && _nightHoldTimer <= 0f )
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

		// Плавный переход без мерцаний
		CurrentTime = MoveTowards( CurrentTime, target, TransitionSpeed * Time.Delta );
		Scene.RenderAttributes.Set( "InvertAmount", CurrentTime );

		// Останавливаем/запускаем генерацию препядствий в ночи/днём
		if ( _obstacleGeneratorComponent != null )
		{
			_obstacleGeneratorComponent.StopGeneration = (_nightHoldTimer > 0f);
		}

		AddScore();
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
			await Task.Delay( ScoreDelay );
			Score++;
			canAddScore = true;
		}
	}

	void SetDay()
	{
		_nightHoldTimer = 0f;
	}

	void SetNight()
	{
		_nightHoldTimer = MathF.Max( _nightHoldTimer, NightHoldSeconds );
	}

	void RecomputeNextNightThreshold( bool force = false )
	{
		ulong interval = (ulong)MathF.Round( MathF.Max( 1f, PointsToNight ) );
		if ( interval == 0 ) interval = 1;

		if ( force || interval != _lastInterval )
		{
			_lastInterval = interval;
			ulong k = (Score / interval) + 1;
			_nextNightAt = k * interval;
		}
	}

	static float MoveTowards( float current, float target, float maxDelta )
	{
		if ( MathF.Abs( target - current ) <= maxDelta )
			return target;
		return current + MathF.Sign( target - current ) * maxDelta;
	}
}
