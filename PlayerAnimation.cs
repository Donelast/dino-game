using System.Threading.Tasks;
using static Sandbox.PlayerController;

namespace Sandbox;

public sealed class PlayerAnimation : Component
{
	[Property] int _frameDelay = 130;
	[Property] public PlayerAnimations CurrentAnimation = PlayerAnimations.Running;
	[Property] bool ignorePlayerStatus = false;

	PlayerMovement _playerMovementComponent;
	ModelRenderer _modelRender;
	bool _canPlayAnimation = true;
	readonly Model[] _runningModels = { Model.Load( "models/vmdl/dino/dino_1.vmdl" ), Model.Load( "models/vmdl/dino/dino_2.vmdl" ), Model.Load( "models/vmdl/dino/dino_3.vmdl" ), Model.Load( "models/vmdl/dino/dino_4.vmdl" ), Model.Load( "models/vmdl/dino/dino_5.vmdl" ), Model.Load( "models/vmdl/dino/dino_6.vmdl" ), Model.Load( "models/vmdl/dino/dino_7.vmdl" ), Model.Load( "models/vmdl/dino/dino_8.vmdl" ) };

	public enum PlayerAnimations
	{
		Running,
		Crouching,
	}

	protected override void OnStart()
	{
		_modelRender = GetComponent<ModelRenderer>();
		_playerMovementComponent = GetComponent<PlayerMovement>();
		if ( ignorePlayerStatus && (_modelRender == null || !_modelRender.IsValid) )
		{
			Log.Info( "The rendering model required to play the animation is missing. Therefore, the component responsible for animation will be disabled in this game object." );
			this.Enabled = false;
		}
		if ( ignorePlayerStatus == false && (_playerMovementComponent == null || !_playerMovementComponent.IsValid) )
		{
			Log.Info( "Player Movement Component is missing or not enabled " );
			this.Enabled = false;
		}
	}

	protected override void OnUpdate()
	{
		PlayFrameAnimation( CurrentAnimation );
	}

	async void PlayFrameAnimation( PlayerAnimations currentAnimation )
	{
		if ( ignorePlayerStatus )
		{
			if ( currentAnimation == PlayerAnimations.Running && _canPlayAnimation )
			{
				_canPlayAnimation = false;
				for ( int frame = 0; frame < _runningModels.Length; frame++ )
				{
					_modelRender.Model = _runningModels[frame];
					await Task.Delay( _frameDelay );
				}
				_canPlayAnimation = true;
			}
		}
		else
		{
			if ( currentAnimation == PlayerAnimations.Running && _canPlayAnimation && _playerMovementComponent.CurrentState == PlayerMovement.PlayerStates.Playing )
			{
				_canPlayAnimation = false;
				for ( int frame = 0; frame < _runningModels.Length && _playerMovementComponent.CurrentState == PlayerMovement.PlayerStates.Playing && currentAnimation == PlayerAnimations.Running; frame++ )
				{
					_modelRender.Model = _runningModels[frame];
					await Task.Delay( _frameDelay );
				}
				_canPlayAnimation = true;
			}
		}
	}

}
