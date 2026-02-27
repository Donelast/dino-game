using System;
using Sandbox;

namespace Sandbox;

public sealed class PterodactylAnimation : Component
{
	[Property, Group("Animation")] int _frameDelay = 150; 
	[Property, Group("Movement")] public float FlightSpeed { get; set; } = 250f; 
	
	[Property, Group("Sound")] public SoundEvent FlapSound { get; set; }

	ModelRenderer _modelRenderer;
	
	readonly static Random _random = new Random();

	readonly Model[] _models = {
		Model.Load( "models/vmdl/pterodactyl/pterodactyl-1.vmdl" ),
		Model.Load( "models/vmdl/pterodactyl/pterodactyl-2.vmdl" ),
		Model.Load( "models/vmdl/pterodactyl/pterodactyl-3.vmdl" ),
		Model.Load( "models/vmdl/pterodactyl/pterodactyl-3.vmdl" ),
		Model.Load( "models/vmdl/pterodactyl/pterodactyl-2.vmdl" ),
		Model.Load( "models/vmdl/pterodactyl/pterodactyl-1.vmdl" )
	};

	int _frameIndex = 0;
	float _timeSinceLastFrame = 0f;

	protected override void OnStart()
	{
		_modelRenderer = GetComponent<ModelRenderer>();
		
		if ( _modelRenderer == null || !_modelRenderer.IsValid )
		{
			Log.Error( "ModelRenderer is missing on Pterodactyl!" );
			this.Enabled = false;
			return;
		}

		if ( _models != null && _models.Length > 0 )
		{
			_modelRenderer.Model = _models[_frameIndex];
		}
	}

	protected override void OnUpdate()
	{
		if ( _models == null || _models.Length == 0 || _modelRenderer == null || !_modelRenderer.IsValid )
			return;

		GameObject.WorldPosition += new Vector3( 0, FlightSpeed * Time.Delta, 0 );

		_timeSinceLastFrame += Time.Delta;
		float delaySeconds = _frameDelay / 1000.0f;

		if ( _timeSinceLastFrame >= delaySeconds )
		{
			_timeSinceLastFrame = 0f;
			_frameIndex++;
			
			if ( _frameIndex >= _models.Length )
			{
				_frameIndex = 0;
			}

			_modelRenderer.Model = _models[_frameIndex];

			if ( _frameIndex == 2 && FlapSound != null )
			{
				var flap = Sound.Play( FlapSound, GameObject.WorldPosition );
				
				flap.Pitch = 0.85f + (_random.NextSingle() * 0.3f);
			}
		}
	}
}