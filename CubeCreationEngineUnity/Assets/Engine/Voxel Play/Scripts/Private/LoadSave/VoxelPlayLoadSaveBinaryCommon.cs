using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		List<string> saveVoxelDefinitionsList;
		Dictionary<VoxelDefinition, int> saveVoxelDefinitionsDict;
		Dictionary<Vector3, Vector3> saveVoxelCustomRotations;
		List<string> saveItemDefinitionsList;
		Dictionary<ItemDefinition, int> saveItemDefinitionsDict;

		void InitSaveGameStructs () {
			if (saveVoxelDefinitionsList == null) {
				saveVoxelDefinitionsList = new List<string> (100);
			} else {
				saveVoxelDefinitionsList.Clear ();
			}
			if (saveVoxelDefinitionsDict == null) {
				saveVoxelDefinitionsDict = new Dictionary<VoxelDefinition, int> (100);
			} else {
				saveVoxelDefinitionsDict.Clear ();
			}
			if (saveVoxelCustomRotations == null) {
				saveVoxelCustomRotations = new Dictionary<Vector3,Vector3> ();
			} else {
				saveVoxelCustomRotations.Clear ();
			}
			if (saveItemDefinitionsList == null) {
				saveItemDefinitionsList = new List<string> (100);
			} else {
				saveItemDefinitionsList.Clear ();
			}
			if (saveItemDefinitionsDict == null) {
				saveItemDefinitionsDict = new Dictionary<ItemDefinition, int> (100);
			} else {
				saveItemDefinitionsDict.Clear ();
			}
		}


		Vector3 DecodeVector3Binary (BinaryReader br) {
			Vector3 v = new Vector3 ();
			v.x = br.ReadSingle ();
			v.y = br.ReadSingle ();
			v.z = br.ReadSingle ();
			return v;
		}

		void EncodeVector3Binary (BinaryWriter bw, Vector3 v) {
			bw.Write (v.x);
			bw.Write (v.y);
			bw.Write (v.z);
		}
	}



}
