using Sandbox;

public sealed class ManaSystem : Component
{
	[Property] public int BaseMana { get; set; } = 20;
	[Property] public int ManaPerLevel { get; set; } = 2;

	[Property] public float OocRegenRate { get; set; } = 2f;
	[Property] public float CombatRegenRate { get; set; } = 0.4f;

	[Property] public float CombatStateDuration { get; set; } = 8f;

	[Sync] public int CurrentMana { get; set; }
	[Sync] public int MaxMana { get; set; }

	float _regenAccum = 0f;
	float _lastCombatTime = -100f;

	public bool IsInCombat => Time.Now - _lastCombatTime < CombatStateDuration;

	public float GetManaDamageMultiplier()
	{
		return 1f;
	}

	public void MarkCombat()
	{
		_lastCombatTime = Time.Now;
	}

	protected override void OnStart()
	{
		UpdateMaxMana();
		CurrentMana = MaxMana;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		UpdateMaxMana();

		if ( CurrentMana < MaxMana )
		{
			float baseRate = IsInCombat ? CombatRegenRate : OocRegenRate;

			var inventory = Components.Get<Inventory>();
			var penalties = CombatTriangle.GetEquippedArmorPenalties( inventory );
			float rate = baseRate - penalties.ManaRegenPenalty;
			if ( rate < 0f ) rate = 0f;

			_regenAccum += rate * Time.Delta;

			int whole = (int)_regenAccum;
			if ( whole > 0 )
			{
				CurrentMana = System.Math.Min( CurrentMana + whole, MaxMana );
				_regenAccum -= whole;
			}
		}
	}

	void UpdateMaxMana()
	{
		var skills = Components.Get<Skills>();
		int magicLevel = skills != null ? skills.GetLevel( SkillType.Magic ) : 1;
		int baseMax = BaseMana + ( magicLevel - 1 ) * ManaPerLevel;

		var inventory = Components.Get<Inventory>();
		float focusBonus = inventory != null ? inventory.GetEnchantmentBonus( EnchantmentType.Focus ) : 0f;
		MaxMana = (int)( baseMax * ( 1f + focusBonus / 100f ) );

		if ( CurrentMana > MaxMana )
			CurrentMana = MaxMana;
	}

	public bool HasMana( int amount ) => CurrentMana >= amount;

	public bool ConsumeMana( int amount )
	{
		if ( CurrentMana < amount )
			return false;

		CurrentMana -= amount;
		MarkCombat();
		return true;
	}

	public void RestoreMana( int amount )
	{
		CurrentMana = System.Math.Min( CurrentMana + amount, MaxMana );
	}

	public bool TryDrinkManaPotion( ItemId potionId )
	{
		var inventory = Components.Get<Inventory>();
		if ( inventory == null )
			return false;

		var slots = inventory.GetSlots();
		for ( int i = 0; i < inventory.MaxSlots; i++ )
		{
			var slot = slots[i];
			if ( slot.IsStack && slot.ItemId == potionId )
				return TryDrinkManaPotionFromSlot( i );
		}

		return false;
	}

	public bool TryDrinkManaPotionFromSlot( int slotIndex )
	{
		if ( IsProxy )
			return false;

		var inventory = Components.Get<Inventory>();
		if ( inventory == null )
			return false;

		var slot = inventory.GetSlot( slotIndex );
		if ( slot == null || !slot.IsStack )
			return false;

		var potionId = slot.ItemId;
		int restoreAmount = 0;

		switch ( potionId )
		{
			case ItemId.LesserManaPotion: restoreAmount = 25; break;
			case ItemId.ManaPotion: restoreAmount = 60; break;
			case ItemId.GreaterManaPotion: restoreAmount = 120; break;
			default: return false;
		}

		var potionSystem = Components.Get<PotionSystem>();
		if ( potionSystem != null && !potionSystem.CanDrink() )
		{
			GameLog.Add( "You can't drink another potion yet.", "#c86464" );
			return false;
		}

		if ( CurrentMana >= MaxMana )
		{
			GameLog.Add( "You're already at full mana.", "#c86464" );
			return false;
		}

		inventory.RemoveFromSlot( slotIndex, 1 );
		RestoreMana( restoreAmount );

		if ( potionSystem != null )
			potionSystem.StartPotionCooldown();

		var def = ItemDatabase.Get( potionId );
		string name = def != null ? def.Name : "Mana Potion";
		GameLog.Add( $"You drink a {name}. Restored {restoreAmount} mana.", "#4a8ac8" );

		return true;
	}
}
