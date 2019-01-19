using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace VoxelPlay {

	public partial class VoxelPlayCharacterControllerBase : MonoBehaviour {


		[Header ("Start Position")]
		[Tooltip ("Places player on a random position in world which is flat. If this option is not enabled, the current gameobject transform position will be used.")]
		public bool startOnFlat = true;

		[Tooltip ("Number of terrain checks to determine a flat position. The more iterations the lower the resulting starting position.")]
		[Range (1, 100)]
		public int startOnFlatIterations = 50;

		[Header ("State Flags (Informative)")]
		[Tooltip ("Player is flying - can go up/down with E and Q keys.")]
		public bool isFlying;

		[Tooltip ("Player is moving (walk or run)")]
		public bool isMoving;

		[Tooltip ("Player is pressing any move key")]
		public bool isPressingMoveKeys;

		[Tooltip ("Player is running")]
		public bool isRunning;

		[Tooltip ("Player is either on water surface or under water")]
		public bool isInWater;

		[Tooltip ("Player is on water surface.")]
		public bool isSwimming;

		[Tooltip ("Player is below water surface.")]
		public bool isUnderwater;

		[Tooltip ("Player is on ground.")]
		public bool isGrounded;

		[Tooltip ("Player is crouched.")]
		public bool isCrouched;



		[Header ("Swimming")]

		// the sound played when character enters water.
		public AudioClip waterSplash;
		// an array of swim stroke sounds that will be randomly selected from.
		public AudioClip[] swimStrokeSounds;
		public float swimStrokeInterval = 8;

		[Header("Walking")]
		// an array of footstep sounds that will be randomly selected from.
		public AudioClip[] footstepSounds;

		[Range (0f, 1f)] public  float runstepLenghten = 0.7f;
		public float footStepInterval = 5;

		// the sound played when character leaves the ground.
		public AudioClip jumpSound;

		// the sound played when character touches back on ground.
		public AudioClip landSound;

		[Header("Other Sounds")]
		public AudioClip cancelSound;

		[Header("World Limits")]
		public bool limitBoundsEnabled;
		public Bounds limitBounds;

		/// <summary>
		/// Triggered when player enters a voxel if that voxel definition has triggerEnterEvent = true
		/// </summary>
		public event VoxelEvent OnVoxelEnter;

		/// <summary>
		/// Triggered when a player walks over a voxel if that voxel definition has triggerWalkEvent = true
		/// </summary>
		public event VoxelEvent OnVoxelWalk;


		// internal fields
		AudioSource m_AudioSource;
		int lastPosX, lastPosY, lastPosZ;
		int lastVoxelTypeIndex;
		float nextPlayerDamageTime;
		float lastDiveTime;
		float m_StepCycle;
		float m_NextStep;

		[NonSerialized]
		public VoxelHitInfo crosshairHitInfo;
		[NonSerialized]
		public	bool crosshairOnBlock;


		protected VoxelPlayPlayer _player;

		public VoxelPlayPlayer player {
			get {
				if (_player == null) {
					_player = GetComponent<VoxelPlayPlayer> ();
					if (_player == null) {
						_player = gameObject.AddComponent<VoxelPlayPlayer> ();
					}
				}
				return _player;
			}
		}

		[NonSerialized]
		public VoxelPlayEnvironment env;



		protected void Init() {
			m_AudioSource = GetComponent<AudioSource> ();
			if (m_AudioSource == null) {
				m_AudioSource = gameObject.AddComponent<AudioSource> ();
			}
			m_StepCycle = 0f;
			m_NextStep = m_StepCycle / 2f;


			// Check player can collide with voxels
			#if UNITY_EDITOR
			if (env!=null && Physics.GetIgnoreLayerCollision (gameObject.layer, env.layerVoxels)) {
				Debug.LogError ("Player can't collide with voxels. Please check physics collision matrix in Project settings or change Voxels Layer in VoxelPlayEnvironment component.");
			}
			#endif
		}

		/// <summary>
		/// Toggles on/off character light
		/// </summary>
		public void ToggleCharacterLight () {
			Light light = GetComponentInChildren<Light> ();
			if (light != null) {
				light.enabled = !light.enabled;
			}
			env.UpdateLights ();
			if (light.enabled) {
				env.ShowMessage ("<color=green>Player torch <color=yellow>ON</color></color>");
			} else {
				env.ShowMessage ("<color=green>Player torch <color=yellow>OFF</color></color>");
			}
		}

		/// <summary>
		/// Toggles on/off character light
		/// </summary>
		public void ToggleCharacterLight (bool state) {
			Light light = GetComponentInChildren<Light> ();
			if (light != null) {
				light.enabled = state;
			}
		}


		protected void CheckFootfalls () {
			if (isGrounded && !isInWater) {
				Vector3 curPos = transform.position;
				int x = (int)curPos.x;
				int y = (int)curPos.y;
				int z = (int)curPos.z;
				if (x != lastPosX || y != lastPosY || z != lastPosZ) {
					lastPosX = x;
					lastPosY = y;
					lastPosZ = z;
					VoxelIndex index = env.GetVoxelUnderIndex (curPos, true);
					if (index.typeIndex != lastVoxelTypeIndex) {
						lastVoxelTypeIndex = index.typeIndex;
						if (lastVoxelTypeIndex != 0) {
							VoxelDefinition vd = index.type;
							SetFootstepSounds (vd.footfalls, vd.landingSound, vd.jumpSound);
							if (vd.triggerWalkEvent && OnVoxelWalk!=null) {
								OnVoxelWalk(index.chunk, index.voxelIndex);
							}
							CheckDamage (vd);
						}
					}
				}
			}
		}

		protected void CheckDamage (VoxelDefinition voxelType) {
			if (voxelType == null)
				return;
			int playerDamage = voxelType.playerDamage;
			if (playerDamage > 0 && Time.time > nextPlayerDamageTime) {
				nextPlayerDamageTime = Time.time + voxelType.playerDamageDelay;
				player.life -= playerDamage;
			}
		}

		protected void CheckEnterTrigger(VoxelChunk chunk, int voxelIndex) {
			if (env.voxelDefinitions [chunk.voxels [voxelIndex].typeIndex].triggerEnterEvent && OnVoxelEnter != null) {
				OnVoxelEnter (chunk, voxelIndex);
			}
		}

		public void SetFootstepSounds (AudioClip[] footStepsSounds, AudioClip jumpSound, AudioClip landSound) {
			this.footstepSounds = footStepsSounds;
			this.jumpSound = jumpSound;
			this.landSound = landSound;
		}

		public void PlayLandingSound () {
			if (isInWater)
				return;
			m_AudioSource.clip = landSound;
			m_AudioSource.Play ();
			m_NextStep = m_StepCycle + .5f;
		}



		public void PlayJumpSound () {
			if (isInWater || isFlying)
				return;
			m_AudioSource.clip = jumpSound;
			m_AudioSource.Play ();
		}


		public void PlayCancelSound () {
			m_AudioSource.clip = cancelSound;
			m_AudioSource.Play ();
		}


		public void PlayWaterSplashSound () {
			if (Time.time - lastDiveTime < 1f)
				return;
			lastDiveTime = Time.time;
			m_NextStep = m_StepCycle + swimStrokeInterval;
			if (waterSplash != null) {
				m_AudioSource.clip = waterSplash;
				m_AudioSource.Play ();
//				AudioSource.PlayClipAtPoint(waterSplash, soundPosition);
			}
		}


		protected void ProgressStepCycle (Vector3 velocity, float speed) {
			if (velocity.sqrMagnitude > 0 && isPressingMoveKeys) {
				m_StepCycle += (velocity.magnitude + (speed * (isMoving ? 1f : runstepLenghten))) * Time.fixedDeltaTime;
			}

			if (!(m_StepCycle > m_NextStep)) {
				return;
			}

			m_NextStep = m_StepCycle + footStepInterval;

			PlayFootStepAudio ();
		}



		private void PlayFootStepAudio () {
			if (!isGrounded) {
				return;
			}
			if (footstepSounds == null || footstepSounds.Length == 0)
				return;
			// pick & play a random footstep sound from the array,
			// excluding sound at index 0
			int n;
			if (footstepSounds.Length == 1) {
				n = 0;
			} else {
				n = Random.Range (1, footstepSounds.Length);
			}
			m_AudioSource.clip = footstepSounds [n];
			m_AudioSource.PlayOneShot (m_AudioSource.clip);
			// move picked sound to index 0 so it's not picked next time
			footstepSounds [n] = footstepSounds [0];
			footstepSounds [0] = m_AudioSource.clip;
		}


		protected void ProgressSwimCycle (Vector3 velocity, float speed) {
			if (velocity.sqrMagnitude > 0 && isPressingMoveKeys) {
				m_StepCycle += (velocity.magnitude + speed) * Time.fixedDeltaTime;
			}

			if (!(m_StepCycle > m_NextStep)) {
				return;
			}

			m_NextStep = m_StepCycle + swimStrokeInterval;

			if (!isUnderwater) {
				PlaySwimStrokeAudio ();
			}
		}


		private void PlaySwimStrokeAudio () {
			if (swimStrokeSounds == null || swimStrokeSounds.Length == 0)
				return;
			// pick & play a random swim stroke sound from the array,
			// excluding sound at index 0
			int n;
			if (swimStrokeSounds.Length == 1) {
				n = 0;
			} else {
				n = Random.Range (1, swimStrokeSounds.Length);
			}
			m_AudioSource.clip = swimStrokeSounds [n];
			m_AudioSource.PlayOneShot (m_AudioSource.clip);
			// move picked sound to index 0 so it's not picked next time
			swimStrokeSounds [n] = swimStrokeSounds [0];
			swimStrokeSounds [0] = m_AudioSource.clip;
		}


	}
}
