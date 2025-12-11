using System;
namespace Sandbox;

public sealed class PlayerCharacter : Component
{
	[Property] public readonly ScorePanel _scorePanel = null;
	[Property] public readonly MainMenu _mainMenu = null;
	[Property] public readonly GameObject StartPosition = null;
	[Property, Group( "Sound" )] public readonly SoundEvent _hitHurtSound = null;
	[Property, Group( "Sound" )] public readonly SoundEvent _jumpSound = null;
	[Property, Group( "Movement" )] public bool IsGrounded = true;
	[Property, Group( "Movement" )] readonly public GameStatus GameStatusComponent;
	[Property, Range( 200, 650 ), Group( "Movement" )] private float _playerSpeed;

	public float PlayerSpeed
	{
		get => _playerSpeed;
		set => _playerSpeed = Math.Clamp( value, 200, 650 );
	}

	public Rigidbody _rigidbody;
	public SoundPointComponent _soundPoint;
	public float DefaultPlayerSpeed;

	const float JumpPower = 29000;

	public enum PlayerStates
	{
		Playing,
		Dead,
		MainMenu
	}

	private GameStatus.PlayerStates _lastState = GameStatus.PlayerStates.MainMenu;
	private bool _waitForJumpKeyUp = false;

	protected override void OnStart()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_soundPoint = GetComponent<SoundPointComponent>();
		DefaultPlayerSpeed = PlayerSpeed;

		if ( _hitHurtSound == null || _jumpSound == null )
		{
			Log.Warning( "Not all sounds were configured in the inspector." );
		}
		if ( _rigidbody == null || !_rigidbody.IsValid || _soundPoint == null || !_soundPoint.IsValid )
		{
			Log.Error( "The game cannot be started. Possible reasons: the Rigid Body and Sound Point are missing or not enabled. ☜(ﾟヮﾟ) " );
			this.Enabled = false;
		}
		if ( GameStatusComponent == null || !GameStatusComponent.IsValid )
		{
			Log.Error( "Game Status component not specified in the inspector or not enabled " );
			this.Enabled = false;
		}
	}

	protected override void OnUpdate()
	{
		if ( GameStatusComponent.CurrentState == GameStatus.PlayerStates.Playing && _lastState != GameStatus.PlayerStates.Playing )
		{
			_waitForJumpKeyUp = true;
		}
		if ( _waitForJumpKeyUp && !Input.Down( "Jump" ) )
		{
			_waitForJumpKeyUp = false;
		}
		_lastState = GameStatusComponent.CurrentState;

		if ( IsGrounded && Input.Down( "Jump" ) && GameStatusComponent.CurrentState == GameStatus.PlayerStates.Playing && !_waitForJumpKeyUp )
		{
			IsGrounded = false;
			_soundPoint.StartSound();
			_rigidbody.ApplyForce( new Vector3( 0, 0, JumpPower ) );
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( GameStatusComponent.CurrentState == GameStatus.PlayerStates.Playing )
		{
			_rigidbody.Velocity = new Vector3( 0, -PlayerSpeed, _rigidbody.Velocity.z );
		}
	}

}
