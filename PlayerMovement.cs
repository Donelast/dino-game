using System;
namespace Sandbox;

public sealed class PlayerMovement : Component
{
	const float JumpPower = 29000;

	private Rigidbody _rigidbody;
	private SoundPointComponent _soundPoint;

	[Property, Range( 185, 650 )] float _playerSpeed = 185f;
	[Property] readonly SoundEvent _hitHurtSound = null;
	[Property] readonly SoundEvent _jumpSound = null;
	[Property] public bool IsGrounded = true;
	[Property] public PlayerStates CurrentState = PlayerStates.Playing;

	public enum PlayerStates
	{
		Playing,
		Dead,
		MainMenu
	}
	
	protected override void OnStart()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_soundPoint = GetComponent<SoundPointComponent>();
	
		if ( _hitHurtSound == null || _jumpSound == null )
		{
			Log.Warning( "One of the sounds is not specified in the inspector." );
		}
		if ( _rigidbody == null || !_rigidbody.IsValid || _soundPoint == null || !_soundPoint.IsValid )
		{
			Log.Error( "One of the components (Rigid body, Sound Point Component) is missing or invalid. The game cannot be started. ☜(ﾟヮﾟ) " );
			this.Enabled = false;
		}
	}

	protected override void OnUpdate()
    {
		if ( IsGrounded && Input.Down( "Jump" ) && CurrentState == PlayerStates.Playing)
        {
            IsGrounded = false;
			_soundPoint.StartSound();
			_rigidbody.ApplyForce(new Vector3(0, 0, JumpPower));
        }
		CheckStatus();
    }

    protected override void OnFixedUpdate()
    {
		if( CurrentState == PlayerStates.Playing )
		{
			_rigidbody.Velocity = new Vector3(0, -_playerSpeed, _rigidbody.Velocity.z);
		}
    }

	void CheckStatus()
	{
		if ( CurrentState == PlayerStates.Dead )
		{
			CurrentState = PlayerStates.MainMenu;
			_soundPoint.SoundEvent = _hitHurtSound;
			_soundPoint.StartSound();
			_soundPoint.SoundEvent = _jumpSound;
		}
	}
}
