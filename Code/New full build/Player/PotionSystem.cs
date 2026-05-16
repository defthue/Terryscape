using Sandbox;
using System;
using System.Collections.Generic;

public enum BuffType
{
	None,
	Attack,
	Defence,
	Archery,
	Magic,
	AllDamage
}

public class ActiveBuff
{
	public BuffType Type;
	public float Percent;
	public float Duration;
	public float Remaining;
}

public sealed class PotionSystem : Component
{
	[Property] public float DrinkDuration { get; set; } = 1f;
	[Property] public float HealTickDuration { get; set; } = 2f;

	[Property] public int MinorHealAmount { get; set; } = 15;
	[Property] public int HealAmount { get; set; } = 30;
	[Property] public int GreaterHealAmount { get; set; } = 60;

	[Property] public float AttackBuffPercent { get; set; } = 15f;
	[Property] public float DefenceBuffPercent { get; set; } = 15f;
	[Property] public float ArcheryBuffPercent { get; set; } = 15f;
	[Property] public float MagicBuffPercent { get; set; } = 15f;
	[Property] public float ElixirBuffPercent { get; set; } = 20f;

	[Property] public float BuffDuration { get; set; } = 60f;
	[Property] public float ElixirDuration { get; set; } = 60f;

	[Property] public float PotionCooldownDuration { get; set; } = 15f;

	public bool IsDrinking { get; set; }
	public bool IsHealTicking { get; private set; }
	public float DrinkTimer { get; set; }

	[Sync] public float PotionCooldownRemaining { get; set; }

	float _healTickTimer;
	float _healPerTick;
	float _healAccumulated;
	int _healTotal;

	List<ActiveBuff> _buffs = new();

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( IsDrinking )
		{
			DrinkTimer -= Time.Delta;
			if ( DrinkTimer <= 0f )
				IsDrinking = false;
		}

		if ( PotionCooldownRemaining > 0f )
		{
			PotionCooldownRemaining -= Time.Delta;
			if ( PotionCooldownRemaining < 0f )
				PotionCooldownRemaining = 0f;
		}

		if ( IsHealTicking )
		{
			_healTickTimer += Time.Delta;
			float healed = _healPerTick * Time.Delta;
			_healAccumulated += healed;

			int toHeal = (int)_healAccumulated;
			if ( toHeal > 0 )
			{
				_healAccumulated -= toHeal;
				var health = Components.Get<PlayerHealth>();
				if ( health != null )
					health.Heal( toHeal );
			}

			if ( _healTickTimer >= HealTickDuration )
			{
				IsHealTicking = false;
				_healTickTimer = 0f;
				_healAccumulated = 0f;
			}
		}

		for ( int i = _buffs.Count - 1; i >= 0; i-- )
		{
			_buffs[i].Remaining -= Time.Delta;
			if ( _buffs[i].Remaining <= 0f )
			{
				GameLog.Add( $"{_buffs[i].Type} buff expired.", "#8a8f9a" );
				_buffs.RemoveAt( i );
			}
		}
	}

	public bool CanDrink()
	{
		return !IsDrinking && !IsHealTicking && PotionCooldownRemaining <= 0f;
	}

	public void StartPotionCooldown()
	{
		PotionCooldownRemaining = PotionCooldownDuration;
	}

	public bool TryDrinkPotion( ItemId potionId )
	{
		var inventory = Components.Get<Inventory>();
		if ( inventory == null )
			return false;

		var slots = inventory.GetSlots();
		for ( int i = 0; i < inventory.MaxSlots; i++ )
		{
			var slot = slots[i];
			if ( slot.IsStack && slot.ItemId == potionId )
				return TryDrinkPotionFromSlot( i );
		}

		return false;
	}

	public bool TryDrinkPotionFromSlot( int slotIndex )
	{
		var inventory = Components.Get<Inventory>();
		if ( inventory == null )
			return false;

		var slot = inventory.GetSlot( slotIndex );
		if ( slot == null || !slot.IsStack )
			return false;

		var potionId = slot.ItemId;

		if ( potionId == ItemId.LesserManaPotion || potionId == ItemId.ManaPotion || potionId == ItemId.GreaterManaPotion )
		{
			var manaSystem = Components.Get<ManaSystem>();
			if ( manaSystem != null )
				return manaSystem.TryDrinkManaPotionFromSlot( slotIndex );
			return false;
		}

		if ( !CanDrink() )
		{
			GameLog.Add( "You can't drink another potion yet.", "#c86464" );
			return false;
		}

		var def = ItemDatabase.Get( potionId );
		if ( def == null || def.Type != ItemType.Potion )
			return false;

		inventory.RemoveFromSlot( slotIndex, 1 );

		IsDrinking = true;
		DrinkTimer = DrinkDuration;
		StartPotionCooldown();

		string name = def.Name;

		switch ( potionId )
		{
			case ItemId.LesserHealingPotion:
				StartHealOverTime( MinorHealAmount );
				GameLog.Add( $"You drink a {name}. Healing {MinorHealAmount} HP over {HealTickDuration}s.", "#4caf78" );
				break;

			case ItemId.HealingPotion:
				StartHealOverTime( HealAmount );
				GameLog.Add( $"You drink a {name}. Healing {HealAmount} HP over {HealTickDuration}s.", "#4caf78" );
				break;

			case ItemId.GreaterHealingPotion:
				StartHealOverTime( GreaterHealAmount );
				GameLog.Add( $"You drink a {name}. Healing {GreaterHealAmount} HP over {HealTickDuration}s.", "#4caf78" );
				break;

			case ItemId.AttackPotion:
				ApplyBuff( BuffType.Attack, AttackBuffPercent, BuffDuration );
				GameLog.Add( $"You drink a {name}. +{AttackBuffPercent}% melee damage for {BuffDuration}s.", "#c9a84c" );
				break;

			case ItemId.DefencePotion:
				ApplyBuff( BuffType.Defence, DefenceBuffPercent, BuffDuration );
				GameLog.Add( $"You drink a {name}. +{DefenceBuffPercent}% armor for {BuffDuration}s.", "#c9a84c" );
				break;

			case ItemId.ArcheryPotion:
				ApplyBuff( BuffType.Archery, ArcheryBuffPercent, BuffDuration );
				GameLog.Add( $"You drink a {name}. +{ArcheryBuffPercent}% ranged damage for {BuffDuration}s.", "#c9a84c" );
				break;

			case ItemId.MagicPotion:
				ApplyBuff( BuffType.Magic, MagicBuffPercent, BuffDuration );
				GameLog.Add( $"You drink a {name}. +{MagicBuffPercent}% magic damage for {BuffDuration}s.", "#c9a84c" );
				break;

			case ItemId.ElixirOfPower:
				ApplyBuff( BuffType.AllDamage, ElixirBuffPercent, ElixirDuration );
				GameLog.Add( $"You drink a {name}. +{ElixirBuffPercent}% all damage for {ElixirDuration}s.", "#c9a84c" );
				break;

			default:
				GameLog.Add( $"You drink a {name}.", "#c9a84c" );
				break;
		}

		return true;
	}

	void StartHealOverTime( int totalHeal )
	{
		IsHealTicking = true;
		_healTickTimer = 0f;
		_healTotal = totalHeal;
		_healPerTick = (float)totalHeal / HealTickDuration;
		_healAccumulated = 0f;
	}

	void ApplyBuff( BuffType type, float percent, float duration )
	{
		for ( int i = _buffs.Count - 1; i >= 0; i-- )
		{
			if ( _buffs[i].Type == type )
			{
				_buffs.RemoveAt( i );
				break;
			}
		}

		_buffs.Add( new ActiveBuff
		{
			Type = type,
			Percent = percent,
			Duration = duration,
			Remaining = duration
		} );
	}

	public float GetBuffMultiplier( BuffType type )
	{
		float total = 0f;

		foreach ( var buff in _buffs )
		{
			if ( buff.Type == type || buff.Type == BuffType.AllDamage )
				total += buff.Percent;
		}

		return 1f + ( total / 100f );
	}

	public float GetDefenceBuffMultiplier()
	{
		float total = 0f;

		foreach ( var buff in _buffs )
		{
			if ( buff.Type == BuffType.Defence )
				total += buff.Percent;
		}

		return 1f + ( total / 100f );
	}

	public List<ActiveBuff> GetActiveBuffs()
	{
		return _buffs;
	}

	public bool HasBuff( BuffType type )
	{
		foreach ( var buff in _buffs )
		{
			if ( buff.Type == type )
				return true;
		}

		return false;
	}

	public void ClearAllBuffs()
	{
		_buffs.Clear();
		IsHealTicking = false;
		_healTickTimer = 0f;
		_healAccumulated = 0f;
	}
}
