using System;
namespace Sandbox;

public sealed class PlayerMovement : Component
{
	[Property, Range( 185, 650 )] float _playerSpeed = 185f;
	[Property] readonly SoundEvent _hitHurtSound = null;
	[Property] readonly SoundEvent _jumpSound = null;
	[Property] public bool IsGrounded = true;
	[Property] public PlayerStates CurrentState = PlayerStates.Playing;

	private Rigidbody _rigidbody;
	private SoundPointComponent _soundPoint;
	const float JumpPower = 29000;

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
			Log.Warning( "Not all sounds were configured in the inspector." );
		}
		if ( _rigidbody == null || !_rigidbody.IsValid || _soundPoint == null || !_soundPoint.IsValid )
		{
			Log.Error( "The game cannot be started. Possible reasons: the Rigid Body and Sound Point are missing or not enabled. ☜(ﾟヮﾟ) " );
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
    }

    protected override void OnFixedUpdate()
    {
		if( CurrentState == PlayerStates.Playing )
		{
			_rigidbody.Velocity = new Vector3(0, -_playerSpeed, _rigidbody.Velocity.z);
		}
    }

}
