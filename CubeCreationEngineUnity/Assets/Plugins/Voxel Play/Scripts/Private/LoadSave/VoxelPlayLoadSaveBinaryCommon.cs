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

		List<string> saveVoxelDefinitionList;
		Dictionary<VoxelDefinition, int> saveVoxelDefinitionDict;
		Dictionary<Vector3, Vector3> saveVoxelCustomRotations;

		void InitSaveGameStructs () {
			if (saveVoxelDefinitionList == null) {
				saveVoxelDefinitionList = new List<string> (100);
			} else {
				saveVoxelDefinitionList.Clear ();
			}
			if (saveVoxelDefinitionDict == null) {
				saveVoxelDefinitionDict = new Dictionary<VoxelDefinition, int> (100);
			} else {
				saveVoxelDefinitionDict.Clear ();
			}
			if (saveVoxelCustomRotations == null) {
				saveVoxelCustomRotations = new Dictionary<Vector3,Vector3> ();
			} else {
				saveVoxelCustomRotations.Clear ();
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
