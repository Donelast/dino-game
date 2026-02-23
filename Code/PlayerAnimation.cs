using System;
using Sandbox;

namespace Sandbox;

public sealed class PlayerAnimation : Component
{
	[Property] int _frameDelay = 130;
	[Property] public PlayerAnimations CurrentAnimation = PlayerAnimations.Running;
	[Property] bool ignorePlayerStatus = false;
	GameStatus _gameStatusComponent;

	PlayerCharacter _playerCharacterComponent;
	ModelRenderer _modelRender;

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
	float _timeSinceLastFrame = 0f;

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
			Log.Error( "The rendering model required to play the animation is missing." );
			this.Enabled = false;
		}
		if ( ignorePlayerStatus == false && (_playerCharacterComponent == null || !_playerCharacterComponent.IsValid) )
		{
			Log.Error( "Player Movement Component is missing or not enabled" );
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
		UpdateAnimation();
	}

	void UpdateAnimation()
	{
		if ( _runningModels == null || _runningModels.Length == 0 || _modelRender == null || !_modelRender.IsValid )
			return;

		bool shouldPlay = false;

		if ( ignorePlayerStatus )
		{
			shouldPlay = (CurrentAnimation == PlayerAnimations.Running);
		}
		else
		{
			if ( _gameStatusComponent.CurrentState == GameStatus.PlayerStates.Playing &&
				 _playerCharacterComponent.IsGrounded &&
				 CurrentAnimation == PlayerAnimations.Running )
			{
				shouldPlay = true;
			}
		}

		if ( shouldPlay )
		{
			_timeSinceLastFrame += Time.Delta;

			// Расчет задержки (уменьшаем, если скорость растет)
			int currentDelayMs = _frameDelay;
			if ( !ignorePlayerStatus && _playerCharacterComponent.PlayerSpeed > _playerCharacterComponent.DefaultPlayerSpeed )
			{
				float diff = _playerCharacterComponent.PlayerSpeed - _playerCharacterComponent.DefaultPlayerSpeed;
				int reduceAmount = (int)(diff / 10f);
				currentDelayMs -= reduceAmount;
			}

			if ( currentDelayMs < 10 ) currentDelayMs = 10;
			
			// Переводим миллисекунды в секунды для сравнения с Time.Delta
			float delaySeconds = currentDelayMs / 1000.0f;

			if ( _timeSinceLastFrame >= delaySeconds )
			{
				_timeSinceLastFrame = 0f;
				_frameIndex = (_frameIndex + 1) % _runningModels.Length;
				_modelRender.Model = _runningModels[_frameIndex];
			}
		}
	}
}