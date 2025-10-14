using System.Threading.Tasks;
using static Sandbox.PlayerCharacter;

namespace Sandbox;

public sealed class PlayerAnimation : Component
{
	[Property] int _frameDelay = 130;
	[Property] public PlayerAnimations CurrentAnimation = PlayerAnimations.Running;
	[Property] bool ignorePlayerStatus = false;
	GameStatus _gameStatusComponent;

	PlayerCharacter _playerCharacterComponent;
	ModelRenderer _modelRender;
	bool _canPlayAnimation = true;

	readonly Model[] _runningModels = {
		Model.Load( "models/vmdl/dino/dino_1.vmdl" ),
		Model.Load( "models/vmdl/dino/dino_2.vmdl" ),
		Model.Load( "models/vmdl/dino/dino_3.vmdl" ),
		Model.Load( "models/vmdl/dino/dino_4.vmdl" ),
		Model.Load( "models/vmdl/dino/dino_5.vmdl" ),
		Model.Load( "models/vmdl/dino/dino_6.vmdl" ),
		Model.Load( "models/vmdl/dino/dino_7.vmdl" ),
		Model.Load( "models/vmdl/dino/dino_8.vmdl" )
	};

	int _frameIndex = 0;

	public enum PlayerAnimations
	{
		Running,
		Crouching,
	}

	protected override void OnStart()
	{
		_modelRender = GetComponent<ModelRenderer>();
		_playerCharacterComponent = GetComponent<PlayerCharacter>();
		_gameStatusComponent = _playerCharacterComponent?.GameStatusComponent;

		if ( ignorePlayerStatus && (_modelRender == null || !_modelRender.IsValid) )
		{
			Log.Error( "The rendering model required to play the animation is missing. Therefore, the component responsible for animation will be disabled in this game object." );
			this.Enabled = false;
		}
		if ( ignorePlayerStatus == false && (_playerCharacterComponent == null || !_playerCharacterComponent.IsValid) )
		{
			Log.Error( "Player Movement Component is missing or not enabled " );
			this.Enabled = false;
		}
		if ( _modelRender == null || !_modelRender.IsValid )
		{
			Log.Error( "ModelRenderer is missing or not enabled" );
			this.Enabled = false;
		}

		if ( _runningModels != null && _runningModels.Length > 0 && _modelRender != null )
		{
			_frameIndex = ((_frameIndex % _runningModels.Length) + _runningModels.Length) % _runningModels.Length;
			_modelRender.Model = _runningModels[_frameIndex];
		}
	}

	protected override void OnUpdate()
	{
		PlayFrameAnimation( CurrentAnimation );
	}

	async void PlayFrameAnimation( PlayerAnimations currentAnimation )
	{
		if ( _runningModels == null || _runningModels.Length == 0 || _modelRender == null || !_modelRender.IsValid )
			return;

		if ( ignorePlayerStatus )
		{
			if ( currentAnimation == PlayerAnimations.Running && _canPlayAnimation )
			{
				_canPlayAnimation = false;

				for ( int step = 0; step < _runningModels.Length; step++ )
				{
					int frame = _frameIndex % _runningModels.Length;
					_modelRender.Model = _runningModels[frame];

					await Task.Delay( _frameDelay );

					_frameIndex = (frame + 1) % _runningModels.Length;
				}

				_canPlayAnimation = true;
			}
		}
		else
		{
			if ( currentAnimation == PlayerAnimations.Running &&
				 _canPlayAnimation &&
				 _gameStatusComponent.CurrentState == GameStatus.PlayerStates.Playing &&
				 _playerCharacterComponent.IsGrounded )
			{
				_canPlayAnimation = false;

				for ( int step = 0;
					  step < _runningModels.Length &&
					  _gameStatusComponent.CurrentState == GameStatus.PlayerStates.Playing &&
					  currentAnimation == PlayerAnimations.Running;
					  step++ )
				{
					if ( !_playerCharacterComponent.IsGrounded )
					{
						_canPlayAnimation = true;
						return; 
					}

					int frame = _frameIndex % _runningModels.Length;
					_modelRender.Model = _runningModels[frame];

					await Task.Delay( _frameDelay );

					_frameIndex = (frame + 1) % _runningModels.Length;
				}

				_canPlayAnimation = true;
			}
		}
	}
}


