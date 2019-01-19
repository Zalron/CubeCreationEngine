using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelPlay {
	
	public class FastHashSet<V> 
	{
		int[] hashes;
		public DictionaryEntry[] entries;
		uint initialsize = 89;
		int nextfree;
		const float loadfactor = 1f;
		static readonly uint[] primeSizes = new uint[]{ 89, 179, 359, 719, 1439, 2879, 5779, 11579, 23159, 46327,
			92657, 185323, 370661, 741337, 1482707, 2965421, 5930887, 11861791,
			23723599, 47447201, 94894427, 189788857, 379577741, 759155483};

		//int maxitems = (int)( initialsize * loadfactor );

		public struct DictionaryEntry
		{
			public uint key;
			public int next;
			public V value;
		}


		public FastHashSet()
		{
			Initialize();
		}

		public FastHashSet(int capacity)
		{
			initialsize = FindNewSize((uint)capacity);
			Initialize();
		}


		public V GetAtPosition(int pos)
		{
			return entries[pos].value;
		}

		public void StoreAtPosition(int pos, V value)
		{
			entries[pos].value = value;
		}


		public bool GetOrAdd(uint key, bool overwrite, out int index) {
			index = GetPosition (key);
			if (index == -1) {
				index = Add (key, true);
				return false;
			}
			return true;
		}

		public int Add(uint key, V value, bool overwrite)
		{
			if (nextfree >= entries.Length)
				Resize();

			uint hashPos = key % (uint)hashes.Length;

			int entryLocation = hashes[hashPos];

			int storePos = nextfree;


			if (entryLocation != -1) // already there
			{
				int currEntryPos = entryLocation;

				do
				{
					DictionaryEntry entry = entries[currEntryPos];

					// same key is in the dictionary
					if (key == entry.key)
					{
						if (!overwrite)
							return currEntryPos;

						storePos = currEntryPos;
						break; // do not increment nextfree - overwriting the value
					}

					currEntryPos = entry.next;

				} while (currEntryPos > -1);

				nextfree++;
			}
			else // new value
			{
				//hashcount++;
				nextfree++;
			}

			hashes[hashPos] = storePos;

			entries[storePos].next = entryLocation;
			entries[storePos].key = key;
			entries[storePos].value = value;
			return storePos;
		}


		public int Add(uint key, bool overwrite)
		{
			if (nextfree >= entries.Length)
				Resize();

			uint hashPos = key % (uint)hashes.Length;

			int entryLocation = hashes[hashPos];

			int storePos = nextfree;


			if (entryLocation != -1) // already there
			{
				int currEntryPos = entryLocation;

				do
				{
					DictionaryEntry entry = entries[currEntryPos];

					// same key is in the dictionary
					if (key == entry.key)
					{
						if (!overwrite)
							return currEntryPos;

						storePos = currEntryPos;
						break; // do not increment nextfree - overwriting the value
					}

					currEntryPos = entry.next;

				} while (currEntryPos > -1);

				nextfree++;
			}
			else // new value
			{
				//hashcount++;
				nextfree++;
			}

			hashes[hashPos] = storePos;

			entries[storePos].next = entryLocation;
			entries[storePos].key = key;
			return storePos;
		}


		private void Resize()
		{
			uint newsize = FindNewSize((uint)hashes.Length * 2 + 1);
			int[] newhashes = new int[newsize];
			DictionaryEntry[] newentries = new DictionaryEntry[newsize];

			Array.Copy(entries, newentries, nextfree);

			for (int i = 0; i < newsize; i++)
			{
				newhashes[i] = -1;
			}

			for (int i = 0; i < nextfree; i++)
			{
				uint pos = newentries[i].key % newsize;
				int prevpos = newhashes[pos];
				newhashes[pos] = i;

				if (prevpos != -1)
					newentries[i].next = prevpos;
			}

			hashes = newhashes;
			entries = newentries;

			//maxitems = (int) (newsize * loadfactor );
		}

		private uint FindNewSize(uint desiredCapacity)
		{
			for (int i = 0; i < primeSizes.Length; i++)
			{
				if (primeSizes[i] >= desiredCapacity)
					return primeSizes[i];
			}

			throw new NotImplementedException("Too large array");
		}

		public V Get(uint key)
		{
			int pos = GetPosition(key);

			if (pos == -1)
				throw new Exception("Key does not exist");

			return entries[pos].value;
		}

		public int GetPosition(uint key)
		{
			uint pos = key % (uint)hashes.Length;

			int entryLocation = hashes[pos];

			if (entryLocation == -1)
				return -1;

			int nextpos = entryLocation;

			do
			{
				DictionaryEntry entry = entries[nextpos];

				if (key == entry.key)
					return nextpos;

				nextpos = entry.next;

			} while (nextpos != -1);

			return -1;
		}

		public bool ContainsKey(uint key)
		{
			return GetPosition(key) != -1;
		}

		public bool TryGetValue(uint key, out V value)
		{
			int pos = GetPosition(key);

			if (pos == -1)
			{
				value = default(V); 
				return false;
			}

			value = entries[pos].value;

			return true;
		}

		public V this[uint key]
		{
			get
			{
				return Get(key);
			}
			set
			{
				Add(key, value, true);
			}
		}

		public void Add(KeyValuePair<uint, V> item)
		{
			int pos = Add(item.Key, item.Value, false);

			if (pos + 1 != nextfree)
				throw new Exception("Key already exists");
		}

		public void Clear()
		{
			Initialize();
		}

		private void Initialize()
		{
			this.hashes = new int[initialsize];
			this.entries = new DictionaryEntry[initialsize];
			nextfree = 0;

			for (int i = 0; i < entries.Length; i++)
			{
				hashes[i] = -1;
			}
		}

		public int Count
		{
			get { return nextfree; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public void Add(uint key, object value)
		{
			int pos = Add(key, (V)value, false);

			if (pos + 1 != nextfree)
				throw new Exception("Key already exists");
		}


	}

}