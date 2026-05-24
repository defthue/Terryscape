using System;

[Flags]
public enum SaveSection
{
	None = 0,
	Stats = 1 << 0,
	Skills = 1 << 1,
	Inventory = 1 << 2,
	Bank = 1 << 3,
	Progress = 1 << 4,
	Kills = 1 << 5,

	All = Stats | Skills | Inventory | Bank | Progress | Kills
}
