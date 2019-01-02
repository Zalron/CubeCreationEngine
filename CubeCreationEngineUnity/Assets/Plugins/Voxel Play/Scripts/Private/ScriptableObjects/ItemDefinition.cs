using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

	public enum ITEM_CATEGORY
	{
		Voxel = 0,
		Torch = 1
	}

	[CreateAssetMenu (menuName = "Voxel Play/Item Definition", fileName = "ItemDefinition", order = 104)]
	public partial class ItemDefinition : ScriptableObject
	{
		public string title;
		public ITEM_CATEGORY category;

		public VoxelDefinition voxelType;

		[Tooltip("Icon used in the inventory panel.")]
		public Texture2D icon;

		[Tooltip("Prefab used when this item can be placed on the scene as a normal gameobject (ie. a torch)")]
		public GameObject prefab;

		[Tooltip("Resistance points for this item.")]
		public byte resistancePoints;

		[Tooltip("Damage what produces when player carries this weapon or item. A value of 0 means that current hitDamage won't be changed.")]
		public int hitDamage = 0;

		[Tooltip("Hit delay for this weapon or item. A value of 0 means that current hitDelay won't be changed.")]
		public float hitDelay = 0;

		[Tooltip ("Sound played when item is placed in the scene")]
		public AudioClip pickupSound;

	}

}