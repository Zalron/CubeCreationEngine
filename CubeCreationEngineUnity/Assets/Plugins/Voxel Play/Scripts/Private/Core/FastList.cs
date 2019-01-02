using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelPlay {

	// Fast index-based list
	public class FastList<T> {
		public T[] values;
		public int count;
		public T last;
		int fillCount;

		public FastList (int initialCapacity = 4) {
			values = new T[initialCapacity];
			count = 0;
			last = default(T);
			fillCount = 0;
		}

		public void Clear () {
			count = 0;
			last = default(T);
		}

		public int Add (T value) {
			if (count >= values.Length) {
				Array.Resize<T> (ref values, count * 2);
			}
			int index = count++;
			values [index] = last = value;
			fillCount++;
			return index;
		}

		public bool Contains (T value) {
			for (int k = 0; k < count; k++) {
				if (values [k]!=null && values[k].Equals (value))
					return true;
			}
			return false;
		}

		public int IndexOf (T value) {
			for (int k = 0; k < count; k++) {
				if (values [k]!=null && values[k].Equals (value))
					return k;
			}
			return -1;
		}

		public void RemoveAt(int index) {
			if (index < 0 || index >= count)
				return;
			for (int k = index; k < count - 1; k++) {
				values [k] = values [k + 1];
			}
			count--;
		}

		/// <summary>
		/// Removes the last added element
		/// </summary>
		public void RemoveLast() {
			if (count <= 0)
				return;
			--count;
			if (count > 0) {
				last = values [count - 1];
			} else {
				last = default(T);
			}
		}

		public T FetchDirty() {
			if (count >= fillCount) {
				return default(T);
			}
			last = values [count++];
			return last;
		}
	}

}
					