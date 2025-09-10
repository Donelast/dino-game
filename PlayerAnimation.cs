using static Sandbox.PlayerController;

namespace Sandbox;

public sealed class PlayerAnimation : Component
{
	PlayerMovement _playerMovementComponent;
	ModelRenderer _modelRender;
	bool _canPlayAnimation = true;
	readonly Model[] _runningModels = { Model.Load( "models/vmdl/dino/dino_1.vmdl" ), Model.Load( "models/vmdl/dino/dino_2.vmdl" ), Model.Load( "models/vmdl/dino/dino_3.vmdl" ), Model.Load( "models/vmdl/dino/dino_4.vmdl" ), Model.Load( "models/vmdl/dino/dino_5.vmdl" ), Model.Load( "models/vmdl/dino/dino_6.vmdl" ), Model.Load( "models/vmdl/dino/dino_7.vmdl" ), Model.Load( "models/vmdl/dino/dino_8.vmdl" ) };
	[Property] int _frameDelay = 130;
	[Property] public PlayerAnimations CurrentAnimation = PlayerAnimations.Running;

	public enum PlayerAnimations
	{
		Running,
		Crouching,
	}

	protected override void OnStart()
	{
		_modelRender = GetComponent<ModelRenderer>();
		_playerMovementComponent = GetComponent<PlayerMovement>();
	}

	protected override void OnUpdate()
	{
		PlayFrameAnimation( CurrentAnimation );
	}

	async void PlayFrameAnimation( PlayerAnimations currentAnimation )
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
