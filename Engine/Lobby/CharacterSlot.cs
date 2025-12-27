using Godot;
using System;
using Spire.Protocol;

namespace Spire.Lobby;

public partial class CharacterSlot : Node
{
	[Export] public required Label NameLabel;
	[Export] public required Label LevelLabel;
	[Export] public required Label RaceLabel;
	[Export] public required TextureRect Portrait;

	public void Init(CharacterData data)
	{
		NameLabel.Text = data.Name;
		LevelLabel.Text = $"Level {data.Level}";
		RaceLabel.Text = data.Race.ToString();
	}
}
