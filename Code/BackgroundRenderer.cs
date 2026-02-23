using Sandbox;
using System.Collections.Generic;

public sealed class BackgroundRenderer : Component
{
	[Property, Group("Models")] public Model CactusModel { get; set; }
	[Property, Group("Models")] public Model RockModel { get; set; }

	// Списки координат. Твой код должен просто обновлять их.
	public List<Transform> CactusPositions = new();
	public List<Transform> RockPositions = new();

	protected override void OnUpdate()
	{
		// Тупо отрисовка того, что лежит в списках
		if ( CactusModel != null && CactusPositions.Count > 0 )
		{
			Graphics.DrawModelInstanced( CactusModel, CactusPositions.ToArray() );
		}

		if ( RockModel != null && RockPositions.Count > 0 )
		{
			Graphics.DrawModelInstanced( RockModel, RockPositions.ToArray() );
		}
	}
}